namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerMatchRoomPool
{
    public string MRPID { get; set; }
    public int PlayerID { get; set; }
    public int Board { get; set; }
    public int Chair { get; set; }
    public string Status { get; set; }
    public string MatchRoomID { get; set; }
    public DateTime RegDate { get; set; }
    public DateTime LasActionDate { get; set; }

}