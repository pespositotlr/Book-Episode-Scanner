using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookEpisodeScanner
{
    class DiscordBot
    {
        private DiscordSocketClient _client;
        private IConfigurationRoot _config;
        private Logger logger;

        public DiscordBot(IConfigurationRoot config)
        {
            _config = config;
            logger = new Logger();
            logger.localLogLocation = config["localLogLocation"];
        }

        public async Task LogToDiscord()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            var token = _config["discordToken"];

            await _client.LoginAsync(Discord.TokenType.Bot, token);
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
            ulong serverid = Convert.ToUInt64(_config["serverId"]);
            ulong channelId = Convert.ToUInt64(_config["generalChannelId"]);

            try
            {
                var chnl = _client.GetGuild(serverid).GetTextChannel(channelId) as SocketTextChannel;
                await chnl.SendMessageAsync(message);
            }catch (Exception ex)
            {
                Console.WriteLine("Failed to post to Discord." + ex.ToString());
            }
        }
    }
}
