using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Collections.Concurrent;
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
                case "MainDoActionRequest":
                    if (playerRequest.playerID != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                        return;

                    DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                    switch (_DuelAction.actionType)
                    {
                        case "doArt":
                            var handler901 = new MainDoActionRequestDoArtHandler(playerConnections, _MatchRooms);
                            await handler901.MainDoActionRequestDoArtHandleAsync(playerRequest, webSocket);
                            break;
                        case "PlayHolomem":
                            var handler902 = new PlayHolomemHandler(playerConnections, _MatchRooms);
                            await handler902.MainDoActionRequestPlayHolomemHandleAsync(playerRequest, webSocket);
                            break;
                        case "BloomHolomemWithEffect":
                        case "BloomHolomem":
                            var handler903 = new BloomHolomemHandler(playerConnections, _MatchRooms);
                            await handler903.MainDoActionRequestBloomHolomemHandleAsync(playerRequest, webSocket);
                            break;
                        case "DoCollab":
                            var handler904 = new DoCollabHandler(playerConnections, _MatchRooms);
                            await handler904.DoCollabHandleAsync(playerRequest, webSocket);
                            break;
                        case "UseSuportStaffMember":
                            var handler906 = new UseSuportStaffMemberHandler(playerConnections, _MatchRooms);
                            await handler906.MainDoActionRequestUseSuportStaffMemberHandleAsync(playerRequest, webSocket);
                            break;
                    }
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
                        cMatchRoom.playerALimiteCardPlayed.Clear();
                    else
                        cMatchRoom.playerBLimiteCardPlayed.Clear();

                    cMatchRoom.currentPlayerTurn = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.secondPlayer : cMatchRoom.firstPlayer;
                    cMatchRoom.currentGamePhase = GAMEPHASE.ResetStep;
                    cMatchRoom.centerStageArtUsed = false;
                    cMatchRoom.collabStageArtUsed = false;

                    CollabEffects.currentActivatedTurnEffect.Clear();

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
                    
            }
        }
    }
}