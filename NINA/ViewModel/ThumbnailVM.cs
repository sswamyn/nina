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

using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Utility.RawConverter;
using nom.tam.fits;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class ThumbnailVM : DockableVM {

        public ThumbnailVM(IProfileService profileService, IImagingMediator imagingMediator) : base(profileService) {
            Title = "LblImageHistory";
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistorySVG"];

            this.imagingMediator = imagingMediator;

            this.imagingMediator.ImageSaved += ImagingMediator_ImageSaved;

            SelectCommand = new AsyncCommand<bool>((object o) => {
                return SelectImage((Thumbnail)o);
            });
        }

        private void ImagingMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            AddThumbnail(e);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(ImageSavedEventArgs msg) {
            return Task<bool>.Run(async () => {
                var factor = 100 / msg.Image.Width;

                BitmapSource scaledBitmap = new WriteableBitmap(new TransformedBitmap(msg.Image, new ScaleTransform(factor, factor)));
                scaledBitmap.Freeze();

                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var thumbnail = new Thumbnail(profileService.ActiveProfile.ImageSettings.HistogramResolution) {
                        ThumbnailImage = scaledBitmap,
                        ImagePath = msg.PathToImage,
                        FileType = msg.FileType,
                        Duration = msg.Duration,
                        Mean = msg.Mean,
                        HFR = msg.HFR,
                        Filter = msg.Filter,
                        StatisticsId = msg.StatisticsId,
                        IsBayered = msg.IsBayered
                    };
                    Thumbnails.Add(thumbnail);
                    SelectedThumbnail = thumbnail;
                }));
                return true;
            });
        }

        private Thumbnail _selectedThumbnail;

        public Thumbnail SelectedThumbnail {
            get {
                return _selectedThumbnail;
            }
            set {
                _selectedThumbnail = value;
                RaisePropertyChanged();
            }
        }

        private ObservableLimitedSizedStack<Thumbnail> _thumbnails;
        private IImagingMediator imagingMediator;
        public ICommand SelectCommand { get; set; }

        private async Task<bool> SelectImage(Thumbnail thumbnail) {
            var iarr = await thumbnail.LoadOriginalImage(profileService);
            if (iarr != null) {
                await imagingMediator.PrepareImage(iarr, new System.Threading.CancellationToken(), false);
                return true;
            } else {
                return false;
            }
        }

        public ObservableLimitedSizedStack<Thumbnail> Thumbnails {
            get {
                if (_thumbnails == null) {
                    _thumbnails = new ObservableLimitedSizedStack<Thumbnail>(50);
                }
                return _thumbnails;
            }
            set {
                _thumbnails = value;
                RaisePropertyChanged();
            }
        }
    }

    public class Thumbnail : BaseINPC {

        public Thumbnail(int histogramResolution) {
            this.histogramResolution = histogramResolution;
        }

        public async Task<ImageArray> LoadOriginalImage(IProfileService profileService) {
            ImageArray iarr = null;

            try {
                if (File.Exists(ImagePath.LocalPath)) {
                    iarr = await ImageArray.FromFile(ImagePath.LocalPath, (int)profileService.ActiveProfile.CameraSettings.BitDepth, IsBayered, profileService.ActiveProfile.ImageSettings.HistogramResolution, profileService.ActiveProfile.CameraSettings.RawConverter);
                    iarr.Statistics.Id = StatisticsId;
                } else {
                    Notification.ShowError("File does not exist");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return iarr;
        }

        public BitmapSource ThumbnailImage { get; set; }

        public double Mean { get; set; }

        public double HFR { get; set; }

        public bool IsBayered { get; set; }

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        private int histogramResolution;

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }

        public int StatisticsId { get; set; }
    }
}