using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace FitWin {

    static class JSON {

        public static string ToJson<T>(this T o) {
            using (var ms = new MemoryStream()) {
                (new DataContractJsonSerializer(typeof(T))).WriteObject(ms, o);
                return Encoding.UTF8.GetString(ms.GetBuffer());
            }
        }

        public static T ToObject<T>(this string j) {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(j))) {
                return (T)(new DataContractJsonSerializer(typeof(T)))
                    .ReadObject(ms);
            }
        }

        public static Dictionary<string, string> ToDictionary(this string j) {
            try {
                return (new JavaScriptSerializer())
                    .Deserialize<Dictionary<string, string>>(j);
            } catch {
                return new Dictionary<string, string>();
            }
        }
    }
}
