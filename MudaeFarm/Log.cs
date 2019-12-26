using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MudaeFarm
{
    public static class Log
    {
        public static bool Enabled { get; set; } = true;
        public static ConsoleColor? Color { get; set; }

        static TextWriter _writer = File.CreateText("_log.txt");

        static readonly object _logLock = new object();

        public static void Close()
        {
            lock (_logLock)
            {
                if (_writer == null)
                    return;

                _writer.Dispose();
                _writer = null;
            }
        }

        public const ConsoleColor DebugColor = ConsoleColor.DarkGray;
        public const ConsoleColor InfoColor = ConsoleColor.Gray;
        public const ConsoleColor WarningColor = ConsoleColor.Yellow;
        public const ConsoleColor ErrorColor = ConsoleColor.Red;

        public static void Debug(string message, Exception exception = null) => Write(Color ?? DebugColor, "[dbug] ", message, exception);
        public static void Info(string message, Exception exception = null) => Write(Color ?? InfoColor, "[info] ", message, exception);
        public static void Warning(string message, Exception exception = null) => Write(Color ?? WarningColor, "[warn] ", message, exception);
        public static void Error(string message, Exception exception = null) => Write(Color ?? ErrorColor, "[erro] ", message, exception);

        static void Write(ConsoleColor color, string prefix, string message, Exception e)
        {
            if (!Enabled)
                return;

            prefix += $"[{DateTime.Now:hh:mm:ss}] ";

            var builder = new StringBuilder();
            var title   = null as string;

            if (message != null)
                foreach (var line in SplitLines(message.Trim()))
                    builder.AppendLine(title = prefix + line);

            if (e != null)
                foreach (var line in SplitLines(e.ToString()))
                    builder.AppendLine(title = prefix + line);

            var text = builder.ToString();

            lock (_logLock)
            {
                Console.ForegroundColor = color;

                _writer?.Write(text);
                _writer?.Flush();

                Console.Write(text);

                if (title != null)
                {
                    if (title.Length > 100)
                        title = title.Substring(0, 97) + "...";

                    Console.Title = "MudaeFarm — " + title;
                }
            }
        }

        static IEnumerable<string> SplitLines(string str) => str.Replace("\r", "").Split('\n');
    }
}