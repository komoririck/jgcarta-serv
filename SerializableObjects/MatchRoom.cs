using Microsoft.AspNetCore.Mvc.Formatters;
using System.Collections.Concurrent;
using System.Text;

namespace hololive_oficial_cardgame_server.SerializableObjects;

public class MatchRoom
{
    public List<CardEffect> ActiveTurnEffects = new();
    public List<CardEffect> ActiveTillNextUsageEffects = new();

    private readonly ConcurrentDictionary<string, Timer> playerTimers = new ConcurrentDictionary<string, Timer>();
    private int turnDurationSeconds = 123 + 1000;

    public bool centerStageArtUsed = false;
    public bool collabStageArtUsed = false;

    public bool usedSPOshiSkillPlayerA = false;
    public bool usedOshiSkillPlayerA = false;
    public bool usedSPOshiSkillPlayerB = false;
    public bool usedOshiSkillPlayerB = false;

    public PlayerInfo playerA;
    public PlayerInfo playerB;

    public string firstPlayer;
    public string secondPlayer;

    public string currentPlayerTurn;

    public int currentTurn = 0;

    public GAMEPHASE currentGamePhase = 0;
    public GAMEPHASE currentPlayerAGamePhase = 0;
    public GAMEPHASE currentPlayerBGamePhase = 0;
    public GAMEPHASE nextGamePhase = 0;

    public int currentGameHigh = 0;

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

    public Card playerAFavourite = null;
    public Card playerBFavourite = null;

    public Card playerAStage = null;
    public Card playerBStage = null;

    public Card playerACollaboration = null;
    public Card playerBCollaboration = null;

    public List<Card> playerAArquive = new List<Card>();
    public List<Card> playerBArquive = new List<Card>();

    public List<Card> playerALife = new List<Card>();
    public List<Card> playerBLife = new List<Card>();

    public List<Card> playerACardCheer = new List<Card>();
    public List<Card> playerBCardCheer = new List<Card>();

    public Card playerAOshi = null;
    public Card playerBOshi = null;

    public string currentCardResolving = "";
    public Art currentArtResolving = null;
    internal int cheersAssignedThisChainAmount;
    internal int cheersAssignedThisChainTotal = 1;
    internal List<object> extraInfo = new();
    internal string currentCardResolvingStage = "";
    internal int currentArtDamage;
    internal List<DuelAction> currentDuelActionResolvingRecieved = new();
    internal int playerAFixedRoll;
    internal int playerBFixedRoll;
    internal List<int> playerADiceRollList = new();
    internal List<int> playerBDiceRollList = new();
    internal bool currentCardResolvingProtectLife = false;
    internal bool playerBUsedSupportThisTurn;
    internal bool playerAUsedSupportThisTurn;

    public List<DuelAction> RecoilDuelActions { get; internal set; }

    [Flags]
    public enum GAMEPHASE : byte
    {
        StartMatch = 0,
        ResetStep = 1,
        ResetStepReSetStage = 11,
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
        HolomemDefeatedCheerChoosed = 105,
        ResolvingDamage = 106,
        RevolingAttachEffect = 107
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

        if (cards == null)
            return returnCards;

        foreach (Card c in cards)
        {
            returnCards.Add(new Card());
        }
        cards = new List<Card>();
        return returnCards;
    }

    static public int FindPlayerMatchRoom(List<MatchRoom> LM, string playerid)
    {
        for (int i = 0; i < LM.Count; i++)
        {
            if (LM[i].playerA.PlayerID.Equals(playerid) || LM[i].playerB.PlayerID.Equals(playerid))
            {
                return i;
            }
        }
        return -1;
    }
    static public string GetOtherPlayer(MatchRoom m, string playerid)
    {
        if (m.playerA.PlayerID.Equals(playerid))
        {
            return m.playerB.PlayerID;
        }
        else
        {
            return m.playerA.PlayerID;
        }
    }




    // Starts or resets the timer for a player's turn
    public void StartOrResetTimer(string playerId, Action<string> onTimeout)
    {
        // If the player already has a timer, reset it; otherwise, create a new one
        if (playerTimers.TryGetValue(playerId, out var existingTimer))
        {
            existingTimer.Change(turnDurationSeconds * 1000, Timeout.Infinite);
        }
        else
        {
            var newTimer = new Timer(_ => TimerExpired(playerId, onTimeout), null, turnDurationSeconds * 1000, Timeout.Infinite);
            playerTimers[playerId] = newTimer;
        }
    }

    // Called when the player takes an action to reset their timer
    public void ResetTimer(string playerId)
    {
        if (playerTimers.TryGetValue(playerId, out var timer))
        {
            timer.Change(turnDurationSeconds * 1000, Timeout.Infinite);
        }
    }

    // Manually stop a player's timer, e.g., when the turn ends or they disconnect
    public void StopTimer(string playerId)
    {
        if (playerTimers.TryRemove(playerId, out var timer))
        {
            timer.Dispose();
        }
    }

    // Called when the timer expires
    private void TimerExpired(string playerId, Action<string> onTimeout)
    {
        StopTimer(playerId); // Clean up the timer
        onTimeout?.Invoke(playerId); // Trigger the timeout action
    }

    public void GenerateArtEffectData(EFFECTTYPE type)
    {
        if (type == EFFECTTYPE.Turn) { 
            //hSD01-006
            ActiveTurnEffects.Add(new CardEffect()
            {
                artName = "SorAZ シンパシー",
                cardNumber = "hSD01-006",
                zoneTarget = "Stage",
                ExistXAtZone_Name = "AZKi",
                type = CardEffectType.BuffThisCardDamageExistXAtZone,
                Damage = 50,
                listIndex = 0
            });
        }
    }

    [Flags]
    public enum EFFECTTYPE : byte
    {
        Turn = 0,
        Match = 1,
        Continuous = 2
    }

}
