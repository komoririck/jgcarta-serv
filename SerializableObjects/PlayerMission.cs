namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerMission
{
    public int PlayerMissionListID { get; set; }

    public string PlayerID { get; set; }
    public int MissionID { get; set; }
    public DateTime? ObtainedDate { get; set; }
    public DateTime? ClearData { get; set; }

}