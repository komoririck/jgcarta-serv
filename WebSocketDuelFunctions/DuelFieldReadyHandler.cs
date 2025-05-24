using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class DuelFieldReadyHandler
    {
        internal async Task DuelFieldReadyHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelFieldData _DuelFieldDataA = new();
            DuelFieldData _DuelFieldDataB = new();

            PlayerRequest pReturnData = new();

            DuelFieldData _duelFieldData = JsonSerializer.Deserialize<DuelFieldData>(playerRequest.requestObject);

            if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
            {
                FirstGameBoardSetup(_duelFieldData, playerRequest.playerID, cMatchRoom, _duelFieldData.playerAStage, _duelFieldData.playerABackPosition);
                cMatchRoom.StopTimer(playerRequest.playerID);
            }
            else
            {
                FirstGameBoardSetup(_duelFieldData, playerRequest.playerID, cMatchRoom, _duelFieldData.playerBStage, _duelFieldData.playerBBackPosition);
                cMatchRoom.StopTimer(playerRequest.playerID);
            }

            if (!(cMatchRoom.playerAInicialBoardSetup && cMatchRoom.playerBInicialBoardSetup))
                return;


            cMatchRoom.playerAOshi.GetCardInfo();
            cMatchRoom.playerBOshi.GetCardInfo();

            Lib.getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerALife, int.Parse(cMatchRoom.playerAOshi.life));
            Lib.getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBLife, int.Parse(cMatchRoom.playerBOshi.life));

            //place the life counter acording to the oshiiiiii
            _DuelFieldDataA = new DuelFieldData
            {
                playerABackPosition = cMatchRoom.playerABackPosition,
                playerAFavourite = cMatchRoom.playerAOshi,
                playerAStage = cMatchRoom.playerAStage,
                playerALife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerALife),

                playerBBackPosition = cMatchRoom.playerBBackPosition,
                playerBFavourite = cMatchRoom.playerBOshi,
                playerBStage = cMatchRoom.playerBStage,
                playerBLife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBLife),

                currentPlayerTurn = cMatchRoom.firstPlayer
            };


            //since we were able to update the users table to lock the match, send both players to the match
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "DuelUpdate", requestObject = JsonSerializer.Serialize(_DuelFieldDataA, Lib.jsonOptions) }; 
            cMatchRoom.RecordPlayerRequest(pReturnData);
            cMatchRoom.PushPlayerRequest(Player.PlayerA);
            cMatchRoom.PushPlayerRequest(Player.PlayerB);

            //update the room phase, so the server can take it automaticaly from here
            cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
            cMatchRoom.currentGameHigh = 7;
            //devolver um synchigh com informações de quem vai comprar

            cMatchRoom.StartOrResetTimer(cMatchRoom.currentPlayerTurn.ToString(), enduel => Lib.EndDuelAsync(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)));
        }

        bool FirstGameBoardSetup(DuelFieldData _duelFieldData, string playerid, MatchRoom matchroom, Card playerStage, List<Card> playerBackStage)
        {


            //checking if there is card in the Stage
            if (playerStage == null)
            {
                Lib.WriteConsoleMessage("\nInvalid play, no card at stage: " + playerid + matchroom.currentGameHigh);
                return false;
            }

            //check if card in the stage can be there
            List<Record> cardlist = FileReader.QueryRecordsByNameAndBloom(new List<Card>() { playerStage }, "Debut");
            cardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(new List<Card>() { playerStage }, "Spot"));
            if (cardlist.Count == 0)
            {
                Lib.WriteConsoleMessage("\nInvalid play, no card suitable at stage: " + playerid + matchroom.currentGameHigh);
                return false;
            }
            List<Card> cardsPlayedThisTurn = new() { playerStage };
            //check if backposition is in the maximum limite
            if (playerBackStage.Count > 5)
            {
                Lib.WriteConsoleMessage("\nInvalid play, more cards at the back stage than what it should: " + playerid + matchroom.currentGameHigh);
                return false;
            }

            //check if all cards at the backposition can be there
            if (playerBackStage.Count > 0)
            {
                int n = 0;
                foreach (Card c in playerBackStage)
                {
                    List<Record> subcardlist = FileReader.QueryRecordsByNameAndBloom(new List<Card>() { c }, "Debut");
                    subcardlist.AddRange(FileReader.QueryRecordsByNameAndBloom(new List<Card>() { c }, "Spot"));
                    if (subcardlist.Count > 0)
                        n++;
                }
                if (n != playerBackStage.Count)
                {
                    Lib.WriteConsoleMessage("\nInvalid play, there card in the backstage that shouldnt: " + playerid + matchroom.currentGameHigh);
                    return false;
                }
            }
            // the duplicated cards are still around here... need to check why.

            //check if all played cards exists in the player hand
            cardsPlayedThisTurn.AddRange(playerBackStage);

            List<Card> playerHand = playerid == matchroom.firstPlayer ? matchroom.playerAHand : matchroom.playerBHand;

            foreach (Card playedThisTurn in cardsPlayedThisTurn) {
                int i = -1;
                for (int j = 0; j < playerHand.Count; j++) {
                    if(playerHand[j].cardNumber.Equals(playedThisTurn.cardNumber)){
                        i = j;
                        break;
                    }
                }
                if (i == -1)
                {
                    Lib.PrintPlayerHand(matchroom);
                    Lib.WriteConsoleMessage("\nInvalid play, there card in the field that are not at player hand: " + playedThisTurn.cardNumber);
                    return false;
                }
                playerHand.RemoveAt(i);
            }

            

            //since we get this far, we remove the played cards from the hand
            if (matchroom.playerA.PlayerID == playerid)
            {
                //since the last code updated the hand, we only need to update the field now:
                matchroom.playerAStage = _duelFieldData.playerAStage;
                matchroom.playerABackPosition = _duelFieldData.playerABackPosition;
                matchroom.playerAInicialBoardSetup = true;
            }
            else
            {
                //since the last code updated the hand, we only need to update the field now:
                matchroom.playerBStage = _duelFieldData.playerBStage;
                matchroom.playerBBackPosition = _duelFieldData.playerBBackPosition;
                matchroom.playerBInicialBoardSetup = true;
            }


            return true;
        }

    }
}