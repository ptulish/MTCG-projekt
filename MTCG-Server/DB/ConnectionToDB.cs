using Npgsql;

namespace server1.DB;

public class ConnectionToDB
{
    private string connectionString;
    
    private NpgsqlConnection connection;

    public NpgsqlConnection GetConnection()
    {
        return connection;
    }

    public ConnectionToDB()
    {
        this.connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=mydb";
        this.connection = new NpgsqlConnection(connectionString);
    }

    public void OpenConnection()
    {
        try
        {
            connection.Open();
            Console.WriteLine("Connected to Database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error at connecting to the Database: {ex.Message}");
        }
    }

    public bool initializeTables()
    {
        string createTokensTableQuery = "CREATE TABLE IF NOT EXISTS tokens (" +
                                        "username VARCHAR(255)," +
                                        "token VARCHAR(255)," +
                                        "login TIMESTAMP," +
                                        "logout TIMESTAMP)";
        using (NpgsqlCommand command = new NpgsqlCommand(createTokensTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }

        string createUsersTableQuery = "CREATE TABLE IF NOT EXISTS users (" +
                                       "user_id SERIAL PRIMARY KEY," +
                                       "username VARCHAR(255) UNIQUE," +
                                       "password VARCHAR(255)," +
                                       "coins INTEGER," +
                                       "admin BOOLEAN)";
    using (NpgsqlCommand command = new NpgsqlCommand(createUsersTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }

        // Создание таблицы cards (предположим, что у вас есть таблица cards)
        string createCardsTableQuery = "CREATE TABLE IF NOT EXISTS cards (" +
                                       "card_id SERIAL PRIMARY KEY," +
                                       "card_name VARCHAR(255)," +
                                       "card_category VARCHAR(255)," +
                                       "card_Type VARCHAR(255)," +
                                       "card_damage VARCHAR(255))";
        using (NpgsqlCommand command = new NpgsqlCommand(createCardsTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }
        
        string createPackagesTableQuery = "CREATE TABLE IF NOT EXISTS packages (" +
                                       "package_id SERIAL PRIMARY KEY," +
                                       "card1_id INTEGER," +
                                       "card2_id INTEGER," +
                                       "card3_id INTEGER," +
                                       "card4_id INTEGER," +
                                       "card5_id INTEGER," +
                                       "inStore BOOLEAN)";
        using (NpgsqlCommand command = new NpgsqlCommand(createPackagesTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }
        
        // Создание таблицы decks
        string createDecksTableQuery = "CREATE TABLE IF NOT EXISTS decks (" +
                                       "deck_id SERIAL PRIMARY KEY," +
                                       "user_id INTEGER REFERENCES users(user_id)," +
                                       "card_id INTEGER REFERENCES cards(card_id))";
        using (NpgsqlCommand command = new NpgsqlCommand(createDecksTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }

        // Создание таблицы stacks
        string createStacksTableQuery = "CREATE TABLE IF NOT EXISTS stacks (" +
                                        "stack_id SERIAL PRIMARY KEY," +
                                        "user_id INTEGER REFERENCES users(user_id)," +
                                        "card_id INTEGER UNIQUE REFERENCES cards(card_id)); ";
        using (NpgsqlCommand command = new NpgsqlCommand(createStacksTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }
        string createTableQuery = @"
                                CREATE TABLE IF NOT EXISTS tradings (
                                    trade_id SERIAL PRIMARY KEY,
                                    user_id INTEGER,
                                    card_id INTEGER,
                                    req_category VARCHAR(255),
                                    req_type VARCHAR(255) NULL,
                                    req_damage INTEGER NULL,
                                    inStore BOOLEAN DEFAULT true
                                );
        ";

        using (NpgsqlCommand command = new NpgsqlCommand(createTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }
        
        string createStatsTableQuery = @"
            CREATE TABLE IF NOT EXISTS stats (
                user_id INT PRIMARY KEY,
                username TEXT,
                games INT DEFAULT 0,
                wins INT DEFAULT 0,
                loses INT DEFAULT 0,
                draws INT DEFAULT 0,
                elo INT DEFAULT 100
            );";

        using (NpgsqlCommand createTableCommand = new NpgsqlCommand(createStatsTableQuery, connection))
        {
            createTableCommand.ExecuteNonQuery();
        }
        
            
        Console.WriteLine("Tables checked");
        return true;
    }
}