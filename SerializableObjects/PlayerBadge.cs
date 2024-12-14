namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerBadge
{
    public int BadgeID { get; set; }

    public int Rank { get; set; }

    public string PlayerID { get; set; }

    public DateTime? ObtainedDate { get; set; }
}