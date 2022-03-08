using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace C26Downloader
{
    public class ScribdExtractor
    {
        private readonly HttpClient _client;
        private readonly StringBuilder _sb = new StringBuilder();
        private static readonly Regex _pageRegex = new Regex("pageParams.contentUrl\\s?=\\s?\"(https?:\\/\\/.*)\";", RegexOptions.Multiline | RegexOptions.Compiled);

        public ScribdExtractor(HttpClient client)
        {
            _client = client ?? new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.81 Safari/537.36");
        }

        public Task<ScribdDocument> ExtractAsync(string requestUri) => ExtractAsync(new Uri(requestUri));

        public async Task<ScribdDocument> ExtractAsync(Uri requestUri)
        {
            if (!requestUri.Host.EndsWith("scribd.com")) throw new Exception("Url is not a scribd document url");

            var content = await _client.GetStringAsync(requestUri);
            var pagesUrl = _pageRegex.Matches(content)
                .Select(x =>
                {
                    _sb.Clear();
                    _sb.Append(x.Groups[1].Value);
                    return _sb.Replace("pages", "images").Replace(".jsonp", ".jpg").ToString();
                })
                .ToList();
            
            return new ScribdDocument();
        }

        public async Task<Stream> ToHtmlAsync(Uri requestUri)
        {
            if (!requestUri.Host.EndsWith("scribd.com")) throw new Exception("Url is not a scribd document url");

            var content = await _client.GetStringAsync(requestUri);
            var pagesUrl = _pageRegex.Matches(content)
                .Select(x =>
                {
                    _sb.Clear();
                    _sb.Append(x.Groups[1].Value);
                    return _sb.Replace("pages", "images").Replace(".jsonp", ".jpg").ToString();
                })
                .ToArray();
            using (FileStream fs = new FileStream(Path.GetRandomFileName() + ".html", FileMode.Create))
            using (var writer = new StreamWriter(fs))
            {
                await writer.WriteLineAsync("<!DOCTYPE html>\n<html>\n<body>");
                foreach (var page in pagesUrl)
                {
                    await writer.WriteLineAsync($"<img src=\"{page}\">");
                }
                await writer.WriteLineAsync("</body>\n</html>");
            }
            return null;
        }

    }

    public struct ScribdDocument
    {
        public string Title { get; set; }
        public long Id { get; set; }
        public string Author { get; set; }
        public long PageCount { get; set; }
        public double Rating { get; set; }
        public IReadOnlyList<string> PageUrls { get; set; }
    }

    public static class Extensions
    {
        public static async Task SaveToAsync(this Stream stream, string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
                await stream.CopyToAsync(fs);
        }
    }
}
