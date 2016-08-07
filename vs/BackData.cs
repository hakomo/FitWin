using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FitWin {

    public class BackData {

        public FormWindowState State { get; set; }
        public Rectangle Bounds { get; set; }

        public int ShortcutKey { get; set; }

        public bool ValidFilter { get; set; }
        public HashSet<string> FilterClassNames { get; set; }

        public int PreviewC { get; set; }

        public BackData() {
            State = FormWindowState.Minimized;
            Bounds = new Rectangle(0, 0, 1041, 528);

            ShortcutKey = 0xa4;

            ValidFilter = true;
            FilterClassNames = new HashSet<string>();

            PreviewC = 0x9fbfdf;
        }

        public BackData(Form f)
            : this() {
            FilterClassNames =
                new HashSet<string>(new[] { Win32.GetClassName(f.Handle) });
        }
    }
}
