using Npgsql.TypeMapping;
using server1.DB;

namespace MyServer.Classes;

public class Package
{
    public List<Card> PackageList = new List<Card>();

    public int createPackage(DbCommands dbCommands, string username)
    {
        int returnValue;
        try
        {
            if (!dbCommands.isAdminUser(username))
            {
                return -2;
            }
            
            Card card = null;
            for (int i = 0; i < 5; i++)
            {
                card = new Card();
                card.card_id = dbCommands.addCardToDb(card.Name, card.Category, card.Type, card.Damage);
                if (card.card_id > -1)
                {
                    PackageList.Add(card);
                }
            }

             returnValue = dbCommands.addPackageToDb(PackageList) > 0 ? 0 : -1;
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }

        return returnValue;

    }
}