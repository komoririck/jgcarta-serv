using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using Org.BouncyCastle.Cms;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class CollabEffects
    {
        public static List<CardEffect> currentActivatedTurnEffect = new();
        List<CardEffect> currentContinuosTurnEffect = new();
        List<CardEffect> currentDuelLimiteEffect = new();

        internal static async Task OnCollabEffectAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, int playerWhoUsedTheEffect, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {
            CardEffect _CardEffect;
            RequestData pReturnData = new();

            List<Card> tempHandList = new();
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();
            Random random = new Random();

            cMatchRoom.currentDuelActionResolvingRecieved.Add(_DuelAction);

            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage)
            {
                case "hSD01-015":
                    Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

                    stage.GetCardInfo(stage.cardNumber);
                    if (stage.name.Equals("ときのそら"))
                    {
                        RequestData ReturnData = new RequestData { type = "GamePhase", description = "DrawCollabEffect", requestObject = "" };
                        if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                        {
                            Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                            Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom, ReturnData);
                        }
                        else
                        {
                            Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                            Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom, ReturnData);
                        }
                        ResetResolution();
                    }
                    else if (stage.name.Equals("AZKi"))
                    {
                        tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                        //add to temp hand so we can get latter
                        tempHandList.Add(holoPowerList[holoPowerList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() {tempHandList[0].cardNumber };

                        cMatchRoom.currentCardResolvingStage = "1";

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        pReturnData = new RequestData { type = "GamePhase", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);

                    }
                    else
                    {
                        ResetResolution();
                    }
                    break;
                case "hSD01-0151":
                    var handler190 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                    {
                        await handler190.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, true, false, false, 1);
                    }
                    ResetResolution();
                    break;
                case "hSD01-004":
                    _CardEffect = new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZone,
                        damageType = 20,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        listIndex = 0
                    };
                    currentActivatedTurnEffect.Add(_CardEffect);
                    ResetResolution();
                    break;
                case "hSD01-007":
                    //get the player holopowerlist
                    holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
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
                    holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                    //random dice number
                    random = new Random();
                    string randomNumber = random.Next(1, 7).ToString();
                    //send a list with the energy and the dice roll
                    returnToclient = new List<string>() { randomNumber };

                    if (int.Parse(randomNumber) > 4 || holoPowerList.Count < 1 || backPos.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }
                    if (int.Parse(randomNumber) < 5)
                    {
                        //add to temp hand so we can get latter
                        tempHandList.Add(holoPowerList[holoPowerList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() { randomNumber, tempHandList[0].cardNumber };
                    }
                    if (int.Parse(randomNumber) < 2)
                    {
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.extraInfo.Add(randomNumber);
                    }
                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new RequestData { type = "GamePhase", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0091":
                    cMatchRoom.currentCardResolvingStage = "2";
                    var handler192 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler192.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, false, false, true, 1);
                    break;
                case "hSD01-0092":
                    if (_DuelAction.actionObject.Equals("Yes") && int.Parse(cMatchRoom.extraInfo[0]) < 2)
                        if (!new Lib().ReturnCollabToBackStage(cMatchRoom))
                            Lib.WriteConsoleMessage("Fallied to finish ReturnCardToBackStage at AskAttachEnergyAndRetreat");
                    ResetResolution();
                    break;
                case "hSD01-012":
                    holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                    List<Card> listToSend = new();
                    foreach (Card card in holoPowerList)
                    {
                        card.GetCardInfo(card.cardNumber);
                        if (card.color.Equals("白") || card.color.Equals("緑"))
                            listToSend.Add(card);
                    }

                    if (listToSend.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }

                    //hold the card at resolving
                    cMatchRoom.currentCardResolvingStage = "1";
                    tempHandList.AddRange(listToSend);

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(listToSend, Lib.options);
                    pReturnData = new RequestData { type = "GamePhase", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0121":
                    if (!_DuelAction.actionObject.Equals("Yes"))
                    {
                        ResetResolution();
                        return;
                    }

                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                    var handler191 = new AttachRangeFromCheerEnergyToZoneHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler191.AttachRangeFromCheerEnergyToZoneHandleAsync(playerRequest, webSocket, true, true, true, tempHandList);
                    ResetResolution();
                    break;
                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                if (cMatchRoom.extraInfo != null)
                    cMatchRoom.extraInfo.Clear();
                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                cMatchRoom.currentDuelActionResolvingRecieved.Clear();
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
                    await handler2.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, false, false, true);
                    break;

                case "AskAttachTopCheerEnergyToBack":
                    var handler19 = new AttachTopCheerEnergyToBackHandler(playerConnections, matchRooms);
                    await handler19.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, false, false, true);
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
            OnCollabEffectAsync(_DuelAction, cMatchRoom, int.Parse(playerRequest.playerID), playerRequest, webSocket);
        }
    }
}