using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class ArtEffects
    {
        public static List<CardEffect> currentActivatedTurnEffect = new();
        List<CardEffect> currentContinuosTurnEffect = new();
        List<CardEffect> currentDuelLimiteEffect = new();

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
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();
            Random random = new Random();

            //switch explanation:
            //card can have more than one art, each one with its own effect and complexit
            //we use currentCardResolvingStage to identify which state of the effect "complexit" we are
            //we use currentArtResolving to check which art we are resolving for that cardnumber

            Lib.WriteConsoleMessage(cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name);
            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name)
            {
                case "hSD01-013-越えたい未来":
                    //hold the card at resolving
                    cMatchRoom.extraInfo = new List<string>();
                    //get the player holopowerlist
                    holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                    //random dice number
                    string randomNumberS = random.Next(1, 7).ToString();
                    //send a list with the energy and the dice roll
                    returnToclient = new List<string>() { randomNumberS };

                    cMatchRoom.extraInfo.Add(JsonSerializer.Serialize(_DuelAction, Lib.options));

                    if (int.Parse(randomNumberS) == 1 || int.Parse(randomNumberS) == 3 || int.Parse(randomNumberS) == 5)
                    {
                        //add to temp hand so we can get latter
                        tempHandList.Add(holoPowerList[holoPowerList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() { randomNumberS, tempHandList[0].cardNumber };

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.extraInfo.Add(randomNumberS);

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        pReturnData = new PlayerRequest { type = "GamePhase", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    }
                    else
                    {
                        PlayerRequest ReturnData = new PlayerRequest { type = "GamePhase", description = "DrawArtEffect", requestObject = "" };
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
                    break;
                case "hSD01-0131-越えたい未来":
                    var handler190 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);

                    if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                    {
                        await handler190.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, true, false, false, 1);
                    }
                    else
                    {
                        await handler190.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, false, true, false, 1);
                    }
                    ResetResolution();
                    break;
                case "hSD01-011-SorAZ グラビティ":
                    //get the card at stage and see it's a tokino sora
                    Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    stage.GetCardInfo(stage.cardNumber);
                    if (!stage.name.Equals("ときのそら"))
                        return;
                    //get the cheer and see if has more than 0 to be able to assign
                    List<Card> cheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    if (cheerDeck.Count < 1)
                        return;

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
                    pReturnData = new PlayerRequest { type = "GamePhase", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0111-SorAZ グラビティ":
                    var handler19 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler19.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, true, true, true, 0);
                    ResetResolution();
                    break;
                case "hSD01-011-デスティニーソング":
                    //random dice number
                    int randomNumber = random.Next(1, 7);

                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], new PlayerRequest { type = "GamePhase", description = "RollDice", requestObject = randomNumber.ToString() });
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], new PlayerRequest { type = "GamePhase", description = "RollDice", requestObject = randomNumber.ToString() });

                    if (randomNumber == 1 || randomNumber == 3 || randomNumber == 5)
                    {
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = JsonSerializer.Deserialize<DuelAction>(cMatchRoom.extraInfo[0]).usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = 50,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        currentActivatedTurnEffect.Add(_CardEffect);
                    }
                    ResetResolution();
                    break;


                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                resolveDamage(cMatchRoom);
                if (cMatchRoom.extraInfo != null)
                    cMatchRoom.extraInfo.Clear();

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

            //same validation as to use art
            Card currentCollabOponnentCard = null;
            Card currentOponnentCard = null;
            Card currentStageOponnentCard = null;


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

            validCard = false;

            currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
            currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

            if (currentStageOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentStageOponnentCard;
                validCard = true;
            }
            else
            if (currentCollabOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentCollabOponnentCard;
                validCard = true;
            }


            if (!validCard)
                return;
            //end of use art validation

            List<Card> attachedCards;
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                attachedCards = _DuelAction.usedCard.cardPosition.Equals("Stage") ? cMatchRoom.playerAStage.attachedEnergy : cMatchRoom.playerACollaboration.attachedEnergy;
            else
                attachedCards = _DuelAction.usedCard.cardPosition.Equals("Stage") ? cMatchRoom.playerBStage.attachedEnergy : cMatchRoom.playerBCollaboration.attachedEnergy;



            if (attachedCards.Count == 0)
                return;

            string currentPlayer = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
            cMatchRoom.currentArtDamage = ArtCalculator.CalculateTotalDamage(cMatchRoom.currentArtResolving, attachedCards, _DuelAction.targetCard.color, _DuelAction.usedCard, _DuelAction.targetCard, currentPlayer, MatchRoom.GetOtherPlayer(cMatchRoom, currentPlayer), cMatchRoom);

            if (cMatchRoom.currentArtDamage < -10000)
            {
                Lib.WriteConsoleMessage("no suficient energy attached");
                return;
            }

            int damage = cMatchRoom.currentArtDamage;

            currentOponnentCard.currentHp -= damage;
            currentOponnentCard.normalDamageRecieved += damage;
            currentOponnentCard.GetCardInfo(currentOponnentCard.cardNumber);


            if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                cMatchRoom.centerStageArtUsed = true;
            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                cMatchRoom.collabStageArtUsed = true;

            if (int.Parse(currentOponnentCard.hp) <= -1 * currentOponnentCard.currentHp)
            {
                List<Card> arquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;

                Lib.DefeatedHoloMemberAsync(arquive, currentOponnentCard, cMatchRoom, true, _DuelAction);
            }
            else
            {
                _DuelAction.playerID = cMatchRoom.currentPlayerTurn.ToString();
                _DuelAction.actionObject = damage.ToString();
                _DuelAction.actionType = "UseArt";

                var pReturnData = new PlayerRequest { type = "GamePhase", description = "UsedArt", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
            }
        }
    }
}