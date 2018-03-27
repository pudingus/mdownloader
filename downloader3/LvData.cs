using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace downloader3
{
    public class LvData
    {
        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public string Directory { get; set; }
        public DownloadClient Client { get; set; }
        public string ErrorMsg { get; set; }

        public void LoadIcon()
        {
            if (Client == null) return;
            else if (Name == null) return;

            Icon icon;
            if (Client.FileName != "" && File.Exists(Client.FullPath))
                icon = ShellIcon.GetSmallIcon(Client.FullPath);
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

        public void Refresh()
        {
            if (Client.FileName == "") Name = Client.Url;
            else Name = Client.FileName;
            if (Client.TotalBytes > 0) Progress = (double)Client.BytesDownloaded / Client.TotalBytes * 100;
            Size = string.Format("{0}/{1}", Util.ConvertBytes(Client.BytesDownloaded), Util.ConvertBytes(Client.TotalBytes));
            if (Client.State == States.Paused) Remaining = Lang.Translate("lang_paused");
            else if (Client.State == States.Queue) Remaining = Lang.Translate("lang_inqueue");
            else if (Client.State == States.Canceled) Remaining = Lang.Translate("lang_canceled");
            else if (Client.State == States.Completed) Remaining = Lang.Translate("lang_completed");
            else if (Client.State == States.Downloading)
            {
                long sec = 0;
                if (Client.BytesDownloaded > 0 && Client.BytesPerSecond > 0)
                    sec = (Client.TotalBytes - Client.BytesDownloaded) * 1 / Client.BytesPerSecond;
                Remaining = Util.ConvertTime(sec);
            }
            else if (Client.State == States.Error) Remaining = Lang.Translate("lang_error") + ": " + ErrorMsg;
            else if (Client.State == States.Starting) Remaining = Lang.Translate("lang_starting");

            if (Client.SpeedLimit > 0)
                Speed = string.Format("{0}/s [{1}/s]", Util.ConvertBytes(Client.BytesPerSecond), Util.ConvertBytes(Client.SpeedLimit));
            else Speed = string.Format("{0}/s", Util.ConvertBytes(Client.BytesPerSecond));

            Directory = Client.Directory;
        }
    }
}
