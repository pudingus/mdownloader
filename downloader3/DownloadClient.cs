using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Reflection;
using mshtml;

namespace downloader3
{
    public class DownloadClient
    {
        public delegate void DownloadError(DownloadClient client, MyData item, string message);
        public event DownloadError OnDownloadError;

        public delegate void DownloadCompleted(DownloadClient client, MyData item);
        public event DownloadCompleted OnDownloadCompleted;

        public delegate void DownloadInit(DownloadClient client, MyData item);
        public event DownloadInit OnDownloadInit;

        public long TotalBytes          { get; private set; }
        public long BytesDownloaded     { get; private set; }
        public long BytesPerSecond      { get; private set; }
        public string Directory         { get; private set; }
        public string FileName          { get; private set; }
        public string Url               { get; private set; }
        public int SpeedLimit           { get; set; } //v kilobajtech        
        public DCStates State           { get; private set; }
        public MyData Item              { get; set; }

        private const int chunkSize     = 1024;        
        private Thread downloadThread; 
        private int processed           = 0;
        private DispatcherTimer timer   = new DispatcherTimer();
        private FileStream fs;
        private AsyncOperation operation;
        private bool append;
        private System.Windows.Forms.WebBrowser webBrowser;
        private int index = 0;
        private bool found = false;

        public DownloadClient(string url, string directory, MyData item, long cachedTotalBytes, string cachedFileName)
        {
            append = true;
            Url = url;
            Directory = directory;
            FileName = cachedFileName;
            Item = item;
            TotalBytes = cachedTotalBytes;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);

            string path = Path.Combine(Directory, FileName);

            if (File.Exists(path))
            {
                FileInfo file = new FileInfo(path);
                BytesDownloaded = file.Length;
            }

            if (cachedTotalBytes == BytesDownloaded) State = DCStates.Completed;
            else State = DCStates.Paused;

            operation = AsyncOperationManager.CreateOperation(null);            
        }

        public DownloadClient(string url, string directory, MyData item)
        {
            Uri uri = new Uri(url);
            string filename = Path.GetFileName(uri.LocalPath);

            append = false;
            Url = url;
            Directory = directory;
            FileName = filename;
            Item = item;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);    

            string[] array = new string[] { "http.*://www.*.zippyshare.com/v/.*/file.html", "http.*://openload.co/f/.*" };

            
            while (index < array.Length && !found)
            {
                Regex regex = new Regex(array[index]);
                Match match = regex.Match(url);
                if (match.Success) found = true;                
                if (!found) index++;
            }

            if (found)
            {
                webBrowser = new System.Windows.Forms.WebBrowser();
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.Navigate(url);
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
            }

            operation = AsyncOperationManager.CreateOperation(null);
        }

        private void WebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {            
            string href = "";
            if (index == 0)
            {
                System.Windows.Forms.HtmlDocument document = webBrowser.Document;
                System.Windows.Forms.HtmlElement element = document.GetElementById("dlbutton");
                if (element != null)
                {
                    href = element.GetAttribute("href");
                }                                
            }
            else if (index == 1)
            {
                System.Windows.Forms.HtmlDocument document = webBrowser.Document;
                System.Windows.Forms.HtmlElement element = document.GetElementById("streamurj");
                if (element != null)
                {
                    string streamurj = element.InnerText;
                    href = "/stream/" + streamurj;
                }
            }
            Uri uri = new Uri(new Uri(webBrowser.Url.AbsoluteUri), href);

            Url = uri.AbsoluteUri;

            downloadThread = new Thread(DownloadWorker);
            downloadThread.Start();
        }


        public void Queue()
        {
            State = DCStates.Queue;
        }

        public void Start()
        {
            State = DCStates.Starting;
            timer.Start();
            
            if (!found)
            {
                if (downloadThread == null || downloadThread.IsAlive == false)
                {
                    downloadThread = new Thread(DownloadWorker);
                    downloadThread.Start();
                }
            }                       
        }   

        public void Cancel()
        {
            State = DCStates.Canceled;
        }

        public void Pause()
        {
            State = DCStates.Paused;
            timer.Stop();
        }        
        
        public void Rename(string newName)
        {     
            if (FileName != newName)
            {
                string newPath = Path.Combine(Directory, newName);
                string oldPath = Path.Combine(Directory, FileName);
                fs?.Close();
                if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                if (downloadThread != null)
                {
                    if (downloadThread.IsAlive) fs = new FileStream(newPath, FileMode.Append, FileAccess.Write, FileShare.Read, 65535);
                }
                FileName = newName;
            }            
        }

        private void DownloadWorker()
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(Url);
                request.Proxy = null;

                string path = Path.Combine(Directory, FileName);

                if (append && File.Exists(path))
                {
                    FileInfo file = new FileInfo(path);
                    BytesDownloaded = file.Length;
                }
                if (BytesDownloaded > 0) request.AddRange(BytesDownloaded);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (!append)
                {                    
                    string header = response.Headers["Content-Disposition"];
                    if (header != null)
                    {
                        FileName = header.Replace("attachment; ", "").Replace("attachment;", "").Replace("filename=", "").Replace("filename*=UTF-8''", "").Replace("\"", "");
                        path = Path.Combine(Directory, FileName);
                    }
                }                

                operation.Post(new SendOrPostCallback(delegate (object state)
                {
                    OnDownloadInit?.Invoke(this, Item);
                }), null);

                Stream receiveStream = response.GetResponseStream();

                if (append && TotalBytes != BytesDownloaded + response.ContentLength)
                {
                    State = DCStates.Error;
                    MessageBox.Show("file size mismatch");

                    operation.Post(new SendOrPostCallback(delegate (object state)
                    {
                        OnDownloadError?.Invoke(this, Item, "file size mismatch");
                    }), null);
                }
                else
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    if (BytesDownloaded > 0) fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 65535); //64kb buffer 
                    else fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65535); //64kb buffer

                    TotalBytes = BytesDownloaded + response.ContentLength;

                    byte[] read = new byte[chunkSize];
                    int count;

                    while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && State != DCStates.Canceled) //dokud není přečten celý stream
                    {
                        while (State == DCStates.Paused) Thread.Sleep(100);
                        State = DCStates.Downloading;

                        fs.Write(read, 0, count);
                        BytesDownloaded += count;
                        processed += count;

                        if ((SpeedLimit > 0) && (processed >= (SpeedLimit * 1024))) Thread.Sleep(1000);
                    }

                    response.Close();
                    fs.Close();
                    timer.Stop();
                    BytesPerSecond = 0;
                    if (State == DCStates.Canceled) File.Delete(path);
                    else
                    {
                        State = DCStates.Completed;
                        operation.Post(new SendOrPostCallback(delegate (object state)
                        {
                            OnDownloadCompleted?.Invoke(this, Item);
                        }), null);
                        operation.OperationCompleted();
                    }
                }
                
            }
            catch (WebException e)
            {  
                State = DCStates.Error;
                timer.Stop();
                MessageBox.Show("Message " + e.Message + "\n", "DownloadClient Error", MessageBoxButton.OK, MessageBoxImage.Error);
                operation.Post(new SendOrPostCallback(delegate (object state)
                {
                    OnDownloadError?.Invoke(this, Item, e.Message);
                }), null);
            }
            catch (ArgumentException e)
            {
                State = DCStates.Error;
                timer.Stop();
                MessageBox.Show("Message " + e.Message + "\n", "DownloadClient Error", MessageBoxButton.OK, MessageBoxImage.Error);
                operation.Post(new SendOrPostCallback(delegate (object state)
                {
                    OnDownloadError?.Invoke(this, Item, e.Message);
                }), null);
            }
            catch (IOException e)
            {
                State = DCStates.Error;
                timer.Stop();
                MessageBox.Show("Message " + e.Message + "\n", "DownloadClient Error", MessageBoxButton.OK, MessageBoxImage.Error);
                operation.Post(new SendOrPostCallback(delegate (object state)
                {
                    OnDownloadError?.Invoke(this, Item, e.Message);
                }), null);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            BytesPerSecond = processed;
            processed = 0;
        }
    }

    public enum DCStates { Paused, Queue, Starting, Downloading, Canceled, Completed, Error }
}