using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.ComponentModel;

namespace downloader3
{
    public class DownloadClient
    {
        public delegate void DownloadCompleted(DownloadClient client, MyData item);
        public event DownloadCompleted OnDownloadCompleted;

        public long TotalBytes          { get; private set; }
        public long BytesDownloaded     { get; private set; }
        public long BytesPerSecond      { get; private set; }
        public double SecondsRemaining  { get; private set; }
        public double Elapsed           { get { return elapsed; }}
        public string FilePath          { get; private set; }
        public string Url               { get; private set; }
        public int SpeedLimit           { get; set; } //v kilobajtech        
        public DCStates State           { get; private set; }
        public MyData Item              { get; set; }
        public bool Append              { get; private set; }

        private double elapsed          = 0;
        private const int chunkSize     = 1024;        
        private Thread downloadThread;             
        private bool rename;
        private string newPath;
        private int processed           = 0;
        private DispatcherTimer timer   = new DispatcherTimer();
        private Stopwatch sw            = new Stopwatch();
        private FileStream fs;
        private AsyncOperation operation;

        public DownloadClient(string url, string filePath, bool append, MyData item, long totalBytes)
        {            
            Url = url;
            FilePath = filePath;
            Item = item;
            Append = append;
            TotalBytes = totalBytes;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            downloadThread = new Thread(DownloadWorker);

            if (append && File.Exists(filePath))
            {
                FileInfo file = new FileInfo(filePath);
                BytesDownloaded = file.Length;
            }

            if (totalBytes == BytesDownloaded) State = DCStates.Completed;
            else State = DCStates.Paused;

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
            rename = true;
            newPath = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + newName;
        }

        private void DownloadWorker()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Proxy = null;

                if (Append && File.Exists(FilePath))
                {
                    FileInfo file = new FileInfo(FilePath);
                    BytesDownloaded = file.Length;
                }
                if (BytesDownloaded > 0)
                {
                    request.AddRange(BytesDownloaded);
                    fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Write, 65535); //64kb buffer    
                }
                else fs = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 65535); //64kb buffer   
                
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                TotalBytes = BytesDownloaded + response.ContentLength;
                byte[] read = new byte[chunkSize];
                int count;                

                while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && State != DCStates.Canceled) //dokud není přečten celý stream
                {
                    while (State == DCStates.Paused) Thread.Sleep(100);
                    if (rename)
                    {
                        fs.Close();
                        File.Move(FilePath, newPath);
                        FilePath = newPath;
                        fs = new FileStream(FilePath, FileMode.Append);
                        rename = false;
                    }

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
                if (State == DCStates.Canceled) File.Delete(FilePath);                
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
            catch (WebException)
            {                
                State = DCStates.Error;
                timer.Stop();
                sw.Reset();
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