using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Microsoft.Extensions.Logging;

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
        readonly ILogger<MudaeCommandHandler> _logger;

        public MudaeCommandHandler(IDiscordClientService discord, IMudaeUserFilter userFilter, ILogger<MudaeCommandHandler> logger)
        {
            _discord    = discord;
            _userFilter = userFilter;
            _logger     = logger;
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
                {
                    var watch = Stopwatch.StartNew();

                    var message = await response.Task;

                    _logger.LogDebug($"Sent command '{command}' in channel '{channel.Name}' ({channel.Id}) and received Mudae response '{message.Content}' ({message.Embeds.Count} embeds) in {watch.Elapsed.TotalMilliseconds}ms.");

                    return message;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Sent command '{command}' in channel '{channel.Name}' ({channel.Id}) but did not receive expected Mudae response.");
                throw;
            }
            finally
            {
                client.MessageReceived -= handleMessage;
            }
        }
    }
}