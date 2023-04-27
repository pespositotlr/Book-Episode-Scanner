using BookEpisodeScanner.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookEpisodeScanner.Utilities
{
    public static class StringHelper
    {
        public static string GetCurrentEpisodeIdFromPreviousEpisodeId(string previousEpisodeId)
        {
            //Repeated 2 numbers is the specific issue number
            string episodeIdPrefix = previousEpisodeId.Substring(0, 12);
            string episodeIdSuffix = previousEpisodeId.Substring(previousEpisodeId.Length - 2);
            string trimmedId = previousEpisodeId.Substring(episodeIdPrefix.Length);
            trimmedId = trimmedId.Substring(0, trimmedId.LastIndexOf(episodeIdSuffix));
            trimmedId = trimmedId.Substring(3);

            if (Int32.TryParse(trimmedId, out int j))
            {
                //Increment previous episode number (converted to an integer) by one
                string currentNumber = (j + 1).ToString();

                //The Episode Number is always 3 digits even if less than 100, so append 0s if needed
                if(currentNumber.Length < 3)
                {
                    for(int i = 0; i <= (3 - currentNumber.Length); i++)
                    {
                       currentNumber = "0" + currentNumber;
                    }
                }

                return episodeIdPrefix + currentNumber + currentNumber + episodeIdSuffix;
            }

            return "";
        }
        public static string GetTranslatedTitle(string sourceLanguageTitle)
        {
            var translatedTitle = sourceLanguageTitle;
                            
            return translatedTitle;
        }

        public static string GetTranslatedAuthor(string sourceLanguageAuthor)
        {
            return sourceLanguageAuthor;
        }

        public static string GetMiddleIDFromS3Key(BookServerData bookData)
        {
            int episodeIdIndex = bookData.S3Key.LastIndexOf(bookData.EpisodeId);

            string middleId = bookData.S3Key.Substring(episodeIdIndex + bookData.EpisodeId.Length, 6);
            return middleId;
        }

        public static string GetPreviewVersionImageUrl(string baseURL, string bookId, string episodeId, string middleId, string tokenQueryString, int pageNumber = 1)
        {
            return GetImageUrl(baseURL, bookId, episodeId, middleId, tokenQueryString, pageNumber, false);
        }
        public static string GetPreviewVersionImageUrl(BookServerData bookData, int pageNumber = 1)
        {
            return GetImageUrl(bookData, pageNumber, false);
        }

        public static string GetFullVersionImageUrl(string baseURL, string bookId, string episodeId, string middleId, string tokenQueryString, int pageNumber = 1)
        {
            return GetImageUrl(baseURL, bookId, episodeId, middleId, tokenQueryString, pageNumber, true);
        }

        public static string GetFullVersionImageUrl(BookServerData bookData, int pageNumber = 1)
        {
            return GetImageUrl(bookData, pageNumber, true);
        }

        private static string GetImageUrl(string baseURL, string bookId, string episodeId, string middleId, string tokenQueryString, int pageNumber = 1, bool isFullVersion = false)
        {
            //The full version of the book has _001 in the end of the url
            string version = "000";
            if (isFullVersion)
                version = "001";

            //BookId is which book it is
            //EpisodeId is the specific issue/number of the series it is
            //The middleId I think has to do with finding which Amazon S3 cloud server it's on, but I'm not sure

            //The bookID in the image URL is always 10 digits long
            string bookIdPaddedZeroes = GetPaddedZeroes(bookId);

            return String.Format("{0}{1}{2}//{3}{4}_{5}//{6}.jpg?{7}", 
                baseURL, bookIdPaddedZeroes, bookId, episodeId, middleId, version, pageNumber, tokenQueryString);
        }

        private static string GetPaddedZeroes(string bookId)
        {
            int numberOfZeroes = (10 - bookId.Length);

            StringBuilder zeroesBuilder = new StringBuilder();

            for (int i=0; i < numberOfZeroes; i++)
            {
                zeroesBuilder.Append('0');
            }

            return zeroesBuilder.ToString();
        }

        private static string GetImageUrl(BookServerData bookData, int pageNumber = 1, bool isFullVersion = false)
        {
            //Full version has _001 in the end of the url rather than _000 in the preview version
            if (isFullVersion)
                bookData.S3Key = bookData.S3Key.Replace("_000", "_001");

            //BookId is which book it is
            //EpisodeId is the specific issue/number of the series
            //The middleId I think has to do with finding which Amazon S3 cloud server it's on, but I'm not sure

            return String.Format("{0}{1}//{2}.jpg?{3}", bookData.GuardianServer, bookData.S3Key, pageNumber, bookData.AdditionalQueryString);
        }
        public static string GetFirstEpisodeId(string bookId)
        {
            //The prefix is BT with 0s padded to 10 digits followed by bookid
            //Then is number is the episode number twice (103)
            //Followed by 01

            string episodeIdPrefix = "BT" + GetPaddedZeroes(bookId) + bookId;
            string episodeIdSuffix = "00100101";

            return episodeIdPrefix + episodeIdSuffix;
        }
        public static int GetSequenceNumberFromEpisodeId(string episodeId)
        {
            string episodeIdPrefix = episodeId.Substring(0, 12);
            string episodeIdSuffix = episodeId.Substring(episodeId.Length - 2);
            string trimmedId = episodeId.Substring(episodeIdPrefix.Length);
            trimmedId = trimmedId.Substring(0, trimmedId.LastIndexOf(episodeIdSuffix));
            trimmedId = trimmedId.Substring(3);

            if (Int32.TryParse(trimmedId, out int j))
            {
                return j;
            }

            return 0;
        }

    }
}
