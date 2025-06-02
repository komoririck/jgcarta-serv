namespace hololive_oficial_cardgame_server.SerializableObjects;

public class DuelAction
{
    public string playerID { get; set; }
    public Card usedCard { get; set; }
    public string playedFrom { get; set; }
    public string local { get; set; }
    public Card targetCard { get; set; }
    public Card cheerCostCard { get; set; }
    public string actionType { get; set; }
    public string actionObject { get; set; }
    public string selectedSkill { get; set; }

    // START OF CONDITIONAL DRAW
    public List<string> SelectedCards { get; set; }
    public List<int> Order { get; set; }
    //END OF CONDITIONAL DRAW

    // START OF DRAW variables
    public bool suffle { get; set; }
    public bool suffleBackToDeck { get; set; }
    public string zone { get; set; }
    public List<Card> cardList { get; set; }
    // END OF DRAW variables

    public DuelAction SetID(string playerID) {
        this.playerID = playerID;
        return this;
    }

    public DuelAction DrawTopCardFromXToY(List<Card> listToDraw, string drawFrom, int amount) 
    {
        if (cardList == null)
            cardList = new List<Card>();

        zone = drawFrom;

        for (int i = amount; i > 0; i--) 
        {
            cardList.Add(listToDraw[i]);
        }
        return this;    
    }
}