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


            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card collab = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
            EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<int> diceList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;

            //switch explanation:
            //card can have more than one art, each one with its own effect and complexit
            //we use currentCardResolvingStage to identify which state of the effect "complexit" we are
            //we use currentArtResolving to check which art we are resolving for that cardnumber

            Lib.WriteConsoleMessage(cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name);

            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage + "-" + cMatchRoom.currentArtResolving.Name)
            {
                case "hBP01-062-キッケリキー！":
                    string SelectedCard = _DuelAction.actionObject;

                    int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, SelectedCard);
                    if (n > -1)
                    {
                        DuelAction duelAction = new()
                        {
                            playerID = cMatchRoom.currentPlayerTurn,
                            cardList = new() { new Card(SelectedCard) }
                        };

                        playerHand.RemoveAt(n);

                        Lib.SendPlayerData(cMatchRoom, false, duelAction, "RemoveCardsFromHand");

                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = _DuelAction.usedCard.cardPosition,
                            type = CardEffectType.BuffThisCardDamage,
                            Damage = 20,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                    }
                    ResetResolution();
                    break;
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
                        activatedTurn = cMatchRoom.currentTurn
                    };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                    
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
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
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
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
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
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                    ResetResolution();
                    break;
                case "hSD01-006-SorAZ シンパシー":
                    cMatchRoom.ActiveEffects.Add(new CardEffect()
                    {
                        artName = "SorAZ シンパシー",
                        cardNumber = "hSD01-006",
                        zoneTarget = "Stage",
                        ExistXAtZone_Name = "AZKi",
                        type = CardEffectType.BuffThisCardDamageExistXAtZone,
                        Damage = 50,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    ResetResolution();
                    break;
                case "hBP01-014-♰漆黒の翼♰":
                    Card target = null;

                    if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        target = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
                    else
                        target = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

                    int damage = ArtCalculator.CalculateTotalDamage(cMatchRoom.currentArtResolving, _DuelAction.usedCard, target, cMatchRoom.currentPlayerTurn, MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn), cMatchRoom);

                    if (-1*(damage - target.currentHp) > 50) { 
                    
                    cMatchRoom.ActiveEffects.Add(new CardEffect()
                    {
                        artName = "♰漆黒の翼♰",
                        cardNumber = "hBP01-014",
                        zoneTarget = "Stage",
                        type = CardEffectType.IncreaseLifeCostIfDamageSurpassX,
                        IncreaseCostAmount = 1,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    }
                    ResetResolution();
                    break;
                case "hBP01-027-アクセスコード：ID":
                    _CardEffect = new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZoneIfHaveTag,
                        cardTag = "#ID",
                        Damage = 50,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    };
                    cMatchRoom.ActiveEffects.Add(_CardEffect);
                    ResetResolution();
                    break;
                case "hBP01-020-みんな一緒に":
                    cMatchRoom.ActiveEffects.Add(new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZoneMultiplyByBackstageCount,
                        Damage = 10,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    cMatchRoom.ActiveEffects.Add(new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Collaboration",
                        type = CardEffectType.BuffDamageToCardAtZoneMultiplyByBackstageCount,
                        Damage = 10,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    ResetResolution();
                    break;
                case "hBP01-037-秘密の合鍵":
                    cMatchRoom.ActiveEffects.Add(new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = _DuelAction.usedCard.cardPosition,
                        type = CardEffectType.BuffDamageToCardAtZoneIfHasATool,
                        Damage = 50,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    ResetResolution();
                    break;
                case "hBP01-051-エールを束ねて":
                    cMatchRoom.ActiveEffects.Add(new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Collaboration",
                        type = CardEffectType.BuffThisCardDamageIfAtZoneAndMultplyByCheer,
                        Damage = 50,
                        cardTag = "#ID",
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn,
                        //BuffThisCardDamageIfAtZoneAndMultplyByCheer
                        nameThatShouldntExistAtZone = "アイラニ・イオフィフティーン",
                        zoneThatShouldHaveTag = "Stage"
                    });
                    ResetResolution();
                    break;
                case "hBP01-057-漆黒の翼で誘おう":
                    Card opsCollab = !(cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

                    if (opsCollab == null)
                    {
                        ResetResolution();
                        return;
                    }

                    _DuelAction = new()
                    {
                        playerID = MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                        targetCard = opsCollab,
                    };

                    opsCollab.currentHp -= 10;

                    _DuelAction.actionObject = "10";
                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                    // Serialize and send data to the current player
                    PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);
                    ResetResolution();
                    break;
                case "hBP01-071-ポルカサーカス":
                    cMatchRoom.ActiveEffects.Add(new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = _DuelAction.usedCard.cardPosition,
                        type = CardEffectType.BuffDamageToCardAtZoneMultiplyByAmountOfToolAtYourSide,
                        Damage = 20,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    });
                    break;
                case "hBP01-072-WAZZUP!!":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new() { diceValue }, COUNTFORRESONSE: false);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0721-WAZZUP!!":
                    opsCollab = !(cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

                    bool hasRedEnergy = false;

                    Card thisCard = _DuelAction.usedCard.cardPosition.Equals("Stage") ? stage : collab;

                    foreach (Card card in thisCard.attachedEnergy)
                    {
                        if (card.color.Equals("赤"))
                            hasRedEnergy = true;
                    }

                    if (opsCollab == null || !Lib.IsOddNumber(diceList.Last()) || !hasRedEnergy)
                    {
                        ResetResolution();
                        return;
                    }

                    _DuelAction = new()
                    {
                        playerID = MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                        targetCard = opsCollab,
                    };

                    opsCollab.currentHp -= 20;

                    _DuelAction.actionObject = "20";
                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                    // Serialize and send data to the current player
                    _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);
                    ResetResolution();
                    break;
                case "hBP01-035-アキロゼ幻想曲":
                    EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                    _DuelAction.targetCard = _DuelAction.usedCard;

                    target = null;
                    if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                    {
                       target = cMatchRoom.currentPlayerTurn != cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    }
                    else
                    {
                        target = cMatchRoom.currentPlayerTurn != cMatchRoom.firstPlayer ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                    }

                    bool canContinue = false;
                    foreach (Card card in target.attachedEquipe) {
                        if (card.cardType.Equals("サポート・ツール")) 
                        {
                            canContinue = true;
                        }
                    }

                    if (!canContinue)
                    {
                        ResetResolution();
                        return;
                    }

                    if (EnergyList.Count > 0)
                    {
                        //add to temp hand so we can get latter
                        tempHandList.Add(EnergyList[EnergyList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() { tempHandList[0].cardNumber };
                    }

                    var handler194 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler194.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, energyIndex: 0);

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