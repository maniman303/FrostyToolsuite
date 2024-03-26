﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System;

namespace Frosty.Core
{
    public class FrostyConfiguration
    {
        public ImageSource Thumbnail { get; private set; }

        public string GamePath { get; }
        public string ProfileName { get; }
        public string GameName { get; private set; }

        public FrostyConfiguration()
        {
            Thumbnail = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;component/Images/Warning.png") as ImageSource;
        }

        public FrostyConfiguration(string profile) : this()
        {
            ProfileName = profile;
            GamePath = Config.Get<string>("GamePath", "", ConfigScope.Game, profile);

            var exeLocation = System.IO.Path.Combine(GamePath, ProfileName) + ".exe";

            FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exeLocation);
            GameName = vi.ProductName;

            // Try to extract the icon
            try
            {
                Icon sysicon = GetFileIcon(exeLocation);

                Thumbnail = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    sysicon.Handle,
                    new Int32Rect(0, 0, 32, 32),
                    BitmapSizeOptions.FromEmptyOptions());

                sysicon.Dispose();
            }
            catch
            {

            }
        }

        private Icon GetFileIcon(string name)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = 0x000000100 | 0x000000010 | 0x000000000;

            SHGetFileInfo(
                name,
                0x00000080,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                flags);

            // Copy (clone) the returned icon to a new object, thus allowing us 
            // to call DestroyIcon immediately
            Icon icon = (Icon) Icon.FromHandle(shfi.hIcon).Clone();

            return icon;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public const int NAMESIZE = 80;
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NAMESIZE)]
            public string szTypeName;
        };

        [DllImport("Shell32.dll")]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );
    }
}
