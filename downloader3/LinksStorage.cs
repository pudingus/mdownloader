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
    public class Link
    {        
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string Url { get; set; }
        public long TotalBytes { get; set; }
        public int SpeedLimit { get; set; }
        public bool Completed { get; set; }
    }

    [Serializable]
    public class LinksStorage
    {
        public List<Link> List = new List<Link>();

        private string filename = "links.xml";

        public LinksStorage Load()
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(LinksStorage));
                return xmls.Deserialize(sr) as LinksStorage;
            }
        }

        public void Save()
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(LinksStorage));
                xmls.Serialize(sw, this);
            }
        }
    }
}
