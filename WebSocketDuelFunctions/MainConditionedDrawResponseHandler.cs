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
        private PlayerRequest _ReturnData;

        internal async Task MainConditionedDrawResponseHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelActionRecieved = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            List<object> ResponseObjList = JsonSerializer.Deserialize<List<object>>(_DuelActionRecieved.actionObject);

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;
            string playerId = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = ISFIRSTPLAYER ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            Card playerStage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> playerBackstage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            DuelAction _ConditionedDraw = new()
            {

                SelectedCards = (List<string>)ResponseObjList[0],
                Order = (List<int>)ResponseObjList[0]
            };

            List<string> ChosedCardList = _ConditionedDraw.SelectedCards;
            Lib.SortOrderToAddDeck(playerTempHand, _ConditionedDraw.Order);

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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
                    shouldUseTempHandValidation = true;
                    break;
                case "hSD01-021":
                    possibleDraw.Add("ときのそら");
                    possibleDraw.Add("AZKi");
                    shouldUseToCompareWithTempHand = "name";
                    canProguess = true;
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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
                    canProguess = Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), ChosedCardList);
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

                    if (playerTempHand.Count > 1)
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

                    if (playerTempHand.Count > 1)
                        canProguess = false;
                    else
                        canProguess = true;

                    break;
            }
            if (!canProguess)
                return;

            List<Card> AddToHand = new List<Card>() { new Card(ChosedCardList[0]) };
            List<Card> ReturnToDeck = new();

            if (shouldUseTempHandValidation)
            {
                AddToHand = new();

                var comparer = StringComparer.Create(new CultureInfo("ja-JP"), true);

                for (int i = 0; i < playerTempHand.Count(); i++)
                {
                    string name = "";
                    playerTempHand[i].GetCardInfo();


                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        name = playerTempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        name = playerTempHand[i].cardNumber;


                    foreach (string s in possibleDraw)
                    {
                        if (comparer.Equals(name, s))
                        {
                            AddToHand.Add(playerTempHand[i]);
                            continue;
                        }
                    }
                    ReturnToDeck.Add(playerTempHand[i]);
                }
            }

            DuelAction DrawReturn = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = AddToHand
            };

            playerHand.AddRange(AddToHand);
            playerDeck.InsertRange(0, ReturnToDeck);
            playerTempHand.Clear();

            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(playerId), hidden: true, type : "DuelUpdate", description : "SuporteEffectDrawXAddIfDone", duelAction: DrawReturn));
            cMatchRoom.PushPlayerAnswer();

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentGameHigh++;
            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
        }
    }
}