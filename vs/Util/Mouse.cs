using System.Runtime.InteropServices;

namespace FitWin {

    static class Mouse {

        [DllImport("user32")]
        static extern void mouse_event(int f, int x, int y, int d, int i);

        const int MOUSEEVENTF_LEFTDOWN = 2;

        public static void Down() {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }
    }
}
