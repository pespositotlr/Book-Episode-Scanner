using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace BookEpisodeScanner.Loggers
{
    class DiscordBot
    {
        private DiscordSocketClient _client;
        private Logger logger;
        private bool logToDiscord = false;
        private string _discordToken;
        private ulong _serverId;
        private ulong _channelId;

        public DiscordBot(IConfigurationRoot config)
        {
            _discordToken = config["discordToken"];
            _serverId = Convert.ToUInt64(config["serverId"]);
            _channelId = Convert.ToUInt64(config["generalChannelId"]);
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            logToDiscord = Convert.ToBoolean(config["sendToDiscordBot"]);
        }

        public async Task LogToDiscord()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            await _client.LoginAsync(Discord.TokenType.Bot, _discordToken);
            await _client.StartAsync();
            await Task.Delay(3000);
        }

        private Task Log(LogMessage msg)
        {
            logger.Log(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task PostMessage(string message)
        {
            if (!logToDiscord)
                return;

            try
            {
                var chnl = _client.GetGuild(_serverId).GetTextChannel(_channelId) as SocketTextChannel;
                await chnl.SendMessageAsync(message);
            } catch (Exception ex)
            {
                Console.WriteLine("Failed to post to Discord." + ex.ToString());
            }
        }
    }
}
