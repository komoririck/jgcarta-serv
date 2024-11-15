using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class BloomEffects
    {
        internal static void OnBloomEffectResolutionAsync(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms, PlayerRequest playerRequest, WebSocket webSocket)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
            {
                Lib.WriteConsoleMessage("not card are current resolving");
                return;
            }

            //End - General activation validations
            PlayerRequest pReturnData;
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = ISFIRSTPLAYER ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            Card playerStage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> playerBackstage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            //check if can play limited cards, and also add to limited list
            List<Card> playerLimitedCardsPlayed = ISFIRSTPLAYER ? cMatchRoom.playerALimiteCardPlayed : cMatchRoom.playerBLimiteCardPlayed;

            bool energyPaid = false;

            _DuelAction.targetCard.GetCardInfo();
            _DuelAction.usedCard.GetCardInfo();
            _DuelAction.cheerCostCard.GetCardInfo();

            //ensure that we have a resolving card, if we have dont need to use
            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            string playerWhoUsedTheEffect = _DuelAction.playerID;

            try
            {

                switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                {
                    case "hBP01-043":
                        if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                            Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn);
                        else if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                            Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn);
                        else
                            Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.usedCard.cardPosition);

                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-030":
                        CardEffect _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Collaboration",
                            type = CardEffectType.BuffDamageToCardAtZoneIfHaveTag,
                            Damage = 30,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Stage",
                            type = CardEffectType.BuffDamageToCardAtZoneIfHaveTag,
                            Damage = 30,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            listIndex = 1
                        };
                        cMatchRoom.ActiveTurnEffects.Add(_CardEffect);
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

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;

            if (cMatchRoom.extraInfo != null)
                cMatchRoom.extraInfo.Clear();

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentCardResolvingStage = "";

            List<Card> temphand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            cMatchRoom.currentGameHigh++;

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            temphand.Clear();
        }
    }
}