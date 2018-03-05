using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TurtleBot.Services;

namespace TurtleBot
{
    internal class Program
    {
        private static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private ConfigModule _config;

        private async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true
            });

            _config = new ConfigModule(BuildConfig());

            RegisterSub();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            services.GetRequiredService<WalletService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();

            services.GetRequiredService<RainService>();

            await Task.Delay(-1);
        }

        private void RegisterSub()
        {
            _config.Enable("threshold", "rainBalanceThreshold");
            _config.Enable("register", "rainRegisterDelayS");
            _config.Enable("announce", "rainAnnounceDelayS");
            _config.Enable("check", "rainCheckIntervalS");
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<WalletService>()
                .AddSingleton<RainService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .BuildServiceProvider();
        }

        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }
    }
}