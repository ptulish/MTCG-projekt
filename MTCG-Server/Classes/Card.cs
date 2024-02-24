namespace MyServer.Classes;

public class Card
{
    public int card_id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public int Damage { get; private set; }
    public bool Spell { get; private set; }
    public string Type { get; private set; }
    public Card(int cardId, string name, string category, int damage, bool spell, string type)
    {
        card_id = cardId;
        Name = name;
        Category = category;
        Damage = damage;
        Spell = spell;
        Type = type;
    }
    public Card()
    {
        Random random = new Random();

        int randomNumber = random.Next(2);
        if (randomNumber == 0)
        {
            Category = "Monster";
            Spell = false;
        }
        else
        {
            Category = "Spell";
            Spell = true;
        }

        randomNumber = random.Next(5);
        switch (randomNumber)
        {
            case 0:
                Type = "Water";
                break;
            case 1:
                Type = "Fire";
                break;
            default:
                Type = "Normal";
                break;
        }

        if (Category == "Spell")
        {
            switch (Type)
            {
                case "Water":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Aqua Cascade";
                            Damage = random.Next(0, 51);
                            break;
                        case 1:
                            Name = "Hydro Vortex";
                            Damage = random.Next(10, 61);
                            break;
                        case 2:
                            Name = "Nimbus Torrent";
                            Damage = random.Next(5, 56);
                            break;
                        case 3:
                            Name = "Aqueduct Surge";
                            Damage = random.Next(15, 66);
                            break;
                        case 4:
                            Name = "Mystic Deluge";
                            Damage = random.Next(20, 71);
                            break;
                        case 5:
                            Name = "Ripple Wave";
                            Damage = random.Next(8, 59);
                            break;
                        case 6:
                            Name = "Tsunami Veil";
                            Damage = random.Next(25, 76);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Fire":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Inferno Blaze";
                            Damage = random.Next(5, 55);
                            break;
                        case 1:
                            Name = "Pyroburst";
                            Damage = random.Next(15, 65);
                            break;
                        case 2:
                            Name = "Ember Surge";
                            Damage = random.Next(8, 59);
                            break;
                        case 3: 
                            Name = "Flamewave Torrent";
                            Damage = random.Next(20, 70);
                            break;
                        case 4:
                            Name = "Ingition Burst";
                            Damage = random.Next(25, 76);
                            break;
                        case 5:
                            Name = "Scorching Cyclone";
                            Damage = random.Next(12, 63);
                            break;
                        case 6:
                            Name = "Firestorm Fury";
                            Damage = random.Next(30, 80);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Normal":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Elemental Strike";
                            Damage = random.Next(3, 48);
                            break;
                        case 1:
                            Name = "Nature's Wrath";
                            Damage = random.Next(12, 58);
                            break;
                        case 2:
                            Name = "Terra Surge";
                            Damage = random.Next(7, 53);
                            break;
                        case 3:
                            Name = "Gale Slash";
                            Damage = random.Next(15, 61);
                            break;
                        case 4:
                            Name = "Aurora Burst";
                            Damage = random.Next(18, 64);
                            break;
                        case 5:
                            Name = "Quake Pulse";
                            Damage = random.Next(10, 56);
                            break;
                        case 6:
                            Name = "Radiant Surge";
                            Damage = random.Next(20, 70);
                            break;
                        default:
                            break;
                    }
                    break;
                default:

                    break;
            }
        }

        if (Category == "Monster")
        {
            switch (Type)
            {
                case "Water":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Water Goblin";
                            Damage = random.Next(20, 40);
                            break;
                        case 1:
                            Name = "Aqua Specter";
                            Damage = random.Next(15, 87);
                            break;
                        case 2:
                            Name = "Tidal Leviathan";
                            Damage = random.Next(30, 77);
                            break;
                        case 3:
                            Name = "Frostbite Kraken";
                            Damage = random.Next(18, 55);
                            break;
                        case 4:
                            Name = "Ripple Behemoth";
                            Damage = random.Next(40, 80);
                            break;
                        case 5:
                            Name = "Torrential Naga";
                            Damage = random.Next(25, 70);
                            break;
                        case 6:
                            Name = "Quicksilver Hydra";
                            Damage = random.Next(30, 68);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Fire":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Fire Elves";
                            Damage = random.Next(37, 65);
                            break;
                        case 1:
                            Name = "Ember Dragon";
                            Damage = random.Next(25, 50);
                            break;
                        case 2:
                            Name = "Inferno Phoenix";
                            Damage = random.Next(20, 46);
                            break;
                        case 3:
                            Name = "Flame Imp";
                            Damage = random.Next(30, 56);
                            break;
                        case 4:
                            Name = "Ignition Hound";
                            Damage = random.Next(28, 64);
                            break;
                        case 5: 
                            Name = "Pyro Wyvern";
                            Damage = random.Next(23, 49);
                            break;
                        case 6:
                            Name = "Scorching Salamander";
                            Damage = random.Next(25, 48);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Normal":
                    randomNumber = random.Next(7);
                    switch (randomNumber)
                    {
                        case 0:
                            Name = "Earth Knight";
                            Damage = random.Next(15, 56);
                            break;
                        case 1:
                            Name = "Sylvan Wizzard";
                            Damage = random.Next(30, 80);
                            break;
                        case 2:
                            Name = "Stone Ork";
                            Damage = random.Next(20, 61);
                            break;
                        case 3:
                            Name = "Wind Sylph";
                            Damage = random.Next(13, 43);
                            break;
                        case 4:
                            Name = "Light Pixie";
                            Damage = random.Next(18, 48);
                            break;
                        case 5:
                            Name = "Quake Elemental";
                            Damage = random.Next(12, 42);
                            break;
                        case 6:
                            Name = "Celestial Unicorn";
                            Damage = random.Next(25, 66);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    
    
}