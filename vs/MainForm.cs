using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FitWin {

    [ComVisible(true)]
    public class MainForm : Form {

        const string version = "1.3.20150605";

        readonly Data data;
        readonly LayeredForm preview;
        readonly NotifyIcon ni;
        readonly WebBrowserEx wb;
        readonly List<Action> undo = new List<Action>();

        BackData back;
        Process hook = null;
        List<long> task = new List<long>();
        List<Rectangle> multi = new List<Rectangle>();

        public EventWaitHandle EventWaitHandle {
            set {
                Task.Factory.StartNew(() => {
                    for (; ; ) {
                        value.WaitOne();
                        Invoke((Action)First);
                    }
                });
            }
        }

        string Combine() {
            var ss = data.EnumStyle();
            return Regex.Replace(Properties.Resources.Index, @"/\*\s(.*?)\s\*/(.*?:).*?([;}])", m => {
                var t = ss.Where(s => m.Value.Contains(s.Key));
                if (t.Count() == 0)
                    return m.Value;
                return m.Groups[2] + m.Groups[1].Value.Replace(t.First().Key, t.First().Value) + m.Groups[3];
            });
        }

        void First() {
            Win32.First(Handle);
            Show();
        }

        static void KillProcess(Process p) {
            if (p == null)
                return;
            try {
                p.Kill();
            } catch {
            }
            p.Dispose();
            p = null;
        }

        void Watch() {
            var ws = Screen.AllScreens.Select(s => s.WorkingArea);
            if (!multi.SequenceEqual(ws)) {
                multi = ws.ToList();

                wb.Document.InvokeScript("onChangedMulti", new object[] {
                    Screen.AllScreens.Select(s => s.Bounds).ToJson(), ws.ToJson() });
            }

            var hws = Win32.Tasks.Where(hw =>
                !back.ValidFilter || !back.FilterClassNames.Contains(Win32.GetClassName(hw)));
            var ls = hws.Select(hw => hw.ToInt64());
            var sorted = ls.ToList();
            sorted.Sort();
            if (!task.SequenceEqual(sorted)) {
                task = sorted;

                wb.Document.InvokeScript("onChangedTask", new object[] {
                    ls.ToJson(),
                    hws.Select(hw => {
                        var ic = hw == Handle ? Icon : Win32.GetIcon(hw);
                        using (var bm = ic.ToBitmap())
                        using (var ms = new MemoryStream()) {
                            if (hw != Handle)
                                ic.Dispose();
                            bm.Save(ms, ImageFormat.Png);
                            return Convert.ToBase64String(ms.GetBuffer());
                        }
                    }).ToJson(),
                });
            }
        }

        public void ClickTask(int x, int y) {
            var p = Cursor.Position;
            Cursor.Position = PointToScreen(new Point(x, y));
            Mouse.Down();
            Cursor.Position = p;
        }

        public string GetTaskTitle(long l) {
            return Win32.GetTitle(new IntPtr(l));
        }

        public void Reload() {
            data.User = (string)wb.Document.InvokeScript("serialize");
            back = data.Back;
            wb.DocumentText = Combine();
            Data.File = data;
        }

        public void SecondTask(long l) {
            Win32.Second(new IntPtr(l), Handle);
        }

        public void SetCursor(int x, int y) {
            Cursor.Position = PointToScreen(new Point(x, y));
        }

        public void ToggleFilter() {
            back.ValidFilter = !back.ValidFilter;
            Watch();
        }

        public void ShowNumber() {
            using (var f = new Form {
                Font = SystemInformation.MenuFont,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
                Text = "数値指定",
            })
            using (var t = new TextBox {
                Location = new Point(8, 8),
                Size = new Size(f.ClientSize.Width - 16, 28),
            })
            using (var l = new Label {
                Height = f.ClientSize.Height - 86,
                Location = new Point(8, 36),
                Width = f.ClientSize.Width - 16,
            })
            using (var ok = new Button {
                Left = f.ClientSize.Width - 208,
                Size = new Size(96, 28),
                Text = "OK",
                Top = f.ClientSize.Height - 36,
            })
            using (var cancel = new Button {
                Left = f.ClientSize.Width - 104,
                Size = new Size(96, 28),
                Text = "キャンセル",
                Top = f.ClientSize.Height - 36,
            }) {
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                ok.Click += (se, ev) => {
                    string s = (string)wb.Document.InvokeScript("getErrorAndDropTask",
                        new object[] { t.Text });
                    if (string.IsNullOrWhiteSpace(s)) {
                        f.Close();
                    } else {
                        l.Text = s;
                    }
                };

                f.Controls.AddRange(new Control[] { t, l, ok, cancel });
                f.ShowDialog(this);
            }
        }

        void ShowAbout() {
            string s = version.Substring(version.Length - 8, 4);
            if (s != "2014")
                s = "2014-" + s;

            using (var f = new Form {
                Font = SystemInformation.MenuFont,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Size = new Size(440, 360),
                StartPosition = FormStartPosition.CenterParent,
                Text = "Fit Win について"
            })
            using (var t = new TextBox {
                BorderStyle = BorderStyle.None,
                Height = f.ClientSize.Height - 52,
                Location = new Point(8, 8),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = string.Format(
@"Fit Win v{0}
http://hakomo.github.io/fitwin/

(C) {1} hakomo Licensed MIT
http://opensource.org/licenses/MIT

Email address : hakomof@gmail.com
Twitter : @hakomof


Fit Win は以下のライブラリを使っています。

jQuery v2.1.1
http://jquery.com/
(C) 2005-2014 jQuery Foundation, Inc. Licensed MIT
https://jquery.org/license/

jQuery UI v1.11.1
http://jqueryui.com/
(C) 2014 jQuery Foundation, Inc. Licensed MIT

jQuery Mouse Wheel Plugin v3.1.12
(C) 2013 Brandon Aaron (http://brandon.aaron.sh/) Licensed MIT", version, s),
                Width = f.ClientSize.Width - 8,
            })
            using (var b = new Button {
                Left = f.ClientSize.Width - 104,
                Size = new Size(96, 28),
                Text = "OK",
                Top = f.ClientSize.Height - 36,
            }) {
                f.AcceptButton = b;
                f.CancelButton = b;
                f.Controls.AddRange(new Control[] { t, b });
                t.Select(0, 0);
                f.ShowDialog(this);
            }
        }

        public void ShowMenu(int x, int y) {
            string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Fit Win.lnk");
            bool b = (bool)wb.Document.InvokeScript("validHah");

            (new ContextMenu(new[] {
                new MenuItem("元に戻す(&U)", (s, e) =>
                    wb.Document.InvokeScript("popUndo"), Shortcut.CtrlZ),
                new MenuItem("テンプレートなどを保存(&S)", (s, e) =>
                    Save(), Shortcut.CtrlS),
                new MenuItem("-"),
                new MenuItem("フィルターをクリア", (s, e) => {
                    if (back.FilterClassNames.Count == 0)
                        return;
                    PushFilter();
                    back.FilterClassNames.Clear();
                    Watch();
                    Save();
                }),
                new MenuItem("&Hit a Hint", (s, e) =>
                    wb.Document.InvokeScript("toggleHah"), Shortcut.CtrlF) { Checked = b },
                new MenuItem("-"),
                new MenuItem("スタートアップ", (se, ev) => {
                    if (File.Exists(p)) {
                        try {
                            File.Delete(p);
                        } catch {
                        }
                    } else {
                        var s = (new IWshRuntimeLibrary.WshShell()).CreateShortcut(p);
                        s.IconLocation = Application.ExecutablePath + ",0";
                        s.TargetPath = Application.ExecutablePath;
                        s.WindowStyle = 7;
                        try {
                            s.Save();
                        } finally {
                            Marshal.ReleaseComObject(s);
                        }
                    }
                }) { Checked = File.Exists(p) },
                new MenuItem("オプション(&O)...", (s, e) => {
                    using (var f = new OptionForm(data))
                        f.ShowDialog(this);
                }),
                new MenuItem("-"),
                new MenuItem("オンラインマニュアル...", (s, e) =>
                    Process.Start("http://hakomo.github.io/fitwin/")),
                new MenuItem("バグの報告・要望...", (s, e) =>
                    Process.Start("https://docs.google.com/forms/d/1y261eyYnYuB5LwlV9NHfPYD_DaU48GlPGwQdp_iWnWQ/viewform?entry.1558357097=Fit+Win&entry.187745740&entry.1950332360")),
                new MenuItem("Fit Win について(&A)...", (s, e) => ShowAbout()),
            })).Show(this, new Point(x, y));
        }

        public void ShowEditorContextMenu(int x, int y, string j) {
            var e = j.ToObject<List<bool>>();
            (new ContextMenu(new[] {
                new MenuItem("水平 2 分割(&S)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 2, false })) { Enabled = e[0] },
                new MenuItem("水平 3 分割(&D)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 3, false })) { Enabled = e[1] },
                new MenuItem("水平 4 分割(&F)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 4, false })) { Enabled = e[2] },
                new MenuItem("-"),
                new MenuItem("垂直 2 分割(&W)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 2, true })) { Enabled = e[3] },
                new MenuItem("垂直 3 分割(&E)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 3, true })) { Enabled = e[4] },
                new MenuItem("垂直 4 分割(&R)", (se, ev) =>
                    wb.Document.InvokeScript("splitEditor",
                        new object[] { 4, true })) { Enabled = e[5] },
            })).Show(this, new Point(x, y));
        }

        public void ShowDeleteContextMenu(int x, int y, string n) {
            (new ContextMenu(new[] {
                new MenuItem("削除(&D)", (s, e) =>
                    wb.Document.InvokeScript(n))
            })).Show(this, new Point(x, y));
        }

        public void ShowTaskContextMenu(int x, int y, long l) {
            var hw = new IntPtr(l);
            (new ContextMenu(new[] {
                new MenuItem("元のサイズに戻す(&R)", (s, e) => {
                    PushRestore(hw);
                    Win32.Restore(hw);
                    Win32.SetForegroundWindow(Handle);
                }) { Enabled = Win32.IsIconic(hw) || Win32.IsZoomed(hw) },
                new MenuItem("最小化(&N)", (s, e) => {
                    PushRestore(hw);
                    Win32.Minimize(hw);
                    Win32.SetForegroundWindow(Handle);
                }) { Enabled = !Win32.IsIconic(hw) && Win32.Minimizable(hw) },
                new MenuItem("最大化(&X)", (s, e) => {
                    PushRestore(hw);
                    Win32.Maximize(hw);
                    Win32.SetForegroundWindow(Handle);
                }) { Enabled = !Win32.IsZoomed(hw) && Win32.Maximizable(hw) },
                new MenuItem("-"),
                new MenuItem("閉じる(&C)", (s, e) => {
                    Win32.Close(hw);
                    Watch();
                }),
                new MenuItem("-"),
                new MenuItem("フィルター(&F)", (s, e) => {
                    PushFilter();
                    string n = Win32.GetClassName(hw);
                    if (back.FilterClassNames.Contains(n)) {
                        back.FilterClassNames.Remove(n);
                    } else {
                        back.FilterClassNames.Add(n);
                    }
                    Watch();
                    Save();
                }) { Checked = back.FilterClassNames.Contains(Win32.GetClassName(hw)) },
            })).Show(this, new Point(x, y));
        }

        public void PushUndo() {
            undo.Add(() => { });
            if (undo.Count > 50)
                undo.RemoveAt(0);
        }

        void PushRestore(IntPtr hw) {
            var p = new Win32.Placement(hw);
            undo.Add(() => {
                p.Restore();
                Win32.SetForegroundWindow(Handle);
            });
            if (undo.Count > 50)
                undo.RemoveAt(0);
            wb.Document.InvokeScript("pushUndo");
        }

        void PushFilter() {
            var ns = new HashSet<string>(back.FilterClassNames);
            undo.Add(() => {
                back.FilterClassNames = ns;
                Watch();
                Save();
            });
            if (undo.Count > 50)
                undo.RemoveAt(0);
            wb.Document.InvokeScript("pushUndo");
        }

        public void PopUndo() {
            if (undo.Count == 0)
                return;
            undo.Last()();
            undo.RemoveAt(undo.Count - 1);
        }

        void ShowPreview(int x, int y, int w, int h) {
            Win32.Second(preview.Handle, Handle);
            using (var b = new Bitmap(w, h))
            using (var g = Graphics.FromImage(b)) {
                g.Clear(Color.FromArgb(back.PreviewC & 16777215 | 1 << 31));
                preview.SetLayeredBitmap(new Point(x, y), b);
            }
        }

        public void HidePreview() {
            using (var b = new Bitmap(1, 1))
                preview.SetLayeredBitmap(preview.Location, b);
        }

        public void MaximizeTask(long l, int x, int y, int w, int h, bool p) {
            if (p) {
                ShowPreview(x, y, w, h);
            } else {
                var hw = new IntPtr(l);
                var b = Win32.GetBoundsNormal(hw);

                if ((new Rectangle(x, y, w, h)).Contains(b.X + b.Width / 2, b.Y + b.Height / 2) &&
                        Win32.IsZoomed(hw) == Win32.Maximizable(hw)) {
                    Win32.Second(hw, Handle);
                } else {
                    PushRestore(hw);
                    if (!(new Rectangle(x, y, w, h)).Contains(b.X + b.Width / 2, b.Y + b.Height / 2))
                        Win32.SetLocation(hw, Handle, x, y);
                    if (Win32.Maximizable(hw))
                        Win32.Maximize(hw);
                    Win32.SetForegroundWindow(Handle);
                }
            }
        }

        public void SetTaskBounds(long l, int x, int y, int w, int h, bool p) {
            if (p) {
                ShowPreview(x, y, w, h);
            } else {
                var hw = new IntPtr(l);
                PushRestore(hw);
                Win32.SetBounds(hw, Handle, x, y, w, h);
                Win32.SetForegroundWindow(Handle);
                if (hw == Handle) {
                    back.State = FormWindowState.Normal;
                    back.Bounds = Bounds;
                }
            }
        }

        static int CalcBounds(int s, string d, int x, int w) {
            if (string.IsNullOrWhiteSpace(d)) {
                return s;
            } else if (d.IndexOf('.') == -1) {
                return int.Parse(d);
            }
            return x + (int)Math.Ceiling(w * double.Parse(d));
        }

        public void ShowMes(string s) {
            using (var f = new Form {
                Font = SystemInformation.MenuFont,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
            })
            using (var l = new Label {
                Height = f.ClientSize.Height - 52,
                Location = new Point(8, 8),
                Text = s,
                Width = f.ClientSize.Width - 16,
            })
            using (var b = new Button {
                Left = f.ClientSize.Width - 104,
                Size = new Size(96, 28),
                Text = "OK",
                Top = f.ClientSize.Height - 36,
            }) {
                f.AcceptButton = b;
                f.CancelButton = b;
                f.Controls.AddRange(new Control[] { l, b });
                f.ShowDialog(this);
            }
        }

        public void SetTaskBoundsStr(long l, string x, string y,
                string w, string h, int bx, int by, int bw, int bh, bool p) {
            string s = GetErrorAndSetTaskBoundsStr(l, x, y, w, h, bx, by, bw, bh, p);

            if (string.IsNullOrWhiteSpace(s)) {
            } else if (p) {
                HidePreview();
            } else {
                ShowMes(s);
            }
        }

        public string GetErrorAndSetTaskBoundsStr(long l, string x, string y,
                string w, string h, int bx, int by, int bw, int bh, bool p) {
            var b = Win32.GetBoundsNormal(new IntPtr(l));

            b.Width = CalcBounds(b.Width, w, 0, bw);
            b.Height = CalcBounds(b.Height, h, 0, bh);
            b.X = CalcBounds(b.X, x, bx, bw - b.Width);
            b.Y = CalcBounds(b.Y, y, by, bh - b.Height);

            if (Screen.AllScreens.FirstOrDefault(q =>
                   q.WorkingArea.Contains(b.X, b.Y) || q.WorkingArea.Contains(b.Right - 1, b.Y)) != null) {
                SetTaskBounds(l, b.X, b.Y, b.Width, b.Height, p);
                return "";
            } else {
                return "モニターのない位置には移動できません。";
            }
        }

        void SetBounds() {
            StartPosition = FormStartPosition.Manual;
            var b = back.Bounds;
            var w = Screen.PrimaryScreen.WorkingArea;
            if (back.State == FormWindowState.Minimized) {
                ClientSize = b.Size;
                Location = new Point((w.Width - Width) / 2, (w.Height - Height) / 2);
                back.State = FormWindowState.Normal;
                back.Bounds = Bounds;
            } else {
                var ws = back.State;
                if (Screen.AllScreens.FirstOrDefault(s =>
                        s.WorkingArea.Contains(b.X, b.Y) || s.WorkingArea.Contains(b.Right - 1, b.Y)) == null) {
                    Location = new Point((w.Width - b.Width) / 2, (w.Height - b.Height) / 2);
                    Size = b.Size;
                } else {
                    Bounds = b;
                }
                WindowState = ws;
            }
        }

        void CheckVersion() {
            Task.Factory.StartNew(() => {
                using (var wc = new WebClient()) {
                    if (wc.DownloadString("http://hakomo.github.io/fitwin/version") != version)
                        Invoke((Action)(() =>
                            Text = "Fit Win - 新しいバージョンが公開されています"));
                }
            });
        }

        MainForm() {
            data = Data.File ?? new Data { Back = new BackData(this) };
            back = data.Back;

            SuspendLayout();

            Icon = Properties.Resources.Icon;
            Text = "Fit Win";

            SetBounds();

            wb = new WebBrowserEx {
                Dock = DockStyle.Fill,
                ObjectForScripting = this,
                Parent = this,
            };
            wb.DocumentCompleted += (s, e) => {
                undo.Clear();
                multi.Clear();
                task.Clear();

                wb.Document.InvokeScript("onCompleted", new object[] {
                    data.Fore, data.User, back.ValidFilter });
                Watch();
                wb.Document.InvokeScript("onActivated");

                KillProcess(hook);
                if (back.ShortcutKey != 0)
                    hook = Process.Start(Application.ExecutablePath,
                        "hook " + Handle + " " + back.ShortcutKey);
            };
            wb.DocumentText = Combine();

            ResumeLayout(false);

            preview = new LayeredForm { ShowInTaskbar = false };
            Win32.ToggleTool(preview.Handle);
            preview.Show();
            HidePreview();

            ni = new NotifyIcon {
                ContextMenu = new ContextMenu(new[] {
                    new MenuItem("ウィンドウを表示(&S)", (s, e) => First()),
                    new MenuItem("閉じる(&C)", (s, e) => Application.Exit()),
                }),
                Icon = Icon,
                Text = "Fit Win",
                Visible = true,
            };
            ni.DoubleClick += (s, e) => First();

            CheckVersion();
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            if (!wb.IsComplete)
                return;
            Watch();
            wb.Document.InvokeScript("onActivated");
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            KillProcess(hook);

            preview.Dispose();
            ni.Dispose();
            wb.Dispose();
        }

        public void Save() {
            data.User = (string)wb.Document.InvokeScript("serialize");
            Data.File = data;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            Hide();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (WindowState == FormWindowState.Maximized) {
                back.State = FormWindowState.Maximized;
            } else if (WindowState == FormWindowState.Minimized) {
                Hide();
            } else {
                back.State = FormWindowState.Normal;
                back.Bounds = Bounds;
            }
        }

        protected override void OnResizeEnd(EventArgs e) {
            base.OnResizeEnd(e);
            back.State = FormWindowState.Normal;
            back.Bounds = Bounds;
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            if (m.Msg == 0x8000) {
                Win32.ForceFirst(Handle);
                Show();
                if (m.WParam != IntPtr.Zero)
                    wb.Document.InvokeScript("clickTask",
                        new object[] { m.WParam.ToInt64() });
            } else if (m.Msg == 0x8001) {
                Hide();
            }
        }

        [STAThread]
        static void Main(string[] a) {
            if (a.Length == 3 && a[0] == "hook") {
                var mh = new MouseHook(a[1], a[2]);
                Application.ApplicationExit += (s, e) => mh.Dispose();
                Application.Run();
            } else {
                EventWaitHandle e = null;
                try {
                    bool c;
                    e = new EventWaitHandle(false, EventResetMode.AutoReset,
                        "91B295C6-9F69-429F-9ABC-9DC404F18B42", out c);
                    if (c) {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new MainForm { EventWaitHandle = e });
                    } else {
                        e.Set();
                    }
                } catch {
                    if (e != null)
                        e.Dispose();
                }
            }
        }
    }
}
