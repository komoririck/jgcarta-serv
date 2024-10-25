using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    class AskForMulliganHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public AskForMulliganHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }
        public async Task AskForMulliganHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer && cMatchRoom.PAMulliganAsked)
            {
                return;
            }
            if (int.Parse(playerRequest.playerID) == cMatchRoom.secondPlayer && cMatchRoom.PBMulliganAsked)
            {
                return;
            }

            DuelAction draw;
            List<Record> cardlist;
            RequestData returnData;
            RequestData pReturnData;


            /////////////
            /////////////
            // For Player A
            if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer && !cMatchRoom.PAMulliganAsked)
            {
                Lib.PrintPlayerHand(cMatchRoom);
                await HandleMulligan(cMatchRoom, true, playerRequest, playerConnections[cMatchRoom.firstPlayer.ToString()], playerConnections[cMatchRoom.secondPlayer.ToString()]);
                cMatchRoom.PAMulliganAsked = true;
            }

            // For Player B
            if (int.Parse(playerRequest.playerID) == cMatchRoom.secondPlayer && !cMatchRoom.PBMulliganAsked)
            {
                Lib.PrintPlayerHand(cMatchRoom);
                await HandleMulligan(cMatchRoom, false, playerRequest, playerConnections[cMatchRoom.secondPlayer.ToString()], playerConnections[cMatchRoom.firstPlayer.ToString()]);
                cMatchRoom.PBMulliganAsked = true;
            }

            // can only go though here if both player have chosen to mulligan
            if (!cMatchRoom.PAMulliganAsked || !cMatchRoom.PBMulliganAsked)
                return;

            //mulligan PA
            for (int i = cMatchRoom.playerAHand.Count; i > 0; i--)
            {
                cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Debut");
                if (cardlist.Count > 0)
                    break;

                int x = cMatchRoom.playerAHand.Count - 1;
                cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand);
                cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, x);
                Console.WriteLine($"PA mulligan " + x);
            }

            //Sending to players PA mulligan hand
            draw = new DuelAction()
            {
                playerID = cMatchRoom.firstPlayer,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerAHand
            };
            pReturnData = new RequestData { type = "duelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);
            // we are changing the line here
            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);

            pReturnData = new RequestData { type = "duelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

            /////////////
            /////////////
            //mulligan PB
            for (int i = cMatchRoom.playerBHand.Count; i > 0; i--)
            {
                cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Debut");
                if (cardlist.Count > 0)
                    break;
                int x = cMatchRoom.playerBHand.Count - 1;
                cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand);
                cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, x);
                Console.WriteLine($"PB mulligan " + x);
            }


            //sending to players mulligan hand
            draw = new DuelAction()
            {
                playerID = cMatchRoom.secondPlayer,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerBHand
            };

            pReturnData = new RequestData { type = "duelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);

            pReturnData = new RequestData { type = "duelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw, Lib.options) };
            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);


            cMatchRoom.currentGameHigh = 6;

            Lib.PrintPlayerHand(cMatchRoom);
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

            if (request.requestData.requestObject.Equals("t"))
            {
                // Shuffle and redraw cards
                room.suffleHandToTheDeck(playerDeck, playerHand);
                playerDeck = room.ShuffleCards(playerDeck);
                Lib.getCardFromDeck(playerDeck, playerHand, 7);
            }

            // Create Draw and DrawDummy objects
            var draw = new DuelAction
            {
                playerID = playerID,
                suffle = false,
                zone = "Deck",
                cardList = playerHand
            };

            var drawDummy = new DuelAction
            {
                playerID = playerID,
                suffle = false,
                zone = "Deck",
                cardList = room.FillCardListWithEmptyCards(playerHand)
            };

            // Handle response for acting player (actual cards)
            var playerResponse = new RequestData
            {
                type = "duelUpdate",
                description = request.requestData.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(draw, Lib.options)  // Acting player gets the real hand
            };
            Lib.SendMessage(socketA, playerResponse);

            // Handle response for opponent (dummy hand)
            var opponentResponse = new RequestData
            {
                type = "duelUpdate",
                description = request.requestData.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
                requestObject = JsonSerializer.Serialize(drawDummy)  // Opponent gets the dummy hand
            };

            Lib.SendMessage(socketB, opponentResponse);
        }
    }

}