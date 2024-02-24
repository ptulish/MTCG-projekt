using server1.DB;

namespace MyServer.Classes;

public class BattleLobby
{
    private static readonly List<User?> Lobby = new List<User?>();
    private static readonly object lockObject = new object();
    public static Battle battle;

    public string EnterLobby(User? user, DbCommands dbCommands)
    {
        Console.WriteLine($"User {user.Username} has entered the Lobby");

        lock (lockObject)
        {
            if (Lobby.Count > 0)
            {
                AddUserToLobby(user);                
                User? opponent = Lobby[0];
                Lobby.RemoveAt(0);
                Monitor.Pulse(lockObject);

                battle = StartBattle(user, opponent, dbCommands);
            }
            else
            {
                Console.WriteLine("And waiting for opponent");
                AddUserToLobby(user);
                Monitor.Wait(lockObject);

                Console.WriteLine(battle.UserLeft);
                Lobby.RemoveAt(0);
                Monitor.Pulse(lockObject);

            }
        }

        if (battle.Winner != null)
        {
            if (user.Username == battle.Winner.Username)
            {
                return battle.returnWinner;
            }
            else
            {
                return battle.returnLoser;
            }
        }

        return battle.returnDraw;

    }

    public void AddUserToLobby(User? user)
    {
        lock (lockObject)
        {
            Lobby.Add(user);
            Monitor.Pulse(lockObject);
        }
    }

    public Battle StartBattle(User? user1, User? user2, DbCommands dbCommands)
    {
        //user1 = Lobby[1]
        //user2 = lobby[0]
        Battle battle = new Battle(user1, user2, dbCommands);
        return battle.StartBattle();
    }
}