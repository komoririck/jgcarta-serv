using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class DrawRequestHandler
    {

        internal async Task DrawRequestHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
             Task task;

            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn) 
            {
                Lib.WriteConsoleMessage("Wrong player calling");
                return;
            }
            if (cMatchRoom.currentGamePhase != GAMEPHASE.DrawStep)
            {
                Lib.WriteConsoleMessage("Called at wrong phase");
                return;
            }


            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawPhase", requestObject = "" };
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                if (cMatchRoom.playerADeck.Count == 0)
                    _ = Lib.EndDuelAsync(cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                task = Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom, ReturnData);
            }
            else
            {
                if (cMatchRoom.playerBDeck.Count == 0)
                    _ = Lib.EndDuelAsync(cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                task = Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom, ReturnData);
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
            cMatchRoom.currentGameHigh++;
        }
    }
}