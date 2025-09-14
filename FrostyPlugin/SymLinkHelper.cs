using FrostyModManager;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Frosty.Core
{
    public static class SymLinkHelper
    {
        private static IntPtr _symlinkModule = IntPtr.Zero;
        private static ConvertWindowsPathDelegate _convertWindowsPathDelegate;
        private static IsWindowsPathSymlinkDelegate _isWindowsPathSymlinkDelegate;
        private static DeleteWindowsPathDelegate _deleteWindowsPathDelegate;
        private static CreateWindowsSymlinkDelegate _createWindowsSymlinkDelegate;

        public const int BatchSize = 8;

        private const string registryPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Environment";
        private const string envVarName = "PATHEXT";

        private static bool _areHardLinksSupported = false;
        public static bool AreHardLinksSupported => _areHardLinksSupported;

        private static bool _areSymLinksLinuxSupported = false;
        public static bool AreSymLinksSupported => !OperatingSystemHelper.IsWine() || _areSymLinksLinuxSupported;

        public static void Initialize(string modPath)
        {
            TestHardLinks(modPath);

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

            if (!InitializeSymlinksLibrary())
            {
                FileLogger.Info("Could not initialize symlink library.");
                _areSymLinksLinuxSupported = false;
                return;
            }

            _areSymLinksLinuxSupported = true;
        }

        private static bool InitializeSymlinksLibrary()
        {
            _symlinkModule = SafeLoadLibrary("ThirdParty/wine-symlink.dll.so");
            if (_symlinkModule == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                FileLogger.Info($"Failed to load wine-symlink.dll.so with error code {errorCode}.");
                return false;
            }

            IntPtr pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "ConvertWindowsPath");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find ConvertWindowsPath in wine-symlink.dll.so.");
                return false;
            }

            _convertWindowsPathDelegate = Marshal.GetDelegateForFunctionPointer<ConvertWindowsPathDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "IsWindowsPathSymlink");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find IsWindowsPathSymlink in wine-symlink.dll.so.");
                return false;
            }

            _isWindowsPathSymlinkDelegate = Marshal.GetDelegateForFunctionPointer<IsWindowsPathSymlinkDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "DeleteWindowsPath");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find DeleteWindowsPath in wine-symlink.dll.so.");
                return false;
            }

            _deleteWindowsPathDelegate = Marshal.GetDelegateForFunctionPointer<DeleteWindowsPathDelegate>(pAddressOfFunctionToCall);

            pAddressOfFunctionToCall = GetProcAddress(_symlinkModule, "CreateWindowsSymlink");
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FileLogger.Info("Failed to find CreateWindowsSymlink in wine-symlink.dll.so.");
                return false;
            }

            _createWindowsSymlinkDelegate = Marshal.GetDelegateForFunctionPointer<CreateWindowsSymlinkDelegate>(pAddressOfFunctionToCall);

            return true;
        }

        public static void DeleteDirectorySafe(string path)
        {
            if (!Directory.Exists(path))
            {
                FileLogger.Info($"Directory delete aborted. Path '{path}' does not exists.");
                return;
            }

            if (!_areSymLinksLinuxSupported)
            {
                Directory.Delete(path, true);
                return;
            }

            if (!_deleteWindowsPathDelegate(path))
            {
                FileLogger.Info($"Could not safely remove directory '{path}'.");

                throw new Exception($"Could not safely remove directory '{path}'.");
            }
        }

        public static void DeleteFileSafe(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (!_areSymLinksLinuxSupported)
            {
                File.Delete(path);
                return;
            }

            if (!_deleteWindowsPathDelegate(path))
            {
                FileLogger.Info($"Could not safely remove file '{path}'.");

                throw new Exception($"Could not safely remove file '{path}'.");
            }
        }

        public static bool IsSymbolicLink(string path)
        {
            if (_areSymLinksLinuxSupported)
            {
                return _isWindowsPathSymlinkDelegate(path);
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

        public static bool DoesDirectoryContainSymLinks(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            if (IsSymbolicLink(path))
            {
                return true;
            }

            var queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                var dirPath = queue.Dequeue();
                var files = Directory.GetFiles(dirPath);

                if (files.Any(f => IsSymbolicLink(f)))
                {
                    return true;
                }

                var subDirs = Directory.GetDirectories(dirPath);
                if (subDirs.Any(s => IsSymbolicLink(s)))
                {
                    return true;
                }

                foreach (var subDir in subDirs)
                {
                    queue.Enqueue(subDir);
                }
            }

            return false;
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

            if (!_createWindowsSymlinkDelegate(source, destination))
            {
                FileLogger.Info($"Could not create symlink for source '{source}' and destination '{destination}'.");
                throw new Exception($"Could not create symlink for source '{source}' and destination '{destination}'.");
            }
        }

        private static string GetLinuxPath(string path)
        {
            var sb = new StringBuilder(2048);
            if (!_convertWindowsPathDelegate(path, sb, sb.Capacity))
            {
                FileLogger.Info("Convert delegate returned false");
                return path;
            }

            var linuxPath = sb.ToString();

            return linuxPath;
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

        public static IntPtr SafeLoadLibrary(string path)
        {
            // save old error mode
            uint oldMode;
            SetThreadErrorMode(SEM_FAILCRITICALERRORS | SEM_NOOPENFILEERRORBOX, out oldMode);

            try
            {
                return LoadLibrary(path);
            }
            finally
            {
                // restore previous mode
                SetThreadErrorMode(oldMode, out _);
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        // LoadLibrary and GetProcAddress are from kernel32
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        private const uint SEM_FAILCRITICALERRORS = 0x0001;
        private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // Delegate that matches the signature of your function
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool ConvertWindowsPathDelegate(
            [MarshalAs(UnmanagedType.LPStr)]  string path,
            [MarshalAs(UnmanagedType.LPStr)]  StringBuilder buffer,
            int bufferSize
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool IsWindowsPathSymlinkDelegate(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool DeleteWindowsPathDelegate(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate bool CreateWindowsSymlinkDelegate(string source, string destination);
    }
}
