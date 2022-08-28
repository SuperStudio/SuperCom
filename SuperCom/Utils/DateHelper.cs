using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Utils
{
    public static class DateHelper
    {


        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp, bool second = true)
        {
            DateTimeOffset dateTimeOffset;
            if (second)
            {
                dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
            }
            else
            {
                dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp);
            }
            return dateTimeOffset.DateTime;
        }

        public static double DateTimeToUnixTimeStamp(DateTime dateTime, bool second = true)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, TimeSpan.FromSeconds(0));
            if (second)
            {
                return dateTimeOffset.ToUnixTimeSeconds();
            }
            else
            {
                return dateTimeOffset.ToUnixTimeMilliseconds();
            }
        }


        public static string Now()
        {
            return DateTime.Now.toLocalDate();
        }
        public static string toLocalDate(this DateTime date)
        {
            if (date == null) return "";
            try
            {
                return date.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";

        }

        public static string toReadableTime(long ms)
        {
            string result = "";
            if (ms < 0) return result;
            try
            {
                TimeSpan t = TimeSpan.FromMilliseconds(ms);

                if (ms < 100)
                {
                    result = string.Format("{0} ms", t.Milliseconds);
                }
                else if (ms < 1000)
                {
                    result = string.Format("{3:D3} ms",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds,
                                        t.Milliseconds);
                }
                else if (ms < 60 * 1000)
                {
                    result = string.Format("{2:D2}s:{3:D3}ms",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds,
                                        t.Milliseconds);
                }
                else if (ms < 60 * 60 * 1000)
                {
                    result = string.Format("{1:D2}m:{2:D2}s:{3:D3}ms",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds,
                                        t.Milliseconds);
                }
                else if (ms < 60 * 60 * 60 * 1000)
                {
                    result = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds,
                                        t.Milliseconds);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        public static string toLocalDate(string str)
        {
            DateTime date = DateTime.Now;
            DateTime.TryParse(str, out date);
            return date.ToString("yyyy-MM-dd");
        }

        public static string NowDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }


    }
}
