﻿using NINA.EquipmentChooser;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    class FilterWheelVM : DockableVM {
        public FilterWheelVM() : base() {
            Title = "LblFilterWheel";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FWSVG"];

            ContentId = nameof(FilterWheelVM);
            ChooseFWCommand = new AsyncCommand<bool>(() => ChooseFW());
            CancelChooseFWCommand = new RelayCommand(CancelChooseFW);
            DisconnectCommand = new RelayCommand(DisconnectFW);
            RefreshFWListCommand = new RelayCommand(RefreshFWList);

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsyncRequest(
                new ChangeFilterWheelPositionMessageHandle(async (ChangeFilterWheelPositionMessage msg) => {
                    return await ChangeFilter(msg.Filter, msg.Token, msg.Progress);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new ConnectFilterWheelMessageHandle(async (ConnectFilterWheelMessage msg) => {
                    await ChooseFWCommand.ExecuteAsync(null);
                    return true;
                })
            );

            Mediator.Instance.RegisterRequest(
                new GetCurrentFilterInfoMessageHandle((GetCurrentFilterInfoMessage msg) => {
                    return SelectedFilter;
                })
            );
        }

        private CancellationTokenSource _changeFilterCancellationSource;
        private Task _changeFilterTask;
        private bool ChangeFilterHelper(FilterInfo filter) {
            _changeFilterCancellationSource?.Cancel();
            try {
                if (_changeFilterCancellationSource != null) {
                    _changeFilterTask?.Wait(_changeFilterCancellationSource.Token);
                }
            } catch (OperationCanceledException) {

            }
            _changeFilterCancellationSource = new CancellationTokenSource();
            _changeFilterTask = ChangeFilter(filter, _changeFilterCancellationSource.Token);

            return true;
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task<FilterInfo> ChangeFilter(FilterInfo filter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null) {
            progress?.Report(new ApplicationStatus() { Source = Locale.Loc.Instance["LblSwitchingFilter"] });

            //Lock access so only one instance can change the filter
            await semaphoreSlim.WaitAsync(token);
            try {
                var prevFilter = SelectedFilter;

                if (FW?.Connected == true && FW?.Position != filter.Position) {                    
                    IsMoving = true;
                    Task changeFocus = null;
                    if (Settings.FocuserUseFilterWheelOffsets) {
                        if (prevFilter != null) {
                            int offset = filter.FocusOffset - prevFilter.FocusOffset;
                            changeFocus = Mediator.Instance.RequestAsync(new MoveFocuserMessage() { Position = offset, Absolute = false, Token = token });                            
                        }
                    }

                    FW.Position = filter.Position;
                    var changeFilter = Task.Run(async () => {
                        while (FW.Position == -1) {
                            await Task.Delay(1000);
                            token.ThrowIfCancellationRequested();
                        }
                    });

                    if (changeFocus != null) {
                        await changeFocus;
                    }

                    await changeFilter;

                    IsMoving = false;
                }
                _selectedFilter = filter;
                RaisePropertyChanged(nameof(SelectedFilter));
                
            } finally {
                //unlock access
                semaphoreSlim.Release();
            }
            progress?.Report(new ApplicationStatus() { Source = string.Empty });
            return SelectedFilter;
        }

        private void RefreshFWList(object obj) {
            FilterWheelChooserVM.GetEquipment();
        }

        private bool _isMoving;
        public bool IsMoving {
            get {
                return _isMoving;
            }
            set {
                _isMoving = value;
                RaisePropertyChanged();
            }
        }

        private IFilterWheel _fW;
        public IFilterWheel FW {
            get {
                return _fW;
            }
            private set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo _selectedFilter;
        public FilterInfo SelectedFilter {
            get {
                return _selectedFilter;
            }
            set {
                ChangeFilterHelper(value);
                RaisePropertyChanged();
            }
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        private async Task<bool> ChooseFW() {
            await ss.WaitAsync();
            try {
                Disconnect();

                if (FilterWheelChooserVM.SelectedDevice.Id == "No_Device") {
                    Settings.FilterWheelId = FilterWheelChooserVM.SelectedDevice.Id;
                    return false;
                }

                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                });

                var fW = (IFilterWheel)FilterWheelChooserVM.SelectedDevice;
                _cancelChooseFilterWheelSource = new CancellationTokenSource();
                if (fW != null) {
                    try {
                        var connected = await fW?.Connect(_cancelChooseFilterWheelSource.Token);
                        _cancelChooseFilterWheelSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.FW = fW;
                            Notification.ShowSuccess(Locale.Loc.Instance["LblFilterwheelConnected"]);
                            Settings.FilterWheelId = FW.Id;
                            if (FW.Position > -1) {
                                SelectedFilter = FW.Filters[FW.Position];
                            }
                            return true;
                        } else {
                            this.FW = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (fW?.Connected == true) { Disconnect(); }
                        return false;
                    }


                } else {
                    return false;
                }
            } finally {
                ss.Release();
                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                });
            }            
        }

        private void CancelChooseFW(object o) {
            _cancelChooseFilterWheelSource?.Cancel();
        }

        CancellationTokenSource _cancelChooseFilterWheelSource;

        private void DisconnectFW(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            if (FW != null) {
                FW.Disconnect();
                FW = null;
                RaisePropertyChanged(nameof(FW));
            }
        }

        private FilterWheelChooserVM _filterWheelChooserVM;
        public FilterWheelChooserVM FilterWheelChooserVM {
            get {
                if (_filterWheelChooserVM == null) {
                    _filterWheelChooserVM = new FilterWheelChooserVM();
                }
                return _filterWheelChooserVM;
            }
            set {
                _filterWheelChooserVM = value;
            }
        }

        public IAsyncCommand ChooseFWCommand { get; private set; }
        public ICommand CancelChooseFWCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand RefreshFWListCommand { get; private set; }
    }

    class FilterWheelChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFilterwheel"]));

            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("FilterWheel")) {

                try {
                    AscomFilterWheel cam = new AscomFilterWheel(device.Key, device.Value);
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }

            if (Devices.Count > 0) {
                var selected = (from device in Devices where device.Id == Settings.FilterWheelId select device).First();
                SelectedDevice = selected;
            }
        }
    }



}
