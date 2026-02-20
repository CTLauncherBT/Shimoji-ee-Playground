using System;
using System.IO;
using System.Linq;

namespace ShimojiPlaygroundApp
{
    public enum LogLevel
    {
        INFO,
        WARN,
        ERROR,
        CRASH
    }

    public static class Logger
    {
        private const int MAX_LOG_FILES = 10;

        private static readonly string LogDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Shimoji-ee", "Playground");

        private static readonly string LogFile;

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                LogFile = Path.Combine(LogDirectory, $"shimoji-ee_playground_{timestamp}.log");

                CleanupOldLogs();
            }
            catch { }
        }

        public static void Info(string message) => Write(LogLevel.INFO, message);
        public static void Warn(string message) => Write(LogLevel.WARN, message);
        public static void Error(string message) => Write(LogLevel.ERROR, message);
        public static void Error(string message, Exception ex) => Write(LogLevel.ERROR, $"{message} | {ex}");
        public static void Crash(string message) => Write(LogLevel.CRASH, message);
        public static void Crash(string message, Exception ex) => Write(LogLevel.CRASH, $"{message} | {ex}");

        private static void Write(LogLevel level, string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
            catch { }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var files = new DirectoryInfo(LogDirectory)
                    .GetFiles("shimoji-ee_playground_*.log")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Skip(MAX_LOG_FILES);

                foreach (var file in files)
                    file.Delete();
            }
            catch { }
        }
    }
}