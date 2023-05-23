using Frosty.Core.Attributes;
using FrostySdk.Interfaces;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace FrostyCore
{
    public class FrostyLogger : ILogger, INotifyPropertyChanged
    {
        private const string logName = "frosty_log.txt";
        private bool isLogInitialized = false;
        private StringBuilder sb = new StringBuilder();
        public string LogText => sb.ToString();

        public void Initialize()
        {
            File.Delete(logName);
            isLogInitialized = true;
        }

        public void Log(string text, params object[] vars)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            string category = "[Core] ";
            var attr = assembly.GetCustomAttribute<PluginDisplayNameAttribute>();

            if (attr != null)
                category = "[" + attr.DisplayName + "] ";

            sb.AppendLine(string.Format("[" + DateTime.Now.ToLongTimeString() + "]: " + category + text, vars));
            RaisePropertyChanged("LogText");

            var formatted = string.Format(text, vars);
            LogToFile($"Log: {formatted}");
        }

        public void LogWarning(string text, params object[] vars)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            string category = "[Core] ";
            var attr = assembly.GetCustomAttribute<PluginDisplayNameAttribute>();

            if (attr != null)
                category = "[" + attr.DisplayName + "] ";

            sb.AppendLine(string.Format("[" + DateTime.Now.ToLongTimeString() + "]: " + category + "(WARNING) " + text, vars));
            RaisePropertyChanged("LogText");

            var formatted = string.Format(text, vars);
            LogToFile($"Warning: {formatted}");
        }

        public void LogError(string text, params object[] vars)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            string category = "[Core] ";
            var attr = assembly.GetCustomAttribute<PluginDisplayNameAttribute>();

            if (attr != null)
                category = "[" + attr.DisplayName + "] ";

            sb.AppendLine(string.Format("[" + DateTime.Now.ToLongTimeString() + "]: " + category + "(ERROR) " + text, vars));
            RaisePropertyChanged("LogText");

            var formatted = string.Format(text, vars);
            LogToFile($"Error: {formatted}");
        }

        public void AddBinding(UIElement elementToBind, DependencyProperty propertyToBind)
        {
            Binding b = new Binding("LogText")
            {
                Source = this,
                Mode = BindingMode.OneWay
            };

            BindingOperations.SetBinding(elementToBind, propertyToBind, b);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LogToFile(string text)
        {
            if (!isLogInitialized)
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(logName, true))
            {
                writer.WriteLine(text);
            }
        }
    }
}
