using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerRequestHandler
    {
        internal async Task CheerRequestHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStep)
               return;

            DuelAction cardCheerDraw = new DuelAction();
            cardCheerDraw.playerID = cMatchRoom.currentPlayerTurn;
            cardCheerDraw.zone = "CardCheer";

            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "CheerStep", requestObject = "" };

            if (cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer))
            {
                if (cMatchRoom.playerACardCheer.Count != 0) 
                { 
                    Lib.getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerAHand, 1);
                    cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerAHand[cMatchRoom.playerAHand.Count - 1] };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);

                    cardCheerDraw.cardList = new List<Card>() { new Card() };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card("Empty")};
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }
            else
            {
                if (cMatchRoom.playerBCardCheer.Count != 0)
                {
                    Lib.getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBHand, 1);
                    cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerBHand[cMatchRoom.playerBHand.Count - 1] };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);

                    cardCheerDraw.cardList = new List<Card>() { new Card() };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                }
                else
                {
                    cardCheerDraw.cardList = new List<Card>() { new Card("Empty") };
                    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw, Lib.options);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], ReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], ReturnData);
                }
            }


            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoose;
            cMatchRoom.currentGameHigh++;


        }
    }
}