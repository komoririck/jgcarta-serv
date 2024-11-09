﻿using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;

namespace hololive_oficial_cardgame_server
{
    internal class AttachEquipamentToHolomemHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public AttachEquipamentToHolomemHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task AttachEquipamentToHolomemHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);


            //validating usedCard
            if (_DuelAction.usedCard == null)
            {
                Lib.WriteConsoleMessage("Invalid used card");
                return;
            }

            _DuelAction.usedCard.GetCardInfo();

            if (!(_DuelAction.usedCard.cardType.Equals("サポート・ツール") || _DuelAction.usedCard.cardType.Equals("サポート・マスコット") || _DuelAction.usedCard.cardType.Equals("サポート・ファン")))
            {
                Lib.WriteConsoleMessage("Not a equip card");
                return;
            }

            //validating targetCard
            if (_DuelAction.targetCard == null)
            {
                Lib.WriteConsoleMessage("targetCard is null ");
                return;
            }
            _DuelAction.targetCard.GetCardInfo();


            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> backStage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            Card stage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card collab = ISFIRSTPLAYER ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
            

            //checking if can attach
            bool hasAttached = false;

            if (stage != null)
                if (_DuelAction.targetCard.cardNumber.Equals(stage.cardNumber) && _DuelAction.targetCard.cardPosition.Equals("Stage"))
                {
                    stage.attachedEquipe.Add(_DuelAction.usedCard);
                    hasAttached = true;
                }

            if (collab != null)
                if (_DuelAction.targetCard.cardNumber.Equals(collab.cardNumber) && _DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                {
                    collab.attachedEquipe.Add(_DuelAction.usedCard);
                    hasAttached = true;
                }

            if (backStage != null)
                if (backStage.Count > 0) // Check if there are elements in the backStage list
                {
                    for (int y = 0; y < backStage.Count; y++)
                    {
                        // Check if the target card number matches the current backstage card number
                        if (_DuelAction.targetCard.cardNumber.Equals(backStage[y].cardNumber) &&
                            _DuelAction.targetCard.cardPosition.Equals(backStage[y].cardPosition))
                        {
                            backStage[y].attachedEquipe.Add(_DuelAction.usedCard);
                            hasAttached = true;
                        }
                    }
                }

            if (!hasAttached) { 
                Lib.WriteConsoleMessage($"Error: failled to equipe at {_DuelAction.targetCard.cardPosition}.");
                return;
            }

            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachSupportItem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer], _ReturnData);

        }
    }
}