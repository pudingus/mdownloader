using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
//using System.Windows.Forms;
using System.Text.RegularExpressions;
using mshtml;
using System.Runtime.InteropServices;

namespace webtest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        /*WebBrowser webBrowser;

        public static string FetchHTML(string sUrl)
        {
            System.Net.WebClient oClient = new System.Net.WebClient();
            return oClient.DownloadString(sUrl);
            //return new System.Text.UTF8Encoding().GetString(oClient.DownloadData(sUrl)); 
        }

        public static string FetchTitleFromHTML(string sHtml)
        {
            string regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";
            System.Text.RegularExpressions.Regex ex = new System.Text.RegularExpressions.Regex(regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return ex.Match(sHtml).Value.Trim();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string url = "http://www50.zippyshare.com/v/1VHXCUpf/file.html";

            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();

            System.Net.WebClient oClient = new System.Net.WebClient();
            string html = oClient.DownloadString(url);

            //html.Replace("")

            //string q = Regex.Replace(html, @"<img>(.*?)<\/img>", "$1");


            webBrowser = new WebBrowser();
            webBrowser.DocumentText = html;
            webBrowser.Url = new Uri("http://www50.zippyshare.com/v/1VHXCUpf/file.html");
            webBrowser.ScriptErrorsSuppressed = true;
            webBrowser.Navigated += WebBrowser_Navigated;
            

            host.Child = webBrowser;




            this.grid.Children.Add(host);
        }

        private void WebBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            //throw new NotImplementedException();

        }*/



        const int FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
        const int SET_FEATURE_ON_PROCESS = 0x00000002;

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int FeatureEntry,
                                                      [MarshalAs(UnmanagedType.U4)] int dwFlags,
                                                      bool fEnable);

        static void DisableClickSounds()
        {
            CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS,
                                        SET_FEATURE_ON_PROCESS,
                                        true);
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisableClickSounds();
        }

        
        private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            //MessageBox.Show("done");
            //webBrowser.Sc

            //zippyshare
            HTMLDocument document = webBrowser.Document as HTMLDocument;
            IHTMLElement element = document.getElementById("dlbutton");
            string href = element.getAttribute("href");


            //openload
            /*HTMLDocument document = webBrowser.Document as HTMLDocument;
            IHTMLElement element = document.getElementById("streamurj");
            string streamurj = element.innerText;
            string href = "/stream/" + streamurj;*/


            Uri uri = new Uri(new Uri(webBrowser.Source.AbsoluteUri), href);

            textBox.Text = uri.AbsoluteUri;
        }

        private void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            dynamic activeX = this.webBrowser.GetType().InvokeMember("ActiveXInstance",
            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null, this.webBrowser, new object[] { });

            activeX.Silent = true;

        }
    }
}
