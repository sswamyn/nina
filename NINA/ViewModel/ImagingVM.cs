﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.CaptureSequence;
using System.ComponentModel;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyTelescope;
using NINA.Utility.Notification;
using Nito.AsyncEx;
using System.Security;
using System.Security.Permissions;
using System.Security.AccessControl;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;

namespace NINA.ViewModel {
    class ImagingVM : DockableVM {

        public ImagingVM() : base() {

            Title = "LblImaging";
            ContentId = nameof(ImagingVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => SnapImage(new Progress<string>(p => Status = p)));
            CancelSnapCommand = new RelayCommand(CancelSnapImage);

            ImageControl = new ImageControlVM();

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

            Mediator.Instance.Register((object o) => _cameraConnected = (bool)o, MediatorMessages.CameraConnectedChanged);
            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);

        }

        ImageControlVM _imageControl;
        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        private bool _cameraConnected;
        private bool CameraConnected {
            get {
                return Cam != null && _cameraConnected;
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = new ApplicationStatus() { Status = _status, Source = Title } });
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

        private ICamera _cam;
        public ICamera Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private double _snapExposureDuration;
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

        private bool _isExposing;
        public bool IsExposing {
            get {
                return _isExposing;
            }
            set {
                _isExposing = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.IsExposingUpdate, _isExposing);
            }
        }

        public IAsyncCommand SnapCommand { get; private set; }

        public ICommand CancelSnapCommand { get; private set; }

        private void CancelSnapImage(object o) {
            _captureImageToken?.Cancel();
        }

        CancellationTokenSource _captureImageToken;

        private async Task ChangeFilter(CaptureSequence seq, CancellationToken token, IProgress<string> progress) {
            if (seq.FilterType != null) {
                await Mediator.Instance.RequestAsync(new ChangeFilterWheelPositionMessage() { Filter = seq.FilterType, Token = token, Progress = progress });
            }
        }

        private void SetBinning(CaptureSequence seq) {
            if (seq.Binning == null) {
                Cam.SetBinning(1, 1);
            } else {
                Cam.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task Capture(CaptureSequence seq, CancellationToken token, IProgress<string> progress) {
            IsExposing = true;
            try {
                double duration = seq.ExposureTime;
                bool isLight = false;
                if (Cam.HasShutter) {
                    isLight = true;
                }
                Cam.StartExposure(duration, isLight);
                var start = DateTime.Now;
                var elapsed = 0.0d;
                ExposureSeconds = 0;
                progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                /* Wait for Capture */
                if (duration >= 1) {
                    await Task.Run(async () => {
                        do {
                            var delta = await Utility.Utility.Delay(500, token);
                            elapsed += delta.TotalSeconds;
                            ExposureSeconds = (int)elapsed;
                            token.ThrowIfCancellationRequested();
                            progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                        } while ((elapsed < duration) && Cam?.Connected == true);
                    });
                }
                token.ThrowIfCancellationRequested();
            } catch (System.OperationCanceledException ex) {
                Logger.Trace(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
            } finally {
                IsExposing = false;
            }


        }

        private async Task<ImageArray> Download(CancellationToken token, IProgress<string> progress) {
            progress.Report(ExposureStatus.DOWNLOADING);
            return await Cam.DownloadExposure(token);
        }

        private async Task<bool> Dither(CaptureSequence seq, CancellationToken token, IProgress<string> progress) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                progress.Report(ExposureStatus.DITHERING);

                return await Mediator.Instance.RequestAsync(new DitherGuiderMessage() { Token = token });                
            }
            token.ThrowIfCancellationRequested();
            return false;
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


        public async Task<BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<string> progress) {
            var iarr = await CaptureImage(sequence, token, progress);
            if(iarr != null) {
                return await _currentPrepareImageTask;
            } else {
                return null;
            }
        }

        public async Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<string> progress, bool bSave = false, string targetname = "") {

            //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released 
            progress.Report("Another process already uses Camera. Waiting for it to finish...");
            await semaphoreSlim.WaitAsync(token);

            if (CameraConnected != true) {
                Notification.ShowWarning(Locale.Loc.Instance["LblNoCameraConnected"]);
                semaphoreSlim.Release();
                return null;
            }

            return await Task.Run<ImageArray>(async () => {
                ImageArray arr = null;

                try {
                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Change Filter*/
                    await ChangeFilter(sequence, token, progress);

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    token.ThrowIfCancellationRequested();

                    /*Set Camera Gain */
                    SetGain(sequence);

                    /*Set Camera Binning*/
                    SetBinning(sequence);

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Capture*/
                    await Capture(sequence, token, progress);

                    token.ThrowIfCancellationRequested();

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }

                    /*Dither*/
                    var ditherTask = Dither(sequence, token, progress);

                    /*Download Image */
                    arr = await Download(token, progress);
                    if (arr == null) {
                        throw new OperationCanceledException();
                    }                    

                    /*Prepare Image for UI*/
                    progress.Report(ImagingVM.ExposureStatus.PREPARING);

                    if (CameraConnected != true) {
                        throw new CameraConnectionLostException();
                    }
                    
                    
                    //Wait for previous prepare image task to complete
                    if(_currentPrepareImageTask != null && !_currentPrepareImageTask.IsCompleted) {
                        progress.Report("Waiting for previous image to finish processing");
                        await _currentPrepareImageTask;
                    }
                    //async prepare image and save
                    progress.Report("Prepare image saving");
                    _currentPrepareImageTask = ImageControl.PrepareImage(arr, progress, token, bSave, sequence, targetname);

                    //Wait for dither to finish. Runs in parallel to download and save.
                    progress.Report(Locale.Loc.Instance["LblDither"]);
                    await ditherTask;

                } catch (System.OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                    if (Cam == null || _cameraConnected == true) {
                        Cam?.AbortExposure();
                    }
                    throw ex;
                } catch (CameraConnectionLostException ex) {
                    Notification.ShowError(Locale.Loc.Instance["LblCameraConnectionLost"]);
                    throw ex;
                } catch (Exception ex) {
                    Notification.ShowError(Locale.Loc.Instance["LblUnexpectedError"]);
                    Logger.Error(ex.Message, ex.StackTrace);
                    if (_cameraConnected == true) {
                        Cam.AbortExposure();
                    }
                    throw ex;
                } finally {
                    progress.Report(ExposureStatus.IDLE);
                    semaphoreSlim.Release();
                }
                return arr;
            });

        }

        Task<BitmapSource> _currentPrepareImageTask;

        private void SetGain(CaptureSequence seq) {
            if (seq.Gain != -1) {
                Cam.Gain = seq.Gain;
            } else {

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

        public async Task<bool> SnapImage(IProgress<string> progress) {
            _captureImageToken = new CancellationTokenSource();

            try {
                var success = true;
                do {
                    var seq = new CaptureSequence(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1);
                    success = await CaptureAndSaveImage(seq, SnapSave, _captureImageToken.Token, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop && success);
            } catch (OperationCanceledException) {

            } finally {
                await _currentPrepareImageTask;
                progress.Report(string.Empty);
            }

            return true;

        }

        public async Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<string> progress, string targetname = "") {
            await CaptureImage(seq, ct, progress, bsave, targetname);            
            return true;
        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string CALCHFR = "Calculating HFR...";
            public const string SAVING = "Saving...";
            public const string IDLE = "";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}
