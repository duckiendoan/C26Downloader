using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace C26Downloader
{
    internal class FileDownloader : IFileDownloader
    {
        public static GoogleDriveDownloader GoogleDrive { get; set; }
            = new GoogleDriveDownloader();

        public event Func<FileProperties, Task> FileProperitiesRetrieved;

        public event Func<FileDownloadCompletedEventArgs, Task> FileDownloadCompleted;

        public event Func<FileDownloadProgress, Task> FileDownloadProgressChanged;
        //Default stream copy buffer size
        public int BufferSize { get; set; } = 81920;

        private readonly HttpClient _client;

        public FileDownloader(HttpClient client = null)
        {
            _client = client ?? new HttpClient() { Timeout = TimeSpan.FromHours(2) };
        }

        public virtual Task DownloadAsync(string requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken))
            => DownloadAsync(new Uri(requestUri), filename, cancellationToken);

        public virtual async Task DownloadAsync(Uri requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Only read the headers
            // New approach
            using (var response = await _client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                await InternalDownloadAsync(response, filename, cancellationToken);
        }

        protected async Task InternalDownloadAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken, long optional_filesize = 0)
        {
            var filename = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path);
            
            if (string.IsNullOrWhiteSpace(directory))
                directory = Directory.GetCurrentDirectory();
            
            if (string.IsNullOrWhiteSpace(filename))
            {
                var content_disposition = response.Content.Headers.ContentDisposition;
                if (content_disposition != null)
                    filename = content_disposition.FileNameStar ?? //Filename star should be our first option
                        content_disposition.FileName ??
                        filename;
                else
                    filename = response.RequestMessage.RequestUri.Segments[^1];
            }
            
            var fileSize = response.Content.Headers.ContentLength ?? optional_filesize;
            //If the file has no extension, maybe get the extension via this?
            //TODO: better handling
            if (string.IsNullOrWhiteSpace(Path.GetExtension(filename)))
            {
                var ext = response.Content.Headers?.ContentType?.MediaType;
                if (ext != null && ext != "application/octet-stream")
                    filename += $".{(ext != "text/plain" ? ext.Split('/')[1] : "txt")}";
            }
            // Check for duplicate
            filename = Path.Combine(directory, Utils.GetFilenameTest(filename, Directory.GetFiles(directory)));
            
            await FileProperitiesRetrieved?.Invoke(new FileProperties
            {
                Filename = filename,
                Size = fileSize,
                SupportByteRange = response.Headers.AcceptRanges?.Contains("bytes") ?? false,
                FileType = response.Content.Headers.ContentType?.MediaType
            });
            var sw = Stopwatch.StartNew();
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                long totalReadBytes = 0;
                
                try
                {
                    do
                    {
                        int bytesRead = await stream.ReadAsync(new Memory<byte>(buffer), cancellationToken);
                        if (bytesRead == 0) break;
                        
                        await fs.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken);
                        totalReadBytes += bytesRead;

                        if (FileDownloadProgressChanged != null)
                        {
                            await FileDownloadProgressChanged?.Invoke(new FileDownloadProgress
                            {
                                Percentage = (double)totalReadBytes / fileSize * 100,
                                DownloadSpeedBytes = totalReadBytes / sw.Elapsed.TotalSeconds,
                                Filename = filename,
                                DownloadedBytes = totalReadBytes
                                //EstimatedFinishTime = DateTimeOffset.Now.AddSeconds((fileSize - totalBytes) / (totalBytes / sw.Elapsed.TotalSeconds))
                            });
                        }
                    }
                    while (true);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    sw.Stop();
                }
                
                await FileDownloadCompleted?.Invoke(new FileDownloadCompletedEventArgs { ElapsedTime = sw.Elapsed });
            }
        }
    }
}