using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace SteamEmuUtility.Common
{
    public class GreenLumaCommon
    {
        private const string user32 = "user32.dll";
        private const string stealth = "NoQuestion.bin";
        public static Task InjectUser32(string dll)
        {
            try
            {
                File.Copy(dll, System.IO.Path.Combine(SteamCommon.GetSteamDir(), user32), true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task DeleteUser32(string dll)
        {
            try
            {
                File.Delete(System.IO.Path.Combine(dll, user32));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task WriteAppList(string appid)
        {
            int count = 0;
            if (!Directory.Exists(System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine((System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist"))));
            }
            try
            {
                File.WriteAllText(System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist", $"{count}.txt"), appid);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task Stealth()
        {
            if (!Directory.Exists(System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine((System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist"))));
            }
            try
            {
                File.WriteAllText(System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist", stealth), null);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task DeleteAppList()
        {
            try
            {
                var dir = new DirectoryInfo(System.IO.Path.Combine(SteamCommon.GetSteamDir(), "applist"));
                dir.Delete(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
    }
}
