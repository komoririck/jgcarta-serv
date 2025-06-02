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

            string playerId = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;

            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;


            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawPhase", requestObject = "" };
            Lib.MoveTopCardFromXToY(playerDeck, playerHand, 1);

            DuelAction newDraw = new DuelAction().SetID(playerId).DrawTopCardFromXToY(playerHand, "Deck", 1);
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(playerId), hidden:true , playerRequest: ReturnData, duelAction: newDraw));
            cMatchRoom.PushPlayerAnswer();

            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
            cMatchRoom.currentGameHigh++;
        }
    }
}