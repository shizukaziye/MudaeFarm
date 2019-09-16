using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Octokit;

namespace MudaeFarm
{
    public class UpdateChecker
    {
        readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("MudaeFarm"));

        public async Task RunAsync()
        {
            var currentVersion = typeof(Program).Assembly.GetName().Version;

            Log.Warning($"MudaeFarm v{currentVersion.ToString(2)} by chiya.dev");
            Log.Info("Checking for updates...");

            var latestRelease = await _client.Repository.Release.GetLatest("chiyadev", "MudaeFarm");

            if (!Version.TryParse(latestRelease.Name.TrimStart('v'), out var latestVersion))
            {
                Log.Warning($"Unable to parse version number: {latestRelease.Name}");
                return;
            }

            if (currentVersion.CompareTo(latestVersion) >= 0)
                return;

            // newer version available
            Log.Warning($"Version v{latestVersion.ToString(2)} available: {latestRelease.HtmlUrl}");

            try
            {
                Process.Start(latestRelease.HtmlUrl);
            }
            catch
            {
                // ignored
            }
        }
    }
}
