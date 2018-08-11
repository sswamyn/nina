﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.CaptureSequence;

namespace NINA.ViewModel {

    internal class ImagingVM : DockableVM, ICameraConsumer {

        public ImagingVM(IProfileService profileService, CameraMediator cameraMediator, TelescopeMediator telescopeMediator, FilterWheelMediator filterWheelMediator) : base(profileService) {
            Title = "LblImaging";
            ContentId = nameof(ImagingVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => SnapImage(new Progress<ApplicationStatus>(p => Status = p)));
            CancelSnapCommand = new RelayCommand(CancelSnapImage);
            StartLiveViewCommand = new AsyncCommand<bool>(StartLiveView);
            StopLiveViewCommand = new RelayCommand(StopLiveView);

            ImageControl = new ImageControlVM(profileService, cameraMediator, telescopeMediator);

            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsyncRequest(
                new CaptureImageMessageHandle(async (CaptureImageMessage msg) => {
                    return await CaptureImage(msg.Sequence, msg.Token, msg.Progress);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new CapturePrepareAndSaveImageMessageHandle(async (CapturePrepareAndSaveImageMessage msg) => {
                    return await CaptureAndSaveImage(msg.Sequence, msg.Save, msg.Token, msg.Progress, msg.TargetName);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new CaptureAndPrepareImageMessageHandle(async (CaptureAndPrepareImageMessage msg) => {
                    return await CaptureAndPrepareImage(msg.Sequence, msg.Token, msg.Progress);
                })
            );
        }

        private ImageControlVM _imageControl;

        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get {
                return cameraInfo;
            }
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private bool _snapSubSample;

        public bool SnapSubSample {
            get {
                return _snapSubSample;
            }
            set {
                _snapSubSample = value;
                RaisePropertyChanged();
            }
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _liveViewCts;

        private async Task<bool> StartLiveView() {
            ImageControl.IsLiveViewEnabled = true;
            _liveViewCts = new CancellationTokenSource();
            await cameraMediator.LiveView(_liveViewCts.Token);
            return true;
        }

        private void StopLiveView(object o) {
            ImageControl.IsLiveViewEnabled = false;
            _liveViewCts?.Cancel();
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

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private bool _loop;

        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }
        }

        private bool _snapSave;

        public bool SnapSave {
            get {
                return _snapSave;
            }
            set {
                _snapSave = value;
                RaisePropertyChanged();
            }
        }

        private double _snapExposureDuration;
        private FilterWheelMediator filterWheelMediator;

        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds;

        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;

        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;
                RaisePropertyChanged();
            }
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }
        public IAsyncCommand StartLiveViewCommand { get; private set; }
        public ICommand StopLiveViewCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        private CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.FilterType != null) {
                await filterWheelMediator.ChangeFilter(seq.FilterType, token, progress);
            }
        }

        private void SetBinning(CaptureSequence seq) {
            if (seq.Binning == null) {
                cameraMediator.SetBinning(1, 1);
            } else {
                cameraMediator.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private void SetSubSample(CaptureSequence seq) {
            cameraMediator.SetSubSample(seq.EnableSubSample);
        }

        private async Task Capture(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            double duration = seq.ExposureTime;
            bool isLight = false;
            if (cameraInfo.HasShutter) {
                isLight = true;
            }

            await cameraMediator.Capture(duration, isLight, token, progress);
        }

        private Task<ImageArray> Download(CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDownloading"] });
            return cameraMediator.Download(token);
        }

        private async Task<bool> Dither(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            if (seq.Dither && ((seq.ProgressExposureCount % seq.DitherAmount) == 0)) {
                return await Mediator.Instance.RequestAsync(new DitherGuiderMessage() { Token = token });
            }
            token.ThrowIfCancellationRequested();
            return false;
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            var iarr = await CaptureImage(sequence, token, progress);
            if (iarr != null) {
                return await _currentPrepareImageTask;
            } else {
                return null;
            }
        }

        public async Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, string targetname = "") {
            //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitingForCamera"] });
            await semaphoreSlim.WaitAsync(token);

            if (cameraInfo.Connected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                semaphoreSlim.Release();
                return null;
            }

            return await Task.Run<ImageArray>(async () => {
                ImageArray arr = null;

                try {
                    if (cameraInfo.Connected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Change Filter*/
                    await ChangeFilter(sequence, token, progress);

                    if (cameraInfo.Connected != true) {
                        throw new CameraConnectionLostException();
                    }

                    token.ThrowIfCancellationRequested();

                    /*Set Camera Gain */
                    SetGain(sequence);

                    /*Set Camera Binning*/
                    SetBinning(sequence);

                    SetSubSample(sequence);

                    if (cameraInfo.Connected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /* Start RMS Recording */
                    Mediator.Instance.Request(new StartRMSRecordingMessage());

                    /*Capture*/
                    await Capture(sequence, token, progress);

                    /* Stop RMS Recording */
                    var rms = Mediator.Instance.Request(new StopRMSRecordingMessage());

                    token.ThrowIfCancellationRequested();

                    if (cameraInfo.Connected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Dither*/
                    var ditherTask = Dither(sequence, token, progress);

                    /*Download Image */
                    arr = await Download(token, progress);
                    if (arr == null) {
                        throw new OperationCanceledException();
                    }

                    if (cameraInfo.Connected != true) {
                        throw new CameraConnectionLostException();
                    }

                    //Wait for previous prepare image task to complete
                    if (_currentPrepareImageTask != null && !_currentPrepareImageTask.IsCompleted) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForImageProcessing"] });
                        await _currentPrepareImageTask;
                    }

                    var parameters = new ImageParameters() {
                        Binning = sequence.Binning.Name,
                        ExposureNumber = sequence.ProgressExposureCount,
                        ExposureTime = sequence.ExposureTime,
                        FilterName = sequence.FilterType?.Name ?? string.Empty,
                        ImageType = sequence.ImageType,
                        TargetName = targetname,
                        RecordedRMS = rms
                    };
                    _currentPrepareImageTask = ImageControl.PrepareImage(arr, token, bSave, parameters);

                    //Wait for dither to finish. Runs in parallel to download and save.
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblWaitForDither"] });
                    await ditherTask;
                } catch (System.OperationCanceledException ex) {
                    cameraMediator.AbortExposure();
                    throw ex;
                } catch (CameraConnectionLostException ex) {
                    Logger.Error(ex);
                    Notification.ShowError(Locale.Loc.Instance["LblCameraConnectionLost"]);
                    throw ex;
                } catch (Exception ex) {
                    Notification.ShowError(Locale.Loc.Instance["LblUnexpectedError"]);
                    Logger.Error(ex);
                    cameraMediator.AbortExposure();
                    throw ex;
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                    semaphoreSlim.Release();
                }
                return arr;
            });
        }

        private Task<BitmapSource> _currentPrepareImageTask;

        private void SetGain(CaptureSequence seq) {
            if (seq.Gain != -1) {
                cameraMediator.SetGain(seq.Gain);
            }
        }

        private Model.MyFilterWheel.FilterInfo _snapFilter;

        public Model.MyFilterWheel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;

        public BinningMode SnapBin {
            get {
                if (_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        private short _snapGain = -1;
        private CameraMediator cameraMediator;

        public short SnapGain {
            get {
                return _snapGain;
            }
            set {
                _snapGain = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> SnapImage(IProgress<ApplicationStatus> progress) {
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                    seq.EnableSubSample = SnapSubSample;
                    seq.Gain = SnapGain;
                    success = await CaptureAndSaveImage(seq, SnapSave, _captureImageToken.Token, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (OperationCanceledException) {
            } finally {
                await _currentPrepareImageTask;
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }

            return true;
        }

        public async Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "") {
            await CaptureImage(seq, ct, progress, bsave, targetname);
            return true;
        }

        public void UpdateCameraInfo(CameraInfo cameraStatus) {
            CameraInfo = cameraStatus;
        }
    }
}