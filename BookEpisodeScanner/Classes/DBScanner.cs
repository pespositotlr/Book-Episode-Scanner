using BookEpisodeScanner.Loggers;
using DataLayer.Accessors;
using DataLayer.Entities;
using BookEpisodeScanner.Entities;
using BookEpisodeScanner.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Threading.Tasks;

namespace BookEpisodeScanner.Classes
{
    /// <summary>
    /// Scans for the next episode (issue) of a book and downloads it when it is found
    /// Simplified user interface by using the database to get the latest episode rather than the user having to specifically search for it.
    /// </summary>
    class DBScanner
    {
        private ScannerSettings settings;
        DiscordBot bot;
        IConfigurationRoot config;
        Logger logger;
        EmailNotifier emailNotifier;
        BookServerData previousBookData;
        //BookData currentBookData;
        int attemptNumber;
        bool done;
        string previewImageUrl;
        string finalImageUrl;
        string tokenQueryString;
        bool logFailedAttempts;
        DBBook bookToSearch;
        DBEpisode latestEpisode;

        public DBScanner(string bookId = "", int maximumPagesToDownload = 1200, int maximumAttempts = 100000, int timeBetweenAttemptsMilliseconds = 600000)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            emailNotifier = new EmailNotifier(config);
            bot = new DiscordBot(config);

            settings.BookId = bookId;
            bookToSearch = DatabaseAccessor.GetBookByBookID(settings.BookId);
            latestEpisode = DatabaseAccessor.GetLatestEpisodeOfBookByBookID(settings.BookId);

            if (latestEpisode.LookupValue != null)
            {
                //Has an episode logged in the database.
                settings.PreviousEpisodeId = latestEpisode.LookupValue;
                settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.PreviousEpisodeId);
            } else
            {
                //Has no episodes logged in the database
                //Get episode 1 and move forward.
                settings.PreviousEpisodeId = StringHelper.GetFirstEpisodeId(settings.BookId);
                settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.PreviousEpisodeId);

            }

            settings.MiddleId = bookToSearch.MiddleID; 
            settings.MaximumPagesToDownload = maximumPagesToDownload;
            settings.MaximumAttempts = maximumAttempts;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
            logFailedAttempts = true; //Don't log failed attempts to save time
            done = false;
        }

        public async Task GetNewestEpisode()
        {
            //First find the newest release episode and then run the scan for the next one
            await bot.LogToDiscord();

            var newestReleasedEpisode = await GetNewestReleasedEpisodeBookData();

            if (newestReleasedEpisode == null)
            {
                logger.Log(String.Format("Could not find newest episode."));
                return;
            }

            previousBookData = newestReleasedEpisode;
            settings.PreviousEpisodeId = previousBookData.EpisodeId;
            settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(previousBookData.EpisodeId);
            tokenQueryString = previousBookData.AdditionalQueryString;
            
            logger.Log(String.Format("Attempting to get latest episode for BookID {0}, Book Name: {1}. Maximum number of attempts is {2}. The current time is {3}", settings.BookId, bookToSearch.Name, settings.MaximumAttempts, DateTime.Now.ToString()));
            logger.Log(String.Format("Last logged episode is EpisodeID {0}, Episode Name: {1}. Checking for newer episodes.", previousBookData.EpisodeId, previousBookData.Title));

            //Searching for the episode AFTER the last released episode
            logger.Log(String.Format("Attempting to get episode following: EpisodeID: {0}", settings.CurrentEpisodeId));
            
            await ScanForEpisode();

            return;
        }

        private async Task ScanForEpisode()
        {
            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();

            while (!done)
            {
                logger.Log(String.Format("Attempt number: {0}. Current time: {1}", attemptNumber, DateTime.Now.ToString()));

                try
                {
                    previewImageUrl = StringHelper.GetPreviewVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, bookToSearch.MiddleID, tokenQueryString);

                    logger.Log(previewImageUrl.Replace("//", "/"));

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
                            BookServerData bookData = await WebHelper.GetBookData(config, settings.BookId, settings.PreviousEpisodeId);
                            tokenQueryString = bookData.AdditionalQueryString;
                            previewImageUrl = StringHelper.GetPreviewVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString);
                            break;
                        default:
                            //Email that we somehow got some other error
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
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private async Task RunFoundEpisodeProcess()
        {
            logger.Log(String.Format("Episode of book found! Found BookID {0} EpisodeID {1} found at {2}", settings.BookId, settings.CurrentEpisodeId, DateTime.Now.ToString()));
            emailNotifier.SendNotificationEmailBookFound(settings.BookId, settings.CurrentEpisodeId);
            await bot.PostMessage(String.Format("Episode of book found! Found BookID {0} EpisodeID {1} found at {2}", settings.BookId, settings.CurrentEpisodeId, DateTime.Now.ToString()));
            await PostBookTitleToBot(previousBookData, false);

            finalImageUrl = StringHelper.GetPreviewVersionImageUrl(config["imageBaseURL"], settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString);

            HttpStatusCode fullVersionStatusCode = await WebHelper.GetUrlStatusCode(finalImageUrl);

            //Sometimes the full version is not put up at the exact same time as the preview version, but usually soon after.
            while (fullVersionStatusCode != HttpStatusCode.OK)
            {
                logger.Log(String.Format("Full version is not up yet. Retrying in 1 minute. The current time is: {0}", DateTime.Now.ToString()));
                await Wait(60000);
                fullVersionStatusCode = await WebHelper.GetUrlStatusCode(finalImageUrl);
            }

            var databaseBook = DatabaseAccessor.GetBookByBookID(settings.BookId);
            if (databaseBook.LookupValue == null)
            {
                logger.Log(String.Format("Requested book {0} not found.", settings.BookId));
                throw new Exception("Book not found.");
            }

            //Use "The episode following" because the currentbookdata isn't up yet
            var episodeToInsert = new DataLayer.Entities.DBEpisode()
            {
                BookID = databaseBook.ID,
                Name = "The episode following: " + previousBookData.Title,
                LookupValue = settings.CurrentEpisodeId,
                IsDownloaded = true,
                SequenceNumber = StringHelper.GetSequenceNumberFromEpisodeId(settings.CurrentEpisodeId)
            };

            DatabaseAccessor.InsertEpisode(episodeToInsert);

            //Will download either the maximum pages to download or when there's no pages left in the episode, whichever comes first.
            await WebHelper.DownloadBookEpisode(config, settings.BookId, settings.CurrentEpisodeId, settings.MiddleId, tokenQueryString, settings.MaximumPagesToDownload);
            return;
        }

        private async Task PostBookTitleToBot(BookServerData bookData, bool isNew = true)
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

        private async Task<BookServerData> GetNewestReleasedEpisodeBookData()
        {
            //Get latest episode in the database
            var currentLatest = DatabaseAccessor.GetLatestEpisodeOfBookByBookID(settings.BookId);
            var foundAnEpisode = false;
            var doneFindingNewestEpisode = false;
            var getNewestEpisodeAttempts = 1;
            var timeToWaitBetweenReleasedAttempts = 500;

            if (currentLatest.LookupValue != null)
            {
                settings.CurrentEpisodeId = currentLatest.LookupValue;
            }
            else
            {
                settings.CurrentEpisodeId = StringHelper.GetFirstEpisodeId(settings.BookId);
            }

            //Check data for this episode
            BookServerData currentBookData = null;

            while (!doneFindingNewestEpisode)
            {
                currentBookData = await WebHelper.GetBookData(config, settings.BookId, settings.CurrentEpisodeId);

                if (currentBookData.S3Key == null && foundAnEpisode)
                {
                    //Found previous episode but did not find the current episode. That means the previous episode is the newest episode.
                    doneFindingNewestEpisode = true;
                }
                else
                {
                    //Check for newer episodes
                    if (currentBookData.S3Key == null)
                    {
                        if (logFailedAttempts)
                            logger.Log(String.Format("Did not find Book ID {0} Episode ID {1} on attempt {2} at {3}. Checking next id.", settings.BookId, settings.CurrentEpisodeId, getNewestEpisodeAttempts, DateTime.Now.ToString()));
                    }
                    else
                    {
                        foundAnEpisode = true;
                        logger.Log(String.Format("FOUND Book ID {0} Episode ID {1} on attempt {2} at {3}. Inserting this episode into the databse.", settings.BookId, settings.CurrentEpisodeId, getNewestEpisodeAttempts, DateTime.Now.ToString()));

                        var databaseBook = DatabaseAccessor.GetBookByBookID(settings.BookId);
                        if (databaseBook.LookupValue == null)
                        {
                            logger.Log(String.Format("Requested book {0} not found.", settings.BookId));
                            throw new Exception("Book not found.");
                        }

                        //Insert this found episode into the database
                        var episodeToInsert = new DataLayer.Entities.DBEpisode()
                        {
                            BookID = databaseBook.ID,
                            Name = currentBookData.Title,
                            LookupValue = currentBookData.EpisodeId,
                            IsDownloaded = false,
                            SequenceNumber = StringHelper.GetSequenceNumberFromEpisodeId(currentBookData.EpisodeId)
                        };

                        DatabaseAccessor.InsertEpisode(episodeToInsert);

                    }

                    //Try next episode ID
                    settings.PreviousEpisodeId = settings.CurrentEpisodeId;
                    previousBookData = currentBookData;
                    settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.CurrentEpisodeId);

                    await Wait(timeToWaitBetweenReleasedAttempts);

                    if (getNewestEpisodeAttempts == settings.MaximumAttempts)
                    {
                        doneFindingNewestEpisode = true;
                        logger.Log(String.Format("Did not find the newest episode ID for Book Id {0} in {1} attempts. The maximum attempts value may be too small, this book may have no episodes, or you started from an Episode ID that is past the newest episode.", settings.BookId, attemptNumber));
                    }
                    else
                        getNewestEpisodeAttempts++;

                }

            }

            return previousBookData;

        }
    }
}
