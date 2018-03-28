using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace downloader3
{
    [Serializable]
    public class SettingsStorage
    {
        public long SpeedLimit          { get; set; } = 0;
        public string Language          { get; set; } = "cs-CZ";
        public int MaxDownloads         { get; set; } = 2;
        public bool ShowNotification    { get; set; } = true;
        public bool PlaySound           { get; set; } = true;

        private static string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string path = Path.Combine(appdata, App.appName);
        private static string filepath = Path.Combine(path, "settings.xml");

        /// <summary>
        /// Načte nastavení
        /// </summary>
        /// <returns></returns>
        public SettingsStorage Load()
        {
            SettingsStorage storage = this;            
            try
            {
                Directory.CreateDirectory(path);
                
                using (StreamReader sr = new StreamReader(filepath))
                {
                    XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                    storage = xmls.Deserialize(sr) as SettingsStorage;
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

            return storage;
        }

        /// <summary>
        /// Uloží nastavení
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filepath))
                {
                    XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
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
