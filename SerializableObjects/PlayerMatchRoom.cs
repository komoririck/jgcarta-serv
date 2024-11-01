namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerMatchRoom
{
    public string RoomID { get; set; }
    public DateTime RegDate { get; set; }
    public int RoomCode { get; set; }
    public int MaxPlayer { get; set; }
    public int OwnerID { get; set; }

    public List<PlayerMatchRoomPool> PlayerMatchRoomPool { get; set; } = new List<PlayerMatchRoomPool>();

}