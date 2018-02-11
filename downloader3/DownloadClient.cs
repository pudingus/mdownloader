using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows;

namespace downloader3
{
    public class DownloadClient
    {
        public delegate void DownloadCompleted(DownloadClient client, MyData item);
        public event DownloadCompleted OnDownloadCompleted;

        public delegate void DownloadInit(DownloadClient client, MyData item);
        public event DownloadInit OnDownloadInit;

        public long TotalBytes          { get; private set; }
        public long BytesDownloaded     { get; private set; }
        public long BytesPerSecond      { get; private set; }
        public double SecondsRemaining  { get; private set; }
        public double Elapsed           { get { return elapsed; }}
        public string Directory         { get; private set; }
        public string FileName          { get; private set; }
        public string Url               { get; private set; }
        public int SpeedLimit           { get; set; } //v kilobajtech        
        public DCStates State           { get; private set; }
        public MyData Item              { get; set; }

        private double elapsed          = 0;
        private const int chunkSize     = 1024;        
        private Thread downloadThread; 
        private int processed           = 0;
        private DispatcherTimer timer   = new DispatcherTimer();
        private Stopwatch sw            = new Stopwatch();
        private FileStream fs;
        private AsyncOperation operation;
        private bool append;

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
            downloadThread = new Thread(DownloadWorker);

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
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            append = false;
            Url = url;
            Directory = directory;
            FileName = filename;
            Item = item;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            downloadThread = new Thread(DownloadWorker);          

            operation = AsyncOperationManager.CreateOperation(null);
        }

        public void Queue()
        {
            State = DCStates.Queue;
        }

        public void Start()
        {
            State = DCStates.Downloading;
            timer.Start();
            sw.Start();
            if (!downloadThread.IsAlive) downloadThread.Start();
        }   

        public void Cancel()
        {
            State = DCStates.Canceled;
        }

        public void Pause()
        {
            State = DCStates.Paused;
            timer.Stop();
            sw.Stop();
        }        
        
        public void Rename(string newName)
        {     
            string newPath = Path.Combine(Directory, newName);
            string oldPath = Path.Combine(Directory, FileName);
            FileName = newName;
            fs?.Close();
            File.Move(oldPath, newPath);
            if (downloadThread.IsAlive) fs = new FileStream(newPath, FileMode.Append, FileAccess.Write, FileShare.Read, 65535);
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

                System.IO.Directory.CreateDirectory(Directory);
                if (BytesDownloaded > 0) fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 65535); //64kb buffer 
                else fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65535); //64kb buffer

                

                operation.Post(new SendOrPostCallback(delegate (object state)
                {
                    OnDownloadInit?.Invoke(this, Item);
                }), null);

                Stream receiveStream = response.GetResponseStream();
                TotalBytes = BytesDownloaded + response.ContentLength;
                byte[] read = new byte[chunkSize];
                int count;                

                while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && State != DCStates.Canceled) //dokud není přečten celý stream
                {
                    while (State == DCStates.Paused) Thread.Sleep(100);                   

                    fs.Write(read, 0, count);                    
                    BytesDownloaded += count;
                    processed += count;
                    
                    if ((SpeedLimit > 0) && (processed >= (SpeedLimit * 1024))) Thread.Sleep(1000);                    
                    elapsed = sw.Elapsed.TotalSeconds;
                }                

                response.Close();
                fs.Close();
                timer.Stop();
                sw.Reset();
                BytesPerSecond = 0; 
                if (State == DCStates.Canceled) File.Delete(path);                
                else
                {
                    operation.Post(new SendOrPostCallback(delegate (object state)
                    {
                        OnDownloadCompleted?.Invoke(this, Item);
                    }), null);
                    operation.OperationCompleted();

                    State = DCStates.Completed;
                }
            }
            catch (WebException e)
            {                
                State = DCStates.Error;
                timer.Stop();
                sw.Reset();
                MessageBox.Show("Message " + e.Message + "\n" +
                                "HResult " + e.HResult + "\n" +
                                "Response " + e.Response + "\n" + 
                                "Source " + e.Source + "\n" +
                                "TargetSite " + e.TargetSite + "\n" +
                                "Data " + e.Data.ToString() + "\n", "DownloadClient Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            BytesPerSecond = processed;
            processed = 0;
        }
    }

    public enum DCStates { Paused, Queue, Downloading, Canceled, Completed, Error }
}