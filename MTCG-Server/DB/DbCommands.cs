using MyServer.Classes;
using Npgsql;

namespace server1.DB;

public class DbCommands
{
    private string _connectionString;
    private NpgsqlConnection _connection;

    public DbCommands(ConnectionToDB connectionToDb)
    {
        this._connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=mydb";
        this._connection = connectionToDb.GetConnection();
    }
    public User? GetUser(string username)
    {
        // Параметризованный SQL-запрос для поиска пользователя по логину
        string selectUserQuery = "SELECT * FROM users WHERE username = @username";
        User? newUser = null;
        using NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserQuery, connection: _connection);
        // Добавление параметра для поиска
        command.Parameters.AddWithValue(parameterName: "@username", value: username);

        using NpgsqlDataReader reader = command.ExecuteReader();
        // Если пользователь найден
        if (reader.Read())
        {
            newUser = new User(User_id: (int)reader[name: "user_id"],
                username: reader[name: "username"].ToString(),
                password: reader[name: "password"].ToString(), coins: (int)reader[name: "coins"]);
        }

        return newUser;

    }
    public List<Card>? GetCardsFromUser(string username)
    {

        User? user = GetUser(username);
        if (user == null)
        {
            return null;
        }
        List<Card>? cards = new List<Card>();
        string selectUserCardsQuery =
            "SELECT cards.* FROM stacks JOIN cards ON stacks.card_id = cards.card_id WHERE stacks.user_id = @user_id;";
        using (NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserCardsQuery, connection: _connection))
        {
            command.Parameters.AddWithValue(parameterName: "@user_id", value: user.user_id);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                List<Card> cardsIds = new List<Card>();


                // Если пользователь найден
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        // Прочитываем значения из результата запроса
                        int card_id = reader.GetInt32(reader.GetOrdinal("card_id"));
                        string Name = reader.GetString(reader.GetOrdinal("card_name"));
                        string Category = reader.GetString(reader.GetOrdinal("card_category"));
                        int Damage = Convert.ToInt32(reader.GetString(reader.GetOrdinal("card_damage")));
                        bool Spell;
                        if (!string.IsNullOrEmpty(Category = "Spell"))
                        {
                            Spell = true;
                        }
                        else
                        {
                            Spell = false;
                        }

                        string Type = reader.GetString(reader.GetOrdinal("card_type"));

                        // Создаем объект Card и добавляем его в список
                        Card card = new Card(card_id, Name, Category, Damage, Spell, Type);
                        cards.Add(card);
                    }
                }
            }
        }

        return cards;
    }
    public int registerNewUSer(string username, string password)
    {
        int returnValue = 0;

        try
        {
            bool isAdmin = username == "admin";
            User returnUser = new User(username, password);
            // Параметризованный SQL-запрос для вставки данных в таблицу decks
            string insertQuery =
                "INSERT INTO users (username, password, coins, admin) VALUES (@username, @password, @coins, @admin) RETURNING user_id";

            using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, _connection))
            {
                // Добавление параметров
                command.Parameters.AddWithValue("@username", returnUser.Username);
                command.Parameters.AddWithValue("@password", returnUser.Password);
                command.Parameters.AddWithValue("@coins", returnUser.Coins);
                command.Parameters.AddWithValue("admin", isAdmin);

                returnValue += (int)command.ExecuteScalar();
            }
            string insertStatsQuery = "INSERT INTO stats (user_id, username, games, wins, loses, draws, elo) VALUES (@user_id, @username, 0, 0, 0, 0, 100)";

            using (NpgsqlCommand insertStatsCommand = new NpgsqlCommand(insertStatsQuery, _connection))
            {
                insertStatsCommand.Parameters.AddWithValue("@user_id", returnValue);
                insertStatsCommand.Parameters.AddWithValue("@username", username);

                insertStatsCommand.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e);

            if (e.Data["SqlState"].ToString() == 23505.ToString())
            {
                returnValue = -5;
            }
            return returnValue;
        }
        return returnValue;
    }
    public int IsValidUser(string username, string password)
    {
        string selectUserQuery = "SELECT * FROM users WHERE username = @username";

        using (NpgsqlCommand command = new NpgsqlCommand(selectUserQuery, _connection))
        {
            // Добавление параметра для поиска
            command.Parameters.AddWithValue("@username", username);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    string storedPassword = reader["password"].ToString();

                    // Сравнение введенного пароля с хешем из базы данных
                    if (storedPassword == password)
                    {
                        User user = new User(User_id: (int)reader[name: "user_id"],
                            username: reader[name: "username"].ToString(),
                            password: reader[name: "password"].ToString(), coins: (int)reader[name: "coins"]);
                        return 0;
                    }
                    return -1;
                }
                return -2;
            }
        }
    }
    public int authenticationLogin(string auth, string username)
    {
        try
        {
            DateTime dateTime = DateTime.Now;
            string insertQuery =
                "INSERT INTO tokens (username, token, login, logout) VALUES (@username, @auth, @current_date, NULL);";
            using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, _connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@auth", auth);
                command.Parameters.AddWithValue("@current_date", dateTime);
                return command.ExecuteNonQuery();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e);
            throw;
        }


    }
    public int changeUsername(string usernameBefore, string usernameToChange)
    {
        int result = 0;

        try
        {
            string updateQuery = "UPDATE users SET username = @usernameToChange WHERE username = @usernameBefore;";
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(updateQuery, _connection))
            {
                npgsqlCommand.Parameters.AddWithValue("@usernameBefore", usernameBefore);
                npgsqlCommand.Parameters.AddWithValue("@usernameToChange", usernameToChange);
                result = npgsqlCommand.ExecuteNonQuery();
            }
            string update1Query = "UPDATE tokens SET username = @usernameToChange WHERE username = @usernameBefore;";
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(update1Query, _connection))
            {
                npgsqlCommand.Parameters.AddWithValue("@usernameBefore", usernameBefore);
                npgsqlCommand.Parameters.AddWithValue("@usernameToChange", usernameToChange);
                result += npgsqlCommand.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            if (e.Data["SqlState"].ToString() == 23505.ToString())
            {
                return -5;
            }

            throw;
        }

        return result;
    }
    public int changePassword(string username, string passwordToChange)
    {
        int result = 0;

        try
        {
            string updateQuery = "UPDATE users SET password = @passwordToChange WHERE username = @username;";
            using (NpgsqlCommand npgsqlCommand = new NpgsqlCommand(updateQuery, _connection))
            {
                npgsqlCommand.Parameters.AddWithValue("@passwordToChange", passwordToChange);
                npgsqlCommand.Parameters.AddWithValue("@username", username);
                result = npgsqlCommand.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            if (e.Data["SqlState"].ToString() == 23505.ToString())
            {
                return -5;
            }

            throw;
        }

        return result;
    }
    public int addCardToDb(string name, string category, string type, int damage)
    {
        try
        {
            // Параметризованный SQL-запрос для вставки данных в таблицу decks
            string insertQuery =
                "INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES (@name, @category, @type, @damage) RETURNING card_id";

            using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, _connection))
            {
                // Добавление параметров
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@type", type);
                command.Parameters.AddWithValue("@damage", damage);

                return (int)command.ExecuteScalar();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
    public int addPackageToDb(List<Card> packageList)
    {
        List<int> Ids = new List<int>();
        foreach (var card in packageList)
        {
            Ids.Add(card.card_id);
        }

        try
        {
            // Параметризованный SQL-запрос для вставки данных в таблицу decks
            string insertQuery =
                "INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (@card1, @card2, @card3, @card4, @card5, @inStore) RETURNING package_id";

            using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, _connection))
            {
                int i = 1;
                foreach (var card in packageList)
                {
                    command.Parameters.AddWithValue("@card" + i, card.card_id);
                    i++;
                }

                command.Parameters.AddWithValue("@inStore", true);

                return (int)command.ExecuteScalar();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }



    }
    public int BuyPackage(string username, int package_id)
    {
        try
        {
            int userCoins = GetUser(username).Coins;
            int userId = GetUser(username).user_id;

            if (userCoins < 5)
            {
                return -1; // Insufficient coins
            }

            List<int> cardIds = new List<int>();
            string selectQuery = "SELECT * FROM packages WHERE package_id = @package_id";

            using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectQuery, _connection))
            {
                selectCommand.Parameters.AddWithValue("@package_id", package_id);
                using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                {
                    if (reader.Read()) // Check if there is a row
                    {
                        // Assuming "admin" is a boolean column
                        bool inStore = Convert.ToBoolean(reader["instore"]);

                        if (!inStore)
                        {
                            return -3; // Package not in the store
                        }

                        cardIds.Add(Convert.ToInt32(reader["card1_id"]));
                        cardIds.Add(Convert.ToInt32(reader["card2_id"]));
                        cardIds.Add(Convert.ToInt32(reader["card3_id"]));
                        cardIds.Add(Convert.ToInt32(reader["card4_id"]));
                        cardIds.Add(Convert.ToInt32(reader["card5_id"]));
                    }
                    else
                    {
                        return -5;
                    }
                }
            }

            int result = 0;

            using (NpgsqlTransaction transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (var card_id in cardIds)
                    {
                        string insertQuery =
                            "INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id) RETURNING stack_id";

                        using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, _connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@user_id", userId);
                            insertCommand.Parameters.AddWithValue("@card_id", card_id);

                            result += (int)insertCommand.ExecuteScalar();
                        }
                    }

                    string updateQuery = "UPDATE users SET coins = @coins WHERE username = @username;";
                    using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateQuery, _connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@coins", userCoins - 5);
                        updateCommand.Parameters.AddWithValue("@username", username);
                        updateCommand.ExecuteNonQuery();
                    }

                    string updatePackageQuery = "UPDATE packages SET inStore = false WHERE package_id = @package_id;";
                    using (NpgsqlCommand updatePackageCommand =
                           new NpgsqlCommand(updatePackageQuery, _connection, transaction))
                    {
                        updatePackageCommand.Parameters.AddWithValue("@package_id", package_id);
                        updatePackageCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Log the exception details for debugging
                    Console.WriteLine($"Error during package purchase: {ex}");

                    // Handle exceptions and rollback the transaction if needed
                    transaction.Rollback();
                    throw;
                }
            }

            return result; // Successful purchase
        }
        catch (Exception e)
        {
            // Log the exception details for debugging
            Console.WriteLine($"Error during package purchase: {e}");
            throw;
        }
    }
    public bool isAdminUser(string username)
    {
        bool returnValue = false;
        try
        {
            string selectUserQuery = "SELECT * FROM users WHERE username = @username";
            using (NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserQuery, connection: _connection))
            {
                // Add the parameter for username
                command.Parameters.AddWithValue(parameterName: "@username", value: username);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read()) // Check if there is a row
                    {
                        // Assuming "admin" is a boolean column
                        if (reader["admin"] != DBNull.Value && (bool)reader["admin"])
                        {
                            returnValue = true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine("Error: " + ex.Message);
        }


        return returnValue;
    }
    public bool isUserOnline(string username, string token)
    {
        try
        {
            string selectUserQuery = "SELECT COUNT(*) FROM tokens WHERE username = @username AND token = @token";
            using (NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserQuery, connection: _connection))
            {
                // Add parameters for username and token
                command.Parameters.AddWithValue(parameterName: "@username", value: username);
                command.Parameters.AddWithValue(parameterName: "@token", value: token);

                int count = Convert.ToInt32(command.ExecuteScalar());

                // If count is greater than 0, user exists
                return count > 0;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public List<Card> getPackage(int packageId)
    {
        List<int> cardIds = new List<int>();
        string selectFromPackagesQuery = "SELECT * FROM packages WHERE package_id = @package_id";

        using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectFromPackagesQuery, _connection))
        {
            selectCommand.Parameters.AddWithValue("@package_id", packageId);
            using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
            {
                if (reader.Read()) // Check if there is a row
                {
                    // Sample usage of IsDBNull to handle NULL values
                    cardIds.Add(
                        reader.IsDBNull(reader.GetOrdinal("card1_id")) ? 0 : Convert.ToInt32(reader["card1_id"]));
                    cardIds.Add(
                        reader.IsDBNull(reader.GetOrdinal("card2_id")) ? 0 : Convert.ToInt32(reader["card2_id"]));
                    cardIds.Add(
                        reader.IsDBNull(reader.GetOrdinal("card3_id")) ? 0 : Convert.ToInt32(reader["card3_id"]));
                    cardIds.Add(
                        reader.IsDBNull(reader.GetOrdinal("card4_id")) ? 0 : Convert.ToInt32(reader["card4_id"]));
                    cardIds.Add(
                        reader.IsDBNull(reader.GetOrdinal("card5_id")) ? 0 : Convert.ToInt32(reader["card5_id"]));
                }
            }
        }


        List<Card> cards = new List<Card>();

        foreach (var id in cardIds)
        {
            string selectQuery = "SELECT * FROM cards WHERE card_id = @card_id";

            using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
            {
                command.Parameters.AddWithValue("@card_id", id);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        bool spell = reader["card_category"].ToString() == "Spell";

                        // Create a Card object and add it to the cards list
                        Card card = new Card(
                            Convert.ToInt32(reader["card_id"]),
                            reader["card_name"].ToString(),
                            reader["card_category"].ToString(),
                            Convert.ToInt32(reader["card_damage"]),
                            spell,
                            reader["card_type"].ToString()
                        );

                        cards.Add(card);
                    }
                }
            }
        }


        return cards;
    }
    public List<Card> getDeckFromUser(string username)
    {
        User? user = GetUser(username);
        List<Card> cards = new List<Card>();
        string selectUserCardsQuery =
            "SELECT cards.* FROM decks JOIN cards ON decks.card_id = cards.card_id WHERE decks.user_id = @user_id;";
        using (NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserCardsQuery, connection: _connection))
        {
            command.Parameters.AddWithValue(parameterName: "@user_id", value: user.user_id);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                // Если пользователь найден
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        // Прочитываем значения из результата запроса
                        int card_id = reader.GetInt32(reader.GetOrdinal("card_id"));
                        string Name = reader.GetString(reader.GetOrdinal("card_name"));
                        string Category = reader.GetString(reader.GetOrdinal("card_category"));
                        int Damage = Convert.ToInt32(reader.GetString(reader.GetOrdinal("card_damage")));
                        bool Spell;
                        if (Category == "Spell")
                        {
                            Spell = true;
                        }
                        else
                        {
                            Spell = false;
                        }

                        string Type = reader.GetString(reader.GetOrdinal("card_type"));

                        // Создаем объект Card и добавляем его в список
                        Card card = new Card(card_id, Name, Category, Damage, Spell, Type);
                        cards.Add(card);
                    }
                }
            }
        }

        return cards;
    }
    public bool CheckCardsBelongToUser(int userId, List<int> cardIds)
    {
        int result = 0;
        try
        {
            string selectQuery = "SELECT COUNT(*) FROM stacks WHERE user_id = @user_id AND card_id IN (";

            // Adding placeholders for card parameters
            List<string> cardPlaceholders = new List<string>();
            for (int i = 0; i < cardIds.Count; i++)
            {
                cardPlaceholders.Add($"@card_id{i}");
            }

            selectQuery += string.Join(", ", cardPlaceholders) + ")";

            using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectQuery, _connection))
            {
                selectCommand.Parameters.AddWithValue("@user_id", userId);

                // Adding card parameters
                for (int i = 0; i < cardIds.Count; i++)
                {
                    selectCommand.Parameters.AddWithValue($"@card_id{i}", cardIds[i]);
                }

                result += Convert.ToInt32(selectCommand.ExecuteScalar());

                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        try
        {
            string selectQuery = "SELECT COUNT(*) FROM decks WHERE user_id = @user_id AND card_id IN (";

            // Adding placeholders for card parameters
            List<string> cardPlaceholders = new List<string>();
            for (int i = 0; i < cardIds.Count; i++)
            {
                cardPlaceholders.Add($"@card_id{i}");
            }

            selectQuery += string.Join(", ", cardPlaceholders) + ")";

            using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectQuery, _connection))
            {
                selectCommand.Parameters.AddWithValue("@user_id", userId);

                // Adding card parameters
                for (int i = 0; i < cardIds.Count; i++)
                {
                    selectCommand.Parameters.AddWithValue($"@card_id{i}", cardIds[i]);
                }

                result += Convert.ToInt32(selectCommand.ExecuteScalar());

                // If the count is equal to the number of cards, it means all cards belong to the user
                return result == cardIds.Count;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public void InsertCardsIntoDeck(int userId, List<int> cardIDs)
    {
        using (NpgsqlTransaction transaction = _connection.BeginTransaction())
        {
            try
            {
                foreach (int cardId in cardIDs)
                {
                    string insertQuery = "INSERT INTO decks (user_id, card_id) VALUES (@user_id, @card_id)";

                    using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, _connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@user_id", userId);
                        insertCommand.Parameters.AddWithValue("@card_id", cardId);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error during deck insertion: {ex}");

                // Handle exceptions and rollback the transaction if needed
                transaction.Rollback();
                throw;
            }
            
            
        }
        using (NpgsqlTransaction transaction = _connection.BeginTransaction())
        {
            try
            {
                foreach (int cardId in cardIDs)
                {
                    string deleteQuery = "DELETE FROM stacks WHERE user_id = @user_id AND card_id = @card_id";
                    
                    using (NpgsqlCommand deleteCommand = new NpgsqlCommand(deleteQuery, _connection, transaction))
                    {
                        deleteCommand.Parameters.AddWithValue("@user_id", userId);
                        deleteCommand.Parameters.AddWithValue("@card_id", cardId);

                        deleteCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting cards from stacks: {ex}");
                
                // Handle exceptions and rollback the transaction if needed
                transaction.Rollback();
                throw;
            }
        }
    }

    public void DeleteDeckByUserId(int userId)
    {
        List<int> deletedIds = getDeckCardIdsFromUser(userId);
        if (deletedIds.Count() == 0)
        {
            return;
        }

        using (NpgsqlTransaction transaction = _connection.BeginTransaction())
        {
            try
            {
                foreach (int cardId in deletedIds)
                {
                    string insertQuery = "INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id)";
                
                    using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, _connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@user_id", userId);
                        insertCommand.Parameters.AddWithValue("@card_id", cardId);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding cards to stacks: {ex}");
            
                // Handle exceptions and rollback the transaction if needed
                transaction.Rollback();
                throw;
            }
        }
        
        
        using (NpgsqlTransaction transaction = _connection.BeginTransaction())
        {
            try
            {
                string deleteQuery = "DELETE FROM decks WHERE user_id = @user_id";

                using (NpgsqlCommand deleteCommand = new NpgsqlCommand(deleteQuery, _connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@user_id", userId);

                    deleteCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error during deck deletion: {ex}");

                // Handle exceptions and rollback the transaction if needed
                transaction.Rollback();
                throw;
            }
        }
    }

    private List<int> getDeckCardIdsFromUser(int userId)
    {
        List<int> userDecks = new List<int>();
        string selectQuery = "SELECT * FROM decks WHERE user_id = @user_id";

        using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
        {
            command.Parameters.AddWithValue("@user_id", userId);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    userDecks.Add(reader.GetInt32(2));
                }
            }
        }

        return userDecks;
    }

    public int setDeckForUser(User? user, List<int>? cardIds)
    {
        if (!CheckCardsBelongToUser(user.user_id, cardIds))
        {
            return -1;
        }

        DeleteDeckByUserId(user.user_id);
        InsertCardsIntoDeck(user.user_id, cardIds);


        return 0;
    }

    public List<Trading> getTransactions()
    {
        List<Trading> tradingList = new List<Trading>();
        string selectQuery = "SELECT * FROM tradings WHERE instore = true";

        using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
        {
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Trading trading = new Trading();

                    trading.Id = reader.GetInt32(0);
                    trading.UserId = reader.GetInt32(1);
                    trading.CardToTrade = reader.GetInt32(2);
                    trading.Category = reader.GetString(3);
                    trading.Type = reader.IsDBNull(4) ? "not required" : reader.GetString(4);
                    trading.MinimumDamage = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                    trading.instore = reader.GetBoolean(6);

                    tradingList.Add(trading);
                }
            }
        }

        return tradingList;

    }

    public int setTransaction(Trading trading, string username)
    {
        User? user = GetUser(username);
        int count;
        //check if the card is in Stack
        string selectQuery = "SELECT COUNT(*) FROM stacks WHERE card_id = @card_id AND user_id = @user_id";
    
        using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
        {
            command.Parameters.AddWithValue("@card_id", trading.CardToTrade);
            command.Parameters.AddWithValue("@user_id", user.user_id); // Замените trading.UserId на актуальный идентификатор пользователя

            count = Convert.ToInt32(command.ExecuteScalar());

            // Если count больше 0, карта существует в таблице stacks
        }

        if (count > 0)
        {
            int cardId = trading.CardToTrade;
            string reqCategory = trading.Category;
            string? reqType = trading.Type;
            int? reqDamage = trading.MinimumDamage;
            int user_id = user.user_id;

            if (reqType == null)
            {
                reqType = string.Empty;
            }

            if (reqDamage == null)
            {
                reqDamage = 0;
            }
            
            using (NpgsqlTransaction transaction = _connection.BeginTransaction())
            {
                try
                {
                    string insertQuery = "INSERT INTO tradings (card_id, user_id, req_category, req_type, req_damage) " +
                                         "VALUES (@card_id, @user_id, @req_category, @req_type, @req_damage)";

                    using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, _connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@card_id", cardId);
                        insertCommand.Parameters.AddWithValue("@req_category", reqCategory);
                        insertCommand.Parameters.AddWithValue("@req_type", reqType);
                        insertCommand.Parameters.AddWithValue("@req_damage", reqDamage);
                        insertCommand.Parameters.AddWithValue("@user_id", user_id);
                        
                        insertCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting data into tradings: {ex}");
                    
                    // Handle exceptions and rollback the transaction if needed
                    transaction.Rollback();
                    return -2;
                    throw;
                }
            }
        }
        else
        {
            return -1;
            
        }

        return 0; 
    }

    public int CardTrading(int tradeId, string requestBody, string username)
    {
        int card_id = Convert.ToInt32(requestBody);
        User? user = GetUser(username);
        Trading trading = CheckIfTradeExists(tradeId);
        if (trading != null && trading.instore != false)
        {
            int userTwo = GetUserIdFromStacks(trading.CardToTrade);
            if (userTwo != user.user_id)
            {
                if (checkIfCardIsOwnedByUser(user.user_id, Convert.ToInt32(requestBody)))
                {
                    Card card = getCardById(card_id);
                    if (card.Category.ToLower() == trading.Category.ToLower() && card.Damage > trading.MinimumDamage)
                    {
                        if ((trading.Type == "") || (trading.Type.ToLower() == card.Type.ToLower()))
                        {
                            // Удаление записи с card_id для user_id
                            DeleteCardFromStacks(userTwo, trading.CardToTrade);
                            DeleteCardFromStacks(user.user_id, card_id);

                            // Добавление новой записи с card_id для userTwo_id
                            AddCardToStacks(user.user_id, trading.CardToTrade);
                            AddCardToStacks(userTwo, card_id);


                            makeTradingInStoreFalse(tradeId);
                            return 0;
                        }
                    }
                }
            }
            
            return -2;
        }
        else
        {
            return -1;
        }

        return 0;
    }

    private Card getCardById(int cardId)
    {
        string selectUserCardsQuery =
            "SELECT * FROM cards WHERE card_id = @card_id";
        using (NpgsqlCommand command = new NpgsqlCommand(cmdText: selectUserCardsQuery, connection: _connection))
        {
            command.Parameters.AddWithValue(parameterName: "@card_id", value: cardId);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                // Если пользователь найден
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        // Прочитываем значения из результата запроса
                        int card_id = reader.GetInt32(reader.GetOrdinal("card_id"));
                        string Name = reader.GetString(reader.GetOrdinal("card_name"));
                        string Category = reader.GetString(reader.GetOrdinal("card_category"));
                        int Damage = Convert.ToInt32(reader.GetString(reader.GetOrdinal("card_damage")));
                        bool Spell;
                        if (Category != "Spell")
                        {
                            Spell = true;
                        }
                        else
                        {
                            Spell = false;
                        }

                        string Type = reader.GetString(reader.GetOrdinal("card_type"));

                        // Создаем объект Card и добавляем его в список
                        return new Card(card_id, Name, Category, Damage, Spell, Type);
                    }
                }
                else
                {
                    return null;
                }

            }
        }

        return null;
    }

    private void makeTradingInStoreFalse(int trade_id)
    {
        string updateQuery = "UPDATE tradings SET instore = false WHERE trade_id = @trade_id";

        using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateQuery, _connection))
        {
            updateCommand.Parameters.AddWithValue("@trade_id", trade_id);

            updateCommand.ExecuteNonQuery();
        }
    }

    void DeleteCardFromStacks(int user_id, int card_id)
    {
        string deleteQuery = "DELETE FROM stacks WHERE user_id = @user_id AND card_id = @card_id";

        using (NpgsqlCommand deleteCommand = new NpgsqlCommand(deleteQuery, _connection))
        {
            deleteCommand.Parameters.AddWithValue("@user_id", user_id);
            deleteCommand.Parameters.AddWithValue("@card_id", card_id);

            deleteCommand.ExecuteNonQuery();
        }
    }
    void AddCardToStacks(int user_id, int card_id)
    {
        string insertQuery = "INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id)";

        using (NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, _connection))
        {
            insertCommand.Parameters.AddWithValue("@user_id", user_id);
            insertCommand.Parameters.AddWithValue("@card_id", card_id);

            insertCommand.ExecuteNonQuery();
        }
    }
    
    int GetUserIdFromStacks(int cardId)
    {
        string selectQuery = "SELECT user_id FROM stacks WHERE card_id = @card_id";
        int userId = -1;

        using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
        {
            command.Parameters.AddWithValue("@card_id", cardId);

            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    userId = reader.GetInt32(0);
                }
            }
        }

        return userId;
    }

    private bool checkIfCardIsOwnedByUser(int userId, int card_id)
    {
        
        bool result = false;
        try
        {
            string selectQuery = "SELECT COUNT(*) FROM stacks WHERE user_id = @user_id AND card_id = @card_id";

            using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectQuery, _connection))
            {
                selectCommand.Parameters.AddWithValue("@user_id", userId);

                // Adding card parameters
                
                selectCommand.Parameters.AddWithValue("@card_id", card_id);
                

                result = Convert.ToInt32(selectCommand.ExecuteScalar()) > 0;

                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return result;
    }

    private Trading CheckIfTradeExists(int tradeId)
    {
        Trading trading = null;
        string selectQuery = "SELECT * FROM tradings WHERE trade_id = @trade_id";

        try
        {
            using (NpgsqlCommand command = new NpgsqlCommand(selectQuery, _connection))
            {
                command.Parameters.AddWithValue("@trade_id", tradeId); // Замените trading.TradeId на актуальный идентификатор торговли

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        trading = new Trading
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            CardToTrade = reader.GetInt32(2),
                            Category = reader.GetString(3),
                            Type = reader.GetString(4),
                            MinimumDamage = reader.GetInt32(5),
                            instore = reader.GetBoolean(6)
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving trading record: {ex.Message}");
        }
        return trading;
    }

    public int DeleteTradingDeal(int idToDelete, string username)
    {
        User? user = GetUser(username);
        string selectQuery = "SELECT user_id FROM tradings WHERE trade_id = @trade_id";
        string updateQuery = "UPDATE tradings SET instore = false WHERE trade_id = @trade_id";

        using (NpgsqlCommand selectCommand = new NpgsqlCommand(selectQuery, _connection))
        {
            selectCommand.Parameters.AddWithValue("@trade_id", idToDelete);
            bool tradeFound = false;
            using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
            {
                

                while (reader.Read())
                {
                    int existingUserId = reader.GetInt32(0);

                    if (existingUserId != user.user_id)
                    {
                        return -3;
                        
                    }
                    else
                    {
                        tradeFound = true;
                        break; // Выход из цикла, так как уже найден пользователь
                    }
                }

                
            }
            if (tradeFound)
            {
                // Обновление значения instore на false
                using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateQuery, _connection))
                {
                    updateCommand.Parameters.AddWithValue("@trade_id", idToDelete);
                    updateCommand.ExecuteNonQuery();
                }

                return 0;
            }
            else
            {
                return -4;
            }
        }
    }

    public void setCardsAfterBattle(User? winner, List<Card> winnerNewCards)
    {
        foreach (var card in winnerNewCards)
        {
            using (NpgsqlCommand deleteCommand = new NpgsqlCommand("DELETE FROM decks WHERE card_id = @card_id", _connection))
            {
                deleteCommand.Parameters.AddWithValue("@card_id", card.card_id);
                deleteCommand.ExecuteNonQuery();
            }
        }
        foreach (var card in winnerNewCards)
        {
            using (NpgsqlCommand insertCommand = new NpgsqlCommand("INSERT INTO stacks (user_id, card_id) VALUES (@user_id, @card_id)", _connection))
            {
                insertCommand.Parameters.AddWithValue("@user_id", winner.user_id);
                insertCommand.Parameters.AddWithValue("@card_id", card.card_id);
                insertCommand.ExecuteNonQuery();
            }
        }

    }

    public void setStatsWinner(User? user)
    {
        string updateStatsQuery = @"
            UPDATE stats 
            SET wins = wins + 1, 
                games = games + 1, 
                elo = elo + 3 
            WHERE user_id = @userId;";

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand updateStatsCommand = new NpgsqlCommand(updateStatsQuery, connection))
            {
                updateStatsCommand.Parameters.AddWithValue("@userId", user.user_id);

                updateStatsCommand.ExecuteNonQuery();
            }
        }
    }

    public void setStatsLoser(User? user)
    {
        string updateStatsQuery = @"
            UPDATE stats 
            SET loses = loses + 1, 
                games = games + 1, 
                elo = elo - 5
            WHERE user_id = @userId;";

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand updateStatsCommand = new NpgsqlCommand(updateStatsQuery, connection))
            {
                updateStatsCommand.Parameters.AddWithValue("@userId", user.user_id);

                updateStatsCommand.ExecuteNonQuery();
            }
        }
    }

    public void setStatsDraw(User? user)
    {
        string updateStatsQuery = @"
            UPDATE stats 
            SET draws = draws + 1, 
                games = games + 1, 
                elo = elo
            WHERE user_id = @userId;";

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand updateStatsCommand = new NpgsqlCommand(updateStatsQuery, connection))
            {
                updateStatsCommand.Parameters.AddWithValue("@userId", user.user_id);

                updateStatsCommand.ExecuteNonQuery();
            }
        }    
    }

    public List<int> getStats(string username)
    {
        User? user = GetUser(username);
        List<int> returnValue = new List<int>();
        string selectStatsQuery = @"
            SELECT games, wins, loses, draws, elo
            FROM stats
            WHERE user_id = @user_id;";

        using (NpgsqlCommand selectStatsCommand = new NpgsqlCommand(selectStatsQuery, _connection))
        {
            selectStatsCommand.Parameters.AddWithValue("@user_id", user.user_id);

            using (NpgsqlDataReader reader = selectStatsCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    returnValue.Add(reader.GetInt32(0));
                    returnValue.Add(reader.GetInt32(1));
                    returnValue.Add(reader.GetInt32(2));
                    returnValue.Add(reader.GetInt32(3));
                    returnValue.Add(reader.GetInt32(4));
                    
                    return returnValue;

                }
            }
        }
        return returnValue;
    }

    public List<Stats> GetAllStats()
    {
        List<Stats> statsList = new List<Stats>();
        string selectStatsQuery = @"
            SELECT username, games, wins, loses, elo, draws
            FROM stats
            ORDER BY elo DESC";

        using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (NpgsqlCommand selectStatsCommand = new NpgsqlCommand(selectStatsQuery, connection))
            {

                using (NpgsqlDataReader reader = selectStatsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statsList.Add(new Stats(reader.GetString(0), reader.GetInt32(4),reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(5)));
                    }
                }
            }
        }

        return statsList;
    }
}