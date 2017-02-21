using System;
using System.Collections.Generic;
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
        public float Percentage { get; set; }


        private const int chunkSize = 1024;
        private string url;
        private string filename;
        private Thread downloadThread; 
        private DispatcherTimer timer;
        private bool doDownload = true;

        public DownloadClient(string url, string filename)
        {
            this.url = url;
            this.filename = filename;
            timer = new DispatcherTimer();
        }

        public void Start()
        {
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100); //500 ms            
            timer.Start();
            downloadThread = new Thread(downloadWorker);
            downloadThread.Start();            
        }

        public void Cancel()
        {
            doDownload = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            OnDownloadProgressChanged(this);
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

                while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && doDownload) //dokud není přečten celý stream
                {
                    fs.Write(read, 0, count);                                    

                    BytesDownloaded += count;
                    Percentage = ((float)BytesDownloaded / (float)FileSize) * 100;
                }
                fs.Close();
                timer.Stop();
                OnDownloadProgressCompleted(this, !doDownload);
                if (!doDownload) File.Delete(filename);
            }
        }

    }
}
