using System;
using System.Threading.Tasks;

namespace MudaeFarm
{
    public class Program
    {
        static async Task Main()
        {
            while (true)
            {
                try
                {
                    using (var mudaeFarm = new MudaeFarm())
                        await mudaeFarm.RunAsync();

                    return;
                }
                catch (DummyRestartException) { }
                catch (Exception e)
                {
                    // fatal error recovery
                    Log.Error(null, e);
                }

                Log.Info("Restarting in 10 seconds...");

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
