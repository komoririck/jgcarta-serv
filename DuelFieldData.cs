namespace hololive_oficial_cardgame_server;

public class DuelFieldData
{

    public List<Card> playerAHand { get; set; }
    public List<Card> playerAArquive { get; set; }
    public List<Card> playerADeck { get; set; }
    public List<Card> playerAHoloPower { get; set; }
    public List<Card> playerABackPosition { get; set; }
    public Card playerAFavourite { get; set; }
    public Card playerAStage { get; set; }
    public Card playerACollaboration { get; set; }
    public List<Card> playerACardCheer { get; set; }
    public List<Card> playerALife { get; set; }

    public List<Card> playerBHand { get; set; }
    public List<Card> playerBArquive { get; set; }
    public List<Card> playerBDeck { get; set; }
    public List<Card> playerBHoloPower { get; set; }
    public List<Card> playerBBackPosition { get; set; }
    public Card playerBFavourite { get; set; }
    public Card playerBStage { get; set; }
    public Card playerBCollaboration { get; set; }
    public List<Card> playerBCardCheer { get; set; }
    public List<Card> playerBLife { get; set; }

    public int currentTurn { get; set; }
    public int currentPlayerTurn { get; set; }
    public int currentPlayerActing { get; set; }
    public int currentGamePhase { get; set; }
    public int firstPlayer { get; set; }
    public int secondPlayer { get; set; }
    public int currentGameHigh { get; set; }
}
