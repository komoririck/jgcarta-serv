namespace hololive_oficial_cardgame_server.SerializableObjects;

public class PlayerRequest
{
    public int id { get; set; } = 0;
    public string? playerID { get; set; }
    public string? password { get; set; }
    public string? email { get; set; }
    public string? type { get; set; }
    public string? description { get; set; }
    public string? requestObject { get; set; }
    public object? jsonObject { get; set; }

}