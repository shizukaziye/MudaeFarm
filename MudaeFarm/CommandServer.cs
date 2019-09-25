using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class CommandServer
    {
        readonly Config _config;
        readonly DiscordSocketClient _client;

        public CommandServer(Config config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task EnsureCreatedAsync()
        {
            if (_config.CommandServerId != 0)
                return;

            try
            {
                var guild = await _client.CreateGuildAsync("MudaeFarm", await _client.GetOptimalVoiceRegionAsync());

                foreach (var channel in await guild.GetChannelsAsync())
                    await channel.DeleteAsync();

                var newChannel = await guild.CreateTextChannelAsync("commands");

                await newChannel.SendMessageAsync("This is your MudaeFarm command server where you can control the bot.\n" +
                                                  "\n" +
                                                  "Check <https://github.com/chiyadev/MudaeFarm#commands> for a detailed list of commands!");

                _config.CommandServerId = guild.Id;
                _config.Save();

                Log.Warning($"Initialized command server '{guild.Name}'! Check your Discord client.");
            }
            catch (Exception e)
            {
                Log.Warning("Could not initialize command server! Try creating it manually.", e);
            }
        }
    }
}