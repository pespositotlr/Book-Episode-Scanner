using BookEpisodeScanner.Entities;
using BookEpisodeScanner.Loggers;
using BookEpisodeScanner.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FODFujiTVMangaScanner.Classes
{
    /// <summary>
    /// Scans any url for a 200 code. Retries if it gets a 404 until it finds it or hits the maximum attempts.
    /// </summary>
    class GenericURLScanner
    {
        private ScannerSettings settings;
        DiscordBot bot;
        IConfigurationRoot config;
        Logger logger;
        EmailNotifier emailNotifier;
        int attemptNumber;
        bool done;
        string url;

        public GenericURLScanner(string urlToCheck, int maximumAttempts = 100000, int timeBetweenAttemptsMilliseconds = 600000)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            emailNotifier = new EmailNotifier(config);
            bot = new DiscordBot(config);

            url = urlToCheck;
            settings.MaximumAttempts = maximumAttempts;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
            done = false;
        }

        public async Task Run()
        {
            await bot.LogToDiscord();
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();

            logger.Log(String.Format("Beginning scan for generic URL {0}. Maximum number of attempts is {1}. The current time is {2}", url, settings.MaximumAttempts, DateTime.Now.ToString()));

            while (!done)
            {
                logger.Log(String.Format("Attempt number: {0}. Current time: {1}", attemptNumber, DateTime.Now.ToString()));

                try
                {

                    HttpStatusCode currentStatusCode = await WebHelper.GetUrlStatusCode(url, config["refererValue"]);

                    switch (currentStatusCode)
                    {
                        case HttpStatusCode.OK:
                            //Found what we're looking for, download the whole book
                            await RunFoundUrlProcess();
                            done = true;
                            break;
                        case HttpStatusCode.NotFound:
                            //Try again later (Units in milliseconds)
                            logger.Log(String.Format("Geric URL not found. Searching again in {0} minute(s). Current time is: {1}", timeBetweenAttemptsString, DateTime.Now.ToString()));
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            break;
                        default:
                            //Log that we somehow got some other error
                            logger.Log(String.Format("Got weird response code: {0}. Current time is: {0}", currentStatusCode.ToString(), DateTime.Now.ToString()));
                            emailNotifier.SendNotificationEmailError("Main", "Got weird response code: " + currentStatusCode.ToString());
                            break;
                    }

                    if (attemptNumber == settings.MaximumAttempts)
                        done = true;
                    else
                        attemptNumber++;

                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Got an error trying to get status code. Error: {0}. Current time is: {1}", ex.ToString(), DateTime.Now.ToString()));
                    await Wait(10000); //Sleep for 10 seconds before trying again.
                }

            }

            if (done)
                logger.Log("All done!");

            return;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private async Task RunFoundUrlProcess()
        {
            logger.Log(String.Format("URL found! Found {0} found at {1}", url, DateTime.Now.ToString()));
            emailNotifier.SendNotificationUrlFound(url);
            await bot.PostMessage(String.Format("URL found! Found {0} found at {1}", url, DateTime.Now.ToString()));

            await WebHelper.DownloadURL(config, url);
            return;
        }

    }
}
