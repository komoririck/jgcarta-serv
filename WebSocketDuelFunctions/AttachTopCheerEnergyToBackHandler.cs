using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class AttachTopCheerEnergyToBackHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public AttachTopCheerEnergyToBackHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task AttachTopCheerEnergyToBackHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo(_DuelAction.cheerCostCard.cardNumber);

            List<string> objects = JsonSerializer.Deserialize<List<string>>(_DuelAction.actionObject);
            string topCheerEnergy = objects[1];

            //temphand
            List<Card> playertemphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            if (!topCheerEnergy.Equals(playertemphand[0].cardNumber) || !playertemphand[0].playedFrom.Equals("CardCheer"))
            {
                // if neither of the matches are valid, something is wrong with the play the player send the information
                Lib.WriteConsoleMessage("card didnt match temphand -- RISK PLAY");
                return;
            }

            //energy list
            List<Card> playerCheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

            if (!topCheerEnergy.Equals(playerCheerDeck[playerCheerDeck.Count - 1].cardNumber)) {
                Lib.WriteConsoleMessage("Card didnt match the last position of the cheer");
                return;
            }

            //since pass the valition remove last pos
            playerCheerDeck.RemoveAt(playerCheerDeck.Count - 1);

        
            //AssignEnergyToZoneAsync checks if the used card is the energy we're trying to attach, só we need to change here for the topCheerEnergy = _DuelAction.actionObject
            //because the client is sending the card of activate the effect as the one who used the effect
            //holding the used card
            Card tempUsedCard = _DuelAction.usedCard;
            //changing for the energy
            _DuelAction.usedCard = playertemphand[0];
            
            bool assinged;
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                assinged = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, null, null, cMatchRoom.playerABackPosition);
            else
                assinged = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, null, null, cMatchRoom.playerBBackPosition);

            if (!assinged) //(x == -1)
            {
                Lib.WriteConsoleMessage("No match found to assign the energy");
                return;
            }

            //since we assign, lets return the usedcard as the card who activate the effect instead of the energy
            //_DuelAction.usedCard = tempUsedCard;

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID) { cMatchRoom.playerATempHand.Clear(); } else { cMatchRoom.playerBTempHand.Clear(); }

            //creating the action to send to the player, we are recycing some information from what player send
            _DuelAction.playedFrom = "CardCheer";

            //lest send to player AttachEnergyResponse since is generic
            RequestData _ReturnData = new RequestData { type = "GamePhase", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);
        }
    }
}