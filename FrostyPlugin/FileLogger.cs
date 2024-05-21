using System;
using System.IO;

namespace Frosty.Core
{
    public static class FileLogger
    {
        private static object locks = new object();
        private const string logName = "executor.log";
        private static bool IsLogInit = false;

        public static void Init()
        {
            lock(locks)
            {
                File.WriteAllText(logName, "Logger started.\n");

                IsLogInit = true;
            }
        }

        public static void Info(string message)
        {
            if (!IsLogInit)
            {
                return;
            }

            lock(locks)
            {
                using (var stream = File.AppendText(logName))
                {
                    stream.WriteLine($"[{DateTime.Now}] {message}");
                }
            }
        }
    }
}
