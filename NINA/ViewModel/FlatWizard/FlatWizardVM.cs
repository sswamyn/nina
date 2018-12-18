﻿#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Locale;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using Nito.AsyncEx;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardVM : DockableVM, ICameraConsumer {
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly ICameraMediator cameraMediator;
        private ApplicationStatus _status;
        private BinningMode binningMode;
        private double calculatedExposureTime;
        private double calculatedHistogramMean;
        private bool cameraConnected;
        private ObservableCollection<FlatWizardFilterSettingsWrapper> filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();
        private int flatCount;
        private CancellationTokenSource flatSequenceCts;
        private short gain;
        private BitmapSource image;
        private bool isPaused = false;
        private int mode;
        private PauseTokenSource pauseTokenSource;
        private FlatWizardFilterSettingsWrapper singleFlatWizardFilterSettings;
        private CameraInfo cameraInfo;
        private readonly ImagingVM imagingVM;

        public FlatWizardVM(IProfileService profileService,
                            ImagingVM imagingVM,
                            ICameraMediator cameraMediator,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FlatWizardSVG"];

            this.imagingVM = imagingVM;

            imagingVM.SetAutoStretch(false);
            imagingVM.SetDetectStars(false);

            this.applicationStatusMediator = applicationStatusMediator;
            this.cameraMediator = cameraMediator;

            flatSequenceCts = new CancellationTokenSource();
            pauseTokenSource = new PauseTokenSource();

            var progress = new Progress<ApplicationStatus>(p => Status = p);

            StartFlatSequenceCommand = new AsyncCommand<bool>(() => StartFlatCapture(new Progress<ApplicationStatus>(p => Status = p), pauseTokenSource.Token));
            CancelFlatExposureSequenceCommand = new RelayCommand(CancelFindExposureTime);
            PauseFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = true; pauseTokenSource.IsPaused = IsPaused; }));
            ResumeFlatExposureSequenceCommand = new RelayCommand(new Action<object>((obj) => { IsPaused = false; pauseTokenSource.IsPaused = IsPaused; }));

            SingleFlatWizardFilterSettings = new FlatWizardFilterSettingsWrapper(null, new FlatWizardFilterSettings {
                HistogramMeanTarget = profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget,
                HistogramTolerance = profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance,
                MaxFlatExposureTime = profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime,
                MinFlatExposureTime = profileService.ActiveProfile.CameraSettings.MinFlatExposureTime,
                StepSize = profileService.ActiveProfile.FlatWizardSettings.StepSize
            });

            SingleFlatWizardFilterSettings.CameraInfo = cameraInfo;

            FlatCount = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            BinningMode = profileService.ActiveProfile.FlatWizardSettings.BinningMode;

            Filters = new ObservableCollection<FlatWizardFilterSettingsWrapper>();

            profileService.ActiveProfile.FilterWheelSettings.PropertyChanged += UpdateFilterWheelsSettings;
            SingleFlatWizardFilterSettings.Settings.PropertyChanged += UpdateProfileValues;

            // first update filters

            UpdateFilterWheelsSettings(null, null);

            // then register consumer and get the cameraInfo so it's populated to all filters including the singleflatwizardfiltersettings

            this.cameraMediator.RegisterConsumer(this);
        }

        private enum FilterCaptureMode {
            SINGLE = 0,
            MULTI = 1
        }

        public BinningMode BinningMode {
            get {
                return binningMode;
            }
            set {
                binningMode = value;
                if (binningMode != profileService.ActiveProfile.FlatWizardSettings.BinningMode)
                    profileService.ActiveProfile.FlatWizardSettings.BinningMode = binningMode;
                RaisePropertyChanged();
            }
        }

        public double CalculatedExposureTime {
            get {
                return calculatedExposureTime;
            }
            set {
                calculatedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public double CalculatedHistogramMean {
            get {
                return calculatedHistogramMean;
            }
            set {
                calculatedHistogramMean = value;
                RaisePropertyChanged();
            }
        }

        public bool CameraConnected {
            get {
                return cameraConnected;
            }
            set {
                cameraConnected = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelFlatExposureSequenceCommand { get; }

        public ObservableCollection<FilterInfo> FilterInfos => new ObservableCollection<FilterInfo>(Filters.Select(f => f.Filter).ToList());

        public ObservableCollection<FlatWizardFilterSettingsWrapper> Filters {
            get {
                return filters;
            }
            set {
                filters = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand FindExposureTimeCommand { get; private set; }

        public int FlatCount {
            get {
                return flatCount;
            }
            set {
                flatCount = value;
                if (flatCount != profileService.ActiveProfile.FlatWizardSettings.FlatCount)
                    profileService.ActiveProfile.FlatWizardSettings.FlatCount = flatCount;
                RaisePropertyChanged();
            }
        }

        public short Gain {
            get {
                return gain;
            }
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        public BitmapSource Image {
            get {
                return image;
            }
            set {
                image = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPaused {
            get {
                return isPaused;
            }
            set {
                isPaused = value;
                RaisePropertyChanged();
            }
        }

        public int Mode {
            get {
                return mode;
            }
            set {
                mode = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand PauseFlatExposureSequenceCommand { get; }

        public RelayCommand ResumeFlatExposureSequenceCommand { get; }

        public FilterInfo SelectedFilter {
            get {
                return singleFlatWizardFilterSettings.Filter;
            }
            set {
                singleFlatWizardFilterSettings.Filter = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SingleFlatWizardFilterSettings));
            }
        }

        public FlatWizardFilterSettingsWrapper SingleFlatWizardFilterSettings {
            get {
                return singleFlatWizardFilterSettings;
            }
            set {
                singleFlatWizardFilterSettings = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand StartFlatSequenceCommand { get; private set; }

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                if (_status.Source == null)
                    _status.Source = Loc.Instance["LblFlatWizardCapture"];
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public IWindowService WindowService { get; set; } = new WindowService();

        private void CancelFindExposureTime(object obj) {
            flatSequenceCts?.Cancel();
        }

        private bool EvaluateUserPromptResult(ref double exposureTime, ref List<DataPoint> dataPoints, ref FlatWizardFilterSettingsWrapper settings, FlatWizardUserPromptVM flatsWizardUserPrompt) {
            WindowService.ShowDialog(flatsWizardUserPrompt, Loc.Instance["LblFlatUserPromptFailure"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow).Wait();
            if (!flatsWizardUserPrompt.Continue) {
                flatSequenceCts.Cancel();
                return true;
            } else {
                settings = flatsWizardUserPrompt.Settings;
                if (flatsWizardUserPrompt.Reset) {
                    exposureTime = settings.Settings.MinFlatExposureTime;
                    dataPoints = new List<DataPoint>();
                }
                return false;
            }
        }

        private async Task<bool> StartFindingExposureTimeSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FlatWizardFilterSettingsWrapper wrapper) {
            bool userPromptCancelled = false;
            double exposureTime = wrapper.Settings.MinFlatExposureTime;

            var status = new ApplicationStatus { Status = string.Format(Loc.Instance["LblFlatExposureCalcStart"], wrapper.Settings.MinFlatExposureTime), Source = Title };
            progress.Report(status);
            ImageArray imageArray = null;
            List<DataPoint> datapoints = new List<DataPoint>();
            TrendLine trendLine;

            double cameraBitDepthADU = Math.Pow(2, cameraInfo.BitDepth);

            do {
                // capture a flat
                var sequence = new CaptureSequence(exposureTime, "FLAT", wrapper.Filter, BinningMode, 1);
                sequence.Gain = Gain;
                imageArray = await imagingVM.CaptureImageWithoutHistoryAndThumbnail(sequence, ct, progress, false, true);
                Image = await ImageControlVM.StretchAsync(
                    imageArray.Statistics.Mean,
                    ImageAnalysis.CreateSourceFromArray(imageArray, System.Windows.Media.PixelFormats.Gray16),
                    profileService.ActiveProfile.ImageSettings.AutoStretchFactor);

                // add mean to statistics
                var currentMean = imageArray.Statistics.Mean;
                datapoints.Add(new DataPoint(exposureTime, currentMean));

                // recalculate mean ADU if the user changed it
                var histogramMeanAdu = wrapper.Settings.HistogramMeanTarget * cameraBitDepthADU;
                var histogramMeanAduTolerance = histogramMeanAdu * wrapper.Settings.HistogramTolerance;
                var histogramToleranceUpperBound = histogramMeanAdu + histogramMeanAduTolerance;
                var histogramToleranceLowerBound = histogramMeanAdu - histogramMeanAduTolerance;

                if (histogramToleranceLowerBound <= currentMean && histogramToleranceUpperBound >= currentMean) {
                    // if the currentMean is within the tolerance we're done
                    CalculatedExposureTime = exposureTime;
                    CalculatedHistogramMean = currentMean;
                    progress.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblFlatExposureCalcFinished"], CalculatedHistogramMean, CalculatedExposureTime), Source = Title });
                    break;
                } else if (currentMean > histogramMeanAdu + histogramMeanAduTolerance) {
                    // if the currentMean is above the mean + tolerance the flats are too bright
                    trendLine = new TrendLine(datapoints);
                    var expectedExposureTime = (histogramMeanAdu - trendLine.Offset) / trendLine.Slope;

                    if (expectedExposureTime < exposureTime && datapoints.Count >= 3) {
                        exposureTime = expectedExposureTime;
                    } else {
                        var flatsWizardUserPrompt = new FlatWizardUserPromptVM(Loc.Instance["LblFlatUserPromptFlatTooBright"],
                            currentMean, cameraBitDepthADU, wrapper, expectedExposureTime
                        );
                        userPromptCancelled = EvaluateUserPromptResult(ref exposureTime, ref datapoints, ref wrapper, flatsWizardUserPrompt);
                        if (userPromptCancelled) break;
                    }
                } else {
                    // we continue with trying to find the proper exposure time by increasing the next exposureTime step by StepSize
                    exposureTime += wrapper.Settings.StepSize;
                    progress.Report(new ApplicationStatus() { Status = string.Format(Loc.Instance["LblFlatExposureCalcContinue"], currentMean, exposureTime), Source = Title });
                }

                if (datapoints.Count >= 3) {
                    // if we have done 3 exposures already and are still not finished
                    // extrapolate the exposure time based on the previous exposures
                    trendLine = new TrendLine(datapoints);
                    exposureTime = (histogramMeanAdu - trendLine.Offset) / trendLine.Slope;
                }

                if ((exposureTime > wrapper.Settings.MaxFlatExposureTime || exposureTime < wrapper.Settings.MinFlatExposureTime)) {
                    // if the new exposure time is above the max exposure time and we are not finished
                    // prompt the user to adjust the flat brightness or mean because the max flat exposure time does not fulfill the requirements for this specific flat set
                    var flatsWizardUserPrompt = new FlatWizardUserPromptVM(Loc.Instance["LblFlatUserPromptFlatTooDim"],
                        currentMean, cameraBitDepthADU, wrapper, exposureTime
                    );
                    userPromptCancelled = EvaluateUserPromptResult(ref exposureTime, ref datapoints, ref wrapper, flatsWizardUserPrompt);
                    if (userPromptCancelled) break;
                }

                await WaitWhilePaused(progress, pt, ct);

                // collect garbage to reduce ram usage
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // throw a cancellation if user requested a cancel as well
                ct.ThrowIfCancellationRequested();
            } while (userPromptCancelled == false);

            if (userPromptCancelled) {
                // reset values when cancelled
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
            }

            return userPromptCancelled;
        }

        private async Task<PauseToken> WaitWhilePaused(IProgress<ApplicationStatus> progress, PauseToken pt, CancellationToken ct) {
            if (pt.IsPaused) {
                // if paused we'll wait until user unpaused here
                IsPaused = true;
                progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblPaused"], Source = Title });
                await pt.WaitWhilePausedAsync(ct);
                IsPaused = false;
            }

            return pt;
        }

        private async Task<bool> StartFlatCapture(IProgress<ApplicationStatus> progress, PauseToken pt) {
            if (flatSequenceCts.IsCancellationRequested) {
                flatSequenceCts = new CancellationTokenSource();
            }
            try {
                if ((FilterCaptureMode)Mode == FilterCaptureMode.SINGLE) {
                    await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, SingleFlatWizardFilterSettings);
                    await StartFlatCaptureSequence(progress, flatSequenceCts.Token, pt, SingleFlatWizardFilterSettings.Filter);
                } else {
                    foreach (var filterSettings in Filters.Where(f => f.IsChecked)) {
                        await StartFindingExposureTimeSequence(progress, flatSequenceCts.Token, pt, filterSettings);
                        await StartFlatCaptureSequence(progress, flatSequenceCts.Token, pt, filterSettings.Filter);
                        filterSettings.IsChecked = false;
                        CalculatedExposureTime = 0;
                        CalculatedHistogramMean = 0;
                    }
                }
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
            } catch (OperationCanceledException) {
                Utility.Notification.Notification.ShowWarning(Loc.Instance["LblFlatSequenceCancelled"]);
                progress.Report(new ApplicationStatus { Status = Loc.Instance["LblFlatSequenceCancelled"], Source = Title });
                CalculatedExposureTime = 0;
                CalculatedHistogramMean = 0;
            }

            imagingVM.DestroyImage();
            Image = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            progress.Report(new ApplicationStatus());
            progress.Report(new ApplicationStatus() { Source = Title });

            return true;
        }

        private async Task<bool> StartFlatCaptureSequence(IProgress<ApplicationStatus> progress, CancellationToken ct, PauseToken pt, FilterInfo filter) {
            CaptureSequence sequence = new CaptureSequence(CalculatedExposureTime, "FLAT", filter, BinningMode, FlatCount);
            sequence.Gain = Gain;
            while (sequence.ProgressExposureCount < sequence.TotalExposureCount) {
                await imagingVM.CaptureImageWithoutHistoryAndThumbnail(sequence, ct, progress, true, false);

                sequence.ProgressExposureCount++;

                if (pt.IsPaused) {
                    IsPaused = true;
                    progress.Report(new ApplicationStatus() { Status = Loc.Instance["LblPaused"], Source = Title });
                    await pt.WaitWhilePausedAsync(ct);
                    IsPaused = false;
                }

                ct.ThrowIfCancellationRequested();
            }

            return true;
        }

        private void UpdateFilterWheelsSettings(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            var selectedFilter = SelectedFilter;
            var newList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Select(s => new FlatWizardFilterSettingsWrapper(s, s.FlatWizardFilterSettings)).ToList();
            FlatWizardFilterSettingsWrapper[] tempList = new FlatWizardFilterSettingsWrapper[Filters.Count];
            Filters.CopyTo(tempList, 0);
            foreach (var item in tempList) {
                var newlistitem = newList.SingleOrDefault(f => f.Filter == item.Filter);
                Filters[Filters.IndexOf(item)] = newlistitem;
                newList.Remove(newlistitem);
            }

            foreach (var item in newList) {
                Filters.Add(item);
            }

            while (Filters.Contains(null)) {
                Filters.Remove(null);
            }

            if (selectedFilter != null) {
                SelectedFilter = Filters.FirstOrDefault(f => f.Filter.Name == selectedFilter.Name)?.Filter;
            }

            RaisePropertyChanged(nameof(Filters));
            RaisePropertyChanged(nameof(FilterInfos));
        }

        private void UpdateProfileValues(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget != SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramMeanTarget = SingleFlatWizardFilterSettings.Settings.HistogramMeanTarget;
            }
            if (profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance != SingleFlatWizardFilterSettings.Settings.HistogramTolerance) {
                profileService.ActiveProfile.FlatWizardSettings.HistogramTolerance = SingleFlatWizardFilterSettings.Settings.HistogramTolerance;
            }
            if (profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime != SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime) {
                profileService.ActiveProfile.CameraSettings.MaxFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MaxFlatExposureTime;
            }
            if (profileService.ActiveProfile.CameraSettings.MinFlatExposureTime != SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime) {
                profileService.ActiveProfile.CameraSettings.MinFlatExposureTime = SingleFlatWizardFilterSettings.Settings.MinFlatExposureTime;
            }
            if (profileService.ActiveProfile.FlatWizardSettings.StepSize != SingleFlatWizardFilterSettings.Settings.StepSize) {
                profileService.ActiveProfile.FlatWizardSettings.StepSize = SingleFlatWizardFilterSettings.Settings.StepSize;
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            cameraInfo = deviceInfo;
            CameraConnected = cameraInfo.Connected;
            foreach (var filter in Filters) {
                filter.CameraInfo = cameraInfo;
            }

            SingleFlatWizardFilterSettings.CameraInfo = cameraInfo;
        }
    }
}