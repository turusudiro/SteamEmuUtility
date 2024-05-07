﻿using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PluginsCommon
{
    // Based on https://github.com/JosefNemec/Playnite
    public enum FileSystemItem
    {
        File,
        Directory
    }

    public static partial class FileSystem
    {
        [DllImport("kernel32.dll")]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
        out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
        out uint lpTotalNumberOfClusters);

        private static ILogger logger = LogManager.GetLogger();
        private const string longPathPrefix = @"\\?\";
        private const string longPathUncPrefix = @"\\?\UNC\";

        /// <summary>
        /// Determines the architecture type of the specified executable file.
        /// </summary>
        /// <param name="exePath">The path to the executable file.</param>
        /// <returns>
        /// 64/32/0.
        /// NOTE
        /// 0 means Unknown/null/error.
        /// </returns>
        public static string GetArchitectureType(string exePath)
        {
            try
            {
                using (FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        fs.Seek(0x3C, SeekOrigin.Begin); // Move to the PE header offset
                        int peOffset = br.ReadInt32(); // Read the PE header offset

                        fs.Seek(peOffset + 4, SeekOrigin.Begin); // Move to the PE signature offset
                        ushort peSignature = br.ReadUInt16(); // Read the PE signature

                        if (peSignature == 0x014C) // PE32 (32-bit)
                        {
                            return "32"; // 32-bit executable
                        }
                        else if (peSignature == 0x8664) // PE32+ (64-bit)
                        {
                            return "64"; // 64-bit executable
                        }
                        else
                        {
                            return "0"; // Unknown architecture
                        }
                    }
                }
            }
            catch
            {
                return "0"; // Error occurred
            }
        }

        public static Process DeleteSymbolicLink(string linkPath, string targetPath)
        {
            var targetdirectory = FixPathLength(targetPath);
            var linkdirectory = FixPathLength(linkPath);
            //var linkdirectoryparent = Path.GetDirectoryName(linkdirectory);
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process { StartInfo = psi };
            process.Start();

            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine($"mklink /d \"{linkdirectory}\" \"{targetdirectory}\"");
                }
            }

            return process;
        }

        /// <summary>
        /// Create a <c>Symlink</c> and return false if it's failed.
        /// </summary>
        /// <param name="linkPath">the path that will be created</param>
        /// <param name="targetPath">the path (relative or absolute) that the new symbolic link refers to (the source folder that want to link)</param>
        public static bool CreateSymbolicLink(string linkPath, string targetPath)
        {
            var targetdirectory = FixPathLength(targetPath);
            var linkdirectory = FixPathLength(linkPath);
            var linkdirectoryparent = Path.GetDirectoryName(linkdirectory);
            if (!DirectoryExists(linkdirectoryparent))
            {
                CreateDirectory(linkdirectoryparent);
            }

            // Run mklink command
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process { StartInfo = psi };
            process.Start();

            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine($"mklink /d \"{linkdirectory}\" \"{targetdirectory}\"");
                }
            }

            process.WaitForExit();
            string errorMessage = process.StandardError.ReadToEnd();
            if (!errorMessage.IsNullOrWhiteSpace())
            {
                return false;
            }
            return true;
        }
        private static Process DeleteSymbolicLink(string path)
        {
            return ProcessCommon.ProcessUtilities.StartProcessHidden("cmd.exe", $"/c rd \"{path}\"");
        }
        public static bool IsSymbolicLink(string path)
        {
            var directory = FixPathLength(path);
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            // Check if the directory attributes include ReparsePoint (symbolic link)
            return (directoryInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        public static void TryWriteText(string path, object content)
        {
            var directory = FixPathLength(path);
            // Check if the path has a file extension
            if (!string.IsNullOrEmpty(Path.GetExtension(directory)))
            {
                // If it has an extension, it's a file path
                // Ensure the directory exists before writing the file
                string directoryPath = Path.GetDirectoryName(directory);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            try
            {
                switch (content)
                {
                    case string singleText:
                        File.WriteAllText(path, singleText);
                        break;

                    case List<string> textList:
                        File.WriteAllLines(path, textList);
                        break;
                }
            }
            catch { }
        }

        public static void CreateDirectory(string path)
        {
            CreateDirectory(path, false);
        }

        public static void CreateDirectory(string path, bool clean)
        {
            var directory = FixPathLength(path);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (Directory.Exists(directory))
            {
                if (clean)
                {
                    Directory.Delete(directory, true);
                }
                else
                {
                    return;
                }
            }

            Directory.CreateDirectory(directory);
        }

        public static void PrepareSaveFile(string path)
        {
            path = FixPathLength(path);
            CreateDirectory(Path.GetDirectoryName(path));
            DeleteFile(path, true);
        }

        public static bool IsDirectoryEmpty(string path)
        {
            path = FixPathLength(path);
            if (Directory.Exists(path))
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }
            else
            {
                return true;
            }
        }

        public static void DeleteFile(string path, bool includeReadonly = false)
        {
            path = FixPathLength(path);
            if (!File.Exists(path))
            {
                return;
            }

            if (includeReadonly)
            {
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(path, attr ^ FileAttributes.ReadOnly);
                }
            }

            File.Delete(path);
        }

        public static void CreateFile(string path, bool createDirectory = false)
        {
            if (createDirectory)
            {
                string dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
            }
            path = FixPathLength(path);
            File.Create(path).Dispose();
        }

        public static bool CopyFile(string sourcePath, string targetPath, bool overwrite = true)
        {
            sourcePath = FixPathLength(sourcePath);
            targetPath = FixPathLength(targetPath);

            try
            {
                logger.Debug($"Copying file {sourcePath} to {targetPath}");
                PrepareSaveFile(targetPath);
                File.Copy(sourcePath, targetPath, overwrite);
                return true;
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error copying file {sourcePath} to {targetPath}");
                return false;
            }
        }

        public static bool MoveFile(string sourcePath, string targetPath)
        {
            sourcePath = FixPathLength(sourcePath);
            targetPath = FixPathLength(targetPath);
            logger.Debug($"Moving file {sourcePath} to {targetPath}");
            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                logger.Debug($"Source path and target path are the same: {sourcePath}");
                return false;
            }

            if (!File.Exists(sourcePath))
            {
                logger.Debug($"Source doesn't exists: {sourcePath}");
                return false;
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            else if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(sourcePath, targetPath);
            return true;
        }

        public static void DeleteDirectory(string path)
        {
            path = FixPathLength(path);
            if (Directory.Exists(path))
            {
                if (IsSymbolicLink(path))
                {
                    DeleteSymbolicLink(path).WaitForExit(500);
                    return;
                }
                Directory.Delete(path, true);
            }
        }

        public static void ClearDirectory(string path)
        {
            path = FixPathLength(path);
            if (!Directory.Exists(path))
            {
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (FileInfo file in dir.GetFiles())
            {
                DeleteFile(file.FullName);
            }

            foreach (DirectoryInfo directory in dir.GetDirectories())
            {
                ClearDirectory(directory.FullName);
                DeleteDirectory(directory.FullName);
            }
        }

        public static void DeleteDirectory(string path, bool includeReadonly)
        {
            path = FixPathLength(path);
            if (!Directory.Exists(path))
            {
                return;
            }

            if (includeReadonly)
            {
                foreach (var s in Directory.GetDirectories(path))
                {
                    DeleteDirectory(s, true);
                }

                foreach (var f in Directory.GetFiles(path))
                {
                    DeleteFile(f, true);
                }

                var dirAttr = File.GetAttributes(path);
                if ((dirAttr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(path, dirAttr ^ FileAttributes.ReadOnly);
                }

                Directory.Delete(path, false);
            }
            else
            {
                DeleteDirectory(path);
            }
        }

        public static bool CanWriteToFolder(string folder)
        {
            folder = FixPathLength(folder);
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                using (var stream = File.Create(Path.Combine(folder, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ReadFileAsStringSafe(string path, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException exc)
                {
                    logger.Debug($"Can't read from file, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
            }

            throw new IOException($"Failed to read {path}", ioException);
        }

        public static byte[] ReadFileAsBytesSafe(string path, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (IOException exc)
                {
                    logger.Debug($"Can't read from file, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
            }

            throw new IOException($"Failed to read {path}", ioException);
        }

        public static Stream CreateWriteFileStreamSafe(string path, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    return new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                }
                catch (IOException exc)
                {
                    logger.Debug($"Can't open write file stream, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
            }

            throw new IOException($"Failed to read {path}", ioException);
        }

        public static Stream OpenReadFileStreamSafe(string path, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    return new FileStream(path, FileMode.Open, FileAccess.Read);
                }
                catch (IOException exc)
                {
                    logger.Debug($"Can't open read file stream, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
            }

            throw new IOException($"Failed to read {path}", ioException);
        }

        public static void WriteStringLinesToFile(string path, IEnumerable<string> content, bool useUtf8 = false)
        {
            path = FixPathLength(path);
            PrepareSaveFile(path);
            if (useUtf8)
            {
                File.WriteAllLines(path, content, Encoding.UTF8);
            }
            else
            {
                File.WriteAllLines(path, content);
            }
        }

        public static void WriteStringToFile(string path, string content, bool useUtf8 = false, bool createDirectory = false)
        {
            path = FixPathLength(path);
            if (createDirectory)
            {
                string dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
            }
            PrepareSaveFile(path);
            if (useUtf8)
            {
                File.WriteAllText(path, content, Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(path, content);
            }
        }

        public static string[] ReadStringLinesFromFile(string path, bool useUtf8 = false)
        {
            path = FixPathLength(path);
            if (useUtf8)
            {
                return File.ReadAllLines(path, Encoding.UTF8);
            }
            else
            {
                return File.ReadAllLines(path);
            }
        }

        public static string ReadStringFromFile(string path, bool useUtf8 = false)
        {
            path = FixPathLength(path);
            if (useUtf8)
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            else
            {
                return File.ReadAllText(path);
            }
        }

        public static void WriteStringToFileSafe(string path, string content, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    PrepareSaveFile(path);
                    File.WriteAllText(path, content);
                    return;
                }
                catch (IOException exc)
                {
                    logger.Error(exc, $"Can't write to a file, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
            }

            //throw new IOException($"Failed to write to {path}", ioException);
        }

        public static void DeleteFileSafe(string path, int retryAttempts = 5)
        {
            path = FixPathLength(path);
            if (!File.Exists(path))
            {
                return;
            }

            IOException ioException = null;
            for (int i = 0; i < retryAttempts; i++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException exc)
                {
                    logger.Debug($"Can't delete file, trying again. {path}");
                    ioException = exc;
                    Task.Delay(500).Wait();
                }
                catch (UnauthorizedAccessException exc)
                {
                    logger.Error(exc, $"Can't delete file, UnauthorizedAccessException. {path}");
                    return;
                }
            }

            throw new IOException($"Failed to delete {path}", ioException);
        }

        public static long GetFileSize(string path)
        {
            path = FixPathLength(path);
            return new FileInfo(path).Length;
        }


        public static long GetFileSizeOnDisk(string path)
        {
            return GetFileSizeOnDisk(new FileInfo(FixPathLength(path)));
        }


        public static long GetFileSizeOnDisk(FileInfo info)
        {
            // From https://stackoverflow.com/a/3751135
            uint dummy, sectorsPerCluster, bytesPerSector;
            int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
            if (result == 0) throw new System.ComponentModel.Win32Exception();
            uint clusterSize = sectorsPerCluster * bytesPerSector;
            uint hosize;
            uint losize = GetCompressedFileSizeW(info.FullName, out hosize);
            long size;
            size = (long)hosize << 32 | losize;
            return ((size + clusterSize - 1) / clusterSize) * clusterSize;
        }

        public static long GetDirectorySize(string path)
        {
            return GetDirectorySize(new DirectoryInfo(FixPathLength(path)));
        }

        private static long GetDirectorySize(DirectoryInfo dir)
        {
            try
            {
                long size = 0;
                // Add file sizes.
                FileInfo[] fis = dir.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }

                // Add subdirectory sizes.
                DirectoryInfo[] dis = dir.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += GetDirectorySize(di);
                }
                return size;
            }
            catch
            {
                return 0;
            }
        }

        public static long GetDirectorySizeOnDisk(string path)
        {
            return GetDirectorySizeOnDisk(new DirectoryInfo(FixPathLength(path)));
        }

        public static long GetDirectorySizeOnDisk(DirectoryInfo dirInfo)
        {
            long size = 0;

            // Add file sizes.
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                size += GetFileSizeOnDisk(file);
            }

            // Add subdirectory sizes.
            foreach (DirectoryInfo directory in dirInfo.GetDirectories())
            {
                size += GetDirectorySizeOnDisk(directory);
            }

            return size;
        }

        public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwrite = true)
        {
            sourceDirName = FixPathLength(sourceDirName);
            destDirName = FixPathLength(destDirName);
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(FixPathLength(path));
        }

        public static bool FileExists(string path)
        {
            return File.Exists(FixPathLength(path));
        }

        public static string FixPathLength(string path)
        {
            // Relative paths don't support long paths
            // https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=cmd
            if (!Paths.IsFullPath(path))
            {
                return path;
            }

            if (path.Length >= 258 && !path.StartsWith(longPathPrefix))
            {
                if (path.StartsWith(@"\\"))
                {
                    return longPathUncPrefix + path.Substring(2);
                }
                else
                {
                    return longPathPrefix + path;
                }
            }

            return path;
        }
    }
}