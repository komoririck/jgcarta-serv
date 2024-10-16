using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class SuporteEffectAttachEnergyIfResponseHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private RequestData _ReturnData;
        private DuelAction _DuelAction;

        public SuporteEffectAttachEnergyIfResponseHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task SuporteEffectAttachEnergyIfResponseHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];



            string selectEnergyToAttach = JsonSerializer.Deserialize<string>(playerRequest.requestData.extraRequestObject);

            List<Card> tempHandd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

            List<Card> playerCheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

            int icounter = 0;
            int jcounter = 0;
            foreach (Card existInCheer in playerCheerDeck)
            {
                if (existInCheer.cardNumber.Equals(selectEnergyToAttach))
                {
                    jcounter = icounter;
                    break;
                }
                icounter++;
            }

            playerCheerDeck[jcounter].GetCardInfo(playerCheerDeck[jcounter].cardNumber);
            if (tempHandd[0].cardNumber.Equals("hBP01-105"))
                if (!tempHandd[2].color.Equals(playerCheerDeck[jcounter].color))
                    return;

            playerCheerDeck.RemoveAt(jcounter);

            if (tempHandd[1] == null)
                tempHandd[1] = new Card();

            //gambiarra
            tempHandd[1].cardNumber = selectEnergyToAttach;
            _DuelAction = new()
            {
                local = tempHandd[2].cardPosition,
                playerID = cMatchRoom.currentPlayerTurn,
                playedFrom = "CardCheer",
                usedCard = tempHandd[1],
                targetCard = tempHandd[2]
            };

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                Lib.GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition);
            else
                Lib.GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);


            _ReturnData = new RequestData { type = "GamePhase", description = "SuporteEffectAttachEnergyIfResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            tempHandd.Clear();

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentGameHigh++;
        }
    }
}