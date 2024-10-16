using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static hololive_oficial_cardgame_server.MatchRoom;

namespace hololive_oficial_cardgame_server
{
    public class MessageDispatcher
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
            RequestData _ReturnData;


            Lib.WriteConsoleMessag("Recieved from " + playerRequest.playerID + ":\n" + playerRequest.requestData.type + ":\n" + playerRequest.requestData.requestObject + ":\n" + playerRequest.requestData.extraRequestObject + ":\n");
            switch (playerRequest.requestData.type)
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
                    if (!(int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn))
                        break;

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" });
                    break;
                case "MainStartRequest":
                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                        break;

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" });
                    break;
                case "MainDoActionRequest":

                    var handler9 = new MainDoActionRequestHandler(playerConnections, _MatchRooms);
                    await handler9.MainDoActionRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "MainPerformanceRequest":
                    cMatchRoom.currentGamePhase = GAMEPHASE.PerformanceStep;
                    break;
                case "MainUseArtRequest":
                    cMatchRoom.currentGamePhase = GAMEPHASE.UseArt;
                    break;
                case "MainEndturnRequest":
                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                        break;

                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                        cMatchRoom.playerALimiteCardPlayed.Clear();
                    else
                        cMatchRoom.playerBLimiteCardPlayed.Clear();

                    cMatchRoom.currentPlayerTurn = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.secondPlayer : cMatchRoom.firstPlayer;
                    cMatchRoom.currentGamePhase = GAMEPHASE.ResetStep;
                    cMatchRoom.centerStageArtUsed = false;
                    cMatchRoom.collabStageArtUsed = false;

                    Lib.WriteConsoleMessag("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\ NEW TURN (\"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\ \n New player turn:" + cMatchRoom.currentPlayerTurn);

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new RequestData { type = "GamePhase", description = "Endturn", requestObject = "" });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new RequestData { type = "GamePhase", description = "Endturn", requestObject = "" });

                    cMatchRoom.currentGameHigh++;
                    break;
                case "ResetRequest":
                    var handler13 = new ResetRequestHandler(playerConnections, _MatchRooms);
                    await handler13.ResetRequestHandleAsync(playerRequest, webSocket);
                    break;
                case "SuporteEffectAttachEnergyIfResponse":
                    var handler15 = new SuporteEffectAttachEnergyIfResponseHandler(playerConnections, _MatchRooms);
                    await handler15.SuporteEffectAttachEnergyIfResponseHandleAsync(playerRequest, webSocket);
                    break;
                case "MainConditionedSummomResponse":
                    var handler16 = new MainConditionedSummomResponseHandler(playerConnections, _MatchRooms);
                    await handler16.MainConditionedSummomResponseHandleAsync(playerRequest, webSocket);
                    break;
                case "MainConditionedDrawResponse":
                    var handler17 = new MainConditionedDrawResponseHandler(playerConnections, _MatchRooms);
                    await handler17.MainConditionedDrawResponseHandleAsync(playerRequest, webSocket);
                    break;
            }
        }
    }
}