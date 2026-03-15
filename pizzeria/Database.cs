using MySql.Data.MySqlClient;

namespace pizzeria
{
    public static class Database
    {
        private static string connectionString = "server=localhost;user=root;password=root;database=pizzeria;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
