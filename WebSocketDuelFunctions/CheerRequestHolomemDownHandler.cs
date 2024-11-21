using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerRequestHolomemDownHandler
    {
        private DuelAction cardCheerDraw;

        public PlayerRequest ReturnData { get; private set; }

        internal async Task CheerRequestHolomemDownHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            string otherPlayer = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn);

            if (!playerRequest.playerID.Equals(otherPlayer)) {
                Lib.WriteConsoleMessage("wrong player calling");
                return;
            }

            if (cMatchRoom.currentGamePhase != GAMEPHASE.HolomemDefeated)
            {
                Lib.WriteConsoleMessage("not right phase");
                return;
            }

            cardCheerDraw = new DuelAction();
            cardCheerDraw.playerID = otherPlayer;
            cardCheerDraw.zone = "Life";

            ReturnData = new PlayerRequest { type = "DuelUpdate", description = "HolomemDefatedSoGainCheer", requestObject = "" };

            if (otherPlayer.Equals(cMatchRoom.firstPlayer))
            {
                if (cMatchRoom.playerACardCheer.Count != 0)
                {
                    Lib.getCardFromDeck(cMatchRoom.playerALife, cMatchRoom.playerAHand, cMatchRoom.cheersAssignedThisChainTotal);
                    cardCheerDraw.cardList = cMatchRoom.playerAHand.Skip(Math.Max(0, cMatchRoom.playerAHand.Count - cMatchRoom.cheersAssignedThisChainTotal)).ToList();
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);

                    //adding empty objects so the current player can keep track of how much objects the oponent is drawing, may be 1 or 2 depending of the buzz
                    cardCheerDraw.cardList = new List<Card>();
                    for (int i = 0; i < cMatchRoom.cheersAssignedThisChainTotal; i++)
                        cardCheerDraw.cardList.Add(new Card());

                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card("Empty") };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }
            else
            {
                if (cMatchRoom.playerBCardCheer.Count != 0)
                {
                    Lib.getCardFromDeck(cMatchRoom.playerBLife, cMatchRoom.playerBHand, cMatchRoom.cheersAssignedThisChainTotal);
                    cardCheerDraw.cardList = cMatchRoom.playerBHand.Skip(Math.Max(0, cMatchRoom.playerBHand.Count - cMatchRoom.cheersAssignedThisChainTotal)).ToList();
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);

                    //adding empty objects so the current player can keep track of how much objects the oponent is drawing, may be 1 or 2 depending of the buzz
                    cardCheerDraw.cardList = new List<Card>();
                    for (int i = 0; i < cMatchRoom.cheersAssignedThisChainTotal; i++)
                        cardCheerDraw.cardList.Add(new Card());

                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card("Empty") };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeatedCheerChoose;
            cMatchRoom.currentGameHigh++;


        }
    }
}