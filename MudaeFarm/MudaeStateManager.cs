using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class MudaeStateManager
    {
        // guildId - state
        readonly ConcurrentDictionary<ulong, MudaeState> _states = new ConcurrentDictionary<ulong, MudaeState>();

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;

        public MudaeStateManager(DiscordSocketClient client, ConfigManager config)
        {
            _client = client;
            _config = config;
        }

        public void Initialize() => _client.MessageReceived += HandleMessage;

        // channelId - completionSource
        readonly ConcurrentDictionary<ulong, TaskCompletionSource<MudaeState>> _stateCompletionSources
            = new ConcurrentDictionary<ulong, TaskCompletionSource<MudaeState>>();

        Task HandleMessage(SocketMessage message)
        {
            if (MudaeInfo.IsMudae(message.Author))
            {
                var state = new MudaeState();

                if (TimersUpParser.TryParse(_client, message, state) && _stateCompletionSources.TryRemove(message.Channel.Id, out var completionSource))
                    completionSource.TrySetResult(state);
            }

            return Task.CompletedTask;
        }

        public MudaeState GetState(IGuild guild) => _states.TryGetValue(guild.Id, out var state) ? state : new MudaeState();

        public async Task<MudaeState> RefreshStateAsync(SocketGuild guild)
        {
            var now   = DateTime.Now;
            var state = GetState(guild);

            // disallow too frequent refreshes
            if (now < state.LastUpdatedTime + _config.MinStateRefresh)
                return state;

            // select a bot channel to send command in
            var channel = guild.TextChannels.FirstOrDefault(c => _config.BotChannelIds.Contains(c.Id));

            if (channel == null)
                return state;

            await _semaphore.WaitAsync();
            try
            {
                var completionSource = new TaskCompletionSource<MudaeState>();

                _stateCompletionSources[channel.Id] = completionSource;

                // send command
                await channel.SendMessageAsync(_config.StateUpdateCommand);

                // wait with timeout
                using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                using (cancellationSource.Token.Register(completionSource.SetCanceled))
                    state = await completionSource.Task;

                // update cache
                _states[guild.Id] = state;

                return state;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}