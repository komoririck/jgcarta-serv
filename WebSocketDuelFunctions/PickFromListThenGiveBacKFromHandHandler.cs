using hololive_oficial_cardgame_server.SerializableObjects;
using Org.BouncyCastle.Asn1.X509;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Policy;
using System.Text.Json;


// hSD01-007 ,

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class PickFromListThenGiveBacKFromHandHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private PlayerRequest pReturnData;
        private DuelAction _DuelAction = new();

        public PickFromListThenGiveBacKFromHandHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task PickFromHoloPowerThenGiveBacKFromHandHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction response = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            List<string> returnedCardList = JsonSerializer.Deserialize<List<string>>(response.actionObject);

            if (returnedCardList.Count != 2)
            {
                Lib.WriteConsoleMessage("List in the wrong format");
                return;
            }


            //checking if the selected card is valid
            List<Card> holoPowerList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
            int n = -1;
            int m = 0;
            foreach (Card card in holoPowerList)
            {
                if (card.cardNumber.Equals(returnedCardList[0]))
                {
                    n = m;
                    break;
                }
                m++;
            }

            if (n == -1)
            {
                Lib.WriteConsoleMessage("Invalid card picked from holopower");
                return;
            }

            holoPowerList.RemoveAt(n);

            //adding selected card to the hand
            List<Card> playerHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

            playerHandList.Add(new() { cardNumber = returnedCardList[0] });
            holoPowerList.Add(new() { cardNumber = returnedCardList[1] });

            //checking if the card to return to the holopower is valid
            n = -1;
            m = 0;
            foreach (Card card in playerHandList)
            {
                if (card.cardNumber.Equals(returnedCardList[1]))
                {
                    n = m;
                    break;
                }
                m++;
            }

            if (n == -1)
            {
                Lib.WriteConsoleMessage("card to return to the holo power is invalid");
                playerHandList.RemoveAt(playerHandList.Count - 1);
                return;
            }

            // if is valid we remove from hand
            playerHandList.RemoveAt(n);

            //creating data to send the player
            _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
            _DuelAction.usedCard = new() { cardNumber = cMatchRoom.currentCardResolving };
            _DuelAction.targetCard = new() { cardNumber = returnedCardList[1] };
            _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
            _DuelAction.suffle = false;
            _DuelAction.zone = "HoloPower";
            _DuelAction.cardList = new List<Card>() { new Card() { cardNumber = returnedCardList[0] } };
            _DuelAction.targetCard = new() { cardNumber = returnedCardList[1] };

            //remove card from resolving
            cMatchRoom.currentCardResolving = "";

            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PickFromListThenGiveBacKFromHandDone", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);

        }
    }
}