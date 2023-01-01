using BookEpisodeScanner.Entities;
using BookEpisodeScanner.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookEpisodeScanner.Classes
{
    class Scanner
    {
        private ScannerSettings settings;
        DiscordBot bot;
        IConfigurationRoot config;
        Logger logger;
        BookData previousBookData;
        int attemptNumber;
        bool done;
        string previewImageUrl;
        string finalImageUrl;
        string tokenQueryString;

        public Scanner(string bookId = "", string previousEpisodeId = "", string middleId = "", 
            int maximumPagesToDownload = 900, int maximumAttempts = 100000, int timeBetweenAttemptsMilliseconds = 600000)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger();
            logger.localLogLocation = config["localLogLocation"];
            bot = new DiscordBot(config);

            settings.BookId = bookId;
            settings.PreviousEpisodeId = previousEpisodeId; 
            settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.PreviousEpisodeId);
            settings.MiddleId = middleId;
            settings.MaximumPagesToDownload = maximumPagesToDownload;
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
            previousBookData = await WebHelper.GetBookData(config, settings.BookId, settings.PreviousEpisodeId);
            tokenQueryString = previousBookData.AdditionalQueryString;
            if (String.IsNullOrEmpty(settings.MiddleId))
                settings.MiddleId = StringHelper.GetMiddleIDFromS3Key(previousBookData);
            previewImageUrl = StringHelper.GetPreviewVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString);

            logger.Log(String.Format("Beginning scan for BookID {0} EpisodeID {1}. Next episode following {2}. Maximum number of attempts is {3}. The current time is {4}", settings.BookId, settings.CurrentEpisodeId, previousBookData.Title, settings.MaximumAttempts, DateTime.Now.ToString()));

            while (!done)
            {
                logger.Log(String.Format("Attempt number: {0}. Current time: {1}", attemptNumber, DateTime.Now.ToString()));

                try
                {

                    HttpStatusCode currentStatusCode = await WebHelper.GetUrlStatusCode(previewImageUrl);

                    switch (currentStatusCode)
                    {
                        case HttpStatusCode.OK:
                            //Found what we're looking for, download the whole book
                            await RunFoundEpisodeProcess();
                            done = true;
                            break;
                        case HttpStatusCode.NotFound:
                            //Try again later (Units in milliseconds)
                            logger.Log(String.Format("Episode of book not found. Searching again in {0} minute(s). Current time is: {1}", timeBetweenAttemptsString, DateTime.Now.ToString()));
                            await Wait(settings.TimeBetweenAttemptsMilliseconds);
                            break;
                        case HttpStatusCode.Forbidden:
                            //The token expired so go get a new one
                            logger.Log(String.Format("Got 403 forbidden error. Fetching new token. Current time is: {0}", DateTime.Now.ToString()));
                            BookData bookData = await WebHelper.GetBookData(config, settings.BookId, settings.PreviousEpisodeId);
                            tokenQueryString = bookData.AdditionalQueryString;
                            previewImageUrl = StringHelper.GetPreviewVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString);
                            break;
                        default:
                            //Email that we somehow got some other error
                            logger.Log(String.Format("Got weird response code: {0}. Current time is: {0}", currentStatusCode.ToString(), DateTime.Now.ToString()));
                            EmailHelper.SendNotificationEmailError(config, "Main", "Got weird response code: " + currentStatusCode.ToString());
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

        private async Task RunFoundEpisodeProcess()
        {
            logger.Log(String.Format("Episode of book found! Found BookID {0} EpisodeID {1} found at {2}", settings.BookId, settings.CurrentEpisodeId, DateTime.Now.ToString()));
            EmailHelper.SendNotificationEmailBookFound(config, settings.BookId, settings.CurrentEpisodeId);
            await bot.PostMessage(String.Format("Episode of book found! Found BookID {0} EpisodeID {1} found at {2}", settings.BookId, settings.CurrentEpisodeId, DateTime.Now.ToString()));
            await PostBookTitleToBot(previousBookData, false);

            finalImageUrl = StringHelper.GetFullVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString);

            HttpStatusCode fullVersionStatusCode = await WebHelper.GetUrlStatusCode(finalImageUrl);

            //Sometimes the full version is not put up at the exact same time as the preview version, but usually soon after.
            while (fullVersionStatusCode != HttpStatusCode.OK)
            {
                logger.Log(String.Format("Full version is not up yet. Retrying in 1 minute. The current time is: {0}", DateTime.Now.ToString()));
                await Wait(60000);
                fullVersionStatusCode = await WebHelper.GetUrlStatusCode(finalImageUrl);
            }

            //Will download either the maximum pages to download or when there's no pages left in the episode, whichever comes first.
            await WebHelper.DownloadBookEpisode(config, settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString, settings.MaximumPagesToDownload);
            return;
        }

        private async Task PostBookTitleToBot(BookData bookData, bool isNew = true)
        {
            if (bookData != null)
            {
                try
                {
                    //Getting the translated verions is just using pre-set word-replacement
                    string translatedTitle = StringHelper.GetTranslatedTitle(bookData.Title);
                    string translatedAuthor = StringHelper.GetTranslatedAuthor(bookData.Author);
                    string downloadMessage = "";
                    if (isNew)
                        downloadMessage = String.Format("Book Title: {0} ({1}) Book Author: {2} ({3}) found at {4}. Downloading.", bookData.Title, translatedTitle, bookData.Author, translatedAuthor, DateTime.Now.ToString());
                    else
                        downloadMessage = String.Format("Book Title (Previous Episode): {0} ({1}) Book Author: {2} ({3}) found at {4}. Downloading new episode.", bookData.Title, translatedTitle, bookData.Author, translatedAuthor, DateTime.Now.ToString());

                    logger.Log(downloadMessage);
                    await bot.PostMessage(downloadMessage);
                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Got episode of book but could not post to bot. Got error {0} at {1}", ex.ToString(), DateTime.Now.ToString()));
                }
            }
        }
    }
}
