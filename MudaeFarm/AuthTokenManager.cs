using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <summary>
    /// Loads the user token from disk.
    /// </summary>
    public class AuthTokenManager
    {
        static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "auth_tokens.json");

        // this is static so that the value is remembered across restarts
        static string _currentUser = "default";

        readonly Dictionary<string, string> _users = new Dictionary<string, string>();

        public string Value
        {
            get => _users.TryGetValue(_currentUser, out var value) ? value : null;

            private set
            {
                _users[_currentUser] = value;

                File.WriteAllText(_path, JsonConvert.SerializeObject(_users, Formatting.Indented));
            }
        }

        public AuthTokenManager()
        {
            try
            {
                // if we can, migrate old "auth_token.txt" plain text file to new "auth_tokens.json"
                var path = Path.Combine(Path.GetDirectoryName(_path) ?? "", "auth_token.txt");

                Value = File.ReadAllText(path);

                File.Delete(path);

                Log.Warning("Successfully migrated old auth tokens.");

                return;
            }
            catch (IOException) { }

            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? "");

            try
            {
                // load tokens from filesystem
                _users = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_path));

                Log.Info($"User tokens loaded from: {_path}");
            }
            catch (IOException)
            {
                Log.Info($"Failed to load user tokens from: {_path}");
            }

            while (!_users.ContainsKey(_currentUser))
            {
                Console.WriteLine(
                    "\n" +
                    "User profiles:\n" +
                    string.Concat(_users.Keys.Select(s => $"  - {s}\n")));

                Console.Write("Choose user: ");

                _currentUser = Console.ReadLine() ?? "";
            }

            Log.Info($"Selected user: {_currentUser}");

            if (string.IsNullOrEmpty(Value))
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

                Value = Console.ReadLine();

                // restart to remove inputted token from console
                throw new DummyRestartException { Delayed = false };
            }
        }

        public void Reset()
        {
            Value = null;

            throw new DummyRestartException();
        }
    }
}