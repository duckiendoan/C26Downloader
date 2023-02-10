using System;
using System.Reflection;
using System.Threading.Tasks;

namespace C26Downloader
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine($"version: {Assembly.GetEntryAssembly().GetName().Version.ToString(3)}");
            Console.WriteLine($"CLR version: {System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion()}");
            Console.WriteLine($"Running on {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");


            bool disabled = false;
            IFileDownloader downloader = null;
            Console.WriteLine("Enter URL: ");
            var uri = new Uri(Console.ReadLine());
            //var uri = new Uri(File.ReadAllText("url.txt"));
            Console.WriteLine(uri);
            downloader = uri.Host switch
            {
                "drive.google.com" => new GoogleDriveDownloader(),
                _ => new FileDownloader(),
            };
            if (!disabled)
            {
                downloader.FileDownloadProgressChanged += (e) =>
                {
                    Console.Write($"\r{e.Percentage:0.00}% completed");
                    Console.Title = $"Downloading {e.Filename} - {Utils.BytesToString((long)e.DownloadSpeedBytes)}/s - {Utils.BytesToString(e.DownloadedBytes)} downloaded";
                    return Task.CompletedTask;
                };
            }
            downloader.FileProperitiesRetrieved += (props) =>
            {
                Console.WriteLine($"File size: {Utils.BytesToString(props.Size)}");
                Console.WriteLine($"File will be saved to {props.Filename}");
                Console.WriteLine($"Support partial download: {props.SupportByteRange}");
                Console.WriteLine($"File type: {props.FileType}");
                return Task.CompletedTask;
            };
            downloader.FileDownloadCompleted += (e) =>
            {
                Console.WriteLine($"\nFinished! Total time: {e.ElapsedTime}");
                return Task.CompletedTask;
            };
            Console.WriteLine($"Using {downloader.GetType().Name}");
            Console.WriteLine("Start downloading...");
            await downloader.DownloadAsync(uri);

            Console.ReadKey(true);     // Prevent app from quitting
        }
    }
}
