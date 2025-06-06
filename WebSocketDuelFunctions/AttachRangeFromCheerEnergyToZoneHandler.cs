﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class AttachRangeFromCheerEnergyToZoneHandler
    {
        internal async Task AttachRangeFromCheerEnergyToZoneHandleAsync(PlayerRequest playerRequest, bool stage, bool collab, bool back)
        {
            MatchRoom cMatchRoom = MatchRoom.FindPlayerMatchRoom(playerRequest.playerID);
            int energyIndex;

        DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

                _DuelAction.targetCard?.GetCardInfo();
                _DuelAction.usedCard?.GetCardInfo();
                _DuelAction.cheerCostCard?.GetCardInfo();

            //temphand
            List<Card> playertemphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            if (playertemphand.Count == 0)
            {
                Lib.WriteConsoleMessage("temp hand null -- RISK PLAY");
                return;
            }

            int x = -1;
            for (int y = 0; y < playertemphand.Count; y++) {
                if (playertemphand[y].cardNumber.Equals(_DuelAction.usedCard.cardNumber)) {
                    x = y;
                }
            }
            if (x == -1)
            {
                Lib.WriteConsoleMessage("Invalid card used");
                return;
            }

            //since pass the valition remove last pos
            playertemphand.RemoveAt(x);

            bool assinged;
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                assinged = cMatchRoom.AssignEnergyToZone(_DuelAction, (stage == true ? cMatchRoom.playerAStage : null), (collab == true ? cMatchRoom.playerACollaboration : null), (back == true ? cMatchRoom.playerABackPosition : null));
            else
                assinged = cMatchRoom.AssignEnergyToZone(_DuelAction, (stage == true ? cMatchRoom.playerBStage : null), (collab == true ? cMatchRoom.playerBCollaboration : null), (back == true ? cMatchRoom.playerBBackPosition : null));

            if (!assinged) //(x == -1)
            {
                Lib.WriteConsoleMessage("No match found to assign the energy");
                return;
            }

            //since we assign, lets return the usedcard as the card who activate the effect instead of the energy
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID) { cMatchRoom.playerATempHand.Clear(); } else { cMatchRoom.playerBTempHand.Clear(); }

            //creating the action to send to the player, we are recycing some information from what player send
            _DuelAction.playedFrom = "CardCheer";

            //lest send to player AttachEnergyResponse since is generic
            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), _ReturnData));
            cMatchRoom.PushPlayerAnswer();
        }
    }
}