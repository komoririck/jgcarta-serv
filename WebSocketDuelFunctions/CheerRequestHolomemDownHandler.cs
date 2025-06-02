using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerRequestHolomemDownHandler
    {
        internal async Task CheerRequestHolomemDownHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            string otherPlayer = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn);

            if (!playerRequest.playerID.Equals(otherPlayer))
            {
                Lib.WriteConsoleMessage("wrong player calling");
                return;
            }

            if (cMatchRoom.currentGamePhase != GAMEPHASE.HolomemDefeated)
            {
                Lib.WriteConsoleMessage("not right phase");
                return;
            }

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;
            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerBHand : cMatchRoom.playerAHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerBCardCheer : cMatchRoom.playerACardCheer;
            List<Card> playerLife = ISFIRSTPLAYER ? cMatchRoom.playerBLife : cMatchRoom.playerALife;

            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "HolomemDefatedSoGainCheer", requestObject = "" };
            DuelAction cardCheerDraw = new DuelAction() { playerID = otherPlayer, zone = "Life" };

            if (playerCheer.Count != 0)
            {
                Lib.MoveTopCardFromXToY(playerLife, playerHand, cMatchRoom.cheersAssignedThisChainTotal);
                cardCheerDraw.cardList = playerHand.Skip(Math.Max(0, playerHand.Count - cMatchRoom.cheersAssignedThisChainTotal)).ToList();
                ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.jsonOptions);

                cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(otherPlayer), hidden: true, duelAction: cardCheerDraw, type: "DuelUpdate", description: "HolomemDefatedSoGainCheer"));
                cMatchRoom.PushPlayerAnswer();
            }
            else
            {
                cardCheerDraw.cardList = new List<Card>() { new Card("Empty") };
                ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.jsonOptions);

                cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), ReturnData));
                cMatchRoom.PushPlayerAnswer();
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeatedCheerChoose;
            cMatchRoom.currentGameHigh++;


        }
    }
}