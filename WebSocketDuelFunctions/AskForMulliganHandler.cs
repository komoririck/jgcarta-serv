using hololive_oficial_cardgame_server.SerializableObjects;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    class AskForMulliganHandler
    {
        public async Task AskForMulliganHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            if ((playerRequest.playerID.Equals(cMatchRoom.firstPlayer) && cMatchRoom.PAMulliganAsked) || (playerRequest.playerID.Equals(cMatchRoom.secondPlayer) && cMatchRoom.PBMulliganAsked) || cMatchRoom.currentGameHigh != 3)
            {
                return;
            }

            bool isFirstPlayer = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? true : false;

            if (isFirstPlayer)
            {
                cMatchRoom.PAMulliganAsked = true;
            }
            else
            {
                cMatchRoom.PBMulliganAsked = true;
            }

            HandleMulligan(cMatchRoom, isFirstPlayer, playerRequest);

            // can only go though here if both player have chosen to mulligan
            if ((!cMatchRoom.PAMulliganAsked || !cMatchRoom.PBMulliganAsked) || cMatchRoom.currentGameHigh == 6) {
                return;
            }

            cMatchRoom.PrintPlayerHand();

            if (isFirstPlayer)
            {
                HandleMulliganF(isFirstPlayer, ref cMatchRoom, ref cMatchRoom.playerAHand, ref cMatchRoom.playerADeck);
                HandleMulliganF(!isFirstPlayer, ref cMatchRoom, ref cMatchRoom.playerBHand, ref cMatchRoom.playerBDeck);
            }
            else
            {
                HandleMulliganF(!isFirstPlayer, ref cMatchRoom, ref cMatchRoom.playerAHand, ref cMatchRoom.playerADeck);
                HandleMulliganF(isFirstPlayer, ref cMatchRoom, ref cMatchRoom.playerBHand, ref cMatchRoom.playerBDeck);
            }

            cMatchRoom.currentGameHigh = 6;
        }

        private static void HandleMulliganF(bool isFirstPlayer, ref MatchRoom cMatchRoom, ref List<Card> playerHand, ref List<Card> playerDeck) {

            var playerName = isFirstPlayer ? "PA" : "PB";
            bool neededMulligan = false;

            List<Record> cardlist = FileReader.QueryRecordsByNameAndBloom(playerHand, "Debut");
            cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(playerHand, "Spot"));

            if (cardlist.Count == 0)
            {
                for (int i = playerHand.Count; i > 0; i--)
                {
                    if (i != 7)
                    {
                        cardlist = FileReader.QueryRecordsByNameAndBloom(playerHand, "Debut");
                        cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(playerHand, "Spot"));
                    }

                    if (cardlist.Count != 0)
                        break;

                    int x = playerHand.Count - 1;
                    cMatchRoom.SuffleHandToTheDeck(playerDeck, playerHand);
                    playerDeck = cMatchRoom.ShuffleCards(ref playerDeck);
                    Lib.MoveTopCardFromXToY(playerDeck, playerHand, x);

                    if (x == 0)
                        Lib.EndDuelAsync(cMatchRoom, isFirstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer);
                }
                neededMulligan = true;
            }

            DuelAction draw = new DuelAction() { playerID = isFirstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer, actionObject = neededMulligan.ToString() };
            if (neededMulligan)
            {
                draw.suffle = false;
                draw.zone = "Deck";
                draw.cardList = playerHand;
            }

            var pResponse = new PlayerRequest
            {
                playerID = draw.playerID,
                type = "DuelUpdate",
                description = $"{playerName}MulliganF",
                requestObject = JsonSerializer.Serialize(draw, Lib.jsonOptions)
            };

            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(new List<string>() { cMatchRoom.secondPlayer, cMatchRoom.firstPlayer }, playerRequest: pResponse, hidden: true));
            cMatchRoom.PushPlayerAnswer();
        }

        private static void HandleMulligan(MatchRoom room, bool isFirstPlayer, PlayerRequest request)
        {
            var playerHand = isFirstPlayer ? room.playerAHand : room.playerBHand;
            var playerDeck = isFirstPlayer ? room.playerADeck : room.playerBDeck;
            var playerID = isFirstPlayer ? room.playerA.PlayerID : room.playerB.PlayerID;
            var playerName = isFirstPlayer ? "PA" : "PB";

            DuelAction draw = new() { playerID = playerID, actionObject = false.ToString() };

            if (request.requestObject.Equals("t"))
            {
                room.SuffleHandToTheDeck(playerDeck, playerHand);
                playerDeck = room.ShuffleCards(ref playerDeck);
                Lib.MoveTopCardFromXToY(playerDeck, playerHand, 7);

                draw.suffle = false;
                draw.zone = "Deck";
                draw.cardList = playerHand;
            }

            var playerResponse = new PlayerRequest
            {
                playerID = playerID,
                type = "DuelUpdate",
                description = request.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(draw, Lib.jsonOptions) 
            };
            room.RecordPlayerRequest(room.ReplicatePlayerRequestForOtherPlayers(new List<string>() { room.secondPlayer, room.firstPlayer }, playerRequest: playerResponse, hidden: true));
            room.PushPlayerAnswer();
        }
    }

}