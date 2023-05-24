﻿using Frosty.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostyModManager
{
    class ModStatusToBitmapSourceConverter : IValueConverter
    {
        private static readonly ImageSource ErrorSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/ModImportError.png") as ImageSource;
        private static readonly ImageSource WarningSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyModManager;component/Images/ModImportWarning.png") as ImageSource;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isWarning = (bool)value;
            return (isWarning) ? WarningSource : ErrorSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }

    public struct ImportErrorInfo
    {
        public string Filename => filename;
        public string Error => error;
        public bool IsWarning => isWarning;

        public string filename;
        public string error;
        public bool isWarning;
    }

    /// <summary>
    /// Interaction logic for ImportResultsWindow.xaml
    /// </summary>
    public partial class InstallErrorsWindow : FrostyDockableWindow
    {
        private List<ImportErrorInfo> _errors;

        public InstallErrorsWindow(List<ImportErrorInfo> errors)
        {
            InitializeComponent();
            _errors = errors;

            if (!OperatingSystemHelper.IsWine())
            {
                CenterDialog();
            }
        }

        private async void FrostyTaskWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(750);

            errorListView.ItemsSource = _errors;

            if (OperatingSystemHelper.IsWine())
            {
                CenterDialog();
            }
        }

        private void CenterDialog()
        {
            Window mainWin = App.Current.MainWindow;
            if (mainWin != null)
            {
                double x = mainWin.Left + (mainWin.Width / 2.0);
                double y = mainWin.Top + (mainWin.Height / 2.0);

                Left = x - (Width / 2.0);
                Top = y - (Height / 2.0);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
