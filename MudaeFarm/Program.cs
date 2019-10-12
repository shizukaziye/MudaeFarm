using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MudaeFarm
{
    public class Program
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

                await UpdateChecker.CheckAsync();

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
            foreach (var arg in args)
            {
                const string reinstallPrefix = "--reinstall=";

                if (arg.StartsWith(reinstallPrefix))
                    await UpdateChecker.InstallUpdateAsync(arg.Substring(reinstallPrefix.Length).Trim('"'));
            }

            return true;
        }
    }
}