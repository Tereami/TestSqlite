using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace TestSqlite
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string version = GetVersion();
            Console.WriteLine($"Installed SQLite version: {version}");

            string dbpath = GetDbFilePath();
            string cs = "URI=file:" + dbpath;
            string tableName = "cars";
            using (SQLiteConnection conn = new SQLiteConnection(cs))
            {
                conn.Open();

                while(true)
                {
                    Console.WriteLine("Select: reset, show, add, delete, clear, quit");
                    string userText = Console.ReadLine();
                    if (userText == "quit") return;
                    switch (userText)
                    {
                        case "reset":
                            WriteTestData(conn, tableName);
                            break;
                        case "show":
                            ShowAllTable(conn, tableName);
                            break;
                        case "add":
                            AddRow(conn, tableName);
                            break;
                        case "delete":
                            DeleteRow(conn, tableName);
                            break;
                        case "clear":
                            ClearTable(conn, tableName);
                            break;
                        default:
                            Console.WriteLine("Unknown command!");
                            break;
                    }
                }

                conn.Close();
            }
            Console.ReadKey();
        }

        private static string GetVersion()
        {
            string cs = "Data Source=:memory:"; //db is created in a memory
            string version = "INVALID";
            using (SQLiteConnection con = new SQLiteConnection(cs))
            {
                con.Open();

                SQLiteCommand cmd = new SQLiteCommand(con);
                string stm = "SELECT SQLITE_VERSION()"; //builtin fuction
                cmd.CommandText = stm;

                version = cmd.ExecuteScalar().ToString();
                con.Close();
            }
            return version;
        }

        private static string GetDbFilePath()
        {
            string curAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string folder = System.IO.Path.GetDirectoryName(curAssembly);
            string dbpath = System.IO.Path.Combine(folder, "rbs.db");
            return dbpath;
        }

        private static void ShowAllTable(SQLiteConnection conn, string tableName)
        {
            SQLiteCommand cmd = new SQLiteCommand(conn);
            cmd.CommandText = $"SELECT * FROM {tableName}";

            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                string headers = $"{reader.GetName(0)}\t{reader.GetName(1)}\t{reader.GetName(2)}";
                Console.WriteLine(headers);
                while(reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int price = reader.GetInt32(2);
                    Console.WriteLine($"{id}\t{name}\t{price}");
                }
            }
        }

        private static void AddRow(SQLiteConnection conn, string tableName)
        {
            Console.WriteLine("Car name:");
            string name = Console.ReadLine();
            Console.WriteLine("Car price:");
            int price = ReadInt();

            SQLiteCommand cmd = new SQLiteCommand(conn);
            cmd.CommandText = $"INSERT INTO {tableName}(name,price) VALUES(@name,@price)";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Prepare();
            cmd.ExecuteNonQuery();
            Console.WriteLine("Add 1 item");
        }

        private static void DeleteRow(SQLiteConnection conn, string tableName)
        {
            Console.WriteLine("Row number to delete:");
            int id = ReadInt();

            SQLiteCommand cmd = new SQLiteCommand(conn);
            cmd.CommandText = $"DELETE FROM {tableName} WHERE id={id}";
            cmd.ExecuteNonQuery();
            Console.WriteLine("Deleted 1 row");
        }

        private static void ClearTable(SQLiteConnection conn, string tableName)
        {
            SQLiteCommand cmd = new SQLiteCommand(conn);
            cmd.CommandText = $"DELETE FROM {tableName}";
            cmd.ExecuteNonQuery();
            Console.WriteLine("Table is cleared");
        }

        private static void CreateTable(string name, SQLiteConnection conn)
        {
            SQLiteCommand cmd = new SQLiteCommand(conn);
            cmd.CommandText = $"DROP TABLE IF EXISTS {name}";
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"CREATE TABLE {name}(id INTEGER PRIMARY KEY, name TEXT, price INT)";
            cmd.ExecuteNonQuery();

            Console.WriteLine($"Table {name} is created");
        }

        private static void WriteTestData(SQLiteConnection conn, string tableName)
        {
            CreateTable(tableName, conn);

            Dictionary<string, int> testData = new Dictionary<string, int>();
            testData.Add("Audi", 30000);
            testData.Add("Toyota", 25000);
            testData.Add("Mercedes", 80000);

            //FillTableV1(conn, tableName, testData);
            FillTableWithPreparedStatements(conn, tableName, testData);

            Console.WriteLine($"Table {tableName} is filled with {testData.Count} values");
        }

       
        /*private static void FillTableV1(
            SQLiteConnection conn, string tablename, Dictionary<string,int> data)
        {
            SQLiteCommand cmd = new SQLiteCommand(conn);

            foreach(var kvp in data)
            {
                string name = kvp.Key;
                int price = kvp.Value;
                cmd.CommandText = $"INSERT INTO {tablename}(name, price) VALUES('{name}',{price})";
                cmd.ExecuteNonQuery();
            }
        }*/

        //Writing variables directly to sql command is not safe because of sql injection possibility
        //you should use prepared statement instead
        private static void FillTableWithPreparedStatements(
            SQLiteConnection conn, string tableName, Dictionary<string, int> data)
        {
            SQLiteCommand cmd = new SQLiteCommand(conn);

            cmd.CommandText = $"INSERT INTO {tableName}(name, price) VALUES(@name, @price)";

            foreach(var kvp in data)
            {
                string name = kvp.Key;
                int price = kvp.Value;
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
        }

        private static int ReadInt()
        {
            int num = -999;

            while (true)
            {
                string numString = Console.ReadLine();
                if (int.TryParse(numString, out num))
                    return num;
                else
                    Console.WriteLine("Invalid number, retry:");
            }
        }
    }
}
