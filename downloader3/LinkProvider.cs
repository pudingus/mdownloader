using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace downloader3
{
    public static class LinkProvider
    {
        static WebBrowser webBrowser;
        static int index = 0;
        public static void GetDirectLink(string url)
        {  
            string[] array = new string[] { "http*://www*.zippyshare.com/v/*/file.html", "http*://openload.co/f/*" };

            int i = 0;
            bool found = false;
            while (i < array.Length && !found)
            {
                Regex regex = new Regex(array[i]);
                Match match = regex.Match(url);
                if (match.Success)
                {                    
                    found = true;
                    index = i;
                }
                i++;
            }

            if (found)
            {
                WebBrowser webBrowser = new WebBrowser();
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                webBrowser.Navigate(url);
            }
        }

        private static void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlDocument document = webBrowser.Document;
            HtmlElement element = document.GetElementById("dlbutton");
            string href = element.GetAttribute("href");
            Uri uri = new Uri(new Uri(webBrowser.Url.AbsoluteUri), href);
        }

        public enum Providers { None, Zippyshare, Openload };
    }
}
