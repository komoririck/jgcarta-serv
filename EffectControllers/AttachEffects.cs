using hololive_oficial_cardgame_server.SerializableObjects;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    internal class AttachEffects
    {
        internal static void OnAttachEffectsAsync(DuelAction? _DuelAction, MatchRoom? cMatchRoom, PlayerRequest playerRequest, WebSocket webSocket)
        {
            //General activation validations
            if (!_DuelAction.usedCard.cardNumber.Equals(cMatchRoom.currentCardResolving))
            {
                Lib.WriteConsoleMessage("Wrong card calling for resolution");
                return;
            }

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = ISFIRSTPLAYER ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            Card playerStage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> playerBackstage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            bool energyPaid = false;

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard?.GetCardInfo();
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard?.GetCardInfo();
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo();

            try
            {
                switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                {
                    case "hBP01-125":
                        string SelectedCard = _DuelAction.actionObject;

                        int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, SelectedCard);
                        if (n > -1)
                        {
                            DuelAction duelAction = new() {
                                playerID = cMatchRoom.currentPlayerTurn,
                                cardList = new() { new Card(SelectedCard) }
                            };

                            playerHand.RemoveAt(n);

                            Lib.SendPlayerData(cMatchRoom, false, duelAction, "RemoveCardsFromHand");

                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawAttachEffect", requestObject = "" };
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
                        ResetResolution(cMatchRoom);
                        break;
                    default:
                        ResetResolution(cMatchRoom);
                        break;
                }

            }
            catch (Exception e)
            {
                Lib.WriteConsoleMessage(e.Message + e.StackTrace + e.InnerException);
            }
        }
        static void ResetResolution(MatchRoom cMatchRoom)
        {
            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentCardResolvingStage = "";

            List<Card> temphand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            cMatchRoom.ResolvingEffectChain.Clear();
            cMatchRoom.currentGameHigh++;

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            temphand.Clear();
        }

    }
}