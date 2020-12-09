using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using donniebot.classes;

namespace donniebot.services
{
    public class NetService
    {
        private readonly HttpClient _hc;
        private readonly RandomService _rand;
        private readonly string uploadKey;
        private readonly string pasteKey;
        private readonly string imageHost;
        private readonly string pasteHost;

        public NetService(DbService db, RandomService rand)
        {
            _hc = new HttpClient();
            _rand = rand;

            uploadKey = db.GetApiKey("upload");
            pasteKey = db.GetApiKey("pasteKey") ?? uploadKey;

            pasteHost = db.GetHost("pastebin");
            if (pasteHost == null)
            {
                Console.WriteLine("What is your preferred pastebin upload endpoint? (only logged to database.db)");
                pasteHost = Console.ReadLine() ?? "https://paste.jakedacatman.me/paste";
                db.AddHost("pastebin", pasteHost);
            }
            imageHost = db.GetHost("imageHost");
            if (imageHost == null)
            {
                Console.WriteLine("What is your preferred image host upload endpoint? (only logged to database.db)");
                imageHost = Console.ReadLine() ?? "https://i.jakedacatman.me/upload";
                db.AddHost("imageHost", imageHost);
            }
        }

        public async Task<bool> IsVideoAsync(string url)
        {
            url = ParseUrl(url);
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            var res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

            if (res.IsSuccessStatusCode)
                if (res.Content.Headers.ContentType.MediaType.Contains("video"))
                    return true;

            res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
            if (res.IsSuccessStatusCode)
                if (res.Content.Headers.ContentType.MediaType.Contains("video"))
                    return true;

            return false;
        }

        public async Task<long> GetContentLengthAsync(string url)
        {
            url = ParseUrl(url);
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return 0;

            var res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

            if (res.IsSuccessStatusCode)
                if (res.Content.Headers.ContentLength.HasValue)
                    return res.Content.Headers.ContentLength.Value;

            return 0L;
        }

        public async Task<string> GetContentTypeAsync(string url)
        {
            url = ParseUrl(url);
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

            var res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

            if (res.IsSuccessStatusCode)
                return res.Content.Headers.ContentType.MediaType;

            return null;
        }

        public async Task<bool> IsSuccessAsync(string url)
        {
            url = ParseUrl(url);
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            var res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

            if (res.IsSuccessStatusCode) return true;

            return false;
        }


        public async Task<byte[]> DownloadFromUrlAsync(string url)
        {
            url = ParseUrl(url);
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
            url = ParseUrl(url);
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

                if (!string.IsNullOrEmpty(uploadKey))
                    sc.Headers.Add("key", uploadKey); //you can always do don.e _db.AddApiKey("upload", <key>) (and additionally change the host used)

                var request = await _hc.PostAsync(pasteHost, sc);
                return await request.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<string> DownloadAsStringAsync(string url) => await _hc.GetStringAsync(url);

        public async Task<Article> GetMediaWikiArticleAsync(string term, string url)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<JArray>(await _hc.GetStringAsync($"https://{url}/w/api.php?action=opensearch&search={term}&limit=1&format=json"));
                var titleArr = data[1];
                var urlArr = data[3];
                if (titleArr.Count() == 0 || urlArr.Count() == 0)
                    return new Article("", "");
                else
                    return new Article(titleArr[0].Value<string>(), urlArr[0].Value<string>());
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public async Task<Article> GetWikipediaArticleAsync(string term) => await GetMediaWikiArticleAsync(term, "en.wikipedia.org");  
        public async Task<Article> GetBulbapediaArticleAsync(string term) => await GetMediaWikiArticleAsync(term, "bulbapedia.bulbagarden.net");

        public async Task<string> UploadAsync(string path, string ext)
        {
            using (var ct = new MultipartFormDataContent())
            {
                ct.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path)), "file", $"temp.{ext}");

                if (!string.IsNullOrEmpty(pasteKey))
                    ct.Headers.Add("key", pasteKey); //you can always do don.e _db.AddApiKey("pasteKey", <key>) (and additionally change the host used)

                var response = await _hc.PostAsync(imageHost, ct);

                File.Delete(path); 
                return await response.Content.ReadAsStringAsync();
            }
        }

        private string ParseUrl(string url) => url.TrimStart('<').TrimEnd('>');
    }
}