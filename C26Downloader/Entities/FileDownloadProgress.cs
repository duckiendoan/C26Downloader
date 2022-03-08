using System;

namespace C26Downloader
{
    internal class FileDownloadProgress
    {
        public double Percentage { get; set; }
        public long DownloadedBytes { get; set; }
        public double DownloadSpeedBytes { get; set; }
        public string Filename { get; set; }
        public DateTimeOffset EstimatedFinishTime { get; set; }
    }
}