using System;

namespace C26Downloader
{
    internal class FileDownloadCompletedEventArgs
    {
        public TimeSpan ElapsedTime { get; set; }
    }
}