# Book-Episode-Scanner
Scans for existence of new episodes of books on a specific website and downloads them.

This is a genericized version of a private version of this solution I'm putting up on GitHub for posterity.
Specific information about the website is removed and dummy data is put in the config file.

This is a simple program for checking for new episodes of books from a specific external website. It gets a token needed for checking for the existence of episodes and then checks the URL for a new episode by incrementing the URL for the previous newest episode by one. It then re-tries until it finds it and fetches a new key if the old one expires. After finding it, it will download the episode locally. The main purpose of this is to give the user alerts when a new episode of a specific book is uploaded as they don't have a set schedule.

There's also features for logging that it found the episode to a discord bot and sending emails to note there was a problem or if the episode has been found. Thus allowing someone to know if it's found without having to watch the computer running it.

This is generally just used by me, so I didn't bother to add a lot of customizability or a database. So there's no UI outside of the cnosole and you need to manually add constants for a new book or episode you want to look up. If I were to expand this in the future I could do something like add a database rather than a big list of constants so the program could just get "the newest episode" of a book rather than needing to manually look up the last episode's ID from the website.
