﻿using NINA.Model.MyFocuser;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class FocuserVM : DockableVM {

        public FocuserVM () {
            Title = "LblFocuser";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];

            ContentId = nameof(FocuserVM);
            ChooseFocuserCommand = new RelayCommand(ChooseFocuser);
            DisconnectCommand = new RelayCommand(DisconnectFocuser);
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList);
            MoveFocuserCommand = new RelayCommand(MoveFocuser);
            HaltFocuserCommand = new RelayCommand(HaltFocuser);

            _updateFocuser = new DispatcherTimer();
            _updateFocuser.Interval = TimeSpan.FromMilliseconds(300);
            _updateFocuser.Tick += UpdateFocuser_Tick;

            Mediator.Instance.RegisterAsync(async (object o) => {
                int offset = (int)o;
                await MoveFocuserRelative(offset);                
            },AsyncMediatorMessages.MoveFocuserRelative);
        }

        private void HaltFocuser(object obj) {
            Focuser.Halt();
        }

        private void MoveFocuser(object obj) {
            Focuser.Move(TargetPosition);
        }

        private async Task MoveFocuserRelative(int offset) {
            if(Focuser?.Connected == true) { 
                await Task.Run(() => {
                    var pos = Focuser.Position + offset;
                    Focuser.Move(pos);
                });
            }
        }

        private void UpdateFocuser_Tick(object sender, EventArgs e) {
            if (Focuser?.Connected == true) {
                Focuser.UpdateValues();
            }
        }

        public void ChooseFocuser(object obj) {
            _updateFocuser.Stop();
            Focuser = (IFocuser)FocuserChooserVM.SelectedDevice;
            if (Focuser?.Connect() == true) {
                _updateFocuser.Start();
                TargetPosition = Focuser.Position;
                Settings.FocuserId = Focuser.Id;
                Focuser.PropertyChanged += Focuser_PropertyChanged;
            } else {
                Focuser = null;
            }
        }

        private void Focuser_PropertyChanged(object sender,System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(Focuser.Position)) {
                this.Position = Focuser.Position;
            }
        }

        private int _position;
        public int Position {
            get {
                return _position;
            }
            private set {
                _position = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.FocuserPositionChanged,_position);
            }
        }

        private int _targetPosition;
        public int TargetPosition {
            get {
                return _targetPosition;
            }
            set {
                _targetPosition = value;
                RaisePropertyChanged();
            }
        }

        public void DisconnectFocuser(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Focuser?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                _updateFocuser.Stop();
                Focuser?.Disconnect();
                Focuser = null;
                RaisePropertyChanged(nameof(Focuser));
            }
        }

        public void RefreshFocuserList(object obj) {
            FocuserChooserVM.GetEquipment();
        }

        private IFocuser _focuser;
        public IFocuser Focuser {
            get {
                return _focuser;
            }
            set {
                _focuser = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.FocuserChanged, _focuser);
            }
        }

        private FocuserChooserVM _focuserChooserVM;
        public FocuserChooserVM FocuserChooserVM {
            get {
                if (_focuserChooserVM == null) {
                    _focuserChooserVM = new FocuserChooserVM();
                }
                return _focuserChooserVM;
            }
            set {
                _focuserChooserVM = value;
            }
        }
        
        private DispatcherTimer _updateFocuser;

        public ICommand RefreshFocuserListCommand { get; private set; }

        public ICommand ChooseFocuserCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }
                
        public ICommand MoveFocuserCommand { get; private set; }
        
        public ICommand HaltFocuserCommand { get; private set; }        
    }

    class FocuserChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            Devices.Clear();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Focuser")) {

                try {
                    AscomFocuser focuser = new AscomFocuser(device.Key, device.Value);
                    Devices.Add(focuser);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }

            if (Devices.Count > 0) {
                var selected = (from device in Devices where device.Id == Settings.FocuserId select device).FirstOrDefault();
                SelectedDevice = selected;
            }
        }
    }
}
