using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace MudaeFarm
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            if (!await HandleArgsAsync(args))
            {
                Console.ReadKey();
                return;
            }

            while (true)
            {
                Console.Clear();

                Log.Warning($"MudaeFarm v{UpdateChecker.CurrentVersion.ToString(3)} by chiya.dev");

                try
                {
                    using (var mudaeFarm = new MudaeFarm())
                        await mudaeFarm.RunAsync();

                    return;
                }
                catch (DummyRestartException e)
                {
                    if (!e.Delayed)
                        continue;
                }
                catch (Exception e)
                {
                    // fatal error recovery
                    Log.Error(null, e);
                }

                Log.Info("Restarting in 10 seconds...");

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        static async Task<bool> HandleArgsAsync(IEnumerable<string> args)
        {
            const string verbose   = "--verbose";
            const string reinstall = "--reinstall=";

            foreach (var arg in args)
            {
                if (arg == verbose)
                    MudaeFarm.DefaultDiscordLogLevel = LogSeverity.Debug;

                else if (arg.StartsWith(reinstall))
                    await UpdateChecker.InstallUpdateAsync(arg.Substring(reinstall.Length).Trim('"'));
            }

            return true;
        }

        public static T2[] ToArray<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> selector) => enumerable.Select(selector).ToArray();
    }
}