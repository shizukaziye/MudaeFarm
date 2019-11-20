using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    /// <summary>
    /// An instance of the bot.
    /// </summary>
    public class MudaeFarm : IDisposable
    {
        public static LogSeverity DefaultDiscordLogLevel { get; set; } = LogSeverity.Info;

        readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel         = DefaultDiscordLogLevel,
            MessageCacheSize = 0
        });

        public MudaeFarm()
        {
            _client.Log += HandleLogAsync;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
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
                    token,
                    login,
                    config,
                    state
                };

                var modules = EnumerateModules(dependencies).ToArray();

                foreach (var module in modules)
                    module.Initialize();

                Log.Warning("Ready!");

                var tasks = modules.Select(m => new
                                    {
                                        name = m.GetType().Name,
                                        func = (Func<Task>) (() => m.RunAsync(cancellationToken))
                                    })
                                   .ToList();

                tasks.Add(new
                {
                    name = state.GetType().Name,
                    func = (Func<Task>) (() => state.RunAsync(cancellationToken)) // state manager is a special module
                });

                await Task.WhenAll(tasks.Select(async x =>
                {
                    while (true)
                    {
                        try
                        {
                            await x.func();
                            return;
                        }
                        catch (TaskCanceledException) { }
                        catch (DummyRestartException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"Restarting failed module '{x.name}'.", e);
                        }
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
            var text = message.Exception == null
                ? message.Message
                : $"{message.Message}: {message.Exception}";

            var severity = message.Severity;

            if (message.Message.Contains("Preemptive Rate limit"))
            {
                text     = "Rate limit triggered. Resuming in a few seconds...";
                severity = LogSeverity.Debug;
            }

            switch (severity)
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