using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class DoCollabHandler
    {
        internal async Task DoCollabHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

                _DuelAction.targetCard?.GetCardInfo();
                _DuelAction.usedCard?.GetCardInfo();

            if (cMatchRoom.firstPlayer.Equals(_DuelAction.playerID))
            {
                if (cMatchRoom.playerADeck.Count == 0)
                {
                    Lib.WriteConsoleMessage("Not able to send a card from deck to holopower");
                    return;
                }
                //must be empty or null so theres no card in the collab before sending 
                if (cMatchRoom.playerACollaboration == null)
                {
                    int x = -1;
                    int j = 0;
                    List<int> indicesToRemove = new List<int>(); // List to track indices to remove
                    foreach (Card c in cMatchRoom.playerABackPosition)
                    {
                        if (c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.cardPosition.Equals(_DuelAction.usedCard.cardPosition) && c.suspended == false)
                        {
                            cMatchRoom.playerAHoloPower.Add(cMatchRoom.playerADeck.Last());
                            cMatchRoom.playerADeck.RemoveAt(cMatchRoom.playerADeck.Count - 1);
                            cMatchRoom.playerACollaboration = c;
                            x = j;
                            break;
                        }
                        j++;
                    }
                    if (x == -1)
                    {
                        Lib.WriteConsoleMessage("No match found");
                        return;
                    }
                    cMatchRoom.playerABackPosition.RemoveAt(x);
                }
            }
            else
            {
                if (cMatchRoom.playerBDeck.Count == 0) {
                    Lib.WriteConsoleMessage("Not able to send a card from deck to holopower");
                    return;
                }
                //must be empty or null so theres no card in the collab before sending 
                if (cMatchRoom.playerBCollaboration == null)
                {
                    int j = 0;
                    int x = -1;
                    List<int> indicesToRemove = new List<int>(); // List to track indices to remove
                    foreach (Card c in cMatchRoom.playerBBackPosition)
                    {
                        if (c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.cardPosition.Equals(_DuelAction.usedCard.cardPosition) && c.suspended == false)
                        {
                            cMatchRoom.playerBHoloPower.Add(cMatchRoom.playerBDeck.Last());
                            cMatchRoom.playerBDeck.RemoveAt(cMatchRoom.playerBDeck.Count - 1);
                            cMatchRoom.playerBCollaboration = c;
                            x = j;
                            break;
                        }
                        j++;
                    }
                    if (x == -1)
                    {
                        Lib.WriteConsoleMessage("No match found");
                        return;
                    }
                    cMatchRoom.playerBBackPosition.RemoveAt(x);
                }
            }

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "DoCollab", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

            cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            cMatchRoom.currentGameHigh++;
        }
    }
}