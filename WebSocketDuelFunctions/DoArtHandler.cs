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

            Card currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            Card currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
            Card currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

            //especial cases
            switch (_DuelAction.usedCard.cardNumber)
            {
                case "hBP01-009":
                    if (!_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        return;
                    break;
            }
            if (currentCollabOponnentCard.cardName.Equals("hBP01-050") && !_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
            { // GIFT: Bodyguard
                return;
            }

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

            foreach (Art art in cMatchRoom.DeclaringAttackCard.Arts)
            {
                if (art.Name.Equals(_DuelAction.selectedSkill))
                {
                    cMatchRoom.ResolvingArt = art;
                    validCard = PassSpecialDeclareAttackCondition(cMatchRoom.DeclaringAttackCard, cMatchRoom.ResolvingArt);
                    break;
                }
                validCard = false;
            }



            if (!validCard)
            {
                cMatchRoom.ResolvingArt = null;
                cMatchRoom.DeclaringAttackCard = null;
                cMatchRoom.BeingTargetedForAttackCard = null;
                return;
            }

            if (cMatchRoom.DeclaringAttackCard.cardPosition.Equals("Stage"))
                cMatchRoom.centerStageArtUsed = true;

            if (cMatchRoom.DeclaringAttackCard.cardPosition.Equals("Collaboration"))
                cMatchRoom.collabStageArtUsed = true;

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ActiveArtEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);

            cMatchRoom.currentGameHigh++;
            return;
        }

        private bool PassSpecialDeclareAttackCondition(Card card, Art currentArt)
        {
            switch (card.cardNumber + "+" + currentArt.Name)
            {
                case "hBP01-070+共依存":
                    foreach (Card _card in card.attachedEquipe)
                        if (_card.cardName.Equals("座員"))
                            return true;
                    return false;
            }
            return true;
        }
    }
}