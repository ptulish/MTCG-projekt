using Npgsql;
using server1.DB;

namespace MTCG_Test;

public static class DatabaseTestCreateEcerything
{
    private static ConnectionToDB ConnectionToDb = new ConnectionToDB();
    public static void ResetDb()
    {
        ConnectionToDb.OpenConnection();

        using (NpgsqlCommand command = new NpgsqlCommand(
                   "DROP TABLE IF EXISTS users, cards, decks, packages, stacks, stats, tokens, tradings",
                   ConnectionToDb.GetConnection()))
        {
            command.ExecuteNonQuery();
        }
    }


    public static void RegisterTestUsers()
    {
        using (var cmd = new NpgsqlCommand())
        {
            cmd.Connection = ConnectionToDb.GetConnection();
            cmd.CommandText = @"
                    INSERT INTO users (username, password, coins, admin) VALUES ('testUser1', 'testPassword1', 20, false);
                    INSERT INTO users (username, password, coins, admin) VALUES ('testUser2', 'testPassword2', 0, false);
                    INSERT INTO users (username, password, coins, admin) VALUES ('testUser3', 'testPassword3', 20, false);";
            cmd.ExecuteNonQuery();
            
        }
    }

    public static void CreateSomePackages()
    {
        using (var cmd = new NpgsqlCommand())
        {
            cmd.Connection = ConnectionToDb.GetConnection();

            cmd.CommandText = @"INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (1, 2, 3, 4, 5, true)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = @"INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (6, 7, 8, 9, 10, true)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = @"INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (11, 12, 13, 14, 15, false)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = @"INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (16, 17, 18, 19, 20, true)";
            cmd.ExecuteNonQuery();
        }
        
        string insertQuery =
            "INSERT INTO packages (card1_id, card2_id, card3_id, card4_id, card5_id, inStore) VALUES (@card1, @card2, @card3, @card4, @card5, @inStore) RETURNING package_id";

    }

    public static void CreateSomeCardsForTesting()
    {
        try
        {
            string name = "TestCard";
            string category = "TestCategory";
            string type = "TestType";
            int damage = 10;
            // Параметризованный SQL-запрос для вставки данных в таблицу decks
            string insertQuery =
                "INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES (@name, @category, @type, @damage) RETURNING card_id";

            for (int i = 0; i < 20; i++)
            {
                using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, ConnectionToDb.GetConnection()))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@category", category);
                    command.Parameters.AddWithValue("@type", type);
                    command.Parameters.AddWithValue("@damage", damage);

                    command.ExecuteScalar();
                }
            }

            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = ConnectionToDb.GetConnection();

                cmd.CommandText =
                    @"INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99)";
                cmd.ExecuteNonQuery();
                cmd.CommandText =
                    @"INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);
                    INSERT INTO cards (card_name, card_category, card_type, card_damage) VALUES ('Test Card', 'hello', 'nothing', 99);";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"INSERT INTO stacks (user_id, card_id) VALUES (3, 21);
                                    INSERT INTO stacks (user_id, card_id) VALUES (3, 22)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"INSERT INTO decks (user_id, card_id) VALUES (3, 23);
                                    INSERT INTO decks (user_id, card_id) VALUES (3, 24);
                                    INSERT INTO decks (user_id, card_id) VALUES (3, 25);
                                    INSERT INTO decks (user_id, card_id) VALUES (3, 26);";
                cmd.ExecuteNonQuery();
            }
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}