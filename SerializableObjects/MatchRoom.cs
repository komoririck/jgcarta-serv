using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hololive_oficial_cardgame_server.SerializableObjects;

public class MatchRoom
{
    public static List<MatchRoom> _MatchRooms = new List<MatchRoom>();
    Guid roomID;

    public List<CardEffect> ActiveEffects = new();

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
    internal int cheersAssignedThisChainAmount = 0;
    internal int cheersAssignedThisChainTotal = 1;
    internal string currentCardResolvingStage = "";

    internal List<DuelAction> ResolvingEffectChain = new();

    public Art ResolvingArt = null;
    internal Card DeclaringAttackCard = null;
    internal Card BeingTargetedForAttackCard = null;

    internal int currentArtDamage;
    internal int currentEffectDamage;

    internal List<int> playerADiceRollList = new();
    internal List<int> playerBDiceRollList = new();

    internal bool playerBUsedSupportThisTurn;
    internal bool playerAUsedSupportThisTurn;

    internal int playerADiceRollCount = 0;
    internal int playerBDiceRollCount = 0;


    internal bool playerAResolveConfirmation;
    internal bool playerBResolveConfirmation;

    public List<DuelAction> RecoilDuelActions = new(); // what the hell i'm saving this for only one effect ?
    public List<PlayerRequest> RecordedPlayerRequest = new();

    public DuelFieldData initialBoardInfo;

    public MatchRoom()
    {
        this.roomID = Guid.NewGuid();


    }
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
        RevolingAttachEffect = 107,
        ResolvingDeclaringAttackEffects = 108
    }
    public enum Player
    {
        FirstPlayer = 0,
        SecondPlayer = 1,
        PlayerA = 0,
        PlayerB = 1,
        TurnPlayer = 2,
    }
    public enum PlayerZone
    {
        Deck,
        Cheer,
        Hand,
        Arquive,
        BackStage,
    }
    public List<string> GetPlayers()
    {
        return new List<string>() { firstPlayer, secondPlayer };
    }
    public List<string> GetPlayersStartWith(string playerID)
    {
        if (firstPlayer.Equals(playerID))
            return new List<string>() { firstPlayer, secondPlayer };
        else if (secondPlayer.Equals(playerID))
            return new List<string>() { secondPlayer, firstPlayer };

        return null;
    }
    public List<PlayerRequest> ReplicatePlayerRequestForOtherPlayers(List<string> playersID, PlayerRequest playerRequest = null, bool hidden = false, DuelAction duelAction = null, string type = "DuelUpdate", string description = "")
    {
        if (duelAction == null && playerRequest == null)
        {
            Lib.WriteConsoleMessage("DuelAction and PlayerRequest should nerver both be null here");
            return null;
        }

        if (playerRequest == null && string.IsNullOrEmpty(description))
        {
            Lib.WriteConsoleMessage("If playerRequest is null, we need a type and description, else the client will ignore");
            return null;
        }

        List<PlayerRequest> returnList = new List<PlayerRequest>();

        PlayerRequest playerWithVisibleData = null;
        if (playerRequest == null)
        {
            playerWithVisibleData = new PlayerRequest()
            {
                playerID = playersID[0],
                type = type,
                description = description,
            };
        }
        else
        {
            playerWithVisibleData = playerRequest;
        }
        playerWithVisibleData.requestObject = (duelAction == null) ? playerWithVisibleData.requestObject : JsonSerializer.Serialize(duelAction, Lib.jsonOptions);

        returnList.Add(playerWithVisibleData);

        if (hidden)
        {
            if (duelAction == null)
            {
                duelAction = JsonSerializer.Deserialize<DuelAction>(playerWithVisibleData.requestObject);
            }
            duelAction.cardList = FillCardListWithEmptyCards(duelAction.cardList);
        }

        foreach (string id in playersID)
        {
            if (id.Equals(playerWithVisibleData))
                continue;

            returnList.Add(new PlayerRequest()
            {
                playerID = id,

                password = playerWithVisibleData.password,
                email = playerWithVisibleData.email,

                type = playerWithVisibleData.type,
                description = playerWithVisibleData.description,
                requestObject = JsonSerializer.Serialize(duelAction, Lib.jsonOptions),

                jsonObject = playerWithVisibleData.jsonObject
            });
        }
        return returnList;
    }
    public void RecordPlayerRequest(List<PlayerRequest> playerRequest)
    {
        int id = RecordedPlayerRequest.Last().id + 1;
        foreach (PlayerRequest request in playerRequest)
        {
            request.id = id;
            RecordedPlayerRequest.Add(new PlayerRequest
            {
                id = id,
                playerID = request.playerID,
                password = request.password,
                email = request.email,
                type = request.type,
                description = request.description,
                requestObject = request.requestObject,
                jsonObject = request.jsonObject
            });
        }
    }
    public void PushPlayerAnswer()
    {
        int targetId = RecordedPlayerRequest[^1].id;
        for (int i = RecordedPlayerRequest.Count - 1; i >= 0; i--)
        {
            if (RecordedPlayerRequest[i].id != targetId)
                break;

            var webSocket = MessageDispatcher.playerConnections[RecordedPlayerRequest[i].playerID];
            webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(RecordedPlayerRequest[i], new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true }))), WebSocketMessageType.Text, true, CancellationToken.None);

        }
    }
    public void SuffleHandToTheDeck(List<Card> deck, List<Card> hand)
    {
        deck.AddRange(hand);
        hand.Clear();
    }
    public void SnapShotInitialBoard()
    {
        //DOTO
    }
    public void SwittchCardXToCardYButKeepPosition(string playerid, Card TargetPosition)
    {
        int backStagePos = CheckIfCardExistAtZone(playerid, TargetPosition, PlayerZone.BackStage);

        if (backStagePos < 0)
        {
            Lib.WriteConsoleMessage("Card targeted didnt exist");
            return;
        }

        // getting cards
        List<Card> playerBackstage = playerid == firstPlayer ? playerABackPosition : playerBBackPosition;
        Card currentStageCard = playerid == firstPlayer ? playerAStage : playerBStage;

        // backups
        Card backstageBackUP = new Card().CloneCard(playerBackstage[backStagePos]);
        Card stageBackUp = new Card().CloneCard(currentStageCard);

        // changing position
        backstageBackUP.cardPosition = currentStageCard.cardPosition; // Current stage position assigned to backstage card
        stageBackUp.cardPosition = playerBackstage[backStagePos].cardPosition; // Backstage position assigned to stage card

        // Assign back to the original references
        if (playerid == firstPlayer)
            playerAStage = backstageBackUP; // Update the stage with the backup
        else
            playerBStage = backstageBackUP; // Update the stage with the backup

        playerBackstage[backStagePos] = stageBackUp; // Update the player backstage       

    }
    public List<Card> GetListOfCardWithTag(Player player, PlayerZone playerZone, string bloomLevel)
    {
        List<Card> query = new();
        switch (playerZone)
        {
            case PlayerZone.Deck:
                query = player.Equals(Player.FirstPlayer) ? playerADeck : playerBDeck;
                break;
            default:
                //TODO
                break;
        }
        foreach (var card in query)
            card.GetCardInfo();
        return query.Where(r => r.bloomLevel == bloomLevel).ToList();
    }
    public int CheckIfCardExistAtZone(string playerId, string UsedCard, PlayerZone playerZone)
    {
        return CheckIfCardExistAtZone(playerId, new Card (UsedCard), playerZone, false);
    }
    public int CheckIfCardExistAtZone(string playerId, Card UsedCard, PlayerZone playerZone, bool checkZone = true)
    {
        int handPos = -1;
        List<Card> playerHand = null;

        switch (playerZone)
        {
            case PlayerZone.Hand:
                playerHand = playerId.Equals(firstPlayer) ? playerAHand : playerBHand;
                break;
            case PlayerZone.Arquive:
                playerHand = playerId.Equals(firstPlayer) ? playerAArquive : playerBArquive;
                break;
            case PlayerZone.Deck:
                playerHand = playerId.Equals(firstPlayer) ? playerADeck : playerBDeck;
                break;
            case PlayerZone.BackStage:
                playerHand = playerId.Equals(firstPlayer) ? playerABackPosition : playerBBackPosition;
                break;
        }

        int handPosCounter = 0;
        foreach (Card inHand in playerHand)
        {
            if (inHand.cardNumber.Equals(UsedCard.cardNumber))
            {
                if (checkZone)
                {
                    if (inHand.cardPosition.Equals(UsedCard.cardPosition))
                    {
                        handPos = handPosCounter;
                        break;
                    }
                }
                else
                {
                    handPos = handPosCounter;
                    break;
                }
            }
            handPosCounter++;
        }
        return handPos;
    }
    public void ShuffleCards(Player player, PlayerZone playerZone)
    {

        if (player.Equals(Player.TurnPlayer))
            player = currentPlayerTurn.Equals(playerA) ? Player.PlayerA : Player.PlayerB;

        if (player.Equals(Player.FirstPlayer) || player.Equals(Player.PlayerA))
        {
            switch (playerZone)
            {
                case PlayerZone.Deck:
                    ShuffleCards(playerADeck);
                    break;
                case PlayerZone.Cheer:
                    ShuffleCards(playerACardCheer);
                    break;
                case PlayerZone.Hand:
                    ShuffleCards(playerAHand);
                    break;
                case PlayerZone.Arquive:
                    ShuffleCards(playerAArquive);
                    break;
            }
        }
        else
        {
            switch (playerZone)
            {
                case PlayerZone.Deck:
                    ShuffleCards(playerBDeck);
                    break;
                case PlayerZone.Cheer:
                    ShuffleCards(playerBCardCheer);
                    break;
                case PlayerZone.Hand:
                    ShuffleCards(playerBHand);
                    break;
                case PlayerZone.Arquive:
                    ShuffleCards(playerBArquive);
                    break;
            }
        }
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
        // why i'm returning stuff here ? need to check why, doesnt make sense since this class is the owner of tha list
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
        return returnCards;
    }
    static public MatchRoom FindPlayerMatchRoom(string playerid)
    {
        for (int i = 0; i < _MatchRooms.Count; i++)
        {
            if (_MatchRooms[i].playerA.PlayerID.Equals(playerid) || _MatchRooms[i].playerB.PlayerID.Equals(playerid))
            {
                return _MatchRooms[i];
            }
        }
        return null;
    }
    static public void RemoveRoom(MatchRoom room)
    {
        _MatchRooms.Remove(room);
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
    public void ResetTimer(string playerId)
    {
        if (playerTimers.TryGetValue(playerId, out var timer))
        {
            timer.Change(turnDurationSeconds * 1000, Timeout.Infinite);
        }
    }
    public void StopTimer(string playerId)
    {
        if (playerTimers.TryRemove(playerId, out var timer))
        {
            timer.Dispose();
        }
    }
    private void TimerExpired(string playerId, Action<string> onTimeout)
    {
        StopTimer(playerId); // Clean up the timer
        onTimeout?.Invoke(playerId); // Trigger the timeout action
    }
    public string AssignCardToBackStage(List<bool> places)
    {
        Card collaborationCard = currentPlayerTurn == playerA.PlayerID ? playerACollaboration : playerBCollaboration;
        var playerBackPosition = currentPlayerTurn == playerA.PlayerID ? playerABackPosition : playerBBackPosition;

        for (int i = 0; i < places.Count; i++)
        {
            if (!places[i])
            {
                collaborationCard.cardPosition = $"BackStage{i + 1}";
                collaborationCard.suspended = true;
                playerBackPosition.Add(collaborationCard);

                if (currentPlayerTurn == playerA.PlayerID)
                    playerACollaboration = null;
                else
                    playerBCollaboration = null;

                return $"BackStage{i + 1}";
            }
        }
        return "failToAssignToBackStage";
    }
    public bool AssignEnergyToZone(DuelAction duelAction, Card stage = null, Card collab = null, List<Card> backStage = null)
    {
        //THIS FUNCTION DOESNOT MAKE VALIDATIONS IF CAN BE PLAYED OR REMOTIONS, ONLY ATTACH IF MATCH THE POSTION/NAME

        //validating usedCard
        if (duelAction.usedCard == null)
        {
            Lib.WriteConsoleMessage("usedCard is null at AssignEnergyToZoneAsync");
            return false;
        }

        duelAction.usedCard.GetCardInfo();

        if (string.IsNullOrEmpty(duelAction.usedCard.cardType))
        {
            Lib.WriteConsoleMessage("usedCard.cardType is empty at AssignEnergyToZoneAsync");
            return false;
        }

        //validating targetCard
        if (duelAction.targetCard == null)
        {
            Lib.WriteConsoleMessage("targetCard is null at AssignEnergyToZoneAsync");
            return false;
        }
        duelAction.targetCard.GetCardInfo();

        //checking if can attach
        bool hasAttached = false;
        if (duelAction.usedCard.cardType.Equals("エール"))
        {
            if (stage != null)
                if (duelAction.targetCard.cardNumber.Equals(stage.cardNumber) && duelAction.targetCard.cardPosition.Equals("Stage"))
                {
                    stage.attachedEnergy.Add(duelAction.usedCard);
                    return hasAttached = true;
                }

            if (collab != null)
                if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.targetCard.cardPosition.Equals("Collaboration"))
                {
                    collab.attachedEnergy.Add(duelAction.usedCard);
                    return hasAttached = true;
                }

            if (backStage != null)
                if (backStage.Count > 0) // Check if there are elements in the backStage list
                {
                    for (int y = 0; y < backStage.Count; y++)
                    {
                        // Check if the target card number matches the current backstage card number
                        if (duelAction.targetCard.cardNumber.Equals(backStage[y].cardNumber) &&
                            duelAction.targetCard.cardPosition.Equals(backStage[y].cardPosition))
                        {
                            backStage[y].attachedEnergy.Add(duelAction.usedCard);
                            return hasAttached = true;
                        }
                    }
                }
            // fallied to find the target to assign the energy
            Lib.WriteConsoleMessage($"Error: failled to assign the energy at {duelAction.local}.");
        }
        else
        {
            Lib.WriteConsoleMessage("Error: used card is not a cheer.");
        }
        return false;
    }
    public bool IsSwitchBlocked(string zone)
    {
        foreach (CardEffect cardEffect in ActiveEffects)
        {
            if (cardEffect.type == CardEffectType.BlockRetreat && cardEffect.zoneTarget != zone)
            {
                return true;
            }
        }
        return false;
    }
    public bool CanBeAttached(Card card, Card target)
    {

        switch (card.cardNumber)
        {
            case "hBP01-123":
                if (target.name.Equals("兎田ぺこら"))
                    return true;
                break;
            case "hBP01-122":
                if (target.name.Equals("アキ・ローゼンタール"))
                    return true;
                break;
            case "hBP01-126":
                if (target.name.Equals("尾丸ポルカ"))
                    return true;
                break;
            case "hBP01-125":
                if (target.name.Equals("小鳥遊キアラ"))
                    return true;
                break;
            case "hBP01-124":
                if (target.name.Equals("AZKi") || target.name.Equals("SorAZ"))
                    return true;
                break;
            case "hBP01-121":
            case "hBP01-120":
            case "hBP01-119":
            case "hBP01-118":
            case "hBP01-117":
            case "hBP01-115":
            case "hBP01-114":
            case "hBP01-116":
                return !AlreadyAttachToThisHolomem(card.cardNumber, card.cardPosition);
        }
        return false;
    }
    public bool CanBeAttachedToAnyInTheField(string playerid, Card usedCard)
    {
        bool IsAbleToAttach = false;
        bool ISFIRSTPLAYER = currentPlayerTurn.Equals(playerid);

        Card playerStage = ISFIRSTPLAYER ? playerAStage : playerBStage;
        IsAbleToAttach = CanBeAttached(usedCard, playerStage);
        if (IsAbleToAttach)
            return IsAbleToAttach;

        Card playerCollab = ISFIRSTPLAYER ? playerACollaboration : playerBCollaboration;
        IsAbleToAttach = CanBeAttached(usedCard, playerCollab);
        if (IsAbleToAttach)
            return IsAbleToAttach;

        List<Card> playerBackstage = ISFIRSTPLAYER ? playerABackPosition : playerBBackPosition;
        foreach (Card card in playerBackstage)
        {
            IsAbleToAttach = CanBeAttached(usedCard, card);
            if (IsAbleToAttach)
                return IsAbleToAttach;
        }
        return IsAbleToAttach;
    }
    bool AlreadyAttachToThisHolomem(string cardNumber, string cardPosition)
    {
        bool ISFIRSTPLAYER = currentPlayerTurn == firstPlayer;

        List<Card> backStage = ISFIRSTPLAYER ? playerABackPosition : playerBBackPosition;
        Card stage = ISFIRSTPLAYER ? playerAStage : playerBStage;
        Card collab = ISFIRSTPLAYER ? playerACollaboration : playerBCollaboration;

        if (cardPosition.Equals("Stage"))
        {
            foreach (Card card in stage.attachedEquipe)
            {
                if (card.cardNumber.Equals(cardNumber))
                    return true;
            }
        }
        else if (cardPosition.Equals("Collaboration"))
        {
            foreach (Card card in collab.attachedEquipe)
            {
                if (card.cardNumber.Equals(cardNumber))
                    return true;
            }
        }
        else
        {
            foreach (Card cardBs in backStage)
            {
                foreach (Card card in cardBs.attachedEquipe)
                    if (card.cardNumber.Equals(cardNumber))
                        return true;
            }
        }
        return false;
    }
    public bool PayCardEffectCheerOrEquipCost(string zone, string cardNumber, bool ENERGY = true)
    {
        Card seletectedCard = null;

        switch (zone)
        {
            case "Favourite":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerAFavourite : playerBFavourite;
                break;
            case "Collaboration":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerACollaboration : playerBCollaboration;
                break;
            case "Stage":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerAStage : playerBStage;
                break;
            case "BackStage1":
            case "BackStage2":
            case "BackStage3":
            case "BackStage4":
            case "BackStage5":
                List<Card> seletectedCardList;
                seletectedCardList = currentPlayerTurn == firstPlayer ? playerABackPosition : playerBBackPosition;
                foreach (Card card in seletectedCardList)
                    if (card.cardPosition.Equals(zone))
                        seletectedCard = card;
                break;
        }

        int removePos = -1;
        int n = 0;


        List<Card> ListToDetach = null;

        if (ENERGY)
            ListToDetach = seletectedCard.attachedEnergy;
        else
            ListToDetach = seletectedCard.attachedEquipe;

        foreach (Card energy in ListToDetach)
        {
            if (energy.cardNumber.Equals(cardNumber))
            {
                removePos = n;
                break;
            }
            n++;
        }

        if (removePos > -1)
        {

            DuelAction _DisposeAction = new()
            {
                playerID = currentPlayerTurn,
                usedCard = new(ListToDetach[removePos].cardNumber, seletectedCard.cardPosition),
            };
            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = ENERGY ? "RemoveEnergyAtAndSendToArquive" : "RemoveEquipAtAndSendToArquive", requestObject = JsonSerializer.Serialize(_DisposeAction, Lib.jsonOptions) };
            RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
            PushPlayerAnswer();

            //adding the card that should be removed to the arquive, then removing from the player hand
            List<Card> tempArquive = currentPlayerTurn == firstPlayer ? playerAArquive : playerBArquive;
            tempArquive.Add(ListToDetach[removePos]);

            if (ENERGY)
                seletectedCard.attachedEnergy.RemoveAt(removePos);
            else
                seletectedCard.attachedEquipe.RemoveAt(removePos);

            return true;
        }

        return false;
    }
    public bool TransferEnergyFromCardAToTarget(Card CardA, Card Energy, DuelAction _DuelAction)
    {
        Card seletectedCard = null;

        switch (CardA.cardPosition)
        {
            case "Favourite":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerAFavourite : playerBFavourite;
                break;
            case "Collaboration":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerACollaboration : playerBCollaboration;
                break;
            case "Stage":
                seletectedCard = currentPlayerTurn == firstPlayer ? playerAStage : playerBStage;
                break;
            case "BackStage1":
            case "BackStage2":
            case "BackStage3":
            case "BackStage4":
            case "BackStage5":
                List<Card> seletectedCardList;
                seletectedCardList = currentPlayerTurn == firstPlayer ? playerABackPosition : playerBBackPosition;
                foreach (Card card in seletectedCardList)
                    if (card.cardPosition.Equals(CardA.cardPosition))
                        seletectedCard = card;
                break;
        }

        int removePos = -1;
        int n = 0;
        foreach (Card energy in seletectedCard.attachedEnergy)
        {
            if (energy.cardNumber.Equals(Energy.cardNumber))
            {
                removePos = n;
                break;
            }
            n++;
        }

        if (removePos > -1)
        {
            DuelAction _DisposeAction = new()
            {
                playerID = GetOtherPlayer(this, currentPlayerTurn),
                usedCard = new(seletectedCard.attachedEnergy[removePos].cardNumber, seletectedCard.cardPosition),
            };

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "RemoveEnergyAtAndDestroy", requestObject = JsonSerializer.Serialize(_DisposeAction, Lib.jsonOptions) };
            RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
            PushPlayerAnswer();

            //lets change duelaction just so the assign energy can work, maybe this need to be reworked to not use duelaction
            _DuelAction.usedCard = Energy;

            bool hasAttached = false;
            if (currentPlayerTurn == playerA.PlayerID)
                hasAttached = AssignEnergyToZone(_DuelAction,  playerAStage, playerACollaboration, playerABackPosition);
            else
                hasAttached = AssignEnergyToZone(_DuelAction, playerBStage, playerBCollaboration, playerBBackPosition);

            seletectedCard.attachedEnergy.RemoveAt(removePos);

            if (!hasAttached)
                return false;

            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };
            RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
            PushPlayerAnswer();
            return true;
        }
        return false;
    }
    public int GetDiceNumber(string actingPlayer, int min = 1, int max = 7, int Amount = 1)
    {
        Random random = new Random();
        int randomNumber = 0;
        List<int> diceRollList = actingPlayer.Equals(firstPlayer) ? playerADiceRollList : playerBDiceRollList;
        int diceRollCount = actingPlayer.Equals(firstPlayer) ? playerADiceRollCount : playerBDiceRollCount;

        randomNumber = random.Next(min, max);

        CardEffect toRemove = null;

        for (int j = 0; j < Amount; j++)
        {

            int m = -1;
            int n = 0;

            foreach (CardEffect cardEffect in ActiveEffects)
            {
                if (cardEffect.type == CardEffectType.FixedDiceRoll && actingPlayer.Equals(cardEffect.playerWhoUsedTheEffect))
                {
                    randomNumber = cardEffect.diceRollValue;
                }
                else if (cardEffect.type == CardEffectType.OneUseFixedDiceRoll && actingPlayer.Equals(cardEffect.playerWhoUsedTheEffect))
                {
                    randomNumber = cardEffect.diceRollValue;
                    m = n;
                }
                n++;
            }

            if (m != -1)
                ActiveEffects.RemoveAt(m);

            diceRollList.Add(randomNumber);
            diceRollCount++;
        }

        return randomNumber;
    }
    public void SendDiceRoll(List<int> diceValue, bool COUNTFORRESONSE)
    {
        DuelAction response = new() { actionObject = JsonSerializer.Serialize(diceValue, Lib.jsonOptions) };
        PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = COUNTFORRESONSE ? "RollDice" : "OnlyDiceRoll", requestObject = JsonSerializer.Serialize(response, Lib.jsonOptions) };
        RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
        PushPlayerAnswer();
    }
    public void RecoveryHP(bool STAGE, bool COLLAB, bool BACKSTAGE, int RecoveryAmount, string targetPlayerID, string cardPosition = "")
    {
        // Determine which player is the target
        bool isFirstPlayer = firstPlayer.Equals(targetPlayerID);

        if (STAGE)
        {
            var targetStage = isFirstPlayer ? playerAStage : playerBStage;
            targetStage.currentHp = Math.Min(targetStage.currentHp + RecoveryAmount, int.Parse(targetStage.hp));

            DuelAction da = new()
            {
                actionObject = RecoveryAmount.ToString(),
                playerID = targetPlayerID,
                targetCard = targetStage
            };
            PlayerRequest pReturnData = new PlayerRequest
            {
                type = "DuelUpdate",
                description = "RecoverHolomem",
                requestObject = JsonSerializer.Serialize(da, Lib.jsonOptions)
            };

            RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
            PushPlayerAnswer();
        }

        if (COLLAB)
        {
            var targetCollab = isFirstPlayer ? playerACollaboration : playerBCollaboration;
            targetCollab.currentHp = Math.Min(targetCollab.currentHp + RecoveryAmount, int.Parse(targetCollab.hp));

            DuelAction da = new()
            {
                actionObject = RecoveryAmount.ToString(),
                playerID = targetPlayerID,
                targetCard = targetCollab
            };
            PlayerRequest pReturnData = new PlayerRequest
            {
                type = "DuelUpdate",
                description = "RecoverHolomem",
                requestObject = JsonSerializer.Serialize(da, Lib.jsonOptions)
            };
            RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
            PushPlayerAnswer();
        }

        if (BACKSTAGE)
        {
            var targetBackPositions = isFirstPlayer ? playerABackPosition : playerBBackPosition;
            foreach (var position in targetBackPositions)
            {
                if (!string.IsNullOrEmpty(cardPosition))
                    if (cardPosition.Equals(position.cardPosition))

                        position.currentHp = Math.Min(position.currentHp + RecoveryAmount, int.Parse(position.hp));

                DuelAction da = new()
                {
                    actionObject = RecoveryAmount.ToString(),
                    playerID = targetPlayerID,
                    targetCard = position
                };
                PlayerRequest pReturnData = new PlayerRequest
                {
                    type = "DuelUpdate",
                    description = "RecoverHolomem",
                    requestObject = JsonSerializer.Serialize(da, Lib.jsonOptions)
                };
                RecordPlayerRequest(ReplicatePlayerRequestForOtherPlayers(GetPlayers(), playerRequest: pReturnData));
                PushPlayerAnswer();
            }
        }
    }
    public void RecoveryHP(DuelAction duelaction, int RecoveryAmount)
    {
        if (duelaction.targetCard.cardPosition.Equals("Stage"))
            RecoveryHP( STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount, targetPlayerID: currentPlayerTurn);
        else if (duelaction.targetCard.cardPosition.Equals("Collaboration"))
            RecoveryHP( STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount, targetPlayerID: currentPlayerTurn);
        else
            RecoveryHP( STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount, targetPlayerID: currentPlayerTurn, duelaction.targetCard.cardPosition);
    }
    public Tuple<List<Card>, List<Card>> CheckForSelectionInTempHandAndReorder(List<string> selectedByTheUser, int pickedLimit, string shouldUseToCompareWithTempHand = "") {

        List<Card> TempHand = currentPlayerTurn == playerA.PlayerID ? playerATempHand : playerBTempHand;
        List<Card> playerHand = currentPlayerTurn == playerA.PlayerID ? playerAHand : playerBTempHand;

        List<Card> ReturnToDeck = null;
        List<Card> AddToHand = null;

        int pickedCount = 0;

        for (int i = 0; i < TempHand.Count(); i++)
        {
            string comparatingValue = "";
            TempHand[i].GetCardInfo();

            if (shouldUseToCompareWithTempHand.Equals("name"))
                comparatingValue = TempHand[i].name;
            else if (shouldUseToCompareWithTempHand.Equals("number"))
                comparatingValue = TempHand[i].cardNumber;

            bool addToDeck = false;
            foreach (string card in selectedByTheUser)
            {
                if (AddToHand.Any(item => item.cardNumber == comparatingValue) ||
                    ReturnToDeck.Any(item => item.cardNumber == comparatingValue))
                {
                    continue;
                }

                if (comparatingValue.Equals(card) && pickedCount < pickedLimit)
                {
                    if (AddToHand == null)
                        AddToHand = new() { TempHand[i] };
                    else
                        AddToHand.Add(TempHand[i]);

                    pickedCount++;
                    addToDeck = true;
                    continue;
                }
            }
            if (!addToDeck)
            {
                if (ReturnToDeck == null)
                    ReturnToDeck = new() { TempHand[i] };
                else
                    ReturnToDeck.Add(TempHand[i]);
            }
            addToDeck = false;
        }
        return Tuple.Create(AddToHand, ReturnToDeck);
    }
    public DuelAction PlayHolomemToBackStage(string playerid, string cardToSummom)
    {
        List<Card> backPosition = playerid.Equals(firstPlayer) ? playerABackPosition : playerBBackPosition;

        string local = "BackStage1";
        for (int j = 0; j < backPosition.Count; j++)
        {
            if (!backPosition[j].cardPosition.Equals("BackStage1"))
            {
                local = "BackStage1";
            }
            else if (!backPosition[j].cardPosition.Equals("BackStage2"))
            {
                local = "BackStage2";
            }
            else if (!backPosition[j].cardPosition.Equals("BackStage3"))
            {
                local = "BackStage3";
            }
            else if (!backPosition[j].cardPosition.Equals("BackStage4"))
            {
                local = "BackStage4";
            }
            else if (!backPosition[j].cardPosition.Equals("BackStage5"))
            {
                local = "BackStage5";
            }
        }
        DuelAction _DuelActio = new()
        {
            usedCard = new Card(cardToSummom, local),
            playedFrom = "Deck",
            local = local,
            playerID = playerid,
            suffle = true
        };

        backPosition.Add(_DuelActio.usedCard);
        return _DuelActio;
    }
    public DuelAction ReturnCollabToBackStage()
    {
        Card currentStageCardd = currentPlayerTurn == playerA.PlayerID ? playerAStage : playerBStage;
        Card currentCollabCardd = currentPlayerTurn == playerA.PlayerID ? playerACollaboration : playerBCollaboration;
        List<Card> currentBackStageCardd = currentPlayerTurn == playerA.PlayerID ? playerABackPosition : playerBBackPosition;

        DuelAction duelAction = null;
        if (!string.IsNullOrEmpty(currentCollabCardd.cardNumber))
        {
            //try to assign the card to the back position
            List<bool> places = new Lib().GetBackStageAvailability(currentBackStageCardd);
            string locall = AssignCardToBackStage(places);
            if (locall.Equals("failToAssignToBackStage"))
            {
                Lib.WriteConsoleMessage("Error assign the card to the backposition");
                return null;
            }

            duelAction = new DuelAction
            {
                playerID = currentPlayerTurn == firstPlayer ? firstPlayer : secondPlayer,
                usedCard = currentCollabCardd,
                playedFrom = "Collaboration",
                actionType = "EffectUndoCollab"
            };
            duelAction.usedCard.cardPosition = locall;

        }
        return duelAction;
    }
    public void PrintPlayerHand()
    {
        string frase = "";
        frase += $"Player:{firstPlayer}-";
        foreach (Card card in playerAHand)
        {
            frase += $"{card.cardNumber}-";
        }
        frase += $"\nPlayer:{secondPlayer}-";
        foreach (Card card in playerBHand)
        {
            frase += $"{card.cardNumber}-";
        }
        Lib.WriteConsoleMessage(frase);
    }
}
