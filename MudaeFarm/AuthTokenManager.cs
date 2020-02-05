using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <summary>
    /// Loads user tokens from the disk.
    /// </summary>
    public class AuthTokenManager
    {
        static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "profiles.json");

        // this is static so that the value is remembered across restarts
        static string _currentProfile = "default";

        readonly Dictionary<string, string> _profiles = new Dictionary<string, string>();

        public string Profile
        {
            get => _currentProfile;

            private set => _currentProfile = value;
        }

        public string Token
        {
            get
            {
                var name = Profile;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _profiles.Count; i++)
                {
                    _profiles.TryGetValue(name, out var value);

                    // inherited profiles
                    if (value != null && _profiles.ContainsKey(value))
                    {
                        name = value;
                        continue;
                    }

                    return value;
                }

                throw new StackOverflowException("Detected circular references in inherited profiles.");
            }

            private set
            {
                _profiles[Profile] = value;

                File.WriteAllText(_path, JsonConvert.SerializeObject(_profiles, Formatting.Indented));
            }
        }

        public AuthTokenManager()
        {
            try
            {
                // migrate old "auth_token.txt" plain text file
                var path = Path.Combine(Path.GetDirectoryName(_path) ?? "", "auth_token.txt");

                Token = File.ReadAllText(path);

                File.Delete(path);

                Log.Warning("Successfully migrated old auth tokens.");

                return;
            }
            catch (IOException) { }

            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? "");

            try
            {
                // load tokens from filesystem
                _profiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_path));

                Log.Info($"Profiles loaded from: {_path}");
            }
            catch (IOException)
            {
                Log.Info($"Failed to load profiles from: {_path}");
            }

            while (!_profiles.ContainsKey(Profile))
            {
                Console.WriteLine(
                    "\n" +
                    "Profiles:\n" +
                    string.Concat(_profiles.Keys.Select(s => $"  - {s}\n")));

                Console.Write("Choose profile: ");

                Profile = Console.ReadLine() ?? "";
            }

            Log.Info($"Selected profile: {Profile}");

            if (string.IsNullOrEmpty(Token))
            {
                Console.WriteLine(
                    "\n" +
                    "MudaeFarm requires your user token in order to proceed.\n" +
                    "\n" +
                    "A user token is a long piece of text that is synonymous to your Discord password.\n" +
                    "How to find your token: https://github.com/chiyadev/MudaeFarm/blob/master/User%20tokens.md\n" +
                    "\n" +
                    "What happens when you enter your token:\n" +
                    "- MudaeFarm will save this token to the disk UNENCRYPTED.\n" +
                    "- MudaeFarm will authenticate to Discord using this token, ACTING AS YOU.\n" +
                    "\n" +
                    "MudaeFarm makes no guarantee regarding your account's privacy nor safety.\n" +
                    "If you are concerned, you may inspect MudaeFarm's complete source code at: https://github.com/chiyadev/MudaeFarm\n" +
                    "\n" +
                    "MudaeFarm is licensed under the MIT License. The authors of MudaeFarm shall not be held liable for any claim, damage or liability.\n" +
                    "You can read the license terms at: https://github.com/chiyadev/MudaeFarm/blob/master/LICENSE\n");

                Console.Write("Enter token: ");

                Token = Console.ReadLine();

                // restart to remove inputted token from console
                throw new DummyRestartException { Delayed = false };
            }
        }

        public void Reset()
        {
            Token = null;

            throw new DummyRestartException();
        }
    }
}