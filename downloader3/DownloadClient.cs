using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Threading;

namespace downloader3
{
    internal class DownloadClient
    {
        public delegate void DownloadProgressChanged(object sender);

        public event DownloadProgressChanged OnDownloadProgressChanged;

        public delegate void DownloadProgressCompleted(object sender, bool cancel);

        public event DownloadProgressCompleted OnDownloadProgressCompleted;

        public long FileSize { get; private set; }
        public long BytesDownloaded { get; private set; }
        public int Index { get; set; }
        public float Percentage { get; private set; }
        public long BytesPerSecond { get; private set; }
        public double SecondsRemaining { get; private set; }
        public int SpeedLimit { get; set; } //v kilobajtech
        public bool Paused { get; private set; }
        public bool Canceled { get; private set; }
        public bool Completed { get; private set; }
        public string FilePath { get; private set; }
        public string Url { get; private set; }

        private const int chunkSize = 1024;
        private long processed;
        private Thread downloadThread;
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timer2 = new DispatcherTimer();
        private Stopwatch sw = new Stopwatch();
        private AutoResetEvent wh = new AutoResetEvent(true);
        private bool changeName;
        private string newPath;

        public DownloadClient(string url, string filePath)
        {
            Url = url;
            FilePath = filePath;
        }

        public void Start()
        {
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000); //500 ms
            timer.Start();

            timer2.Tick += new EventHandler(Timer2_Tick);
            timer2.Interval = new TimeSpan(0, 0, 1); //1 s
            timer2.Start();
            sw.Start();
            downloadThread = new Thread(DownloadWorker);
            downloadThread.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (BytesDownloaded > 0) SecondsRemaining = (FileSize - BytesDownloaded) * sw.Elapsed.TotalSeconds / BytesDownloaded;
            OnDownloadProgressChanged(this);
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            wh.Set();
            BytesPerSecond = processed;
            processed = 0;
        }

        public void Cancel()
        {
            Canceled = true;
            Paused = false;
        }

        public void Pause()
        {
            Paused = true;
            sw.Stop();
            timer.Stop();
            timer2.Stop();
        }

        public void Resume()
        {
            Paused = false;
            sw.Start();
            timer.Start();
            timer2.Start();
        }

        public void Rename(string newName)
        {
            changeName = true;
            newPath = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + newName;
        }

        private void DownloadWorker()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 65535); //64kb buffer
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream receiveStream = response.GetResponseStream();

                FileSize = response.ContentLength;

                byte[] read = new byte[chunkSize];
                int count;

                while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && !Canceled) //dokud není přečten celý stream
                {
                    if (changeName)
                    {
                        fs.Close();
                        File.Move(FilePath, newPath);
                        FilePath = newPath;
                        fs = new FileStream(FilePath, FileMode.Append);
                        changeName = false;
                    }

                    fs.Write(read, 0, count);

                    BytesDownloaded += count;
                    processed += count;
                    Percentage = ((float)BytesDownloaded / (float)FileSize) * 100;

                    if (processed >= (SpeedLimit * 1024)) wh.WaitOne();

                    while (Paused) Thread.Sleep(100);
                }
                fs.Close();
                timer.Stop();
                sw.Reset();
                OnDownloadProgressCompleted(this, Canceled);
                if (Canceled) File.Delete(FilePath);
                else Completed = true;
            }
        }
    }
}