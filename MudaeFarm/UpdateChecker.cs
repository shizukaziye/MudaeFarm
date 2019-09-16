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
            try
            {
                var currentVersion = typeof(Program).Assembly.GetName().Version;

                Log.Warning($"MudaeFarm v{currentVersion.ToString(2)} by chiya.dev");

                var latestRelease = await _client.Repository.Release.GetLatest("chiyadev", "MudaeFarm");
                var latestVersion = Version.Parse(latestRelease.Name.TrimStart('v'));

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
            catch
            {
                Log.Info("Could not check for updates.");
            }
        }
    }
}