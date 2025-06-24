using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostyModManager.Windows
{
    /// <summary>
    /// Interaction logic for PrelaunchWindow2.xaml
    /// </summary>
    public partial class PrelaunchWindow2 : FrostyDockableWindow
    {
        private List<FrostyConfiguration> configs = new List<FrostyConfiguration>();
        private FrostyConfiguration defaultConfig = null;

        Config ini = new Config();

        public PrelaunchWindow2()
        {
            InitializeComponent();
        }

        private void LaunchConfig(string profile /*Config config, string filename*/)
        {
            //App.configFilename = filename;
            //Config.Load(config); // Load game config

            // load profiles
            if (!ProfilesLibrary.Initialize(profile))
            {
                FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Mod Manager");
                Close();
                return;
            }

            //if (!ProfilesLibrary.Initialize(Config.Get<string>("Init", "Profile", "")))
            //{
            //    FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Editor");
            //    Close();
            //    return;
            //}

            if (ProfilesLibrary.RequiresKey && ProfilesLibrary.DataVersion == (int)ProfileVersion.Fifa19)
            {
                byte[] keyData = null;
                if (!File.Exists(ProfilesLibrary.CacheName + ".key"))
                {
                    // prompt for encryption key
                    KeyPromptWindow keyPromptWin = new KeyPromptWindow();
                    if (keyPromptWin.ShowDialog() == false)
                    {
                        FrostyMessageBox.Show("Encryption key not entered. Unable to load profile.", "Frosty Editor");
                        return;
                    }

                    keyData = keyPromptWin.EncryptionKey;
                    using (NativeWriter writer = new NativeWriter(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Create)))
                        writer.Write(keyData);
                }
                else
                {
                    // otherwise just read the key from file
                    keyData = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
                }

                // add primary encryption key
                byte[] key = new byte[0x10];
                Array.Copy(keyData, key, 0x10);
                KeyManager.Instance.AddKey("Key1", key);

                if (keyData.Length > 0x10)
                {
                    // add additional encryption keys
                    key = new byte[0x10];
                    Array.Copy(keyData, 0x10, key, 0, 0x10);
                    KeyManager.Instance.AddKey("Key2", key);

                    key = new byte[0x4000];
                    Array.Copy(keyData, 0x20, key, 0, 0x4000);
                    KeyManager.Instance.AddKey("Key3", key);
                }
            }

            // launch Mod Manager
            SplashWindow splashWin = new SplashWindow();
            App.Current.MainWindow = splashWin;
            splashWin.Show();
        }

        private static bool ValidateWorkingDirContent()
        {
            var dir = Directory.GetCurrentDirectory();

            var files = Directory.GetFiles(dir).Select(x => new FileInfo(x)).Select(x => x.Name.ToLower()).ToList();

            if (files.All(x => x != "frostymodmanager.exe"))
            {
                return false;
            }

            if (files.All(x => x != "frostysdk.dll"))
            {
                return false;
            }

            if (files.All(x => x != "frostycore.dll"))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateWorkingDirAccess()
        {
            var dir = Directory.GetCurrentDirectory();

            try
            {
                var filePath = Path.Combine(dir, "test.test");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var sw = File.CreateText(filePath);
                sw.WriteLine("test");

                sw.Close();

                var res = File.ReadAllText(filePath);

                if (!res.Contains("test"))
                {
                    return false;
                }

                File.Delete(filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool ValidateDesktopDirAccess()
        {
            if (!OperatingSystemHelper.IsWine())
            {
                return true;
            }

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrWhiteSpace(dir))
            {
                return false;
            }

            if (!Directory.Exists(dir))
            {
                return false;
            }

            try
            {
                var filePath = Path.Combine(dir, "frosty.test");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var sw = File.CreateText(filePath);
                sw.WriteLine("test");

                sw.Close();

                var res = File.ReadAllText(filePath);

                if (!res.Contains("test"))
                {
                    return false;
                }

                File.Delete(filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ValidateWorkingDirContent())
            {
                var message = "Working directory does not match Frosty Mod Manager installation location.\r\n";

                if (OperatingSystemHelper.IsWine())
                {
                    message += "\r\nOn Linux make sure Frosty Mod Manager is run from a directory accessible via a Wine drive.";
                    message += "\r\nIf Wine is run from a Flatpak application, make sure that application has access to the Frosty Mod Manager location (can be set up with Flatseal).";
                }

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            if (!ValidateWorkingDirAccess())
            {
                var message = "Working directory does not have read and write access.\r\n";

                if (OperatingSystemHelper.IsWine())
                {
                    message += "\r\nOn Linux make sure Frosty Mod Manager is run from a directory accessible via a Wine drive that is not Z.";
                    message += "\r\nIf Wine is run from a Flatpak application, make sure that application has read and write access to the Frosty Mod Manager location (can be set up with Flatseal).";
                }

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            if (!ValidateDesktopDirAccess())
            {
                var message = "Desktop direcotry cannot be accessed.\r\n";

                message += "\r\nOn Linux make sure application running Wine (like Bottles, Lutris) has access to /home/{user}/Desktop directory.";
                message += "\r\nYou can use Flatseal to add this access to Flatpak applications.";
                message += "\r\nIt is recommended to select 'All user files' for maximum compatibility.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
                Close();
                return;
            }

            RefreshConfigurationList();

            RemoveConfigButton.IsEnabled = false;
            LaunchConfigButton.IsEnabled = false;

            //ini.LoadEntries("DefaultSettings.ini");

            string defaultConfigName = Config.Get<string>("DefaultProfile", null);
            //string defaultConfigName = ini.GetEntry("Init", "DefaultConfiguration", "");

            foreach (FrostyConfiguration name in configs)
            {
                if (name.ProfileName == defaultConfigName)
                {
                    defaultConfig = name;
                }
            }

            ConfigList.SelectedItem = defaultConfig;
        }

        private void RefreshConfigurationList()
        {
            configs.Clear();

            foreach (string profile in Config.GameProfiles)
            {
                try
                {
                    configs.Add(new FrostyConfiguration(profile));
                }
                catch (System.IO.FileNotFoundException)
                {
                    Config.RemoveGame(profile); // couldn't find the exe, so remove it from the profile list
                    Config.Save();
                }
            }
            //foreach (string s in Directory.EnumerateFiles("./", "FrostyModManager*.ini"))
            //{
            //    try
            //    {
            //        FrostyConfiguration config = new FrostyConfiguration(s);
            //        configs.Add(config);
            //    }
            //    catch (Exception /*ex*/)
            //    {
            //        //FrostyMessageBox.Show("Couldn't load profile from '" + s + "': \n\n" + ex.ToString());
            //    }
            //}

            ConfigList.ItemsSource = configs;
        }

        private void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveConfigButton.IsEnabled = true;
            LaunchConfigButton.IsEnabled = true;
        }

        private void TryShowFlatpakMessage()
        {
            if (Config.Get("FlatpakMessage", OperatingSystemHelper.IsWine()))
            {
                Config.Add("FlatpakMessage", false);

                var message = "If Frosty is run through Flatpak application (Bottles, Lutris, Heroic), then make sure to select 'All user files' in Flatseal for that application.";
                message += "\r\n\r\nOtherwise Frosty Mod Manager might crash.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
            }
        }

        private void NewConfigButton_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlatpakMessage();

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.exe (Game Executable)|*.exe",
                Title = "Choose Game Executable"
            };

            if (ofd.ShowDialog() == false)
            {
                FrostyMessageBox.Show("No game executable chosen.", "Frosty Mod Manager");
                return;
            }

            if (OperatingSystemHelper.IsWine() && !DriveHelper.IsZDrive(ofd.FileName))
            {
                var sb = new StringBuilder();
                sb.Append("Game is not located on Wine Z: drive, which is not recommended.\r\n\r\n");
                sb.Append("This can cause broken sym-links and game not booting, especially when game is launched through sandboxed environment like flatpak.");
                sb.Append("\r\n\r\nAdd game via Z: drive for better stability.");

                FrostyMessageBox.Show(sb.ToString(), "Frosty Mod Manager");
            }

            AddGameProfile(ofd.FileName, out var errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                FrostyMessageBox.Show(errorMessage, "Frosty Mod Manager");
            }

            ConfigList.Items.Refresh();
        }

        private static bool CheckGameProfile(string path)
        {
            FileInfo fi = new FileInfo(path);

            return ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4));
        }

        private void AddGameProfile(string path, out string errorMessage)
        {
            errorMessage = string.Empty;

            FileInfo fi = new FileInfo(path);

            // try to load game profile 
            if (!ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4)))
            {
                errorMessage = "There was an error when trying to load game using specified profile.";
                return;
            }

            // make sure config doesnt already exist
            foreach (FrostyConfiguration config in configs)
            {
                if (config.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                {
                    errorMessage = "That game already has a configuration.";
                    return;
                }
            }

            // create
            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
            configs.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
            Config.Save();
        }

        private async void ConfigList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
                return;

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }
            ConfigList.SelectedIndex = -1;
        }

        private void RemoveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrostyMessageBox.Show("Are you sure you want to delete this configuration?", "Frosty Mod Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrostyConfiguration selectedItem = ConfigList.SelectedItem as FrostyConfiguration;

                Config.RemoveGame(selectedItem.ProfileName);
                //if (File.Exists(selectedItem.Filename))
                //{
                //    File.Delete(selectedItem.Filename);
                //}

                configs.Remove(selectedItem);
                ConfigList.Items.Refresh();

                ConfigList.SelectedIndex = 0;
                Config.Save();
            }
        }

        private async void LaunchConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
            { 
                return;
            }

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }

            ConfigList.SelectedIndex = -1;
        }

        private async void ScanForGamesButton_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlatpakMessage();

            var games = new List<string>();

            await Task.Delay(1);

            CancellationTokenSource cancelToken = new CancellationTokenSource();

            FrostyTaskWindow.Show("Scanning for games", "", (logger) =>
            {
                logger.Log("Scanning registry...");

                using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node"))
                {
                    int totalCount = 0;

                    var regGames = IterateSubKeys(lmKey, ref totalCount);

                    games.AddRange(regGames);
                }

                if (OperatingSystemHelper.IsWine())
                {
                    logger.Log("Scanning Z: drive...");
                }

                games.AddRange(ScanZDirectory(cancelToken));
            }, showCancelButton: true, cancelCallback: (logger) => cancelToken.Cancel());

            games = games.Select(x => x.Trim()).Distinct().ToList();

            games.Sort((x, y) => string.Compare(x, y, true) * -1);

            foreach (var game in games)
            {
                FileLogger.Info($"Scanning found game candidate: '{game}'.");

                AddGameProfile(game, out _);
            }

            ConfigList.Items.Refresh();
        }

        private class PathItem
        {
            public string Path { get; set; }
            public int Depth { get; set; }
        }

        private List<string> ScanZDirectory(CancellationTokenSource cancelToken)
        {
            var res = new List<string>();

            var rootPath = "Z:\\home\\";

            if (!Directory.Exists(rootPath))
            {
                FileLogger.Info($"Drive '{rootPath}' was not found during scanning.");
                return res;
            }

            var queue = new Queue<PathItem>();
            
            queue.Enqueue(new PathItem { Path = rootPath, Depth = 1 });

            var mountPath = "Z:\\run\\media";
            if (Directory.Exists(mountPath))
            {
                queue.Enqueue(new PathItem { Path = mountPath, Depth = 10 });
            }

            string[] files;
            string[] dirs;

            while (queue.Count > 0)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return res;
                }

                var item = queue.Dequeue();

                if (!Directory.Exists(item.Path))
                {
                    continue;
                }

                try
                {
                    files = Directory.GetFiles(item.Path, "*.exe");
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    if (CheckGameProfile(file))
                    {
                        res.Add(file);
                    }
                }

                if (item.Depth >= 20)
                {
                    continue;
                }

                try
                {
                    dirs = Directory.GetDirectories(item.Path).Where(d =>
                    {
                        var dirTempName = Path.GetFileName(d);

                        if (string.IsNullOrWhiteSpace(dirTempName))
                        {
                            return false;
                        }

                        dirTempName = dirTempName.Trim().ToLower();

                        if (dirTempName.StartsWith("$"))
                        {
                            return false;
                        }

                        if (dirTempName == "cache" || dirTempName == "config" || dirTempName == "tmp")
                        {
                            return false;
                        }

                        if (dirTempName.StartsWith(".") && dirTempName != ".local" && dirTempName != ".var")
                        {
                            return false;
                        }

                        return true;
                    }).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var dir in dirs)
                {
                    queue.Enqueue(new PathItem { Path = dir, Depth = item.Depth + 1 });
                }
            }

            return res;
        }

        private List<string> IterateSubKeys(RegistryKey subKey, ref int totalCount)
        {
            var res = new List<string>();

            foreach (string subKeyName in subKey.GetSubKeyNames())
            {
                try
                {
                    res.AddRange(IterateSubKeys(subKey.OpenSubKey(subKeyName), ref totalCount));
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            foreach (string subKeyValue in subKey.GetValueNames())
            {
                if (subKeyValue.IndexOf("Install Dir", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    string installDir = subKey.GetValue("Install Dir") as string;
                    if (string.IsNullOrEmpty(installDir))
                        continue;
                    if (!Directory.Exists(installDir))
                        continue;

                    foreach (string filename in Directory.EnumerateFiles(installDir, "*.exe"))
                    {
                        if (CheckGameProfile(filename))
                        {
                            res.Add(filename);

                            totalCount++;
                        }
                    }
                }
            }

            return res;
        }
    }
}
