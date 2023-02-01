using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using BookEpisodeScanner.Entities;
using System.Threading;
using Microsoft.Extensions.Configuration;
using BookEpisodeScanner.Loggers;

namespace BookEpisodeScanner.Utilities
{
    public static class WebHelper
    {

        public async static Task<BookData> GetBookData(IConfigurationRoot config, string bookId, string episodeId)
        {
            string WEBSERVICE_URL = String.Format("{0}?book_id={1}&episode_id={2}", config["webserviceURLRoot"], bookId, episodeId);

            try
            {
                var webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 20000;
                    webRequest.ContentType = "application/json";

                    //These values must be occasionally updated when the website updates
                    webRequest.Headers.Add(config["webRequestHeaderKey"], config["webRequestHeaderValue"]);

                    using (WebResponse response = await webRequest.GetResponseAsync())
                    {
                        using (Stream s = response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                var jsonResponse = sr.ReadToEnd();
                                dynamic responseObject = JsonConvert.DeserializeObject(jsonResponse);
                                BookData newBook = new BookData();
                                newBook.BookId = bookId;
                                newBook.EpisodeId = episodeId;
                                newBook.GuardianServer = responseObject.GUARDIAN_SERVER;
                                newBook.AdditionalQueryString = responseObject.ADDITIONAL_QUERY_STRING;
                                if (responseObject.book_data != null)
                                {
                                    newBook.S3Key = responseObject.book_data.s3_key;
                                    newBook.Title = responseObject.book_data.title;
                                    newBook.Author = responseObject.book_data.author;
                                    newBook.PageCount = responseObject.book_data.page_count;
                                }
                                return newBook;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EmailNotifier emailNotifier = new EmailNotifier(config);
                emailNotifier.SendNotificationEmailError("GetTokenQueryString", ex.ToString());
            }

            return null;
        }

        public static async Task<HttpStatusCode> GetUrlStatusCode(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                return response.StatusCode;
            }
        }

        public static async Task DownloadBookEpisode(IConfigurationRoot config, string bookId, string episodeId, string middleId, string tokenQueryString, int pagesToDownload)
        {
            string urlToDownload = "";
            string downloadFolder = @config["localDownloadFolder"]; //Make sure this folder exists or an error will be thrown.
            var logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            var emailNotifier = new EmailNotifier(config);
            int errorCount = 0;

            using (WebClient client = new WebClient())
            {
                try
                {
                    for (int i = 1; i <= pagesToDownload; i++)
                    {
                        if (errorCount > 5)
                        {
                            logger.Log(String.Format("Got more than 5 errors in a row, probably reached end of the book. Stopping download. Current time is {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            return;
                        }

                        try
                        {
                            urlToDownload = StringHelper.GetFullVersionImageUrl(config["imageBaseURL"], bookId, episodeId, middleId, tokenQueryString, i);
                            logger.Log(String.Format("Downloading file {0} of {1}. Current time is {2}", i, pagesToDownload, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            await client.DownloadFileTaskAsync(new Uri(urlToDownload), downloadFolder + i.ToString("D3") + ".jpg");
                            errorCount = 0;
                        }
                        catch (Exception ex)
                        {
                            logger.Log(String.Format("Error downloading page {0}. Current time is {1}. Error message: {1}", i, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture), ex.ToString()));
                            logger.Log(String.Format("Trying again in 10 seconds.", i, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                            emailNotifier.SendNotificationEmailError("DownloadPage", ex.ToString());
                            Thread.Sleep(10000);
                            i--;
                            errorCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Error in downloading book. Current time is {0}. Error: {1}", DateTime.Now.ToString(),ex.ToString()));
                    emailNotifier.SendNotificationEmailError("DownloadBook", ex.ToString());
                    errorCount++;
                }
            }
        }

        public static async Task DownloadURL(IConfigurationRoot config, string url)
        {
            string downloadFolder = @config["localDownloadFolder"]; //Make sure this folder exists or an error will be thrown.
            var logger = new Logger(config["localLogLocation"], Convert.ToBoolean(config["logToTextFile"]));
            var emailNotifier = new EmailNotifier(config);

            using (WebClient client = new WebClient())
            {
                try
                {
                    try
                    {
                        logger.Log(String.Format("Downloading specific URL {0}. Current time is {1}", url, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture)));
                        await client.DownloadFileTaskAsync(new Uri(url), downloadFolder + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture).ToString() + ".jpg");
                    }
                    catch (Exception ex)
                    {
                        logger.Log(String.Format("Error in downloading url. Current time is {0}. Error: {1}", DateTime.Now.ToString(), ex.ToString()));
                        emailNotifier.SendNotificationEmailError("DownloadURL", ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(String.Format("Error in downloading url. Current time is {0}. Error: {1}", DateTime.Now.ToString(), ex.ToString()));
                    emailNotifier.SendNotificationEmailError("DownloadURL", ex.ToString());
                }
            }
        }
    }
}
