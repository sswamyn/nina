﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Profile;
using NINA.Utility;

namespace NINA.ViewModel.Equipment.FlatDevice {

    public interface IFlatDeviceChooserVM {
        IDevice SelectedDevice { get; set; }

        void GetEquipment();
    }

    internal class FlatDeviceChooserVM : EquipmentChooserVM, IFlatDeviceChooserVM {

        public FlatDeviceChooserVM(IProfileService profileService) : base(profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblFlatDeviceNoDevice"]));

            Logger.Trace("Adding Alnitak Flat Devices");
            Devices.Add(new AlnitakFlipFlatSimulator(profileService));
            Devices.Add(new AlnitakFlatDevice(profileService));
            Devices.Add(new ArteskyFlatBox(profileService));
            Devices.Add(new PegasusAstroFlatMaster(profileService));
            DetermineSelectedDevice(profileService.ActiveProfile.FlatDeviceSettings.Id);
        }
    }
}