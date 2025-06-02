using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Transactions;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.Extensions.Options;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    class AskForMulliganHandler
    {
        public async Task AskForMulliganHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer) && cMatchRoom.PAMulliganAsked)
            {
                return;
            }

            else if (playerRequest.playerID.Equals(cMatchRoom.secondPlayer) && cMatchRoom.PBMulliganAsked)
            {
                return;
            }

            DuelAction draw;
            List<Record> cardlist;
            PlayerRequest returnData;
            PlayerRequest pReturnData;

            if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer) && !cMatchRoom.PAMulliganAsked)
            {
                await HandleMulligan(cMatchRoom, true, playerRequest);
                cMatchRoom.PAMulliganAsked = true;
            }
            else if (playerRequest.playerID.Equals(cMatchRoom.secondPlayer) && !cMatchRoom.PBMulliganAsked)
            {
                await HandleMulligan(cMatchRoom, false, playerRequest);
                cMatchRoom.PBMulliganAsked = true;
            }

            // can only go though here if both player have chosen to mulligan
            if (!cMatchRoom.PAMulliganAsked || !cMatchRoom.PBMulliganAsked) {
                return;
            }


            cMatchRoom.PrintPlayerHand();

            bool neededMulligan = false;
            //mulligan PA
            cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Debut");
            cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Spot"));
            if (cardlist.Count == 0) {
                for (int i = cMatchRoom.playerAHand.Count; i > 0; i--)
                {
                    if (i != 7)
                    {
                        cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Debut");
                        cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Spot"));
                    }

                    if (cardlist.Count != 0)
                        break;

                    int x = cMatchRoom.playerAHand.Count - 1;
                    cMatchRoom.SuffleHandToTheDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand);
                    cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                    Lib.MoveTopCardFromXToY(cMatchRoom.playerADeck, cMatchRoom.playerAHand, x);
                    Console.WriteLine($"PA mulligan " + x);

                    if (x == 0)
                        Lib.EndDuelAsync(cMatchRoom, cMatchRoom.secondPlayer);
                }
                neededMulligan = true;
            }

            //Sending to players PA mulligan hand
            if (neededMulligan)
            {
                draw = new DuelAction()
                {
                    playerID = cMatchRoom.firstPlayer,
                    suffle = false,
                    zone = "Deck",
                    cardList = cMatchRoom.playerAHand,
                    actionObject = neededMulligan.ToString()
                };
            }
            else
            {
                draw = new DuelAction()
                {
                    playerID = cMatchRoom.firstPlayer,
                    actionObject = neededMulligan.ToString()
                };
            }

            List<string> playerList = new List<string>() { cMatchRoom.firstPlayer, cMatchRoom.secondPlayer};
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(playerList, duelAction: draw, type: "DuelUpdate", description: "PAMulliganF"));
            cMatchRoom.PushPlayerAnswer();

            /////////////
            /////////////
            //mulligan PB
            neededMulligan = false;
            cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Debut");
            cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Spot"));
            if (cardlist.Count == 0) { 
                for (int i = cMatchRoom.playerBHand.Count; i > 0; i--)
                {
                    if (i != 7) {
                        cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Debut");
                        cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Spot"));
                    }

                    if (cardlist.Count != 0)
                        break;

                    int x = cMatchRoom.playerBHand.Count - 1;
                    cMatchRoom.SuffleHandToTheDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand);
                    cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                    Lib.MoveTopCardFromXToY(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, x);

                    if (x == 0)
                        Lib.EndDuelAsync(cMatchRoom, cMatchRoom.firstPlayer);

                }
                neededMulligan = true;
            }

            //sending to players mulligan hand
            if (neededMulligan)
            {
                draw = new DuelAction()
                {
                    playerID = cMatchRoom.secondPlayer,
                    suffle = false,
                    zone = "Deck",
                    cardList = cMatchRoom.playerBHand,
                    actionObject = neededMulligan.ToString()
                };
            }
            else
            {
                draw = new DuelAction()
                {
                    playerID = cMatchRoom.secondPlayer,
                    actionObject = neededMulligan.ToString()
                };
            }

            playerList = new List<string>() { cMatchRoom.secondPlayer, cMatchRoom.firstPlayer };
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(playerList, duelAction: draw, type: "DuelUpdate", description: "PBMulliganF"));
            cMatchRoom.PushPlayerAnswer();

            cMatchRoom.currentGameHigh = 6;
        }

        private async Task HandleMulligan(MatchRoom room, bool isFirstPlayer, PlayerRequest request)
        {

            // Determine which player is acting (A or B)
            var playerHand = isFirstPlayer ? room.playerAHand : room.playerBHand;
            var playerDeck = isFirstPlayer ? room.playerADeck : room.playerBDeck;
            var opponentHand = isFirstPlayer ? room.playerBHand : room.playerAHand;
            var opponentDeck = isFirstPlayer ? room.playerBDeck : room.playerADeck;
            var playerID = isFirstPlayer ? room.playerA.PlayerID : room.playerB.PlayerID;
            var playerName = isFirstPlayer ? "PA" : "PB";  // For PANoMulligan, PBMulligan, etc.

            DuelAction draw = new() { playerID = playerID, actionObject = false.ToString() };
            DuelAction drawDummy = new() { playerID = playerID, actionObject = false.ToString() };



            if (request.requestObject.Equals("t"))
            {
                // Shuffle and redraw cards
                room.SuffleHandToTheDeck(playerDeck, playerHand);
                playerDeck = room.ShuffleCards(playerDeck);
                Lib.MoveTopCardFromXToY(playerDeck, playerHand, 7);


                // Create Draw and DrawDummy objects
                draw = new DuelAction
                {
                    playerID = playerID,
                    suffle = false,
                    zone = "Deck",
                    cardList = playerHand,
                    actionObject = true.ToString()
                };

                drawDummy = new DuelAction
                {
                    playerID = playerID,
                    suffle = false,
                    zone = "Deck",
                    cardList = room.FillCardListWithEmptyCards(playerHand),
                    actionObject = true.ToString()
                };
            }

            // Handle response for acting player (actual cards)
            var playerResponse = new PlayerRequest
            {
                type = "DuelUpdate",
                description = request.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(draw, Lib.jsonOptions)  // Acting player gets the real hand
            };
            var opponentResponse = new PlayerRequest
            {
                type = "DuelUpdate",
                description = request.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(drawDummy)  // Opponent gets the dummy hand
            };
            room.RecordPlayerRequest(new List<PlayerRequest> {playerResponse, opponentResponse});
            room.PushPlayerAnswer();
        }
    }

}