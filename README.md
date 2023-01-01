# Book-Episode-Scanner
Scans for existence of new episodes of books on a specific website and downloads them built in .NET Core 3.1.

This is a genericized version of a private version of this solution I'm putting up on GitHub for posterity.
Specific information about the website is removed and dummy data is put in the config file.

This is a simple program for checking for new episodes of books from a specific external website. It gets a token needed for checking for the existence of episodes and then checks the URL for a new episode by incrementing the URL for the previous newest episode by one. It then re-tries until it finds it and fetches a new key if the old one expires. After finding it, it will download the episode locally. The main purpose of this is to give the user alerts when a new episode of a specific book is uploaded as they don't have a set schedule.

There's also features for logging that it found the episode to a discord bot and sending emails to note there was a problem or if the episode has been found, thus allowing someone to know if it's found without having to watch the computer running it.

To use, create a new "Scanner" object in Program.cs with the ids of whichever book and episode number you'd like to download (these Ids are stored in the constants files) and call the scanner's "Run()" function. The episode number should be the last episode already released and it will scan for the "next" episode. If not found, it will retry in 10 minutes and runs continuously until it finds it. I run this directly from Visual Studio.

This is generally just used by me, so I didn't bother to add a lot of customizability or a database. So there's no UI outside of the console and you need to manually add constants for a new book or episode you want to look up. If I were to expand this in the future I could do something like add a database rather than a big list of constants so the program could just get "the newest episode" of a book rather than needing to manually look up the last episode's ID from the website. I didn't go that far because I'm probably the only use for this. As of now if I want to test it without writing emails or posting to the discord bot, I just comment out the lines for posting them.
