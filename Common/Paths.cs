using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Playnite.Native;

namespace PluginsCommon
{
    public class Paths
    {
        private const string longPathPrefix = @"\\?\";
        private const string longPathUncPrefix = @"\\?\UNC\";
        public static readonly char[] DirectorySeparators = new char[] { '\\', '/' };

        public static string GetFinalPathName(string path)
        {
            var h = Kernel32.CreateFile(path,
                0,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                Fileapi.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (path.StartsWith(@"\\"))
            {
                return path;
            }

            if (h == Winuser.INVALID_HANDLE_VALUE)
            {
                throw new Win32Exception();
            }

            try
            {
                var sb = new StringBuilder(1024);
                var res = Kernel32.GetFinalPathNameByHandle(h, sb, 1024, 0);
                if (res == 0)
                {
                    throw new Win32Exception();
                }

                var targetPath = sb.ToString();
                if (targetPath.StartsWith(longPathUncPrefix))
                {
                    return targetPath.Replace(longPathUncPrefix, @"\\");
                }
                else
                {
                    return targetPath.Replace(longPathPrefix, string.Empty);
                }
            }
            finally
            {
                Kernel32.CloseHandle(h);
            }
        }
        public static bool IsFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // Don't use Path.IsPathRooted because it fails on paths starting with one backslash.
            return Regex.IsMatch(path, @"^([a-zA-Z]:\\|\\\\)");
        }
    }
}
