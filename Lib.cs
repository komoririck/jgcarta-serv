using hololive_oficial_cardgame_server.SerializableObjects;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server
{
    public class Lib
    {
        static public JsonSerializerOptions jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        static public void DefeatedHoloMemberAsync(List<Card> arquive, Card currentOponnentCard, MatchRoom cMatchRoom, string playerWhoDealDamage)
        {
            cMatchRoom.cheersAssignedThisChainTotal = GetDownneedCheerAmount(currentOponnentCard.cardNumber);

            if (cMatchRoom.cheersAssignedThisChainTotal > 1)
            {
                cMatchRoom.cheersAssignedThisChainAmount = 0;
            }

            //adding cards from the deafeat holomem to the arquive
            arquive.AddRange(currentOponnentCard.attachedEnergy);
            arquive.AddRange(currentOponnentCard.bloomChild);
            arquive.Add(currentOponnentCard);


            string otherPlayer = GetOtherPlayer(cMatchRoom, playerWhoDealDamage);

            if (otherPlayer.Equals(cMatchRoom.firstPlayer))
            {
                if (currentOponnentCard.cardPosition.Equals("Stage"))
                {
                    cMatchRoom.playerAStage = null;
                }
                else if (currentOponnentCard.cardPosition.Equals("Collaboration"))
                {
                    cMatchRoom.playerACollaboration = null;
                }
            }
            else
            {
                if (currentOponnentCard.cardPosition.Equals("Stage"))
                {
                    cMatchRoom.playerBStage = null;

                }
                else if (currentOponnentCard.cardPosition.Equals("Collaboration"))
                {
                    cMatchRoom.playerBCollaboration = null;
                }
            }

            DuelAction _duelaction = new DuelAction()
            {
                playerID = otherPlayer,
                targetCard = currentOponnentCard,
            };

            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DefeatedHoloMember", requestObject = JsonSerializer.Serialize(_duelaction, jsonOptions) };

            cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeated;

            //assign the values need to check if the user win the duel
            List<Card> attackedPlayerBackStage = _duelaction.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<Card> attackedPlayerLife = _duelaction.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerALife : cMatchRoom.playerBLife;

            // if the player didnt win the duel, awnser the player to get his new cheer
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), playerRequest: _ReturnData));
            cMatchRoom.PushPlayerAnswer();

            if (attackedPlayerLife.Count == 1)
            {
                EndDuelAsync(cMatchRoom);
                return;
            }

            if (attackedPlayerBackStage.Count == 0 && ((otherPlayer.Equals(cMatchRoom.firstPlayer)) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration) == null)
            {
                EndDuelAsync(cMatchRoom);
                return;
            }
        }
        static public void EndDuelAsync(MatchRoom cMatchRoom, string pickWinner = "")
        {
            string PlayerWinner = (string.IsNullOrEmpty(pickWinner)) ? cMatchRoom.currentPlayerTurn : pickWinner;
            string oponnent = GetOtherPlayer(cMatchRoom, PlayerWinner);

            DuelAction _duelaction = new()
            {
                actionType = "Victory",
                playerID = PlayerWinner,
            };

            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "Endduel", requestObject = JsonSerializer.Serialize(_duelaction, jsonOptions) };
            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), playerRequest: _ReturnData));
            cMatchRoom.PushPlayerAnswer();

            //we need to update the socket, so the players can be paired or enter the pool again
            bool isPlayersnew = new DBConnection().SetWinnerForMatch(PlayerWinner, oponnent);

            if (!isPlayersnew)
                throw new Exception("Error while removing players from the lock status in the database");

            //continue to remove players from the websocketlist
            try
            {
                if (cMatchRoom != null)
                {
                    cMatchRoom.StopTimer(cMatchRoom.firstPlayer);
                    cMatchRoom.StopTimer(cMatchRoom.secondPlayer);
                    MatchRoom.RemoveRoom(cMatchRoom);
                }

                if (MessageDispatcher.playerConnections.TryGetValue(PlayerWinner.ToString(), out var currentPlayerConnection))
                {
                    try
                    {
                        if (currentPlayerConnection.State != WebSocketState.Closed) { }
                    }
                    catch (WebSocketException ex)
                    {
                        // Handle the exception, log it, or notify the user
                        Debug.WriteLine($"Error closing connection for {PlayerWinner}: {ex.Message}");
                    }
                    MessageDispatcher.playerConnections[PlayerWinner.ToString()].Dispose();
                    MessageDispatcher.playerConnections.TryRemove(PlayerWinner.ToString(), out _);
                }

                if (MessageDispatcher.playerConnections.TryGetValue(oponnent.ToString(), out var opponentConnection))
                {
                    try
                    {
                        if (opponentConnection.State != WebSocketState.Closed) { }
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
        public static void WriteConsoleMessage(string s)
        {
            Console.WriteLine("\n" + s);

        }
        static public int MoveTopCardFromXToY(List<Card> deck, List<Card> target, int amount)
        {
            if (deck.Count >= amount)
            {
                if (deck.Count < amount)
                    amount = deck.Count;

                for (int i = 0; i < amount; i++)
                {
                    target.Add(deck[deck.Count - 1]);
                    if (deck[deck.Count - 1] != null)
                        deck.RemoveAt(deck.Count - 1);
                    else
                        return i - amount;

                }
                return amount;
            }
            return amount;
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
        public static void SortOrderToAddDeck(List<Card> cardList, List<int> numberList)
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
        static private int GetDownneedCheerAmount(string cardNumber)
        {
            switch (cardNumber)
            {
                case "hSD01-006":
                    return 2;
                default:
                    return 1;
            }
        }
        public List<bool> GetBackStageAvailability(List<Card> backPosition)
        {
            List<bool> places = new List<bool> { false, false, false, false, false };
            foreach (Card _card in backPosition)
            {
                switch (_card.cardPosition)
                {
                    case "BackStage1":
                        places[0] = true;
                        break;
                    case "BackStage2":
                        places[1] = true;
                        break;
                    case "BackStage3":
                        places[2] = true;
                        break;
                    case "BackStage4":
                        places[3] = true;
                        break;
                    case "BackStage5":
                        places[4] = true;
                        break;
                }
            }
            return places;
        }
        static public bool IsOddNumber(int x) {
            if ((x & 1) == 0)
                return false;
            else
                return true;
        }
        public static bool MatchCardColors(Card card, Card Target)
        {
            if (card.color.Equals(Target.color) || card.color.Equals("白"))
                return true;
            return false;
        }

    }
}
