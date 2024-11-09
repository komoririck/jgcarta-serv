using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResolveArtDamageHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public ResolveArtDamageHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task ResolveArtDamageHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            DuelAction LastDuelAction = (DuelAction)cMatchRoom.extraInfo[0];

            Card currentOponnentCard = null;

            Card currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
            Card currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

            bool validCard = false;
            if (currentStageOponnentCard.cardNumber.Equals(LastDuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentStageOponnentCard;
                validCard = true;
            }
            else
            if (currentCollabOponnentCard.cardNumber.Equals(LastDuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentCollabOponnentCard;
                validCard = true;
            }

            if (!validCard)
                return;

            if (_DuelAction.actionObject.Equals("false"))
            {

                int damage = cMatchRoom.currentArtDamage;

                currentOponnentCard.currentHp -= damage;
                currentOponnentCard.normalDamageRecieved += damage;
                currentOponnentCard.GetCardInfo();

                if (int.Parse(currentOponnentCard.hp) <= -1 * currentOponnentCard.currentHp)
                {
                    List<Card> arquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                    Lib.DefeatedHoloMemberAsync(arquive, currentOponnentCard, cMatchRoom, true, LastDuelAction);
                }
                else
                {
                    var pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveDamageToHolomem", requestObject = JsonSerializer.Serialize(LastDuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                }
                cMatchRoom.extraInfo.Clear();
            }
        }
    }
}