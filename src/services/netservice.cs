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
using System.Net;
using HtmlAgilityPack;
using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;
using LiteDB;

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

        private readonly string spotifyAuthString;
        private readonly HttpClient _spHc;
        private readonly AccountsService _accs;

        public NetService(DbService db, RandomService rand)
        {
            _hc = new HttpClient();
            _rand = rand;

            pasteHost = db.GetHost("pastebin");
            if (pasteHost is null)
            {
                Console.WriteLine("What is your preferred pastebin upload endpoint? (only logged to database.db)");
                pasteHost = Console.ReadLine() ?? "https://paste.jakedacatman.me/paste";
                db.AddHost("pastebin", pasteHost);
            }
            imageHost = db.GetHost("imageHost");
            if (imageHost is null)
            {
                Console.WriteLine("What is your preferred image host upload endpoint? (only logged to database.db)");
                imageHost = Console.ReadLine() ?? "https://i.jakedacatman.me/upload";
                db.AddHost("imageHost", imageHost);
            }


            spotifyAuthString = db.GetApiKey("spotify");
            if (spotifyAuthString is null)
            {
                Console.WriteLine("What is your Spotify app's client id?");
                var spotifyClientId = Console.ReadLine();

                Console.WriteLine("What is your Spotify app's client secret?");
                var spotifyClientSecret = Console.ReadLine();

                if (string.IsNullOrEmpty(spotifyClientId) || string.IsNullOrEmpty(spotifyClientSecret)) //refrain from poisoning db
                    throw new Exception("Must provide Spotify information.");

                spotifyAuthString = $"{spotifyClientId}:{spotifyClientSecret}";
                db.AddApiKey("spotify", spotifyAuthString);
            }

            Environment.SetEnvironmentVariable("SpotifyApiClientId", spotifyAuthString.Split(':')[0]);
            Environment.SetEnvironmentVariable("SpotifyApiClientSecret", spotifyAuthString.Split(':')[1]);

            _spHc = new HttpClient();
            _accs = new AccountsService(_spHc);

            uploadKey = db.GetApiKey("uploadKey") ?? db.GetApiKey("upload");
            pasteKey = db.GetApiKey("pasteKey") ?? uploadKey;

            Console.Clear();
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
            if (url == null || !Uri.IsWellFormedUriString(url, UriKind.Absolute) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

            var res = await _hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));

            if (res.IsSuccessStatusCode) return true;
            
            switch (res.StatusCode)
            {
                case HttpStatusCode.Redirect:
                    return true;
                case HttpStatusCode.RedirectKeepVerb:
                    return true;
                case HttpStatusCode.RedirectMethod:
                    return true;
            }

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
                var web = new HtmlWeb();
                var html = await web.LoadFromWebAsync(url);

                url = html
                    .GetElementbyId("single-gif-container")
                    .FirstChild
                    .Element("div")
                    .Element("img")
                    .GetAttributeValue("src", null);
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
            else if (url.Contains("tenor.com") && !url.Contains("media.tenor.com"))
            {
                var web = new HtmlWeb();
                var html = await web.LoadFromWebAsync(url);

                url = html
                    .GetElementbyId("single-gif-container")
                    .FirstChild
                    .Element("div")
                    .Element("img")
                    .GetAttributeValue("src", null);
            }
            
            var response = await _hc.GetAsync(new Uri(url));
            if (response.IsSuccessStatusCode)
            {
                var id = _rand.GenerateId();
                using (var f = File.Open(id.ToString(), System.IO.FileMode.OpenOrCreate))
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
            var sc = new FormUrlEncodedContent( new Dictionary<string, string> { { "input", stuffToUpload } } );
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            if (!string.IsNullOrEmpty(pasteKey))
                sc.Headers.Add("key", pasteKey); //you can always do don.e _db.AddApiKey("pasteKey", <key>) (and additionally change the host used)

            var request = await _hc.PostAsync(pasteHost, sc);
            return await request.Content.ReadAsStringAsync();
        }

        public async Task<string> DownloadAsStringAsync(string url) => await _hc.GetStringAsync(url);

        public async Task<Article> GetMediaWikiArticleAsync(string term, string url)
        {
            var data = JsonConvert.DeserializeObject<JArray>(await _hc.GetStringAsync($"https://{url}/w/api.php?action=opensearch&search={term}&limit=1&format=json"));
            var titleArr = data[1];
            var urlArr = data[3];
            if (titleArr.Count() == 0 || urlArr.Count() == 0)
                return new Article("", "");
            else
                return new Article(titleArr[0].Value<string>(), urlArr[0].Value<string>());
        }
        public async Task<Article> GetWikipediaArticleAsync(string term) => await GetMediaWikiArticleAsync(term, "en.wikipedia.org");  
        public async Task<Article> GetBulbapediaArticleAsync(string term) => await GetMediaWikiArticleAsync(term, "bulbapedia.bulbagarden.net");

        public async Task<string> UploadAsync(string path, string ext)
        {
            using (var ct = new MultipartFormDataContent())
            {
                ct.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path)), "file", $"temp.{ext}");

                if (!string.IsNullOrEmpty(uploadKey))
                    ct.Headers.Add("key", uploadKey); //you can always do don.e _db.AddApiKey("upload", <key>) (and additionally change the host used)

                var response = await _hc.PostAsync(imageHost, ct);

                File.Delete(path); 
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<List<PlaylistTrack>> GetSpotifySongsAsync(string url)
        {
            url = ParseUrl(url);
            if (url == null || 
                !Uri.IsWellFormedUriString(url, UriKind.Absolute) || 
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !url.Contains("open.spotify.com/")) 
                    throw new WebException("Invalid URL.");

            var list = new List<PlaylistTrack>();

            if (url.Contains("/playlist/")) //different apis
            {
                var r = Regex.Match(url, @"\/playlist\/([^\/\?\&]+)\??");

                if (!r.Success)
                    throw new NotSupportedException("That was not a supported Spotify URL.");

                var id = r.Captures[0].Value.Replace("/playlist/", "");

                //shamelessly stolen from the example here: https://github.com/Ringobot/SpotifyApi.NetCore/blob/master/README.md
                var playlists = new PlaylistsApi(_spHc, _accs);
                int limit = 100;
                var playlist = await playlists.GetTracks(id, limit: limit);
                int offset = 0;
                while (playlist.Items.Any())
                {
                    for (int i = 0; i < playlist.Items.Length; i++)
                    {
                        list.Add(playlist.Items[i]);
                    }
                    offset += limit;
                    playlist = await playlists.GetTracks(id, limit: limit, offset: offset);
                }
            }
            /*else if (url.Contains("/album/"))
            {
                var r = Regex.Match(url, @"\/album\/([^\/\?\&]+)\??");
                var id = r.Captures[0].Value;
                
                var albums = new AlbumsApi(_spHc, _accs);

                int limit = 50;
                var album = await albums.GetAlbumTracks<PlaylistPaged>(id, limit: limit);
                int offset = 0;
                while (album.Items.Any())
                {
                    for (int i = 0; i < album.Items.Length; i++)
                    {
                        list.Add(album.Items[0]);
                    }
                    offset += limit;
                    album = await albums.GetAlbumTracks<PlaylistPaged>(id, limit: limit, offset: offset);
                }
            }*/
            else
            {
                throw new NotSupportedException("That was not a supported Spotify URL.");
            }

            return list;
        }

        private string ParseUrl(string url) => url.TrimStart('<').TrimEnd('>');
    }
}