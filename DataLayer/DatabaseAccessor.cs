using DataLayer.Entities;
using System;
using System.Data;
using System.Data.SQLite;

namespace DataLayer.Accessors
{
    public static class DatabaseAccessor
    {
        private const string _connectionString = "Data Source=BookEpisodeDB.db; Version = 3; Compress = true";

        public static List<Book> GetBooks()
        {
            List<Book> books = new List<Book>();
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
                        books.Add(new Book()
                        {
                            ID = Convert.ToInt32(resultRow["ID"]),
                            Name = Convert.ToString(resultRow["Name"]),
                            LookupValue = Convert.ToString(resultRow["LookupValue"]),
                            MiddleID = Convert.ToString(resultRow["MiddleID"]),
                        });
                    }
                }
            }
            return books;
        }

        public static List<Episode> GetEpisodesByBookName(string bookName)
        {
            List<Episode> episodes = new List<Episode>();
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
                        episodes.Add(new Episode()
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
            }
            return episodes;
        }

    }
}
