using System.Windows.Forms;

namespace FitWin {

    class WebBrowserEx : WebBrowser {

        public bool IsComplete { get; private set; }

        public WebBrowserEx() {
            AllowWebBrowserDrop = false;
            IsComplete = false;
            ScriptErrorsSuppressed = true;
        }

        protected override void OnDocumentCompleted(
                WebBrowserDocumentCompletedEventArgs e) {
            IsComplete = true;
            base.OnDocumentCompleted(e);
        }

        protected override void OnNavigating(WebBrowserNavigatingEventArgs e) {
            IsComplete = false;
            base.OnNavigating(e);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (e.KeyData == (Keys.Control | Keys.O) ||
                    e.KeyData == (Keys.Control | Keys.P))
                e.IsInputKey = true;
        }
    }
}
