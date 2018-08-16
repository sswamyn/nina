﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    internal class PlatesolveVM : DockableVM, ICameraConsumer, ITelescopeConsumer {
        public const string ASTROMETRYNETURL = "http://nova.astrometry.net";

        public PlatesolveVM(
                IProfileService profileService,
                CameraMediator cameraMediator,
                TelescopeMediator telescopeMediator,
                ImagingMediator imagingMediator
        ) : base(profileService) {
            Title = "LblPlateSolving";
            ContentId = nameof(PlatesolveVM);
            SyncScope = false;
            SlewToTarget = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
            this.imagingMediator = imagingMediator;

            SolveCommand = new AsyncCommand<bool>(() => CaptureSolveSyncAndReslew(new Progress<ApplicationStatus>(p => Status = p)));
            CancelSolveCommand = new RelayCommand(CancelSolve);

            SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
            SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
            Repeat = false;
            RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                SnapExposureDuration = profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
                SnapFilter = profileService.ActiveProfile.PlateSolveSettings.Filter;
                RepeatThreshold = profileService.ActiveProfile.PlateSolveSettings.Threshold;
            };
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = _status });
            }
        }

        private bool _syncScope;

        public bool SyncScope {
            get {
                return _syncScope;
            }
            set {
                _syncScope = value;
                if (!_syncScope && SlewToTarget) {
                    SlewToTarget = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _slewToTarget;

        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (_slewToTarget && !SyncScope) {
                    SyncScope = true;
                }
                if (!_slewToTarget && Repeat) {
                    Repeat = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _repeat;

        public bool Repeat {
            get {
                return _repeat;
            }
            set {
                _repeat = value;
                if (_repeat && !SlewToTarget) {
                    SlewToTarget = true;
                }
                RaisePropertyChanged();
            }
        }

        private double _repeatThreshold;

        public double RepeatThreshold {
            get {
                return _repeatThreshold;
            }
            set {
                _repeatThreshold = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;
        private Model.MyFilterWheel.FilterInfo _snapFilter;
        private double _snapExposureDuration;

        public BinningMode SnapBin {
            get {
                return _snapBin;
            }

            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private short _snapGain = -1;

        public short SnapGain {
            get {
                return _snapGain;
            }

            set {
                _snapGain = value;
                RaisePropertyChanged();
            }
        }

        private void ImageChanged(Object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Image") {
                this.PlateSolveResult = null;
            }
        }

        /// <summary>
        /// Syncs telescope to solved coordinates
        /// </summary>
        /// <returns></returns>
        private bool SyncronizeTelescope() {
            var success = false;

            if (TelescopeInfo.Connected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblUnableToSync"]);
                return false;
            }

            if (PlateSolveResult != null && PlateSolveResult.Success) {
                Coordinates solved = PlateSolveResult.Coordinates;
                solved = solved.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);  //Transform to JNow if required

                if (telescopeMediator.Sync(solved.RA, solved.Dec) == true) {
                    Notification.ShowSuccess(Locale.Loc.Instance["LblTelescopeSynced"]);
                    success = true;
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblSyncFailed"]);
                }
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCoordinatesForSync"]);
            }
            return success;
        }

        private BitmapSource _image;

        public BitmapSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                if (_image != null) {
                    var factor = 300 / _image.Width;

                    BitmapSource scaledBitmap = new WriteableBitmap(new TransformedBitmap(_image, new ScaleTransform(factor, factor)));
                    scaledBitmap.Freeze();
                    Thumbnail = scaledBitmap;
                }

                RaisePropertyChanged();
            }
        }

        private BitmapSource thumbnail;

        public BitmapSource Thumbnail {
            get {
                return thumbnail;
            }
            set {
                thumbnail = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Captures an image and solves it
        /// </summary>
        /// <param name="duration">   </param>
        /// <param name="progress">   </param>
        /// <param name="canceltoken"></param>
        /// <param name="filter">     </param>
        /// <param name="binning">    </param>
        /// <returns></returns>
        public async Task<PlateSolveResult> SolveWithCapture(CaptureSequence seq, IProgress<ApplicationStatus> progress, CancellationToken canceltoken, bool silent = false) {
            var oldAutoStretch = imagingMediator.SetAutoStretch(true);
            var oldDetectStars = imagingMediator.SetDetectStars(false);

            Image = await imagingMediator.CaptureAndPrepareImage(seq, canceltoken, progress);

            imagingMediator.SetAutoStretch(oldAutoStretch);
            imagingMediator.SetDetectStars(oldDetectStars);

            canceltoken.ThrowIfCancellationRequested();

            var success = await Solve(Image, progress, canceltoken, silent); ;
            Image = null;
            return success;
        }

        private async Task<bool> CaptureSolveSyncAndReslew(IProgress<ApplicationStatus> progress) {
            _solveCancelToken = new CancellationTokenSource();
            var seq = new CaptureSequence(SnapExposureDuration, CaptureSequence.ImageTypes.SNAP, SnapFilter, SnapBin, 1);
            seq.Gain = SnapGain;
            return await this.CaptureSolveSyncAndReslew(seq, this.SyncScope, this.SlewToTarget, this.Repeat, _solveCancelToken.Token, progress) != null;
        }

        /// <summary>
        /// Calls "SolveWithCaputre" and syncs + reslews afterwards if set
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task<PlateSolveResult> CaptureSolveSyncAndReslew(
                CaptureSequence seq,
                bool syncScope,
                bool slewToTarget,
                bool repeat,
                CancellationToken token,
                IProgress<ApplicationStatus> progress,
                bool silent = false,
                double repeatThreshold = 1.0d) {
            PlateSolveResult solveresult = null;
            bool repeatPlateSolve = false;
            do {
                solveresult = await SolveWithCapture(seq, progress, token, silent);

                if (solveresult != null && solveresult.Success) {
                    if (syncScope) {
                        if (TelescopeInfo.Connected != true) {
                            Notification.ShowWarning(Locale.Loc.Instance["LblUnableToSync"]);
                            return null;
                        }
                        var coords = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                        if (SyncronizeTelescope() && slewToTarget) {
                            await telescopeMediator.SlewToCoordinatesAsync(coords);
                        }
                    }
                }

                if (solveresult?.Success == true && repeat && Math.Abs(Astrometry.DegreeToArcmin(solveresult.RaError)) > repeatThreshold) {
                    repeatPlateSolve = true;
                    progress.Report(new ApplicationStatus() { Status = "Telescope not inside tolerance. Repeating..." });
                    //Let the scope settle
                    await Task.Delay(TimeSpan.FromSeconds(profileService.ActiveProfile.TelescopeSettings.SettleTime));
                } else {
                    repeatPlateSolve = false;
                }
            } while (repeatPlateSolve);

            RaiseAllPropertiesChanged();
            return solveresult;
        }

        /// <summary>
        /// Calculates the error based on the solved coordinates and the actual telescope coordinates
        /// and puts them into the PlateSolveResult
        /// </summary>
        private void CalculateError() {
            if (TelescopeInfo.Connected == true) {
                Coordinates solved = PlateSolveResult.Coordinates;
                solved = solved.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);

                var coords = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);

                PlateSolveResult.RaError = coords.RADegrees - solved.RADegrees;
                PlateSolveResult.DecError = coords.Dec - solved.Dec;
            }
        }

        /// <summary>
        /// Creates an instance of IPlatesolver, reads the image into memory and calls solve logic of platesolver
        /// </summary>
        /// <param name="progress">   </param>
        /// <param name="canceltoken"></param>
        /// <returns>true: success; false: fail</returns>
        public async Task<PlateSolveResult> Solve(BitmapSource source, IProgress<ApplicationStatus> progress, CancellationToken canceltoken, bool silent = false) {
            var solver = GetPlateSolver(source);
            if (solver == null) {
                return null;
            }

            var result = await Solve(solver, source, progress, canceltoken);

            if (!result?.Success == true) {
                MessageBoxResult dialog = MessageBoxResult.Yes;
                if (!silent) {
                    dialog = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblUseBlindSolveFailover"], Locale.Loc.Instance["LblPlatesolveFailed"], MessageBoxButton.YesNo, MessageBoxResult.Yes);
                }
                if (dialog == MessageBoxResult.Yes) {
                    solver = GetBlindSolver(source);
                    result = await Solve(solver, source, progress, canceltoken);
                    if (!result?.Success == true) {
                        Notification.ShowWarning(Locale.Loc.Instance["LblPlatesolveFailed"]);
                    }
                }
            }

            PlateSolveResult = result;
            progress.Report(new ApplicationStatus() { Status = string.Empty });
            return result;
        }

        /// <summary>
        /// Creates an instance of IPlatesolver, reads the image into memory and calls solve logic of platesolver
        /// </summary>
        /// <param name="progress">   </param>
        /// <param name="canceltoken"></param>
        /// <returns>true: success; false: fail</returns>
        public async Task<PlateSolveResult> BlindSolve(BitmapSource source, IProgress<ApplicationStatus> progress, CancellationToken canceltoken, bool silent = false, bool blind = false) {
            var solver = GetBlindSolver(source);
            if (solver == null) {
                return null;
            }

            var result = await Solve(solver, source, progress, canceltoken);

            progress.Report(new ApplicationStatus() { Status = string.Empty });
            return result;
        }

        private async Task<PlateSolveResult> Solve(IPlateSolver solver, BitmapSource source, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            BitmapFrame image = null;

            image = BitmapFrame.Create(source);
            /* Read image into memorystream */
            using (var ms = new MemoryStream()) {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder() { QualityLevel = 100 };
                encoder.Frames.Add(image);

                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                return await solver.SolveAsync(ms, progress, canceltoken);
            }
        }

        private CancellationTokenSource _solveCancelToken;

        private void CancelSolve(object o) {
            _solveCancelToken?.Cancel();
        }

        private IPlateSolver GetPlateSolver(BitmapSource img) {
            IPlateSolver solver = null;
            if (img != null) {
                Coordinates coords = null;
                if (TelescopeInfo.Connected == true) {
                    coords = new Coordinates(TelescopeInfo.RightAscension, TelescopeInfo.Declination, profileService.ActiveProfile.AstrometrySettings.EpochType, Coordinates.RAType.Hours);
                }
                var binning = CameraInfo.BinX;
                if (binning < 1) { binning = 1; }

                solver = PlateSolverFactory.CreateInstance(profileService, profileService.ActiveProfile.PlateSolveSettings.PlateSolverType, binning, img.Width, img.Height, coords);
            }

            return solver;
        }

        private IPlateSolver GetBlindSolver(BitmapSource img) {
            IPlateSolver solver = null;
            if (img != null) {
                var binning = CameraInfo.BinX;
                if (binning < 1) { binning = 1; }

                PlateSolverEnum type;
                if (profileService.ActiveProfile.PlateSolveSettings.BlindSolverType == BlindSolverEnum.LOCAL) {
                    type = PlateSolverEnum.LOCAL;
                } else {
                    type = PlateSolverEnum.ASTROMETRY_NET;
                }

                solver = PlateSolverFactory.CreateInstance(profileService, type, binning, img.Width, img.Height);
            }

            return solver;
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.CameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.TelescopeInfo = telescopeInfo;
        }

        private CameraMediator cameraMediator;
        private TelescopeMediator telescopeMediator;
        private ImagingMediator imagingMediator;

        public IAsyncCommand SolveCommand { get; private set; }

        public ICommand CancelSolveCommand { get; private set; }

        private AsyncObservableLimitedSizedStack<PlateSolveResult> _plateSolveResultList;

        public AsyncObservableLimitedSizedStack<PlateSolveResult> PlateSolveResultList {
            get {
                if (_plateSolveResultList == null) {
                    _plateSolveResultList = new AsyncObservableLimitedSizedStack<PlateSolveResult>(15);
                }
                return _plateSolveResultList;
            }
            set {
                _plateSolveResultList = value;
                RaisePropertyChanged();
            }
        }

        private PlateSolveResult _plateSolveResult;
        private CameraInfo cameraInfo;
        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get {
                return telescopeInfo ?? DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
            }
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public CameraInfo CameraInfo {
            get {
                return cameraInfo ?? DeviceInfo.CreateDefaultInstance<CameraInfo>();
            }
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;

                if (_plateSolveResult.Success) {
                    CalculateError();
                }
                PlateSolveResultList.Add(_plateSolveResult);

                RaisePropertyChanged();
            }
        }
    }
}