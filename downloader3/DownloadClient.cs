﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;

namespace downloader3
{
    /// <summary>
    /// Specifikuje stav stahování
    /// </summary>
    public enum States { Paused, Queue, Starting, Downloading, Canceled, Completed, Error }

    /// <summary>
    /// Má na starosti získávání dat ze serveru a ukládání na disk, poskytuje informace o průběhu a umožňuje ho ovládat.
    /// </summary>
    public class DownloadClient
    {
        public delegate void DownloadError(DownloadClient item, string message);
        public delegate void DownloadCompleted(DownloadClient item);
        public delegate void DownloadInit(DownloadClient item);
        public delegate void DownloadStateChanged(DownloadClient item, States oldState, States newState);


        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public string ErrorMsg { get; set; }


        /// <summary>
        /// Nastane když stahovaní přeruší chyba
        /// </summary>
        public event DownloadError OnDownloadError;

        /// <summary>
        /// Nastane když se stahování úspěšně dokončí
        /// </summary>
        public event DownloadCompleted OnDownloadCompleted;

        /// <summary>
        /// Nastane když se zahají stahování, po tom, co je soubor vytvořen na disku
        /// </summary>
        public event DownloadInit OnDownloadInit;

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
                        OnDownloadStateChanged?.Invoke(this, oldState, _state);
                    }), null);
                }
            }
        }
        //private Thread downloadThread;
        private Task task;

        private States _state;
        private int processed = 0;
        private DispatcherTimer timer = new DispatcherTimer();
        private FileStream fs;
        private AsyncOperation operation = AsyncOperationManager.CreateOperation(null);
        private bool append;


        private const int bufferSize = 1024;
        private const int maxRenameCount = 999;
        private const int requestTimeout = 6000;    //v milisekundách
        private const int browserTimeout = 6000;
        private const int updateInterval = 1000;
        private const int pauseSleep = 100;
        private const int speedlimitSleep = 200;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="DownloadClient"/>.
        /// </summary>
        /// <param name="url">Odkaz ze kterého se soubor stáhne.</param>
        /// <param name="directory">Úplná cesta ke složce, kde se soubor uloží.</param>
        public DownloadClient(string url, string directory)
        {
            append = false;
            Url = WebUtility.UrlDecode(url);
            Directory = directory;

            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, updateInterval);
        }


        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="DownloadClient"/> a načte informace o předchozím stahování.
        /// </summary>
        /// <param name="url">Odkaz ze kterého se soubor stáhne.</param>
        /// <param name="directory">Úplná cesta ke složce, kde se soubor uloží.</param>
        /// <param name="cachedTotalBytes">Očekávaná celková velikost soubor. Slouží ke kontrole, jestli nebyl soubor na serveru změněn.</param>
        /// <param name="cachedFileName">Název souboru, na který se má navázat</param>
        public DownloadClient(string url, string directory,
                              long cachedTotalBytes, string cachedFileName)
        {
            append = true;
            Url = WebUtility.UrlDecode(url);
            Directory = directory;
            TotalBytes = cachedTotalBytes;
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

            Resolver resolver = new Resolver(Url);
            resolver.OnExtractionCompleted += (extractedUrl) =>
            {
                Url = extractedUrl;

                if (task == null || task.IsCompleted)
                {
                    task = new Task(DownloadWorker, CancellationToken.None, TaskCreationOptions.LongRunning);
                    task.Start();

                }

            };
            resolver.Extract();
        }

        /// <summary>
        /// Zruší současné stahování a smaže soubor.
        /// </summary>
        public void Cancel()
        {
            State = States.Canceled;

            if (task == null || task.IsCompleted)
                if (File.Exists(FullPath))
                    File.Delete(FullPath);
        }

        /// <summary>
        /// Pozastaví stahování.
        /// </summary>
        public void Pause()
        {
            State = States.Paused;
            timer.Stop();
            BytesPerSecond = 0;
            processed = 0;
        }

        /// <summary>
        /// Zařadí stahování do fronty
        /// </summary>
        /// <remarks>
        /// Funguje podobně jako pozastavení. Slouží pro rozlišení stavu, jestli bylo stahování přerušeno uživatelem
        /// nebo programem, který spravuje položky ve frontě.
        /// </remarks>
        public void Queue()
        {
            State = States.Queue;
            timer.Stop();
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
                if (task != null)
                {
                    if (!task.IsCompleted)
                        fs = new FileStream(newPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                FileName = newName;
            }
        }

        //Zkratka pro vyvolání údalosti při chybě
        private void CallError(string message)
        {
            State = States.Error;
            operation.Post(new SendOrPostCallback(delegate (object state)
            {
                OnDownloadError?.Invoke(this, message);
            }), null);
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

                //když je FileName prázdný, ještě nebyl získan název souboru ze serveru, jinak se použije stávající
                if (FileName == "")
                {
                    string header = response.Headers["Content-Disposition"];
                    if (header != null)
                    {
                        string s = header.Replace("attachment; ", "").Replace("attachment;", "").Replace("filename=", "").Replace("filename*=UTF-8''", "").Replace("\"", "");

                        // z:  form%c3%a1ln%c3%ad%20%c3%baprava%20podle%20norem.docx
                        // na: formální úprava podle norem.docx

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
                        OnDownloadInit?.Invoke(this);
                    }), null);

                    byte[] buffer = new byte[bufferSize];
                    int receivedCount;
                    while ((receivedCount = receiveStream.Read(buffer, 0, bufferSize)) > 0 && State != States.Canceled) //dokud není přečten celý stream
                    {
                        fs.Write(buffer, 0, receivedCount);
                        BytesDownloaded += receivedCount;
                        processed += receivedCount;

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
                        OnDownloadCompleted?.Invoke(this);
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

        /// <summary>
        /// Načte ikonu souboru podle jeho typu
        /// </summary>
        public void LoadIcon()
        {
            if (Name == null) return;

            Icon icon;
            if (FileName != "" && File.Exists(FullPath))
                icon = ShellIcon.GetSmallIcon(FullPath);
            else
            {
                string ext = Path.GetExtension(Name);
                if (ext == "") ext = Name;
                icon = ShellIcon.GetSmallIconFromExtension(ext);
            }
            var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            icon.Dispose();
            Icon = bmpSrc;
        }

        /// <summary>
        /// Obnoví data položky
        /// </summary>
        public void Refresh()
        {
            if (FileName == "") Name = Url;
            else Name = FileName;
            if (TotalBytes > 0) Progress = (double)BytesDownloaded / TotalBytes * 100;
            Size = string.Format("{0}/{1}", Util.ConvertBytes(BytesDownloaded), Util.ConvertBytes(TotalBytes));
            if (State == States.Paused) Remaining = Lang.Translate("lang_paused");
            else if (State == States.Queue) Remaining = Lang.Translate("lang_inqueue");
            else if (State == States.Canceled) Remaining = Lang.Translate("lang_canceled");
            else if (State == States.Completed) Remaining = Lang.Translate("lang_completed");
            else if (State == States.Downloading)
            {
                long sec = 0;
                if (BytesDownloaded > 0 && BytesPerSecond > 0)
                    sec = (TotalBytes - BytesDownloaded) * 1 / BytesPerSecond;
                Remaining = Util.ConvertTime(sec);
            }
            else if (State == States.Error) Remaining = Lang.Translate("lang_error") + ": " + ErrorMsg;
            else if (State == States.Starting) Remaining = Lang.Translate("lang_starting");

            if (SpeedLimit > 0)
                Speed = string.Format("{0}/s [{1}/s]", Util.ConvertBytes(BytesPerSecond), Util.ConvertBytes(SpeedLimit));
            else Speed = string.Format("{0}/s", Util.ConvertBytes(BytesPerSecond));
        }
    }
}