using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class MudaeFarm : IDisposable
    {
        readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel         = LogSeverity.Info,
            MessageCacheSize = 0
        });

        public MudaeFarm()
        {
            _client.Log += HandleLogAsync;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // check version
            var update = new UpdateChecker();

            await update.RunAsync();

            // retrieve auth token
            var token = new AuthTokenManager();

            // discord login
            var login = new DiscordLogin(_client, token);

            await login.RunAsync();

            try
            {
                // configuration manager
                var config = new ConfigManager(_client);

                await config.InitializeAsync();

                // state management
                var state = new MudaeStateManager(_client, config);

                // module initialization
                var dependencies = new object[]
                {
                    _client,
                    update,
                    token,
                    login,
                    config,
                    state
                };

                var modules = EnumerateModules(dependencies).ToArray();

                foreach (var module in modules)
                    module.Initialize();

                Log.Warning("Ready!");

                // keep running
                var tasks = modules.Select(m => (name: m.GetType().Name, task: m.RunAsync(cancellationToken))).ToList();

                tasks.Add((state.GetType().Name, state.RunAsync(cancellationToken)));

                await Task.WhenAll(tasks.Select(async x =>
                {
                    var (name, task) = x;

                    try
                    {
                        await task;
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception e)
                    {
                        Log.Warning($"Module '{name}' failed.", e);
                    }
                }));
            }
            finally
            {
                await _client.StopAsync();
            }
        }

        IEnumerable<IModule> EnumerateModules(IEnumerable<object> dependencies)
        {
            var deps = dependencies.ToDictionary(x => x.GetType());

            foreach (var type in GetType().Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IModule).IsAssignableFrom(t)))
            {
                IModule module;

                try
                {
                    var ctor = type.GetConstructors().FirstOrDefault();

                    if (ctor == null)
                        continue;

                    var args = ctor.GetParameters().Select(param => deps.FirstOrDefault(x => x.Key.IsAssignableFrom(param.ParameterType)).Value).ToArray();

                    module = (IModule) ctor.Invoke(args);
                }
                catch (Exception e)
                {
                    Log.Warning($"Could not initialize module '{type}'.", e);
                    continue;
                }

                yield return module;
            }
        }

        public void Dispose() => _client.Dispose();

        static Task HandleLogAsync(LogMessage message)
        {
            // these errors occur from using an old version of Discord.Net
            // they should not affect any functionality
            if (message.Message.Contains("Error handling Dispatch (TYPING_START)") ||
                message.Message.Contains("Unknown Dispatch (SESSIONS_REPLACE)") ||
                message.Message.Contains("Preemptive Rate limit"))
                return Task.CompletedTask;

            var text = message.Exception == null
                ? message.Message
                : $"{message.Message}: {message.Exception}";

            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    Log.Debug(text);
                    break;
                case LogSeverity.Verbose:
                    Log.Debug(text);
                    break;
                case LogSeverity.Info:
                    Log.Info(text);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(text);
                    break;
                case LogSeverity.Error:
                    Log.Error(text);
                    break;
                case LogSeverity.Critical:
                    Log.Error(text);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}