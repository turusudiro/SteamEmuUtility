using System;
using System.IO;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SteamEmuUtility.Common
{
    public class GreenLumaCommon
    {
        private const string greenlumafeature = "Steam Emu Utility : [GL] Enabled";
        public const string user32 = "User32.dll";
        private const string stealth = "NoQuestion.bin";

        public static GameFeature GreenLumaFeature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(greenlumafeature);
        }
        public static Task InjectUser32(string dll)
        {
            try
            {
                File.Copy(dll, Path.Combine(SteamCommon.GetSteamDir(), user32), true);
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
                File.Delete(Path.Combine(dll, user32));
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
            if (!Directory.Exists(Path.Combine(SteamCommon.GetSteamDir(), "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamCommon.GetSteamDir(), "applist")));
            }
            try
            {
                File.WriteAllText(Path.Combine(SteamCommon.GetSteamDir(), "applist", $"{count}.txt"), appid);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task Stealth()
        {
            if (!Directory.Exists(Path.Combine(SteamCommon.GetSteamDir(), "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamCommon.GetSteamDir(), "applist")));
            }
            try
            {
                File.WriteAllText(Path.Combine(SteamCommon.GetSteamDir(), "applist", stealth), null);
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
                var dir = new DirectoryInfo(Path.Combine(SteamCommon.GetSteamDir(), "applist"));
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
