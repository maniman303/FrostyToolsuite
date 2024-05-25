using System.Runtime.InteropServices;
using System;
using System.IO;
using FrostyModManager;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Microsoft.Win32;

namespace Frosty.Core
{
    public static class SymLinkHelper
    {
        public const int BatchSize = 8;

        private const string linuxTemp = "linux_temp";

        private const string registryPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment";
        private const string envVarName = "PATHEXT";

        private const int waitLoops = 33;
        private const int waitTime = 6;

        private const string symlinkBin = "./ThirdParty/wine-symlink-helper.exe.so";

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

            if (!UpdateEnviromentalVariables())
            {
                FileLogger.Info($"Missing value for env var {envVarName}.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            if (!File.Exists(symlinkBin))
            {
                FileLogger.Info($"Missing file '{symlinkBin}'");
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
                IsSymbolicLink(tempFile);
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Failed to test symbolic link initialization. Reason: {ex.Message}");
                _areSymLinksLinuxSupported = false;
            }

            File.Delete(tempFile);
        }

        public static void DeleteDirectorySafe(string path)
        {
            if (!Directory.Exists(path))
            {
                FileLogger.Info($"Directory delete aborted. Path '{path}' does not exists.");
                return;
            }

            path = Path.GetFullPath(path);

            if (!OperatingSystemHelper.IsWine() || !_areSymLinksLinuxSupported)
            {
                Directory.Delete(path, true);
                return;
            }

            var linuxPath = GetLinuxPath(path);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = symlinkBin,
                    Arguments = $"-r \"{linuxPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            
            try
            {
                proc.WaitForExit();
            }
            catch
            {
                FileLogger.Info("Process finished early on directory delete.");
            }

            proc.Close();

            for (int i = 0; i < waitLoops; i++)
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                Thread.Sleep(waitTime);
            }

            FileLogger.Info($"Could not determine if directory '{path}' was removed.");

            throw new Exception($"Could not determine if directory '{path}' was removed.");
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

            var linuxPath = GetLinuxPath(path);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = symlinkBin,
                    Arguments = $"-r \"{linuxPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            try
            {
                proc.WaitForExit();
            }
            catch
            {
                FileLogger.Info("Process finished early on file delete.");
            }

            proc.Close();

            for (int i = 0; i < waitLoops; i++)
            {
                if (!File.Exists(path))
                {
                    return;
                }

                Thread.Sleep(waitTime);
            }

            FileLogger.Info($"Could not determine if file '{path}' was removed.");

            throw new Exception($"Could not determine if file '{path}' was removed.");
        }

        public static bool DoesDirectoryContainSymLinks(string path)
        {
            if (!Directory.Exists(path) || !_areSymLinksLinuxSupported)
            {
                return false;
            }

            path = CleanPath(Path.GetFullPath(path));
            var linuxPath = GetLinuxPath(path);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = symlinkBin,
                    Arguments = $"-s \"{linuxPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            try
            {
                proc.WaitForExit();
            }
            catch
            {
                FileLogger.Info("Process finished early on directory scan.");
            }

            int exitCode;

            try
            {
                exitCode = proc.ExitCode;
            }
            catch
            {
                FileLogger.Info("Could not retrieve exit code for directory scan.");
                throw new Exception("Could not retrieve exit code for directory scan.");
            }

            proc.Close();

            if (exitCode < 0)
            {
                throw new Exception("Could not determine if directory has symbolic links.");
            }

            if (exitCode == 0)
            {
                return false;
            }

            return true;
        }

        public static void CreateSymlinkLinux(string source, string destination)
        {
            if (string.IsNullOrWhiteSpace(source) ||  string.IsNullOrWhiteSpace(destination))
            {
                FileLogger.Info($"Symbolic Link aborted. Invalid source '{source}' or destination '{destination}'.");
                return;
            }

            var isDirectory = Directory.Exists(source);

            if (!isDirectory && !File.Exists(source))
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
                    FileName = symlinkBin,
                    Arguments = $"-c \"{sourceLinux}\" \"{destinationLinux}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            
            try
            {
                proc.WaitForExit();
            }
            catch
            {
                FileLogger.Info("Process finished early on symlink creation.");
            }

            proc.Close();

            for (int i = 0; i < waitLoops; i++)
            {
                if (isDirectory && Directory.Exists(destination))
                {
                    return;
                }
                else if (!isDirectory && File.Exists(destination))
                {
                    return;
                }

                Thread.Sleep(waitTime);
            }

            FileLogger.Info($"Could not determine if sym link was created for source '{source}' and destination '{destination}'.");
            throw new Exception($"Could not determine if sym link was created for source '{source}' and destination '{destination}'.");
        }

        public static bool IsSymbolicLink(string path)
        {
            if (OperatingSystemHelper.IsWine())
            {
                return IsSymbolicLinkLinux(path);
            }

            var isDirectory = Directory.Exists(path);

            if (!isDirectory && !File.Exists(path))
            {
                return false;
            }

            FileAttributes attributes;

            if (isDirectory)
            {
                var di = new DirectoryInfo(path);
                attributes = di.Attributes;
            }
            else
            {
                var fi = new FileInfo(path);
                attributes = fi.Attributes;
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

            if (!Directory.Exists(path) && !File.Exists(path))
            {
                return false;
            }

            var linuxPath = GetLinuxPath(path);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = symlinkBin,
                    Arguments = $"-v \"{linuxPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            try
            {
                proc.WaitForExit();
            }
            catch
            {
                FileLogger.Info("Process finished early on symlink valdiation.");
            }


            int exitCode;

            try
            {
                exitCode = proc.ExitCode;
            }
            catch
            {
                FileLogger.Info("Could not retrieve exit code for symbolic link check.");
                throw new Exception("Could not retrieve exit code for symbolic link check.");
            }

            proc.Close();

            if (exitCode < 0)
            {
                FileLogger.Info($"Could not determine if '{path}' is a symbolic link.");
                throw new Exception($"Could not determine if '{path}' is a symbolic link.");
            }
            else if (exitCode == 1)
            {
                return true;
            }

            return false;
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

            proc.Close();

            linuxPath = CleanPath(linuxPath);

            return linuxPath;
        }

        private static string CleanPath(string path)
        {
            if (path.EndsWith(":/") || path.EndsWith(":\\"))
            {
                return path;
            }

            path = path.Trim();

            if (path == "./" || path == ".\\")
            {
                return path;
            }

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

        private static bool UpdateEnviromentalVariables()
        {
            var envVar = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrWhiteSpace(envVar))
            {
                _areSymLinksLinuxSupported = false;
                return false;
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

            return true;
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

        public static void HandleAggregateException(AggregateException ax)
        {
            var ex = ax.Flatten().InnerExceptions.FirstOrDefault();

            if (ex == null)
            {
                FileLogger.Info($"Retrieved empty Aggregate Exception. Details:\n{ax}");
                return;
            }

            FileLogger.Info($"Retrieved Aggregate Exception with following exception. Details:\n{ex}");

            throw ex;
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
