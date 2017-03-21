﻿using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Model {
    public class FilterWheelModel :BaseINPC {
        public FilterWheelModel() {

        }

        private void Init() {

        }

        private FilterWheel _fW;
        public FilterWheel FW {
            get {
                return _fW;
            }

            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        

        private string _progId;
        public string ProgId {
            get {
                return _progId;
            }

            set {
                _progId = value;
                RaisePropertyChanged();
            }
        }

        public bool Connected {
            get {
                var con = _connected;
                if(_connected) {
                    try {
                        con = FW.Connected;
                    } catch(Exception ex) {
                        Notification.ShowError(ex.Message);
                        disconnect();
                    }
                }
                return _connected;
            }

            set {
                _connected = value;
                if (FW != null) {
                    try {
                        FW.Connected = value;
                    } catch(Exception ex) {
                        Notification.ShowError(ex.Message);
                        _connected = false;
                    }                    
                }
                RaisePropertyChanged();
            }
        }

        private string _description;
        private string _name;
        private string _driverInfo;
        private string _driverVersion;
        private short _interfaceVersion;
        private int[] _focusOffsets;
        private string[] _names;
        private ArrayList _supportedActions;



        public string Description {
            get {
                return _description;
            }

            set {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        public string DriverInfo {
            get {
                return _driverInfo;
            }

            set {
                _driverInfo = value;
                RaisePropertyChanged();
            }
        }

        public string DriverVersion {
            get {
                return _driverVersion;
            }

            set {
                _driverVersion = value;
                RaisePropertyChanged();
            }
        }

        public short InterfaceVersion {
            get {
                return _interfaceVersion;
            }

            set {
                _interfaceVersion = value;
                RaisePropertyChanged();
            }
        }

        public int[] FocusOffsets {
            get {
                return _focusOffsets;
            }

            set {
                _focusOffsets = value;
                RaisePropertyChanged();
            }
        }

        public string[] Names {
            get {
                return _names;
            }

            set {
                _names = value;
                RaisePropertyChanged();
            }
        }

        public short Position {
            get {
                if(Connected) {
                    return FW.Position;
                } else {
                    return -1;
                }
                
            }

            set {                
                if (Connected) {
                    try {                 
                        FW.Position = value;                        
                    } catch (Exception ex) {
                        Logger.Trace(ex.Message);
                    
                    }
                    
                }                
                RaisePropertyChanged();
            }
        }

        public ArrayList SupportedActions {
            get {
                return _supportedActions;
            }

            set {
                _supportedActions = value;
                RaisePropertyChanged();
            }
        }

        private bool _connected;

        public bool Connect() {            
            bool con = false;
            string oldProgId = this.ProgId;
            string filterwheelid = Settings.FilterWheelId;
            ProgId = FilterWheel.Choose(filterwheelid);
            if ((!Connected || oldProgId != ProgId) && ProgId != "") {

                Init();
                try {
                    FW = new FilterWheel(ProgId);
                    
                    //AscomCamera.Connected = true;
                    Connected = true;
                    Settings.FilterWheelId = ProgId;
                    GetFWInfo();
                    Notification.ShowSuccess("Filter wheel connected");
                    con = true;
                }
                catch (ASCOM.DriverAccessCOMException ex) {
                    Notification.ShowError("Unable to connect to filter wheel");
                    Logger.Error("Unable to connect to filter wheel");
                    Logger.Trace(ex.Message);
                    Connected = false;
                }
                catch (Exception ex) {
                    Notification.ShowError("Unable to connect to filter wheel");
                    Logger.Error("Unable to connect to filter wheel");
                    Logger.Trace(ex.Message);
                    Connected = false;
                }
            }
            return con;
        }

        public void disconnect() {
            Connected = false;
            Position = -1;

            Filters.Clear();            
            
            FW.Dispose();
            Init();            
        }


        private void GetFWInfo() {
            try {
                Description = FW.Description;
                Name = FW.Name;
            } catch (Exception ex) {
                Logger.Error("Unable to connect to FilterWheel");
                Logger.Trace(ex.Message);


            }

            try {
                DriverInfo = FW.DriverInfo;
                DriverVersion = FW.DriverVersion;
                InterfaceVersion = FW.InterfaceVersion;
            }catch (Exception ex) {
                Logger.Warning("Used FilterWheel AscomDriver does not implement DriverInfo");
                Logger.Trace(ex.Message);
            }
            
            try {
                FocusOffsets = FW.FocusOffsets;
                Names = FW.Names;
                Position = FW.Position;

                var l = new AsyncObservableCollection<FilterInfo>();
                for (int i = 0; i < Names.Length; i++) {                    
                    l.Add(new FilterInfo(Names[i], FocusOffsets[i], (short)i));
                }
                Filters = l;
                
            } catch (Exception ex) {
                Logger.Warning("Used FilterWheel AscomDriver does not implement FocusOffsets, Names or Positions");
                Logger.Trace(ex.Message);
            }

            
            try {
                SupportedActions = FW.SupportedActions;
            } catch (Exception ex) {
                Logger.Warning("Used FilterWheel AscomDriver does not implement SupportedActions");
                Logger.Trace(ex.Message);

            }
        }

        private AsyncObservableCollection<FilterInfo> _filters;
        public AsyncObservableCollection<FilterInfo> Filters {
            get {                
                return _filters;
            }
            set {
                _filters = value;
                RaisePropertyChanged();
            }
        }

        public class FilterInfo :BaseINPC {
            private string _name;
            private int _focusOffset;
            private short _position;

            public string Name {
                get {
                    return _name;
                }

                set {
                    _name = value;
                    RaisePropertyChanged();
                }
            }

            public int FocusOffset {
                get {
                    return _focusOffset;
                }

                set {
                    _focusOffset = value;
                    RaisePropertyChanged();
                }
            }

            public short Position {
                get {
                    return _position;
                }

                set {
                    _position = value;
                    RaisePropertyChanged();
                }
            }

            public FilterInfo(string n, int offset, short position) {
                Name = n;
                FocusOffset = offset;
                Position = position;
            }              
        }
    }
}
