using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace downloader3
{

    class Resolver
    {
        /// <summary>
        /// Specifikuje typ nebo poskytovatele odkazu
        /// </summary>
        public enum Providers { DirectLink, Zippyshare, Openload }

        public delegate void ExtractionCompleted(string url);
        public event ExtractionCompleted OnExtractionCompleted;

        public delegate void ExtractionError(string message);
        public event ExtractionError OnExtractionError;


        public Providers Provider { get; private set; }
        public string Url { get; private set; }

        private System.Windows.Forms.WebBrowser webBrowser;
        private DispatcherTimer wbTimer = new DispatcherTimer();
        private const int browserTimeout = 6000;

        public Resolver(string url)
        {
            Url = url;
            //Slovník podporovaných serverů. Klíč typu Providers udává název. Hodnota typu string udává formát regulérního výrazu

            var dict = new Dictionary<Providers, string>();
            dict.Add(Providers.Zippyshare, "http.*://www.*.zippyshare.com/v/.*/file.html");
            dict.Add(Providers.Openload, "http.*://openload.co/f/.*");

            Provider = Providers.DirectLink;
            foreach (var item in dict)
            {
                Regex regex = new Regex(item.Value);
                Match match = regex.Match(Url);
                if (match.Success)
                {
                    Provider = item.Key;
                    break;
                }
            }
        }

        public void Extract()
        {
            if (Provider != Providers.DirectLink) //jinak vytvoří nový objekt WebBrowser, který načte stránkou se současnou Url adresou
            {
                webBrowser = new System.Windows.Forms.WebBrowser();
                webBrowser.ScriptErrorsSuppressed = true; //potlačí vyskakovací okna
                webBrowser.Navigate(Url);
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;

                //spustí časovač, udává časový limit pro načtení stránky
                wbTimer.Interval = new TimeSpan(0, 0, 0, 0, browserTimeout);
                wbTimer.Tick += WbTimer_Tick;
                wbTimer.Start();
            }
            else
            {
                OnExtractionCompleted?.Invoke(Url);
            }
        }

        //Má na starosti extrakci přímých odkazů z podporovaných stránek
        private void ResolveLink()
        {
            string href = "";
            if (Provider == Providers.Zippyshare)
            {
                System.Windows.Forms.HtmlElement element = webBrowser.Document.GetElementById("dlbutton");
                if (element != null) href = element.GetAttribute("href");
                else
                {
                    OnExtractionError?.Invoke(Lang.Translate("lang_unable_to_extract"));
                    return;
                }
            }
            else if (Provider == Providers.Openload)
            {
                System.Windows.Forms.HtmlElement element = webBrowser.Document.GetElementById("streamurj");
                if (element != null) href = "/stream/" + element.InnerText;
                else
                {
                    OnExtractionError?.Invoke(Lang.Translate("lang_unable_to_extract"));
                    return;
                }
            }
            Uri uri = new Uri(new Uri(webBrowser.Url.AbsoluteUri), href);

            OnExtractionCompleted?.Invoke(WebUtility.UrlDecode(uri.AbsoluteUri));
        }

        //Nastane po úplném načtení stránky
        private void WebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            wbTimer.Stop();
            ResolveLink();
            webBrowser.Dispose();
        }

        //Nastane po vypršení časového limitu pro načtení stránky
        private void WbTimer_Tick(object sender, EventArgs e)
        {
            wbTimer.Stop();
            if (!webBrowser.IsDisposed)
            {
                webBrowser.Stop();
                ResolveLink();
                webBrowser.Dispose();
            }
        }

    }
}
