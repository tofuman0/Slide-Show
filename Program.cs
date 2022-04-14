using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using System.Diagnostics;

namespace Slide_Show
{
    class Program
    {
        static private bool EventlogSourceExists;
        static private readonly Int32 SS_OK           = 0;
        static private readonly Int32 SS_ERROR        = 1 << 31;
        static private readonly Int32 SS_WARNING      = 3 << 30;
        static private readonly Int32 SS_WALLPAPER    = 1 << 0;
        static private readonly Int32 SS_LOCKSCREEN   = 1 << 1;
        static private readonly Int32 SS_ACCESS       = 1 << 2;
        static private readonly Int32 SS_FOLDER       = 1 << 3;
        static private readonly Int32 SS_FILE         = 1 << 4;
        static private readonly Int32 SS_REGISTRY     = 1 << 5;
        static int Main(string[] args)
        {
            EventlogSourceExists = CheckOrCreateEventLog();
            if (EventlogSourceExists == false)
                WriteEventLog("Unabled to create \"Slide Show\" Eventlog source. User requires write access to eventlog to create source. \"Application\" will be used instead.", EventlogSourceExists, (UInt16)(SS_WARNING | SS_REGISTRY), EventLogEntryType.Warning);

            // Check if process is already running
            // Vercas - https://stackoverflow.com/a/6392077
            var exists = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;
            if(exists)
            {
                WriteEventLog("Slide Show is already running so has closed", EventlogSourceExists, (UInt16)(SS_ERROR));
                return SS_ERROR;
            }

            String SlideShowPath = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "SlideShowPath", @"C:\Windows\Wallpaper\SlideShow").ToString();
            if (SlideShowPath == "")
                SlideShowPath = @"C:\Windows\Wallpaper\SlideShow";
            UInt32 SlideShowTick = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "SlideShowTicks", 1800000));
            if (SlideShowTick == 0)
                SlideShowTick = 1800000;
            UInt32 SlideShowOptions = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "SlideShowShuffle", DesktopSlideshowDirection.NoShuffle));
            if (SlideShowOptions != 0 && SlideShowOptions != 1)
                SlideShowOptions = 0;
            UInt32 SlideShowPosition = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "SlideShowPosition", DesktopWallpaperPosition.Stretch));
            if (SlideShowPosition > 5)
                SlideShowPosition = 0;
            bool SlideShowEnable = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "SlideShowEnable", 0)) == 1 ? true : false;
            String LockScreenSlideShowPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lock Screen", "LockScreenSlideShowPath", @"C:\Windows\Wallpaper\Lockscreen").ToString();
            if (LockScreenSlideShowPath == "")
                LockScreenSlideShowPath = @"C:\Windows\Wallpaper\Lockscreen";
            UInt32 LockScreenSlideShowTick = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lock Screen", "LockScreenSlideShowTicks", 15000));
            if (LockScreenSlideShowTick == 0)
                LockScreenSlideShowTick = 15000;
            bool LockScreenSlideShowEnable = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lock Screen", "LockScreenSlideShowEnable", 0)) == 1 ? true : false;
            bool LockScreenSlideShowShuffle = Convert.ToUInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lock Screen", "LockScreenSlideShowShuffle", 0)) == 1 ? true : false;

            try
            {
                Int32 res = 0;
                if (SlideShowEnable)
                {
                    res = Wallpaper(SlideShowPath, SlideShowTick, SlideShowOptions, (DesktopWallpaperPosition)SlideShowPosition);
                }

                if (LockScreenSlideShowEnable)
                {
                    LockScreen(LockScreenSlideShowPath, LockScreenSlideShowTick, LockScreenSlideShowShuffle).Wait();
                }
                if (res == SS_OK)
                    WriteEventLog("Slide Show exited without error.", EventlogSourceExists, (UInt16)(SS_OK));
                return res;
            }
            catch (Exception ex)
            {
                WriteEventLog("Slide Show exited with error:\r\n\r\n" + ex.Message, EventlogSourceExists, (UInt16)(SS_ERROR | SS_WALLPAPER | SS_LOCKSCREEN), EventLogEntryType.Error);
                return SS_ERROR | SS_WALLPAPER | SS_LOCKSCREEN;
            }
        }

        static Int32 Wallpaper(String SlideShowPath, UInt32 SlideShowTick, UInt32 SlideShowOptions, DesktopWallpaperPosition SlideShowPosition)
        {
            try
            {
                IShellItem pShellItem = null;
                IShellItemArray pShellItemArray = null;
                if (SHCreateItemFromParsingName(SlideShowPath, IntPtr.Zero, typeof(IShellItem).GUID, out pShellItem) == HRESULT.S_OK)
                {
                    if (SHCreateShellItemArrayFromShellItem(pShellItem, typeof(IShellItemArray).GUID, out pShellItemArray) == HRESULT.S_OK)
                    {
                        IDesktopWallpaper pDesktopWallpaper = (IDesktopWallpaper)(new DesktopWallpaperClass());
                        pDesktopWallpaper.SetSlideshowOptions(
                            (SlideShowOptions == 0) ? DesktopSlideshowDirection.NoShuffle : DesktopSlideshowDirection.Shuffle,
                            SlideShowTick
                        );
                        pDesktopWallpaper.SetPosition(SlideShowPosition);
                        pDesktopWallpaper.SetSlideshow(pShellItemArray);
                    }
                    return SS_OK;
                }
                else
                {
                    WriteEventLog("Configured wallpaper path: \"" + SlideShowPath + "\" not found.\r\n\r\n", EventlogSourceExists, (UInt16)(SS_WARNING | SS_FOLDER | SS_WALLPAPER), EventLogEntryType.Warning);
                    return SS_ERROR | SS_FOLDER | SS_WALLPAPER;
                }
            }
            catch (Exception ex)
            {
                WriteEventLog("Error setting wallpaper:\r\n\r\n" + ex.Message, EventlogSourceExists, (UInt16)(SS_ERROR | SS_WALLPAPER), EventLogEntryType.Error);
                return SS_ERROR | SS_WALLPAPER;
            }
        }
        static async Task LockScreen(String path, UInt32 tick, bool LockScreenSlideShowShuffle)
        {
            try
            {
                var images = Directory.GetFiles(path, "*.*").Where(file => file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("jpeg") || file.ToLower().EndsWith("png") || file.ToLower().EndsWith("bmp")).ToList();
                Int32 imageOffset = 0;
                if (images.Count == 1)
                {
                    var lockscreenImage = await StorageFile.GetFileFromPathAsync(images[0]);
                    await Windows.System.UserProfile.LockScreen.SetImageFileAsync(lockscreenImage);
                }
                else if (images.Count > 0)
                {
                    if(LockScreenSlideShowShuffle)
                    {
                        var rand = new Random();
                        images = images.OrderBy(x => rand.Next()).ToList();
                    }
                    while (true)
                    {
                        var lockscreenImage = await StorageFile.GetFileFromPathAsync(images[imageOffset++]);
                        await Windows.System.UserProfile.LockScreen.SetImageFileAsync(lockscreenImage);
                        if (imageOffset >= images.Count)
                            imageOffset = 0;
                        await Task.Delay((Int32)tick);
                    }
                }
                else
                    WriteEventLog("Configured lockscreen path is empty: " + path, EventlogSourceExists, (UInt16)(SS_WARNING | SS_FOLDER | SS_LOCKSCREEN), EventLogEntryType.Warning);
            }
            catch (Exception ex)
            {
                WriteEventLog("Error setting lockscreen:\r\n\r\n" + ex.Message, EventlogSourceExists, (UInt16)(SS_ERROR | SS_LOCKSCREEN), EventLogEntryType.Error);
            }
        }

        static void WriteEventLog(String message, bool EventlogSourceExists, UInt16 errorcode = 0, EventLogEntryType type = EventLogEntryType.Information)
        {
            EventLog appLog = new EventLog("Application");
            if (EventlogSourceExists)
                appLog.Source = "Slide Show";
            else
                appLog.Source = "Application";
            appLog.WriteEntry(message, type, errorcode, 0);
        }

        static bool CheckOrCreateEventLog()
        {
            // Admin permissions are required to create source so if we can't create source we'll use "Application" instead.
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists("Slide Show"))
                {
                    System.Diagnostics.EventLog.CreateEventSource("Slide Show", "Application");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public enum HRESULT : int
        {
            S_OK = 0,
            S_FALSE = 1,
            E_NOINTERFACE = unchecked((int)0x80004002),
            E_NOTIMPL = unchecked((int)0x80004001),
            E_FAIL = unchecked((int)0x80004005)
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern HRESULT SHCreateItemFromParsingName(string pszPath, IntPtr pbc, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);


        [DllImport("Shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern HRESULT SHCreateShellItemArrayFromShellItem(IShellItem psi, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemArray ppv);

        public enum GETPROPERTYSTOREFLAGS
        {
            GPS_DEFAULT = 0,
            GPS_HANDLERPROPERTIESONLY = 0x1,
            GPS_READWRITE = 0x2,
            GPS_TEMPORARY = 0x4,
            GPS_FASTPROPERTIESONLY = 0x8,
            GPS_OPENSLOWITEM = 0x10,
            GPS_DELAYCREATION = 0x20,
            GPS_BESTEFFORT = 0x40,
            GPS_NO_OPLOCK = 0x80,
            GPS_PREFERQUERYPROPERTIES = 0x100,
            GPS_EXTRINSICPROPERTIES = 0x200,
            GPS_EXTRINSICPROPERTIESONLY = 0x400,
            GPS_MASK_VALID = 0x7FF
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct REFPROPERTYKEY
        {
            private Guid fmtid;
            private int pid;
            public Guid FormatId
            {
                get
                {
                    return this.fmtid;
                }
            }
            public int PropertyId
            {
                get
                {
                    return this.pid;
                }
            }
            public REFPROPERTYKEY(Guid formatId, int propertyId)
            {
                this.fmtid = formatId;
                this.pid = propertyId;
            }
            public static readonly REFPROPERTYKEY PKEY_DateCreated = new REFPROPERTYKEY(new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC"), 15);
        }

        public enum SIATTRIBFLAGS
        {
            SIATTRIBFLAGS_AND = 0x1,
            SIATTRIBFLAGS_OR = 0x2,
            SIATTRIBFLAGS_APPCOMPAT = 0x3,
            SIATTRIBFLAGS_MASK = 0x3,
            SIATTRIBFLAGS_ALLITEMS = 0x4000
        }

        [ComImport()]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
        public interface IShellItemArray
        {
            HRESULT BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, ref IntPtr ppvOut);
            HRESULT GetPropertyStore(GETPROPERTYSTOREFLAGS flags, ref Guid riid, ref IntPtr ppv);
            HRESULT GetPropertyDescriptionList(REFPROPERTYKEY keyType, ref Guid riid, ref IntPtr ppv);
            HRESULT GetAttributes(SIATTRIBFLAGS AttribFlags, int sfgaoMask, ref int psfgaoAttribs);
            HRESULT GetCount(ref int pdwNumItems);
            HRESULT GetItemAt(int dwIndex, ref IShellItem ppsi);
            HRESULT EnumItems(ref IntPtr ppenumShellItems);

        }

        [ComImport()]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        public interface IShellItem
        {
            [PreserveSig()]
            HRESULT BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, ref IntPtr ppv);
            HRESULT GetParent(ref IShellItem ppsi);
            HRESULT GetDisplayName(SIGDN sigdnName, ref System.Text.StringBuilder ppszName);
            HRESULT GetAttributes(uint sfgaoMask, ref uint psfgaoAttribs);
            HRESULT Compare(IShellItem psi, uint hint, ref int piOrder);
        }
        public enum SIGDN : int
        {
            SIGDN_NORMALDISPLAY = 0x0,
            SIGDN_PARENTRELATIVEPARSING = unchecked((int)0x80018001),
            SIGDN_DESKTOPABSOLUTEPARSING = unchecked((int)0x80028000),
            SIGDN_PARENTRELATIVEEDITING = unchecked((int)0x80031001),
            SIGDN_DESKTOPABSOLUTEEDITING = unchecked((int)0x8004C000),
            SIGDN_FILESYSPATH = unchecked((int)0x80058000),
            SIGDN_URL = unchecked((int)0x80068000),
            SIGDN_PARENTRELATIVEFORADDRESSBAR = unchecked((int)0x8007C001),
            SIGDN_PARENTRELATIVE = unchecked((int)0x80080001)
        }

        /// <summary>
        ///     This enumeration is used to set and get slideshow options.
        /// </summary>
        public enum DesktopSlideshowOptions
        {
            ShuffleImages = 0x01,
            // When set, indicates that the order in which images in the slideshow are displayed can be randomized.
        }

        /// <summary>
        ///     This enumeration is used by GetStatus to indicate the current status of the slideshow.
        /// </summary>
        public enum DesktopSlideshowState
        {
            Enabled = 0x01,
            Slideshow = 0x02,
            DisabledByRemoteSession = 0x04,
        }

        /// <summary>
        ///     This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slideshow forward or
        ///     backward.
        /// </summary>
        public enum DesktopSlideshowDirection
        {
            NoShuffle = 0,
            Shuffle = 1,
        }

        /// <summary>
        ///     This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
        ///     The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
        /// </summary>
        public enum DesktopWallpaperPosition
        {
            Center = 0,
            Tile = 1,
            Stretch = 2,
            Fit = 3,
            Fill = 4,
            Span = 5,
        }

        [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                              [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            /// <summary>
            ///     Gets the monitor device path.
            /// </summary>
            /// <param name="monitorIndex">Index of the monitor device in the monitor device list.</param>
            /// <returns></returns>
            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetMonitorDevicePathAt(uint monitorIndex);

            /// <summary>
            ///     Gets number of monitor device paths.
            /// </summary>
            /// <returns></returns>
            [return: MarshalAs(UnmanagedType.U4)]
            uint GetMonitorDevicePathCount();

            [return: MarshalAs(UnmanagedType.Struct)]
            Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);

            [return: MarshalAs(UnmanagedType.U4)]
            uint GetBackgroundColor();

            void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);

            [return: MarshalAs(UnmanagedType.I4)]
            DesktopWallpaperPosition GetPosition();

            void SetSlideshow(IShellItemArray items);
            IntPtr GetSlideshow();

            void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);

            [PreserveSig]
            uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

            void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                                  [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

            DesktopSlideshowDirection GetStatus();

            bool Enable();

        }

        /// <summary>
        ///     CoClass DesktopWallpaper
        /// </summary>
        [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
        public class DesktopWallpaperClass
        {
        }
    }
}


