using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace downloader3
{
    /// <summary>
    /// Poskytuje možnost měnit jazyk programu a překládat
    /// </summary>
    public class Lang
    {
        /// <summary>
        /// Nastaví jazyk programu
        /// </summary>
        /// <param name="culture"></param>
        public static void SetLanguage(string culture)
        {
            if (String.IsNullOrEmpty(culture)) return;

            var list = Application.Current.Resources.MergedDictionaries.ToList();

            string requested = $"lang.{culture}.xaml";

            var resDict = list.FirstOrDefault(d => d.Source.OriginalString == requested);

            if (resDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resDict);
                Application.Current.Resources.MergedDictionaries.Add(resDict);

                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            }            
        }

        /// <summary>
        /// Vyhledá zadaný prostředek v přostředcích aplikace a vrátí odpovídající řetězec
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static string Translate(string resource)
        {
            string result = (string)Application.Current.TryFindResource(resource);
            if (result == null)
            {
                MessageBox.Show("Language resource \"" + resource + "\" is invalid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return resource;
            }
            return result;
        }
    }
}
