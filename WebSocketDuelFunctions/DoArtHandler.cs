using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class DoArtHandler
    {
        internal async Task DoArtHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            switch (_DuelAction.usedCard.cardNumber)
            {
                case "hBP01-009":
                    if (!_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        return;
                    break;
            }

            Card currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            bool validCard = false;

            if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                if (currentStageCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber)) 
                { 
                    cMatchRoom.DeclaringAttackCard = currentStageCard;
                    validCard = true;
                }


            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                if (currentCollabCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                {
                    cMatchRoom.DeclaringAttackCard = currentCollabCard;
                    validCard = true;
                }


            if ((_DuelAction.usedCard.cardPosition.Equals("Stage") && cMatchRoom.centerStageArtUsed) || (_DuelAction.usedCard.cardPosition.Equals("Collaboration") && cMatchRoom.collabStageArtUsed))
                validCard = false;

            if (!validCard)
                return;

            validCard = false;

            Card currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
            Card currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

            if (currentStageOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
            {
                cMatchRoom.BeingTargetedForAttackCard = currentStageOponnentCard;
                validCard = true;
            }
            else if (currentCollabOponnentCard != null)
            {
                if (currentCollabOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
                {
                    cMatchRoom.BeingTargetedForAttackCard = currentCollabOponnentCard;
                    validCard = true;
                }
            }

            if (!validCard)
                return;

            foreach (Art art in _DuelAction.usedCard.Arts)
            {
                if (art.Name.Equals(_DuelAction.selectedSkill))
                {
                    cMatchRoom.ResolvingArt = art;
                    break;
                }
            }

            if (cMatchRoom.DeclaringAttackCard.cardPosition.Equals("Stage"))
                cMatchRoom.centerStageArtUsed = true;

            if (cMatchRoom.DeclaringAttackCard.cardPosition.Equals("Collaboration"))
                cMatchRoom.collabStageArtUsed = true;

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ActiveArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), pReturnData));
            cMatchRoom.PushPlayerAnswer();

            cMatchRoom.currentGameHigh++;
            return;
        }
    }
}