using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class NekoEndpoints
    {
        public Dictionary<string, string> Nsfw { get; }
        public Dictionary<string, string> Sfw { get; }

        public NekoEndpoints(JObject data)
        {
            var nsfw = (JObject)data["nsfw"];
            var sfw = (JObject)data["sfw"];

            Nsfw = new Dictionary<string, string>();
            Sfw = new Dictionary<string, string>();

            foreach (var t in nsfw)
            {
                var val = t.Value.Value<string>();
                if (val.Contains("/img/"))
                    Nsfw.Add(t.Key, t.Value.Value<string>());
            }

            foreach (var t in sfw)
            {
                var val = t.Value.Value<string>();
                if (val.Contains("/img/"))
                    Sfw.Add(t.Key, t.Value.Value<string>());
            }
        }
    }
}