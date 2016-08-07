using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FitWin {

    [ComVisible(true)]
    public class OptionForm : Form {

        readonly Data data;
        readonly WebBrowserEx wb;

        public OptionForm(Data data) {
            this.data = data;

            SuspendLayout();

            ShowIcon = false;
            ShowInTaskbar = false;
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            Text = "オプション";

            wb = new WebBrowserEx {
                Dock = DockStyle.Fill,
                ObjectForScripting = this,
                Parent = this,
                WebBrowserShortcutsEnabled = false,
            };
            wb.DocumentCompleted += (s, e) => {
                wb.Document.InvokeScript("onCompleted",
                    new object[] { data.Fore, data.Back.ToJson() });
                wb.Document.InvokeScript("onActivated");
            };
            wb.DocumentText = Properties.Resources.Option;

            ResumeLayout(false);
        }

        public void Adapt(string f, string b) {
            data.Fore = f;
            data.Back = b.ToObject<BackData>();
            ((MainForm)Owner).Reload();
        }

        protected override void Dispose(bool d) {
            base.Dispose(d);
            wb.Dispose();
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            if (wb.IsComplete)
                wb.Document.InvokeScript("onActivated");
        }
    }
}
