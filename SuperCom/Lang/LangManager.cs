using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SuperCom.Lang
{
    public static class LangManager
    {
        public static bool SetLang(string lang)
        {
            if (!SuperControls.Style.LangManager.SupportLanguages.Contains(lang))
            {
                App.Logger.Error($"Language not support: {lang}");
                return false;
            }

            string format = "pack://application:,,,/SuperCom;Component/Lang/{0}.xaml";
            format = string.Format(format, lang);
            foreach (ResourceDictionary mergedDictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (mergedDictionary.Source != null && mergedDictionary.Source.OriginalString.Contains("SuperCom;Component/Lang"))
                {
                    try
                    {
                        bool flag = Application.Current.Resources.MergedDictionaries.Remove(mergedDictionary);
                        mergedDictionary.Source = new Uri(format, UriKind.RelativeOrAbsolute);
                        Application.Current.Resources.MergedDictionaries.Add(mergedDictionary);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Error(ex.Message);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
