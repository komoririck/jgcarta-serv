using System.Collections.Concurrent;
using System.Globalization;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class MainConditionedDrawResponseHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private PlayerRequest _ReturnData;

        public MainConditionedDrawResponseHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainConditionedDrawResponseHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelActionRecieved = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            List<object> ResponseObjList = JsonSerializer.Deserialize<List<object>>(_DuelActionRecieved.actionObject);

            DuelAction _ConditionedDraw = new()
            {

                SelectedCards = (List<string>)ResponseObjList[0],
                Order = (List<int>)ResponseObjList[0]
            };

            List<string> ChosedCardList = _ConditionedDraw.SelectedCards;

            List<Card> TempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

            //filtering the used card condition to draw
            bool canProguess = false;
            bool shouldUseTempHandValidation = false;
            string shouldUseToCompareWithTempHand = "";

            List<string> possibleDraw = new();
            switch (cMatchRoom.currentCardResolving)
            {
                case "hBP01-109":
                    possibleDraw.Add("兎田ぺこら");
                    possibleDraw.Add("ムーナ・ホシノヴァ");
                    shouldUseToCompareWithTempHand = "name";
                    canProguess = true;
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hSD01-018":
                    List<Record> limitedSuport = FileReader.QueryRecords(null, "サポート・アイテム・LIMITED", null, null);
                    limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・イベント・LIMITED", null, null));
                    limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・スタッフ・LIMITED", null, null));

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    canProguess = true;
                    shouldUseToCompareWithTempHand = "number";
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hBP01-104":
                    limitedSuport = FileReader.QueryRecords(null, null, "Debut", null);

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.Name);
                    }
                    canProguess = true;
                    shouldUseToCompareWithTempHand = "number";
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hBP01-102":
                    limitedSuport = FileReader.QueryRecords(null, null, null, null, "#歌");

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    canProguess = true;
                    shouldUseToCompareWithTempHand = "number";
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hSD01-021":
                    possibleDraw.Add("ときのそら");
                    possibleDraw.Add("AZKi");
                    shouldUseToCompareWithTempHand = "name";
                    canProguess = true;
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hBP01-111":
                    limitedSuport = FileReader.QueryRecords(null, null, null, null, "#ID3rdGrade"); //// REVIEW TAG

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    canProguess = true;
                    shouldUseToCompareWithTempHand = "number";
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hBP01-113":
                    limitedSuport = FileReader.QueryRecords(null, null, null, null, "#Promise"); //// REVIEW TAG

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    canProguess = true;
                    shouldUseToCompareWithTempHand = "number";
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(TempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hSD01-019":
                    limitedSuport = FileReader.QueryRecords(null, null, "1st", null);
                    limitedSuport.AddRange(FileReader.QueryRecords(null, null, "2nd", null));

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    shouldUseToCompareWithTempHand = "number";

                    if (TempHand.Count > 1)
                        canProguess = false;
                    else
                        canProguess = true;

                    break;
                case "hBP01-103":
                    limitedSuport = FileReader.QueryRecords(null, null, "1st", null);
                    limitedSuport.AddRange(FileReader.QueryRecords(null, null, "Debut", null));

                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }
                    shouldUseToCompareWithTempHand = "number";

                    if (TempHand.Count > 1)
                        canProguess = false;
                    else
                        canProguess = true;

                    break;
            }
            if (!canProguess)
                return;

            List<Card> AddToHand = new List<Card>() { new Card() { cardNumber = ChosedCardList[0] } };
            AddToHand[0].GetCardInfo(AddToHand[0].cardNumber);
            List<Card> ReturnToDeck = new();

            if (shouldUseTempHandValidation)
            {
                AddToHand = new();

                var comparer = StringComparer.Create(new CultureInfo("ja-JP"), true);

                for (int i = 0; i < TempHand.Count(); i++)
                {
                    string name = "";
                    TempHand[i].GetCardInfo(TempHand[i].cardNumber);


                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        name = TempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        name = TempHand[i].cardNumber;


                    foreach (string s in possibleDraw)
                    {
                        if (comparer.Equals(name, s)) //TempHand[i].name.Normalize().Equals(s.Normalize()))
                        {
                            AddToHand.Add(TempHand[i]);
                            continue;
                        }
                    }
                    ReturnToDeck.Add(TempHand[i]);
                }
                Lib.SortOrderToAddDeck(TempHand, _ConditionedDraw.Order);
            }

            DuelAction DrawReturn = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = AddToHand
            };
            _ReturnData = new PlayerRequest { type = "GamePhase", description = "SuporteEffectDrawXAddIfDone", requestObject = JsonSerializer.Serialize(DrawReturn, Lib.options) };

            if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
            {
                cMatchRoom.playerAHand.AddRange(AddToHand);
                cMatchRoom.playerADeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerATempHand.Clear();


                Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
                DrawReturn.cardList = cMatchRoom.FillCardListWithEmptyCards(DrawReturn.cardList);
                Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            }
            else
            {
                cMatchRoom.playerBHand.AddRange(AddToHand);
                cMatchRoom.playerBDeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerBTempHand.Clear();

                Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);
                DrawReturn.cardList = cMatchRoom.FillCardListWithEmptyCards(DrawReturn.cardList);
                Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            }
            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentGameHigh++;
            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
        }
    }
}