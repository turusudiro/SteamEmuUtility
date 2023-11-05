using System.Diagnostics;
using System.Threading;

namespace ProcessCommon
{
    public static class ProcessUtilities
    {
        public static Process TryStartProcess(string executablePath)
        {
            return TryStartProcess(executablePath, string.Empty, string.Empty, 1, 1000, false);
        }
        public static Process TryStartProcess(string executablePath, string arguments)
        {
            return TryStartProcess(executablePath, arguments, string.Empty, 1, 1000, false);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir)
        {
            return TryStartProcess(executablePath, arguments, workingdir, 1, 1000, false);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts)
        {
            return TryStartProcess(executablePath, arguments, workingdir, maxAttempts, 1000, false);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts, int IntervalMilliseconds)
        {
            return TryStartProcess(executablePath, arguments, workingdir, maxAttempts, IntervalMilliseconds, false);
        }
        public static Process TryStartProcess(string executablePath, string arguments, string workingdir, int maxAttempts, int IntervalMilliseconds, bool shellExecute)
        {
            var process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.UseShellExecute = shellExecute;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingdir;
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
        public static bool IsErrorDialog(Process process, string error)
        {
            // You can implement logic to identify error dialogs based on process details
            // For example, you can check the process title, or other attributes.
            return process.MainWindowTitle.Contains(error); // Modify this condition as needed.
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
}
