namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerTitle
{
    public int TitleID { get; set; }
    public string PlayerID { get; set; }
    public string TitleName { get; set; }
    public string TitleDescription { get; set; }
    public DateTime ObtainedDate { get; set; }
}