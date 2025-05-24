using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class AttachTopCheerEnergyToBackHandler
    {
        internal async Task AttachCheerEnergyHandleAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, bool stage, bool collab, bool back, bool TOPCHEERDECK, bool FULLCHEERDECK, int ClientEnergyIndex = -1, bool ARQUIVEFULLDECK = false)
        {

            _DuelAction.targetCard?.GetCardInfo();
            _DuelAction.usedCard?.GetCardInfo();

            //since sometimes we call this function passing a string instead of a list, we try to parse the json, if not able, we just assign
            string SelectedCheerNumber = "";

            try
            {
                List<string> objects = JsonSerializer.Deserialize<List<string>>(_DuelAction.actionObject);
                SelectedCheerNumber = objects[ClientEnergyIndex];
            }
            catch (JsonException)
            {
                SelectedCheerNumber = _DuelAction.actionObject.ToString();
            }

            //temphand
            List<Card> playertemphand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            if (playertemphand.Count == 0)
            {
                Lib.WriteConsoleMessage("temp hand null -- RISK PLAY");
                return;
            }

            if (ARQUIVEFULLDECK)
            {
                List<Card> playerArquiveDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                int n = -1;
                for (int j = 0; j < playerArquiveDeck.Count; j++)
                {
                    if (playerArquiveDeck[j].cardNumber.Equals(SelectedCheerNumber))
                    {
                        n = j;
                        break;
                    }
                }
                if (n == -1)
                {
                    Lib.WriteConsoleMessage("Card didnt match one of the cheer");
                    return;
                }
                playerArquiveDeck.RemoveAt(n);
            }
            else
            {
                //energy list
                List<Card> playerCheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                if (TOPCHEERDECK)
                {
                    if (!SelectedCheerNumber.Equals(playerCheerDeck[playerCheerDeck.Count - 1].cardNumber))
                    {
                        Lib.WriteConsoleMessage("Card didnt match the last position of the cheer");
                        return;
                    }
                    //since pass the valition remove last pos
                    playerCheerDeck.RemoveAt(playerCheerDeck.Count - 1);
                }
                else if (FULLCHEERDECK)
                {
                    int n = -1;
                    for (int j = 0; j < playerCheerDeck.Count; j++)
                    {
                        if (playerCheerDeck[j].cardNumber.Equals(SelectedCheerNumber))
                        {
                            n = j;
                            break;
                        }
                    }
                    if (n == -1)
                    {
                        Lib.WriteConsoleMessage("Card didnt match one of the cheer");
                        return;
                    }
                    playerCheerDeck.RemoveAt(n);
                }

            }

            //AssignEnergyToZoneAsync checks if the used card is the energy we're trying to attach, só we need to change here for the topCheerEnergy = _DuelAction.actionObject
            //because the client is sending the card of activate the effect as the one who used the effect
            //holding the used card
            //Card tempUsedCard = _DuelAction.usedCard;
            //changing for the energy
            _DuelAction.usedCard = playertemphand[0];

            DuelAction da = new() { playerID = _DuelAction.playerID, usedCard = playertemphand[0] };
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "RemoveEnergyFrom", requestObject = JsonSerializer.Serialize(da, Lib.jsonOptions) });
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "RemoveEnergyFrom", requestObject = JsonSerializer.Serialize(da, Lib.jsonOptions) });

            bool assinged;

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                assinged = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, (stage == true ? cMatchRoom.playerAStage : null), (collab == true ? cMatchRoom.playerACollaboration : null), (back == true ? cMatchRoom.playerABackPosition : null));
            else
                assinged = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, (stage == true ? cMatchRoom.playerBStage : null), (collab == true ? cMatchRoom.playerBCollaboration : null), (back == true ? cMatchRoom.playerBBackPosition : null));

            if (!assinged) //(x == -1)
            {
                Lib.WriteConsoleMessage("No match found to assign the energy");
                return;
            }

            //since we assign, lets return the usedcard as the card who activate the effect instead of the energy
            //_DuelAction.usedCard = tempUsedCard;

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID) { cMatchRoom.playerATempHand.Clear(); } else { cMatchRoom.playerBTempHand.Clear(); }

            //creating the action to send to the player, we are recycing some information from what player send
            _DuelAction.playedFrom = "CardCheer";

            //lest send to player AttachEnergyResponse since is generic
            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);
        }
    }
}