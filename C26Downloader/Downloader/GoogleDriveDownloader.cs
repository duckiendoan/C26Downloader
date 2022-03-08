using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace C26Downloader
{
    internal class GoogleDriveDownloader : FileDownloader
    {
        private readonly HttpClient _client;
        private static readonly Regex _fileSizeRegex = new Regex(@"<\/a>\s?\((.*)\)<\/span>\sis\stoo");
        private static readonly Regex _confirmCodeRegex = new Regex(";confirm=(.{4})");

        public GoogleDriveDownloader(HttpClient client = null)
        {
            _client = client ?? new HttpClient();
        }

        public override Task DownloadAsync(Uri requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DownloadAsync(requestUri.ToString(), filename, cancellationToken);
        }

        public override async Task DownloadAsync(string requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var id = GetFileId(requestUri);
            var downloadUrl = $"https://drive.google.com/uc?id={id}&export=download";
            using (var response = await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (response.Content.Headers.ContentType.MediaType.Contains("text/html")) //We're in a confirmation page
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var confirmCode = _confirmCodeRegex.Match(content).Groups[1].Value;
                    var fileSize = _fileSizeRegex.Match(content).Groups[1].Value;
                    downloadUrl = $"https://drive.google.com/uc?export=download&confirm={confirmCode}&id={id}";
                    await InternalDownloadAsync(
                        await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken),
                        filename,
                        cancellationToken,
                        GetFileSize(fileSize));
                    return;
                }
                await InternalDownloadAsync(response, filename, cancellationToken);
            }
        }

        private string GetFileId(string url)
        {
            var uri = new Uri(url);
            if (uri.Host != "drive.google.com") throw new InvalidOperationException("URL is not a google drive url");
            //Handle https://drive.google.com/file/d/FILEID/view?usp=sharing
            if (url.Contains("/file/d")) return uri.Segments[3].Trim('/');
            //Handle https://drive.google.com/open?id=FILEID
            else if (url.Contains("?id=")) return HttpUtility.ParseQueryString(uri.Query).Get("id");

            return null;
        }

        private long GetFileSize(string size)
        {
            var unit = size[size.Length - 1];
            var s = double.Parse(size.Substring(0, size.Length - 1));
            switch (unit)
            {
                case 'M': return (long)(s * 1048576);
                case 'G': return (long)(s * 1073741824);
                default: return 0;
            }
        }
    }
}