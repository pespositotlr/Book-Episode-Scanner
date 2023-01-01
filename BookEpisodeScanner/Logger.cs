using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BookEpisodeScanner
{
    class Logger
    {
        public string localLogLocation { get; set; }

        public void Log(string logMessage)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(logMessage);

            string path = String.Format(@"{0}Scanner_Log_{1}_{2}_{3}.txt", localLogLocation, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Year);

            using (StreamWriter sw = new StreamWriter(path, append: true))
            {
                sw.WriteLine(logMessage);
            }

        }
    }
}
