using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace MudaeFarm
{
    /// <summary>
    /// Responsible for updating the bot using GitHub releases.
    /// </summary>
    public class UpdateChecker : IModule
    {
        public static readonly Version CurrentVersion = typeof(Program).Assembly.GetName().Version;

        readonly ConfigManager _config;

        public UpdateChecker(ConfigManager config)
        {
            _config = config;
        }

        void IModule.Initialize() { }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // check for updates every hour
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);

                if (_config.AutoUpdate)
                    await CheckAsync();
            }
        }

        static readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("MudaeFarm"));

        public static async Task CheckAsync()
        {
            // prevent updates
            Log.Warning("Updates have been skipped.");
            return;

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

                    // close log so updater can delete it
                    Log.Close();

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
                using (var stream = File.Open(filePath, System.IO.FileMode.Create, FileAccess.Write))
                    await entryStream.CopyToAsync(stream);
            }
        }

        public static async Task InstallUpdateAsync(string path)
        {
            Log.Info($"Upgrading to v{CurrentVersion.ToString(3)}...");

            // wait for old process to exit
            await Task.Delay(TimeSpan.FromSeconds(1));

            var dir = Directory.CreateDirectory(path);

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
        }
    }
}