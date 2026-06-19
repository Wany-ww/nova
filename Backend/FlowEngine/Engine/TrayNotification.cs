using System;
using System.Runtime.InteropServices;

namespace FlowEngine.Engine
{
    /// <summary>
    /// Provides methods to display Windows system tray balloon notifications using native Win32 APIs.
    /// </summary>
    public static class TrayNotification
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_INFO = 0x00000010;

        private const int NIIF_INFO = 0x00000001;
        private const int NIIF_WARNING = 0x00000002;
        private const int NIIF_ERROR = 0x00000003;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, [In] ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        private static bool _iconAdded = false;
        private static readonly object _lock = new object();
        private const int IconId = 1002;

        /// <summary>
        /// Displays a balloon tip notification from the system tray.
        /// </summary>
        public static void Show(string title, string message, string type)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var mainWin = System.Windows.Application.Current.MainWindow;
                    if (mainWin == null) return;

                    var wih = new System.Windows.Interop.WindowInteropHelper(mainWin);
                    IntPtr hWnd = wih.Handle;
                    if (hWnd == IntPtr.Zero) return;

                    int infoFlags = NIIF_INFO;
                    if (type.Equals("warning", StringComparison.OrdinalIgnoreCase)) infoFlags = NIIF_WARNING;
                    else if (type.Equals("error", StringComparison.OrdinalIgnoreCase)) infoFlags = NIIF_ERROR;

                    lock (_lock)
                    {
                        var nid = new NOTIFYICONDATA();
                        nid.cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
                        nid.hWnd = hWnd;
                        nid.uID = IconId;
                        nid.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;
                        nid.szTip = "NOVA Engine";
                        nid.hIcon = GetDefaultIconHandle();

                        if (!_iconAdded)
                        {
                            if (Shell_NotifyIcon(NIM_ADD, ref nid))
                            {
                                _iconAdded = true;
                            }
                        }

                        // Now show balloon
                        var nidModify = new NOTIFYICONDATA();
                        nidModify.cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
                        nidModify.hWnd = hWnd;
                        nidModify.uID = IconId;
                        nidModify.uFlags = NIF_INFO;
                        nidModify.szInfo = message;
                        nidModify.szInfoTitle = title;
                        nidModify.dwInfoFlags = infoFlags;
                        nidModify.uTimeoutOrVersion = 5000;

                        Shell_NotifyIcon(NIM_MODIFY, ref nidModify);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Notify Error: " + ex.Message);
                    // throw;
                }
            });
        }

        /// <summary>
        /// Cleans up and deletes the tray icon from the system taskbar.
        /// </summary>
        public static void Cleanup()
        {
            lock (_lock)
            {
                if (_iconAdded)
                {
                    try
                    {
                        var mainWin = System.Windows.Application.Current.MainWindow;
                        if (mainWin != null)
                        {
                            var wih = new System.Windows.Interop.WindowInteropHelper(mainWin);
                            IntPtr hWnd = wih.Handle;
                            if (hWnd != IntPtr.Zero)
                            {
                                var nid = new NOTIFYICONDATA();
                                nid.cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA));
                                nid.hWnd = hWnd;
                                nid.uID = IconId;
                                Shell_NotifyIcon(NIM_DELETE, ref nid);
                            }
                        }
                    }
                    catch
                    {
                        // throw;
                    }
                    // lock (_lock)
                    _iconAdded = false;
                }
            }
        }

        private static IntPtr GetDefaultIconHandle()
        {
            try
            {
                // IDI_APPLICATION = 32512
                return LoadIcon(IntPtr.Zero, (IntPtr)32512);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    }
}
