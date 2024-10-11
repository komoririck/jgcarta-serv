using System.Text;

namespace hololive_oficial_cardgame_server;

public class MatchRoom
{
    public bool centerStageArtUsed = false;
    public bool collabStageArtUsed = false;

    public PlayerInfo playerA;
    public PlayerInfo playerB;

    public int startPlayer = 0;

    public int firstPlayer = 0;
    public int secondPlayer = 0;

    public int currentPlayerActing = 0;
    public int currentPlayerTurn = 0;

    public int currentTurn = 0;

    public GAMEPHASE currentGamePhase = 0;
    public GAMEPHASE currentPlayerAGamePhase = 0;
    public GAMEPHASE currentPlayerBGamePhase = 0;
    public GAMEPHASE nextGamePhase = 0;

    public int currentGameHigh = 0;
    public int playerAGameHigh = 0;
    public int playerBGameHigh = 0;

    public int actionHight = 0;

    public int playerAActionTimmer = 180;
    public int playerBActionTimmer = 180;

    public bool PAMulliganAsked = false;
    public bool PBMulliganAsked = false;

    public bool playerAInicialBoardSetup = false;
    public bool playerBInicialBoardSetup = false;

    public List<Card> playerALimiteCardPlayed = new List<Card>();
    public List<Card> playerBLimiteCardPlayed = new List<Card>();

    public List<Card> playerAHand = new List<Card>();
    public List<Card> playerBHand = new List<Card>();

    public List<Card> playerATempHand = new List<Card>();
    public List<Card> playerBTempHand = new List<Card>();

    public List<Card> playerAHoloPower = new List<Card>();
    public List<Card> playerBHoloPower = new List<Card>();

    public List<Card> playerADeck = new List<Card>();
    public List<Card> playerBDeck = new List<Card>();

    public List<Card> playerABackPosition = new List<Card>();
    public List<Card> playerBBackPosition = new List<Card>();

    public Card playerAFavourite = new Card();
    public Card playerBFavourite = new Card();

    public Card playerAStage = new Card();
    public Card playerBStage = new Card();

    public Card playerACollaboration = new Card();
    public Card playerBCollaboration = new Card();

    public List<Card> playerAArquive = new List<Card>();
    public List<Card> playerBArquive = new List<Card>();

    public List<Card> playerALife = new List<Card>();
    public List<Card> playerBLife = new List<Card>();

    public List<Card> playerACardCheer = new List<Card>();
    public List<Card> playerBCardCheer = new List<Card>();

    public Card playerAOshi = new Card();
    public Card playerBOshi = new Card();

    public string currentCardResolving = "";

    [Flags]
    public enum GAMEPHASE : byte
    {
        StartMatch = 0,
        ResetStep = 1,
        DrawStep = 2,
        CheerStep = 3,
        CheerStepChoose = 4,
        CheerStepChoosed = 5,
        MainStep = 6,
        PerformanceStep = 7,
        UseArt = 8,
        EndStep = 9,
        ConditionedDraw = 101,
        ConditionedSummom = 102,
        HolomemDefeated = 103,
        HolomemDefeatedCheerChoose = 104,
        HolomemDefeatedCheerChoosed = 105
    }

    public bool CheckRoomPlayers(int id)
    {
        if (playerA.PlayerID == id || playerB.PlayerID == id)
            return true;
        return false;
    }

    public void PassTurn() {
        if (currentPlayerActing == playerA.PlayerID)
            currentPlayerActing = playerB.PlayerID;
        else
            currentPlayerActing = playerA.PlayerID;

        currentGamePhase = nextGamePhase;
        nextGamePhase++;
    }

    public List<string> CardListToStringList(List<Card> cards)
    {
        List<string> returnCards = new List<string>();
        foreach (Card c in cards) {
            returnCards.Add(c.cardNumber);
        }
        return returnCards;
    }

    static public int getCardFromDeck(List<Card> deck, List<Card> target, int amount)
    {
        if (deck.Count > amount) {
            if (deck.Count < amount)
                amount = deck.Count;

            for (int i = 0; i < amount; i++)
            {
                target.Add(deck[deck.Count - 1]);
                if (deck[deck.Count - 1] != null)
                    deck.RemoveAt(deck.Count - 1);
                else
                    return (i - amount);

            }
            return amount;
        }
        return amount;
    }
    static public void getCardFromDeckIfType(List<Card> deck, List<Card> target, string type)
    {
        List<Card> newDeck = new();
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].cardType.Equals(type))
            {
                target.Add(deck[i]);
            }
            else
            {
                newDeck.Add(deck[i]);
            }
        }
        deck = newDeck;
    }

    public void suffleHandToTheDeck(List<Card> deck, List<Card> hand)
    {
        deck.AddRange(hand);
        hand.Clear();
    }

    public List<Card> ShuffleCards(List<Card> list)
    {
        Random random = new Random();
        int n = list.Count;

        for (int i = list.Count - 1; i > 1; i--)
        {
            int rnd = random.Next(i + 1);

            Card value = list[rnd];
            list[rnd] = list[i];
            list[i] = value;
        }
        return list;
    }
    public List<Card> FillCardListWithEmptyCards(List<Card> cards)
    {
        List<Card> returnCards = new List<Card>();
        foreach (Card c in cards)
        {
            Card newCard = new Card();
            newCard.cardNumber = "";
            returnCards.Add(newCard);
        }
        cards = new List<Card>();
        return returnCards;
    }

    static public int FindPlayerMatchRoom(List<MatchRoom> LM, string playerid)
    {
        for (int i = 0; i < LM.Count; i++) {
            if (LM[i].playerA.PlayerID.Equals(int.Parse(playerid))  || LM[i].playerB.PlayerID.Equals(int.Parse(playerid))) { 
                return i;   
            }
        }
        return -1;
    }
    static public int GetOtherPlayer(MatchRoom m, int playerid)
    {
        if (m.playerA.PlayerID == playerid)
        {
            return m.playerB.PlayerID;
        }
        else {
            return m.playerA.PlayerID;
        } 
    }
}
