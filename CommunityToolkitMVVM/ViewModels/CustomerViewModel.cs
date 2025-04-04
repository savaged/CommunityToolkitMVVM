﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkitMVVM.Models;
using CommunityToolkitMVVM.Models.Extensions;
using CommunityToolkitMVVM.Services;
using CommunityToolkitMVVM.ViewModels.Messages;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CommunityToolkitMVVM.ViewModels
{
    public class CustomerViewModel : ViewModelBase, ISelectedItemViewModel<Customer>
    {
        private readonly IDataService<Customer> _dataService;
        private readonly ISystemDialogService _systemDialogService;
        private Customer? _customer;

        public CustomerViewModel(
            IBusyStateService busyStateService,
            IDataService<Customer> dataService,
            ISystemDialogService systemDialogService)
            : base(busyStateService)
        {
            _dataService = dataService ??
                throw new ArgumentNullException(nameof(dataService));

            _systemDialogService = systemDialogService ??
                throw new ArgumentNullException(nameof(systemDialogService));

            AddCmd = new AsyncRelayCommand(OnAdd, () => CanAdd);
            SaveCmd = new AsyncRelayCommand(OnSave, () => CanSave);
            DeleteCmd = new AsyncRelayCommand(OnDelete, () => CanDelete);
        }

        public Customer? SelectedItem
        {
            get => _customer;
            set
            {
                if (value == null && SelectedItem != null)
                {
                    SelectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
                }
                if (SetProperty(ref _customer, value))
                {
                    OnPropertyChanged(nameof(IsItemSelected));
                    if (SelectedItem != null)
                    {
                        SelectedItem.PropertyChanged += OnSelectedItemPropertyChanged;
                    }
                }
            }
        }

        public bool IsItemSelected => SelectedItem != null;

        public IAsyncRelayCommand AddCmd { get; }

        public IAsyncRelayCommand SaveCmd { get; }

        public IAsyncRelayCommand DeleteCmd { get; }

        public bool CanAdd => CanExecute;

        public bool CanSave => CanExecute && IsValidCustomer;

        public bool CanDelete => CanExecute && IsValidCustomer;

        private bool IsValidCustomer => SelectedItem.IsValid();

        private async Task OnAdd()
        {
            BusyStateService.RegisterIsBusy(nameof(OnAdd));
            SelectedItem = await _dataService.CreateAsync();
            BusyStateService.UnregisterIsBusy(nameof(OnAdd));
        }

        private async Task OnSave()
        {
            BusyStateService.RegisterIsBusy(nameof(OnSave));
            if (SelectedItem != null)
            {
                var savedAction = SavedAction._;
                if (SelectedItem.IsNullOrNew())
                {
                    await _dataService.InsertAsync(SelectedItem);
                    savedAction = SavedAction.Inserted;
                }
                else
                {
                    await _dataService.UpdateAsync(SelectedItem);
                    savedAction = SavedAction.Updated;
                }
                WeakReferenceMessenger.Default.Send(
                    new ModelSavedMessage<Customer>(savedAction, SelectedItem));
                SelectedItem = null;
            }
            BusyStateService.UnregisterIsBusy(nameof(OnSave));
        }

        private async Task OnDelete()
        {
            BusyStateService.RegisterIsBusy(nameof(OnDelete));
            if (SelectedItem != null)
            {
                if (_systemDialogService.ShowConfirmation())
                {
                    await _dataService.DeleteAsync(SelectedItem.Id);
                    WeakReferenceMessenger.Default.Send(
                        new ModelSavedMessage<Customer>(SavedAction.Deleted, SelectedItem));
                }
            }
            BusyStateService.UnregisterIsBusy(nameof(OnDelete));
        }

        private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Customer.FirstName) ||
                e.PropertyName == nameof(Customer.Surname))
            {
                SaveCmd.NotifyCanExecuteChanged();
                DeleteCmd.NotifyCanExecuteChanged();
            }
        }

    }
}
