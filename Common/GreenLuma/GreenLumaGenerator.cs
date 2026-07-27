using DlcManagerCommon;
using IniParser;
using IniParser.Model;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static GreenLumaCommon.GreenLuma;

namespace GreenLumaCommon
{
    public class GreenLumaGenerator
    {
        public static void CreateDLLInjectorIni(string pluginPath, GreenLumaMode mode, IEnumerable<string> argsList, string dllPath, string destination)
        {
            CreateDLLInjectorIni(pluginPath, mode, string.Empty, argsList, dllPath, destination);
        }
        public static void CreateDLLInjectorIni(string pluginPath, GreenLumaMode mode, string steamexePath, IEnumerable<string> argsList, string dllPath, string destinationDir)
        {
            string args = string.Empty;

            if (argsList.Any())
            {
                args = string.Join(" ", argsList);
            }

            string glPath = Path.Combine(pluginPath, "GreenLuma");

            string path = Path.Combine(destinationDir, "DLLInjector.ini");
            string section = "DllInjector";
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            IniData data = new IniData();
            data.Sections.AddSection(section);

            // default DLLInjector configs

            // [DllInjector]
            data.Sections[section]["AllowMultipleInstancesOfDLLInjector"] = "0";
            data.Sections[section]["UseFullPathsFromIni"] = "0";

            // Exe to start
            data.Sections[section]["Exe"] = "Steam.exe";
            data.Sections[section]["CommandLine"] = "-inhibitbootstrap";

            // Dll to inject
            data.Sections[section]["Dll"] = dllPath;

            // Wait for started exe to close before exiting the DllInjector process.
            data.Sections[section]["WaitForProcessTermination"] = "1";

            // Set a fake parent process
            // EnableMitigationsOnChildProcess must be disabled for this.
            data.Sections[section]["EnableFakeParentProcess"] = "0";
            data.Sections[section]["FakeParentProcess"] = "explorer.exe";

            // Enable security mitigations on child process.
            data.Sections[section]["EnableMitigationsOnChildProcess"] = "0";

            data.Sections[section]["DEP"] = "1";
            data.Sections[section]["SEHOP"] = "1";
            data.Sections[section]["HeapTerminate"] = "1";
            data.Sections[section]["ForceRelocateImages"] = "1";
            data.Sections[section]["BottomUpASLR"] = "1";
            data.Sections[section]["HighEntropyASLR"] = "1";
            data.Sections[section]["RelocationsRequired"] = "1";
            data.Sections[section]["StrictHandleChecks"] = "0";
            data.Sections[section]["Win32kSystemCallDisable"] = "0";
            data.Sections[section]["ExtensionPointDisable"] = "1";
            data.Sections[section]["CFG"] = "1";
            data.Sections[section]["CFGExportSuppression"] = "1";
            data.Sections[section]["StrictCFG"] = "1";
            data.Sections[section]["DynamicCodeDisable"] = "0";
            data.Sections[section]["DynamicCodeAllowOptOut"] = "0";
            data.Sections[section]["BlockNonMicrosoftBinaries"] = "0";
            data.Sections[section]["FontDisable"] = "1";
            data.Sections[section]["NoRemoteImages"] = "1";
            data.Sections[section]["NoLowLabelImages"] = "1";
            data.Sections[section]["PreferSystem32"] = "0";
            data.Sections[section]["RestrictIndirectBranchPrediction"] = "1";
            data.Sections[section]["SpeculativeStoreBypassDisable"] = "0";
            data.Sections[section]["ShadowStack"] = "0";
            data.Sections[section]["ContextIPValidation"] = "0";
            data.Sections[section]["BlockNonCETEHCONT"] = "0";
            data.Sections[section]["BlockFSCTL"] = "0";

            // Number to files to create
            data.Sections[section]["CreateFiles"] = "1";

            // Name of the file(s) to create
            data.Sections[section]["FileToCreate_1"] = "NoQuestion.bin";
            data.Sections[section]["FileToCreate_2"] = "";

            // Patch an x86 exe to enable IMAGE_FILE_LARGE_ADDRESS_AWARE
            data.Sections[section]["Use4GBPatch"] = "0";
            data.Sections[section]["FileToPatch_1"] = "";

            data.Sections[section]["BootImage"] = "";
            data.Sections[section]["BootImageWidth"] = "0";
            data.Sections[section]["BootImageHeight"] = "0";
            data.Sections[section]["BootImageXOffest"] = "0";
            data.Sections[section]["BootImageYOffest"] = "0";

            if (mode == GreenLumaMode.Stealth || mode == GreenLumaMode.Family)
            {
                data.Sections[section]["CommandLine"] = args;
                data.Sections[section]["Dll"] = Path.Combine(glPath, dllPath);
                data.Sections[section]["EnableMitigationsOnChildProcess"] = "0";
                data.Sections[section]["UseFullPathsFromIni"] = "1";
                data.Sections[section]["Exe"] = steamexePath;
                data.Sections[section]["WaitForProcessTermination"] = "0";
                data.Sections[section]["EnableFakeParentProcess"] = "1";
                data.Sections[section]["CreateFiles"] = "2";
                data.Sections[section]["FileToCreate_1"] = "StealthMode.bin";
                data.Sections[section]["FileToCreate_2"] = "NoQuestion.bin";
            }
            else
            {
                data.Sections[section]["CommandLine"] = $"-inhibitbootstrap {args}";
            }

            parser.WriteFile(path, data, new UTF8Encoding());
        }
        /// <summary>
        /// Create txt files with GreenLuma appids style like "0.txt", "1.txt". and more.
        /// </summary>
        /// <para>Create txt files in applist Steam directory like "0.txt", "1.txt". and more.</para>
        /// <param name="appids">List appids to write into applist</param>
        /// <param name="destinationPath">Destination path</param>
        /// /// <param name="overwrite">Delete existing applist</param>
        /// <returns></returns>
        public static void WriteAppList(IEnumerable<string> appids, string applistIniFile, string destinationPath, bool overwrite = false)
        {
            var regex = new Regex(@"(?<appid>\d+)\s=");
            var oldAppids = new List<string>();
            foreach (var line in File.ReadAllLines(applistIniFile))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    string appid = match.Groups["appid"].Value;
                    oldAppids.Add(appid);
                }
            }

            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";

            Dictionary<string, string> existsAppIds = new Dictionary<string, string>();
            var applistIniTargetPath = Path.Combine(destinationPath, "AppList.ini");
            if (!overwrite && FileSystem.FileExists(applistIniTargetPath))
            {
                var existsApplistIniData = parser.ReadFile(applistIniTargetPath);
                existsApplistIniData["AppList"].RemoveKey("NumAppIDs");
                foreach (var key in existsApplistIniData.Sections["AppList"])
                {
                    oldAppids.Remove(key.KeyName);
                    existsAppIds[key.KeyName] = key.Value;
                }
            }
            else if (FileSystem.DirectoryExists(destinationPath))
            {
                FileSystem.DeleteDirectory(destinationPath);
            }
            FileSystem.CreateDirectory(destinationPath);

            var applistIniData = parser.ReadFile(applistIniFile);
            applistIniData["AppList"].RemoveAllKeys();

            foreach (var exist in existsAppIds)
            {
                applistIniData["AppList"][exist.Key] = exist.Value;
            }

            int count = 0;
            foreach (var appid in appids)
            {
                if (count >= oldAppids.Count) break;
                applistIniData["AppList"][oldAppids[count]] = appid.ToString();
                count++;
            }
            var total = applistIniData["AppList"].Count;
            applistIniData["AppList"]["NumAppIDs"] = total.ToString();

            parser.WriteFile(applistIniTargetPath, applistIniData, new UTF8Encoding(false));
        }
        public static void GenerateDLC(Game game, SteamService steam, GlobalProgressActionArgs progress, string apiKey, string pluginPath)
        {
            DlcManager.GenerateDLC(game.GameId, steam, progress, apiKey, pluginPath);
        }
    }
}
