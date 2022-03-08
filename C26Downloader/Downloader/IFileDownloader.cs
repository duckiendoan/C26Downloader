using System;
using System.Threading;
using System.Threading.Tasks;

namespace C26Downloader
{
    internal interface IFileDownloader
    {
        event Func<FileProperties, Task> FileProperitiesRetrieved;

        event Func<FileDownloadCompletedEventArgs, Task> FileDownloadCompleted;

        event Func<FileDownloadProgress, Task> FileDownloadProgressChanged;

        Task DownloadAsync(Uri requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken));

        Task DownloadAsync(string requestUri, string filename = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}