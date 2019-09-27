using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace MudaeFarm
{
    public class UpdateChecker
    {
        public static readonly Version CurrentVersion = typeof(Program).Assembly.GetName().Version;

        readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("MudaeFarm"));

        public async Task RunAsync()
        {
            try
            {
                Log.Warning($"MudaeFarm v{CurrentVersion.ToString(3)} by chiya.dev");

                var latestRelease = await _client.Repository.Release.GetLatest("chiyadev", "MudaeFarm");
                var latestVersion = Version.Parse(latestRelease.Name.TrimStart('v'));

                if (CurrentVersion.CompareTo(latestVersion) >= 0)
                    return;

                var asset = latestRelease.Assets.FirstOrDefault(a => a.Name.Contains(".zip"));

                if (asset != null)
                {
                    Log.Info($"Downloading v{latestVersion.ToString(3)}...");

                    using (var memory = new MemoryStream())
                    {
                        // download release onto memory
                        using (var http = new HttpClient())
                        using (var stream = await http.GetStreamAsync(asset.BrowserDownloadUrl))
                            await stream.CopyToAsync(memory);

                        memory.Position = 0;

                        // extract to a temp place
                        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                        using (var archive = new ZipArchive(memory, ZipArchiveMode.Read, true))
                            archive.ExtractToDirectory(path);

                        // run new installation in update mode
                        Process.Start(new ProcessStartInfo
                        {
                            FileName        = new DirectoryInfo(path).EnumerateFiles("*.exe").First().FullName,
                            Arguments       = $"--reinstall=\"{AppDomain.CurrentDomain.BaseDirectory}\"",
                            UseShellExecute = false
                        });

                        Process.GetCurrentProcess().Kill();
                    }
                }

                try
                {
                    Process.Start(latestRelease.HtmlUrl);
                }
                catch
                {
                    // ignored
                }
            }
            catch (Exception e)
            {
                Log.Debug("Could not check for updates.", e);
            }
        }
    }
}