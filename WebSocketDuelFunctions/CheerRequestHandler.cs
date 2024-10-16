using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Extensions;
using System.ComponentModel.Design;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerRequestHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public CheerRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task CheerRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStep)
               return;

            Draw cardCheerDraw = new Draw();
            cardCheerDraw.playerID = cMatchRoom.currentPlayerTurn;
            cardCheerDraw.zone = "CardCheer";

            RequestData ReturnData = new RequestData { type = "GamePhase", description = "CheerStep", requestObject = "" };

            if (cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer))
            {
                if (cMatchRoom.playerACardCheer.Count != 0) 
                { 
                    Lib.getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerAHand, 1);
                    cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerAHand[cMatchRoom.playerAHand.Count - 1] };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);

                    cardCheerDraw.cardList = new List<Card>() { new Card() };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card() { cardNumber="Empty"} };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }
            else
            {
                if (cMatchRoom.playerBCardCheer.Count != 0)
                {
                    Lib.getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBHand, 1);
                    cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerBHand[cMatchRoom.playerBHand.Count - 1] };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);

                    cardCheerDraw.cardList = new List<Card>() { new Card() };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card() { cardNumber = "Empty" } };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }


            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoose;
            cMatchRoom.currentGameHigh++;


        }
    }
}