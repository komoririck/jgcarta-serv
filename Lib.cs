using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server
{
    public class Lib
    {
        static public JsonSerializerOptions options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        public static void WriteConsoleMessage(string s)
        {
            Console.WriteLine("\n" + s);

        }
        static public int getCardFromDeck(List<Card> deck, List<Card> target, int amount)
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

        static public async Task EndDuelAsync(MatchRoom cMatchRoom, string pickWinner = "")
        {
            string PlayerWinner = (string.IsNullOrEmpty(pickWinner)) ? cMatchRoom.currentPlayerTurn : pickWinner;
            string oponnent = GetOtherPlayer(cMatchRoom, PlayerWinner);

            DuelAction _duelaction = new() {
                actionType = "Victory",
                playerID = PlayerWinner,
            };

            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "Endduel", requestObject = JsonSerializer.Serialize(_duelaction, options) };

            await SendMessage(MessageDispatcher.playerConnections[PlayerWinner], _ReturnData);
            await SendMessage(MessageDispatcher.playerConnections[oponnent], _ReturnData);

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

        public static async Task SendPlayerData(MatchRoom cMatchRoom, bool reveal, DuelAction DuelActionResponse, string description)
        {
            string otherPlayer = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerB.PlayerID : cMatchRoom.playerA.PlayerID;

            // Serialize and send data to the current player
            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

            // Handle reveal logic and send data to the other player
            if (reveal == false)
                DuelActionResponse.cardList = cMatchRoom.FillCardListWithEmptyCards(DuelActionResponse.cardList);

            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };
            await Lib.SendMessage(MessageDispatcher.playerConnections[otherPlayer.ToString()], _ReturnData);
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

            duelAction.usedCard.GetCardInfo();

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
            duelAction.targetCard.GetCardInfo();

            //checking if can attach
            if (duelAction.usedCard.cardType.Equals("エール"))
            {
                if (stage != null)
                    if (duelAction.targetCard.cardNumber.Equals(stage.cardNumber) && duelAction.targetCard.cardPosition.Equals("Stage"))
                    {
                        stage.attachedEnergy.Add(duelAction.usedCard);
                        return true;
                    }

                if (collab != null)
                    if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.targetCard.cardPosition.Equals("Collaboration"))
                    {
                        collab.attachedEnergy.Add(duelAction.usedCard);
                        return true;
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
                                return true;
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


        static public async Task DefeatedHoloMemberAsync(List<Card> arquive, Card currentOponnentCard, MatchRoom cMatchRoom, string playerWhoDealDamage)
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

            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DefeatedHoloMember", requestObject = JsonSerializer.Serialize(_duelaction, options) };

            cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeated;

            //assign the values need to check if the user win the duel
            List<Card> attackedPlayerBackStage = _duelaction.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<Card> attackedPlayerLife = _duelaction.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerALife : cMatchRoom.playerBLife;

            // if the player didnt win the duel, awnser the player to get his new cheer
            SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
            SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);

            if (attackedPlayerLife.Count == 1)
            {
                _ = EndDuelAsync(cMatchRoom);
                return;
            }

            if (attackedPlayerBackStage.Count == 0 && ((otherPlayer.Equals(cMatchRoom.firstPlayer)) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration) == null)
            {
                _ = EndDuelAsync(cMatchRoom);
                return;
            }
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

        public string AssignCardToBackStage(List<bool> places, MatchRoom cMatchRoom)
        {
            Card collaborationCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            for (int i = 0; i < places.Count; i++)
            {
                if (!places[i])
                {
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                    {
                        collaborationCard.cardPosition = $"BackStage{i + 1}";
                        collaborationCard.suspended = true;
                        cMatchRoom.playerABackPosition.Add(collaborationCard);
                        cMatchRoom.playerACollaboration = null;
                    }
                    else
                    {
                        collaborationCard.cardPosition = $"BackStage{i + 1}";
                        collaborationCard.suspended = true;
                        cMatchRoom.playerBBackPosition.Add(collaborationCard);
                        cMatchRoom.playerBCollaboration = null;
                    }
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
                string locall = AssignCardToBackStage(places, cMatchRoom);
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

                PlayerRequest _ReturnData = new() { type = "DuelUpdate", description = "UnDoCollab", requestObject = JsonSerializer.Serialize(duelAction, options) };
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

        static public int CheckIfCardExistAtList(MatchRoom cMatchRoom, string playerId, string UsedCard, string list = "Hand")
        {
            int handPos = -1;
            List<Card> playerHand = null;
            switch (list)
            {
                case "Hand":
                    playerHand = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
                    break;
                case "Arquive":
                    playerHand = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                    break;
                case "Deck":
                    playerHand = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                    break;
                case "TempHand":
                    playerHand = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    break;
            }
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
        static public int CheckIfCardExistAtList(MatchRoom cMatchRoom, string playerId, string UsedCard, List<Card> list)
        {
            int handPos = -1;
            List<Card> playerHand = list;
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
        static public int CheckIfCardExistInPlayerBackStage(MatchRoom cMatchRoom, string playerId, Card card)
        {
            List<Card> playerBackStage = playerId.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            int handPos = -1;
            int handPosCounter = 0;
            foreach (Card inBackStage in playerBackStage)
            {
                if (inBackStage.cardNumber.Equals(card.cardNumber) && inBackStage.cardPosition.Equals(card.cardPosition))
                {
                    handPos = handPosCounter;
                    break;
                }
                handPosCounter++;
            }
            return handPos;
        }
        static public bool IsOddNumber(int x) {
            if ((x & 1) == 0)
                return false;
            else
                return true;
        }
        static public bool PayCardEffectCheerOrEquipCost(MatchRoom cMatchRoom, string zone, string cardNumber, bool ENERGY = true)
        {

            Card seletectedCard = null;

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


            List<Card> ListToDetach = null;

            if (ENERGY)
                ListToDetach = seletectedCard.attachedEnergy;
            else
                ListToDetach = seletectedCard.attachedEquipe;

            foreach (Card energy in ListToDetach)
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
                    playerID = cMatchRoom.currentPlayerTurn,
                    usedCard = new(ListToDetach[removePos].cardNumber, seletectedCard.cardPosition),
                };
                PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = ENERGY ? "RemoveEnergyAtAndSendToArquive" : "RemoveEquipAtAndSendToArquive", requestObject = JsonSerializer.Serialize(_DisposeAction, options) };
                SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);


                //adding the card that should be removed to the arquive, then removing from the player hand
                List<Card> tempArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                tempArquive.Add(ListToDetach[removePos]);

                if (ENERGY)
                    seletectedCard.attachedEnergy.RemoveAt(removePos);
                else
                    seletectedCard.attachedEquipe.RemoveAt(removePos);

                return true;
            }

            return false;
        }

        static public async Task<bool> TransferEnergyFromCardAToTargetAsync(MatchRoom cMatchRoom, Card CardA, Card Energy, DuelAction _DuelAction)
        {
            Card seletectedCard = null;

            switch (CardA.cardPosition)
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
                        if (card.cardPosition.Equals(CardA.cardPosition))
                            seletectedCard = card;
                    break;
            }

            int removePos = -1;
            int n = 0;
            foreach (Card energy in seletectedCard.attachedEnergy)
            {
                if (energy.cardNumber.Equals(Energy.cardNumber))
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
                    usedCard = new(seletectedCard.attachedEnergy[removePos].cardNumber, seletectedCard.cardPosition),
                };

                PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "RemoveEnergyAtAndDestroy", requestObject = JsonSerializer.Serialize(_DisposeAction, options) };
                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);

                //lets change duelaction just so the assign energy can work, maybe this need to be reworked to not use duelaction
                _DuelAction.usedCard = Energy;

                bool hasAttached = false;
                if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                    hasAttached = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition);
                else
                    hasAttached = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);

                seletectedCard.attachedEnergy.RemoveAt(removePos);

                if (!hasAttached)
                    return false;

                pReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
                return true;
            }
            return false;
        }

        static public void SwittchCardYToCardZButKeepPosition(MatchRoom cMatchRoom, string playerid, Card TargetPosition)
        {
            int backStagePos = Lib.CheckIfCardExistInPlayerBackStage(cMatchRoom, playerid, TargetPosition);

            if (!(TargetPosition.cardPosition.Equals("BackStage1") || TargetPosition.cardPosition.Equals("BackStage2") || TargetPosition.cardPosition.Equals("BackStage3") ||
                TargetPosition.cardPosition.Equals("BackStage4") || TargetPosition.cardPosition.Equals("BackStage5")))
            {
                Lib.WriteConsoleMessage("Invalid target position");
                return;
            }

            if (backStagePos < 0)
            {
                Lib.WriteConsoleMessage("Card targeted didnt exist");
                return;
            }

            // getting cards
            List<Card> playerBackstage = playerid == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            Card currentStageCard = playerid == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

            // backups
            Card backstageBackUP = new Card().CloneCard(playerBackstage[backStagePos]);
            Card stageBackUp = new Card().CloneCard(currentStageCard);

            // changing position
            backstageBackUP.cardPosition = currentStageCard.cardPosition; // Current stage position assigned to backstage card
            stageBackUp.cardPosition = playerBackstage[backStagePos].cardPosition; // Backstage position assigned to stage card

            // Assign back to the original references
            if (playerid == cMatchRoom.firstPlayer)
            {
                cMatchRoom.playerAStage = backstageBackUP; // Update the stage with the backup
            }
            else
            {
                cMatchRoom.playerBStage = backstageBackUP; // Update the stage with the backup
            }

            playerBackstage[backStagePos] = stageBackUp; // Update the player backstage       

        }

        internal static int GetDiceNumber(MatchRoom cMatchRoom, string actingPlayer, int min = 1, int max = 7, int Amount = 1)
        {
            Random random = new Random();
            int randomNumber = 0;
            List<int> diceRollList = actingPlayer.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
            int diceRollCount = actingPlayer.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollCount : cMatchRoom.playerBDiceRollCount;

            randomNumber = random.Next(min, max);

            CardEffect toRemove = null;

            for (int j = 0; j < Amount; j++)
            {

                int m = -1;
                int n = 0;

                foreach (CardEffect cardEffect in cMatchRoom.ActiveEffects)
                {
                    if (cardEffect.type == CardEffectType.FixedDiceRoll && actingPlayer.Equals(cardEffect.playerWhoUsedTheEffect))
                    {
                        randomNumber = cardEffect.diceRollValue;
                    }
                    else if (cardEffect.type == CardEffectType.OneUseFixedDiceRoll && actingPlayer.Equals(cardEffect.playerWhoUsedTheEffect))
                    {
                        randomNumber = cardEffect.diceRollValue;
                        m = n;
                    }
                    n++;
                }

                if (m != -1)
                    cMatchRoom.ActiveEffects.RemoveAt(m);

                diceRollList.Add(randomNumber);
                diceRollCount++;
            }

            return randomNumber;
        }
        internal static async Task SendDiceRollAsync(MatchRoom cMatchRoom, List<int> diceValue, bool COUNTFORRESONSE)
        {


            DuelAction response = new() { actionObject = JsonSerializer.Serialize(diceValue, options) };
            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = COUNTFORRESONSE ? "RollDice" : "OnlyDiceRoll", requestObject = JsonSerializer.Serialize(response, Lib.options) };

            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

        }

        internal static async Task RecoveryHPAsync(MatchRoom cMatchRoom, bool STAGE, bool COLLAB, bool BACKSTAGE, int RecoveryAmount, string targetPlayerID, string cardPosition = "")
        {
            // Determine which player is the target
            bool isFirstPlayer = cMatchRoom.firstPlayer.Equals(targetPlayerID);

            if (STAGE)
            {
                var targetStage = isFirstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                targetStage.currentHp = Math.Min(targetStage.currentHp + RecoveryAmount, int.Parse(targetStage.hp));

                DuelAction da = new()
                {
                    actionObject = RecoveryAmount.ToString(),
                    playerID = targetPlayerID,
                    targetCard = targetStage
                };
                PlayerRequest pReturnData = new PlayerRequest
                {
                    type = "DuelUpdate",
                    description = "RecoverHolomem",
                    requestObject = JsonSerializer.Serialize(da, Lib.options)
                };

                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
            }

            if (COLLAB)
            {
                var targetCollab = isFirstPlayer ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                targetCollab.currentHp = Math.Min(targetCollab.currentHp + RecoveryAmount, int.Parse(targetCollab.hp));

                DuelAction da = new()
                {
                    actionObject = RecoveryAmount.ToString(),
                    playerID = targetPlayerID,
                    targetCard = targetCollab
                };
                PlayerRequest pReturnData = new PlayerRequest
                {
                    type = "DuelUpdate",
                    description = "RecoverHolomem",
                    requestObject = JsonSerializer.Serialize(da, Lib.options)
                };

                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
            }

            if (BACKSTAGE)
            {
                var targetBackPositions = isFirstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                foreach (var position in targetBackPositions)
                {
                    if (!string.IsNullOrEmpty(cardPosition))
                        if (cardPosition.Equals(position.cardPosition))

                            position.currentHp = Math.Min(position.currentHp + RecoveryAmount, int.Parse(position.hp));

                    DuelAction da = new()
                    {
                        actionObject = RecoveryAmount.ToString(),
                        playerID = targetPlayerID,
                        targetCard = position
                    };
                    PlayerRequest pReturnData = new PlayerRequest
                    {
                        type = "DuelUpdate",
                        description = "RecoverHolomem",
                        requestObject = JsonSerializer.Serialize(da, Lib.options)
                    };

                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);
                }
            }
        }

        internal static void RecoveryHP(MatchRoom cMatchRoom, DuelAction duelaction, int RecoveryAmount)
        {
            if (duelaction.targetCard.cardPosition.Equals("Stage"))
                RecoveryHPAsync(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount, targetPlayerID: cMatchRoom.currentPlayerTurn);
            else if (duelaction.targetCard.cardPosition.Equals("Collaboration"))
                RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount, targetPlayerID: cMatchRoom.currentPlayerTurn);
            else
                RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount, targetPlayerID: cMatchRoom.currentPlayerTurn, duelaction.targetCard.cardPosition);
        }

        public static async Task UseCardEffectDrawXAddIfMatchCondition(MatchRoom cMatchRoom, List<Card> queryy, DuelAction _DuelAction, bool reveal = false)
        {

            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (var card in query)
                card.GetCardInfo();

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card(_DuelAction.usedCard.cardNumber),
                targetCard = _DuelAction.targetCard,
                cheerCostCard = _DuelAction.cheerCostCard,
                suffle = false,
                zone = "Deck",
                cardList = queryy
            };

            Lib.SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
        }

        public static void UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(MatchRoom cMatchRoom, int cNum, int HandMustHave, bool reveal = false)
        {

            if (cMatchRoom.firstPlayer == cMatchRoom.currentPlayerTurn)
            {
                if (cMatchRoom.playerADeck.Count < HandMustHave && HandMustHave > 0)
                    return;

                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerATempHand, cNum);
            }
            else
            {
                if (cMatchRoom.playerBDeck.Count < HandMustHave && HandMustHave > 0)
                    return;

                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBTempHand, cNum);
            }

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card(cMatchRoom.currentCardResolving),
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
            };

            Lib.SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
        }
        public static void UseCardEffectDrawAny(MatchRoom cMatchRoom, int cNum, string cUsedNumber)
        {
            if (cMatchRoom.firstPlayer == cMatchRoom.currentPlayerTurn)
                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, cNum);
            else
                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, cNum);

            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn.ToString(),
                usedCard = new Card(cUsedNumber),
                suffle = false,
                zone = "Deck",
                //getting the range of cards from the player hand, then getting the last ones to add to the draw
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
            };

            Lib.SendPlayerData(cMatchRoom, false, _Draw, "SupportEffectDraw");
        }
        public static void FromTheListAddFirstToHandThenAddRemainingToBottom(MatchRoom cMatchRoom, List<string> possibleDraw, DuelAction duelaction, bool shouldUseTempHandValidation, int pickedLimit, string shouldUseToCompareWithTempHand = "")
        {
            List<Card> TempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBTempHand;
            //List<Card> AddToHand = new List<Card>() { new Card() { cardNumber = duelaction.SelectedCards[0] } };
            //AddToHand[0].GetCardInfo(AddToHand[0].cardNumber);

            List<Card> ReturnToDeck = null;
            List<Card> AddToHand = null;

            int pickedCount = 0;

            if (shouldUseTempHandValidation)
            {
                for (int i = 0; i < TempHand.Count(); i++)
                {
                    string comparatingValue = "";
                    TempHand[i].GetCardInfo();

                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        comparatingValue = TempHand[i].cardName;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        comparatingValue = TempHand[i].cardNumber;

                    bool addToDeck = false;
                    foreach (string s in possibleDraw)
                    {
                        if (AddToHand.Any(item => item.cardNumber == comparatingValue) ||
                            ReturnToDeck.Any(item => item.cardNumber == comparatingValue))
                        {
                            continue;
                        }

                        if (comparatingValue.Equals(s) && pickedCount < pickedLimit)
                        {
                            if (AddToHand == null)
                                AddToHand = new() { TempHand[i] };
                            else
                                AddToHand.Add(TempHand[i]);

                            pickedCount++;
                            addToDeck = true;
                            continue;
                        }
                    }
                    if (!addToDeck)
                    {
                        if (ReturnToDeck == null)
                            ReturnToDeck = new() { TempHand[i] };
                        else
                            ReturnToDeck.Add(TempHand[i]);
                    }
                    addToDeck = false;
                }
                Lib.SortOrderToAddDeck(TempHand, duelaction.Order);
            }

            DuelAction DrawReturn = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = AddToHand
            };

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                cMatchRoom.playerAHand.AddRange(AddToHand);
                cMatchRoom.playerADeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerATempHand.Clear();
            }
            else
            {
                cMatchRoom.playerBHand.AddRange(AddToHand);
                cMatchRoom.playerBDeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerBTempHand.Clear();
            }

            Lib.SendPlayerData(cMatchRoom, false, DrawReturn, "SupportEffectDraw");
        }
        public static async Task MainConditionedSummomResponseHandleAsync(MatchRoom cMatchRoom, string playerid, string cardToSummom)
        {
            List<Card> backPosition = playerid.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            string local = "BackStage1";
            if (playerid.Equals(cMatchRoom.firstPlayer))
            {
                for (int j = 0; j < backPosition.Count; j++)
                {
                    if (!backPosition[j].cardPosition.Equals("BackStage1"))
                    {
                        local = "BackStage1";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage2"))
                    {
                        local = "BackStage2";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage3"))
                    {
                        local = "BackStage3";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage4"))
                    {
                        local = "BackStage4";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage5"))
                    {
                        local = "BackStage5";
                    }
                }
            }
            DuelAction _DuelActio = new()
            {
                usedCard = new Card(cardToSummom, local),
                playedFrom = "Deck",
                local = local,
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = true
            };
            backPosition.Add(_DuelActio.usedCard);

            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelActio, Lib.options) });
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelActio, Lib.options) });
            cMatchRoom.currentGameHigh++;
        }
        public static void UseCardEffectToSummom(MatchRoom cMatchRoom, string zone, string cUsedNumber, string bloomLevel)
        {
            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (var card in query)
                card.GetCardInfo();

            query = query.Where(r => r.bloomLevel == bloomLevel).ToList();

            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card() { cardNumber = cUsedNumber },
                suffle = false,
                zone = "Deck",
                cardList = query
            };

            Lib.SendPlayerData(cMatchRoom, false, _Draw, "ResolveOnSupportEffect");
        }
        public static bool MatchCardColors(Card card, Card Target)
        {
            if (card.color.Equals(Target.color) || card.color.Equals("白"))
                return true;
            return false;
        }

        public static bool IsSwitchBlocked(MatchRoom cMatchRoom, string zone)
        {
            foreach (CardEffect cardEffect in cMatchRoom.ActiveEffects)
            {
                if (cardEffect.type == CardEffectType.BlockRetreat && cardEffect.zoneTarget != zone)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool CanBeAttached(MatchRoom cMatchRoom, Card card, Card target)
        {

            switch (card.cardNumber)
            {
                case "hBP01-123":
                    if (target.cardName.Equals("兎田ぺこら"))
                        return true;
                    break;
                case "hBP01-122":
                    if (target.cardName.Equals("アキ・ローゼンタール"))
                        return true;
                    break;
                case "hBP01-126":
                    if (target.cardName.Equals("尾丸ポルカ"))
                        return true;
                    break;
                case "hBP01-125":
                    if (target.cardName.Equals("小鳥遊キアラ"))
                        return true;
                    break;
                case "hBP01-124":
                    if (target.cardName.Equals("AZKi") || target.cardName.Equals("SorAZ"))
                        return true;
                    break;
                case "hBP01-121":
                case "hBP01-120":
                case "hBP01-119":
                case "hBP01-118":
                case "hBP01-117":
                case "hBP01-115":
                case "hBP01-114":
                case "hBP01-116":
                    return !AlreadyAttachToThisHolomem(cMatchRoom, card.cardNumber, card.cardPosition);
            }
            return false;
        }
        public static bool CanBeAttachedToAnyInTheField(MatchRoom cMatchRoom, string playerid, Card usedCard)
        {
            bool IsAbleToAttach = false;
            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn.Equals(playerid);

            Card playerStage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            IsAbleToAttach = CanBeAttached(cMatchRoom, usedCard, playerStage);
            if (IsAbleToAttach)
                return IsAbleToAttach;

            Card playerCollab = ISFIRSTPLAYER ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
            IsAbleToAttach = CanBeAttached(cMatchRoom, usedCard, playerCollab);
            if (IsAbleToAttach)
                return IsAbleToAttach;

            List<Card> playerBackstage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            foreach (Card card in playerBackstage) 
            {
                IsAbleToAttach = CanBeAttached(cMatchRoom, usedCard, card);
                if (IsAbleToAttach)
                    return IsAbleToAttach;
            }
            return IsAbleToAttach;
        }
        static private bool AlreadyAttachToThisHolomem(MatchRoom cMatchRoom, string cardNumber, string cardPosition)
        {
            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> backStage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            Card stage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card collab = ISFIRSTPLAYER ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            if (cardPosition.Equals("Stage"))
            {
                foreach (Card card in stage.attachedEquipe)
                {
                    if (card.cardNumber.Equals(cardNumber))
                        return true;
                }
            }
            else if (cardPosition.Equals("Collaboration"))
            {
                foreach (Card card in collab.attachedEquipe)
                {
                    if (card.cardNumber.Equals(cardNumber))
                        return true;
                }
            }
            else
            {
                foreach (Card cardBs in backStage)
                {
                    foreach (Card card in cardBs.attachedEquipe)
                        if (card.cardNumber.Equals(cardNumber))
                            return true;
                }
            }
            return false;
        }
        public static string[] GetAreasThatContainsCardWithColorOrTagOrName(MatchRoom cMatchRoom, string playerID, string color = "", string tag = "", string name = "")
        {
            List<Card> allAttachments = new();
            List<string> list = new();

            allAttachments.AddRange(cMatchRoom.firstPlayer.Equals(playerID) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition);
            allAttachments.Add(cMatchRoom.firstPlayer.Equals(playerID) ? cMatchRoom.playerAStage : cMatchRoom.playerBStage);
            allAttachments.Add(cMatchRoom.firstPlayer.Equals(playerID) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration);

            if (!string.IsNullOrEmpty(color))
            {
                foreach (Card card in allAttachments)
                    if (card.color.Equals(color))
                        list.Add(card.cardPosition);
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                foreach (Card card in allAttachments)
                    if (card.cardTag.Contains(tag))
                        list.Add(card.cardPosition);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                foreach (Card card in allAttachments)
                    if (card.cardName.Contains(name))
                        list.Add(card.cardPosition);
            }

            return list.ToArray();
        }
    }
}
