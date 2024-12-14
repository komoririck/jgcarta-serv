using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Transactions;
using hololive_oficial_cardgame_server.SerializableObjects;

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
                await HandleMulligan(cMatchRoom, true, playerRequest, MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()]);
                cMatchRoom.PAMulliganAsked = true;
            }
            else if (playerRequest.playerID.Equals(cMatchRoom.secondPlayer) && !cMatchRoom.PBMulliganAsked)
            {
                await HandleMulligan(cMatchRoom, false, playerRequest, MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()]);
                cMatchRoom.PBMulliganAsked = true;
            }

            // can only go though here if both player have chosen to mulligan
            if (!cMatchRoom.PAMulliganAsked || !cMatchRoom.PBMulliganAsked) {
                return;
            }


            Lib.PrintPlayerHand(cMatchRoom);

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
                    cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand);
                    cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                    Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, x);
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

            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);
            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(draw.cardList);
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

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
                    cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand);
                    cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                    Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, x);

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

            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);
            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(draw.cardList);
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);


            cMatchRoom.currentGameHigh = 6;
        }

        private async Task HandleMulligan(MatchRoom room, bool isFirstPlayer, PlayerRequest request, WebSocket socketA, WebSocket socketB)
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
                room.suffleHandToTheDeck(playerDeck, playerHand);
                playerDeck = room.ShuffleCards(playerDeck);
                Lib.getCardFromDeck(playerDeck, playerHand, 7);


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
                requestObject = JsonSerializer.Serialize(draw, Lib.options)  // Acting player gets the real hand
            };
            await Lib.SendMessage(socketA, playerResponse);

            // Handle response for opponent (dummy hand)
            var opponentResponse = new PlayerRequest
            {
                type = "DuelUpdate",
                description = request.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(drawDummy)  // Opponent gets the dummy hand
            };

            await Lib.SendMessage(socketB, opponentResponse);
        }
    }

}