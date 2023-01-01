using System;
using System.Collections.Generic;
using System.Text;

namespace BookEpisodeScanner.Entities
{
    public class BookData
    {
        public string BookId { get; set; }
        public string EpisodeId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int PageCount { get; set; }
        public string GuardianServer { get; set; }
        public string AdditionalQueryString { get; set; }
        public string S3Key { get; set; }
    }
}
