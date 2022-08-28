using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Utils
{
    public static class TransHelper
    {
        // returns: "48656C6C6F20776F726C64" for "Hello world"
        public static string StrToHex(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            var sb = new StringBuilder();

            try
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                foreach (var t in bytes)
                {
                    sb.Append(t.ToString("X2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        // returns: "Hello world" for "48656C6C6F20776F726C64"
        public static string HexToStr(string hexString)
        {
            if (string.IsNullOrEmpty(hexString)) return "";
            var bytes = new byte[hexString.Length / 2];
            try
            {
                for (var i = 0; i < bytes.Length; i++)
                {

                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

                }

                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
