using System;

namespace MudaeFarm
{
    public static class Utilities
    {
        public static string RandString(int len)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var buffer = new char[len];
            var random = new Random();

            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = chars[random.Next(chars.Length)];

            return new string(buffer);
        }

        public static void Lock<T>(this T value, Action<T> action)
        {
            lock (value)
                action(value);
        }

        public static TReturn Lock<T, TReturn>(this T value, Func<T, TReturn> func)
        {
            lock (value)
                return func(value);
        }
    }
}