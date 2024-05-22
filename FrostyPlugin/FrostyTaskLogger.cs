using Frosty.Core.Windows;
using FrostySdk.Interfaces;
using System;

namespace Frosty.Core
{
    public class FrostyTaskLogger : ILogger
    {
        private FrostyTaskWindow task;
        private Progress<string> progress = new Progress<string>();
        private IProgress<string> progressReporter => progress;

        public FrostyTaskLogger(FrostyTaskWindow inTask)
        {
            task = inTask;
            progress.ProgressChanged += (s, text) => {
                if (text.StartsWith("progress:"))
                {
                    text = text.Replace("progress:", "").Trim();
                    task.Update(null, double.Parse(text.Trim()));
                }
                if (text.Contains("~"))
                {
                    var splits = text.Split('~');

                    if (splits.Length == 0)
                    {
                        return;
                    }

                    if (splits.Length == 1 && text.StartsWith("~"))
                    {
                        task.Update(null, double.Parse(splits[0].Trim()));
                        return;
                    }

                    if (splits.Length == 1 && text.EndsWith("~"))
                    {
                        task.Update(splits[0]);
                        return;
                    }

                    task.Update(splits[0], double.Parse(splits[1].Trim()));
                }
                else
                {
                    task.Update(text);
                }
            };
        }

        public void Log(string text, params object[] vars)
        {
            text = text.Trim();

            if (text.StartsWith("progress:"))
            {
                progressReporter.Report(text);
            }
            else
            {
                progressReporter.Report(string.Format(text, vars));
            }
        }

        public void LogProgress(string text, double value)
        {
            var progInt = (IProgress<string>)progress;

            progInt.Report($"{text}~{value}");
        }

        public void LogProgress(double value)
        {
            var progInt = (IProgress<string>)progress;

            progInt.Report($"~{value}");
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
