using Frosty.Core.Windows;
using FrostySdk.Interfaces;
using System;

namespace Frosty.Core
{
    internal class FrostyTaskLogger : ILogger
    {
        private FrostyTaskWindow task;
        private Progress<string> progress = new Progress<string>();

        public FrostyTaskLogger(FrostyTaskWindow inTask)
        {
            task = inTask;
            progress.ProgressChanged += (s, text) => {
                if (text.StartsWith("progress:"))
                {
                    text = text.Replace("progress:", "");
                    task.Update(null, double.Parse(text.Trim()));
                }
                else
                {
                    task.Update(text);
                }
            };
        }

        public void Log(string text, params object[] vars)
        {
            var progInt = (IProgress<string>)progress;

            if (text.StartsWith("progress:"))
            {
                progInt.Report(text);
            }
            else
            {
                progInt.Report(string.Format(text, vars));
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }

        public void LogError(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }
    }
}
