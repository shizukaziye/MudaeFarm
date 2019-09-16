using System;
using System.Collections.Generic;
using System.Text;

namespace MudaeFarm
{
    public static class Log
    {
        static readonly object _logLock = new object();

        public static void Debug(string message, Exception exception = null) => Write(ConsoleColor.DarkGray, "[dbug] ", message, exception);
        public static void Info(string message, Exception exception = null) => Write(ConsoleColor.Gray, "[info] ", message, exception);
        public static void Warning(string message, Exception exception = null) => Write(ConsoleColor.Yellow, "[warn] ", message, exception);
        public static void Error(string message, Exception exception = null) => Write(ConsoleColor.Red, "[erro] ", message, exception);

        static void Write(ConsoleColor color, string prefix, string message, Exception e)
        {
            var builder = new StringBuilder();

            if (message != null)
                foreach (var line in SplitLines(message.Trim()))
                    builder.AppendLine(prefix + line);

            if (e != null)
                foreach (var line in SplitLines(e.ToString()))
                    builder.AppendLine(prefix + line);

            lock (_logLock)
            {
                Console.ForegroundColor = color;

                Console.Write(builder.ToString());
            }
        }

        static IEnumerable<string> SplitLines(string str) => str.Replace("\r", "").Split('\n');
    }
}
