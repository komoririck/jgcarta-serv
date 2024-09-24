namespace hololive_oficial_cardgame_server;

public class RequestData
{
    public string type { get; set; }
    public string description { get; set; }
    public int sync { get; set; }
    public string requestObject { get; set; }
    public string extraRequestObject { get; set; }
}

public class PlayerRequest
{
    public string playerID { get; set; }
    public string password { get; set; }
    public RequestData requestData { get; set; }
}