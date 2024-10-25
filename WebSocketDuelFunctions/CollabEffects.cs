using Org.BouncyCastle.Cms;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    class CollabEffects
    {
        public static List<CardEffect> currentActivatedTurnEffect = new();
        List<CardEffect> currentContinuosTurnEffect = new();
        List<CardEffect> currentDuelLimiteEffect = new();

        internal static async Task OnCollabEffectAsync(Card? usedCard, MatchRoom cMatchRoom, int playerWhoUsedTheEffect, DuelAction da = null, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {
            CardEffect _CardEffect;
            DuelAction draw = new();
            RequestData pReturnData = new();
            DuelAction _DuelAction = new()
            {
                usedCard = usedCard,
                playerID = playerWhoUsedTheEffect
            };

           switch (usedCard.cardNumber + cMatchRoom.currentCardResolvingStage) {
                case "hSD01-004":
                    _CardEffect = new CardEffect
                    {
                        cardNumber = usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZone,
                        damageType = 20,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        listIndex = 0
                    };
                    currentActivatedTurnEffect.Add(_CardEffect);
                    break;
                case "hSD01-007":
                    //hold the card at resolving
                    cMatchRoom.currentCardResolving = "hSD01-007";
                    //get the player holopowerlist
                    List<Card> holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(holoPowerList, Lib.options);
                    pReturnData = new RequestData { type = "GamePhase", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    cMatchRoom.currentCardResolvingStage = "1";
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0071":
                    var handler1 = new PickFromListThenGiveBacKFromHandHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler1.PickFromListThenGiveBacKFromHandHandleAsync(playerRequest, webSocket);
                    ResetResolution();
                    break;
                case "hSD01-009":
                    //hold the card at resolving
                    cMatchRoom.extraInfo = new List<string>();
                    //get the player holopowerlist
                    List<Card> energyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    List<Card> tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    List<Card> backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                    //random dice number
                    Random random = new Random();
                    string randomNumber = random.Next(1, 7).ToString(); 
                    //send a list with the energy and the dice roll
                    List<string> returnToclient = new List<string>() {randomNumber};

                    if (int.Parse(randomNumber) > 4 || energyList.Count < 1 || backPos.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }
                    if (int.Parse(randomNumber) < 5) {
                        //add to temp hand so we can get latter
                        tempHandList.Add(energyList[energyList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() {randomNumber, tempHandList[0].cardNumber};
                    }
                    if (int.Parse(randomNumber) < 2)
                    {
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.extraInfo.Add(randomNumber);
                        cMatchRoom.currentCardResolving = "hSD01-009";
                    }
                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new RequestData { type = "GamePhase", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0091":
                    cMatchRoom.currentCardResolvingStage = "2";
                    var handler19 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler19.AttachTopCheerEnergyToBackHandleAsync(playerRequest, webSocket);
                    break;
                case "hSD01-0092":
                    if (da.actionObject.Equals("Yes") && int.Parse(cMatchRoom.extraInfo[0]) < 2)
                        if (!new Lib().ReturnCollabToBackStage(cMatchRoom))
                            Lib.WriteConsoleMessage("Fallied to finish ReturnCardToBackStage at AskAttachEnergyAndRetreat");
                    ResetResolution();
                    break;
                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                cMatchRoom.extraInfo.Clear();
                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                temphand.Clear();
            }

        }
        internal static async Task OnCollabEffectResolutionAsync(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms, PlayerRequest playerRequest, WebSocket webSocket)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
            {
                Lib.WriteConsoleMessage("Wrong player calling");
                return;
            }

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
            {
                Lib.WriteConsoleMessage("not card are current resolving");
            }

            switch (_DuelAction.actionType)
            {
                case "AssignToOfCheerToBackCard":
                    if (_DuelAction.targetCard.cardPosition.Equals("Collaboration") || _DuelAction.targetCard.cardPosition.Equals("Stage"))
                        return;
                    
                    var handler2 = new AttachTopCheerEnergyToBackHandler(playerConnections, matchRooms);
                    await handler2.AttachTopCheerEnergyToBackHandleAsync(playerRequest, webSocket);
                    break;

                case "AskAttachTopCheerEnergyToBack":
                    var handler19 = new AttachTopCheerEnergyToBackHandler(playerConnections, matchRooms);
                    await handler19.AttachTopCheerEnergyToBackHandleAsync(playerRequest, webSocket);
                    break;
                /*case "Retreat":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                    //retrating the card
                    if (_DuelAction.actionObject.Equals("Yes"))
                        if (!new Lib().ReturnCollabToBackStage(cMatchRoom))
                            Lib.WriteConsoleMessage("Fallied to finish ReturnCardToBackStage at AskAttachEnergyAndRetreat");
                    break;
                */
            }
            OnCollabEffectAsync(new Card() { cardNumber = cMatchRoom.currentCardResolving }, cMatchRoom, int.Parse(playerRequest.playerID), _DuelAction,  playerRequest,  webSocket);
        }
    }
}