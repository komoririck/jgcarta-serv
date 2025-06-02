using hololive_oficial_cardgame_server.SerializableObjects;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

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

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicArtDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), pReturnData));
            cMatchRoom.PushPlayerAnswer();

            cMatchRoom.currentGamePhase = MatchRoom.GAMEPHASE.ResolvingDamage;
        }
        private static void OnArtUsedEffects(Card attackingCard, MatchRoom cMatchRoom)
        {
            string playerId = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;

            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (Card card in attackingCard.attachedEquipe)
            {
                switch (card.cardNumber)
                {
                    case "hBP01-120":
                        if (attackingCard.cardPosition.Equals("Stage"))
                        {
                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawBloomIncreaseEffect", requestObject = "" };
                            Lib.MoveTopCardFromXToY(playerDeck, playerHand, 1);

                            DuelAction newDraw = new DuelAction().SetID(playerId).DrawTopCardFromXToY(playerHand, "Deck", 1);
                            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(playerId), hidden: true, playerRequest: ReturnData, duelAction: newDraw));
                            cMatchRoom.PushPlayerAnswer();
                        }
                        break;
                }
            }
        }
    }
}