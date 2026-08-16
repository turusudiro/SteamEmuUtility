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

            // Export to call in dll
            data.Sections[section]["Export"] = "Init";

            // Check if call to export returned positive value, debugbreak if not
            data.Sections[section]["CheckReturnValue"] = "0";

            // Terminate the process when an error is sent from the injected dll
            data.Sections[section]["TerminateOnError"] = "1";

            // Wait for started exe to close before exiting the DllInjector process.
            data.Sections[section]["WaitForProcessTermination"] = "1";

            // Set fake parent process
            data.Sections[section]["EnableFakeParentProcess"] = "0";
            data.Sections[section]["FakeParentProcess"] = "explorer.exe";

            // Number to files to create
            data.Sections[section]["CreateFiles"] = "0";

            // Name of the file(s) to create
            data.Sections[section]["FileToCreate_1"] = "";
            data.Sections[section]["FileToCreate_2"] = "";

            data.Sections[section]["BootImage"] = "";

            if (mode == GreenLumaMode.Stealth || mode == GreenLumaMode.Family)
            {
                data.Sections[section]["CommandLine"] = args;
                data.Sections[section]["Dll"] = Path.Combine(glPath, dllPath);
                data.Sections[section]["UseFullPathsFromIni"] = "1";
                data.Sections[section]["Exe"] = steamexePath;
                data.Sections[section]["WaitForProcessTermination"] = "0";
                data.Sections[section]["EnableFakeParentProcess"] = "1";
                data.Sections[section]["CreateFiles"] = "1";
                data.Sections[section]["FileToCreate_1"] = "StealthMode.bin";
            }
            else
            {
                data.Sections[section]["CommandLine"] = $"-inhibitbootstrap {args}";
            }

            parser.WriteFile(path, data, new UTF8Encoding());
        }
        /// <summary>
        /// Create AppList.ini files GreenLuma
        /// </summary>
        /// <param name="appids">List appids to write into applist</param>
        /// <param name="destinationPath">Destination path</param>
        /// /// <param name="overwrite">Delete existing applist</param>
        /// <returns>Written appids</returns>
        public static IEnumerable<string> WriteAppList(IEnumerable<string> appids, string applistIniFile, string destinationPath, bool overwrite = false)
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

            HashSet<string> appidsSet = new HashSet<string>(appids);
            Dictionary<string, string> existsAppIds = new Dictionary<string, string>();
            var applistIniTargetPath = Path.Combine(destinationPath, "AppList.ini");
            if (!overwrite && FileSystem.FileExists(applistIniTargetPath))
            {
                var existsApplistIniData = parser.ReadFile(applistIniTargetPath);
                foreach (var key in existsApplistIniData.Sections["AppList"])
                {
                    appidsSet.Add(key.Value);
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
            foreach (var appid in appidsSet)
            {
                if (count > oldAppids.Count) break;
                applistIniData["AppList"][oldAppids[count]] = appid.ToString();
                count++;
            }

            parser.WriteFile(applistIniTargetPath, applistIniData, new UTF8Encoding(false));

            return appidsSet;
        }
        public static void GenerateDLC(Game game, SteamService steam, GlobalProgressActionArgs progress, string apiKey, string pluginPath)
        {
            DlcManager.GenerateDLC(game.GameId, steam, progress, apiKey, pluginPath);
        }
    }
}
