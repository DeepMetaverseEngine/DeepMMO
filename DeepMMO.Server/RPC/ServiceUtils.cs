using DeepCore;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    public static class ServiceUtils
    {
        public static void LogState(this IService service)
        {
            var info = service.State;
            Task.Run(() =>
            {
                var type = service.GetType();
                try
                {
                    lock (type)
                    {
                        var file = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "state" + Path.DirectorySeparatorChar + service.SelfAddress.FullPath + ".txt";
                        CFiles.CreateFile(new FileInfo(file));
                        File.WriteAllText(file, info, CUtils.UTF8);
                    }
                }
                catch { }
            });
        }
        
    }
}
