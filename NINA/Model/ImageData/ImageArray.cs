﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Model.ImageData {

    public class ImageArray : IImageArray {

        private ImageArray() {
        }

        public ImageArray(ushort[] flatArray) : this(flatArray, null, null) {
        }

        public ImageArray(ushort[] flatArray, byte[] rawData, string rawType) {
            this.FlatArray = flatArray;
            this.RAWData = rawData;
            this.RAWType = rawType;
        }

        public ushort[] FlatArray { get; private set; }

        /// <summary>
        /// Contains RAW DSLR Data if available
        /// </summary>
        public byte[] RAWData { get; private set; }

        /// <summary>
        /// Contains the type of DSLR data (e.g. cr2)
        /// </summary>
        public string RAWType { get; private set; }
    }
}