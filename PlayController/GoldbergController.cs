﻿using GoldbergCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginsCommon;
using ProcessCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static GoldbergCommon.Goldberg;

namespace SteamEmuUtility.Controller
{
    public class GoldBergController : PlayController
    {
        private Stopwatch stopWatch;
        private ProcessMonitor procMon;
        private readonly GoldbergGame game;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly SteamEmuUtilitySettings settings;
        public GoldBergController(Game game, IPlayniteAPI PlayniteApi, SteamEmuUtilitySettings settings) : base(game)
        {
            Name = string.Format(ResourceProvider.GetString("LOCSEU_StartWithGoldberg"), game.Name);
            this.PlayniteApi = PlayniteApi;
            this.settings = settings;
            this.game = GoldbergTasks.ConvertGame(game, PlayniteApi);
        }
        public override void Dispose()
        {
            procMon?.Dispose();
        }
        public override void Play(PlayActionArgs args)
        {
            Dispose();
            if (!ColdClientExists(out List<string> _))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GoldbergErrorRequiredFiles"));
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }
            procMon = new ProcessMonitor();
            procMon.TreeStarted += ProcMon_TreeStarted;
            procMon.TreeDestroyed += Monitor_TreeDestroyed;
            if (!GoldbergTasks.InjectJob(game, PlayniteApi, out string message, settings.SteamWebApi))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(message);
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }
            string installDirectory = Paths.GetFinalPathName(Game.InstallDirectory);
            string GamePath = Path.Combine(GameSettingsPath(Game.GameId));
            switch (game.ConfigsEmu.Architecture)
            {
                case "64":
                    if (game.ConfigsEmu.RunAsAdmin)
                    {
                        ProcessUtilities.StartProcess(ColdClientExecutable64, true);
                    }
                    else { ProcessUtilities.StartProcess(ColdClientExecutable64); }
                    break;
                case "32":
                    if (game.ConfigsEmu.RunAsAdmin)
                    {
                        ProcessUtilities.StartProcess(ColdClientExecutable32, true);
                    }
                    else { ProcessUtilities.StartProcess(ColdClientExecutable32); }
                    break;
                default:
                    if (game.ConfigsEmu.RunAsAdmin)
                    {
                        ProcessUtilities.StartProcess(ColdClientExecutable64, true);
                    }
                    else { ProcessUtilities.StartProcess(ColdClientExecutable64); }
                    break;
            }
            if (FileSystem.DirectoryExists(installDirectory))
            {
                procMon.WatchDirectoryProcesses(installDirectory, false, false);
            }
            else
            {
                InvokeOnStopped(new GameStoppedEventArgs());
            }
        }
        private void ProcMon_TreeStarted(object sender, ProcessMonitor.TreeStartedEventArgs args)
        {
            stopWatch = Stopwatch.StartNew();
            InvokeOnStarted(new GameStartedEventArgs() { StartedProcessId = args.StartedId });
        }

        private void Monitor_TreeDestroyed(object sender, EventArgs args)
        {
            stopWatch?.Stop();
            InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopWatch?.Elapsed.TotalSeconds ?? 0)));
        }
    }
}