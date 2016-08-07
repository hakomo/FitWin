using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FitWin {

    class MouseHook : IDisposable {

        delegate int HookProcedure(int cd, int wp, IntPtr lp);

        [DllImport("user32")]
        static extern int CallNextHookEx(int hh, int cd, int wp, IntPtr lp);
        [DllImport("user32")]
        static extern short GetKeyState(int k);
        [DllImport("user32")]
        static extern bool PostMessage(IntPtr hw, int m, IntPtr wp, int lp);
        [DllImport("user32")]
        static extern IntPtr SetWindowsHookEx(int id, HookProcedure hp, int hi, int tid);
        [DllImport("user32")]
        static extern bool UnhookWindowsHookEx(IntPtr hh);

        const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14,
            WM_KEYUP = 0x101, WM_LBUTTONDOWN = 0x201, WM_SYSKEYUP = 0x105;

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT {
            public int Code, ScanCode, Flags, Time, ExtraInfo;
        }

        readonly int k;
        readonly IntPtr hw, hk, hm;
        readonly HookProcedure kp, mp;

        bool v = false;

        public MouseHook(string s, string t) {
            long l;
            long.TryParse(s, out l);
            hw = new IntPtr(l);
            int.TryParse(t, out k);
            kp = new HookProcedure(OnKeyboardHook);
            mp = new HookProcedure(OnMouseHook);
            hk = SetWindowsHookEx(WH_KEYBOARD_LL, kp, 0, 0);
            hm = SetWindowsHookEx(WH_MOUSE_LL, mp, 0, 0);
        }

        int OnKeyboardHook(int cd, int wp, IntPtr lp) {
            if (cd >= 0 && v && (wp == WM_KEYUP || wp == WM_SYSKEYUP) &&
                    ((KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT))).Code == k) {
                if (PostMessage(hw, 0x8001, IntPtr.Zero, 0)) {
                    v = false;
                } else {
                    Application.Exit();
                }
            }
            return CallNextHookEx(0, cd, wp, lp);
        }

        int OnMouseHook(int cd, int wp, IntPtr lp) {
            if (cd >= 0 && wp == WM_LBUTTONDOWN && GetKeyState(k) < 0) {
                var q = Win32.Tasks.FirstOrDefault(p =>
                    Win32.GetBounds(p).Contains(Control.MousePosition));
                if (q != hw) {
                    if (PostMessage(hw, 0x8000, q, 0)) {
                        v = true;
                        return 1;
                    } else {
                        Application.Exit();
                    }
                }
            }
            return CallNextHookEx(0, cd, wp, lp);
        }

        public void Dispose() {
            UnhookWindowsHookEx(hk);
            UnhookWindowsHookEx(hm);
        }
    }
}
