﻿using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server
{
    public class Lib
    {
        static public JsonSerializerOptions options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public static void WriteConsoleMessage(string s)
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
                        return i - amount;

                }
                return amount;
            }
            return amount;
        }

        /*
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
        */



        static public async Task EndDuelAsync(MatchRoom matchRoom, string pickWinner = "")
        {

            string currentPlayerTurn = matchRoom.currentPlayerTurn;
            if (string.IsNullOrEmpty(pickWinner))
                currentPlayerTurn = pickWinner;

            string oponnent = GetOtherPlayer(matchRoom, currentPlayerTurn);
            DuelAction _duelaction = new();
            _duelaction.actionType = "Victory";
            _duelaction.playerID = currentPlayerTurn.ToString();

            PlayerRequest _ReturnData = new PlayerRequest { type = "GamePhase", description = "Endduel", requestObject = JsonSerializer.Serialize(_duelaction, options) };


            await SendMessage(MessageDispatcher.playerConnections[currentPlayerTurn.ToString()], _ReturnData);
            await SendMessage(MessageDispatcher.playerConnections[oponnent.ToString()], _ReturnData);

            //we need to update the socket, so the players can be paired or enter the pool again
            bool isPlayersnew = new DBConnection().SetWinnerForMatch(currentPlayerTurn, oponnent);

            if (!isPlayersnew)
                throw new Exception("Error while removing players from the lock status in the database");

            //continue to remove players from the websocketlist
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

        public static async Task SendMessage(WebSocket webSocket, PlayerRequest data, [CallerMemberName] string callerName = "")
        {

            WriteConsoleMessage("Send to " + GetKeyByValue(webSocket) + ":\n" + $"{callerName}\n" + JsonSerializer.Serialize(data, options).Replace("\\u0022", "\"") + ":\n");

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

        public static bool AssignEnergyToZoneAsync(DuelAction duelAction, MatchRoom matchRoom, Card stage = null, Card collab = null, List<Card> backStage = null)
        {
            //THIS FUNCTION DOESNOT MAKE VALIDATIONS IF CAN BE PLAYED OR REMOTIONS, ONLY ATTACH IF MATCH THE POSTION/NAME

            //validating usedCard
            if (duelAction.usedCard == null)
            {
                WriteConsoleMessage("usedCard is null at AssignEnergyToZoneAsync");
                return false;
            }

            duelAction.usedCard.GetCardInfo(duelAction.usedCard.cardNumber);

            if (string.IsNullOrEmpty(duelAction.usedCard.cardType))
            {
                WriteConsoleMessage("usedCard.cardType is empty at AssignEnergyToZoneAsync");
                return false;
            }

            //validating targetCard
            if (duelAction.targetCard == null)
            {
                WriteConsoleMessage("targetCard is null at AssignEnergyToZoneAsync");
                return false;
            }
            duelAction.targetCard.GetCardInfo(duelAction.targetCard.cardNumber);

            //checking if can attach
            bool hasAttached = false;
            if (duelAction.usedCard.cardType.Equals("エール"))
            {
                if (stage != null)
                    if (duelAction.targetCard.cardNumber.Equals(stage.cardNumber) && duelAction.targetCard.cardPosition.Equals("Stage"))
                    {
                        stage.attachedEnergy.Add(duelAction.usedCard);
                        return hasAttached = true;
                    }

                if (collab != null)
                    if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.targetCard.cardPosition.Equals("Collaboration"))
                    {
                        collab.attachedEnergy.Add(duelAction.usedCard);
                        return hasAttached = true;
                    }

                if (backStage != null)
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
                // fallied to find the target to assign the energy
                WriteConsoleMessage($"Error: failled to assign the energy at {duelAction.local}.");
            }
            else
            {
                WriteConsoleMessage("Error: used card is not a cheer.");
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


        static public async Task DefeatedHoloMemberAsync(List<Card> arquive, Card currentOponnentCard, MatchRoom cMatchRoom, bool result, DuelAction _Duelaction)
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

            DuelAction _duelaction = _Duelaction;
            _duelaction.actionType = "DefeatedHoloMember";
            _duelaction.playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn);
            _duelaction.targetCard = currentOponnentCard;


            string otherPlayer = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn);

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

            PlayerRequest _ReturnData = new PlayerRequest { type = "GamePhase", description = "DefeatedHoloMember", requestObject = JsonSerializer.Serialize(_duelaction, options) }; /// mudei o targeto aqui de _DuelAction para duelaction, conferir dps

            cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeated;

            //assign the values need to check if the user win the duel
            List<Card> attackedPlayerBackStage = _duelaction.playerID == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<Card> attackedPlayerLife = _duelaction.playerID == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerALife : cMatchRoom.playerBLife;

            if (attackedPlayerLife.Count == 1)
            {
                _ = EndDuelAsync(cMatchRoom);
                return;
            }

            if (attackedPlayerBackStage.Count == 0)
            {
                _ = EndDuelAsync(cMatchRoom);
                return;
            }
            // if the player didnt win the duel, awnser the player to get his new cheer
            SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
            SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);
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

        public string AssignCardToBackStage(List<bool> places, List<Card> backPosition, Card collaborationCard)
        {
            for (int i = 0; i < places.Count; i++)
            {
                if (!places[i])
                {
                    collaborationCard.cardPosition = $"BackStage{i + 1}";
                    backPosition.Add(collaborationCard);
                    return $"BackStage{i + 1}";
                }
            }
            return "failToAssignToBackStage";
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

        public bool ReturnCollabToBackStage(MatchRoom cMatchRoom)
        {
            Card currentStageCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
            List<Card> currentBackStageCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            if (!string.IsNullOrEmpty(currentCollabCardd.cardNumber))
            {
                //try to assign the card to the back position
                List<bool> places = GetBackStageAvailability(currentBackStageCardd);
                string locall = AssignCardToBackStage(places, currentBackStageCardd, currentCollabCardd);
                if (locall.Equals("failToAssignToBackStage"))
                {
                    WriteConsoleMessage("Error assign the card to the backposition");
                    return false;
                }

                DuelAction duelAction = new DuelAction
                {
                    playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer,
                    usedCard = currentCollabCardd,
                    playedFrom = "Collaboration",
                    actionType = "EffectUndoCollab"
                };
                duelAction.usedCard.cardPosition = locall;
                currentCollabCardd = null;

                PlayerRequest _ReturnData = new() { type = "GamePhase", description = "UnDoCollab", requestObject = JsonSerializer.Serialize(duelAction, options) };
                SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
                SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);
            }
            return true;
        }

        internal static void PrintPlayerHand(MatchRoom matchRoom)
        {
            string frase = "";
            frase += $"Player:{matchRoom.firstPlayer}-";
            foreach (Card card in matchRoom.playerAHand)
            {
                frase += $"{card.cardNumber}-";
            }
            frase += $"\nPlayer:{matchRoom.secondPlayer}-";
            foreach (Card card in matchRoom.playerBHand)
            {
                frase += $"{card.cardNumber}-";
            }
            WriteConsoleMessage(frase);
        }

        static public async Task AddTopDeckToDrawObjectAsync(string playerid, List<Card> PlayerHand, bool result, MatchRoom mr, PlayerRequest ReturnData)
        {
            DuelAction newDraw = new DuelAction();
            newDraw.playerID = playerid;
            newDraw.zone = "Deck";

            newDraw.cardList = new List<Card>() { PlayerHand[PlayerHand.Count - 1] };
            ReturnData.requestObject = JsonSerializer.Serialize(newDraw, options);

            SendMessage(MessageDispatcher.playerConnections[newDraw.playerID.ToString()], ReturnData);

            newDraw.cardList = new List<Card>() { new Card() };
            ReturnData.requestObject = JsonSerializer.Serialize(newDraw, options);
            SendMessage(MessageDispatcher.playerConnections[GetOtherPlayer(mr, newDraw.playerID).ToString()], ReturnData);
        }

        static public int CheckIfCardExistInPlayerHand(MatchRoom cMatchRoom, string playerId, string UsedCard)
        {
            List<Card> playerHand = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            int handPos = -1;
            int handPosCounter = 0;
            foreach (Card inHand in playerHand)
            {
                if (inHand.cardNumber.Equals(UsedCard))
                {
                    handPos = handPosCounter;
                    break;
                }
                handPosCounter++;
            }
            return handPos;
        }

        static public bool PayCardEffectCheerFieldCost(MatchRoom cMatchRoom, string zone, string cardNumber)
        {

            Card seletectedCard = new();

            switch (zone)
            {
                case "Favourite":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAFavourite : cMatchRoom.playerBFavourite;
                    break;
                case "Collaboration":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                    break;
                case "Stage":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":
                    List<Card> seletectedCardList;
                    seletectedCardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                    foreach (Card card in seletectedCardList)
                        if (card.cardPosition.Equals(zone))
                            seletectedCard = card;
                    break;
            }

            int removePos = -1;
            int n = 0;
            foreach (Card energy in seletectedCard.attachedEnergy)
            {
                if (energy.cardNumber.Equals(cardNumber))
                {
                    removePos = n;
                    break;
                }
                n++;
            }

            if (removePos > -1)
            {

                DuelAction _DisposeAction = new()
                {
                    playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                    usedCard = new() { cardNumber = seletectedCard.attachedEnergy[removePos].cardNumber },
                };
                PlayerRequest pReturnData = new PlayerRequest { type = "GamePhase", description = "DisposeUsedSupport", requestObject = JsonSerializer.Serialize(_DisposeAction, options) };
                SendMessage(MessageDispatcher.playerConnections[GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);


                //adding the card that should be removed to the arquive, then removing from the player hand
                List<Card> tempArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                tempArquive.Add(seletectedCard.attachedEnergy[removePos]);
                seletectedCard.attachedEnergy.RemoveAt(removePos);

                return true;
            }

            return false;
        }

    }
}