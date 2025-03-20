using hololive_oficial_cardgame_server.SerializableObjects;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResolveArtHandler
    {
        public static async Task ResolveDamage(MatchRoom cMatchRoom)
        {

            cMatchRoom.currentArtDamage = ArtCalculator.CalculateTotalDamage(cMatchRoom.ResolvingArt, cMatchRoom.DeclaringAttackCard, cMatchRoom.BeingTargetedForAttackCard, cMatchRoom.currentPlayerTurn, MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn), cMatchRoom);

            if (cMatchRoom.currentArtDamage < -10000)
            {
                Lib.WriteConsoleMessage("no suficient energy attached");
                return;
            }

            DuelAction _DuelAction = new()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = cMatchRoom.DeclaringAttackCard,
                targetCard = cMatchRoom.BeingTargetedForAttackCard,
                actionObject = cMatchRoom.currentArtDamage.ToString()
            };

            OnArtUsedEffects(cMatchRoom.DeclaringAttackCard, cMatchRoom);

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicArtDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

            cMatchRoom.currentGamePhase = MatchRoom.GAMEPHASE.ResolvingDamage;
        }
        private static void OnArtUsedEffects(Card attackingCard, MatchRoom cMatchRoom)
        {
            foreach (Card card in attackingCard.attachedEquipe)
            {
                switch (card.cardNumber)
                {
                    case "hBP01-120":
                        if (attackingCard.cardPosition.Equals("Stage"))
                        {
                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawBloomIncreaseEffect", requestObject = "" };
                            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                            {
                                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                                Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom, ReturnData);
                            }
                            else
                            {
                                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                                Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom, ReturnData);
                            }
                        }
                        break;
                }
            }
        }
    }
}