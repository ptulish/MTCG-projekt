using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Transactions;
using MyServer.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using server1.DB;
using JsonSerializer = System.Text.Json.JsonSerializer;

class Program
{
    public static BattleLobby lobby = new BattleLobby();

    public static List<User> Lobby = new List<User>();
    private static ConcurrentDictionary<string, TcpClient>
        activeClients = new ConcurrentDictionary<string, TcpClient>();

    private static ConnectionToDB connectionToDb = new ConnectionToDB();

    static void Main()
    {
        connectionToDb.OpenConnection();
        connectionToDb.initializeTables();

        DbCommands dbCommands = new DbCommands(connectionToDb);

        string url = "http://localhost:2345/";
        
        var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10001);
        TcpListener listener = new(ipEndPoint);

        try
        {
            listener.Start();
            Console.WriteLine($"Server runs on the address: {url}");

            List<string> tokList = new List<string>();
            while (true)
            {

                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(() => HandleRequest(client));
                thread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }

    static void HandleRequest(TcpClient tcpClient)
    {
        User user = null;
        NetworkStream networkStream = tcpClient.GetStream();
        DbCommands dbCommands = new DbCommands(connectionToDb);
        // Create a new instance of Random class

        using (BinaryReader binaryReader = new BinaryReader(networkStream))
        using (BinaryWriter binaryWriter = new BinaryWriter(networkStream))
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            StringBuilder allData = new StringBuilder();

            while (networkStream.DataAvailable && (bytesRead = binaryReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                allData.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
            
            // Получаем метод и путь запроса
            string[] infotmation;
            infotmation = getInofrmation(allData.ToString());
            int contentLength;
            string content = string.Empty;
            infotmation[3] = infotmation[3].Trim();
            if (infotmation[1] == "")
            {
                contentLength = 0;
                content = string.Empty;
            }
            else
            {
                contentLength = Convert.ToInt32(infotmation[1]);
                content = infotmation[2];
            }
            string[] method = infotmation[0].Split(" ");
            string path = method[1];
            string httpMethod = method[0];
            string auththenticationUser = infotmation[3];
            
            // Логика обработки запроса в зависимости от метода и пути
            if (httpMethod == "GET")
            {
                HandleGetRequest(path, binaryWriter, content, auththenticationUser, dbCommands);
            }
            else if (httpMethod == "POST")
            {
                HandlePostRequest(path, binaryWriter, content, auththenticationUser, dbCommands);
            }
            else if (httpMethod == "PUT")
            {
                HandlePutRequest(path, binaryWriter, content, auththenticationUser, dbCommands);
            }
            else if (httpMethod == "DELETE")
            {
                HandleDeleteRequest(path, binaryWriter, auththenticationUser, dbCommands);
            }
        }

        // Закрываем поток
        networkStream.Close();
        tcpClient.Close();
    }

    public static string[] getInofrmation(string buffer)
    {
        string length = string.Empty;
        string content = string.Empty;
        string[] lines = buffer.Split('\n');
        string auth = string.Empty;
        int i = 0;
        
        foreach (string line in lines)
        {
            if (line.StartsWith("Content-Length"))
            {
                string[] cntLng = line.Split(":");
                length = cntLng[1].Trim();

            }
            i++;
            if (line.StartsWith("Authorization: "))
            {
                auth = line.Split(" ")[2];
            }
        }

        for (int j = i - 1; j < lines.Length; j++)
        {
            content += lines[j];
        }

        return new string[] { lines[0].Trim(), length, content, auth };
    }

    private static void HandlePostRequest(string path, BinaryWriter binaryWriter, string requestBody, string auth,
        DbCommands dbCommands)
    {
        string[] pathStrings = path.Split('/', '?', '{', '}');
        
        switch (pathStrings[1])
        {
            case "users":
            {
                if (pathStrings.Length == 2)
                {
                    try
                    {
                        UserCred userCred = JsonSerializer.Deserialize<UserCred>(requestBody);
                        if (dbCommands.registerNewUSer(userCred.Username, userCred.Password) == -5)
                        {
                            string responseBody =
                                "HTTP/1.1 409 ERORR\r\nContent-Type: text/plain\r\n\r\nUser with same username already registered";
                            byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                            binaryWriter.Write(buffer);
                            binaryWriter.Flush();
                        }
                        else
                        {

                            string responseBody =
                                "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nData successfully retrieved";
                            byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                            binaryWriter.Write(buffer);
                            binaryWriter.Flush();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                break;
            }
            case "sessions":{
                UserCred userCred = JsonSerializer.Deserialize<UserCred>(requestBody);

                int result = dbCommands.IsValidUser(userCred.Username, userCred.Password);
                if (result < 0)
                {
                    string responseBody =
                        "HTTP/1.1 404 ERORR\r\nContent-Type: text/plain\r\n\r\nInvalid username/password provided";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }

                if (dbCommands.authenticationLogin("mtcgToken", userCred.Username) > 0)
                {
                    string responseBody = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nUser succuesfully logged in {\"Username\":\"" + userCred.Username + "-mtcgToken\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                break;
            }
            case "packages":
            {
                                
                string responseBody;
                byte[] buffer;
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                string username = auth.Split('-')[0];


                if (!dbCommands.isAdminUser(username))
                {
                    responseBody =
                        "HTTP/1.1 403 ERORR\r\nContent-Type: text/plain\r\n\r\nProvided user is not \"admin\"";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                

                if (pathStrings.Length == 2)
                {
                    
                    Package package = new Package();
                    int fromDB = package.createPackage(dbCommands, username);

                    if (fromDB == -1)
                    {
                        responseBody =
                            "HTTP/1.1 409 ERORR\r\nContent-Type: text/plain\r\n\r\nAt least one card in the packages already exists";
                        buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    } 
                    if (fromDB == -2)
                    {
                        responseBody =
                            "HTTP/1.1 403 ERORR\r\nContent-Type: text/plain\r\n\r\nUser is not admin";
                        buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }

                    string responsecontent = JsonSerializer.Serialize<List<Card>>(package.PackageList);
                    responseBody =
                        "HTTP/1.1 201 OK\r\nContent-Type: application/json\r\n\r\nPackage and cards successfully created" + responsecontent;
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                break;
            }
            case "transactions":
            {
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    var responseBody = "HTTP/1.1 401 ERROR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or not valid";
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                string username = auth.Split('-')[0];
                
                JObject jsonObject = JObject.Parse(requestBody);
                string package_id = jsonObject["package_id"]?.ToString();

                int result = dbCommands.BuyPackage(username, Convert.ToInt32(package_id));

                if (result == -1)
                {
                    var responseBody = "HTTP/1.1 403 ERROR\r\nContent-Type: text/plain\r\n\r\nNot enough money for buying a card package";
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                } else if (result == -5)
                {
                    var responseBody = "HTTP/1.1 404 ERROR\r\nContent-Type: text/plain\r\n\r\nPackage not found";
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                else if (result > 0)
                {
                    List<Card> cards = dbCommands.getPackage(Convert.ToInt32(package_id));
                    var responseBody = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nA package has been successfully bought " + JsonSerializer.Serialize<List<Card>>(cards);
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                } else if (result == -3)
                {
                    var responseBody = "HTTP/1.1 402 ERROR\r\nContent-Type: text/plain\r\n\r\nThe package is not more in the store";
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                
                break;
            }
            case "battles":
            {
                string responseBody;
                byte[] buffer;
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }

                if (dbCommands.getDeckFromUser(auth.Split('-')[0]).Count == 0)
                {
                    responseBody =
                        "HTTP/1.1 403 ERORR\r\nContent-Type: text/plain\r\n\r\nYour Deck is not configured";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }

                User? user = dbCommands.GetUser(auth.Split('-')[0]);
                
                string returnLog = lobby.EnterLobby(user, dbCommands);
                if (returnLog.StartsWith("You have"))
                {
                    dbCommands.setStatsWinner(user);
                } else if (returnLog.StartsWith("You lose"))
                {
                    dbCommands.setStatsLoser(user);
                }
                else
                {
                    dbCommands.setStatsDraw(user);
                }
                if (returnLog != string.Empty)
                {
                    responseBody =
                        "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n" + returnLog;
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }

                return;
            }
            case "tradings":
            {
                if (pathStrings.Length == 2)
                {
                    if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                    {
                        var responseBody =
                            "HTTP/1.1 401 ERROR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or not valid";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }
                    string username = auth.Split('-')[0];

                    Trading trading = JsonSerializer.Deserialize<Trading>(requestBody);

                    int result = dbCommands.setTransaction(trading, username);

                    if (result == -1)
                    {
                        var responseBody =
                            "HTTP/1.1 403 ERROR\r\nContent-Type: text/plain\r\n\r\nThe deal contains a card that is not owned by the user or locked in the deck.";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }

                    if (result == -2)
                    {
                        var responseBody =
                            "HTTP/1.1 409 ERROR\r\nContent-Type: text/plain\r\n\r\nA deal with this deal ID already exists.";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }

                    if (result == 0)
                    {
                        var responseBody =
                            "HTTP/1.1 201 OK\r\nContent-Type: text/plain\r\n\r\nTrading deal successfully created.";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }
                } else if (pathStrings.Length == 3)
                {
                    if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                    {
                        var responseBody =
                            "HTTP/1.1 401 ERROR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or not valid";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                        return;
                    }
                    string username = auth.Split('-')[0];

                    int result = dbCommands.CardTrading(Convert.ToInt32(pathStrings[2]),requestBody, username);
                    if (result == -1)
                    {
                        var responseBody =
                            "HTTP/1.1 404 ERROR\r\nContent-Type: text/plain\r\n\r\nThe provided ID was not found";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }

                    if (result == -2)
                    {
                        var responseBody =
                            "HTTP/1.1 403 ERROR\r\nContent-Type: text/plain\r\n\r\nThe offered Card is not owned by the user, or the requirements are not met (Type, MinimumDamage), or the offered card is locked in the deck, or the user tries to trade with self";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }

                    if (result == 0)
                    {
                        var responseBody =
                            "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nTrading deal successfully executed";
                        var buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }
                }
                break;
            }
        }
    }

    private static void HandleGetRequest(string path, BinaryWriter binaryWriter, string requestBody, string auth,
        DbCommands dbCommands)
    {
        string[] pathStrings = path.Split('/', '?', '{', '}');
        pathStrings = pathStrings.Select(s => s.Replace("%7D", "}")).ToArray();
        pathStrings = pathStrings.Select(s => s.Replace("%7B", "{")).ToArray();

        switch (pathStrings[1])
        {
            case "users":
            {
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {

                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                if (auth.Split('-')[0] != "admin-mtcgToken" && auth.Split('-')[0] != pathStrings[2])
                {
                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nYou do not have access\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                string username = pathStrings[2];
                try
                {
                    User? user = dbCommands.GetUser(username);
                    if (user == null)
                    {
                        string responseBody =
                            "HTTP/1.1 404 ERORR\r\nContent-Type: text/plain\r\n\r\nUser not found";
                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }
                    else
                    {
                        string content = JsonSerializer.Serialize<User>(user);
                        string responseBody =
                            "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nData successfully retrieved" +
                            content;
                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                break;
            }
            case "cards":
            {
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                string usernameToken = auth.Split('-')[0];

                List<Card>? cards = dbCommands.GetCardsFromUser(usernameToken);

                if (cards.Count() > 0)
                {
                    
                    string responseBody =
                        "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nData successfully retrieved" +
                        JsonSerializer.Serialize<List<Card>>(cards);
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                else
                {
                    string responseBody =
                        "HTTP/1.1 204 OK\r\nContent-Type: text/plain\r\n\r\nThe request was fine, but the user doesn't have any cards\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                
                break;
            }
            case "deck":
            {
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                string usernameToken = auth.Split('-')[0];
                string token = auth.Split('-')[1];
                
                List<Card> cards = dbCommands.getDeckFromUser(usernameToken);

                if (cards.Count() > 0)
                {
                    if (pathStrings.Length == 2)
                    {
                        string responseBody =
                            "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nData successfully retrieved" +
                            JsonSerializer.Serialize<List<Card>>(cards);
                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }
                    else
                    {
                        string respPlain = "Your Deck is: ";
                        int i = 1;
                        foreach (var card in cards)
                        {
                            respPlain +=
                                $"\n{i}. Name: {card.Name} Category: {card.Category} Type: {card.Type} Damage: {card.Damage}";
                            i++;
                        }


                        string responseBody =
                            "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nData successfully retrieved" +
                            respPlain;
                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        binaryWriter.Write(buffer);
                        binaryWriter.Flush();
                    }
                    
                }
                else
                {
                    string responseBody =
                        "HTTP/1.1 204 OK\r\nContent-Type: text/plain\r\n\r\nThe request was fine, but the user doesn't have any cards\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
                
                
                break;
                
            }
            case "tradings":
            {
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {

                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                    
                }

                List<Trading> tradings = dbCommands.getTransactions();

                if (tradings.Any())
                {
                    string responseBody =
                                        "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\nThere are traiding deals available\n" + JsonSerializer.Serialize<List<Trading>>(tradings);
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                else
                {
                    string responseBody =
                        "HTTP/1.1 204 OK\r\nContent-Type: text/plain\r\n\r\nThe request was fine, but there are no trading deals available\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                break;
            }
            case "stats":
            {
                string responseBody;
                byte[] buffer;
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {

                    responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                    
                }

                string username = auth.Split('-')[0];
                List<int> result = dbCommands.getStats(auth.Split('-')[0]);
                Stats stats = new Stats(username, result[4], result[0], result[1], result[2], result[3]);
                
                string jsonStats = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });

                responseBody =
                    "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n\nThe stats could be retrieved successfully" + jsonStats;
                buffer = Encoding.UTF8.GetBytes(responseBody);
                binaryWriter.Write(buffer);
                binaryWriter.Flush();

                break;
            }
            case "scoreboard":
            {
                string responseBody;
                byte[] buffer;
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {

                    responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;

                }

                string username = auth.Split('-')[0];
                List<Stats> statsList = dbCommands.GetAllStats();
                int i = 1;
                string responseJson =
                    JsonSerializer.Serialize(statsList, new JsonSerializerOptions { WriteIndented = true });

                responseBody =
                    "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nThe scoreboard could be retrieved successfully\n" + responseJson;
                buffer = Encoding.UTF8.GetBytes(responseBody);
                binaryWriter.Write(buffer);
                binaryWriter.Flush();
                return;

                break;
            }
        }
    }
    
    private static void HandleDeleteRequest(string path, BinaryWriter binaryWriter, string auth, DbCommands dbCommands)
    {
        string[] pathStrings = path.Split('/');
        
        if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
        {
            string responseBody =
                "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
            byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
            binaryWriter.Write(buffer);
            binaryWriter.Flush();
            return;
                    
        }
        string usernameToken = auth.Split('-')[0];
        int idToDelete = Convert.ToInt32(pathStrings[2]);

        if (pathStrings[1] == "tradings")
        {
            int result = dbCommands.DeleteTradingDeal(idToDelete, usernameToken);
            if (result == -3)
            {
                string responseBody =
                    "HTTP/1.1 403 ERORR\r\nContent-Type: text/plain\r\n\r\nThe deal contains a card that is not owned by the user\n";
                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                binaryWriter.Write(buffer);
                binaryWriter.Flush();
                return;
            }

            if (result == -4)
            {
                string responseBody =
                    "HTTP/1.1 404 ERORR\r\nContent-Type: text/plain\r\n\r\nThe deal not found\n";
                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                binaryWriter.Write(buffer);
                binaryWriter.Flush();
                return;
            }

            if (result == 0)
            {
                string responseBody =
                    "HTTP/1.1 200 ERORR\r\nContent-Type: text/plain\r\n\r\nTrading deal successfully deleted\n";
                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                binaryWriter.Write(buffer);
                binaryWriter.Flush();
                return;
            }
        }
    }

    private static void HandlePutRequest(string path, BinaryWriter binaryWriter, string requestBody, string auth, DbCommands dbCommands)
    {
        string[] pathStrings = path.Split('/', '?', '{', '}');

        try
        {
            if (pathStrings[1] == "users")
            {
                // Извлекаем подстроку между фигурными скобками
                string username = pathStrings[2];
                
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                if (auth.Split('-')[0] != "admin-mtcgToken" && auth.Split('-')[0] != username)
                {
                    string responseBody =
                        "HTTP/1.1 402 ERORR\r\nContent-Type: text/plain\r\n\r\nYou dont have access\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                
                
                string usernameToken = auth.Split('-')[0];

                JObject jsonObject = JObject.Parse(requestBody);
                string usernameToChange = jsonObject["Username"]?.ToString();
                string passwordToChange = jsonObject["Password"]?.ToString();
                int result = 0;
                if (usernameToChange != null) 
                {
                    result = dbCommands.changeUsername(username, usernameToChange);
                } 
                else if (passwordToChange != null)
                {
                    result = dbCommands.changePassword(username, passwordToChange);
                }
                if (result == -5)
                {
                    string responseBody =
                        "HTTP/1.1 410 ERORR\r\nContent-Type: text/plain\r\n\r\nUsername already in use";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                } 
                else if (result == 0)
                {
                    string responseBody =
                        "HTTP/1.1 404 ERORR\r\nContent-Type: text/plain\r\n\r\nUser not found";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                } else if (result == 1) 
                {
                    string responseBody =
                        "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nUser succuesfully updated";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                }
            }
            else if (pathStrings[1] == "deck")
            {
                
                if (auth.Length == 0 || !dbCommands.isUserOnline(auth.Split('-')[0], auth.Split('-')[1]))
                {
                    string responseBody =
                        "HTTP/1.1 401 ERORR\r\nContent-Type: text/plain\r\n\r\nAccess token is missing or invalid\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                    
                }
                string usernameToken = auth.Split('-')[0];

                List<int> cardIdFromRequest = JsonConvert.DeserializeObject<List<int>>(requestBody);

                if (cardIdFromRequest.Count() != 4)
                {
                    string responseBody =
                        "HTTP/1.1 400 ERORR\r\nContent-Type: text/plain\r\n\r\nThe provided deck did not include the required amoung of cards\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }
                User? user = dbCommands.GetUser(usernameToken);
                int result = dbCommands.setDeckForUser(user, cardIdFromRequest);

                if (result == -1)
                {
                    string responseBody =
                        "HTTP/1.1 403 ERORR\r\nContent-Type: text/plain\r\n\r\nAt least one of provided cards does not belong to the user or is not available\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();
                    return;
                }

                if (result == 0)
                {
                    string responseBody =
                        "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nThe deck has been successfully configured\n";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                    binaryWriter.Write(buffer);
                    binaryWriter.Flush();

                }
                
                

            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        

    }
}


public class UserCred
{
    public string Username { get; set; }
    public string Password { get; set; }

}
public class Stats
{
    public string Name { get; set; }
    public int Elo { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Loses { get; set; }
    public int Draws { get; set; }

    public Stats(string Name, int Elo, int Games, int Wins, int Loses, int Draws)
    {
        this.Name = Name;
        this.Elo = Elo;
        this.Games = Games;
        this.Wins = Wins;
        this.Loses = Loses;
        this.Draws = Draws;
    }
}