using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using SteamEmuUtility.Services;

namespace SteamEmuUtility.Common
{
    public class GreenLumaCommon
    {
        private const string greenlumagame = "Steam Emu Utility : [GL] Game Unlocking";
        private const string greenlumadlc = "Steam Emu Utility : [GL] DLC Unlocking";
        private const string greenlumafull = "Steam Emu Utility : [GL] Game And DLC Unlocking";
        public const string user32 = "User32.dll";
        private const string stealth = "NoQuestion.bin";

        public static Task InjectGame(OnGameStartingEventArgs args, SteamEmuUtility plugin, ILogger logger)
        {

            WriteAppList(args.Game.GameId, 0);
            return Task.CompletedTask;
        }
        public static Task InjectDLC(OnGameStartingEventArgs args, SteamEmuUtility plugin, ILogger logger, int count)
        {
            string dlcpath = $"{plugin.GetPluginUserDataPath()}\\Common\\{args.Game.GameId}.txt";
            if (File.Exists(dlcpath))
            {
                foreach (string line in File.ReadLines(dlcpath))
                {
                    WriteAppList(line, count);
                    count++;
                }
                return Task.CompletedTask;
            }
            else
            {
                var Steamservice = new SteamService(plugin, logger);
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                var b = plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    global.IsIndeterminate = false;
                    global.ProgressMaxValue = 1;
                    global.CurrentProgressValue = 0;
                    logger.Info("Getting DLC Info...");
                    global.Text = $"Getting DLC Info for {args.Game.Name}";
                    global.CurrentProgressValue++;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    var job = Steamservice.GetDLCStore(args.Game, global);
                    if (job.GetAwaiter().GetResult() == true)
                    {
                        foreach (string line in File.ReadLines(dlcpath))
                        {
                            WriteAppList(line, count);
                            count++;
                        }
                    }
                }, progress);
                return Task.CompletedTask;
            }
        }
        public static GameFeature GreenLumaFull(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(greenlumafull);
        }
        public static GameFeature GreenLumaGame(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(greenlumagame);
        }
        public static GameFeature GreenLumaDLC(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(greenlumadlc);
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
        public static Task WriteAppList(string appid, int count)
        {
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
