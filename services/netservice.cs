using System;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace donniebot.services
{
    public class NetService
    {
        private readonly HttpClient _hc;
        private readonly RandomService _rand;
        private readonly string uploadKey;

        public NetService(DbService db, RandomService rand)
        {
            _hc = new HttpClient();
            _rand = rand;
            uploadKey = db.GetApiKey("upload");
        }

        public async Task<bool> IsVideoAsync(string url)
        {
            url = url.Trim('<').Trim('>');

            var res = await _hc.GetAsync(new Uri(url));

            if (res.IsSuccessStatusCode)
                if (res.Content.Headers.ContentType.MediaType.Contains("video"))
                    return true;

            return false;
        }

        public async Task<byte[]> DownloadFromUrlAsync(string url)
        {
            url = url.TrimStart('<').TrimEnd('>');
            if (url.Contains("giphy.com")) 
            {
                if (url.Contains('-'))
                    url = $"https://i.giphy.com/media/{url.Split('-').Last()}/giphy.gif";
                else 
                    if (url.Contains("media.giphy.com"))
                        url = url.Replace("media.", "i.");
                    else
                        url = $"https://i.giphy.com/media/{url.Split('/').Last()}/giphy.gif";
            }
            else if (url.Contains("tenor.com") && !url.Contains("media.tenor.com"))
            {
                Regex r = new Regex(@"(https:\\u002F\\u002Fmedia1\.tenor\.com[A-z0-9]+\.gif)");
                var html = await DownloadAsStringAsync(url);
                var match = r.Match(html);
                if (match != null)
                    url = match.Groups[0].Value.Replace("\\u002F", "/");
            }
            var response = await _hc.GetAsync(new Uri(url));

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();
            else 
                throw new NullReferenceException("The data could not be found.");
        }
        public async Task<string> DownloadToFileAsync(string url)
        {
            url = url.Trim('<').Trim('>');
            if (url.Contains("giphy.com")) 
            {
                if (url.Contains('-'))
                    url = $"https://i.giphy.com/media/{url.Split('-').Last()}/giphy.gif";
                else 
                    if (url.Contains("media.giphy.com"))
                        url = url.Replace("media.", "i.");
                    else
                        url = $"https://i.giphy.com/media/{url.Split('/').Last()}/giphy.gif";
            }
            else if (url.Contains("tenor.com"))
            {
                Regex r = new Regex(@"(https:\\u002F\\u002Fmedia1\.tenor\.com[A-z0-9]+\.gif)");
                var html = await DownloadAsStringAsync(url);
                var match = r.Match(html);
                if (match != null)
                    url = match.Groups[0].Value.Replace("\\u002F", "/");
            }
            var response = await _hc.GetAsync(new Uri(url));
            if (response.IsSuccessStatusCode)
            {
                var id = _rand.GenerateId();
                using (var f = File.Open(id.ToString(), FileMode.OpenOrCreate))
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await f.WriteAsync(content, 0, content.Length);
                    await f.FlushAsync();
                }
                return id.ToString();
            }
            else return "";
        }

        public async Task<string> UploadToPastebinAsync(string stuffToUpload)
        {
            try
            {
                var sc = new FormUrlEncodedContent( new Dictionary<string, string> { { "input", stuffToUpload } } );
                sc.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                sc.Headers.Add("key", uploadKey); //you can always do don.e _db.AddApiKey("upload", <key>) and change the host used

                var request = await _hc.PostAsync("https://paste.jakedacatman.me/paste", sc);
                return await request.Content.ReadAsStringAsync();
            }
            catch (Exception e) //when (e.Message == "The remote server returned an error: (520) Origin Error.")
            {
                throw e;
            }
        }

        public async Task<string> DownloadAsStringAsync(string url) => await _hc.GetStringAsync(url);

        public async Task<Tuple<string, string>> GetWikipediaArticleAsync(string term)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<JArray>(await _hc.GetStringAsync($"https://en.wikipedia.org/w/api.php?action=opensearch&search={term}&limit=1&format=json"));
                var titleArr = data[1];
                var urlArr = data[3];
                if (titleArr.Count() == 0 || urlArr.Count() == 0)
                    return new Tuple<string, string>("", "");
                else
                    return new Tuple<string, string>(titleArr[0].Value<string>(), urlArr[0].Value<string>());
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<string> UploadAsync(string path, string ext)
        {
            using (var ct = new MultipartFormDataContent())
            {
                ct.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path)), "file", $"temp.{ext}");
                ct.Headers.Add("key", uploadKey);
                var response = await _hc.PostAsync("https://i.jakedacatman.me/upload", ct);

                File.Delete(path); 
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}