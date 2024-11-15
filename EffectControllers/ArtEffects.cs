using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class ArtEffects
    {
        int Damage = 0;

        internal static async Task OnArtEffectResolutionAsync(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms, PlayerRequest playerRequest, WebSocket webSocket)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];



            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
            {
                cMatchRoom.currentArtResolving = null;
                cMatchRoom.currentCardResolving = "";
                Lib.WriteConsoleMessage("Wrong player calling");
                return;
            }

            //we assign the art during the attack and remove at the end of the effect, so cannot be null here
            if (cMatchRoom.currentArtResolving == null)
            {
                cMatchRoom.currentArtResolving = null;
                cMatchRoom.currentCardResolving = "";
                Lib.WriteConsoleMessage("There no card resolving");
                return;
            }

            OnArtEffectsAsync(_DuelAction, cMatchRoom, playerRequest.playerID, cMatchRoom.currentArtResolving, playerRequest, webSocket);
        }

        internal static async Task OnArtEffectsAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, string playerWhoUsedTheEffect, Art art, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {
            CardEffect _CardEffect;
            PlayerRequest pReturnData = new();

            cMatchRoom.currentDuelActionResolvingRecieved.Add(_DuelAction);

            List<Card> tempHandList = new();
            List<Card> EnergyList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();

            Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<int> diceList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;

            //switch explanation:
            //card can have more than one art, each one with its own effect and complexit
            //we use currentCardResolvingStage to identify which state of the effect "complexit" we are
            //we use currentArtResolving to check which art we are resolving for that cardnumber

            Lib.WriteConsoleMessage(cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name);

            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name)
            {
                case "hBP01-043-全人類兎化計画":
                    //random dice number
                    int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn, Amount: 3);
                    cMatchRoom.currentCardResolvingStage = "1";
                    Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0431-全人類兎化計画":
                    List<int> diceRollList = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
                    int diceRollCount = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollCount : cMatchRoom.playerBDiceRollCount;

                    int totalRoll = diceRollList.Take(diceRollCount).Sum();

                    _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = JsonSerializer.Deserialize<DuelAction>((string)cMatchRoom.extraInfo[0]).usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = 10 * totalRoll,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
                    
                    ResetResolution();
                    break;

                case "hSD01-013-越えたい未来":

                    //random dice number
                     diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                    cMatchRoom.currentCardResolvingStage = "1";
                    Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hSD01-0131-越えたい未来":
                    cMatchRoom.extraInfo.Add(JsonSerializer.Serialize(_DuelAction, Lib.options));
                    diceValue = diceList.Last();

                    if (diceValue == 1 || diceValue == 3 || diceValue == 5)
                    {
                        tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                        //add to temp hand so we can get latter
                        tempHandList.Add(EnergyList[EnergyList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player

                        var handler191 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);

                        _DuelAction.actionObject = JsonSerializer.Serialize(new List<string>() { tempHandList[0].cardNumber }, Lib.options);
                        _DuelAction.targetCard = _DuelAction.usedCard;

                        PlayerRequest _playerRequest = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        await handler191.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: false, TOPCHEERDECK: true, FULLCHEERDECK: false, energyIndex: 0);
                    }
                    else
                    {
                        PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawArtEffect", requestObject = "" };
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
                    }
                    ResetResolution();
                    break;
                case "hSD01-011-SorAZ グラビティ":
                    //get the card at stage and see it's a tokino sora
                    stage.GetCardInfo();
                    //get the cheer and see if has more than 0 to be able to assign
                    List<Card> cheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                    if (!stage.name.Equals("ときのそら") || !stage.name.Equals("SorAZ") || (cheerDeck.Count < 1))
                    {
                        ResetResolution();
                        return;
                    }

                    cMatchRoom.extraInfo.Add(JsonSerializer.Serialize(_DuelAction, Lib.options));

                    //add to temp hand so we can get latter
                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    tempHandList.Add(cheerDeck[cheerDeck.Count - 1]);
                    //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                    tempHandList[0].playedFrom = "CardCheer";
                    //setup list to send to player
                    returnToclient = new List<string>() { tempHandList[0].cardNumber };

                    cMatchRoom.currentCardResolvingStage = "1";
                    cMatchRoom.currentCardResolving = "hSD01-011";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
                    break;
                case "hSD01-0111-SorAZ グラビティ":
                    var handler19 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler19.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, true, true, true, TOPCHEERDECK: true, FULLCHEERDECK: false, energyIndex: 0);
                    ResetResolution();
                    break;
                case "hSD01-011-デスティニーソング":
                    //random dice number
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                    break;
                case "hSD01-0111-デスティニーソング":
                    diceValue = diceList.Last();

                    if (diceValue == 1 || diceValue == 3 || diceValue == 5)
                    {
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = JsonSerializer.Deserialize<DuelAction>((string)cMatchRoom.extraInfo[0]).usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = (diceValue > 1) ? 50 : 100,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
                    }
                    ResetResolution();
                    break;
                case "hBP01-038-こんぺこー！":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: false);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0381-こんぺこー！":
                    diceValue = diceList.Last();

                    if (diceValue == 2 || diceValue == 4 || diceValue == 6)
                    {
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = _DuelAction.usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = 20,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
                    }
                    ResetResolution();
                    break;
                case "hBP01-042-きｔらあああ":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: false);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0421-きｔらあああ":
                    diceValue = diceList.Last();

                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = _DuelAction.usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = (10 * diceValue),
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
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

                resolveDamage(cMatchRoom);

                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                cMatchRoom.currentArtResolving = null;
                cMatchRoom.currentArtDamage = 0;
                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                cMatchRoom.currentDuelActionResolvingRecieved.Clear();
                temphand.Clear();
            }
        }
        internal static async Task resolveDamage(MatchRoom cMatchRoom)
        {

            DuelAction _DuelAction = cMatchRoom.currentDuelActionResolvingRecieved[0];

            Card currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            bool validCard = false;

            if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                if (currentStageCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                    validCard = true;


            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                if (currentCollabCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                    validCard = true;


            if (_DuelAction.usedCard.cardPosition.Equals("Stage") && cMatchRoom.centerStageArtUsed || _DuelAction.usedCard.cardPosition.Equals("Collaboration") && cMatchRoom.collabStageArtUsed)
                validCard = false;


            if (!validCard)
                return;

            Card usedCard = null;
            Card targetCard = null;

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) {

                 usedCard = _DuelAction.usedCard.cardPosition.Equals("Stage") ? cMatchRoom.playerAStage : cMatchRoom.playerACollaboration;
                 targetCard = _DuelAction.targetCard.cardPosition.Equals("Stage") ? cMatchRoom.playerBStage : cMatchRoom.playerBCollaboration;

            }
            else {
                 usedCard = _DuelAction.usedCard.cardPosition.Equals("Stage") ? cMatchRoom.playerBStage : cMatchRoom.playerBCollaboration;
                 targetCard = _DuelAction.targetCard.cardPosition.Equals("Stage") ? cMatchRoom.playerAStage : cMatchRoom.playerACollaboration;
            }

            if (usedCard.attachedEnergy.Count == 0)
                return;

            cMatchRoom.currentArtDamage = ArtCalculator.CalculateTotalDamage(cMatchRoom.currentArtResolving, usedCard, targetCard, cMatchRoom.currentPlayerTurn, MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn), cMatchRoom);

            if (cMatchRoom.currentArtDamage < -10000)
            {
                Lib.WriteConsoleMessage("no suficient energy attached");
                return;
            }

            _DuelAction.actionObject = cMatchRoom.currentArtDamage.ToString();
            cMatchRoom.extraInfo.Add(_DuelAction);

            if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                cMatchRoom.centerStageArtUsed = true;
            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                cMatchRoom.collabStageArtUsed = true;

            OnArtUsedEffects(usedCard, cMatchRoom);

            var pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicArtDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

            cMatchRoom.currentGamePhase = MatchRoom.GAMEPHASE.ResolvingDamage;
        }
        private static void OnArtUsedEffects(Card attackingCard, MatchRoom cMatchRoom)
        {
            foreach (Card card in attackingCard.attachedEquipe)
            {
                switch (card.cardNumber)
                {
                    case "hBP01-120":
                        if (attackingCard.cardPosition.Equals("Stage"))
                        {
                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawBloomIncreaseEffect", requestObject = "" };
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
                        }
                        break;
                }
            }
        }

    }
}