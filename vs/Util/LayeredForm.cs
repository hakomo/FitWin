using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FitWin {

    class LayeredForm : Form {

        [DllImport("gdi32")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32")]
        static extern int DeleteDC(IntPtr hdc);
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr h);
        [DllImport("gdi32")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("user32")]
        static extern IntPtr GetDC(IntPtr hw);
        [DllImport("user32")]
        static extern int ReleaseDC(IntPtr hw, IntPtr hdc);
        [DllImport("user32")]
        static extern int UpdateLayeredWindow(IntPtr hw, IntPtr hdcDst, ref Point pDst,
            ref Size s, IntPtr hdcSrc, ref Point pSrc, int c, ref BLENDFUNCTION b, int f);

        [StructLayout(LayoutKind.Sequential)]
        struct BLENDFUNCTION {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;

            public static BLENDFUNCTION Default {
                get {
                    return new BLENDFUNCTION {
                        SourceConstantAlpha = 255, AlphaFormat = 1
                    };
                }
            }
        }

        public LayeredForm() {
            FormBorderStyle = FormBorderStyle.None;
        }

        public void SetLayeredBitmap(Point l, Bitmap bmp) {
            IntPtr hdcDst = IntPtr.Zero, hdcSrc = IntPtr.Zero,
                hbmpNew = IntPtr.Zero, hbmpOld = IntPtr.Zero;
            try {
                hdcDst = GetDC(IntPtr.Zero);
                hdcSrc = CreateCompatibleDC(hdcDst);
                hbmpNew = bmp.GetHbitmap(Color.FromArgb(0));
                hbmpOld = SelectObject(hdcSrc, hbmpNew);
                Bounds = new Rectangle(l, bmp.Size);
                var p = new Point();
                var s = bmp.Size;
                var b = BLENDFUNCTION.Default;
                UpdateLayeredWindow(
                    Handle, hdcDst, ref l, ref s, hdcSrc, ref p, 0, ref b, 2);
            } finally {
                if (hdcDst != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, hdcDst);
                if (hdcSrc != IntPtr.Zero)
                    DeleteDC(hdcSrc);
                if (hbmpNew != IntPtr.Zero) {
                    SelectObject(hdcSrc, hbmpOld);
                    DeleteObject(hbmpNew);
                }
            }
        }

        protected override CreateParams CreateParams {
            get {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80000;
                return cp;
            }
        }
    }
}
