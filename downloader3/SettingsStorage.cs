using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace downloader3
{
    [Serializable]
    public class SettingsStorage
    {
        public int SpeedLimit { get; set; } = 2000;
        public string Language { get; set; } = "cs-CZ";
        public int MaxDownloads { get; set; } = 5;

        private string filename = "settings.xml";

        public SettingsStorage Load()
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                return xmls.Deserialize(sr) as SettingsStorage;
            }
        }

        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                xmls.Serialize(sw, this);
            }
        }
    }
}
