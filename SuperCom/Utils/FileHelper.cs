using SuperControls.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Utils
{
    public static class FileHelper
    {
        public static bool TryOpenUrl(string url, Action<string> callBack = null)
        {
            try
            {
                Process.Start(url);
                return true;

            }
            catch (Exception ex)
            {
                callBack?.Invoke(ex.Message);
            }

            return false;
        }


        public static bool IsFile(string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    return false;
                else
                    return true;
            }
            catch
            {
                return true;
            }

        }

        public static bool TryOpenSelectPath(string path)
        {
            try
            {
                if (IsFile(path))
                {
                    if (File.Exists(path))
                    {
                        Process.Start("explorer.exe", "/select, \"" + path + "\"");
                        return true;
                    }
                    else
                    {
                        MessageCard.Error($"文件不存在：{path}");
                    }
                }
                else
                {
                    if (Directory.Exists(path))
                    {
                        Process.Start("explorer.exe", " \"" + path + "\"");
                        return true;
                    }
                    else
                    {
                        MessageCard.Error($"文件夹不存在：{path}");

                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
