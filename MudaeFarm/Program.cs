using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                {
                    Log.Info($"Upgrading to v{UpdateChecker.CurrentVersion.ToString(3)}...");

                    // wait for old process to exit
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    var dir = Directory.CreateDirectory(arg.Substring(reinstallPrefix.Length).Trim('"'));

                    try
                    {
                        // delete all old files
                        foreach (var file in dir.EnumerateFiles())
                            file.Delete();
                    }
                    catch
                    {
                        // ignored
                    }

                    // copy ourselves in
                    foreach (var file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).EnumerateFiles())
                    {
                        try
                        {
                            var dest = Path.Combine(dir.FullName, file.Name);

                            file.CopyTo(dest, true);

                            Log.Debug(dest);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Could not copy: {file.Name}", e);
                        }
                    }

                    // run new installation
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = dir.EnumerateFiles("*.exe").First().FullName,
                        UseShellExecute = false
                    });

                    Process.GetCurrentProcess().Kill();
                    break;
                }
            }

            return true;
        }
    }
}