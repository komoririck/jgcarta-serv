using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server
{
    public class MessageDispatcher : Lib
    {
        // WebSockets
        public static ConcurrentDictionary<string, WebSocket> playerConnections = new ConcurrentDictionary<string, WebSocket>();

        //Players DuelRoom
        public static List<MatchRoom> _MatchRooms = new List<MatchRoom>();

        //list containing all the cards in the system
        public static List<Record> CardList = FileReader.ReadFile("CardList.xlsx");

        public static async Task DispatchMessage(PlayerRequest playerRequest, WebSocket webSocket)
        {
            MatchRoom cMatchRoom = null;
            int matchnumber = MatchRoom.FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
            if (matchnumber != -1)
                cMatchRoom = _MatchRooms[matchnumber];
            PlayerRequest _ReturnData;


            Lib.WriteConsoleMessage("Recieved from " + playerRequest.playerID + ":\n" + playerRequest.type + ":\n" + playerRequest.requestObject + ":\n");
            switch (playerRequest.type)
            {
                case "JoinPlayerQueueList":
                    var handler = new JoinPlayerQueueListHandler(playerConnections, _MatchRooms);
                    await handler.JoinPlayerQueueListHandleAsync(playerRequest, webSocket);
                    break;
                case "AskForMulligan":
                    var handler1 = new AskForMulliganHandler(playerConnections, _MatchRooms);
                    await handler1.AskForMulliganHandleAsync(playerRequest, webSocket);
                    break;
                case "DuelFieldReady":
                    var handler2 = new DuelFieldReadyHandler(playerConnections, _MatchRooms);
                    await handler2.DuelFieldReadyHandleAsync(playerRequest, webSocket);
                    break;
                case "DrawRequest":
                    var handler3 = new DrawRequestHandler(playerConnections, _MatchRooms);
                    await handler3.DrawRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "CheerRequest":
                    var handler4 = new CheerRequestHandler(playerConnections, _MatchRooms);
                    await handler4.CheerRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "CheerRequestHolomemDown":
                    var handler5 = new CheerRequestHolomemDownHandler(playerConnections, _MatchRooms);
                    await handler5.CheerRequestHolomemDownHandleAsync(playerRequest, webSocket);
                    break;
                case "CheerChooseRequest":
                    var handler6 = new CheerChooseRequestHandler(playerConnections, _MatchRooms);
                    await handler6.CheerChooseRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "CheerChooseRequestHolomemDown":
                    var handler66 = new CheerChooseRequestHolomemDownHandler(playerConnections, _MatchRooms);
                    await handler66.CheerChooseRequestHolomemDownHandleAsync(playerRequest, webSocket);
                    break;
                case "ContinueCurrentPlayerTurn":
                    if (!(playerRequest.playerID != cMatchRoom.currentPlayerTurn))
                        break;

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "MainPhase", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "MainPhase", requestObject = "" });
                    break;
                case "MainStartRequest":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
                        break;

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "MainPhase", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "MainPhase", requestObject = "" });
                    break;
                case "PlayHolomem":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                        return;
                    DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    var handler902 = new PlayHolomemHandler(playerConnections, _MatchRooms);
                    await handler902.MainDoActionRequestPlayHolomemHandleAsync(playerRequest, webSocket);
                    break;
                case "BloomHolomemWithEffect":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                        return;
                     _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    break;
                case "BloomHolomem":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                        return;
                     _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    var handler903 = new BloomHolomemHandler(playerConnections, _MatchRooms);
                    await handler903.MainDoActionRequestBloomHolomemHandleAsync(playerRequest, webSocket);
                    break;
                case "DoCollab":
                    var handler904 = new DoCollabHandler(playerConnections, _MatchRooms);
                    await handler904.DoCollabHandleAsync(playerRequest, webSocket);
                    break;
                case "doArt":
                    var handler901 = new MainDoActionRequestDoArtHandler(playerConnections, _MatchRooms);
                    await handler901.MainDoActionRequestDoArtHandleAsync(playerRequest, webSocket);
                    break;
                case "ResolveDamageToHolomem":
                    var handler910 = new ResolveArtDamageHandler(playerConnections, _MatchRooms);
                    await handler910.ResolveArtDamageHandleAsync(playerRequest, webSocket);
                    break;
                case "MainPerformanceRequest":
                    cMatchRoom.currentGamePhase = GAMEPHASE.PerformanceStep;
                    break;
                case "MainUseArtRequest":
                    cMatchRoom.currentGamePhase = GAMEPHASE.UseArt;
                    break;
                case "MainEndturnRequest":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
                        break;

                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                    {
                        cMatchRoom.playerALimiteCardPlayed.Clear();
                        cMatchRoom.usedOshiSkillPlayerA = false;
                        cMatchRoom.playerBUsedSupportThisTurn = true;
                    }
                    else { 
                        cMatchRoom.playerBLimiteCardPlayed.Clear();
                        cMatchRoom.usedOshiSkillPlayerB = false;
                        cMatchRoom.playerBUsedSupportThisTurn = false;
                    }

                    cMatchRoom.currentTurn++;

                    cMatchRoom.currentPlayerTurn = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.secondPlayer : cMatchRoom.firstPlayer;
                    cMatchRoom.currentGamePhase = GAMEPHASE.ResetStep;
                    cMatchRoom.centerStageArtUsed = false;
                    cMatchRoom.collabStageArtUsed = false;

                    cMatchRoom.ActiveEffects.Clear();
                    List<CardEffect> tempEffectList = new List<CardEffect>();
                    foreach (var effect in cMatchRoom.ActiveEffects) {
                        if (cMatchRoom.currentTurn < effect.activatedTurn)
                            tempEffectList.Add(effect);
                    }
                    cMatchRoom.ActiveEffects.Clear();
                    cMatchRoom.ActiveEffects = tempEffectList;

                    Lib.WriteConsoleMessage("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\ NEW TURN (\"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\ \n New player turn:" + cMatchRoom.currentPlayerTurn);
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "Endturn", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "Endturn", requestObject = "" });

                    cMatchRoom.currentGameHigh++;
                    break;
                case "ResetRequest":
                    var handler13 = new ResetRequestHandler(playerConnections, _MatchRooms);
                    await handler13.ResetRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "ReSetCardAtStage":
                    var handler905 = new ReSetCardAtStageHandler(playerConnections, _MatchRooms);
                    await handler905.MainDoActionRequestReSetCardAtStageHandleAsync(playerRequest, webSocket);
                    break;
                case "AttachEquipamentToHolomem":
                    var handler151 = new AttachEquipamentToHolomemHandler(playerConnections, _MatchRooms);
                    await handler151.AttachEquipamentToHolomemHandleAsync(playerRequest, webSocket);
             
                    break;
                case "AskAttachEnergy":
                    //call to attach energy
                    var handler15 = new SuporteEffectAttachEnergyIfResponseHandler(playerConnections, _MatchRooms);
                    await handler15.SuporteEffectAttachEnergyHandleAsync(playerRequest, webSocket);
                    break;
                case "ResolveOnCollabEffect":
                    CollabEffects.OnCollabEffectResolutionAsync(playerConnections, _MatchRooms, playerRequest, webSocket);
                    break;
                case "ResolveOnArtEffect":
                    ArtEffects.OnArtEffectResolutionAsync(playerConnections, _MatchRooms, playerRequest, webSocket);
                    break;
                case "ResolveOnSupportEffect":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    SupportEffects.OnSupportEffectsAsync(_DuelAction, cMatchRoom, playerRequest, webSocket);
                    break;
                case "ResolveOnOshiSPEffect":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    OshiEffects.OnOshiEffectsAsync(_DuelAction, cMatchRoom, playerRequest, webSocket, SPSKILL:true);
                    break;
                case "ResolveOnOshiEffect":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    OshiEffects.OnOshiEffectsAsync(_DuelAction, cMatchRoom, playerRequest, webSocket);
                    break;
                case "ResolveOnAttachEffect":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    AttachEffects.OnAttachEffectsAsync(_DuelAction, cMatchRoom, playerRequest, webSocket);
                    break;
                case "ResolveOnBloomEffect":
                    BloomEffects.OnBloomEffectResolutionAsync(playerConnections, _MatchRooms, playerRequest, webSocket);
                    break;
                case "Retreat":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

                    if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                        _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                    {
                        Lib.WriteConsoleMessage("Invalid target position");
                        return;
                    }
                    //paying the cost for retrat
                    bool energyPaid = Lib.PayCardEffectCheerOrEquipCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);
                    if (!energyPaid)
                        break;

                    //since retreated, cannot use art anymore
                    cMatchRoom.centerStageArtUsed = true;

                    if (Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.targetCard.cardPosition) || Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.usedCard.cardPosition))
                    {
                        Lib.WriteConsoleMessage("Cannot retreat by effect");
                        return;
                    }

                    Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, playerRequest.playerID, _DuelAction.targetCard);

                    _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "SwitchStageCardByRetreat", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

                    break;
                case "ResolveRerollEffect":
                    //random dice number

                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    bool canContinue = Lib.PayCardEffectCheerOrEquipCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, ENERGY: false);

                    if (canContinue)
                    {
                        List<int> diceRollList = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
                        int diceRollCount = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollCount : cMatchRoom.playerBDiceRollCount;

                        int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn, diceRollCount);
                        Lib.SendDiceRoll(cMatchRoom, diceRollList.GetRange(Math.Max(0, diceRollList.Count - 3), Math.Min(3, diceRollList.Count)), COUNTFORRESONSE: true);
                    }
                    break;


            }
        }
    }
}