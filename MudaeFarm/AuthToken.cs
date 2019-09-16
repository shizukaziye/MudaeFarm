using System;

namespace MudaeFarm
{
    public class AuthToken
    {
        readonly Config _config;

        public AuthToken(Config config)
        {
            _config = config;
        }

        public void EnsureInitialized()
        {
            if (!string.IsNullOrEmpty(_config.AuthToken))
                return;

            Log.Info("MudaeFarm requires your user token in order to proceed.\n" +
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

            _config.AuthToken = Console.ReadLine();
            _config.Save();
        }
    }
}