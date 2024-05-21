﻿using System.Runtime.InteropServices;
using System;
using System.IO;
using FrostyModManager;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace Frosty.Core
{
    public static class SymLinkHelper
    {
        private const string linuxSufix = "linux_result";
        private const string linuxTemp = "linux_temp";

        private const string registryPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment";
        private const string envVarName = "PATHEXT";

        private const int waitLoops = 33;
        private const int waitTime = 6;

        private const string lsPath = "/bin/ls";
        private const string lnPath = "/bin/ln";
        private const string rmPath = "/bin/rm";

        private static bool _areHardLinksSupported = false;
        public static bool AreHardLinksSupported => _areHardLinksSupported;

        private static bool _areSymLinksLinuxSupported = false;
        public static bool AreSymLinksSupported => !OperatingSystemHelper.IsWine() || _areSymLinksLinuxSupported;

        public static void Initialize(string modPath)
        {
            TestHardLinks(modPath);

            if (!OperatingSystemHelper.IsWine(false))
            {
                _areSymLinksLinuxSupported = false;
                return;
            }

            if (!UpdateRegistry())
            {
                FileLogger.Info("Registry update failed.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            var envVar = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrWhiteSpace(envVar))
            {
                FileLogger.Info($"Missing value for env var {envVarName}.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            if (envVar != "." && !envVar.Contains(".;") && !envVar.EndsWith(";."))
            {
                if (envVar.EndsWith(";"))
                {
                    Environment.SetEnvironmentVariable(envVarName, envVar + ".");
                }
                else
                {
                    Environment.SetEnvironmentVariable(envVarName, envVar + ";.");
                }
            }

            if (!File.Exists(lsPath) || !File.Exists(lnPath) || !File.Exists(rmPath))
            {
                FileLogger.Info($"Missing file '{lsPath}' or '{lnPath}' or '{rmPath}'.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            _areSymLinksLinuxSupported = true;

            var tempFile = Path.Combine(modPath, linuxTemp);

            try
            {
                if (!File.Exists(tempFile))
                {
                    File.Create(tempFile).Close();
                }
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Exception when testing symbolic links. Details: {ex.Message}");
                _areSymLinksLinuxSupported = false;
                return;
            }

            try
            {
                IsSymbolicLinkLinux(tempFile);
            }
            catch
            {
                FileLogger.Info("Failed to test symbolic link initialization.");
                _areSymLinksLinuxSupported = false;
            }

            File.Delete(tempFile);
        }

        public static void DeleteDirectorySafe(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            path = Path.GetFullPath(path);

            if (!OperatingSystemHelper.IsWine() || !_areSymLinksLinuxSupported)
            {
                Directory.Delete(path, true);
                return;
            }

            FileLogger.Info($"Removing directory '{path}'.");

            if (IsSymbolicLinkLinux(path))
            {
                var linuxPath = GetLinuxPath(path);
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {rmPath} \"{linuxPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                proc.WaitForExit();

                for (int i = 0; i < waitLoops; i++)
                {
                    if (!Directory.Exists(path))
                    {
                        FileLogger.Info($"Symbolic link directory removed at try: {i}");
                        break;
                    }

                    Thread.Sleep(waitTime);
                }

                return;
            }

            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                DeleteFileSafe(file);
            }

            var dirs = Directory.GetDirectories(path);

            foreach (var dir in dirs)
            {
                DeleteDirectorySafe(dir);
            }

            Directory.Delete(path, true);
        }

        public static void DeleteFileSafe(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (!OperatingSystemHelper.IsWine() || !_areSymLinksLinuxSupported)
            {
                File.Delete(path);
                return;
            }

            if (!IsSymbolicLinkLinux(path))
            {
                File.Delete(path);
                return;
            }

            var linuxPath = GetLinuxPath(path);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {rmPath} \"{linuxPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            for (int i = 0; i < waitLoops; i++)
            {
                if (!File.Exists(path))
                {
                    FileLogger.Info($"Symbolic link file removed at try: {i}");
                    break;
                }

                Thread.Sleep(waitTime);
            }
        }

        public static void CreateSymlinkLinux(string source, string destination)
        {
            var isFile = File.Exists(source);

            if (!isFile && !Directory.Exists(source))
            {
                FileLogger.Info($"Symbolic Link aborted. Source '{source}' does not exists.");
                return;
            }

            if (!_areSymLinksLinuxSupported)
            {
                return;
            }

            var sourceLinux = GetLinuxPath(source);
            var destinationLinux = GetLinuxPath(destination);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {lnPath} -s \"{sourceLinux}\" \"{destinationLinux}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            for (int i = 0; i < waitLoops; i++)
            {
                if (isFile && File.Exists(destination))
                {
                    break;
                }
                else if (!isFile && Directory.Exists(destination))
                {
                    break;
                }

                Thread.Sleep(waitTime);
            }
        }

        public static bool IsSymbolicLink(string path)
        {
            if (OperatingSystemHelper.IsWine())
            {
                return IsSymbolicLinkLinux(path);
            }

            var isFile = File.Exists(path);

            if (!isFile && !Directory.Exists(path))
            {
                return false;
            }

            FileAttributes attributes;

            if (isFile)
            {
                var fi = new FileInfo(path);
                attributes = fi.Attributes;
            }
            else
            {
                var di = new DirectoryInfo(path);
                attributes = di.Attributes;
            }

            return (attributes & FileAttributes.ReparsePoint) != 0;
        }

        private static bool IsSymbolicLinkLinux(string path)
        {
            if (!_areSymLinksLinuxSupported)
            {
                return false;
            }

            path = CleanPath(path);

            var isFile = File.Exists(path);

            if (!isFile && !Directory.Exists(path))
            {
                return false;
            }

            var resSufix = $".{DateTime.Now:ddMMHHmmss}.{linuxSufix}";
            var resFilePath = $"{path}{resSufix}";

            var linuxPath = GetLinuxPath(path);

            File.Delete(resFilePath);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {lsPath} -l \"{linuxPath}\" > {linuxPath}{resSufix}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            string status = string.Empty;

            for (int i = 0; i < waitLoops; i++)
            {
                status = File.ReadAllText(resFilePath);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    break;
                }

                Thread.Sleep(waitTime);
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                FileLogger.Info($"Could not determine if '{path}' is a symbolic link.");
                throw new Exception($"Could not determine if '{path}' is a symbolic link.");
            }

            File.Delete(resFilePath);

            return status.ToLower().StartsWith("l");
        }

        private static string GetLinuxPath(string path)
        {
            var linuxPath = string.Empty;
            var realPath = GetRealPath(path);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winepath.exe",
                    Arguments = $"-u \"{realPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                linuxPath = proc.StandardOutput.ReadLine();
            }

            linuxPath = CleanPath(linuxPath);

            return linuxPath;
        }

        private static string CleanPath(string path)
        {
            while (path.EndsWith("/") || path.EndsWith("\\"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        private static string GetRealPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new Exception("Argument for GetRealPath was null or an empty string.");
            }

            var absolutePath = Path.GetFullPath(path);

            if (absolutePath.EndsWith(":\\") || absolutePath.EndsWith(":/"))
            {
                return absolutePath;
            }

            var name = Path.GetFileName(absolutePath);
            var parent = Path.GetDirectoryName(absolutePath);

            if (string.IsNullOrWhiteSpace(name))
            {
                return GetRealPath(parent);
            }

            string realName = string.Empty;

            try
            {
                if (Directory.Exists(parent))
                {
                    realName = Directory.GetFiles(parent, name).FirstOrDefault();

                    if (string.IsNullOrEmpty(realName))
                    {
                        realName = Directory.GetDirectories(parent, name).FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Crashed when visiting directory '{parent}' and looking for '{name}'.");

                throw ex;
            }

            if (string.IsNullOrEmpty(realName))
            {
                realName = name;
            }

            var realParent = GetRealPath(parent);

            return Path.Combine(realParent, Path.GetFileName(realName));
        }

        private static void TestHardLinks(string modPath)
        {
            var orgFile = Path.Combine(modPath, "hard_link_test.txt");
            var linkFile = Path.Combine(modPath, "hard_link_test_link.txt");

            try
            {
                if (!File.Exists(orgFile))
                {
                    File.Create(orgFile).Close();
                }
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Exception when testing hard links. Details: {ex.Message}");
                _areHardLinksSupported = false;
                return;
            }

            if (!File.Exists(orgFile))
            {
                FileLogger.Info("Could not create test file for hard linking.");
                _areHardLinksSupported = false;
                return;
            }

            CreateHardLink(orgFile, linkFile);

            if (!File.Exists(linkFile))
            {
                FileLogger.Info("Could not create hard link to the test file.");
                File.Delete(orgFile);
                _areHardLinksSupported = false;
                return;
            }

            File.Delete(linkFile);
            File.Delete(orgFile);

            _areHardLinksSupported = true;
        }

        private static bool UpdateRegistry()
        {
            if (!OperatingSystemHelper.IsWine())
            {
                return true;
            }

            var valueObj = Registry.GetValue(registryPath, envVarName, string.Empty);

            if (valueObj == null || !(valueObj is string) || string.IsNullOrWhiteSpace((string)valueObj))
            {
                FileLogger.Info("Missing registry key 'HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment\\PATHEXT'.");

                return false;
            }

            var value = (string)valueObj;

            if (value == "." || value.Contains(".;") || value.EndsWith(";."))
            {
                return true;
            }

            if (value.EndsWith(";"))
            {
                value += ".";
            }
            else
            {
                value += ";.";
            }

            Registry.SetValue(registryPath, envVarName, value);

            FileLogger.Info("Registry updated.");

            return true;
        }

        public static void CreateHardLink(string source, string destination)
        {
            if (!File.Exists(source))
            {
                FileLogger.Info($"Hard Link aborted. Source '{source}' does not exists.");
                return;
            }

            CreateHardLink(destination, source, IntPtr.Zero);
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );
    }
}
