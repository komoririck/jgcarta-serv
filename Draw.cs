namespace hololive_oficial_cardgame_server;


[Serializable]
public class Draw
{
    public int playerID { get; set; }
    public bool suffle { get; set; }
    public string zone { get; set; }
    public List<Card> cardList { get; set; } = new List<Card>();
}
