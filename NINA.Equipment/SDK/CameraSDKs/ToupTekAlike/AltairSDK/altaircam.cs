using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#if !(NETFX_CORE || WINDOWS_UWP)

using System.Security.Permissions;
using System.Runtime.ConstrainedExecution;

#endif

using System.Collections.Generic;
using System.Threading;
using NINA.Core.Utility;
using System.IO;
using System.Runtime.ExceptionServices;

/*
    Versin: 48.17962.2020.1110

    For Microsoft dotNET Framework & dotNet Core

    We use P/Invoke to call into the altaircam.dll API, the c# class Altaircam is a thin wrapper class to the native api of altaircam.dll.
    So the manual en.html(English) and hans.html(Simplified Chinese) are also applicable for programming with altaircam.cs.
    See them in the 'doc' directory:
       (1) en.html, English
       (2) hans.html, Simplified Chinese
*/

namespace Altair {

    public class AltairCam : IDisposable {

        static AltairCam() {
            DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
        }

        [Flags]
        public enum eFLAG : ulong {
            FLAG_CMOS = 0x00000001,   /* cmos sensor */
            FLAG_CCD_PROGRESSIVE = 0x00000002,   /* progressive ccd sensor */
            FLAG_CCD_INTERLACED = 0x00000004,   /* interlaced ccd sensor */
            FLAG_ROI_HARDWARE = 0x00000008,   /* support hardware ROI */
            FLAG_MONO = 0x00000010,   /* monochromatic */
            FLAG_BINSKIP_SUPPORTED = 0x00000020,   /* support bin/skip mode */
            FLAG_USB30 = 0x00000040,   /* usb3.0 */
            FLAG_TEC = 0x00000080,   /* Thermoelectric Cooler */
            FLAG_USB30_OVER_USB20 = 0x00000100,   /* usb3.0 camera connected to usb2.0 port */
            FLAG_ST4 = 0x00000200,   /* ST4 */
            FLAG_GETTEMPERATURE = 0x00000400,   /* support to get the temperature of the sensor */
            FLAG_HIGH_FULLWELL = 0x00000800,   /* high fullwell capacity */
            FLAG_RAW10 = 0x00001000,   /* pixel format, RAW 10bits */
            FLAG_RAW12 = 0x00002000,   /* pixel format, RAW 12bits */
            FLAG_RAW14 = 0x00004000,   /* pixel format, RAW 14bits */
            FLAG_RAW16 = 0x00008000,   /* pixel format, RAW 16bits */
            FLAG_FAN = 0x00010000,   /* cooling fan */
            FLAG_TEC_ONOFF = 0x00020000,   /* Thermoelectric Cooler can be turn on or off, support to set the target temperature of TEC */
            FLAG_ISP = 0x00040000,   /* ISP (Image Signal Processing) chip */
            FLAG_TRIGGER_SOFTWARE = 0x00080000,   /* support software trigger */
            FLAG_TRIGGER_EXTERNAL = 0x00100000,   /* support external trigger */
            FLAG_TRIGGER_SINGLE = 0x00200000,   /* only support trigger single: one trigger, one image */
            FLAG_BLACKLEVEL = 0x00400000,   /* support set and get the black level */
            FLAG_AUTO_FOCUS = 0x00800000,   /* support auto focus */
            FLAG_BUFFER = 0x01000000,   /* frame buffer */
            FLAG_DDR = 0x02000000,   /* use very large capacity DDR (Double Data Rate SDRAM) for frame buffer. The capacity is not less than one full frame */
            FLAG_CG = 0x04000000,   /* support Conversion Gain mode: HCG, LCG */
            FLAG_YUV411 = 0x08000000,   /* pixel format, yuv411 */
            FLAG_VUYY = 0x10000000,   /* pixel format, yuv422, VUYY */
            FLAG_YUV444 = 0x20000000,   /* pixel format, yuv444 */
            FLAG_RGB888 = 0x40000000,   /* pixel format, RGB888 */
            [Obsolete("Use FLAG_RAW10")]
            FLAG_BITDEPTH10 = FLAG_RAW10,   /* obsolete, same as FLAG_RAW10 */
            [Obsolete("Use FLAG_RAW12")]
            FLAG_BITDEPTH12 = FLAG_RAW12,   /* obsolete, same as FLAG_RAW12 */
            [Obsolete("Use FLAG_RAW14")]
            FLAG_BITDEPTH14 = FLAG_RAW14,   /* obsolete, same as FLAG_RAW14 */
            [Obsolete("Use FLAG_RAW16")]
            FLAG_BITDEPTH16 = FLAG_RAW16,   /* obsolete, same as FLAG_RAW16 */
            FLAG_RAW8 = 0x80000000,   /* pixel format, RAW 8 bits */
            FLAG_GMCY8 = 0x0000000100000000,  /* pixel format, GMCY, 8 bits */
            FLAG_GMCY12 = 0x0000000200000000,  /* pixel format, GMCY, 12 bits */
            FLAG_UYVY = 0x0000000400000000,  /* pixel format, yuv422, UYVY */
            FLAG_CGHDR = 0x0000000800000000,  /* Conversion Gain: HCG, LCG, HDR */
            FLAG_GLOBALSHUTTER = 0x0000001000000000,  /* global shutter */
            FLAG_FOCUSMOTOR = 0x0000002000000000,  /* support focus motor */
            FLAG_PRECISE_FRAMERATE = 0x0000004000000000,  /* support precise framerate & bandwidth, see OPTION_PRECISE_FRAMERATE & OPTION_BANDWIDTH */
            FLAG_HEAT = 0x0000008000000000,  /* support heat to prevent fogging up */
            FLAG_LOW_NOISE = 0x0000010000000000,  /* support low noise mode (Higher signal noise ratio, lower frame rate) */
            FLAG_LEVELRANGE_HARDWARE = 0x0000020000000000,  /* hardware level range, put(get)_LevelRangeV2 */
            FLAG_EVENT_HARDWARE = 0x0000040000000000,  /* hardware event, such as exposure start & stop */
            FLAG_LIGHTSOURCE = 0x0000080000000000,  /* embedded light source */
            FLAG_FILTERWHEEL = 0x0000100000000000,  /* astro filter wheel */
            FLAG_GIGE = 0x0000200000000000,  /* 1 Gigabit GigE */
            FLAG_10GIGE = 0x0000400000000000,  /* 10 Gigabit GigE */
            FLAG_5GIGE = 0x0000800000000000,  /* 5 Gigabit GigE */
            FLAG_25GIGE = 0x0001000000000000,  /* 2.5 Gigabit GigE */
            FLAG_AUTOFOCUSER = 0x0002000000000000,  /* astro auto focuser */
            FLAG_LIGHT_SOURCE = 0x0004000000000000,  /* stand alone light source */
            FLAG_CAMERALINK = 0x0008000000000000,  /* camera link */
            FLAG_CXP = 0x0010000000000000   /* CXP: CoaXPress */
        };

        public enum eEVENT : uint {
            EVENT_EXPOSURE = 0x0001, /* exposure time or gain changed */
            EVENT_TEMPTINT = 0x0002, /* white balance changed, Temp/Tint mode */
            EVENT_CHROME = 0x0003, /* reversed, do not use it */
            EVENT_IMAGE = 0x0004, /* live image arrived, use Altaircam_PullImage to get this image */
            EVENT_STILLIMAGE = 0x0005, /* snap (still) frame arrived, use Altaircam_PullStillImage to get this frame */
            EVENT_WBGAIN = 0x0006, /* white balance changed, RGB Gain mode */
            EVENT_TRIGGERFAIL = 0x0007, /* trigger failed */
            EVENT_BLACK = 0x0008, /* black balance changed */
            EVENT_FFC = 0x0009, /* flat field correction status changed */
            EVENT_DFC = 0x000a, /* dark field correction status changed */
            EVENT_ROI = 0x000b, /* roi changed */
            EVENT_LEVELRANGE = 0x000c, /* level range changed */
            EVENT_AUTOEXPO_CONV = 0x000d, /* auto exposure convergence */
            EVENT_AUTOEXPO_CONVFAIL = 0x000e, /* auto exposure once mode convergence failed */
            EVENT_ERROR = 0x0080, /* generic error */
            EVENT_DISCONNECTED = 0x0081, /* camera disconnected */
            EVENT_NOFRAMETIMEOUT = 0x0082, /* no frame timeout error */
            EVENT_AFFEEDBACK = 0x0083, /* auto focus feedback information */
            EVENT_FOCUSPOS = 0x0084, /* focus positon */
            EVENT_NOPACKETTIMEOUT = 0x0085, /* no packet timeout */
            EVENT_EXPO_START = 0x4000, /* hardware event: exposure start */
            EVENT_EXPO_STOP = 0x4001, /* hardware event: exposure stop */
            EVENT_TRIGGER_ALLOW = 0x4002, /* hardware event: next trigger allow */
            EVENT_HEARTBEAT = 0x4003, /* hardware event: heartbeat, can be used to monitor whether the camera is alive */
            EVENT_TRIGGER_IN = 0x4004, /* hardware event: trigger in */
            EVENT_FACTORY = 0x8001  /* restore factory settings */
        };

        public enum eOPTION : uint {
            OPTION_NOFRAME_TIMEOUT = 0x01,       /* no frame timeout: 0 => disable, positive value (>= NOFRAME_TIMEOUT_MIN) => timeout milliseconds. default: disable */
            OPTION_THREAD_PRIORITY = 0x02,       /* set the priority of the internal thread which grab data from the usb device.
                                                         Win: iValue: 0 => THREAD_PRIORITY_NORMAL; 1 => THREAD_PRIORITY_ABOVE_NORMAL; 2 => THREAD_PRIORITY_HIGHEST; 3 => THREAD_PRIORITY_TIME_CRITICAL; default: 1; see: https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setthreadpriority
                                                         Linux & macOS: The high 16 bits for the scheduling policy, and the low 16 bits for the priority; see: https://linux.die.net/man/3/pthread_setschedparam
                                                    */
            OPTION_RAW = 0x04,       /* raw data mode, read the sensor "raw" data. This can be set only while camea is NOT running. 0 = rgb, 1 = raw, default value: 0 */
            OPTION_HISTOGRAM = 0x05,       /* 0 = only one, 1 = continue mode */
            OPTION_BITDEPTH = 0x06,       /* 0 = 8 bits mode, 1 = 16 bits mode */
            OPTION_FAN = 0x07,       /* 0 = turn off the cooling fan, [1, max] = fan speed */
            OPTION_TEC = 0x08,       /* 0 = turn off the thermoelectric cooler, 1 = turn on the thermoelectric cooler */
            OPTION_LINEAR = 0x09,       /* 0 = turn off the builtin linear tone mapping, 1 = turn on the builtin linear tone mapping, default value: 1 */
            OPTION_CURVE = 0x0a,       /* 0 = turn off the builtin curve tone mapping, 1 = turn on the builtin polynomial curve tone mapping, 2 = logarithmic curve tone mapping, default value: 2 */
            OPTION_TRIGGER = 0x0b,       /* 0 = video mode, 1 = software or simulated trigger mode, 2 = external trigger mode, 3 = external + software trigger, default value = 0 */
            OPTION_RGB = 0x0c,       /* 0 => RGB24; 1 => enable RGB48 format when bitdepth > 8; 2 => RGB32; 3 => 8 Bits Grey (only for mono camera); 4 => 16 Bits Grey (only for mono camera when bitdepth > 8); 5 => RGB64 */
            OPTION_COLORMATIX = 0x0d,       /* enable or disable the builtin color matrix, default value: 1 */
            OPTION_WBGAIN = 0x0e,       /* enable or disable the builtin white balance gain, default value: 1 */
            OPTION_TECTARGET = 0x0f,       /* get or set the target temperature of the thermoelectric cooler, in 0.1 degree Celsius. For example, 125 means 12.5 degree Celsius, -35 means -3.5 degree Celsius */
            OPTION_AUTOEXP_POLICY = 0x10,       /* auto exposure policy:
                                                            0: Exposure Only
                                                            1: Exposure Preferred
                                                            2: Gain Only
                                                            3: Gain Preferred
                                                         default value: 1
                                                    */
            OPTION_FRAMERATE = 0x11,       /* limit the frame rate, range=[0, 63], the default value 0 means no limit */
            OPTION_DEMOSAIC = 0x12,       /* demosaic method for both video and still image: BILINEAR = 0, VNG(Variable Number of Gradients) = 1, PPG(Patterned Pixel Grouping) = 2, AHD(Adaptive Homogeneity Directed) = 3, EA(Edge Aware) = 4, see https://en.wikipedia.org/wiki/Demosaicing, default value: 0 */
            OPTION_DEMOSAIC_VIDEO = 0x13,       /* demosaic method for video */
            OPTION_DEMOSAIC_STILL = 0x14,       /* demosaic method for still image */
            OPTION_BLACKLEVEL = 0x15,       /* black level */
            OPTION_MULTITHREAD = 0x16,       /* multithread image processing */
            OPTION_BINNING = 0x17,       /* binning
                                                           0x01: (no binning)
                                                           n: (saturating add, n*n), 0x02(2*2), 0x03(3*3), 0x04(4*4), 0x05(5*5), 0x06(6*6), 0x07(7*7), 0x08(8*8). The Bitdepth of the data remains unchanged.
                                                           0x40 | n: (unsaturated add, n*n, works only in RAW mode), 0x42(2*2), 0x43(3*3), 0x44(4*4), 0x45(5*5), 0x46(6*6), 0x47(7*7), 0x48(8*8). The Bitdepth of the data is increased. For example, the original data with bitdepth of 12 will increase the bitdepth by 2 bits and become 14 after 2*2 binning.
                                                           0x80 | n: (average, n*n), 0x02(2*2), 0x03(3*3), 0x04(4*4), 0x05(5*5), 0x06(6*6), 0x07(7*7), 0x08(8*8). The Bitdepth of the data remains unchanged.
                                                       The final image size is rounded down to an even number, such as 640/3 to get 212
                                                    */
            OPTION_ROTATE = 0x18,       /* rotate clockwise: 0, 90, 180, 270 */
            OPTION_CG = 0x19,       /* Conversion Gain mode: 0 = LCG, 1 = HCG, 2 = HDR */
            OPTION_PIXEL_FORMAT = 0x1a,       /* pixel format */
            OPTION_FFC = 0x1b,       /* flat field correction
                                                        set:
                                                            0: disable
                                                            1: enable
                                                            -1: reset
                                                            (0xff000000 | n): set the average number to n, [1~255]
                                                        get:
                                                            (val & 0xff): 0 => disable, 1 => enable, 2 => inited
                                                            ((val & 0xff00) >> 8): sequence
                                                            ((val & 0xff0000) >> 16): average number
                                                    */
            OPTION_DDR_DEPTH = 0x1c,       /* the number of the frames that DDR can cache
                                                        1: DDR cache only one frame
                                                        0: Auto:
                                                            => one for video mode when auto exposure is enabled
                                                            => full capacity for others
                                                        1: DDR can cache frames to full capacity
                                                    */
            OPTION_DFC = 0x1d,       /* dark field correction
                                                        set:
                                                            0: disable
                                                            1: enable
                                                            -1: reset
                                                            (0xff000000 | n): set the average number to n, [1~255]
                                                        get:
                                                            (val & 0xff): 0 => disable, 1 => enable, 2 => inited
                                                            ((val & 0xff00) >> 8): sequence
                                                            ((val & 0xff0000) >> 16): average number
                                                    */
            OPTION_SHARPENING = 0x1e,       /* Sharpening: (threshold << 24) | (radius << 16) | strength)
                                                        strength: [0, 500], default: 0 (disable)
                                                        radius: [1, 10]
                                                        threshold: [0, 255]
                                                    */
            OPTION_FACTORY = 0x1f,       /* restore the factory settings */
            OPTION_TEC_VOLTAGE = 0x20,       /* get the current TEC voltage in 0.1V, 59 mean 5.9V; readonly */
            OPTION_TEC_VOLTAGE_MAX = 0x21,       /* TEC maximum voltage in 0.1V */
            OPTION_DEVICE_RESET = 0x22,       /* reset usb device, simulate a replug */
            OPTION_UPSIDE_DOWN = 0x23,       /* upsize down:
                                                        1: yes
                                                        0: no
                                                        default: 1 (win), 0 (linux/macos)
                                                    */
            OPTION_FOCUSPOS = 0x24,       /* focus positon */
            OPTION_AFMODE = 0x25,       /* auto focus mode (0:manul focus; 1:auto focus; 2:once focus; 3:conjugate calibration) */
            OPTION_AFZONE = 0x26,       /* auto focus zone */
            OPTION_AFFEEDBACK = 0x27,       /* auto focus information feedback; 0:unknown; 1:focused; 2:focusing; 3:defocus; 4:up; 5:down */
            OPTION_TESTPATTERN = 0x28,       /* test pattern:
                                                        0: off
                                                        3: monochrome diagonal stripes
                                                        5: monochrome vertical stripes
                                                        7: monochrome horizontal stripes
                                                        9: chromatic diagonal stripes
                                                    */
            OPTION_AUTOEXP_THRESHOLD = 0x29,       /* threshold of auto exposure, default value: 5, range = [2, 15] */
            OPTION_BYTEORDER = 0x2a,       /* Byte order, BGR or RGB: 0 => RGB, 1 => BGR, default value: 1(Win), 0(macOS, Linux, Android) */
            OPTION_NOPACKET_TIMEOUT = 0x2b,       /* no packet timeout: 0 => disable, positive value (>= NOPACKET_TIMEOUT_MIN) => timeout milliseconds. default: disable */
            OPTION_MAX_PRECISE_FRAMERATE = 0x2c,       /* get the precise frame rate maximum value in 0.1 fps, such as 115 means 11.5 fps. E_NOTIMPL means not supported */
            OPTION_PRECISE_FRAMERATE = 0x2d,       /* precise frame rate current value in 0.1 fps, range:[1~maximum] */
            OPTION_BANDWIDTH = 0x2e,       /* bandwidth, [1-100]% */
            OPTION_RELOAD = 0x2f,       /* reload the last frame in trigger mode */
            OPTION_CALLBACK_THREAD = 0x30,       /* dedicated thread for callback */
            OPTION_FRONTEND_DEQUE_LENGTH = 0x31,       /* frontend (raw) frame buffer deque length, range: [2, 1024], default: 4
                                                        All the memory will be pre-allocated when the camera starts, so, please attention to memory usage
                                                    */
            OPTION_FRAME_DEQUE_LENGTH = 0x31,       /* alias of TOUPCAM_OPTION_FRONTEND_DEQUE_LENGTH */
            OPTION_MIN_PRECISE_FRAMERATE = 0x32,       /* get the precise frame rate minimum value in 0.1 fps, such as 15 means 1.5 fps */
            OPTION_SEQUENCER_ONOFF = 0x33,       /* sequencer trigger: on/off */
            OPTION_SEQUENCER_NUMBER = 0x34,       /* sequencer trigger: number, range = [1, 255] */
            OPTION_SEQUENCER_EXPOTIME = 0x01000000, /* sequencer trigger: exposure time, iOption = OPTION_SEQUENCER_EXPOTIME | index, iValue = exposure time
                                                        For example, to set the exposure time of the third group to 50ms, call:
                                                           Toupcam_put_Option(TOUPCAM_OPTION_SEQUENCER_EXPOTIME | 3, 50000)
                                                    */
            OPTION_SEQUENCER_EXPOGAIN = 0x02000000, /* sequencer trigger: exposure gain, iOption = OPTION_SEQUENCER_EXPOGAIN | index, iValue = gain */
            OPTION_DENOISE = 0x35,       /* denoise, strength range: [0, 100], 0 means disable */
            OPTION_HEAT_MAX = 0x36,       /* get maximum level: heat to prevent fogging up */
            OPTION_HEAT = 0x37,       /* heat to prevent fogging up */
            OPTION_LOW_NOISE = 0x38,       /* low noise mode (Higher signal noise ratio, lower frame rate): 1 => enable */
            OPTION_POWER = 0x39,       /* get power consumption, unit: milliwatt */
            OPTION_GLOBAL_RESET_MODE = 0x3a,       /* global reset mode */
            OPTION_OPEN_ERRORCODE = 0x3b,       /* get the open camera error code */
            OPTION_FLUSH = 0x3d,        /* 1 = hard flush, discard frames cached by camera DDR (if any)
                                                        2 = soft flush, discard frames cached by toupcam.dll (if any)
                                                        3 = both flush
                                                        Toupcam_Flush means 'both flush'
                                                        return the number of soft flushed frames if successful, HRESULT if failed
                                                     */
            OPTION_NUMBER_DROP_FRAME = 0x3e,        /* get the number of frames that have been grabbed from the USB but dropped by the software */
            OPTION_DUMP_CFG = 0x3f,        /* 0 = when camera is stopped, do not dump configuration automatically
                                                        1 = when camera is stopped, dump configuration automatically
                                                        -1 = explicitly dump configuration once
                                                        default: 1
                                                     */
            OPTION_DEFECT_PIXEL = 0x40,        /* Defect Pixel Correction: 0 => disable, 1 => enable; default: 1 */
            OPTION_BACKEND_DEQUE_LENGTH = 0x41,        /* backend (pipelined) frame buffer deque length (Only available in pull mode), range: [2, 1024], default: 3
                                                        All the memory will be pre-allocated when the camera starts, so, please attention to memory usage
                                                     */
            OPTION_LIGHTSOURCE_MAX = 0x42,        /* get the light source range, [0 ~ max] */
            OPTION_LIGHTSOURCE = 0x43,        /* light source */
            OPTION_HEARTBEAT = 0x44,        /* Heartbeat interval in millisecond, range = [HEARTBEAT_MIN, HEARTBEAT_MAX], 0 = disable, default: disable */
            OPTION_FRONTEND_DEQUE_CURRENT = 0x45,        /* get the current number in frontend deque */
            OPTION_BACKEND_DEQUE_CURRENT = 0x46,        /* get the current number in backend deque */
            OPTION_EVENT_HARDWARE = 0x04000000,  /* enable or disable hardware event: 0 => disable, 1 => enable; default: disable
                                                            (1) iOption = TOUPCAM_OPTION_EVENT_HARDWARE, master switch for notification of all hardware events
                                                            (2) iOption = TOUPCAM_OPTION_EVENT_HARDWARE | (event type), a specific type of sub-switch
                                                        Only if both the master switch and the sub-switch of a particular type remain on are actually enabled for that type of event notification.
                                                     */
            OPTION_PACKET_NUMBER = 0x47,        /* get the received packet number */
            OPTION_FILTERWHEEL_SLOT = 0x48,        /* filter wheel slot number */
            OPTION_FILTERWHEEL_POSITION = 0x49,        /* filter wheel position:
                                                             set:
                                                                 -1: calibrate
                                                                 val & 0xff: position between 0 and N-1, where N is the number of filter slots
                                                                 (val >> 8) & 0x1: direction, 0 => clockwise spinning, 1 => auto direction spinning
                                                             get:
                                                                -1: in motion
                                                                val: position arrived
                                                     */
            OPTION_AUTOEXPOSURE_PERCENT = 0x4a,        /* auto exposure percent to average:
                                                             1~99: peak percent average
                                                             0 or 100: full roi average
                                                     */
            OPTION_ANTI_SHUTTER_EFFECT = 0x4b,        /* anti shutter effect: 1 => disable, 0 => disable; default: 1 */
            OPTION_CHAMBER_HT = 0x4c,        /* get chamber humidity & temperature:
                                                             high 16 bits: humidity, in 0.1%, such as: 325 means humidity is 32.5%
                                                             low 16 bits: temperature, in 0.1 degrees Celsius, such as: 32 means 3.2 degrees Celsius
                                                     */
            OPTION_ENV_HT = 0x4d,        /* get environment humidity & temperature */
            OPTION_EXPOSURE_PRE_DELAY = 0x4e,        /* exposure signal pre-delay, microsecond */
            OPTION_EXPOSURE_POST_DELAY = 0x4f,        /* exposure signal post-delay, microsecond */
            OPTION_AUTOEXPO_CONV = 0x50,        /* get auto exposure convergence status: 1(YES) or 0(NO), -1(NA) */
            OPTION_AUTOEXPO_TRIGGER = 0x51,        /* auto exposure on trigger mode: 0 => disable, 1 => enable; default: 0 */
            OPTION_LINE_PRE_DELAY = 0x52,        /* specified line signal pre-delay, microsecond */
            OPTION_LINE_POST_DELAY = 0x53,        /* specified line signal post-delay, microsecond */
            OPTION_TEC_VOLTAGE_MAX_RANGE = 0x54,        /* get the tec maximum voltage range:
                                                             high 16 bits: max
                                                             low 16 bits: min
                                                     */
            OPTION_HIGH_FULLWELL = 0x55,        /* high fullwell capacity: 0 => disable, 1 => enable */
            OPTION_DYNAMIC_DEFECT = 0x56,        /* dynamic defect pixel correction:
                                                             threshold, t1: (high 16 bits): [10, 100], means: [1.0, 10.0]
                                                             value, t2: (low 16 bits): [0, 100], means: [0.00, 1.00]
                                                     */
            OPTION_HDR_KB = 0x57,        /* HDR synthesize
                                                             K (high 16 bits): [1, 25500]
                                                             B (low 16 bits): [0, 65535]
                                                             0xffffffff => set to default
                                                     */
            OPTION_HDR_THRESHOLD = 0x58,        /* HDR synthesize
                                                             threshold: [1, 4094]
                                                             0xffffffff => set to default
                                                     */
            OPTION_GIGETIMEOUT = 0x5a,        /* For GigE cameras, the application periodically sends heartbeat signals to the camera to keep the connection to the camera alive.
                                                        If the camera doesn't receive heartbeat signals within the time period specified by the heartbeat timeout counter, the camera resets the connection.
                                                        When the application is stopped by the debugger, the application cannot create the heartbeat signals
                                                             0 => auto: when the camera is opened, disable if debugger is present or enable if no debugger is present
                                                             1 => enable
                                                             2 => disable
                                                             default: auto
                                                     */
            OPTION_EEPROM_SIZE = 0x5b,        /* get EEPROM size */
            OPTION_OVERCLOCK_MAX = 0x5c,        /* get overclock range: [0, max] */
            OPTION_OVERCLOCK = 0x5d,        /* overclock, default: 0 */
            OPTION_RESET_SENSOR = 0x5e,        /* reset sensor */
            OPTION_ADC = 0x08000000,  /* Analog-Digital Conversion:
                                                            get:
                                                                (option | 'C'): get the current value
                                                                (option | 'N'): get the supported ADC number
                                                                (option | n): get the nth supported ADC value, such as 11bits, 12bits, etc; the first value is the default
                                                            set: val = ADC value, such as 11bits, 12bits, etc
                                                     */
            OPTION_ISP = 0x5f,        /* Enable hardware ISP: 0 => auto (disable in RAW mode, otherwise enable), 1 => enable, -1 => disable; default: 0 */
            OPTION_AUTOEXP_EXPOTIME_STEP = 0x60,        /* Auto exposure: time step (thousandths) */
            OPTION_AUTOEXP_GAIN_STEP = 0x61,        /* Auto exposure: gain step (thousandths) */
            OPTION_MOTOR_NUMBER = 0x62,        /* range: [1, 20] */
            OPTION_MOTOR_POS = 0x10000000,   /* range: [1, 702] */
            /* Pseudo: start color, BGR format */
            OPTION_PSEUDO_COLOR_START = 0x63,
            /* Pseudo: end color, BGR format */
            OPTION_PSEUDO_COLOR_END = 0x64,
            /*  Pseudo: 
                -1 => custom: use startcolor & endcolor to generate the colormap
                0 => disable
                1 => spot
                2 => spring
                3 => summer
                4 => autumn
                5 => winter
                6 => bone
                7 => jet
                8 => rainbow
                9 => deepgreen
                10 => ocean
                11 => cool
                12 => hsv
                13 => pink
                14 => hot
                15 => parula
                16 => magma
                17 => inferno
                18 => plasma
                19 => viridis
                20 => cividis
                21 => twilight
                22 => twilight_shifted
                23 => turbo
            */
            OPTION_PSEUDO_COLOR_ENABLE = 0x65,
            /* Low Power Consumption: 0 => disable, 1 => enable */
            OPTION_LOW_POWERCONSUMPTION = 0x66,
            /*  Fix Pattern Noise Correction
                    set:
                        0: disable
                        1: enable
                        -1: reset
                        (0xff000000 | n): set the average number to n, [1~255]
                    get:
                        (val & 0xff): 0 => disable, 1 => enable, 2 => inited
                        ((val & 0xff00) >> 8): sequence
                        ((val & 0xff0000) >> 16): average number
            */
            OPTION_FPNC = 0x67,
            /* Auto exposure over exposure policy: when overexposed,
                    0 => directly reduce the exposure time/gain to the minimum value; or
                    1 => reduce exposure time/gain in proportion to current and target brightness.
                The advantage of policy 0 is that the convergence speed is faster, but there is black screen.
                Policy 1 avoids the black screen, but the convergence speed is slower.
                Default: 0
            */
            OPTION_OVEREXP_POLICY = 0x68,
            /* Readout mode: 0 = IWR (Integrate While Read), 1 = ITR (Integrate Then Read) */
            OPTION_READOUT_MODE = 0x69
        };

        /* HRESULT: error code */
        public const int S_OK = 0x00000000;                             /* Success */
        public const int S_FALSE = 0x00000001;                          /* Success with noop */
        public const int E_UNEXPECTED = unchecked((int)0x8000ffff);     /* Catastrophic failure */
        public const int E_NOTIMPL = unchecked((int)0x80004001);        /* Not supported or not implemented */
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);   /* Permission denied */
        public const int E_OUTOFMEMORY = unchecked((int)0x8007000e);    /* Out of memory */
        public const int E_INVALIDARG = unchecked((int)0x80070057);     /* One or more arguments are not valid */
        public const int E_POINTER = unchecked((int)0x80004003);        /* Pointer that is not valid */
        public const int E_FAIL = unchecked((int)0x80004005);           /* Generic failure */
        public const int E_WRONG_THREAD = unchecked((int)0x8001010e);   /* Call function in the wrong thread */
        public const int E_GEN_FAILURE = unchecked((int)0x8007001f);    /* Device not functioning */
        public const int E_PENDING = unchecked((int)0x8000000a);        /* The data necessary to complete this operation is not yet available */
        public const int E_TIMEOUT = unchecked((int)0x8001011f);        /* This operation returned because the timeout period expired */

        public const int EXPOGAIN_DEF = 100;      /* exposure gain, default value */
        public const int EXPOGAIN_MIN = 100;      /* exposure gain, minimum value */
        public const int TEMP_DEF = 6503;     /* color temperature, default value */
        public const int TEMP_MIN = 2000;     /* color temperature, minimum value */
        public const int TEMP_MAX = 15000;    /* color temperature, maximum value */
        public const int TINT_DEF = 1000;     /* tint */
        public const int TINT_MIN = 200;      /* tint */
        public const int TINT_MAX = 2500;     /* tint */
        public const int HUE_DEF = 0;        /* hue */
        public const int HUE_MIN = -180;     /* hue */
        public const int HUE_MAX = 180;      /* hue */
        public const int SATURATION_DEF = 128;      /* saturation */
        public const int SATURATION_MIN = 0;        /* saturation */
        public const int SATURATION_MAX = 255;      /* saturation */
        public const int BRIGHTNESS_DEF = 0;        /* brightness */
        public const int BRIGHTNESS_MIN = -64;      /* brightness */
        public const int BRIGHTNESS_MAX = 64;       /* brightness */
        public const int CONTRAST_DEF = 0;        /* contrast */
        public const int CONTRAST_MIN = -100;     /* contrast */
        public const int CONTRAST_MAX = 100;      /* contrast */
        public const int GAMMA_DEF = 100;      /* gamma */
        public const int GAMMA_MIN = 20;       /* gamma */
        public const int GAMMA_MAX = 180;      /* gamma */
        public const int AETARGET_DEF = 120;      /* target of auto exposure */
        public const int AETARGET_MIN = 16;       /* target of auto exposure */
        public const int AETARGET_MAX = 220;      /* target of auto exposure */
        public const int WBGAIN_DEF = 0;        /* white balance gain */
        public const int WBGAIN_MIN = -127;     /* white balance gain */
        public const int WBGAIN_MAX = 127;      /* white balance gain */
        public const int BLACKLEVEL_MIN = 0;        /* minimum black level */
        public const int BLACKLEVEL8_MAX = 31;       /* maximum black level for bitdepth = 8 */
        public const int BLACKLEVEL10_MAX = 31 * 4;   /* maximum black level for bitdepth = 10 */
        public const int BLACKLEVEL12_MAX = 31 * 16;  /* maximum black level for bitdepth = 12 */
        public const int BLACKLEVEL14_MAX = 31 * 64;  /* maximum black level for bitdepth = 14 */
        public const int BLACKLEVEL16_MAX = 31 * 256; /* maximum black level for bitdepth = 16 */
        public const int SHARPENING_STRENGTH_DEF = 0;        /* sharpening strength */
        public const int SHARPENING_STRENGTH_MIN = 0;        /* sharpening strength */
        public const int SHARPENING_STRENGTH_MAX = 500;      /* sharpening strength */
        public const int SHARPENING_RADIUS_DEF = 2;        /* sharpening radius */
        public const int SHARPENING_RADIUS_MIN = 1;        /* sharpening radius */
        public const int SHARPENING_RADIUS_MAX = 10;       /* sharpening radius */
        public const int SHARPENING_THRESHOLD_DEF = 0;        /* sharpening threshold */
        public const int SHARPENING_THRESHOLD_MIN = 0;        /* sharpening threshold */
        public const int SHARPENING_THRESHOLD_MAX = 255;      /* sharpening threshold */
        public const int AUTOEXPO_THRESHOLD_DEF = 5;        /* auto exposure threshold */
        public const int AUTOEXPO_THRESHOLD_MIN = 2;        /* auto exposure threshold */
        public const int AUTOEXPO_THRESHOLD_MAX = 15;       /* auto exposure threshold */
        public const int AUTOEXPO_STEP_DEF = 1000;     /* auto exposure step: thousandths */
        public const int AUTOEXPO_STEP_MIN = 1;        /* auto exposure step: thousandths */
        public const int AUTOEXPO_STEP_MAX = 1000;     /* auto exposure step: thousandths */
        public const int BANDWIDTH_DEF = 100;      /* bandwidth */
        public const int BANDWIDTH_MIN = 1;        /* bandwidth */
        public const int BANDWIDTH_MAX = 100;      /* bandwidth */
        public const int DENOISE_DEF = 0;        /* denoise */
        public const int DENOISE_MIN = 0;        /* denoise */
        public const int DENOISE_MAX = 100;      /* denoise */
        public const int TEC_TARGET_MIN = -500;     /* TEC target: -50.0 degrees Celsius */
        public const int TEC_TARGET_DEF = 100;      /* TEC target: 0.0 degrees Celsius */
        public const int TEC_TARGET_MAX = 400;      /* TEC target: 40.0 degrees Celsius */
        public const int HEARTBEAT_MIN = 100;      /* millisecond */
        public const int HEARTBEAT_MAX = 10000;    /* millisecond */
        public const int AE_PERCENT_MIN = 0;        /* auto exposure percent, 0 => full roi average */
        public const int AE_PERCENT_MAX = 100;
        public const int AE_PERCENT_DEF = 10;
        public const int NOPACKET_TIMEOUT_MIN = 500;      /* no packet timeout minimum: 500ms */
        public const int NOFRAME_TIMEOUT_MIN = 500;      /* no frame timeout minimum: 500ms */
        public const int DYNAMIC_DEFECT_T1_MIN = 10;       /* dynamic defect pixel correction, threshold, means: 1.0 */
        public const int DYNAMIC_DEFECT_T1_MAX = 100;      /* means: 10.0 */
        public const int DYNAMIC_DEFECT_T1_DEF = 13;       /* means: 1.3 */
        public const int DYNAMIC_DEFECT_T2_MIN = 0;        /* dynamic defect pixel correction, value, means: 0.00 */
        public const int DYNAMIC_DEFECT_T2_MAX = 100;      /* means: 1.00 */
        public const int DYNAMIC_DEFECT_T2_DEF = 100;
        public const int HDR_K_MIN = 1;        /* HDR synthesize */
        public const int HDR_K_MAX = 25500;
        public const int HDR_B_MIN = 0;
        public const int HDR_B_MAX = 65535;
        public const int HDR_THRESHOLD_MIN = 0;
        public const int HDR_THRESHOLD_MAX = 4094;

        public enum ePIXELFORMAT : uint {
            PIXELFORMAT_RAW8 = 0x00,
            PIXELFORMAT_RAW10 = 0x01,
            PIXELFORMAT_RAW12 = 0x02,
            PIXELFORMAT_RAW14 = 0x03,
            PIXELFORMAT_RAW16 = 0x04,
            PIXELFORMAT_YUV411 = 0x05,
            PIXELFORMAT_VUYY = 0x06,
            PIXELFORMAT_YUV444 = 0x07,
            PIXELFORMAT_RGB888 = 0x08,
            PIXELFORMAT_GMCY8 = 0x09,
            PIXELFORMAT_GMCY12 = 0x0a,
            PIXELFORMAT_UYVY = 0x0b
        };

        public enum eFRAMEINFO_FLAG : uint {
            FRAMEINFO_FLAG_SEQ = 0x0001, /* frame sequence number */
            FRAMEINFO_FLAG_TIMESTAMP = 0x0002, /* timestamp */
            FRAMEINFO_FLAG_EXPOTIME = 0x0004, /* exposure time */
            FRAMEINFO_FLAG_EXPOGAIN = 0x0008, /* exposure gain */
            FRAMEINFO_FLAG_BLACKLEVEL = 0x0010, /* black level */
            FRAMEINFO_FLAG_SHUTTERSEQ = 0x0020, /* sequence shutter counter */
            FRAMEINFO_FLAG_STILL = 0x8000  /* still image */
        };

        public enum eIoControType : uint {
            IOCONTROLTYPE_GET_SUPPORTEDMODE = 0x01, /* 0x01 => Input, 0x02 => Output, (0x01 | 0x02) => support both Input and Output */
            IOCONTROLTYPE_GET_GPIODIR = 0x03, /* 0x00 => Input, 0x01 => Output */
            IOCONTROLTYPE_SET_GPIODIR = 0x04,
            IOCONTROLTYPE_GET_FORMAT = 0x05, /*
                                                           0x00 => not connected
                                                           0x01 => Tri-state: Tri-state mode (Not driven)
                                                           0x02 => TTL: TTL level signals
                                                           0x03 => LVDS: LVDS level signals
                                                           0x04 => RS422: RS422 level signals
                                                           0x05 => Opto-coupled
                                                        */
            IOCONTROLTYPE_SET_FORMAT = 0x06,
            IOCONTROLTYPE_GET_OUTPUTINVERTER = 0x07, /* boolean, only support output signal */
            IOCONTROLTYPE_SET_OUTPUTINVERTER = 0x08,
            IOCONTROLTYPE_GET_INPUTACTIVATION = 0x09, /* 0x00 => Positive, 0x01 => Negative */
            IOCONTROLTYPE_SET_INPUTACTIVATION = 0x0a,
            IOCONTROLTYPE_GET_DEBOUNCERTIME = 0x0b, /* debouncer time in microseconds, [0, 20000] */
            IOCONTROLTYPE_SET_DEBOUNCERTIME = 0x0c,
            IOCONTROLTYPE_GET_TRIGGERSOURCE = 0x0d, /*
                                                           0x00 => Opto-isolated input
                                                           0x01 => GPIO0
                                                           0x02 => GPIO1
                                                           0x03 => Counter
                                                           0x04 => PWM
                                                           0x05 => Software
                                                        */
            IOCONTROLTYPE_SET_TRIGGERSOURCE = 0x0e,
            IOCONTROLTYPE_GET_TRIGGERDELAY = 0x0f, /* Trigger delay time in microseconds, [0, 5000000] */
            IOCONTROLTYPE_SET_TRIGGERDELAY = 0x10,
            IOCONTROLTYPE_GET_BURSTCOUNTER = 0x11, /* Burst Counter, range: [1 ~ 65535] */
            IOCONTROLTYPE_SET_BURSTCOUNTER = 0x12,
            IOCONTROLTYPE_GET_COUNTERSOURCE = 0x13, /* 0x00 => Opto-isolated input, 0x01 => GPIO0, 0x02=> GPIO1 */
            IOCONTROLTYPE_SET_COUNTERSOURCE = 0x14,
            IOCONTROLTYPE_GET_COUNTERVALUE = 0x15, /* Counter Value, range: [1 ~ 65535] */
            IOCONTROLTYPE_SET_COUNTERVALUE = 0x16,
            IOCONTROLTYPE_SET_RESETCOUNTER = 0x18,
            IOCONTROLTYPE_GET_PWM_FREQ = 0x19,
            IOCONTROLTYPE_SET_PWM_FREQ = 0x1a,
            IOCONTROLTYPE_GET_PWM_DUTYRATIO = 0x1b,
            IOCONTROLTYPE_SET_PWM_DUTYRATIO = 0x1c,
            IOCONTROLTYPE_GET_PWMSOURCE = 0x1d, /* 0x00 => Opto-isolated input, 0x01 => GPIO0, 0x02 => GPIO1 */
            IOCONTROLTYPE_SET_PWMSOURCE = 0x1e,
            IOCONTROLTYPE_GET_OUTPUTMODE = 0x1f, /*
                                                           0x00 => Frame Trigger Wait
                                                           0x01 => Exposure Active
                                                           0x02 => Strobe
                                                           0x03 => User output
                                                        */
            IOCONTROLTYPE_SET_OUTPUTMODE = 0x20,
            IOCONTROLTYPE_GET_STROBEDELAYMODE = 0x21, /* boolean, 1 => delay, 0 => pre-delay; compared to exposure active signal */
            IOCONTROLTYPE_SET_STROBEDELAYMODE = 0x22,
            IOCONTROLTYPE_GET_STROBEDELAYTIME = 0x23, /* Strobe delay or pre-delay time in microseconds, [0, 5000000] */
            IOCONTROLTYPE_SET_STROBEDELAYTIME = 0x24,
            IOCONTROLTYPE_GET_STROBEDURATION = 0x25, /* Strobe duration time in microseconds, [0, 5000000] */
            IOCONTROLTYPE_SET_STROBEDURATION = 0x26,
            IOCONTROLTYPE_GET_USERVALUE = 0x27, /*
                                                           bit0 => Opto-isolated output
                                                           bit1 => GPIO0 output
                                                           bit2 => GPIO1 output
                                                        */
            IOCONTROLTYPE_SET_USERVALUE = 0x28,
            IOCONTROLTYPE_GET_UART_ENABLE = 0x29, /* enable: 1 => on; 0 => off */
            IOCONTROLTYPE_SET_UART_ENABLE = 0x2a,
            IOCONTROLTYPE_GET_UART_BAUDRATE = 0x2b, /* baud rate: 0 => 9600; 1 => 19200; 2 => 38400; 3 => 57600; 4 => 115200 */
            IOCONTROLTYPE_SET_UART_BAUDRATE = 0x2c,
            IOCONTROLTYPE_GET_UART_LINEMODE = 0x2d, /* line mode: 0 => TX(GPIO_0)/RX(GPIO_1); 1 => TX(GPIO_1)/RX(GPIO_0) */
            IOCONTROLTYPE_SET_UART_LINEMODE = 0x2e,
            IOCONTROLTYPE_GET_EXPO_ACTIVE_MODE = 0x2f, /* exposure time signal: 0 => specified line, 1 => common exposure time */
            IOCONTROLTYPE_SET_EXPO_ACTIVE_MODE = 0x30,
            IOCONTROLTYPE_GET_EXPO_START_LINE = 0x31, /* exposure start line, default: 0 */
            IOCONTROLTYPE_SET_EXPO_START_LINE = 0x32,
            IOCONTROLTYPE_GET_EXPO_END_LINE = 0x33, /* exposure end line, default: 0
                                                           end line must be no less than start line
                                                        */
            IOCONTROLTYPE_SET_EXPO_END_LINE = 0x34,
            IOCONTROLTYPE_GET_EXEVT_ACTIVE_MODE = 0x35, /* exposure event: 0 => specified line, 1 => common exposure time */
            IOCONTROLTYPE_SET_EXEVT_ACTIVE_MODE = 0x36
        };

        /* hardware level range mode */
        public enum eLevelRange : ushort {
            LEVELRANGE_MANUAL = 0x0000,    /* manual */
            LEVELRANGE_ONCE = 0x0001,    /* once */
            LEVELRANGE_CONTINUE = 0x0002,    /* continue */
            LEVELRANGE_ROI = 0xffff     /* update roi rect only */
        };

        public struct Resolution {
            public uint width;
            public uint height;
        };
        public struct ModelV2 {
            public string name;         /* model name */
            public ulong flag;          /* ALTAIRCAM_FLAG_xxx, 64 bits */
            public uint maxspeed;       /* number of speed level, same as get_MaxSpeed(), the speed range = [0, maxspeed], closed interval */
            public uint preview;        /* number of preview resolution, same as get_ResolutionNumber() */
            public uint still;          /* number of still resolution, same as get_StillResolutionNumber() */
            public uint maxfanspeed;    /* maximum fan speed, fan speed range = [0, max], closed interval */
            public uint ioctrol;        /* number of input/output control */
            public float xpixsz;        /* physical pixel size */
            public float ypixsz;        /* physical pixel size */
            public Resolution[] res;
        };
        public struct DeviceV2 {
            public string displayname; /* display name */
            public string id;          /* unique and opaque id of a connected camera */
            public ModelV2 model;
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct FrameInfoV3 {
            public uint width;
            public uint height;
            public uint flag;          /* FRAMEINFO_FLAG_xxxx */
            public uint seq;           /* frame sequence number */
            public ulong timestamp;    /* microsecond */
            public uint shutterseq;    /* sequence shutter counter */
            public uint expotime;      /* exposure time */
            public ushort expogain;    /* exposure gain */
            public ushort blacklevel;  /* black level */
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct FrameInfoV2 {
            public uint width;
            public uint height;
            public uint flag;          /* FRAMEINFO_FLAG_xxxx */
            public uint seq;           /* frame sequence number */
            public ulong timestamp;    /* microsecond */
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct AfParam {
            public int imax;           /* maximum auto focus sensor board positon */
            public int imin;           /* minimum auto focus sensor board positon */
            public int idef;           /* conjugate calibration positon */
            public int imaxabs;        /* maximum absolute auto focus sensor board positon, micrometer */
            public int iminabs;        /* maximum absolute auto focus sensor board positon, micrometer */
            public int zoneh;          /* zone horizontal */
            public int zonev;          /* zone vertical */
        };
        [Obsolete("Use ModelV2")]
        public struct Model {
            public string name;
            public uint flag;
            public uint maxspeed;
            public uint preview;
            public uint still;
            public Resolution[] res;
        };
        [Obsolete("Use DeviceV2")]
        public struct Device {
            public string displayname; /* display name */
            public string id;          /* unique and opaque id of a connected camera */
            public Model model;
        };

#if !(NETFX_CORE || WINDOWS_UWP)

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, IntPtr Length);

#endif

        /* only for compatibility with .Net 4.0 and below */

        public static IntPtr IncIntPtr(IntPtr p, int offset) {
            return new IntPtr(p.ToInt64() + offset);
        }

        private static bool IsUnicode() {
#if (WINDOWS_UWP)
        return true;
#else
            return (Environment.OSVersion.Platform == PlatformID.Win32NT);
#endif
        }

        public delegate void DelegateEventCallback(eEVENT nEvent);

        public delegate void DelegateDataCallbackV3(IntPtr pData, ref FrameInfoV2 info, bool bSnap);

        public delegate void DelegateHistogramCallback(float[] aHistY, float[] aHistR, float[] aHistG, float[] aHistB);

        [UnmanagedFunctionPointerAttribute(cc)]
        private delegate void EVENT_CALLBACK(eEVENT nEvent, IntPtr pCtx);

        [UnmanagedFunctionPointerAttribute(cc)]
        private delegate void DATA_CALLBACK_V3(IntPtr pData, IntPtr pInfo, bool bSnap, IntPtr pCallbackCtx);

        [UnmanagedFunctionPointerAttribute(cc)]
        private delegate void HISTOGRAM_CALLBACK(IntPtr aHistY, IntPtr aHistR, IntPtr aHistG, IntPtr aHistB, IntPtr pCtx);

#if !(NETFX_CORE || WINDOWS_UWP)

        public class SafeCamHandle : SafeHandleZeroOrMinusOneIsInvalid {

            static SafeCamHandle() {
                DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
            }

            [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
            private static extern void Altaircam_Close(IntPtr h);

            public SafeCamHandle()
                : base(true) {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle() {
                // Here, we must obey all rules for constrained execution regions.
                Altaircam_Close(handle);
                return true;
            }
        };

#else
    public class SafeCamHandle : SafeHandle
    {
        static SafeCamHandle() {
            DllLoader.LoadDll(Path.Combine("Altair", DLLNAME));
        }

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern void Altaircam_Close(IntPtr h);

        public SafeCamHandle()
            : base(IntPtr.Zero, true)
        {
        }

        override protected bool ReleaseHandle()
        {
            Altaircam_Close(handle);
            return true;
        }

        public override bool IsInvalid
        {
            get { return base.handle == IntPtr.Zero || base.handle == (IntPtr)(-1); }
        }
    };
#endif

        private const CallingConvention cc = CallingConvention.StdCall;
        private const UnmanagedType ut = UnmanagedType.LPWStr;

        private const string DLLNAME = "altaircam.dll";

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int left, top, right, bottom;
        };

        /* FUTURE REF: Original sdk shipped code changed due to app crash */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern IntPtr Altaircam_Version();

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc), Obsolete("Use Altaircam_EnumV2")]
        private static extern uint Altaircam_Enum(IntPtr ti);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_EnumV2(IntPtr ti);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern SafeCamHandle Altaircam_Open([MarshalAs(ut)] string id);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern SafeCamHandle Altaircam_OpenByIndex(uint index);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_StartPullModeWithWndMsg(SafeCamHandle h, IntPtr hWnd, uint nMsg);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_StartPullModeWithCallback(SafeCamHandle h, EVENT_CALLBACK pEventCallback, IntPtr pCallbackCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullImage(SafeCamHandle h, IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullStillImage(SafeCamHandle h, IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullImageWithRowPitch(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullStillImageWithRowPitch(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullImageV2(SafeCamHandle h, [Out] ushort[] pImageData, int bits, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullStillImageV2(SafeCamHandle h, IntPtr pImageData, int bits, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullImageWithRowPitchV2(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_PullStillImageWithRowPitchV2(SafeCamHandle h, IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_StartPushModeV3(SafeCamHandle h, DATA_CALLBACK_V3 pDataCallback, IntPtr pDataCallbackCtx, EVENT_CALLBACK pEventCallback, IntPtr pEventCallbackCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Stop(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Pause(SafeCamHandle h, int bPause);

        /* for still image snap */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Snap(SafeCamHandle h, uint nResolutionIndex);

        /* multiple still image snap */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_SnapN(SafeCamHandle h, uint nResolutionIndex, uint nNumber);

        /*
            soft trigger:
            nNumber:    0xffff:     trigger continuously
                        0:          cancel trigger
                        others:     number of images to be triggered
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Trigger(SafeCamHandle h, ushort nNumber);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Size(SafeCamHandle h, int nWidth, int nHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Size(SafeCamHandle h, out int nWidth, out int nHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_eSize(SafeCamHandle h, uint nResolutionIndex);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_eSize(SafeCamHandle h, out uint nResolutionIndex);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_FinalSize(SafeCamHandle h, out int nWidth, out int nHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_ResolutionNumber(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_Resolution(SafeCamHandle h, uint nResolutionIndex, out int pWidth, out int pHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_ResolutionRatio(SafeCamHandle h, uint nResolutionIndex, out int pNumerator, out int pDenominator);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_Field(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_RawFormat(SafeCamHandle h, out uint nFourCC, out uint bitdepth);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_RealTime(SafeCamHandle h, int val);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_RealTime(SafeCamHandle h, out int val);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Flush(SafeCamHandle h);

        /* sensor Temperature */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Temperature(SafeCamHandle h, out short pTemperature);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Temperature(SafeCamHandle h, short nTemperature);

        /* ROI */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Roi(SafeCamHandle h, out uint pxOffset, out uint pyOffset, out uint pxWidth, out uint pyHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Roi(SafeCamHandle h, uint xOffset, uint yOffset, uint xWidth, uint yHeight);

        /*
            ------------------------------------------------------------------|
            | Parameter               |   Range       |   Default             |
            |-----------------------------------------------------------------|
            | Auto Exposure Target    |   16~235      |   120                 |
            | Temp                    |   2000~15000  |   6503                |
            | Tint                    |   200~2500    |   1000                |
            | LevelRange              |   0~255       |   Low = 0, High = 255 |
            | Contrast                |   -100~100    |   0                   |
            | Hue                     |   -180~180    |   0                   |
            | Saturation              |   0~255       |   128                 |
            | Brightness              |   -64~64      |   0                   |
            | Gamma                   |   20~180      |   100                 |
            | WBGain                  |   -127~127    |   0                   |
            ------------------------------------------------------------------|
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_AutoExpoEnable(SafeCamHandle h, out int bAutoExposure);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_AutoExpoEnable(SafeCamHandle h, int bAutoExposure);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_AutoExpoTarget(SafeCamHandle h, out ushort Target);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_AutoExpoTarget(SafeCamHandle h, ushort Target);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_MaxAutoExpoTimeAGain(SafeCamHandle h, uint maxTime, ushort maxAGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_MaxAutoExpoTimeAGain(SafeCamHandle h, out uint maxTime, out ushort maxAGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_MinAutoExpoTimeAGain(SafeCamHandle h, uint minTime, ushort minAGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_MinAutoExpoTimeAGain(SafeCamHandle h, out uint minTime, out ushort minAGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ExpoTime(SafeCamHandle h, out uint Time)/* in microseconds */;

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_ExpoTime(SafeCamHandle h, uint Time)/* inmicroseconds */;

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ExpTimeRange(SafeCamHandle h, out uint nMin, out uint nMax, out uint nDef);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ExpoAGain(SafeCamHandle h, out ushort AGain);/* percent, such as 300 */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_ExpoAGain(SafeCamHandle h, ushort AGain);/* percent */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ExpoAGainRange(SafeCamHandle h, out ushort nMin, out ushort nMax, out ushort nDef);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_LevelRange(SafeCamHandle h, [In] ushort[] aLow, [In] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_LevelRange(SafeCamHandle h, [Out] ushort[] aLow, [Out] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_LevelRangeV2(SafeCamHandle h, ushort mode, ref RECT roiRect, [In] ushort[] aLow, [In] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_LevelRangeV2(SafeCamHandle h, out ushort mode, out RECT pRoiRect, [Out] ushort[] aLow, [Out] ushort[] aHigh);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Hue(SafeCamHandle h, int Hue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Hue(SafeCamHandle h, out int Hue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Saturation(SafeCamHandle h, int Saturation);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Saturation(SafeCamHandle h, out int Saturation);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Brightness(SafeCamHandle h, int Brightness);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Brightness(SafeCamHandle h, out int Brightness);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Contrast(SafeCamHandle h, out int Contrast);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Contrast(SafeCamHandle h, int Contrast);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Gamma(SafeCamHandle h, out int Gamma);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Gamma(SafeCamHandle h, int Gamma);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Chrome(SafeCamHandle h, out int bChrome);    /* monochromatic mode */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Chrome(SafeCamHandle h, int bChrome);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_VFlip(SafeCamHandle h, out int bVFlip);  /* vertical flip */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_VFlip(SafeCamHandle h, int bVFlip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_HFlip(SafeCamHandle h, out int bHFlip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_HFlip(SafeCamHandle h, int bHFlip);  /* horizontal flip */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Negative(SafeCamHandle h, out int bNegative);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Negative(SafeCamHandle h, int bNegative);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Speed(SafeCamHandle h, ushort nSpeed);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Speed(SafeCamHandle h, out ushort pSpeed);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_MaxSpeed(SafeCamHandle h);/* get the maximum speed, "Frame Speed Level", speed range = [0, max] */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_MaxBitDepth(SafeCamHandle h);/* get the max bit depth of this camera, such as 8, 10, 12, 14, 16 */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_FanMaxSpeed(SafeCamHandle h);/* get the maximum fan speed, the fan speed range = [0, max], closed interval */

        /* power supply:
                0 -> 60HZ AC
                1 -> 50Hz AC
                2 -> DC
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_HZ(SafeCamHandle h, int nHZ);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_HZ(SafeCamHandle h, out int nHZ);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Mode(SafeCamHandle h, int bSkip); /* skip or bin */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Mode(SafeCamHandle h, out int bSkip);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_TempTint(SafeCamHandle h, int nTemp, int nTint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_TempTint(SafeCamHandle h, out int nTemp, out int nTint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_WhiteBalanceGain(SafeCamHandle h, [In] int[] aGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_WhiteBalanceGain(SafeCamHandle h, [Out] int[] aGain);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_BlackBalance(SafeCamHandle h, [In] ushort[] aSub);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_BlackBalance(SafeCamHandle h, [Out] ushort[] aSub);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_AWBAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_AWBAuxRect(SafeCamHandle h, out RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_AEAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_AEAuxRect(SafeCamHandle h, out RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_ABBAuxRect(SafeCamHandle h, ref RECT pAuxRect);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ABBAuxRect(SafeCamHandle h, out RECT pAuxRect);

        /*
            S_FALSE:    color mode
            S_OK:       mono mode, such as EXCCD00300KMA and UHCCD01400KMA
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_MonoMode(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern uint Altaircam_get_StillResolutionNumber(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_StillResolution(SafeCamHandle h, uint nResolutionIndex, out int pWidth, out int pHeight);

        /*
            get the revision
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Revision(SafeCamHandle h, out ushort pRevision);

        /*
            get the serial number which is always 32 chars which is zero-terminated such as "TP110826145730ABCD1234FEDC56787"
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_SerialNumber(SafeCamHandle h, IntPtr sn);

        /*
            get the camera firmware version, such as: 3.2.1.20140922
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_FwVersion(SafeCamHandle h, IntPtr fwver);

        /*
            get the camera hardware version, such as: 3.2.1.20140922
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_HwVersion(SafeCamHandle h, IntPtr hwver);

        /*
            get the FPGA version, such as: 1.3
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_FpgaVersion(SafeCamHandle h, IntPtr fpgaver);

        /*
            get the production date, such as: 20150327
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_ProductionDate(SafeCamHandle h, IntPtr pdate);

        /*
            get the sensor pixel size, such as: 2.4um
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_PixelSize(SafeCamHandle h, uint nResolutionIndex, out float x, out float y);

        /*
                    ------------------------------------------------------------|
                    | Parameter         |   Range       |   Default             |
                    |-----------------------------------------------------------|
                    | VidgetAmount      |   -100~100    |   0                   |
                    | VignetMidPoint    |   0~100       |   50                  |
                    -------------------------------------------------------------
        */

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_VignetEnable(SafeCamHandle h, int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_VignetEnable(SafeCamHandle h, out int bEnable);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_VignetAmountInt(SafeCamHandle h, int nAmount);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_VignetAmountInt(SafeCamHandle h, out int nAmount);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_VignetMidPointInt(SafeCamHandle h, int nMidPoint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_VignetMidPointInt(SafeCamHandle h, out int nMidPoint);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_AwbOnce(SafeCamHandle h, IntPtr fnTTProc, IntPtr pTTCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_AwbInit(SafeCamHandle h, IntPtr fnWBProc, IntPtr pWBCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_LevelRangeAuto(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_GetHistogram(SafeCamHandle h, HISTOGRAM_CALLBACK fnHistogramProc, IntPtr pHistogramCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_AbbOnce(SafeCamHandle h, IntPtr fnBBProc, IntPtr pBBCtx);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_LEDState(SafeCamHandle h, ushort iLed, ushort iState, ushort iPeriod);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_write_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_read_EEPROM(SafeCamHandle h, uint addr, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_write_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_read_Pipe(SafeCamHandle h, uint pipeNum, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_feed_Pipe(SafeCamHandle h, uint pipeNum);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_write_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_read_UART(SafeCamHandle h, IntPtr pBuffer, uint nBufferLen);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Option(SafeCamHandle h, eOPTION iOption, int iValue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_Option(SafeCamHandle h, eOPTION iOption, out int iValue);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Linear(SafeCamHandle h, byte[] v8, ushort[] v16);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_Curve(SafeCamHandle h, byte[] v8, ushort[] v16);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_ColorMatrix(SafeCamHandle h, double[] v);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_put_InitWBGain(SafeCamHandle h, ushort[] v);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_FrameRate(SafeCamHandle h, out uint nFrame, out uint nTime, out uint nTotalFrame);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_FfcOnce(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_DfcOnce(SafeCamHandle h);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_FfcExport(SafeCamHandle h, [MarshalAs(ut)] string filepath);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_FfcImport(SafeCamHandle h, [MarshalAs(ut)] string filepath);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_DfcExport(SafeCamHandle h, [MarshalAs(ut)] string filepath);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_DfcImport(SafeCamHandle h, [MarshalAs(ut)] string filepath);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_IoControl(SafeCamHandle h, uint index, eIoControType eType, int outVal, out int inVal);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_get_AfParam(SafeCamHandle h, out AfParam pAfParam);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern double Altaircam_calc_ClarityFactor(IntPtr pImageData, int bits, uint nImgWidth, uint nImgHeight);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern void Altaircam_deBayerV2(uint nBayer, int nW, int nH, IntPtr input, IntPtr output, byte nBitDepth, byte nBitCount);

        [DllImport(DLLNAME, ExactSpelling = true, CallingConvention = cc)]
        private static extern int Altaircam_Replug([MarshalAs(ut)] string id);

        static public uint MAKEFOURCC(uint a, uint b, uint c, uint d) {
            return ((uint)(byte)(a) | ((uint)(byte)(b) << 8) | ((uint)(byte)(c) << 16) | ((uint)(byte)(d) << 24));
        }

        private static int _sid = 0;
        private static Dictionary<int, AltairCam> _map = new Dictionary<int, AltairCam>();

        private SafeCamHandle _handle;
        private IntPtr _id;
        private DelegateDataCallbackV3 _dDataCallbackV3;
        private DelegateEventCallback _dEventCallback;
        private DelegateHistogramCallback _dHistogramCallback;
        private DATA_CALLBACK_V3 _pDataCallbackV3;
        private EVENT_CALLBACK _pEventCallback;
        private HISTOGRAM_CALLBACK _pHistogramCallback;

        private void EventCallback(eEVENT nEvent) {
            if (_dEventCallback != null)
                _dEventCallback(nEvent);
        }

        private void DataCallbackV3(IntPtr pData, IntPtr pInfo, bool bSnap) {
            if (pData == IntPtr.Zero || pInfo == IntPtr.Zero) /* pData == 0 means that something error, we callback to tell the application */
            {
                if (_dDataCallbackV3 != null) {
                    FrameInfoV2 info = new FrameInfoV2();
                    _dDataCallbackV3(IntPtr.Zero, ref info, bSnap);
                }
            } else {
#if !(NETFX_CORE || WINDOWS_UWP)
                FrameInfoV2 info = (FrameInfoV2)Marshal.PtrToStructure(pInfo, typeof(FrameInfoV2));
#else
            FrameInfoV2 info = Marshal.PtrToStructure<FrameInfoV2>(pInfo);
#endif
                if (_dDataCallbackV3 != null)
                    _dDataCallbackV3(pData, ref info, bSnap);
            }
        }

        private void HistogramCallback(float[] aHistY, float[] aHistR, float[] aHistG, float[] aHistB) {
            if (_dHistogramCallback != null) {
                _dHistogramCallback(aHistY, aHistR, aHistG, aHistB);
                _dHistogramCallback = null;
            }
            _pHistogramCallback = null;
        }

        private static void DataCallbackV3(IntPtr pData, IntPtr pInfo, bool bSnap, IntPtr pCallbackCtx) {
            AltairCam pthis = null;
            if (_map.TryGetValue(pCallbackCtx.ToInt32(), out pthis) && (pthis != null)) {
                if (pthis != null)
                    pthis.DataCallbackV3(pData, pInfo, bSnap);
            }
        }

        private static void EventCallback(eEVENT nEvent, IntPtr pCallbackCtx) {
            AltairCam pthis = null;
            if (_map.TryGetValue(pCallbackCtx.ToInt32(), out pthis) && (pthis != null))
                pthis.EventCallback(nEvent);
        }

        private static void HistogramCallback(IntPtr aHistY, IntPtr aHistR, IntPtr aHistG, IntPtr aHistB, IntPtr pCallbackCtx) {
            AltairCam pthis = null;
            if (_map.TryGetValue(pCallbackCtx.ToInt32(), out pthis) && (pthis != null)) {
                float[] arrHistY = new float[256];
                float[] arrHistR = new float[256];
                float[] arrHistG = new float[256];
                float[] arrHistB = new float[256];
                Marshal.Copy(aHistY, arrHistY, 0, 256);
                Marshal.Copy(aHistR, arrHistR, 0, 256);
                Marshal.Copy(aHistG, arrHistG, 0, 256);
                Marshal.Copy(aHistB, arrHistB, 0, 256);
                pthis.HistogramCallback(arrHistY, arrHistR, arrHistG, arrHistB);
            }
        }

        /*
            the object of Altaircam must be obtained by static mothod Open or OpenByIndex, it cannot be obtained by obj = new Altaircam (The constructor is private on purpose)
        */

        private AltairCam(SafeCamHandle h) {
            _handle = h;
            _id = new IntPtr(Interlocked.Increment(ref _sid));
            _map.Add(_id.ToInt32(), this);
        }

        ~AltairCam() {
            Dispose(false);
        }

#if !(NETFX_CORE || WINDOWS_UWP)

        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
        protected virtual void Dispose(bool disposing) {
            // Note there are three interesting states here:
            // 1) CreateFile failed, _handle contains an invalid handle
            // 2) We called Dispose already, _handle is closed.
            // 3) _handle is null, due to an async exception before
            //    calling CreateFile. Note that the finalizer runs
            //    if the constructor fails.
            if (_handle != null && !_handle.IsInvalid) {
                // Free the handle
                _handle.Dispose();
            }
            // SafeHandle records the fact that we've called Dispose.
        }

        public void Dispose()  // Follow the Dispose pattern - public nonvirtual.
        {
            Dispose(true);
            _map.Remove(_id.ToInt32());
            GC.SuppressFinalize(this);
        }

        public void Close() {
            Dispose();
        }

        /* get the version of this dll/so, which is: 48.17962.2020.1110 */

        public static string Version() {
            return Marshal.PtrToStringUni(Altaircam_Version());
        }

        /* enumerate Altaircam cameras that are currently connected to computer */

        public static DeviceV2[] EnumV2() {
            IntPtr p = Marshal.AllocHGlobal(512 * 16);
            IntPtr ti = p;
            uint cnt = Altaircam_EnumV2(p);
            DeviceV2[] arr = new DeviceV2[cnt];
            if (cnt > 0) {
                float[] tmp = new float[1];
                for (uint i = 0; i < cnt; ++i) {
                    if (IsUnicode()) {
                        arr[i].displayname = Marshal.PtrToStringUni(p);
                        p = IncIntPtr(p, sizeof(char) * 64);
                        arr[i].id = Marshal.PtrToStringUni(p);
                        p = IncIntPtr(p, sizeof(char) * 64);
                    } else {
                        arr[i].displayname = Marshal.PtrToStringAnsi(p);
                        p = IncIntPtr(p, 64);
                        arr[i].id = Marshal.PtrToStringAnsi(p);
                        p = IncIntPtr(p, 64);
                    }

                    IntPtr q = Marshal.ReadIntPtr(p);
                    p = IncIntPtr(p, IntPtr.Size);

                    {
                        arr[i].model.name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(q));
                        q = IncIntPtr(q, IntPtr.Size);
                        if ((4 == IntPtr.Size) && IsUnicode())   /* 32bits windows */
                            q = IncIntPtr(q, 4); //skip 4 bytes, different from the linux version
                        arr[i].model.flag = (ulong)Marshal.ReadInt64(q);
                        q = IncIntPtr(q, sizeof(long));
                        arr[i].model.maxspeed = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.preview = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.still = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.maxfanspeed = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.ioctrol = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        Marshal.Copy(q, tmp, 0, 1);
                        arr[i].model.xpixsz = tmp[0];
                        q = IncIntPtr(q, sizeof(float));
                        Marshal.Copy(q, tmp, 0, 1);
                        arr[i].model.ypixsz = tmp[0];
                        q = IncIntPtr(q, sizeof(float));
                        uint resn = Math.Max(arr[i].model.preview, arr[i].model.still);
                        arr[i].model.res = new Resolution[resn];
                        for (uint j = 0; j < resn; ++j) {
                            arr[i].model.res[j].width = (uint)Marshal.ReadInt32(q);
                            q = IncIntPtr(q, sizeof(int));
                            arr[i].model.res[j].height = (uint)Marshal.ReadInt32(q);
                            q = IncIntPtr(q, sizeof(int));
                        }
                    }
                }
            }
            Marshal.FreeHGlobal(ti);
            return arr;
        }

        [Obsolete("Use EnumV2")]
        public static Device[] Enum() {
            IntPtr p = Marshal.AllocHGlobal(512 * 16);
            IntPtr ti = p;
            uint cnt = Altaircam_Enum(p);
            Device[] arr = new Device[cnt];
            if (cnt > 0) {
                for (uint i = 0; i < cnt; ++i) {
                    if (IsUnicode()) {
                        arr[i].displayname = Marshal.PtrToStringUni(p);
                        p = IncIntPtr(p, sizeof(char) * 64);
                        arr[i].id = Marshal.PtrToStringUni(p);
                        p = IncIntPtr(p, sizeof(char) * 64);
                    } else {
                        arr[i].displayname = Marshal.PtrToStringAnsi(p);
                        p = IncIntPtr(p, 64);
                        arr[i].id = Marshal.PtrToStringAnsi(p);
                        p = IncIntPtr(p, 64);
                    }

                    IntPtr q = Marshal.ReadIntPtr(p);
                    p = IncIntPtr(p, IntPtr.Size);

                    {
                        arr[i].model.name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(q));
                        q = IncIntPtr(q, IntPtr.Size);
                        arr[i].model.flag = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.maxspeed = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.preview = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));
                        arr[i].model.still = (uint)Marshal.ReadInt32(q);
                        q = IncIntPtr(q, sizeof(int));

                        uint resn = Math.Max(arr[i].model.preview, arr[i].model.still);
                        arr[i].model.res = new Resolution[resn];
                        for (uint j = 0; j < resn; ++j) {
                            arr[i].model.res[j].width = (uint)Marshal.ReadInt32(q);
                            q = IncIntPtr(q, sizeof(int));
                            arr[i].model.res[j].height = (uint)Marshal.ReadInt32(q);
                            q = IncIntPtr(q, sizeof(int));
                        }
                    }
                }
            }
            Marshal.FreeHGlobal(ti);
            return arr;
        }

        /*
            the object of Altaircam must be obtained by static mothod Open or OpenByIndex, it cannot be obtained by obj = new Altaircam (The constructor is private on purpose)
        */

        // id: enumerated by EnumV2, null means the first camera
        public static AltairCam Open(string id) {
            SafeCamHandle tmphandle = Altaircam_Open(id);
            if (tmphandle == null || tmphandle.IsInvalid || tmphandle.IsClosed)
                return null;
            return new AltairCam(tmphandle);
        }

        /*
            the object of Altaircam must be obtained by static mothod Open or OpenByIndex, it cannot be obtained by obj = new Altaircam (The constructor is private on purpose)
        */
        /*
            the same with Open, but use the index as the parameter. such as:
            index == 0, open the first camera,
            index == 1, open the second camera,
            etc
        */

        public static AltairCam OpenByIndex(uint index) {
            SafeCamHandle tmphandle = Altaircam_OpenByIndex(index);
            if (tmphandle == null || tmphandle.IsInvalid || tmphandle.IsClosed)
                return null;
            return new AltairCam(tmphandle);
        }

        public SafeCamHandle Handle {
            get {
                return _handle;
            }
        }

        public uint ResolutionNumber {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_ResolutionNumber(_handle);
            }
        }

        public uint StillResolutionNumber {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_StillResolutionNumber(_handle);
            }
        }

        public bool MonoMode {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return false;
                return (0 == Altaircam_get_MonoMode(_handle));
            }
        }

        /* get the maximum speed, "Frame Speed Level" */

        public uint MaxSpeed {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_MaxSpeed(_handle);
            }
        }

        /* get the max bit depth of this camera, such as 8, 10, 12, 14, 16 */

        public uint MaxBitDepth {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_MaxBitDepth(_handle);
            }
        }

        /* get the maximum fan speed, the fan speed range = [0, max], closed interval */

        public uint FanMaxSpeed {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return Altaircam_get_FanMaxSpeed(_handle);
            }
        }

        /* get the revision */

        public ushort Revision {
            get {
                ushort rev = 0;
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return rev;
                Altaircam_get_Revision(_handle, out rev);
                return rev;
            }
        }

        /* get the serial number which is always 32 chars which is zero-terminated such as "TP110826145730ABCD1234FEDC56787" */

        public string SerialNumber {
            get {
                string str = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return str;
                IntPtr ptr = Marshal.AllocHGlobal(64);
                if (Altaircam_get_SerialNumber(_handle, ptr) >= 0)
                    str = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return str;
            }
        }

        /* get the camera firmware version, such as: 3.2.1.20140922 */

        public string FwVersion {
            get {
                string str = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return str;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_FwVersion(_handle, ptr) >= 0)
                    str = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return str;
            }
        }

        /* get the camera hardware version, such as: 3.2.1.20140922 */

        public string HwVersion {
            get {
                string str = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return str;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_HwVersion(_handle, ptr) >= 0)
                    str = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return str;
            }
        }

        /* such as: 20150327 */

        public string ProductionDate {
            get {
                string str = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return str;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_ProductionDate(_handle, ptr) >= 0)
                    str = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return str;
            }
        }

        /* such as: 1.3 */

        public string FpgaVersion {
            get {
                string str = "";
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return str;
                IntPtr ptr = Marshal.AllocHGlobal(32);
                if (Altaircam_get_FpgaVersion(_handle, ptr) >= 0)
                    str = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return str;
            }
        }

        public uint Field {
            get {
                if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                    return 0;
                return (uint)Altaircam_get_Field(_handle);
            }
        }

#if !(NETFX_CORE || WINDOWS_UWP)

        public bool StartPullModeWithWndMsg(IntPtr hWnd, uint nMsg) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_StartPullModeWithWndMsg(_handle, hWnd, nMsg) >= 0);
        }

#endif

        public bool StartPullModeWithCallback(DelegateEventCallback edelegate) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            _dEventCallback = edelegate;
            if (edelegate != null) {
                _pEventCallback = new EVENT_CALLBACK(EventCallback);
                return (Altaircam_StartPullModeWithCallback(_handle, _pEventCallback, _id) >= 0);
            } else {
                return (Altaircam_StartPullModeWithCallback(_handle, null, IntPtr.Zero) >= 0);
            }
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray) */

        [HandleProcessCorruptedStateExceptions]
        public bool PullImage(IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }
            return (Altaircam_PullImage(_handle, pImageData, bits, out pnWidth, out pnHeight) >= 0);
        }

        [HandleProcessCorruptedStateExceptions]
        public bool PullImageV2(ushort[] pImageData, int bits, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }
            return (Altaircam_PullImageV2(_handle, pImageData, bits, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray) */

        [HandleProcessCorruptedStateExceptions]
        public bool PullStillImage(IntPtr pImageData, int bits, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }
            return (Altaircam_PullStillImage(_handle, pImageData, bits, out pnWidth, out pnHeight) >= 0);
        }

        [HandleProcessCorruptedStateExceptions]
        public bool PullStillImageV2(IntPtr pImageData, int bits, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }
            return (Altaircam_PullStillImageV2(_handle, pImageData, bits, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray)
            rowPitch: The distance from one row to the next row. rowPitch = 0 means using the default row pitch
        */

        [HandleProcessCorruptedStateExceptions]
        public bool PullImageWithRowPitch(IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }
            return (Altaircam_PullImageWithRowPitch(_handle, pImageData, bits, rowPitch, out pnWidth, out pnHeight) >= 0);
        }

        [HandleProcessCorruptedStateExceptions]
        public bool PullImageWithRowPitchV2(IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }
            return (Altaircam_PullImageWithRowPitchV2(_handle, pImageData, bits, rowPitch, out pInfo) >= 0);
        }

        /*  bits: 24 (RGB24), 32 (RGB32), 8 (Gray) or 16 (Gray)
            rowPitch: The distance from one row to the next row. rowPitch = 0 means using the default row pitch
        */

        [HandleProcessCorruptedStateExceptions]
        public bool PullStillImageWithRowPitch(IntPtr pImageData, int bits, int rowPitch, out uint pnWidth, out uint pnHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pnWidth = pnHeight = 0;
                return false;
            }
            return (Altaircam_PullStillImageWithRowPitch(_handle, pImageData, bits, rowPitch, out pnWidth, out pnHeight) >= 0);
        }

        [HandleProcessCorruptedStateExceptions]
        public bool PullStillImageWithRowPitchV2(IntPtr pImageData, int bits, int rowPitch, out FrameInfoV2 pInfo) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed) {
                pInfo.width = pInfo.height = pInfo.flag = pInfo.seq = 0;
                pInfo.timestamp = 0;
                return false;
            }
            return (Altaircam_PullStillImageWithRowPitchV2(_handle, pImageData, bits, rowPitch, out pInfo) >= 0);
        }

        public bool StartPushModeV3(DelegateDataCallbackV3 ddelegate, DelegateEventCallback edelegate) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            _dDataCallbackV3 = ddelegate;
            _dEventCallback = edelegate;
            _pDataCallbackV3 = new DATA_CALLBACK_V3(DataCallbackV3);
            _pEventCallback = new EVENT_CALLBACK(EventCallback);
            return (Altaircam_StartPushModeV3(_handle, _pDataCallbackV3, _id, _pEventCallback, _id) >= 0);
        }

        public bool Stop() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Stop(_handle) >= 0);
        }

        public bool Pause(bool bPause) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Pause(_handle, bPause ? 1 : 0) >= 0);
        }

        public bool Snap(uint nResolutionIndex) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Snap(_handle, nResolutionIndex) >= 0);
        }

        /* multiple still image snap */

        public bool SnapN(uint nResolutionIndex, uint nNumber) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_SnapN(_handle, nResolutionIndex, nNumber) >= 0);
        }

        /*
            soft trigger:
            nNumber:    0xffff:     trigger continuously
                        0:          cancel trigger
                        others:     number of images to be triggered
        */

        public bool Trigger(ushort nNumber) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Trigger(_handle, nNumber) >= 0);
        }

        public bool put_Size(int nWidth, int nHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Size(_handle, nWidth, nHeight) >= 0);
        }

        public bool get_Size(out int nWidth, out int nHeight) {
            nWidth = nHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Size(_handle, out nWidth, out nHeight) >= 0);
        }

        /*
            put_Size, put_eSize, can be used to set the video output resolution BEFORE Start.
            put_Size use width and height parameters, put_eSize use the index parameter.
            for example, UCMOS03100KPA support the following resolutions:
                index 0:    2048,   1536
                index 1:    1024,   768
                index 2:    680,    510
            so, we can use put_Size(h, 1024, 768) or put_eSize(h, 1). Both have the same effect.
        */

        public bool put_eSize(uint nResolutionIndex) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_eSize(_handle, nResolutionIndex) >= 0);
        }

        public bool get_eSize(out uint nResolutionIndex) {
            nResolutionIndex = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_eSize(_handle, out nResolutionIndex) >= 0);
        }

        /*
            final size after ROI, rotate, binning
        */

        public bool get_FinalSize(out int nWidth, out int nHeight) {
            nWidth = nHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_FinalSize(_handle, out nWidth, out nHeight) >= 0);
        }

        public bool get_Resolution(uint nResolutionIndex, out int pWidth, out int pHeight) {
            pWidth = pHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Resolution(_handle, nResolutionIndex, out pWidth, out pHeight) >= 0);
        }

        /*
            get the sensor pixel size, such as: 2.4um
        */

        public bool get_PixelSize(uint nResolutionIndex, out float x, out float y) {
            x = y = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_PixelSize(_handle, nResolutionIndex, out x, out y) >= 0);
        }

        /*
            numerator/denominator, such as: 1/1, 1/2, 1/3
        */

        public bool get_ResolutionRatio(uint nResolutionIndex, out int pNumerator, out int pDenominator) {
            pNumerator = pDenominator = 1;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ResolutionRatio(_handle, nResolutionIndex, out pNumerator, out pDenominator) >= 0);
        }

        /*
        see: http://www.fourcc.org
        FourCC:
            MAKEFOURCC('G', 'B', 'R', 'G'), see http://www.siliconimaging.com/RGB%20Bayer.htm
            MAKEFOURCC('R', 'G', 'G', 'B')
            MAKEFOURCC('B', 'G', 'G', 'R')
            MAKEFOURCC('G', 'R', 'B', 'G')
            MAKEFOURCC('Y', 'Y', 'Y', 'Y'), monochromatic sensor
            MAKEFOURCC('Y', '4', '1', '1'), yuv411
            MAKEFOURCC('V', 'U', 'Y', 'Y'), yuv422
            MAKEFOURCC('U', 'Y', 'V', 'Y'), yuv422
            MAKEFOURCC('Y', '4', '4', '4'), yuv444
            MAKEFOURCC('R', 'G', 'B', '8'), RGB888
        */

        public bool get_RawFormat(out uint nFourCC, out uint bitdepth) {
            nFourCC = bitdepth = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_RawFormat(_handle, out nFourCC, out bitdepth) >= 0);
        }

        /*
            0: stop grab frame when frame buffer deque is full, until the frames in the queue are pulled away and the queue is not full
            1: realtime
                  use minimum frame buffer. When new frame arrive, drop all the pending frame regardless of whether the frame buffer is full.
                  If DDR present, also limit the DDR frame buffer to only one frame.
            2: soft realtime
                  Drop the oldest frame when the queue is full and then enqueue the new frame
            default: 0
        */

        public bool put_RealTime(int val) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_RealTime(_handle, val) >= 0);
        }

        public bool get_RealTime(out int val) {
            val = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            return (Altaircam_get_RealTime(_handle, out val) >= 0);
        }

        public bool Flush() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_Flush(_handle) >= 0);
        }

        public bool get_AutoExpoEnable(out bool bAutoExposure) {
            bAutoExposure = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEnable = 0;
            if (Altaircam_get_AutoExpoEnable(_handle, out iEnable) < 0)
                return false;

            bAutoExposure = (iEnable != 0);
            return true;
        }

        public bool put_AutoExpoEnable(bool bAutoExposure) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_AutoExpoEnable(_handle, bAutoExposure ? 1 : 0) >= 0);
        }

        public bool get_AutoExpoTarget(out ushort Target) {
            Target = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_AutoExpoTarget(_handle, out Target) >= 0);
        }

        public bool put_AutoExpoTarget(ushort Target) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_AutoExpoTarget(_handle, Target) >= 0);
        }

        public bool put_MaxAutoExpoTimeAGain(uint maxTime, ushort maxAGain) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_MaxAutoExpoTimeAGain(_handle, maxTime, maxAGain) >= 0);
        }

        public bool get_MaxAutoExpoTimeAGain(out uint maxTime, out ushort maxAGain) {
            maxTime = 0;
            maxAGain = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_MaxAutoExpoTimeAGain(_handle, out maxTime, out maxAGain) >= 0);
        }

        public bool put_MinAutoExpoTimeAGain(uint minTime, ushort minAGain) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_MinAutoExpoTimeAGain(_handle, minTime, minAGain) >= 0);
        }

        public bool get_MinAutoExpoTimeAGain(out uint minTime, out ushort minAGain) {
            minTime = 0;
            minAGain = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_MinAutoExpoTimeAGain(_handle, out minTime, out minAGain) >= 0);
        }

        public bool get_ExpoTime(out uint Time)/* in microseconds */
        {
            Time = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoTime(_handle, out Time) >= 0);
        }

        public bool put_ExpoTime(uint Time)/* in microseconds */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ExpoTime(_handle, Time) >= 0);
        }

        public bool get_ExpTimeRange(out uint nMin, out uint nMax, out uint nDef) {
            nMin = nMax = nDef = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpTimeRange(_handle, out nMin, out nMax, out nDef) >= 0);
        }

        public bool get_ExpoAGain(out ushort AGain)/* percent, such as 300 */
        {
            AGain = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoAGain(_handle, out AGain) >= 0);
        }

        public bool put_ExpoAGain(ushort AGain)/* percent */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ExpoAGain(_handle, AGain) >= 0);
        }

        public bool get_ExpoAGainRange(out ushort nMin, out ushort nMax, out ushort nDef) {
            nMin = nMax = nDef = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_ExpoAGainRange(_handle, out nMin, out nMax, out nDef) >= 0);
        }

        public bool put_LevelRange(ushort[] aLow, ushort[] aHigh) {
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_LevelRange(_handle, aLow, aHigh) >= 0);
        }

        public bool get_LevelRange(ushort[] aLow, ushort[] aHigh) {
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_LevelRange(_handle, aLow, aHigh) >= 0);
        }

        public bool put_LevelRangeV2(ushort mode, int roiX, int roiY, int roiWidth, int roiHeight, ushort[] aLow, ushort[] aHigh) {
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            RECT rc = new RECT();
            rc.left = roiX;
            rc.right = roiX + roiWidth;
            rc.top = roiY;
            rc.bottom = roiY + roiHeight;
            return (Altaircam_put_LevelRangeV2(_handle, mode, ref rc, aLow, aHigh) >= 0);
        }

        public bool get_LevelRangeV2(out ushort mode, out int roiX, out int roiY, out int roiWidth, out int roiHeight, ushort[] aLow, ushort[] aHigh) {
            mode = 0;
            roiX = roiY = roiWidth = roiHeight = 0;
            if (aLow.Length != 4 || aHigh.Length != 4)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            RECT rc = new RECT();
            if (Altaircam_get_LevelRangeV2(_handle, out mode, out rc, aLow, aHigh) < 0)
                return false;
            roiX = rc.left;
            roiY = rc.top;
            roiWidth = rc.right - rc.left;
            roiHeight = rc.bottom - rc.top;
            return true;
        }

        public bool put_Hue(int Hue) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Hue(_handle, Hue) >= 0);
        }

        public bool get_Hue(out int Hue) {
            Hue = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Hue(_handle, out Hue) >= 0);
        }

        public bool put_Saturation(int Saturation) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Saturation(_handle, Saturation) >= 0);
        }

        public bool get_Saturation(out int Saturation) {
            Saturation = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Saturation(_handle, out Saturation) >= 0);
        }

        public bool put_Brightness(int Brightness) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Brightness(_handle, Brightness) >= 0);
        }

        public bool get_Brightness(out int Brightness) {
            Brightness = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Brightness(_handle, out Brightness) >= 0);
        }

        public bool get_Contrast(out int Contrast) {
            Contrast = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Contrast(_handle, out Contrast) >= 0);
        }

        public bool put_Contrast(int Contrast) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Contrast(_handle, Contrast) >= 0);
        }

        public bool get_Gamma(out int Gamma) {
            Gamma = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Gamma(_handle, out Gamma) >= 0);
        }

        public bool put_Gamma(int Gamma) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Gamma(_handle, Gamma) >= 0);
        }

        public bool get_Chrome(out bool bChrome)    /* monochromatic mode */
        {
            bChrome = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEnable = 0;
            if (Altaircam_get_Chrome(_handle, out iEnable) < 0)
                return false;

            bChrome = (iEnable != 0);
            return true;
        }

        public bool put_Chrome(bool bChrome) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Chrome(_handle, bChrome ? 1 : 0) >= 0);
        }

        public bool get_VFlip(out bool bVFlip) /* vertical flip */
        {
            bVFlip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iVFlip = 0;
            if (Altaircam_get_VFlip(_handle, out iVFlip) < 0)
                return false;

            bVFlip = (iVFlip != 0);
            return true;
        }

        public bool put_VFlip(bool bVFlip) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VFlip(_handle, bVFlip ? 1 : 0) >= 0);
        }

        public bool get_HFlip(out bool bHFlip) {
            bHFlip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iHFlip = 0;
            if (Altaircam_get_HFlip(_handle, out iHFlip) < 0)
                return false;

            bHFlip = (iHFlip != 0);
            return true;
        }

        public bool put_HFlip(bool bHFlip)  /* horizontal flip */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_HFlip(_handle, bHFlip ? 1 : 0) >= 0);
        }

        /* negative film */

        public bool get_Negative(out bool bNegative) {
            bNegative = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iNegative = 0;
            if (Altaircam_get_Negative(_handle, out iNegative) < 0)
                return false;

            bNegative = (iNegative != 0);
            return true;
        }

        /* negative film */

        public bool put_Negative(bool bNegative) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Negative(_handle, bNegative ? 1 : 0) >= 0);
        }

        public bool put_Speed(ushort nSpeed) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Speed(_handle, nSpeed) >= 0);
        }

        public bool get_Speed(out ushort pSpeed) {
            pSpeed = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Speed(_handle, out pSpeed) >= 0);
        }

        /* power supply:
                0 -> 60HZ AC
                1 -> 50Hz AC
                2 -> DC
        */

        public bool put_HZ(int nHZ) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_HZ(_handle, nHZ) >= 0);
        }

        public bool get_HZ(out int nHZ) {
            nHZ = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_HZ(_handle, out nHZ) >= 0);
        }

        public bool put_Mode(bool bSkip) /* skip or bin */
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Mode(_handle, bSkip ? 1 : 0) >= 0);
        }

        public bool get_Mode(out bool bSkip) {
            bSkip = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iSkip = 0;
            if (Altaircam_get_Mode(_handle, out iSkip) < 0)
                return false;

            bSkip = (iSkip != 0);
            return true;
        }

        /* White Balance, Temp/Tint mode */

        public bool put_TempTint(int nTemp, int nTint) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_TempTint(_handle, nTemp, nTint) >= 0);
        }

        /* White Balance, Temp/Tint mode */

        public bool get_TempTint(out int nTemp, out int nTint) {
            nTemp = nTint = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_TempTint(_handle, out nTemp, out nTint) >= 0);
        }

        /* White Balance, RGB Gain Mode */

        public bool put_WhiteBalanceGain(int[] aGain) {
            if (aGain.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_WhiteBalanceGain(_handle, aGain) >= 0);
        }

        /* White Balance, RGB Gain Mode */

        public bool get_WhiteBalanceGain(int[] aGain) {
            if (aGain.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_WhiteBalanceGain(_handle, aGain) >= 0);
        }

        public bool put_AWBAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_AWBAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_AWBAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_AWBAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool put_AEAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_AEAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_AEAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_AEAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool put_BlackBalance(ushort[] aSub) {
            if (aSub.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_BlackBalance(_handle, aSub) >= 0);
        }

        public bool get_BlackBalance(ushort[] aSub) {
            if (aSub.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_BlackBalance(_handle, aSub) >= 0);
        }

        public bool put_ABBAuxRect(int X, int Y, int Width, int Height) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            rc.left = X;
            rc.right = X + Width;
            rc.top = Y;
            rc.bottom = Y + Height;
            return (Altaircam_put_ABBAuxRect(_handle, ref rc) >= 0);
        }

        public bool get_ABBAuxRect(out int X, out int Y, out int Width, out int Height) {
            X = Y = Width = Height = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            RECT rc = new RECT();
            if (Altaircam_get_ABBAuxRect(_handle, out rc) < 0)
                return false;

            X = rc.left;
            Y = rc.top;
            Width = rc.right - rc.left;
            Height = rc.bottom - rc.top;
            return true;
        }

        public bool get_StillResolution(uint nResolutionIndex, out int pWidth, out int pHeight) {
            pWidth = pHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_StillResolution(_handle, nResolutionIndex, out pWidth, out pHeight) >= 0);
        }

        public bool put_VignetEnable(bool bEnable) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetEnable(_handle, bEnable ? 1 : 0) >= 0);
        }

        public bool get_VignetEnable(out bool bEnable) {
            bEnable = false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;

            int iEanble = 0;
            if (Altaircam_get_VignetEnable(_handle, out iEanble) < 0)
                return false;

            bEnable = (iEanble != 0);
            return true;
        }

        public bool put_VignetAmountInt(int nAmount) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetAmountInt(_handle, nAmount) >= 0);
        }

        public bool get_VignetAmountInt(out int nAmount) {
            nAmount = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_VignetAmountInt(_handle, out nAmount) >= 0);
        }

        public bool put_VignetMidPointInt(int nMidPoint) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_VignetMidPointInt(_handle, nMidPoint) >= 0);
        }

        public bool get_VignetMidPointInt(out int nMidPoint) {
            nMidPoint = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_VignetMidPointInt(_handle, out nMidPoint) >= 0);
        }

        /* led state:
            iLed: Led index, (0, 1, 2, ...)
            iState: 1 -> Ever bright; 2 -> Flashing; other -> Off
            iPeriod: Flashing Period (>= 500ms)
        */

        public bool put_LEDState(ushort iLed, ushort iState, ushort iPeriod) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_LEDState(_handle, iLed, iState, iPeriod) >= 0);
        }

        public int write_EEPROM(uint addr, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_EEPROM(_handle, addr, pBuffer, nBufferLen);
        }

        public int read_EEPROM(uint addr, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_EEPROM(_handle, addr, pBuffer, nBufferLen);
        }

        public int write_Pipe(uint pipeNum, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_Pipe(_handle, pipeNum, pBuffer, nBufferLen);
        }

        public int read_Pipe(uint pipeNum, IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_Pipe(_handle, pipeNum, pBuffer, nBufferLen);
        }

        public int feed_Pipe(uint pipeNum) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_feed_Pipe(_handle, pipeNum);
        }

        public int write_UART(IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_write_UART(_handle, pBuffer, nBufferLen);
        }

        public int read_UART(IntPtr pBuffer, uint nBufferLen) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return 0;
            return Altaircam_read_UART(_handle, pBuffer, nBufferLen);
        }

        public bool put_Option(eOPTION iOption, int iValue) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Option(_handle, iOption, iValue) >= 0);
        }

        public bool get_Option(eOPTION iOption, out int iValue) {
            iValue = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Option(_handle, iOption, out iValue) >= 0);
        }

        public bool put_Linear(byte[] v8, ushort[] v16) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Linear(_handle, v8, v16) >= 0);
        }

        public bool put_Curve(byte[] v8, ushort[] v16) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Curve(_handle, v8, v16) >= 0);
        }

        public bool put_ColorMatrix(double[] v) {
            if (v.Length != 9)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_ColorMatrix(_handle, v) >= 0);
        }

        public bool put_InitWBGain(ushort[] v) {
            if (v.Length != 3)
                return false;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_InitWBGain(_handle, v) >= 0);
        }

        /* get the temperature of the sensor, in 0.1 degrees Celsius (32 means 3.2 degrees Celsius, -35 means -3.5 degree Celsius) */

        public bool get_Temperature(out short pTemperature) {
            pTemperature = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Temperature(_handle, out pTemperature) == 0);
        }

        /* set the target temperature of the sensor or TEC, in 0.1 degrees Celsius (32 means 3.2 degrees Celsius, -35 means -3.5 degree Celsius) */

        public bool put_Temperature(short nTemperature) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Temperature(_handle, nTemperature) == 0);
        }

        /* xOffset, yOffset, xWidth, yHeight: must be even numbers */

        public bool put_Roi(uint xOffset, uint yOffset, uint xWidth, uint yHeight) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_put_Roi(_handle, xOffset, yOffset, xWidth, yHeight) >= 0);
        }

        public bool get_Roi(out uint pxOffset, out uint pyOffset, out uint pxWidth, out uint pyHeight) {
            pxOffset = pyOffset = pxWidth = pyHeight = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_Roi(_handle, out pxOffset, out pyOffset, out pxWidth, out pyHeight) >= 0);
        }

        /*
            get the frame rate: framerate (fps) = Frame * 1000.0 / nTime
        */

        public bool get_FrameRate(out uint nFrame, out uint nTime, out uint nTotalFrame) {
            nFrame = nTime = nTotalFrame = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_FrameRate(_handle, out nFrame, out nTime, out nTotalFrame) >= 0);
        }

        public bool LevelRangeAuto() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_LevelRangeAuto(_handle) >= 0);
        }

        /* Auto White Balance "Once", Temp/Tint Mode */

        public bool AwbOnce() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_AwbOnce(_handle, IntPtr.Zero, IntPtr.Zero) >= 0);
        }

        [Obsolete("Use AwbOnce")]
        public bool AwbOnePush() {
            return AwbOnce();
        }

        /* Auto White Balance, RGB Gain Mode */

        public bool AwbInit() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_AwbOnce(_handle, IntPtr.Zero, IntPtr.Zero) >= 0);
        }

        public bool AbbOnce() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_AbbOnce(_handle, IntPtr.Zero, IntPtr.Zero) >= 0);
        }

        [Obsolete("Use AbbOnce")]
        public bool AbbOnePush() {
            return AbbOnce();
        }

        public bool FfcOnce() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_FfcOnce(_handle) >= 0);
        }

        [Obsolete("Use FfcOnce")]
        public bool FfcOnePush() {
            return FfcOnce();
        }

        public bool DfcOnce() {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_DfcOnce(_handle) >= 0);
        }

        [Obsolete("Use DfcOnce")]
        public bool DfcOnePush() {
            return DfcOnce();
        }

        public bool FfcExport(string filepath) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_FfcExport(_handle, filepath) >= 0);
        }

        public bool FfcImport(string filepath) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_FfcImport(_handle, filepath) >= 0);
        }

        public bool DfcExport(string filepath) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_DfcExport(_handle, filepath) >= 0);
        }

        public bool DfcImport(string filepath) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_DfcImport(_handle, filepath) >= 0);
        }

        public bool IoControl(uint index, eIoControType eType, int outVal, out int inVal) {
            inVal = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_IoControl(_handle, index, eType, outVal, out inVal) >= 0);
        }

        public bool get_AfParam(out AfParam pAfParam) {
            pAfParam.idef = pAfParam.imax = pAfParam.imin = pAfParam.imaxabs = pAfParam.iminabs = pAfParam.zoneh = pAfParam.zonev = 0;
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            return (Altaircam_get_AfParam(_handle, out pAfParam) >= 0);
        }

        public bool GetHistogram(DelegateHistogramCallback fnHistogramProc) {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                return false;
            _dHistogramCallback = fnHistogramProc;
            _pHistogramCallback = new HISTOGRAM_CALLBACK(HistogramCallback);
            return (Altaircam_GetHistogram(_handle, _pHistogramCallback, _id) >= 0);
        }

        /*
            calculate the clarity factor:
            pImageData: pointer to the image data
            bits: 8(Grey), 24 (RGB24), 32(RGB32)
            nImgWidth, nImgHeight: the image width and height
        */

        public static double calcClarityFactor(IntPtr pImageData, int bits, uint nImgWidth, uint nImgHeight) {
            return Altaircam_calc_ClarityFactor(pImageData, bits, nImgWidth, nImgHeight);
        }

        /*
            nBitCount: output bitmap bit count
            when nBitDepth == 8:
                nBitCount must be 24 or 32
            when nBitDepth > 8
                nBitCount:  24 -> RGB24
                            32 -> RGB32
                            48 -> RGB48
                            64 -> RGB64
        */

        public static void deBayerV2(uint nBayer, int nW, int nH, IntPtr input, IntPtr output, byte nBitDepth, byte nBitCount) {
            Altaircam_deBayerV2(nBayer, nW, nH, input, output, nBitDepth, nBitCount);
        }

        /*
            simulate replug:
            return > 0, the number of device has been replug
            return = 0, no device found
            return E_ACCESSDENIED if without UAC Administrator privileges
            for each device found, it will take about 3 seconds
        */

        public static int Replug(string id) {
            return Altaircam_Replug(id);
        }
    }
}