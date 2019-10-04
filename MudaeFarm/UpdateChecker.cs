using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using FileMode = System.IO.FileMode;

namespace MudaeFarm
{
    public class UpdateChecker : IModule
    {
        public static readonly Version CurrentVersion = typeof(Program).Assembly.GetName().Version;

        readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("MudaeFarm"));

        void IModule.Initialize() { }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await CheckAsync();

                // check for updates every hour
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }

        async Task CheckAsync()
        {
            Release release;
            Version version;

            try
            {
                release = await _client.Repository.Release.GetLatest("chiyadev", "MudaeFarm");

                if (!Version.TryParse(release.Name.TrimStart('v'), out version))
                    return;

                if (CurrentVersion.CompareTo(version) >= 0)
                    return;
            }
            catch (Exception e)
            {
                Log.Debug("Could not check for updates.", e);

                return;
            }

            try
            {
                var asset = release.Assets.FirstOrDefault(a => a.Name.Contains(".zip"));

                if (asset == null)
                    return;

                Log.Info($"Downloading v{version.ToString(3)}...");

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
                        await ExtractToDirectoryAsync(archive, path);

                    // run new installation in update mode
                    var current = Process.GetCurrentProcess();

                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = new DirectoryInfo(path).EnumerateFiles("*.exe").First().FullName,
                        Arguments       = $"--reinstall=\"{AppDomain.CurrentDomain.BaseDirectory}\"",
                        UseShellExecute = false
                    });

                    current.Kill();
                }
            }
            catch (Exception e)
            {
                Log.Debug("Could not install updates.", e);
            }
        }

        static async Task ExtractToDirectoryAsync(ZipArchive archive, string path)
        {
            // we cannot use ZipFileExtensions.ExtractToDirectory
            // https://github.com/dotnet/corefx/issues/26996
            foreach (var entry in archive.Entries)
            {
                var filePath = Path.Combine(path, entry.FullName);

                if (entry.FullName.EndsWith("/") && string.IsNullOrEmpty(entry.Name))
                {
                    // https://stackoverflow.com/questions/40223451/how-to-tell-if-a-ziparchiveentry-is-directory
                    Directory.CreateDirectory(filePath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var entryStream = entry.Open())
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                    await entryStream.CopyToAsync(stream);
            }
        }
    }
}