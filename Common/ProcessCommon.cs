using Playnite.SDK;
using PluginsCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessCommon
{
    public static class CmdLineTools
    {
        public const string TaskKill = "taskkill";
        public const string Cmd = "cmd";
        public const string IPConfig = "ipconfig";
    }
    public static class ProcessUtilities
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public static Process StartUrl(string url)
        {
            logger.Debug($"Opening URL: {url}");
            try
            {
                return Process.Start(url);
            }
            catch (Exception e)
            {
                // There are some crash report with 0x80004005 error when opening standard URL.
                logger.Error(e, "Failed to open URL.");
                return Process.Start(CmdLineTools.Cmd, $"/C start {url}");
            }
        }
        public static Process StartProcess(string path, bool asAdmin = false)
        {
            return StartProcess(path, string.Empty, string.Empty, asAdmin);
        }

        public static Process StartProcess(string path, string arguments, bool asAdmin = false)
        {
            return StartProcess(path, arguments, string.Empty, asAdmin);
        }
        public static Process StartProcessHidden(string path, string arguments)
        {
            var startupPath = path;
            if (path.Contains(".."))
            {
                startupPath = Path.GetFullPath(path);
            }

            var info = new ProcessStartInfo(startupPath)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments,
                WorkingDirectory = new FileInfo(startupPath).Directory.FullName
            };

            return Process.Start(info);
        }
        public static Process StartProcess(string path, string arguments, string workDir, bool asAdmin = false)
        {
            logger.Debug($"Starting process: {path}, {arguments}, {workDir}, {asAdmin}");
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("Cannot start process, executable path is specified.");
            }

            var startupPath = path;
            if (path.Contains(".."))
            {
                startupPath = Path.GetFullPath(path);
            }

            var info = new ProcessStartInfo(startupPath)
            {
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrEmpty(workDir) ? (new FileInfo(startupPath)).Directory.FullName : workDir
            };

            if (asAdmin)
            {
                info.Verb = "runas";
            }

            return Process.Start(info);
        }
        public static Process TryStartProcess(string executablePath, bool asAdmin = false)
        {
            return TryStartProcess(executablePath, string.Empty, string.Empty, 1, 1000, false, asAdmin);
        }
        public static Process TryStartProcess(string executablePath, string arguments, bool asAdmin = false)
        {
            return TryStartProcess(executablePath, arguments, string.Empty, 1, 1000, false, asAdmin);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, bool asAdmin = false)
        {
            return TryStartProcess(executablePath, arguments, workingdir, 1, 1000, false, asAdmin);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts, bool asAdmin = false)
        {
            return TryStartProcess(executablePath, arguments, workingdir, maxAttempts, 1000, false, asAdmin);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts, int IntervalMilliseconds, bool asAdmin = false)
        {
            return TryStartProcess(executablePath, arguments, workingdir, maxAttempts, IntervalMilliseconds, false, asAdmin);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts, int IntervalMilliseconds, bool shellExecute, bool AsAdmin = false)
        {
            var process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.UseShellExecute = shellExecute;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingdir;
            if (AsAdmin)
            {
                process.StartInfo.Verb = "runas";
            }
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                process.Start();
                // Wait for a brief moment to give the process a chance to start
                Thread.Sleep(IntervalMilliseconds); // 0,5 second
                bool processRunning = IsProcessRunning(process.ProcessName);
                if (processRunning) // Check if the process is running
                {
                    break; //if process start normally
                }
                // Sleep for the specified interval before the next attempt
                Thread.Sleep(IntervalMilliseconds);
            }

            return process; // All attempts failed
        }

        public static int StartProcessWait(string path, string arguments, string workDir, bool noWindow = false)
        {
            logger.Debug($"Starting process: {path}, {arguments}, {workDir}");
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("Cannot start process, executable path is specified.");
            }

            var startupPath = path;
            if (path.Contains(".."))
            {
                startupPath = Path.GetFullPath(path);
            }

            var info = new ProcessStartInfo(startupPath)
            {
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrEmpty(workDir) ? (new FileInfo(startupPath)).Directory.FullName : workDir
            };

            if (noWindow)
            {
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
            }

            using (var proc = Process.Start(info))
            {
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        public static int StartProcessWait(
            string path,
            string arguments,
            string workDir,
            out string stdOutput,
            out string stdError)
        {
            logger.Debug($"Starting process: {path}, {arguments}, {workDir}");
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("Cannot start process, executable path is specified.");
            }

            var startupPath = path;
            if (path.Contains(".."))
            {
                startupPath = Path.GetFullPath(path);
            }

            var info = new ProcessStartInfo(startupPath)
            {
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrEmpty(workDir) ? (new FileInfo(startupPath)).Directory.FullName : workDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var stdout = string.Empty;
            var stderr = string.Empty;
            using (var proc = new Process())
            {
                proc.StartInfo = info;
                proc.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdout += e.Data + Environment.NewLine;
                    }
                };

                proc.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stderr += e.Data + Environment.NewLine;
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                stdOutput = stdout;
                stdError = stderr;
                return proc.ExitCode;
            }
        }
        public static bool IsErrorDialog(Process process, string error)
        {
            // You can implement logic to identify error dialogs based on process details
            // For example, you can check the process title, or other attributes.
            return process.MainWindowTitle.Contains(error); // Modify this condition as needed.
        }
        public static Process[] GetProcesses(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
                return processes;
            }
            else
            {
                return null;
            }
        }
        public static bool ProcessKill(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
                //processes are running, let's terminate them
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                return true;
            }
            return false;
        }

    }
    public class ProcessMonitor : IDisposable
    {
        public class TreeStartedEventArgs
        {
            public int StartedId { get; set; }
        }

        public event EventHandler<TreeStartedEventArgs> TreeStarted;
        public event EventHandler TreeDestroyed;

        private SynchronizationContext execContext;
        private CancellationTokenSource watcherToken;
        private static ILogger logger = LogManager.GetLogger();
        private const int maxFailCount = 5;

        public ProcessMonitor()
        {
            execContext = SynchronizationContext.Current;
        }

        public void Dispose()
        {
            StopWatching();
        }

        public async Task WatchProcessTree(Process process)
        {
            await WatchProcess(process);
        }

        public async Task WatchSingleProcess(Process process)
        {
            watcherToken = new CancellationTokenSource();
            while (!process.HasExited)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            OnTreeDestroyed();
        }

        public async Task WatchDirectoryProcesses(string directory, bool alreadyRunning, bool byProcessNames = false, int trackingDelay = 2000)
        {
            logger.Debug($"Watching dir processes {directory}, {alreadyRunning}, {byProcessNames}");
            // Get real path in case that original path is symlink or junction point
            var realPath = directory;
            try
            {
                realPath = Paths.GetFinalPathName(directory);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to get target path for a directory {directory}");
            }

            if (byProcessNames)
            {
                await WatchDirectoryByProcessNames(realPath, alreadyRunning, trackingDelay);
            }
            else
            {
                await WatchDirectory(realPath, alreadyRunning, trackingDelay);
            }
        }

        public void StopWatching()
        {
            watcherToken?.Cancel();
            watcherToken?.Dispose();
        }
        public static bool IsWatchableByProcessNames(string directory)
        {
            var realPath = directory;
            try
            {
                realPath = Paths.GetFinalPathName(directory);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to get target path for a directory {directory}");
            }

            var executables = Directory.GetFiles(realPath, "*.exe", SearchOption.AllDirectories);
            return executables.Count() > 0;
        }

        private async Task WatchDirectoryByProcessNames(string directory, bool alreadyRunning, int trackingDelay = 2000)
        {
            if (!FileSystem.DirectoryExists(directory))
            {
                throw new DirectoryNotFoundException($"Cannot watch directory processes, {directory} not found.");
            }

            var executables = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
            if (executables.Count() == 0)
            {
                logger.Error($"Cannot watch directory processes {directory}, no executables found.");
                OnTreeDestroyed();
            }

            var procNames = executables.Select(a => Path.GetFileName(a)).ToList();
            var procNamesNoExt = executables.Select(a => Path.GetFileNameWithoutExtension(a)).ToList();
            watcherToken = new CancellationTokenSource();
            var startedCalled = false;
            var processStarted = false;
            var foundProcessId = 0;
            var failCount = 0;

            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                if (failCount == maxFailCount)
                {
                    OnTreeDestroyed();
                    return;
                }

                var processFound = false;
                try
                {
                    var processes = Process.GetProcesses().Where(a => a.SessionId != 0);
                    foreach (var process in processes)
                    {
                        if (process.TryGetMainModuleFileName(out var procPath))
                        {
                            if (procNames.Contains(Path.GetFileName(procPath)))
                            {
                                processFound = true;
                                processStarted = true;
                                foundProcessId = process.Id;
                                break;
                            }
                        }
                        else if (procNamesNoExt.Contains(process.ProcessName))
                        {
                            processFound = true;
                            processStarted = true;
                            foundProcessId = process.Id;
                            break;
                        }
                    }
                }
                catch (Exception e) when (failCount < maxFailCount)
                {
                    // This shouldn't happen, but there were some crash reports from Process.GetProcesses
                    failCount++;
                    logger.Error(e, "WatchDirectoryByProcessNames failed to check processes.");
                }

                if (!alreadyRunning && processFound && !startedCalled)
                {
                    OnTreeStarted(foundProcessId);
                    startedCalled = true;
                }

                if (!processFound && processStarted)
                {
                    OnTreeDestroyed();
                    return;
                }

                await Task.Delay(trackingDelay);
            }
        }

        private async Task WatchDirectory(string directory, bool alreadyRunning, int trackingDelay = 2000)
        {
            if (!FileSystem.DirectoryExists(directory))
            {
                throw new DirectoryNotFoundException($"Cannot watch directory processes, {directory} not found.");
            }

            watcherToken = new CancellationTokenSource();
            var startedCalled = false;
            var processStarted = false;
            var foundProcessId = 0;
            var failCount = 0;

            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                if (failCount == maxFailCount)
                {
                    OnTreeDestroyed();
                    return;
                }

                var processFound = false;
                try
                {
                    var processes = Process.GetProcesses().Where(a => a.SessionId != 0);
                    foreach (var process in processes)
                    {
                        if (process.TryGetMainModuleFileName(out var procPath))
                        {
                            if (procPath.IndexOf(directory, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                processFound = true;
                                processStarted = true;
                                foundProcessId = process.Id;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e) when (failCount < maxFailCount)
                {
                    // This shouldn't happen, but there were some crash reports from Process.GetProcesses
                    failCount++;
                    logger.Error(e, "WatchDirectory failed to check processes.");
                }

                if (!alreadyRunning && processFound && !startedCalled)
                {
                    OnTreeStarted(foundProcessId);
                    startedCalled = true;
                }

                if (!processFound && processStarted)
                {
                    OnTreeDestroyed();
                    return;
                }

                await Task.Delay(trackingDelay);
            }
        }

        private async Task WatchProcess(Process process)
        {
            watcherToken = new CancellationTokenSource();
            var ids = new List<int>() { process.Id };
            var failCount = 0;

            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                if (ids.Count == 0 || failCount == maxFailCount)
                {
                    OnTreeDestroyed();
                    return;
                }

                try
                {
                    var processes = Process.GetProcesses().Where(a => a.SessionId != 0);
                    var runningIds = new List<int>();
                    foreach (var proc in processes)
                    {
                        if (proc.TryGetParentId(out var parent))
                        {
                            if (ids.Contains(parent) && !ids.Contains(proc.Id))
                            {
                                ids.Add(proc.Id);
                            }
                        }

                        if (ids.Contains(proc.Id))
                        {
                            runningIds.Add(proc.Id);
                        }
                    }

                    ids = runningIds;
                }
                catch (Exception e) when (failCount < maxFailCount)
                {
                    // This shouldn't happen, but there were some crash reports from Process.GetProcesses
                    failCount++;
                    logger.Error(e, "WatchProcess failed to check processes.");
                }

                await Task.Delay(500);
            }
        }

        private void OnTreeStarted(int processId)
        {
            execContext.Post((a) => TreeStarted?.Invoke(this, new TreeStartedEventArgs { StartedId = processId }), null);
        }

        private void OnTreeDestroyed()
        {
            execContext.Post((a) => TreeDestroyed?.Invoke(this, EventArgs.Empty), null);
        }
    }
}
