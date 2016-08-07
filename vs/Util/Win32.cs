using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace FitWin {

    static class Win32 {

        delegate bool wep(IntPtr hw, int lp);

        [DllImport("user32")]
        static extern bool AttachThreadInput(int ida, int idat, bool a);
        [DllImport("user32")]
        static extern bool EnumWindows(wep ewp, int lp);
        [DllImport("user32")]
        static extern bool DestroyIcon(IntPtr hi);
        [DllImport("user32")]
        static extern IntPtr GetClassLong(IntPtr hw, int i);
        [DllImport("user32")]
        static extern int GetClassName(IntPtr hw, StringBuilder s, int mx);
        [DllImport("user32")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32")]
        static extern IntPtr GetWindow(IntPtr hw, int c);
        [DllImport("user32")]
        static extern int GetWindowLong(IntPtr hw, int i);
        [DllImport("user32")]
        static extern bool GetWindowPlacement(IntPtr hw, out WINDOWPLACEMENT wp);
        [DllImport("user32")]
        static extern bool GetWindowRect(IntPtr hw, out RECT r);
        [DllImport("user32")]
        static extern int GetWindowText(IntPtr hw, StringBuilder s, int mx);
        [DllImport("user32")]
        static extern int GetWindowThreadProcessId(IntPtr hw, out int pid);
        [DllImport("user32")]
        static extern bool IsWindowVisible(IntPtr hw);
        [DllImport("user32")]
        static extern IntPtr SendMessage(IntPtr hw, int m, int wp, int lp);
        [DllImport("user32")]
        static extern int SetWindowLong(IntPtr hw, int i, int l);
        [DllImport("user32")]
        static extern bool SetWindowPos(IntPtr hw, IntPtr hwia, int x, int y, int w, int h, int f);
        [DllImport("user32")]
        static extern bool SetWindowPlacement(IntPtr hw, ref WINDOWPLACEMENT wp);
        [DllImport("user32")]
        static extern bool ShowWindow(IntPtr hw, int c);

        [DllImport("user32")]
        public static extern bool IsIconic(IntPtr hw);
        [DllImport("user32")]
        public static extern bool IsZoomed(IntPtr hw);
        [DllImport("user32")]
        public static extern bool SetForegroundWindow(IntPtr hw);

        const int GCLP_HICON = -14, GW_OWNER = 4,
            GWL_EXSTYLE = -20, GWL_STYLE = -16, ICON_BIG = 1,
            SW_RESTORE = 9, SW_SHOW = 5,
            SW_SHOWMAXIMIZED = 3, SW_SHOWMINIMIZED = 2,
            SWP_NOACTIVATE = 0x10, SWP_NOMOVE = 2, SWP_NOSIZE = 1,
            WM_CLOSE = 0x10, WM_GETICON = 0x7f,
            WS_EX_TOOLWINDOW = 0x80, WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZEBOX = 0x20000, WS_SIZEBOX = 0x40000;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT {
            public int Length, Flags, ShowCmd;
            public Point MinPosition, MaxPosition;
            public RECT NormalPosition;

            public static WINDOWPLACEMENT Default {
                get {
                    return new WINDOWPLACEMENT {
                        Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT))
                    };
                }
            }
        }

        public class Placement {

            readonly IntPtr hw;
            readonly Rectangle r;
            WINDOWPLACEMENT wp;

            public Placement(IntPtr hw) {
                this.hw = hw;
                r = GetBounds(hw);
                wp = WINDOWPLACEMENT.Default;
                GetWindowPlacement(hw, out wp);
            }

            public void Restore() {
                SetWindowPlacement(hw, ref wp);
                SetWindowPos(hw, IntPtr.Zero, r.X, r.Y,
                    r.Width, r.Height, SWP_NOACTIVATE);
            }
        }

        public static void Close(IntPtr hw) {
            SendMessage(hw, WM_CLOSE, 0, 0);
        }

        public static void First(IntPtr hw) {
            ShowWindow(hw, IsIconic(hw) ? SW_RESTORE : SW_SHOW);
            SetForegroundWindow(hw);
        }

        public static void ForceFirst(IntPtr hw) {
            int p;
            int f = GetWindowThreadProcessId(GetForegroundWindow(), out p);
            int t = GetWindowThreadProcessId(hw, out p);
            AttachThreadInput(t, f, true);
            First(hw);
            AttachThreadInput(t, f, false);
        }

        public static Rectangle GetBounds(IntPtr hw) {
            RECT r;
            GetWindowRect(hw, out r);
            return new Rectangle(r.Left, r.Top,
                r.Right - r.Left, r.Bottom - r.Top);
        }

        public static Rectangle GetBoundsNormal(IntPtr hw) {
            WINDOWPLACEMENT wp = WINDOWPLACEMENT.Default;
            GetWindowPlacement(hw, out wp);
            var r = wp.NormalPosition;
            return new Rectangle(r.Left, r.Top,
                r.Right - r.Left, r.Bottom - r.Top);
        }

        public static string GetClassName(IntPtr hw) {
            var sb = new StringBuilder(900);
            GetClassName(hw, sb, sb.Capacity);
            return sb.ToString();
        }

        public static Icon GetIcon(IntPtr hw) {
            var hi = SendMessage(hw, WM_GETICON, ICON_BIG, 0);
            if (hi == IntPtr.Zero)
                hi = GetClassLong(hw, GCLP_HICON);
            if (hi == IntPtr.Zero)
                return SystemIcons.Application;
            DestroyIcon(hi);
            return Icon.FromHandle(hi);
        }

        public static string GetTitle(IntPtr hw) {
            var sb = new StringBuilder(900);
            GetWindowText(hw, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool Maximizable(IntPtr hw) {
            return (GetWindowLong(hw, GWL_STYLE) & WS_MAXIMIZEBOX) != 0;
        }

        public static void Maximize(IntPtr hw) {
            ShowWindow(hw, SW_SHOWMAXIMIZED);
        }

        public static bool Minimizable(IntPtr hw) {
            return (GetWindowLong(hw, GWL_STYLE) & WS_MINIMIZEBOX) != 0;
        }

        public static void Minimize(IntPtr hw) {
            ShowWindow(hw, SW_SHOWMINIMIZED);
        }

        public static void Restore(IntPtr hw) {
            ShowWindow(hw, SW_RESTORE);
        }

        public static void Second(IntPtr hw, IntPtr hwia) {
            if (IsIconic(hw)) {
                ShowWindow(hw, SW_RESTORE);
                SetForegroundWindow(hwia);
            } else {
                SetWindowPos(hw, hw == hwia ? IntPtr.Zero : hwia, 0, 0, 0, 0,
                    SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        public static void SetBounds(
                IntPtr hw, IntPtr hwia, int x, int y, int w, int h) {
            ShowWindow(hw, SW_RESTORE);
            ShowWindow(hw, SW_RESTORE);
            SetWindowPos(hw, hw == hwia ? IntPtr.Zero : hwia, x, y, w, h, SWP_NOACTIVATE |
                ((GetWindowLong(hw, GWL_STYLE) & WS_SIZEBOX) == 0 ? SWP_NOSIZE : 0));
        }

        public static void SetLocation(IntPtr hw, IntPtr hwia, int x, int y) {
            ShowWindow(hw, SW_RESTORE);
            ShowWindow(hw, SW_RESTORE);
            SetWindowPos(hw, hw == hwia ? IntPtr.Zero : hwia, x, y, 0, 0,
                SWP_NOACTIVATE | SWP_NOSIZE);
        }

        public static void ToggleTool(IntPtr hw) {
            int s = GetWindowLong(hw, GWL_EXSTYLE);
            if ((s & WS_EX_TOOLWINDOW) == 0) {
                SetWindowLong(hw, GWL_EXSTYLE, s | WS_EX_TOOLWINDOW);
            } else {
                SetWindowLong(hw, GWL_EXSTYLE, s & ~WS_EX_TOOLWINDOW);
            }
        }

        public static List<IntPtr> Tasks {
            get {
                var hws = new List<IntPtr>();
                EnumWindows((hw, lp) => {
                    if (IsWindowVisible(hw) && GetWindow(hw, GW_OWNER) == IntPtr.Zero &&
                            (GetWindowLong(hw, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) == 0)
                        hws.Add(hw);
                    return true;
                }, 0);
                return hws;
            }
        }
    }
}
