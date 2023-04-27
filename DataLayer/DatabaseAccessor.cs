using DataLayer.Entities;
using System;
using System.Data;
using System.Data.SQLite;

namespace DataLayer.Accessors
{
    public static class DatabaseAccessor
    {
        private const string _connectionString = "Data Source=..\\..\\..\\..\\DataLayer\\BookEpisodeDB.db; Version = 3; Compress = true";

        public static List<DBBook> GetBooks()
        {
            List<DBBook> books = new List<DBBook>();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT ID, Name, LookupValue, MiddleID FROM Book";
                    command.CommandType = CommandType.Text;
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                        books.Add(new DBBook()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            MiddleID = Convert.ToString(resultRow["MiddleID"]),
                        });
                    }
                }
                connection.Close();
            }
            return books;
        }
        public static DBBook GetBookByBookID(string lookupValue)
        {
            DBBook book = new DBBook();
            Console.WriteLine(Environment.CurrentDirectory);
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT ID, Name, LookupValue, MiddleID FROM Book WHERE LookupValue = @LookupValue";
                    command.CommandType = CommandType.Text;

                    command.Parameters.AddWithValue("@LookupValue", lookupValue);
                    command.Prepare();

                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                        book = new DBBook()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            MiddleID = Convert.ToString(resultRow["MiddleID"]),
                        };
                    }
                }
                connection.Close();
            }
            return book;
        }


        public static DBBook GetBookByName(string name)
        {
            DBBook book = new DBBook();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT ID, Name, LookupValue, MiddleID FROM Book WHERE Name = @Name";
                    command.CommandType = CommandType.Text;

                    command.Parameters.AddWithValue("@Name", name);
                    command.Prepare();

                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                        book = new DBBook()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            MiddleID = Convert.ToString(resultRow["MiddleID"]),
                        };
                    }
                }
                connection.Close();
            }
            return book;
        }

        public static DBEpisode GetEpisodeByEpisodeID(string lookupValue)
        {
            DBEpisode episode = new DBEpisode();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT ID, BookID, Name, LookupValue, IsDownloaded, DateCreated 
                                        FROM Episode e 
                                        WHERE LookupBalue = @LookupValue";
                        command.CommandType = CommandType.Text;

                        command.Parameters.AddWithValue("@LookupValue", lookupValue);
                        command.Prepare();

                        SQLiteDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                            episode = new DBEpisode()
                            {
                                ID = Convert.ToInt32(resultRow["ID"]),
                                BookID = Convert.ToInt32(resultRow["BookID"]),
                                Name = Convert.ToString(resultRow["Name"]),
                                LookupValue = Convert.ToString(resultRow["LookupValue"]),
                                IsDownloaded = Convert.ToBoolean(resultRow["IsDownloaded"]),
                                DateCreated = Convert.ToDateTime(resultRow["DateCreated"]),
                            };
                        }
                    }
                    connection.Close();
                }
                return episode;
            }
            catch
            {
                return episode;
            }
        }

        public static List<DBEpisode> GetEpisodesByBookName(string bookName)
        {
            List<DBEpisode> episodes = new List<DBEpisode>();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT e.ID, e.BookID, e.Name, e.LookupValue, e.IsDownloaded, e.DateCreated 
                                        FROM Episode e 
                                        INNER JOIN Book b ON e.BookID = b.ID
                                        WHERE b.Name = @bookName";
                    command.CommandType = CommandType.Text;

                    command.Parameters.AddWithValue("@bookName", bookName);
                    command.Prepare();

                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                        episodes.Add(new DBEpisode()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            BookID = Convert.ToInt32(resultRow["BookID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            IsDownloaded = Convert.ToBoolean(resultRow["IsDownloaded"]),
                            DateCreated = Convert.ToDateTime(resultRow["DateCreated"]),
                        });
                    }
                }
                connection.Close();
            }
            return episodes;
        }

        public static bool InsertBook(DBBook book)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Book (Name, LookupValue, MiddleID)   VALUES(@Name, @LookupValue, @MiddleID)";
                        command.CommandType = CommandType.Text;

                        command.Parameters.AddWithValue("@Name", book.Name);
                        command.Parameters.AddWithValue("@LookupValue", book.LookupValue);
                        command.Parameters.AddWithValue("@MiddleID", book.MiddleID);
                        command.Prepare();

                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool InsertEpisode(DBEpisode episode)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Episode (BookID, Name, LookupValue, IsDownloaded, SequenceNumber) VALUES(@BookID, @Name, @LookupValue, @IsDownloaded, @SequenceNumber)";
                        command.CommandType = CommandType.Text;

                        command.Parameters.AddWithValue("@BookID", episode.BookID);
                        command.Parameters.AddWithValue("@Name", episode.Name);
                        command.Parameters.AddWithValue("@LookupValue", episode.LookupValue);
                        command.Parameters.AddWithValue("@IsDownloaded", episode.IsDownloaded);
                        command.Parameters.AddWithValue("@SequenceNumber", episode.SequenceNumber);
                        command.Prepare();

                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static DBEpisode GetLatestEpisodeOfBookByBookID(string lookupValue)
        {
            DBEpisode episode = new DBEpisode();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT e.ID, e.BookID, e.Name, e.LookupValue, e.IsDownloaded, e.DateCreated 
                                        FROM Episode e 
                                        INNER JOIN Book b ON e.BookID = b.ID
                                        WHERE b.LookupValue = @lookupValue
                                        ORDER BY e.DateCreated DESC
                                        LIMIT 1";

                    command.CommandType = CommandType.Text;

                    command.Parameters.AddWithValue("@lookupValue", lookupValue);
                    command.Prepare();

                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var resultRow = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                        episode = new DBEpisode()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            BookID = Convert.ToInt32(resultRow["BookID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            IsDownloaded = Convert.ToBoolean(resultRow["IsDownloaded"]),
                            DateCreated = Convert.ToDateTime(resultRow["DateCreated"]),
                        };
                    }
                }
                connection.Close();
            }
            return episode;
        }
    }
}
