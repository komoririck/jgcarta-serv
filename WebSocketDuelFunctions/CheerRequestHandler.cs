using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerRequestHandler
    {
        internal async Task CheerRequestHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStep)
               return;

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;
            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

            DuelAction cardCheerDrawX = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, zone = "CardCheer" };

            if (playerCheer.Count != 0)
            {
                Lib.MoveTopCardFromXToY(playerCheer, playerHand, 1);
                cardCheerDrawX.cardList = new List<Card>() { playerHand[playerHand.Count - 1] };

                cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: cardCheerDrawX, type : "DuelUpdate", description : "CheerStep"));
                cMatchRoom.PushPlayerAnswer();
            }
            else
            {
                cardCheerDrawX.cardList = new List<Card>() { new Card("Empty") };
                PlayerRequest ReturnData = new PlayerRequest() { type = "DuelUpdate", description = "CheerStep", requestObject = JsonSerializer.Serialize(cardCheerDrawX, Lib.jsonOptions) };

                cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), ReturnData));
                cMatchRoom.PushPlayerAnswer();
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoose;
            cMatchRoom.currentGameHigh++;


        }
    }
}