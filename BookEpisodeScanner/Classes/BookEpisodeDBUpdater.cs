using DataLayer.Accessors;
using DataLayer.Entities;
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

namespace BookEpisodeScanner.Classes
{
    /// <summary>
    /// Finds the newest episode of a given book rather than get it directly from the FOD site.
    /// </summary>
    class BookEpisodeDBUpdater
    {
        private ScannerSettings settings;
        IConfigurationRoot config;
        Logger logger;
        BookData previousBookData;
        BookData currentBookData;
        Book bookToSearchFor;
        int attemptNumber;
        bool logFailedAttempts;
        bool foundAnEpisode;
        bool done;

        public BookEpisodeDBUpdater(string bookId = "",
            int maximumAttempts = 999, int timeBetweenAttemptsMilliseconds = 10)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));

            settings.BookId = bookId;
            settings.MaximumAttempts = maximumAttempts;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
            logFailedAttempts = true; //Not logging failed attempts saves time
            done = false;

            bookToSearchFor = DatabaseAccessor.GetBookByBookID(settings.BookId);
            if (bookToSearchFor.LookupValue == null)
            {
                logger.Log(String.Format("Requested book {0} not found.", settings.BookId));
                throw new Exception("Book not found.");
            }
        }

        public async Task Run()
        {
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();

            var currentLatest = DatabaseAccessor.GetLatestEpisodeOfBook(settings.BookId);

            if (currentLatest.LookupValue != null)
            {
                settings.CurrentEpisodeId = currentLatest.LookupValue;
            }
            else
            {
                settings.CurrentEpisodeId = StringHelper.GetFirstEpisodeId(settings.BookId);
            }

            logger.Log(String.Format("Updating Book data in Database for BookID {0} starting with Episode ID {1}", settings.BookId, settings.CurrentEpisodeId));
            logger.Log(String.Format("Maximum number of attempts is {0}. The current time is {1}", settings.MaximumAttempts, DateTime.Now.ToString()));

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
                    if (currentBookData.S3Key == null && logFailedAttempts)
                    {
                        logger.Log(String.Format("Did not find Book ID {0} Episode ID {1} on attempt {2} at {3}. Checking next id.", settings.BookId, settings.CurrentEpisodeId, attemptNumber, DateTime.Now.ToString()));
                    }
                    else
                    {
                        foundAnEpisode = true;
                        RunInsertEpisodeProcess(currentBookData, attemptNumber);
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

        private void RunInsertEpisodeProcess(BookData bookData, int attemptNumber)
        {
            logger.Log(String.Format("Found Book ID {0} Episode ID {1} on attempt {2} at {3}.", bookData.BookId, bookData.EpisodeId, attemptNumber, DateTime.Now.ToString()));
            //Check if episode is already in DB
            var existingEpisode = DatabaseAccessor.GetEpisodeByEpisodeID(bookData.BookId);
            if (existingEpisode.LookupValue != null)
            {
                logger.Log(String.Format("This episode was already in the databse, so now checking next episode."));
                return;
            }

            //If not, insert into DB
            var episodeToInsert = new Episode()
            {
                BookID = bookToSearchFor.ID,
                Name = bookData.Title,
                LookupValue = bookData.EpisodeId,
                IsDownloaded = false,
                SequenceNumber = StringHelper.GetSequenceNumberFromEpisodeId(bookData.EpisodeId)
            };

            DatabaseAccessor.InsertEpisode(episodeToInsert);

            //Log that moving on to next episode.
            logger.Log(String.Format("Insert successful. Checking next episode."));

            return;
        }

    }
}
