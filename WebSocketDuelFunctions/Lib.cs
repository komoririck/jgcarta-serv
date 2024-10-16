﻿using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    public class Lib
    {
        static public JsonSerializerOptions options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public static void WriteConsoleMessag(string s)
        {
            Console.WriteLine("\n" + s);

        }
        static public int getCardFromDeck(List<Card> deck, List<Card> target, int amount)
        {
            if (deck.Count > amount)
            {
                if (deck.Count < amount)
                    amount = deck.Count;

                for (int i = 0; i < amount; i++)
                {
                    target.Add(deck[deck.Count - 1]);
                    if (deck[deck.Count - 1] != null)
                        deck.RemoveAt(deck.Count - 1);
                    else
                        return (i - amount);

                }
                return amount;
            }
            return amount;
        }


        static public void RemovePlayedCardsFromHand(List<Card> handCards, List<Card> playedCards)
        {
            foreach (var playedCard in playedCards)
            {
                var cardToRemove = handCards.FirstOrDefault(card => card.name == playedCard.name);

                if (cardToRemove != null)
                {
                    handCards.Remove(cardToRemove);
                }
            }
        }

        static public bool HaveSameObjectCounts(List<Card> playedthisturn, List<Card> hand)
        {
            // Create a copy of the hand to safely modify it
            List<Card> handCopy = new List<Card>(hand);
            int n = 0;

            foreach (Card cardPlayed in playedthisturn)
            {
                // Find a match in the copy of the hand
                Card matchingCard = handCopy.FirstOrDefault(card => card.cardNumber == cardPlayed.cardNumber);

                if (matchingCard != null)
                {
                    handCopy.Remove(matchingCard); // Remove the matched card from the copy
                    n++;
                }
                else
                {
                    return false; // No match found for this card, return false
                }
            }

            // Check if the count matches the number of played cards
            return n == playedthisturn.Count;
        }




        static public async Task EndDuelAsync(Boolean result, MatchRoom matchRoom, int pickWinner = -2)
        {

            int currentPlayerTurn = matchRoom.currentPlayerTurn;
            if (pickWinner > -1)
                currentPlayerTurn = pickWinner;

            int oponnent = MatchRoom.GetOtherPlayer(matchRoom, currentPlayerTurn);
            DuelAction _duelaction = new();
            _duelaction.actionType = "Victory";
            _duelaction.playerID = currentPlayerTurn;

            RequestData _ReturnData = new RequestData { type = "GamePhase", description = "Endduel", requestObject = JsonSerializer.Serialize(_duelaction, Lib.options) };


            await SendMessage(MessageDispatcher.playerConnections[currentPlayerTurn.ToString()], _ReturnData);
            await SendMessage(MessageDispatcher.playerConnections[oponnent.ToString()], _ReturnData);

            // NEED TO CALL FOR THE DATABASE
            try
            {
                MessageDispatcher._MatchRooms.Remove(matchRoom);

                if (MessageDispatcher.playerConnections.TryGetValue(currentPlayerTurn.ToString(), out var currentPlayerConnection))
                {
                    try
                    {
                        if (currentPlayerConnection.State != WebSocketState.Closed) { }
//                        await currentPlayerConnection.CloseAsync(WebSocketCloseStatus.NormalClosure,"Closing connection",CancellationToken.None);
                    }
                    catch (WebSocketException ex)
                    {
                        // Handle the exception, log it, or notify the user
                        Debug.WriteLine($"Error closing connection for {currentPlayerTurn}: {ex.Message}");
                    }
                    MessageDispatcher.playerConnections[currentPlayerTurn.ToString()].Dispose();
                    MessageDispatcher.playerConnections.TryRemove(currentPlayerTurn.ToString(), out _);
                }

                if (MessageDispatcher.playerConnections.TryGetValue(oponnent.ToString(), out var opponentConnection))
                {
                    try
                    {
                        if (opponentConnection.State != WebSocketState.Closed) { }
//                            await opponentConnection.CloseAsync(WebSocketCloseStatus.NormalClosure,"Closing connection",CancellationToken.None);
                    }
                    catch (WebSocketException ex)
                    {
                        // Handle the exception, log it, or notify the user
                        Debug.WriteLine($"Error closing connection for {oponnent}: {ex.Message}");
                    }
                    MessageDispatcher.playerConnections[oponnent.ToString()].Dispose();
                    MessageDispatcher.playerConnections.TryRemove(oponnent.ToString(), out _);
                }
            }
            catch (Exception ex)
            {
                // Handle the out-of-range exception
                Debug.WriteLine($"ArgumentOutOfRangeException: {ex.Message}");
            }
        }

        public static async Task SendMessage(WebSocket webSocket, RequestData data, [CallerMemberName] string callerName = "")
        {

            Lib.WriteConsoleMessag("Send to " +  GetKeyByValue(webSocket) + ":\n" + $"{callerName}\n" + JsonSerializer.Serialize(data, options).Replace("\\u0022", "\"") + ":\n");

            webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, options))), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        static public string GetKeyByValue(WebSocket socket)
        {
            foreach (var pair in MessageDispatcher.playerConnections)
            {
                if (pair.Value == socket)
                {
                    return pair.Key; // Return the key when the matching value is found
                }
            }
            return null; // Return null if no matching value is found
        }




        public static bool GamePhaseCheerChoosedAsync(DuelAction duelAction, MatchRoom matchRoom, Card stage, Card collab, List<Card> backStage)
        {
            if (duelAction.usedCard == null)
                return false;

            if (duelAction.targetCard == null)
                return false;

            duelAction.usedCard.GetCardInfo(duelAction.usedCard.cardNumber);
            duelAction.targetCard.GetCardInfo(duelAction.targetCard.cardNumber);
            bool hasAttached = false;

            if (string.IsNullOrEmpty(duelAction.usedCard.cardType))
                return false;

            if (duelAction.usedCard.cardType.Equals("エール"))
            {
                if (duelAction.targetCard.cardNumber.Equals(stage.cardNumber) && duelAction.local.Equals("Stage"))
                {
                    stage.attachedEnergy.Add(duelAction.usedCard);
                    return hasAttached = true;
                }
                if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.local.Equals("Collaboration"))
                {
                    collab.attachedEnergy.Add(duelAction.usedCard);
                    return hasAttached = true;
                }

                if (backStage.Count > 0) // Check if there are elements in the backStage list
                {
                    for (int y = 0; y < backStage.Count; y++)
                    {
                        // Check if the target card number matches the current backstage card number
                        if (duelAction.targetCard.cardNumber.Equals(backStage[y].cardNumber) &&
                            duelAction.targetCard.cardPosition.Equals(backStage[y].cardPosition))
                        {
                            backStage[y].attachedEnergy.Add(duelAction.usedCard);
                            return hasAttached = true;
                        }
                    }
                }
                else
                {
                    // Handle case when backStage is empty if necessary
                    Lib.WriteConsoleMessag("Error: backStage list is empty.");
                }
            }


            return false;
        }






        public static void SortOrderToAddDeck(List<Card> cardList, List<int> numberList) // FUCKING BUBBLE SORT
        {

            for (int i = 0; i < numberList.Count; i++)
            {
                for (int j = 0; j < numberList.Count - 1; j++)
                {
                    if (numberList[j] > numberList[j + 1])
                    {
                        Card tempc = cardList[j];
                        int temp = numberList[j];
                        numberList[j] = numberList[j + 1];
                        cardList[j] = cardList[j + 1];
                        numberList[j + 1] = temp;
                        cardList[j + 1] = tempc;
                    }
                }
            }

        }



        static public bool ValidatePlayerRequest(PlayerRequest playerRequest)
        {
            // Implement your validation logic here
            // For example, check if PlayerID and Password are valid
            return !string.IsNullOrEmpty(playerRequest.playerID) && !string.IsNullOrEmpty(playerRequest.password);
        }

        static public List<string> CardListToStringList(List<Card> cards)
        {
            List<string> returnCards = new();
            foreach (Card c in cards)
            {
                returnCards.Add(c.cardNumber);
            }
            return returnCards;
        }
        static public bool HaveSameWords(List<string> list1, List<string> list2)
        {
            // Step 1: Check if both lists have the same length
            if (list1.Count != list2.Count)
            {
                return false;
            }

            // Step 2: Create dictionaries to store the word counts for both lists
            Dictionary<string, int> list1WordCount = new Dictionary<string, int>();
            Dictionary<string, int> list2WordCount = new Dictionary<string, int>();

            // Step 3: Count occurrences of each word in list1
            foreach (var word in list1)
            {
                if (list1WordCount.ContainsKey(word))
                {
                    list1WordCount[word]++;
                }
                else
                {
                    list1WordCount[word] = 1;
                }
            }

            // Step 4: Count occurrences of each word in list2
            foreach (var word in list2)
            {
                if (list2WordCount.ContainsKey(word))
                {
                    list2WordCount[word]++;
                }
                else
                {
                    list2WordCount[word] = 1;
                }
            }

            // Step 5: Compare the two dictionaries
            foreach (var word in list1WordCount)
            {
                if (!list2WordCount.ContainsKey(word.Key) || list2WordCount[word.Key] != word.Value)
                {
                    return false;
                }
            }

            // If all words have matching counts, return true
            return true;
        }

    }
}