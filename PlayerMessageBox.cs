namespace hololive_oficial_cardgame_server;

public class PlayerMessageBox
{
    public int MessageID { get; set; }

    public int PlayerID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? ObtainedDate { get; set; }

}