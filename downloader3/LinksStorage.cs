using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace downloader3
{
    /// <summary>
    /// Představuje uložená data pro jednotlivou položku
    /// </summary>
    [Serializable]
    public class Link
    {        
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string Url { get; set; }
        public long TotalBytes { get; set; }
        public long SpeedLimit { get; set; }
    }

    /// <summary>
    /// Představuje úložiště odkazů
    /// </summary>
    [Serializable]
    public class LinksStorage
    {
        /// <summary>
        /// Představuje seznam odkazů
        /// </summary>
        public List<Link> List = new List<Link>();

        private static string filename = "links.xml";
        private static string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string path = Path.Combine(appdata, App.appName, filename);

        /// <summary>
        /// Načte seznam odkazů
        /// </summary>
        /// <returns></returns>
        public LinksStorage Load()
        {
            LinksStorage linksStorage = this;
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    XmlSerializer xmls = new XmlSerializer(typeof(LinksStorage));
                    linksStorage = xmls.Deserialize(sr) as LinksStorage;
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return linksStorage;
        }

        /// <summary>
        /// Uloží seznam odkazů
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    XmlSerializer xmls = new XmlSerializer(typeof(LinksStorage));
                    xmls.Serialize(sw, this);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
