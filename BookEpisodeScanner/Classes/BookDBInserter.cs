using DataLayer.Accessors;
using DataLayer.Entities;
using BookEpisodeScanner.Entities;
using BookEpisodeScanner.Loggers;
using BookEpisodeScanner.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookEpisodeScanner.Classes
{
    /// <summary>
    /// Finds the newest episode of a given book rather than get it directly from the FOD site.
    /// </summary>
    class BookDBInserter
    {
        IConfigurationRoot config;
        Logger logger;
        private ScannerSettings settings;
        private BookData currentBookData;
        int attemptNumber = 0;
        string bookTitle;
        bool done;

        public BookDBInserter(string bookId = "", string title = "", string latestEpisodeId = "",
            int maximumAttempts = 999, int timeBetweenAttemptsMilliseconds = 10)
        {
            settings = new ScannerSettings();
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables();
            config = builder.Build();
            logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));

            settings.BookId = bookId;
            bookTitle = title;
            if (!String.IsNullOrEmpty(latestEpisodeId))
                settings.CurrentEpisodeId = latestEpisodeId;
            settings.TimeBetweenAttemptsMilliseconds = timeBetweenAttemptsMilliseconds;
            done = false;
        }

        public async Task Run()
        {
            DateTime.Now.ToString();
            //First check if book already exists by bookID
            Book existingBook = DatabaseAccessor.GetBookByBookID(settings.BookId);
            if (!String.IsNullOrEmpty(existingBook.LookupValue))
            {
                logger.Log(String.Format("Requested book {0} is already in database.", settings.BookId));
                return;
            }

            existingBook = DatabaseAccessor.GetBookByName(bookTitle);
            if (!String.IsNullOrEmpty(existingBook.LookupValue))
            {
                logger.Log(String.Format("Requested book {0} is already in database.", settings.BookId));
                return;
            }

            //Book not in DB so looking it up to get its title

            attemptNumber = 1;
            TimeSpan t = TimeSpan.FromMilliseconds(settings.TimeBetweenAttemptsMilliseconds);
            string timeBetweenAttemptsString = t.Minutes.ToString();

            if(String.IsNullOrEmpty(settings.CurrentEpisodeId))
                settings.CurrentEpisodeId = StringHelper.GetFirstEpisodeId(settings.BookId);

            logger.Log(String.Format("Starting search for Book Id {0} - {1}. Time is {2}.", settings.BookId, bookTitle, DateTime.Now.ToString()));

            while (!done)
            {
                currentBookData = await WebHelper.GetBookData(config, settings.BookId, settings.CurrentEpisodeId);

                if (currentBookData.S3Key == null)
                {
                    //Did not find this episode, trying the next one.
                    logger.Log(String.Format("Did not find Book Id {0} Episode Id {1}. Trying next Episode Id.", settings.BookId, settings.CurrentEpisodeId));
                    settings.PreviousEpisodeId = settings.CurrentEpisodeId;
                    settings.CurrentEpisodeId = StringHelper.GetCurrentEpisodeIdFromPreviousEpisodeId(settings.CurrentEpisodeId);
                }
                else
                {
                    logger.Log(String.Format("Found an episode for Book Id {0} - {1}. Inserting book.", settings.BookId, bookTitle));

                    //Found the episode, insert it into the database.
                    string title = currentBookData.Title;

                    //Crop out episode name
                    int index = currentBookData.Title.IndexOf(' ');
                    if(index > 0)
                        title = currentBookData.Title.Substring(0, index);

                    var bookToInsert = new Book()
                    {
                        Name = title,
                        LookupValue = currentBookData.BookId,
                        MiddleID = StringHelper.GetMiddleIDFromS3Key(currentBookData),
                    };

                    DatabaseAccessor.InsertBook(bookToInsert);

                    logger.Log(String.Format("Inserting done. Time is {0}.", DateTime.Now.ToString()));
                    done = true;
                }

                await Wait(settings.TimeBetweenAttemptsMilliseconds);

                if (attemptNumber == settings.MaximumAttempts)
                {
                    done = true;
                    logger.Log(String.Format("Did not find any episodes for Book Id {0} in {1} attempts. The maximum attempts value may be too small, this book may have no episodes, or you started from an Episode ID that is past the newest episode.", settings.BookId, attemptNumber));
                }
                else
                    attemptNumber++;
                                               
            }

            logger.Log("All done!");

            return;
        }

        private async Task Wait(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }


    }
}
