namespace hololive_oficial_cardgame_server.SerializableObjects;

public class DuelFieldData
{

    public List<Card> playerABackPosition { get; set; }
    public Card playerAFavourite { get; set; }
    public Card playerAStage { get; set; }
    public List<Card> playerACardCheer { get; set; }
    public List<Card> playerALife { get; set; }

    public List<Card> playerBBackPosition { get; set; }
    public Card playerBFavourite { get; set; }
    public Card playerBStage { get; set; }
    public List<Card> playerBCardCheer { get; set; }
    public List<Card> playerBLife { get; set; }

    public string currentPlayerTurn { get; set; }
    public string firstPlayer { get; set; }
    public string secondPlayer { get; set; }
}
