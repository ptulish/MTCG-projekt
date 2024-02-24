namespace MyServer.Classes;

public class Trading
{   
    public int Id { get; set; } 
    public int CardToTrade { get; set; }
    public string Category { get; set; }
    public string? Type { get; set; }
    public int? MinimumDamage { get; set; }
    public int UserId { get; set; }
    public bool instore { get; set; }

    public Trading()
    {
        
    }
}