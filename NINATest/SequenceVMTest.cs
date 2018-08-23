﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyTelescope;
using NINA.PlateSolving;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.ViewModel;
using NINA.ViewModel.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINATest {

    [TestFixture]
    public class SequenceVMTest {
        public TestContext TestContext { get; set; }

        private SequenceProfileService profileService;
        private SequenceCameraMediator cameraMediator;
        private SequenceTelescopeMediator telescopeMediator;
        private SequenceFocuserMediator focuserMediator;
        private SequenceFilterWheelMediator filterWheelMediator;
        private SequenceGuiderMediator guiderMediator;
        private SequenceImagingMediator imagingMediator;
        private SequenceApplicationStatusMediator applicationStatusMediator;

        [SetUp]
        public void SequenceVM_TestInit() {
            profileService = new SequenceProfileService();
            profileService.ActiveProfile = new SequenceProfile();
            profileService.ActiveProfile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;

            cameraMediator = new SequenceCameraMediator();
            telescopeMediator = new SequenceTelescopeMediator();
            focuserMediator = new SequenceFocuserMediator();
            filterWheelMediator = new SequenceFilterWheelMediator();
            guiderMediator = new SequenceGuiderMediator();
            imagingMediator = new SequenceImagingMediator();
            applicationStatusMediator = new SequenceApplicationStatusMediator();
        }

        [TearDown]
        public void Cleanup() {
        }

        [Test]
        public void Sequence_ConsumerRegistered() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);

            Assert.AreEqual(vm, telescopeMediator.Consumer);
            Assert.AreEqual(vm, focuserMediator.Consumer);
            Assert.AreEqual(vm, filterWheelMediator.Consumer);
        }

        [Test]
        public async Task ProcessSequence_Default() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(0, vm.SelectedSequenceIdx);
            Assert.AreEqual(vm.IsPaused, false);
            Assert.AreEqual(vm.Sequence.IsRunning, false);
        }

        private CaptureSequenceList CreateDummySequenceList() {
            var l = new CaptureSequenceList();
            l.Add(new CaptureSequence() { TotalExposureCount = 10 });
            l.Add(new CaptureSequence() { TotalExposureCount = 20 });
            l.Add(new CaptureSequence() { TotalExposureCount = 5 });
            return l;
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontSlewToTargetTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.SlewToTarget = false;
            vm.Sequence = l;

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(null, telescopeMediator.SlewToCoordinatesAsyncParams);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_CoordinatesSlewTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.CenterTarget = true;
            var coordinates = new Coordinates(10, 10, Epoch.J2000, Coordinates.RAType.Degrees);
            l.Coordinates = coordinates;

            vm.Sequence = l;

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreSame(coordinates, telescopeMediator.SlewToCoordinatesAsyncParams);
        }

        /*Todo [Test]
        public async Task ProcessSequence_StartOptions_CenterTargetParameterTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.CenterTarget = true;
            vm.Sequence = l;

            var actualSyncSlewRepeat = false;
            Mediator.Instance.RegisterAsyncRequest(
                new PlateSolveMessageHandle((PlateSolveMessage msg) => {
                    actualSyncSlewRepeat = msg.SyncReslewRepeat;
                    return Task.FromResult(new PlateSolveResult());
                })
            );

        //Act
        await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, actualSyncSlewRepeat);
        }*/

        /*Todo[Test]
        public async Task ProcessSequence_StartOptions_DontCenterTargetTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.CenterTarget = false;
            vm.Sequence = l;

            var slewCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle((SlewToCoordinatesMessage msg) => {
                    slewCalled = true;
                    return null;
                })
            );

        var centerCalled = false;
            Mediator.Instance.RegisterAsyncRequest(
                new PlateSolveMessageHandle((PlateSolveMessage msg) => {
                    centerCalled = true;
                    return null;
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, slewCalled);
            Assert.AreEqual(false, centerCalled);
        }*/

        /*todo [Test]
        public async Task ProcessSequence_StartOptions_AutoFocusTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.AutoFocusOnStart = true;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle((StartAutoFocusMessage msg) => {
                    called = true;
                    return Task.FromResult(true);
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, called);
        }*/

        /*todo [Test]
        public async Task ProcessSequence_StartOptions_AutoFocusParameterTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            var filter = new NINA.Model.MyFilterWheel.FilterInfo("TestFilter", 0, 100);
            l.Items[0].FilterType = filter;
            l.Items[1].FilterType = new NINA.Model.MyFilterWheel.FilterInfo("TestFilter2", 2, 100);
            l.AutoFocusOnStart = true;
            vm.Sequence = l;

            NINA.Model.MyFilterWheel.FilterInfo actualFilter = null;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle((StartAutoFocusMessage msg) => {
                    actualFilter = msg.Filter;
                    return Task.FromResult(true);
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreSame(filter, actualFilter);
        }*/

        /*todo [Test]
        public async Task ProcessSequence_StartOptions_DontAutoFocusTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.AutoFocusOnStart = false;
            vm.Sequence = l;

            var called = false;
            Mediator.Instance.RegisterAsyncRequest(
                new StartAutoFocusMessageHandle((StartAutoFocusMessage msg) => {
                    called = true;
                    return Task.FromResult(true);
                })
            );

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, called);
        } */

        [Test]
        public async Task ProcessSequence_StartOptions_StartGuidingTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.StartGuiding = true;
            vm.Sequence = l;

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(true, guiderMediator.StartGuidingCalled);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontStartGuidingTest() {
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            var l = CreateDummySequenceList();
            l.StartGuiding = false;
            vm.Sequence = l;

            //Act
            await vm.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.AreEqual(false, guiderMediator.StartGuidingCalled);
        }

        [Test]
        public void ProcessSequence_AddSequenceCommand() {
            //Arrange
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);

            //Act
            vm.AddSequenceCommand.Execute(null);

            //Assert
            Assert.AreEqual(1, vm.SelectedSequenceIdx);
            Assert.AreEqual(2, vm.Sequence.Count);
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_AddSequenceCommand() {
            //Arrange
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);
            vm.Sequence = new CaptureSequenceList();

            //Act
            vm.AddSequenceCommand.Execute(null);

            //Assert
            Assert.AreEqual(0, vm.SelectedSequenceIdx);
            Assert.AreEqual(1, vm.Sequence.Count);
        }

        [Test]
        public void ProcessSequence_RemoveSequenceCommand() {
            //Arrange
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);

            //Act
            vm.RemoveSequenceCommand.Execute(null);

            //Assert
            Assert.AreEqual(-1, vm.SelectedSequenceIdx);
            Assert.AreEqual(0, vm.Sequence.Count);
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_RemoveSequenceCommand() {
            //Arrange
            var vm = new SequenceVM(profileService, cameraMediator, telescopeMediator, focuserMediator, filterWheelMediator, guiderMediator, imagingMediator, applicationStatusMediator);

            //Act
            vm.RemoveSequenceCommand.Execute(null);
            vm.RemoveSequenceCommand.Execute(null);

            //Assert
            Assert.AreEqual(-1, vm.SelectedSequenceIdx);
            Assert.AreEqual(0, vm.Sequence.Count);
        }
    }

    internal class SequenceSettings : ISequenceSettings {
        public TimeSpan EstimatedDownloadTime { get; set; }

        public string TemplatePath { get; set; }

        public long TimeSpanInTicks { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    internal class SequenceImageFileSettings : IImageFileSettings {
        public string FilePath { get; set; }

        public string FilePattern { get; set; }

        public FileTypeEnum FileType { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    internal class SequenceProfile : IProfile {

        public IApplicationSettings ApplicationSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IAstrometrySettings AstrometrySettings { get; set; } = new AstrometrySettings();

        public ICameraSettings CameraSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IColorSchemaSettings ColorSchemaSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFilterWheelSettings FilterWheelSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFocuserSettings FocuserSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IFramingAssistantSettings FramingAssistantSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IGuiderSettings GuiderSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Guid Id {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IImageFileSettings ImageFileSettings { get; set; } = new SequenceImageFileSettings();

        public IImageSettings ImageSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool IsActive {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IMeridianFlipSettings MeridianFlipSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public string Name {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IPlateSolveSettings PlateSolveSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IPolarAlignmentSettings PolarAlignmentSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public ISequenceSettings SequenceSettings { get; set; } = new SequenceSettings();

        public ITelescopeSettings TelescopeSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IWeatherDataSettings WeatherDataSettings {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void MatchFilterSettingsWithFilterList() {
            throw new NotImplementedException();
        }
    }

    internal class SequenceCameraMediator : ICameraMediator {

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        public void Broadcast(CameraInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public Task Capture(double exposureTime, bool isLightFrame, CancellationToken token, IProgress<ApplicationStatus> progress) {
            throw new NotImplementedException();
        }

        public Task<bool> Connect() {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            throw new NotImplementedException();
        }

        public Task<ImageArray> Download(CancellationToken token) {
            throw new NotImplementedException();
        }

        public System.Collections.Async.IAsyncEnumerable<ImageArray> LiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void RegisterConsumer(ICameraConsumer consumer) {
            throw new NotImplementedException();
        }

        public void RegisterHandler(ICameraVM handler) {
            throw new NotImplementedException();
        }

        public ICameraConsumer Consumer;

        public void RemoveConsumer(ICameraConsumer consumer) {
            this.Consumer = consumer;
        }

        public void SetBinning(short x, short y) {
            throw new NotImplementedException();
        }

        public void SetGain(short gain) {
            throw new NotImplementedException();
        }

        public void SetSubSample(bool subSample) {
            throw new NotImplementedException();
        }

        public void SetSubSampleArea(int x, int y, int width, int height) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceTelescopeMediator : ITelescopeMediator {

        public void Broadcast(TelescopeInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public Task<bool> Connect() {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            throw new NotImplementedException();
        }

        public bool MeridianFlip(Coordinates targetCoordinates) {
            throw new NotImplementedException();
        }

        public void MoveAxis(ASCOM.DeviceInterface.TelescopeAxes axis, double rate) {
            throw new NotImplementedException();
        }

        public ITelescopeConsumer Consumer;

        public void RegisterConsumer(ITelescopeConsumer consumer) {
            this.Consumer = consumer;
        }

        public void RegisterHandler(ITelescopeVM handler) {
            throw new NotImplementedException();
        }

        public void RemoveConsumer(ITelescopeConsumer consumer) {
            throw new NotImplementedException();
        }

        public bool SendToSnapPort(bool start) {
            throw new NotImplementedException();
        }

        public bool SetTracking(bool tracking) {
            throw new NotImplementedException();
        }

        public Coordinates SlewToCoordinatesAsyncParams;

        public async Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            this.SlewToCoordinatesAsyncParams = coords;
            return true;
        }

        public bool Sync(double ra, double dec) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceFocuserMediator : IFocuserMediator {

        public void Broadcast(FocuserInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public Task<bool> Connect() {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            throw new NotImplementedException();
        }

        public Task<int> MoveFocuser(int position) {
            throw new NotImplementedException();
        }

        public Task<int> MoveFocuserRelative(int position) {
            throw new NotImplementedException();
        }

        public IFocuserConsumer Consumer;

        public void RegisterConsumer(IFocuserConsumer consumer) {
            this.Consumer = consumer;
        }

        public void RegisterHandler(IFocuserVM handler) {
            throw new NotImplementedException();
        }

        public void RemoveConsumer(IFocuserConsumer consumer) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceFilterWheelMediator : IFilterWheelMediator {

        public void Broadcast(FilterWheelInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = default(CancellationToken), IProgress<ApplicationStatus> progress = null) {
            throw new NotImplementedException();
        }

        public Task<bool> Connect() {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            throw new NotImplementedException();
        }

        public ICollection<FilterInfo> GetAllFilters() {
            throw new NotImplementedException();
        }

        public IFilterWheelConsumer Consumer;

        public void RegisterConsumer(IFilterWheelConsumer consumer) {
            this.Consumer = consumer;
        }

        public void RegisterHandler(IFilterWheelVM handler) {
            throw new NotImplementedException();
        }

        public void RemoveConsumer(IFilterWheelConsumer consumer) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceGuiderMediator : IGuiderMediator {

        public Task<bool> AutoSelectGuideStar(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void Broadcast(GuiderInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public Task<bool> Connect() {
            throw new NotImplementedException();
        }

        public void Disconnect() {
            throw new NotImplementedException();
        }

        public Task<bool> Dither(CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<bool> PauseGuiding(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void RegisterConsumer(IGuiderConsumer consumer) {
            throw new NotImplementedException();
        }

        public void RegisterHandler(IGuiderVM handler) {
            throw new NotImplementedException();
        }

        public void RemoveConsumer(IGuiderConsumer consumer) {
            throw new NotImplementedException();
        }

        public Task<bool> ResumeGuiding(CancellationToken token) {
            throw new NotImplementedException();
        }

        public bool StartGuidingCalled = false;

        public async Task<bool> StartGuiding(CancellationToken token) {
            StartGuidingCalled = true;
            return true;
        }

        public Guid StartRMSRecording() {
            throw new NotImplementedException();
        }

        public Task<bool> StopGuiding(CancellationToken token) {
            throw new NotImplementedException();
        }

        public RMS StopRMSRecording(Guid handle) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceImagingMediator : IImagingMediator {

        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public Task<System.Windows.Media.Imaging.BitmapSource> CaptureAndPrepareImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress) {
            throw new NotImplementedException();
        }

        public Task<bool> CaptureAndSaveImage(CaptureSequence seq, bool bsave, CancellationToken ct, IProgress<ApplicationStatus> progress, string targetname = "") {
            throw new NotImplementedException();
        }

        public Task<ImageArray> CaptureImage(CaptureSequence sequence, CancellationToken token, IProgress<ApplicationStatus> progress, bool bSave = false, string targetname = "") {
            throw new NotImplementedException();
        }

        public void OnImageSaved(ImageSavedEventArgs e) {
            throw new NotImplementedException();
        }

        public Task<BitmapSource> PrepareImage(ImageArray iarr, CancellationToken token, bool bSave = false, ImageParameters parameters = null) {
            throw new NotImplementedException();
        }

        public void RegisterHandler(IImagingVM handler) {
            throw new NotImplementedException();
        }

        public bool SetAutoStretch(bool value) {
            throw new NotImplementedException();
        }

        public bool SetDetectStars(bool value) {
            throw new NotImplementedException();
        }
    }

    internal class SequenceApplicationStatusMediator : IApplicationStatusMediator {

        public void RegisterHandler(IApplicationStatusVM handler) {
            throw new NotImplementedException();
        }

        public void StatusUpdate(ApplicationStatus status) {
        }
    }

    internal class SequenceProfileService : IProfileService {

        public Profiles Profiles {
            get {
                throw new NotImplementedException();
            }
        }

        public IProfile ActiveProfile { get; set; }

        public event EventHandler LocaleChanged;

        public event EventHandler LocationChanged;

        public event EventHandler ProfileChanged;

        public void Add() {
            new SequenceProfile();
        }

        public void ChangeHemisphere(Hemisphere hemisphere) {
            throw new NotImplementedException();
        }

        public void ChangeLatitude(double latitude) {
            throw new NotImplementedException();
        }

        public void ChangeLocale(CultureInfo language) {
            throw new NotImplementedException();
        }

        public void ChangeLongitude(double longitude) {
            throw new NotImplementedException();
        }

        public void Clone(Guid guid) {
            throw new NotImplementedException();
        }

        public void RemoveProfile(Guid guid) {
            throw new NotImplementedException();
        }

        public void SelectProfile(Guid guid) {
            throw new NotImplementedException();
        }
    }
}