using server1.DB;

namespace MyServer.Classes;

public class User
{
    public int user_id { get; set; }
    public string? Username { get; set; }
    public string Password { get; set; }
    public int Coins { get; set; }
    public List<Card>? Deck { get; set; }
    public List<Card>? MyStack { get; set; }
    public User(int User_id, string username, string password, int coins)
    {
        user_id = User_id;
        Username = username;
        Password = password;
        Coins = coins;
        Deck = getDeck(user_id);
        MyStack = getMyStack(user_id);
    }

    public User()
    {
        
    }

    public User(string username, string password)
    {
        Username = username;
        Password = password;
        Coins = 20;
        Deck = new List<Card>();
        MyStack = new List<Card>();
    }

    private List<Card> getMyStack(int userId)
    {
        return new List<Card>(); //DbCommands.getCardsFromUser(userId);
    }

    private List<Card> getDeck(int userId)
    {
        return new List<Card>();
    }
}