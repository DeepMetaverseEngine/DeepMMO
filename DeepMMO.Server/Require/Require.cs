using DeepCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepMMO.Server.Require
{
    public class Require
    {
        
        /// <summary>
        /// 弃用的
        /// </summary>
        /// <param name="key"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public delegate Tuple<bool, int> CheckHandler(string key, int min, int max);
        public delegate Task<Tuple<bool, int>> CheckHandlerAsync(string key, int min, int max);

        HashMap<string, CheckHandler> map = new HashMap<string, CheckHandler>();
        HashMap<string, CheckHandlerAsync> mapAsync = new HashMap<string, CheckHandlerAsync>();
        public void RegistHandler(string key, CheckHandler handler)
        {
            try
            {
                map.Add(key, handler);
            }
            catch (Exception e)
            {
                string err = string.Format("RegistHandler Error Key = [{0}] {1}", key, e.ToString());
                throw new Exception(err);
            }

        }
        public void RegistHandler(string key, CheckHandlerAsync handler)
        {
            try
            {
                mapAsync.Add(key, handler);
            }
            catch (Exception e)
            {
                string err = string.Format("RegistHandler Error Key = [{0}] {1}", key, e.ToString());
                throw new Exception(err);
            }

        }
        public void UnRegistHandler(string key)
        {
            map.Remove(key);
            mapAsync.Remove(key);
        }

        public void Dispose()
        {
            map.Clear();
            mapAsync.Clear();
        }

        public bool CheckRequire(RequireData data, out string reason)
        {
            reason = null;
            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
                return true;

            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandler handler;
            string prefix = null;
            string k = null;
            for (int i = 0; i < key.Length; i++)
            {
                try
                {
                    k = key[i];
                    if (string.IsNullOrEmpty(k) || k == "0")
                        continue;

                    prefix = k.Substring(0, 1);
                    k = k.Substring(1);
                    handler = map.Get(prefix);

                    if (handler == null)
                        return false;

                    var tuple = handler.Invoke(k, min[i], max[i]);

                    if (tuple.Item1 == false)
                    {
                        if (text != null && i < text.Length)
                            reason = text[i];
                        return false;
                    }
                }
                catch (Exception)
                {
                    string info = string.Format("checkRequire Error key={0},min={1},max={2},reason{3}",
                                                key[i], min[i], max[i]);
                    throw new Exception(info);
                }
            }
            return true;
        }

        public bool CheckRequireExceptNullHander(RequireData data, out string reason)
        {
            reason = null;
            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
                return true;

            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandler handler;
            string prefix = null;
            string k = null;
            for (int i = 0; i < key.Length; i++)
            {
                try
                {
                    k = key[i];
                    if (string.IsNullOrEmpty(k) || k == "0")
                        continue;

                    prefix = k.Substring(0, 1);
                    k = k.Substring(1);
                    handler = map.Get(prefix);

                    if (handler == null)
                        continue;

                    var tuple = handler.Invoke(k, min[i], max[i]);

                    if (tuple.Item1 == false)
                    {
                        if (text != null && i < text.Length)
                            reason = text[i];
                        return false;
                    }
                }
                catch (Exception)
                {
                    string info = string.Format("checkRequire Error key={0},min={1},max={2},reason{3}",
                                                key[i], min[i], max[i]);
                    throw new Exception(info);
                }
            }
            return true;
        }


        public async Task<RequireResult> CheckRequireAsync(RequireData data)
        {
            RequireResult result = new RequireResult();
            result.Result = true;
            result.Reason = string.Empty;

            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
            {
                return result;
            }

            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandlerAsync handler;
            string prefix = null;
            string k = null;
            for (int i = 0; i < key.Length; i++)
            {
                try
                {
                    k = key[i];
                    if (string.IsNullOrEmpty(k) || k == "0")
                    {
                        return result;
                    }

                    prefix = k.Substring(0, 1);
                    k = k.Substring(1);
                    handler = mapAsync.Get(prefix);

                    if (handler == null)
                    {
                        result.Result = false;
                        return result;
                    }
     

                    var tuple = await handler.Invoke(k, min[i], max[i]);

                    if (tuple.Item1 == false)
                    {
                        if (text != null && i < text.Length)
                        {
                            result.Reason = text[i];
                        }
                        result.Result = false;
                        return result;
                    }
                }
                catch (Exception)
                {
                    string info = string.Format("checkRequire Error key={0},min={1},max={2},reason{3}",
                                                key[i], min[i], max[i]);
                    throw new Exception(info);
                }
            }
            return result;
        }

        public void CheckRequire(RequireData data, ref List<RequireResult> resultlist)
        {
            RequireResult rr = null;

            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
            {
                rr = new RequireResult();
                rr.Result = true;
                resultlist.Add(rr);
                return;
            }


            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandler handler;
            string prefix = null;
            string k = null;
            for (int i = 0; i < key.Length; i++)
            {

                try
                {
                    k = key[i];
                    if (string.IsNullOrEmpty(k) || k == "0")
                    {
                        continue;
                    }

                    rr = new RequireResult();
                    prefix = k.Substring(0, 1);
                    k = k.Substring(1);
                    handler = map.Get(prefix);

                    if (handler == null)
                    {
                        rr.Result = false;
                        rr.Reason = "can not find require handler";
                    }
                    else
                    {
                        var ret = handler.Invoke(k, min[i], max[i]);
                        rr.Result = ret.Item1;
                        rr.CurVal = ret.Item2;
                        rr.MaxVal = max[i];
                        rr.MinVal = min[i];
                        if (text != null)
                            rr.Reason = text[i];
                    }
                }
                catch (Exception)
                {
                    string info = string.Format("checkRequire Error key={0},min={1},max={2},reason{3}",
                                                key[i], min[i], max[i], text[i]);
                    throw new Exception(info);
                }

                resultlist.Add(rr);
            }
        }
        public bool CheckDetailRequireByAppendCount(RequireData data, int index, int count, out string reason)
        {
            reason = null;
            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
                return true;

            if (data.Key.Length <= index)
                return false;


            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandler handler;
            string k = key[index];
            if (string.IsNullOrEmpty(k) || k == "0")
                return true;

            string prefix = k.Substring(0, 1);
            handler = map.Get(prefix);
            if (handler == null)
                return false;

            var tuple = handler.Invoke(k, min[index], max[index]);
            if (tuple.Item1 == false)
            {
                if (text != null && index < text.Length)
                    reason = text[index];
                return false;
            }
            else
            {
                var checkVal = tuple.Item2 + count;
                if (max[index] == -1)
                {
                    return (checkVal >= min[index]);

                }
                else
                {
                    return checkVal >= min[index] && checkVal < max[index];
                }

            }
        }


        public async Task<RequireResult> CheckDetailRequireByAppendCountAsync(RequireData data, int index, int count)
        {
            RequireResult result = new RequireResult();
            result.Result = true;

            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
            {
                return result;
            }

            if (data.Key.Length <= index)
            {
                result.Result = false;
                return result;
            }

            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandlerAsync handler;
            string k = key[index];
            if (string.IsNullOrEmpty(k) || k == "0")
            {
                return result;
            }


            string prefix = k.Substring(0, 1);
            handler = mapAsync.Get(prefix);
            if (handler == null)
            {
                result.Result = false;
                return result;
            }


            var tuple = await handler.Invoke(k, min[index], max[index]);
            if (tuple.Item1 == false)
            {
                result.Result = false;
                if (text != null && index < text.Length)
                    result.Reason = text[index];
                return result;
            }
            else
            {
                var checkVal = tuple.Item2 + count;
                if (max[index] == -1)
                {
                    if (checkVal < min[index])
                    {
                        result.Result = false;
                        if (text != null && index < text.Length)
                            result.Reason = text[index];
                    }

                }
                else
                {
                    if (!(checkVal >= min[index] && checkVal < max[index]))
                    {
                        result.Result = false;
                        if (text != null && index < text.Length)
                            result.Reason = text[index];
                    }
                }
                return result;
            }
        }
        public async Task<List<RequireResult>> CheckRequireListAsync(RequireData data)
        {
            List<RequireResult> resultlist = new List<RequireResult>();
            RequireResult rr = null;

            if (data == null || data.Key == null || data.Maxval == null || data.Minval == null)
            {
                rr = new RequireResult();
                rr.Result = true;
                rr.Result = true;
                resultlist.Add(rr);
                return resultlist;
            }


            string[] key = data.Key;
            int[] min = data.Minval;
            int[] max = data.Maxval;
            string[] text = data.Text;

            CheckHandlerAsync handler;
            string prefix = null;
            string k = null;
            for (int i = 0; i < key.Length; i++)
            {

                try
                {
                    k = key[i];
                    if (string.IsNullOrEmpty(k) || k == "0")
                    {
                        continue;
                    }

                    rr = new RequireResult();
                    prefix = k.Substring(0, 1);
                    k = k.Substring(1);
                    handler = mapAsync.Get(prefix);

                    if (handler == null)
                    {
                        rr.Result = false;
                        rr.Reason = "can not find require handler";
                    }
                    else
                    {
                        var ret = await handler.Invoke(k, min[i], max[i]);
                        rr.Result = ret.Item1;
                        rr.CurVal = ret.Item2;
                        rr.MaxVal = max[i];
                        rr.MinVal = min[i];
                        if (text != null)
                            rr.Reason = text[i];
                    }
                }
                catch (Exception)
                {
                    string info = string.Format("checkRequire Error key={0},min={1},max={2},reason{3}",
                                                key[i], min[i], max[i], text[i]);
                    throw new Exception(info);
                }

                resultlist.Add(rr);
            }
            return resultlist;
        }


        public class RequireResult
        {
            public bool Result;
            public int CurVal;
            public int MinVal;
            public int MaxVal;
            public string Reason;
        }

        public class RequireData
        {
            public string[] Key;
            public int[] Minval;
            public int[] Maxval;
            public string[] Text;
        }


    }
}
