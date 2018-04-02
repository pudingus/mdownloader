using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace downloader3
{
    /// <summary>
    /// Poskytuje různé statické metody
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Ověřuje jestli zadaná url je platná
        /// </summary>
        /// <param name="url">Url adresa</param>
        /// <returns></returns>
        public static bool IsValidURL(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Ověřuje jestli zadaná cesta je platná
        /// </summary>
        /// <param name="path">Cesta</param>
        /// <returns></returns>
        public static bool IsValidPath(string path)
        {
            bool valid = false;
            try
            {
                if (path.StartsWith("\\") || path.StartsWith("/")) valid = false;
                else
                {
                    Path.GetFullPath(path);
                    valid = Path.IsPathRooted(path);
                }
            }
            catch (Exception) { }

            return valid;
        }

        /// <summary>
        /// Převede bajty do čitelného formátu a vrátí řetězec
        /// </summary>
        /// <param name="bytes">Bajty</param>
        /// <returns></returns>
        public static string ConvertBytes(long bytes)
        {
            double KB = Math.Pow(1024, 1);
            double MB = Math.Pow(1024, 2);
            double GB = Math.Pow(1024, 3);
            double TB = Math.Pow(1024, 4);

            if (bytes >= TB) return string.Format("{0:0.0} TB", (bytes / TB));
            else if (bytes >= GB) return string.Format("{0:0.0} GB", (bytes / GB));
            else if (bytes >= MB) return string.Format("{0:0.0} MB", (bytes / MB));
            else if (bytes >= 0) return string.Format("{0:0.0} kB", (bytes / KB));
            else return "unk";
        }

        /// <summary>
        /// Převede sekundy do čitelného formátu a vrátí řetězec
        /// </summary>
        /// <param name="sec">Sekundy</param>
        /// <returns></returns>
        public static string ConvertTime(long sec)
        {
            string str = " ";
            long min, hour;

            min = sec / 60;
            sec = sec - (min * 60);
            hour = min / 60;
            min = min - (hour * 60);

            if (hour == 0 && min == 0) str = $"{sec}s";
            else if (min >= 1 && hour == 0) str = $"{min}m {sec}s";
            else if (hour >= 1 && min == 0) str = $"{hour}h";
            else str = $"{hour}h {min}m";

            return str;
        }
    }
}
