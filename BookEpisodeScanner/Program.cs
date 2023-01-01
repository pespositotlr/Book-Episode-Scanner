using System;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using BookEpisodeScanner.Classes;
using BookEpisodeScanner.Constants;

namespace BookEpisodeScanner
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Scanner Scanner = new Scanner(Book.DummyBook, Episode.DummyEpisode, MiddleId.DummyMiddleID);

            await Scanner.Run();
        }

    }

}
