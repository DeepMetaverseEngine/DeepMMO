using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal
{
    public static class ServerUtils
    {

        public static ExpandoObject ShallowCloneExpando(ExpandoObject original)
        {
            var clone = new ExpandoObject();

            var _original = (IDictionary<string, object>) original;
            var _clone = (IDictionary<string, object>) clone;

            foreach (var kvp in _original)
                _clone.Add(kvp);

            return clone;
        }

        public static ExpandoObject FromProperteis(DeepCore.Properties prop)
        {
            ExpandoObject ret = new ExpandoObject();
            foreach (var e in prop)
            {
                ((IDictionary<string, object>) ret).Add(e.Key, e.Value);
            }
            return ret;
        }

        public static DeepCore.Properties ToProperteis(ExpandoObject prop)
        {
            DeepCore.Properties ret = new DeepCore.Properties();
            foreach (var e in prop)
            {
                ret.Add(e.Key, e.Value + "");
            }
            return ret;
        }

        public static object DynamicToObject(dynamic src, Type dtype)
        {
            try
            {
                if (dtype.IsPrimitive || dtype==(typeof(string)))
                {
                    return Convert.ChangeType(src, dtype);
                }
                else if (dtype.IsEnum)
                {
                    return Enum.Parse(dtype, src.ToString());
                }
                else if (dtype.IsArray)
                {
                    ArrayList temp = new ArrayList();
                    var elementType = dtype.GetElementType();
                    foreach (dynamic sitem in src)
                    {
                        var ditem = DynamicToObject(sitem, elementType);
                        temp.Add(ditem);
                    }
                    Array dfv = ReflectionUtil.CreateInstance(dtype, temp.Count) as Array;
                    for (int i = 0; i < temp.Count; i++)
                    {
                        dfv.SetValue(temp[i], i);
                    }
                    return dfv;
                }
                else if (dtype.GetInterface(typeof(IList).Name) != null)
                {
                    var dfv = ReflectionUtil.CreateInstance(dtype) as IList;
                    var elementType = dtype.GetGenericArguments()[0];
                    foreach (dynamic sitem in src)
                    {
                        var ditem = DynamicToObject(sitem, elementType);
                        dfv.Add(ditem);
                    }
                    return dfv;
                }
                else
                {
                    var dst = ReflectionUtil.CreateInstance(dtype);
                    foreach (var f in dtype.GetFields())
                    {
                        dynamic sfv = src[f.Name];
                        if (sfv != null)
                        {
                            var ditem = DynamicToObject(sfv, f.FieldType);
                            f.SetValue(dst, ditem);
                        }
                    }
                    return dst;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return null;
            }
        }

        /// <summary>
        /// 随机一次
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public static T GetRandomList<T>(T[] arr, int[] weight = null, Random random = null)
        {
            var ret = GetRandomList(arr, 1, weight,random);
            if (ret != null)
            {
                return ret[0];
            }
            return default(T);
        }

        /// <summary>
        /// 简单随机算法，可指定每项权重
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="count"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public static T[] GetRandomList<T>(T[] arr, int count, int[] weight = null, Random ran = null)
        {
            if (arr == null || arr.Length == 0)
            {
                return null;
            }
            var totalWeight = 0;
            int[] numArr = null;
            if (weight != null && weight.Length == arr.Length)
            {
                var currentIndex = 0;
                numArr = new int[weight.Length];
                for (var i = 0; i < weight.Length; i++)
                {
                    totalWeight += weight[i];
                    if (weight[i] == 0)
                    {
                        numArr[i] = 0;
                    }
                    else
                    {
                        numArr[i] = currentIndex + weight[i];
                        currentIndex = numArr[i];
                    }
                }
            }
            if (ran == null)
            {
                ran = new Random();
            }
            var ret = new T[count];
            for (var i = 0; i < count; i++)
            {
                if (totalWeight > 0)
                {
                    var num = ran.Next(0, totalWeight);
                    for (var j = 0; j < numArr.Length; j++)
                    {
                        if (num < numArr[j])
                        {
                            ret[i] = arr[j];
                            break;
                        }
                    }
                }
                else
                {
                    var index = ran.Next(0, arr.Length);
                    ret[i] = arr[index];
                }
            }

            return ret;
        }



        /// <summary>
        /// 获得当天内指定时间戳.
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns>DateTime</returns>
        public static DateTime GetTodayTimeStampUTC(int hour, int minute)
        {
            var Today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            Today = Today.AddHours(hour).AddMinutes(minute);
            if ((DateTime.Now - Today).TotalSeconds < 0)
            {
                return Today.ToUniversalTime();
            }
            else
            {
                return Today.AddHours(24).ToUniversalTime();
            }
        }
        /// <summary>
        /// 获取当前时间到该周某一天的到期时间戳.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public static DateTime GetTimeOfWeekTimeStampUTC(DayOfWeek w, int hour, int minute)
        {
            var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);
            var off = w - today.DayOfWeek;
            DateTime dt;
            if (off > 0)
            {
                dt = today.AddDays(off);
            }
            else
            {
                dt = today.AddDays(7 + off);
            }

            return dt.ToUniversalTime();
        }
        /// <summary>
        /// 获取每月结算日的到期时间戳.
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public static DateTime GetLastDayOfMonthTimeStampUTC(int hour, int minute)
        {
            var dt = LastDayOfCurMonth();
            dt = dt.AddHours(hour).AddMinutes(minute).AddSeconds(1);
            return dt.ToUniversalTime();
        }
        /// <summary>
        /// 获取到目标时间点的时差.
        /// </summary>
        /// <param name="hour"></param>
        /// <returns></returns>
        public static TimeSpan GetTimeSpanToNextTimeOfDay(int hour, int minute)
        {
            var today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            DateTime target = GetTodayTimeStampUTC(hour, minute);
            return target - today;
        }
        /// <summary>
        /// 获取当前时间到一周内某一天的时间差.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public static TimeSpan GetTimeSpanToTimeOfWeek(DayOfWeek w, int hour, int minute)
        {
            var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);
            var off = w - today.DayOfWeek;
            DateTime dt;
            if (off > 0)
            {
                dt = today.AddDays(off);
            }
            else
            {
                dt = today.AddDays(7 + off);
            }

            return dt - today;
        }
        /// <summary>
        /// 获取某月的最后一天59分59秒
        /// </summary>
        /// <param name="datetime">要取得月份的某一天</param>
        /// <returns></returns>
        public static DateTime LastDayOfCurMonth()
        {
            DateTime datetime = DateTime.Now;

            return DateTime.Parse(datetime.AddDays(1 - datetime.Day).AddMonths(1).ToShortDateString()).AddSeconds(-1);
        }
        /// <summary>
        /// 获取每月结算日所剩时间.
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        public static TimeSpan GetTimeSpanToLastDayOfMonth(int hour, int minute)
        {
            var dt = LastDayOfCurMonth();
            dt = dt.AddHours(hour).AddMinutes(minute).AddSeconds(1);
            return dt - DateTime.Now;
        }

        /// <summary>
        /// 是否过期.
        /// </summary>
        /// <param name="UTCtimeStamp"></param>
        /// <returns></returns>
        public static bool IsExpired(DateTime UTCtimeStamp)
        {
            var ret = DateTime.UtcNow - UTCtimeStamp;
            return (ret.TotalMilliseconds > 0);
        }


        /// <summary>
        /// 是否开始
        /// </summary>
        /// <param name="starTime"></param>
        /// <returns></returns>
        public static bool IsStarted(string starTime)
        { 
            if (!string.IsNullOrEmpty(starTime) && DateTime.TryParse(starTime, out DateTime starDate))
            {
                // 对于开始时间来说 过期了代表开始
                return ServerUtils.IsExpired(starDate.ToUniversalTime());
            }
            return true;


        }

        /// <summary>
        /// 是否结束
        /// </summary>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static bool IsEnded(string endTime)
        { 
            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out DateTime endDate))
            {
                //  过期了代表结束
                return ServerUtils.IsExpired(endDate.ToUniversalTime());
            }
            return false;
        }

        public static bool IsOpening(string starTime,string endTime)
        {
            // 已开始且没结束
            if(IsStarted(starTime) && !IsEnded(endTime))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 百分比随机，v/1000000.0f
        /// </summary>
        /// <param name="random"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool RandomWithMillionPer(Random random,int v)
        {
            float rd = v / 1000000.0f;
            var r = random.NextDouble();
            return r < rd;
        }
            
    }



}