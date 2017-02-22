using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace downloader3
{
    class DownloadClient
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

        private const int chunkSize = 1024;
        private string url;
        private string filename;
        private Thread downloadThread; 
        private DispatcherTimer timer, timer2;
        private bool cancel = false;
        private bool pause = false;
        private long persec;
        private Stopwatch sw;     
        

        public DownloadClient(string url, string filename)
        {
            this.url = url;
            this.filename = filename;
            timer = new DispatcherTimer();
            timer2 = new DispatcherTimer();
            sw = new Stopwatch();
        }

        public void Start()
        {
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500); //500 ms            
            timer.Start();

            timer2.Tick += new EventHandler(Timer2_Tick);
            timer2.Interval = new TimeSpan(0, 0, 1); //1 s            
            timer2.Start();
            sw.Start();
            downloadThread = new Thread(downloadWorker);
            downloadThread.Start();            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (BytesDownloaded > 0) SecondsRemaining = (FileSize - BytesDownloaded) * sw.Elapsed.TotalSeconds / BytesDownloaded;
            OnDownloadProgressChanged(this);            
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            BytesPerSecond = persec;
            persec = 0;
        }

        public void Cancel()
        {
            cancel = true;
        }

        public void Pause()
        {
            pause = true;
            sw.Stop();
            timer.Stop();
            timer2.Stop();
        }

        public void Resume()
        {
            pause = false;
            sw.Start();
            timer.Start();
            timer2.Start();
        }        

        private void downloadWorker()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream receiveStream = response.GetResponseStream();

                FileSize = response.ContentLength;

                byte[] read = new byte[chunkSize];
                int count;

                while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && !cancel) //dokud není přečten celý stream
                {
                    fs.Write(read, 0, count);                                    

                    BytesDownloaded += count;
                    persec += count;
                    Percentage = ((float)BytesDownloaded / (float)FileSize) * 100;

                    while (pause) Thread.Sleep(100);
                }
                fs.Close();
                timer.Stop();
                sw.Reset();
                OnDownloadProgressCompleted(this, cancel);
                if (cancel) File.Delete(filename);
            }
        }

    }
}
