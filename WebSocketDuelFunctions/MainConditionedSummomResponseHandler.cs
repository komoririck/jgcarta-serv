using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class MainConditionedSummomResponseHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private PlayerRequest _ReturnData;
        private DuelAction _DuelAction;

        public MainConditionedSummomResponseHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainConditionedSummomResponseHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelActionRecieved = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            List<object> ResponseObjList = JsonSerializer.Deserialize<List<object>>(_DuelActionRecieved.actionObject);
            string Response = (string)ResponseObjList[0];

            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            bool canContinue = false;

            int stageCount = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition.Count : cMatchRoom.playerBBackPosition.Count;

            if (stageCount > 5)
                return;

            int n = 0;
            foreach (Card deckCard in query)
            {
                if (deckCard.cardNumber.Equals(Response))
                {
                    canContinue = true;
                    break;
                }
                n++;
            }

            if (!canContinue)
                return;

            switch (cMatchRoom.currentCardResolving)
            {
                case "hBP01-104":
                    DuelAction _DuelActio = new();
                    string local = "BackStage1";
                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
                    {
                        cMatchRoom.playerADeck.RemoveAt(n);
                        cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                        for (int j = 0; j < cMatchRoom.playerABackPosition.Count; j++)
                        {
                            if (!cMatchRoom.playerABackPosition[j].cardPosition.Equals("BackStage1"))
                            {
                                local = "BackStage1";
                            }
                            else if (!cMatchRoom.playerABackPosition[j].cardPosition.Equals("BackStage2"))
                            {
                                local = "BackStage2";
                            }
                            else if (!cMatchRoom.playerABackPosition[j].cardPosition.Equals("BackStage3"))
                            {
                                local = "BackStage3";
                            }
                            else if (!cMatchRoom.playerABackPosition[j].cardPosition.Equals("BackStage4"))
                            {
                                local = "BackStage4";
                            }
                            else if (!cMatchRoom.playerABackPosition[j].cardPosition.Equals("BackStage5"))
                            {
                                local = "BackStage5";
                            }
                        }
                        cMatchRoom.playerABackPosition.Add(_DuelAction.usedCard);
                    }
                    else
                    {
                        cMatchRoom.playerBDeck.RemoveAt(n);
                        cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                        for (int j = 0; j < cMatchRoom.playerBBackPosition.Count; j++)
                        {
                            if (!cMatchRoom.playerBBackPosition[j].cardPosition.Equals("BackStage1"))
                            {
                                local = "BackStage1";
                            }
                            else if (!cMatchRoom.playerBBackPosition[j].cardPosition.Equals("BackStage2"))
                            {
                                local = "BackStage2";
                            }
                            else if (!cMatchRoom.playerBBackPosition[j].cardPosition.Equals("BackStage3"))
                            {
                                local = "BackStage3";
                            }
                            else if (!cMatchRoom.playerBBackPosition[j].cardPosition.Equals("BackStage4"))
                            {
                                local = "BackStage4";
                            }
                            else if (!cMatchRoom.playerBBackPosition[j].cardPosition.Equals("BackStage5"))
                            {
                                local = "BackStage5";
                            }
                        }

                        cMatchRoom.playerBBackPosition.Add(_DuelAction.usedCard);
                    }

                    _DuelAction.usedCard.cardNumber = Response;
                    _DuelAction.usedCard.cardPosition = local;
                    _DuelAction.playedFrom = "Deck";
                    _DuelAction.local = local;
                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                    break;
                default:
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "GamePhase", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) });
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "GamePhase", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) });
                    cMatchRoom.currentGameHigh++;
                    break;
            }
        }
    }
}