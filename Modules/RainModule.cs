using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using TurtleBot.Services;

namespace TurtleBot.Modules
{
    public class RainModule : ModuleBase<SocketCommandContext>
    {
        private readonly RainService _rainService;
        private readonly WalletService _walletService;
        private readonly ConfigModule _config;

        public RainModule(RainService rainService, WalletService walletService, ConfigModule config)
        {
            _rainService = rainService;
            _walletService = walletService;
            _config = config;
        }

        [Command("rain")]
        [Summary("Checks the weather")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Rain([Remainder] string ignore = null)
        {
            try
            {
                switch (_rainService.State)
                {
                    case RainServiceState.CheckingBalance:
                        var balance = await _walletService.GetBalance(_rainService.BotWallet);
                        var missing = (_rainService.BalanceThreshold - balance) / 100.0M;
                        var embed = new EmbedBuilder()
                            .WithColor(new Color(114, 137, 218))
                            .WithTitle("See how to participate in the rain")
                            .WithUrl(_config["rainWikiUrl"])
                            .WithDescription(
                                $"The rain is still **{missing}** TRTL away. Donate to make it rain again! ```\n{_rainService.BotWallet.Address}```")
                            .WithThumbnailUrl(_config["rainImageUrlTRTL"])
                            .Build();
                        await ReplyAsync("", false, embed);
                        return;

                    case RainServiceState.BalanceExceeded:
                    case RainServiceState.AcceptingRegistrations:
                    case RainServiceState.Raining:
                        await ReplyAsync("Be patient, little turtle, it's raining soon.");
                        return;
                    default:
                        await ReplyAsync("The sky is blue, no rain for today...");
                        return;
                }
            }
            catch (Exception)
            {
                await ReplyAsync("The sky is blue, no rain for today...");
            }
        }

        [Command("rain")]
        [Summary("Controls the rain function")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireOwner]
        public async Task Rain(string subCommand, [Remainder] string ignore = null)
        {
            switch (subCommand.ToLowerInvariant())
            {
                case "start":
                    _rainService.Start();
                    await ReplyAsync("Better watch out for rain, my little turtles...");
                    return;
                case "stop":
                    _rainService.Stop();
                    await ReplyAsync("Okay, time to go home turtles, it's not raining today.");
                    return;
                case "reset":
                    _config.Reset();

                    await ReplyAsync("Succses: All configuration values have been reset");
                    break;
                default:

                    try
                    {
                        var value = _config.GetValue(subCommand);

                        if (string.IsNullOrEmpty(value))
                        {
                            break;
                        }

                        await ReplyAsync($"The current value is `{value}`");
                    }
                    catch
                    {
                        await ReplyAsync($"Error: `{subCommand}` is not a enabled");
                    }

                    return;
            }
        }

        [Command("rain")]
        [Summary("Controls the config for the bot")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireOwner]
        public async Task Rain(string subCommand, string value, [Remainder] string ignore = null)
        {
            try
            {
                _config.Execute($"{subCommand} {value}");

                await ReplyAsync($"Succses: The value of `{subCommand}` is now `{_config.GetValue(subCommand)}`");
            }
            catch
            {
                await ReplyAsync("Error: Unexpected value");
            }
        }
    }
}