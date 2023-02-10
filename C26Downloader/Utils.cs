using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace C26Downloader
{
    internal class Utils
    {
        public static string GetFilenameTest(string filename, IEnumerable<string> files)
        {
            files = files.Select(x => Path.GetFileName(x));
            int index = 1;
            var name = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);
            while (files.Contains(filename))
            {
                filename = $"{name} ({index}){ext}";
                index++;
            }
            return filename;
        }

        //I didn't write this. Credits: https://www.somacon.com/p576.php
        public static string BytesToString(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = i >> 10;
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.00") + suffix;
        }

        public static bool ParseYesNo(string value)
        {
            value = value.ToLower();
            switch (value)
            {
                case "y":
                case "yes":
                    return true;
                default:
                    return default;
            }
        }
    }
}