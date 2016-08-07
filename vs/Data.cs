using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace FitWin {

    public class Data {

        public string Fore { get; set; }
        public string User { get; set; }
        public BackData Back { get; set; }

        public Data() {
            Fore = "{}";
            User = Properties.Resources.User;
            Back = new BackData();
        }

        public static Data File {
            get {
                FileStream fs = null;
                try {
                    fs = new FileStream(Path.Combine(Application.StartupPath, "fitwin.data"), FileMode.Open);
                    using (var gs = new GZipStream(fs, CompressionMode.Decompress))
                        return (Data)(new DataContractSerializer(typeof(Data))).ReadObject(gs);
                } catch (UnauthorizedAccessException) {
                    MessageBox.Show("Fit Win は Program Files などの管理者権限が必要なフォルダには配置できません。");
                    return null;
                } catch {
                    return null;
                } finally {
                    if (fs != null)
                        fs.Dispose();
                }
            }
            set {
                FileStream fs = null;
                try {
                    fs = new FileStream(Path.Combine(Application.StartupPath, "fitwin.data"), FileMode.Create);
                    using (var gs = new GZipStream(fs, CompressionMode.Compress))
                        (new DataContractSerializer(typeof(Data))).WriteObject(gs, value);
                } catch {
                } finally {
                    if (fs != null)
                        fs.Dispose();
                }
            }
        }

        static int CastValueOrDefault(
                Dictionary<string, string> d, string k, int e) {
            string s;
            d.TryGetValue(k, out s);
            int p;
            return int.TryParse(s, out p) ? p : e;
        }

        public IEnumerable<KeyValuePair<string, string>> EnumStyle() {
            var d = Fore.ToDictionary();

            int m = CastValueOrDefault(d, "TemplateMarginY", 12);
            int p = CastValueOrDefault(d, "TemplatePaddingY", 24);
            if (m - p != -12)
                d["TemplateMarginTop"] = (m - p).ToString();
            if (p * 2 - m != 36)
                d["TemplatePaddingTop"] = (p * 2 - m).ToString();

            m = CastValueOrDefault(d, "TaskMarginY", 8);
            p = CastValueOrDefault(d, "TaskPaddingY", 8);
            if (m - p != 0)
                d["TaskMarginTop"] = (m - p).ToString();
            if (p * 2 - m != 8)
                d["TaskPaddingTop"] = (p * 2 - m).ToString();
            return new List<KeyValuePair<string, string>>(d);
        }
    }
}
