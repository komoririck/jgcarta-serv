using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResolveArtDamageHandler
    {
        internal async Task ResolveArtDamageHandleAsync(PlayerRequest playerRequest)
        {
            MatchRoom cMatchRoom = MatchRoom.FindPlayerMatchRoom(playerRequest.playerID);

            if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
                cMatchRoom.playerAResolveConfirmation = true;
            else
                cMatchRoom.playerBResolveConfirmation = true;

            if (!(cMatchRoom.playerBResolveConfirmation && cMatchRoom.playerAResolveConfirmation))
                return;

            cMatchRoom.BeingTargetedForAttackCard.GetCardInfo();

            ResolveOnDamageStepEffects(cMatchRoom);

            cMatchRoom.BeingTargetedForAttackCard.currentHp -= cMatchRoom.currentArtDamage;
            cMatchRoom.BeingTargetedForAttackCard.currentHp -= cMatchRoom.currentEffectDamage;
            cMatchRoom.BeingTargetedForAttackCard.normalDamageRecieved += cMatchRoom.currentArtDamage;
            cMatchRoom.BeingTargetedForAttackCard.effectDamageRecieved += cMatchRoom.currentEffectDamage;

            if (int.Parse(cMatchRoom.BeingTargetedForAttackCard.hp) + cMatchRoom.BeingTargetedForAttackCard.currentHp < 1)
            {
                List<Card> arquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                Lib.DefeatedHoloMemberAsync(arquive, cMatchRoom.BeingTargetedForAttackCard, cMatchRoom, cMatchRoom.currentPlayerTurn);
            }
            else
            {
                DuelAction _DuelAction = new() { playerID = cMatchRoom.currentPlayerTurn, targetCard = cMatchRoom.BeingTargetedForAttackCard, actionObject = cMatchRoom.currentArtDamage.ToString() ?? cMatchRoom.currentEffectDamage.ToString() };
                var pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
            }

            //reset information
            cMatchRoom.playerAResolveConfirmation = false;
            cMatchRoom.playerBResolveConfirmation = false;

            cMatchRoom.ResolvingArt = null;
            cMatchRoom.currentArtDamage = 0;
            cMatchRoom.currentEffectDamage = 0;

            cMatchRoom.DeclaringAttackCard = null;
            cMatchRoom.BeingTargetedForAttackCard = null;
        }

        private void ResolveOnDamageStepEffects(MatchRoom cMatchRoom)
        {
            //resolve the effects that was add when attaccking
        }
    }
}