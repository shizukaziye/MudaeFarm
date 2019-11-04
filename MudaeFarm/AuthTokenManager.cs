using System;
using System.IO;

namespace MudaeFarm
{
    /// <summary>
    /// Loads the user token from disk.
    /// </summary>
    public class AuthTokenManager
    {
        readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "auth_token.txt");

        public readonly string Value;

        public AuthTokenManager()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));

            // legacy config import
            var legacyCfg = LegacyConfig.Load();

            if (legacyCfg != null)
                File.WriteAllText(_path, legacyCfg.AuthToken);

            // load token from filesystem
            if (File.Exists(_path))
                Value = File.ReadAllText(_path);

            if (string.IsNullOrWhiteSpace(Value))
            {
                Log.Info(
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
                    "You can read the license terms at: https://github.com/chiyadev/MudaeFarm/blob/master/LICENSE\n" +
                    "\n" +
                    "Enter your token:");

                Value = Console.ReadLine();

                File.WriteAllText(_path, Value);

                throw new DummyRestartException { Delayed = false };
            }

            Log.Info($"User token loaded from: {_path}");
        }

        public void Reset()
        {
            File.Delete(_path);

            throw new DummyRestartException();
        }
    }
}