using BookEpisodeScanner.Loggers;
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
    /// <summary>
    /// Finds the newest episode of a given book.
    /// </summary>
    class NewestEpisodeFinder
    {
        private ScannerSettings settings;
        IConfigurationRoot config;
        Logger logger;
        BookData previousBookData;
        BookData currentBookData;
        int attemptNumber;
        bool logFailedAttempts;
        bool foundAnEpisode;
        bool done;

        public NewestEpisodeFinder(string bookId = "", string previousEpisodeId = "",
            int maximumAttempts = 999, int timeBetweenAttemptsMilliseconds = 10)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));

            settings.BookId = bookId;
            settings.PreviousEpisodeId = previousEpisodeId; //Where we start counting from to save time. If this is not provided, we start at 1.
            settings.CurrentEpisodeId = previousEpisodeId;
            settings.MaximumAttempts = maximumAttempts;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
            logFailedAttempts = true; //Not logging failed attempts saves time
            done = false;
        }

        public async Task Run()
        {
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();

            //If not provided with an episode ID, start with 1.
            if (String.IsNullOrEmpty(settings.PreviousEpisodeId))
            {
                settings.PreviousEpisodeId = StringHelper.GetFirstEpisodeId(settings.BookId);
                settings.CurrentEpisodeId = settings.PreviousEpisodeId;
            }

            logger.Log(String.Format("Beginning scan for newest episode of BookID {0}. Starting at EpisodeID {1}. Maximum number of attempts is {2}. The current time is {3}", settings.BookId, settings.CurrentEpisodeId, settings.MaximumAttempts, DateTime.Now.ToString()));

            while (!done)
            {
                currentBookData = await WebHelper.GetBookData(config, settings.BookId, settings.CurrentEpisodeId);

                if (currentBookData.S3Key == null && foundAnEpisode)
                {
                    //Found previous episode but did not find the current episode. That means the previous episode is the newest episode.
                    RunFoundNewestEpisodeProcess();
                    done = true;
                }
                else
                {
                    if (logFailedAttempts)
                    {
                        if (currentBookData.S3Key == null)
                            logger.Log(String.Format("Did not find Book ID {0} Episode ID {1} on attempt {2} at {3}. Checking next id.", settings.BookId, settings.CurrentEpisodeId, attemptNumber, DateTime.Now.ToString()));
                        else
                        {
                            foundAnEpisode = true;
                            logger.Log(String.Format("Found Book ID {0} Episode ID {1} on attempt {2} at {3}. Checking next id.", settings.BookId, settings.CurrentEpisodeId, attemptNumber, DateTime.Now.ToString()));
                        }
                    }

                    //Try next episode ID
                    settings.PreviousEpisodeId = settings.CurrentEpisodeId;
                    previousBookData = currentBookData;
                    settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.CurrentEpisodeId);

                    await Wait(settings.TimeBetweenAttemptsMilliseconds);

                    if (attemptNumber == settings.MaximumAttempts)
                    {
                        done = true;
                        logger.Log(String.Format("Did not find the newest episode ID for Book Id {0} in {1} attempts. The maximum attempts value may be too small, this book may have no episodes, or you started from an Episode ID that is past the newest episode.", settings.BookId, attemptNumber));
                    }
                    else
                        attemptNumber++;

                }
            }

            logger.Log("All done!");

            return;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private void RunFoundNewestEpisodeProcess()
        {
            logger.Log(String.Format("Newest episode of Book ID: {0}, Title: {1} found at {2}", previousBookData.BookId, previousBookData.Title, DateTime.Now.ToString()));
            logger.Log(String.Format("The current newest Episode ID is \n{0}", settings.PreviousEpisodeId));
            logger.Log(String.Format("The expected next upcoming Episode ID is \n{0}", settings.CurrentEpisodeId));
            return;
        }

    }
}
