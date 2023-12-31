﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GoldbergCommon;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginsCommon;
using ProcessCommon;
using static GoldbergCommon.Goldberg;

namespace SteamEmuUtility.Controller
{
    public class GoldBergController : PlayController
    {
        private Stopwatch stopWatch;
        private ProcessMonitor procMon;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly SteamEmuUtilitySettings settings;
        public GoldBergController(Game game, IPlayniteAPI PlayniteApi, SteamEmuUtilitySettings settings) : base(game)
        {
            Name = $"Start {game.Name} with Goldberg";
            this.PlayniteApi = PlayniteApi;
            this.settings = settings;
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
                PlayniteApi.Dialogs.ShowErrorMessage("Required Goldberg files not found. Check your settings");
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }
            procMon = new ProcessMonitor();
            procMon.TreeStarted += ProcMon_TreeStarted;
            procMon.TreeDestroyed += Monitor_TreeDestroyed;
            if (!GoldbergTasks.InjectJob(Game, PlayniteApi, out string message, settings.SteamWebApi))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(message);
                InvokeOnStopped(new GameStoppedEventArgs());
                return;
            }
            string installDirectory = Paths.GetFinalPathName(Game.InstallDirectory);
            if (FileSystem.FileExists(Path.Combine(GameSettingsPath(Game.GameId), "admin.txt")))
            {
                ProcessUtilities.StartProcess(ColdClientExecutable, true);
            }
            else { ProcessUtilities.StartProcess(ColdClientExecutable); }
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