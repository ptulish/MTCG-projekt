using System.Runtime.CompilerServices;
using server1.DB;

namespace MyServer.Classes;

public class Battle
{
    
    public Battle(User? userLeft, User? userRight, DbCommands dbCommands)
    {
        this.UserLeft = userLeft;
        this.UserRight = userRight;
        this.DbCommandsForBattle = dbCommands;
        userLeftDeck = dbCommands.getDeckFromUser(userLeft.Username);
        userRightDeck = dbCommands.getDeckFromUser(userRight.Username);
    }
    public DbCommands DbCommandsForBattle { get; set; }
    public User? UserLeft { get; set; }
    public  User? UserRight { get; set; }
    private List<Card> userLeftDeck = new List<Card>();
    private List<Card> userRightDeck = new List<Card>();
    private List<Card> winnerNewCards = new List<Card>();
    public User? Winner = null;
    public string returnWinner { get; set; }
    public string returnLoser { get; set; }

    public string returnDraw { get; set; }
    public string RoundLogs { get; set; }
    public Battle StartBattle()
    {
        int i = 1;
        while (userLeftDeck.Count > 0 && userRightDeck.Count > 0 && i < 101)
        {
            Random random = new Random();
            int randomNumber1 = random.Next(userLeftDeck.Count);
            int randomNumber2 = random.Next(userRightDeck.Count);
            RoundLogs +=  $"\nRound {i}.: {startRound(randomNumber1, randomNumber2)}";
            i++;
        }

        if (userLeftDeck.Count() == 0)
        {
            Winner = UserRight;
            winnerNewCards = DbCommandsForBattle.getDeckFromUser(UserLeft.Username);

        } else if (userRightDeck.Count == 0)
        {
            Winner = UserLeft;
            winnerNewCards = DbCommandsForBattle.getDeckFromUser(UserRight.Username);
        }
        else
        {
            returnDraw = $"In this Battle is no winner.\n {i - 1} Rounds\n Battle log: {RoundLogs}";
            return this;
        }
        returnLoser = $"You lose in the Battle.\n {i} Rounds\n Battle log: {RoundLogs}";
        returnWinner = $"You have won this Battle!!!\n{i} Rounds\nBattle log: {RoundLogs}\n Got new Cards";
        i = 1;
        foreach (var card in winnerNewCards)
        {
            returnWinner += $"\n {i}. {card.Name} {card.Category} {card.Type} {card.Damage}";
            i++;
        }

        DbCommandsForBattle.setCardsAfterBattle(Winner, winnerNewCards);
        
        Console.WriteLine(RoundLogs);

        return this;
    }

    private string startRound(int random1, int random2)
    {
        
        Card card1 = userLeftDeck[random1];
        Card card2 = userRightDeck[random2];
        bool isCard1Spell = card1.Category == "Spell";
        bool isCard2Spell = card1.Category == "Spell";
        int damageCard1ForTheRound = card1.Damage;
        int damageCard2ForTheRound = card2.Damage;
        string winner = string.Empty;

        if (isCard1Spell || isCard2Spell)
        {
            switch (card1.Type)
            {
                case "Water":
                {
                    if (card1.Type == "Fire")
                    {
                        damageCard1ForTheRound *= 2;
                    }

                    if (card1.Type == "Normal")
                    {
                        damageCard1ForTheRound /= 2;
                    }
                    break;
                }
                case "Fire":
                {
                    if (card1.Type == "Normal")
                    {
                        damageCard1ForTheRound *= 2;
                    }

                    if (card1.Type == "Water")
                    {
                        damageCard1ForTheRound /= 2;
                    }
                    break;
                }
                case "Normal":
                {
                    if (card1.Type == "Water")
                    {
                        damageCard1ForTheRound *= 2;
                    }

                    if (card1.Type == "Fire")
                    {
                        damageCard1ForTheRound /= 2;
                    }
                    break;
                }
            }
        }
        
        if (damageCard1ForTheRound > damageCard2ForTheRound)
        {
            userLeftDeck.Add(userRightDeck[random2]);
            userRightDeck.RemoveAt(random2);
            winner = card1.Name;
        }
        else
        {
            userRightDeck.Add(userLeftDeck[random1]);
            userLeftDeck.RemoveAt(random1);
            winner = card2.Name;
        }

        return $" {card1.Name} vs. {card2.Name}. Winner: {winner} !";
    }
}