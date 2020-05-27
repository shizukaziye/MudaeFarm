using System;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;

namespace MudaeFarm
{
    public interface IMudaeCommandHandler
    {
        Task<IUserMessage> SendAsync(IMessageChannel channel, string command, CancellationToken cancellationToken = default);
    }

    public class MudaeCommandHandler : IMudaeCommandHandler
    {
        readonly IDiscordClientService _discord;
        readonly IMudaeUserFilter _userFilter;

        public MudaeCommandHandler(IDiscordClientService discord, IMudaeUserFilter userFilter)
        {
            _discord    = discord;
            _userFilter = userFilter;
        }

        public async Task<IUserMessage> SendAsync(IMessageChannel channel, string command, CancellationToken cancellationToken = default)
        {
            var client = await _discord.GetClientAsync();

            await client.SendMessageAsync(channel.Id, command);

            var response = new TaskCompletionSource<IUserMessage>();

            Task handleMessage(MessageReceivedEventArgs e)
            {
                if (e.Message.Channel.Id == channel.Id && _userFilter.IsMudae(e.Message.Author) && e.Message is IUserMessage message)
                    response.TrySetResult(message);

                return Task.CompletedTask;
            }

            client.MessageReceived += handleMessage;

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                cancellationToken = linkedCts.Token;

                await using (cancellationToken.Register(() => response.TrySetCanceled(cancellationToken)))
                    return await response.Task;
            }
            finally
            {
                client.MessageReceived -= handleMessage;
            }
        }
    }
}