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
            using (var sb = StringBuilderObjectPool.AllocAutoRelease())
            {
                if (service.GetState(sb))
                {
                    var info = sb.ToString();
                    Task.Run(() =>
                    {
                        var type = service.GetType();
                        try
                        {
                            lock (type)
                            {
                                var name = CFiles.ReplaceSpecialChars(service.SelfAddress.FullPath);
                                var file = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "state" + Path.DirectorySeparatorChar + name + ".txt";
                                CFiles.CreateFile(new FileInfo(file));
                                File.WriteAllText(file, info, CUtils.UTF8);
                            }
                        }
                        catch { }
                    });
                }
            }
        }
        
    }
}
