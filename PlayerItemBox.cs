namespace hololive_oficial_cardgame_server;

public class PlayerItemBox
{
    public int PlayerItemBoxID { get; set; }

    public int PlayerID { get; set; }

    public int ItemID { get; set; }

    public int Amount { get; set; }

    public DateTime ObtainedDate { get; set; }

    public DateTime? ExpirationDate { get; set; }
}