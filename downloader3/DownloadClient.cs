using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace downloader3
{
    /// <summary>
    /// Specifikuje stav stahování
    /// </summary>
    public enum States { Paused, Queue, Starting, Downloading, Canceled, Completed, Error }

    /// <summary>
    /// Specifikuje typ nebo poskytovatele odkazu
    /// </summary>
    public enum Providers { DirectLink, Zippyshare, Openload }

    /// <summary>
    /// Má na starosti získávání dat ze serveru a ukládání na disk, poskytuje informace o průběhu a umožňuje ho ovládat.
    /// </summary>
    public class DownloadClient
    {        
        public delegate void DownloadError(DownloadClient client, LvData item, string message);
        /// <summary>
        /// Nastane když stahovaní přeruší chyba
        /// </summary>
        public event DownloadError OnDownloadError;

        public delegate void DownloadCompleted(DownloadClient client, LvData item);
        /// <summary>
        /// Nastane když se stahování úspěšně dokončí
        /// </summary>
        public event DownloadCompleted OnDownloadCompleted;

        public delegate void DownloadInit(DownloadClient client, LvData item);
        /// <summary>
        /// Nastane když se zahají stahování, po tom, co je soubor vytvořen na disku
        /// </summary>
        public event DownloadInit OnDownloadInit;

        public delegate void DownloadStateChanged(DownloadClient client, LvData item, States oldState, States newState);
        /// <summary>
        /// Nastane když se změní stav stahování
        /// </summary>
        public event DownloadStateChanged OnDownloadStateChanged;

         
        /// <summary>
        /// Získá celkovou velikost souboru v bajtech
        /// </summary>
        public long TotalBytes          { get; private set; }

        /// <summary>
        /// Získá počet stažených bajtů
        /// </summary>
        public long BytesDownloaded     { get; private set; }

        /// <summary>
        /// Získá rychlost stahování v bajtech za sekundu
        /// </summary>
        public long BytesPerSecond      { get; private set; }

        /// <summary>
        /// Získá umístění složky, kde bude soubor uložený
        /// </summary>
        public string Directory         { get; private set; }

        /// <summary>
        /// Získá název souboru
        /// </summary>
        public string FileName          { get; private set; } = "";

        /// <summary>
        /// Získá Url adresu odkazu na stahovaný soubor
        /// </summary>
        public string Url               { get; private set; }

        /// <summary>
        /// Získá nebo nastaví rychlostní limit v bajtech
        /// </summary>
        public long SpeedLimit          { get; set; }


        /// <summary>
        /// Získá úplnou cestu ke stahovanému souboru
        /// </summary>
        public string FullPath          { get { return Path.Combine(Directory, FileName); } }

        /// <summary>
        /// Získá počet aktivních stahování
        /// </summary>
        public static int ActiveCount   { get; private set; }

        /// <summary>
        /// Získá stav současného stahování
        /// </summary>        
        public States State {
            get {
                return _state;
            }

            private set {

                if ((_state != States.Downloading && _state != States.Starting) &&
                    (value == States.Downloading || value == States.Starting)) ActiveCount++;

                if ((value != States.Downloading && value != States.Starting) &&
                    (_state == States.Downloading || _state == States.Starting)) ActiveCount--;

                if (_state != value)
                {
                    /* je nutné vytvořit pomocnou proměnou, a 'value' načíst do '_state' před voláním události,
                     * protože by jinak funkce v události pracovali se starou hodnotou */
                    States oldState = _state;
                    _state = value;

                    operation.Post(new SendOrPostCallback(delegate (object state)
                    {
                        OnDownloadStateChanged?.Invoke(this, Item, oldState, _state);
                    }), null);
                }
            }
        }
        private States _state;

        private LvData Item { get; set; }


        private Thread downloadThread; 
        private int processed = 0;
        private DispatcherTimer timer = new DispatcherTimer();
        private FileStream fs;
        private AsyncOperation operation = AsyncOperationManager.CreateOperation(null);
        private bool append;
        private System.Windows.Forms.WebBrowser webBrowser;
        private DispatcherTimer wbTimer = new DispatcherTimer();
        private Providers provider;

        private const int chunkSize = 1024;
        private const int maxRenameCount = 999;
        private const int requestTimeout = 6000;    //v milisekundách
        private const int browserTimeout = 6000;
        private const int updateInterval = 1000;        
        private const int pauseSleep = 100;
        private const int speedlimitSleep = 200;


        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="DownloadClient"/> a načte informace o předchozím stahování.
        /// </summary>
        /// <param name="url">Odkaz ze kterého se soubor stáhne.</param>
        /// <param name="directory">Úplná cesta ke složce, kde se soubor uloží.</param>
        /// <param name="item">Reference na položku v listview, která obsahuje tento objekt.</param>
        /// <param name="cachedTotalBytes">Očekávaná celková velikost soubor. Slouží ke kontrole, jestli nebyl soubor na serveru změněn.</param>
        /// <param name="cachedFileName">Název souboru, na který se má navázat</param>
        public DownloadClient(string url, string directory, LvData item, long cachedTotalBytes, string cachedFileName)
        {
            append = true;
            Url = url;
            Directory = directory;
            TotalBytes = cachedTotalBytes;
            Item = item;
            FileName = cachedFileName;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, updateInterval);

            if (File.Exists(FullPath))
            {
                FileInfo file = new FileInfo(FullPath);
                BytesDownloaded = file.Length;
            }

            if (BytesDownloaded == TotalBytes && TotalBytes > 0) State = States.Completed;
            else State = States.Paused;
        }

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="DownloadClient"/>.
        /// </summary>
        /// <param name="url">Odkaz ze kterého se soubor stáhne.</param>
        /// <param name="directory">Úplná cesta ke složce, kde se soubor uloží.</param>
        /// <param name="item">Reference na položku v listview, která obsahuje tento objekt.</param>
        public DownloadClient(string url, string directory, LvData item)
        {    
            append = false;
            Url = url;
            Directory = directory;
            Item = item;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, updateInterval);
        }        

        /// <summary>
        /// Zahájí stahování bez blokování současného vlákna
        /// </summary>
        /// <remarks>
        /// Metoda také zjištuje, jestli použitá url adresa neodpovídá formátu
        /// podporovaných serverů pro extrakci přímého odkazu z HTML kódu stránky.
        /// </remarks>
        public void Start()
        {
            timer.Start();
            State = States.Starting;

            //Slovník podporovaných serverů. Klíč typu Providers udává název. Hodnota typu string udává formát regulérního výrazu            
            var dict = new Dictionary<Providers, string>();
            dict.Add(Providers.Zippyshare, "http.*://www.*.zippyshare.com/v/.*/file.html");
            dict.Add(Providers.Openload, "http.*://openload.co/f/.*");

            provider = Providers.DirectLink;
            foreach (var item in dict)
            {
                Regex regex = new Regex(item.Value);
                Match match = regex.Match(Url);
                if (match.Success)
                {
                    provider = item.Key;
                    break;
                }
            }             

            if (provider == Providers.DirectLink) //pokud se jedná o přímých odkaz, rovnou se vytvoří vlákno a spustí stahování
            {
                if (downloadThread == null || !downloadThread.IsAlive)
                {
                    downloadThread = new Thread(DownloadWorker);
                    downloadThread.Start();
                }
            }
            else //jinak vytvoří nový objekt WebBrowser, který načte stránkou se současnou Url adresou
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
        }

        /// <summary>
        /// Zruší současné stahování a smaže soubor, pokud existuje.
        /// </summary>
        public void Cancel()
        {
            State = States.Canceled;
            if (downloadThread == null || !downloadThread.IsAlive)
            {
                if (File.Exists(Path.Combine(Directory, FileName)))
                    File.Delete(Path.Combine(Directory, FileName));
            }
        }

        /// <summary>
        /// Pozastaví stahování.
        /// </summary>
        public void Pause()
        {
            State = States.Paused;
            timer.Stop();
            wbTimer.Stop();
            webBrowser?.Stop();
            BytesPerSecond = 0;
            processed = 0;
        }

        /// <summary>
        /// Zařadí stahování do fronty
        /// </summary>
        /// <remarks>
        /// Funguje podobně jako pozastavení. Slouží pro rozlišení, jestli bylo stahování přerušeno uživatelem
        /// nebo programem, který spravuje položky ve frontě.
        /// </remarks>
        public void Queue()
        {
            State = States.Queue;
            timer.Stop();
            wbTimer.Stop();
            webBrowser?.Stop();
            BytesPerSecond = 0;
            processed = 0;
        }

        /// <summary>
        /// Přejmenuje soubor na disku.
        /// </summary>
        /// <param name="newName">Nový název souboru</param>
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
                    if (downloadThread.IsAlive) fs = new FileStream(newPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                FileName = newName;
            }
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

        //Zkratka pro vyvolání údalosti při chybě
        private void CallError(string message)
        {
            State = States.Error;
            operation.Post(new SendOrPostCallback(delegate (object state)
            {
                OnDownloadError?.Invoke(this, Item, message);
            }), null);
        }

        //Má na starosti extrakci přímých odkazů z podporovaných stránek
        private void ResolveLink()
        {
            string href = "";
            if (provider == Providers.Zippyshare)
            {
                System.Windows.Forms.HtmlElement element = webBrowser.Document.GetElementById("dlbutton");
                if (element != null)
                {
                    href = element.GetAttribute("href");
                }
                else
                {
                    CallError(Lang.Translate("lang_unable_to_extract"));
                    return;
                }

            }
            else if (provider == Providers.Openload)
            {
                System.Windows.Forms.HtmlElement element = webBrowser.Document.GetElementById("streamurj");
                if (element != null)
                {
                    string streamurj = element.InnerText;
                    href = "/stream/" + streamurj;
                }
                else
                {
                    CallError(Lang.Translate("lang_unable_to_extract"));
                    return;
                }
            }
            Uri uri = new Uri(new Uri(webBrowser.Url.AbsoluteUri), href);
            Url = WebUtility.UrlDecode(uri.AbsoluteUri);            

            downloadThread = new Thread(DownloadWorker);
            downloadThread.Start();
        }                

        //Obsluhuje čtení dat ze serveru a zápis na disk. Běží v samostatném vlákně.
        private void DownloadWorker()
        {
            HttpWebResponse response = null;
            Stream receiveStream = null;
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(Url);
                request.Proxy = null;
                request.Timeout = requestTimeout;

                if (append)
                {
                    if (File.Exists(FullPath))
                    {
                        FileInfo file = new FileInfo(FullPath);
                        BytesDownloaded = file.Length;
                    }
                    else BytesDownloaded = 0;
                }
                if (BytesDownloaded > 0) request.AddRange(BytesDownloaded);
                
                response = (HttpWebResponse)request.GetResponse();

                //když je FileName prázdný, ještě jsme nezískali jméno ze serveru, jinak použít stávající
                if (FileName == "")
                {                    
                    string header = response.Headers["Content-Disposition"];
                    if (header != null)
                    {
                        string s = header.Replace("attachment; ", "").Replace("attachment;", "").Replace("filename=", "").Replace("filename*=UTF-8''", "").Replace("\"", "");
                        FileName = WebUtility.UrlDecode(s);                        
                    }
                    else
                    {
                        Uri uri = new Uri(Url);
                        FileName = Path.GetFileName(uri.LocalPath);                        
                    }

                    //pokud již existuje soubor se stejným jménem, přidá se číslo               
                    int i = 1;
                    string baseName = FileName;
                    while (File.Exists(FullPath) && i <= maxRenameCount)
                    {
                        FileName = $"{Path.GetFileNameWithoutExtension(baseName)} ({i}){Path.GetExtension(baseName)}";
                        i++;
                    }
                    if (File.Exists(FullPath)) //pokud existuje i 999. soubor, vyhodí se error
                    {
                        CallError(Lang.Translate("lang_file_exists"));
                        return;
                    }
                }                 

                receiveStream = response.GetResponseStream();                

                //když se navazuje stahování, ale celková velikost souboru se liší - soubor na serveru byl změněn,
                //nebo odkaz přestal platit
                if (append && TotalBytes > 0 && TotalBytes != BytesDownloaded + response.ContentLength)
                {
                    CallError(Lang.Translate("lang_file_size_mismatch"));
                }
                else
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    if (BytesDownloaded > 0) fs = new FileStream(FullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    else fs = new FileStream(FullPath, FileMode.Create, FileAccess.Write, FileShare.Read);

                    TotalBytes = BytesDownloaded + response.ContentLength;

                    operation.Post(new SendOrPostCallback(delegate (object state)
                    {
                        OnDownloadInit?.Invoke(this, Item);
                    }), null);

                    byte[] read = new byte[chunkSize];
                    int count;                    

                    while ((count = receiveStream.Read(read, 0, chunkSize)) > 0 && State != States.Canceled) //dokud není přečten celý stream
                    {   
                        fs.Write(read, 0, count);
                        BytesDownloaded += count;
                        processed += count;

                        while (State == States.Paused || State == States.Queue) Thread.Sleep(pauseSleep);
                        if (State == States.Starting) State = States.Downloading;

                        if (SpeedLimit > 0 && processed >= SpeedLimit) Thread.Sleep(speedlimitSleep);
                    }                    
                }                
            }            
            catch (WebException e) //server neexistuje; errory 400, 500;
            {
                CallError(e.Message);
            }
            catch (ArgumentException e) 
            {
                CallError(e.Message);
            }
            catch (IOException e) //k souboru nelze přistupovat
            {
                CallError(e.Message);
            }
            catch (Exception e)
            {
                CallError(e.Message);
            }
            finally
            {
                response?.Close();
                receiveStream?.Close();
                fs?.Close();
                timer.Stop();
                BytesPerSecond = 0;
                processed = 0;

                if (State == States.Canceled) File.Delete(FullPath);
                else if (BytesDownloaded == TotalBytes && TotalBytes > 0)
                {
                    State = States.Completed;
                    operation.Post(new SendOrPostCallback(delegate (object state)
                    {
                        OnDownloadCompleted?.Invoke(this, Item);
                    }), null);
                }
                else if (State != States.Error)
                {
                    Console.WriteLine("restarting");
                    State = States.Starting;
                    timer.Start();
                    DownloadWorker();
                }
            }            
        }

        //proběhně každou sekundu (updateInterval), aktualizuje rychlost stahování
        private void Timer_Tick(object sender, EventArgs e)
        {
            BytesPerSecond = processed;
            processed = 0;
        }
    }
}