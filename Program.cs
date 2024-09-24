using hololive_oficial_cardgame_server;
using Microsoft.OpenApi.Extensions;
using MySqlX.XDevAPI.Common;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Transactions;
using static hololive_oficial_cardgame_server.MatchRoom;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the base URL for the application
var baseUrl = builder.Configuration.GetValue<string>("ASPNETCORE_URLS") ?? "http://localhost:5000";
var webSocketPath = "/ws";

// Print the WebSocket URL
var webSocketUrl = $"{baseUrl}{webSocketPath}";
Console.WriteLine($"Listening for WebSocket connections at: {webSocketUrl}");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// WebSocket handling middleware
var playerConnections = new ConcurrentDictionary<string, WebSocket>();

//Players DuelRoom
List<MatchRoom> _MatchRooms = new List<MatchRoom>();

List<Record> CarList = FileReader.ReadFile("CardList.xlsx");

// Add WebSocket handling middleware before other middlewares
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == webSocketPath && context.Request.Headers["Upgrade"] == "websocket")
    {
        await HandleWebSocketAsync(context, playerConnections);
    }
    else
    {
        await next(); // Ensure this is called if it's not a WebSocket request
    }
});




async Task HandleWebSocketAsync(HttpContext context, ConcurrentDictionary<string, WebSocket> playerConnections)
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];
        PlayerRequest playerRequest = null;

        try
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            _ = Task.Run(async () =>
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    await Task.Delay(10000, token);
                    if (webSocket.State != WebSocketState.Open)
                        break;
                }
                playerConnections.TryRemove(playerRequest.playerID, out _);
            });

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocket server", CancellationToken.None);
                    try {
                        playerConnections.TryRemove(playerRequest.playerID, out _);

                    } catch (Exception ex) { Debug.WriteLine(ex); }
                    //process victory
                    int matchnumber = MatchRoom.FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
                    _MatchRooms.Remove(_MatchRooms[matchnumber]);
                }
                else
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Deserialize the received JSON message
                    playerRequest = JsonSerializer.Deserialize<PlayerRequest>(receivedMessage);
                    string print = receivedMessage.Replace("\\u0022", "\"").Replace("\\", "");
                    Console.WriteLine($"Received: {print}" + "\n\n");

                    if (playerRequest != null && ValidatePlayerRequest(playerRequest))
                    {
                        Draw draw;
                        Draw drawDummy;
                        List<Record> cardlist;
                        DuelFieldData _DuelFieldDataA = new();
                        DuelFieldData _DuelFieldDataB = new();
                        RequestData pReturnData = new();
                        bool matchfound = false;
                        switch (playerRequest.requestData.type)
                        {
                            case "JoinPlayerQueueList":

                                if (playerConnections.ContainsKey(playerRequest.playerID))
                                {
                                    break;
                                }

                                // Add the WebSocket connection to the dictionary with player ID as key
                                playerConnections[playerRequest.playerID] = webSocket;

                                // create the user response
                                RequestData returnData = new() { type = "Waitingforopponent", description = "Waitingforopponent", requestObject = "" };

                                // Send the data as a text message
                                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(returnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);


                                // Add the WebSocket connection to the dictionary with player ID as key
                                playerConnections[playerRequest.playerID] = webSocket;
                                List<PlayerInfo> pList = new DBConnection().CheckForAvaliablePlayers();

                                if (pList.Count < 2)
                                {
                                    returnData.type = "processing";
                                    returnData.description = "cod:422446";
                                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(returnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    break;

                                }

                                if (pList[0].PlayerID != int.Parse(playerRequest.playerID) && pList[1].PlayerID != int.Parse(playerRequest.playerID))
                                {
                                    returnData.type = "error";
                                    returnData.description = "cod:651152";
                                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(returnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    break;
                                }

                                if (!(playerConnections.ContainsKey(pList[0].PlayerID.ToString()) && playerConnections.ContainsKey(pList[1].PlayerID.ToString())))
                                {
                                    returnData.type = "error";
                                    returnData.description = "cod:694353";
                                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(returnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    break;
                                }

                                // since we passed the verification, players are ready in the base and in the socket
                                pReturnData = new RequestData { type = "matchFound", description = "matchFound", requestObject = "" };
                                await playerConnections[pList[0].PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                await playerConnections[pList[1].PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                List<List<Card>> decksinfo = new DBConnection().GetMatchPlayersDeck(pList[0].PlayerID, pList[1].PlayerID);
                                if (!(decksinfo.Count == 6))
                                {
                                    Console.WriteLine($"not able to get players decks");
                                    break;
                                }

                                //validing if players exist in the queue table
                                /*
                                if (!(new DBConnection().CofirmMatchForAvaliablePlayers(pList[0].PlayerID, pList[1].PlayerID))) {
                                    Console.WriteLine($"not able to update players");
                                    break;
                                }*/

                                //creating the players matchroom
                                MatchRoom _MatchRoom = new MatchRoom
                                {
                                    playerA = pList[0],
                                    playerB = pList[1],
                                    startPlayer = pList[0].PlayerID,
                                    currentPlayerActing = pList[0].PlayerID,
                                    currentPlayerTurn = pList[0].PlayerID,
                                    firstPlayer = pList[0].PlayerID,
                                    secondPlayer = pList[1].PlayerID,
                                    currentGamePhase = GAMEPHASE.StartMatch,
                                    nextGamePhase = GAMEPHASE.DrawStep,
                                    playerADeck = decksinfo[0],
                                    playerACardCheer = decksinfo[1],
                                    playerAOshi = decksinfo[2][0],
                                    playerBDeck = decksinfo[3],
                                    playerBCardCheer = decksinfo[4],
                                    playerBOshi = decksinfo[5][0],
                                    playerAHand = new List<Card>(),
                                    playerAArquive = new List<Card>(),
                                    playerAHoloPower = new List<Card>(),
                                    playerABackPosition = new List<Card>(),
                                    playerBHand = new List<Card>(),
                                    playerBArquive = new List<Card>(),
                                    playerBHoloPower = new List<Card>(),
                                    playerBBackPosition = new List<Card>(),
                                };

                                // SUFFLE ENERGY
                                _MatchRoom.playerACardCheer = _MatchRoom.ShuffleCards(_MatchRoom.playerACardCheer);
                                _MatchRoom.playerBCardCheer = _MatchRoom.ShuffleCards(_MatchRoom.playerBCardCheer);
                                //SUFFLE DECK
                                _MatchRoom.playerADeck = _MatchRoom.ShuffleCards(_MatchRoom.playerADeck);
                                _MatchRoom.playerBDeck = _MatchRoom.ShuffleCards(_MatchRoom.playerBDeck);

                                _DuelFieldDataA = new DuelFieldData
                                {
                                    playerAHand = new List<Card>(),
                                    playerAArquive = new List<Card>(),
                                    playerADeck = _MatchRoom.FillCardListWithEmptyCards(decksinfo[0]),
                                    playerAHoloPower = new List<Card>(),
                                    playerABackPosition = new List<Card>(),
                                    playerAFavourite = decksinfo[2][0],
                                    playerAStage = new Card(),
                                    playerACollaboration = new Card(),
                                    playerACardCheer = _MatchRoom.FillCardListWithEmptyCards(decksinfo[1]),
                                    playerALife = new List<Card>(),

                                    playerBHand = new List<Card>(),
                                    playerBArquive = new List<Card>(),
                                    playerBDeck = _MatchRoom.FillCardListWithEmptyCards(decksinfo[3]),
                                    playerBHoloPower = new List<Card>(),
                                    playerBBackPosition = new List<Card>(),
                                    playerBFavourite = new Card(),
                                    playerBStage = new Card(),
                                    playerBCollaboration = new Card(),
                                    playerBCardCheer = _MatchRoom.FillCardListWithEmptyCards(decksinfo[4]),
                                    playerBLife = new List<Card>(),

                                    currentTurn = 0,
                                    currentPlayerTurn = pList[0].PlayerID,
                                    currentPlayerActing = pList[0].PlayerID,
                                    currentGamePhase = ((int)GAMEPHASE.StartMatch),
                                    firstPlayer = pList[0].PlayerID,
                                    secondPlayer = pList[1].PlayerID,
                                    currentGameHigh = 0
                                };

                                _DuelFieldDataB = new DuelFieldData
                                {
                                    playerAHand = new List<Card>(),
                                    playerAArquive = new List<Card>(),
                                    playerADeck = _MatchRoom.FillCardListWithEmptyCards(decksinfo[0]),
                                    playerAHoloPower = new List<Card>(),
                                    playerABackPosition = new List<Card>(),
                                    playerAFavourite = new Card(),
                                    playerAStage = new Card(),
                                    playerACollaboration = new Card(),
                                    playerACardCheer = _MatchRoom.FillCardListWithEmptyCards(decksinfo[1]),
                                    playerALife = new List<Card>(),

                                    playerBHand = new List<Card>(),
                                    playerBArquive = new List<Card>(),
                                    playerBDeck = _MatchRoom.FillCardListWithEmptyCards(decksinfo[3]),
                                    playerBHoloPower = new List<Card>(),
                                    playerBBackPosition = new List<Card>(),
                                    playerBFavourite = decksinfo[5][0],
                                    playerBStage = new Card(),
                                    playerBCollaboration = new Card(),
                                    playerBCardCheer = _MatchRoom.FillCardListWithEmptyCards(decksinfo[4]),
                                    playerBLife = new List<Card>(),

                                    currentTurn = 0,
                                    currentPlayerTurn = pList[0].PlayerID,
                                    currentPlayerActing = pList[0].PlayerID,
                                    currentGamePhase = ((int)GAMEPHASE.StartMatch),
                                    firstPlayer = pList[0].PlayerID,
                                    secondPlayer = pList[1].PlayerID,
                                    currentGameHigh = 0
                                };


                                //since we were able to update the users table to lock the match, send both players to the match
                                pReturnData = new RequestData { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataA) };
                                await playerConnections[pList[0].PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                pReturnData = new RequestData { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataB) };
                                await playerConnections[pList[1].PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);


                                _MatchRoom.currentGameHigh = 1;
                                //END OF LOADING GAME SCENE


                                //GET PLAYERS STARTER HAND
                                getCardFromDeck(_MatchRoom.playerADeck, _MatchRoom.playerAHand, 7);
                                getCardFromDeck(_MatchRoom.playerBDeck, _MatchRoom.playerBHand, 7);


                                //BEGIN - SEND FIRST PLAYER STARTER HAND 
                                draw = new Draw()
                                {
                                    playerID = _MatchRoom.startPlayer,
                                    suffle = false,
                                    zone = "Deck",
                                    cardList = _MatchRoom.playerAHand
                                };
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw) };
                                Debug.WriteLine(pReturnData.requestObject);
                                await playerConnections[_MatchRoom.startPlayer.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                draw.cardList = _MatchRoom.FillCardListWithEmptyCards(_MatchRoom.playerAHand);
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw) };
                                Debug.WriteLine(pReturnData.requestObject);
                                await playerConnections[GetOtherPlayer(_MatchRoom, _MatchRoom.startPlayer).ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                _MatchRoom.currentGameHigh = 2;

                                //P2 START HAND
                                draw = new Draw()
                                {
                                    playerID = GetOtherPlayer(_MatchRoom, _MatchRoom.startPlayer),
                                    suffle = false,
                                    zone = "Deck",
                                    cardList = _MatchRoom.playerBHand
                                };

                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw) };
                                Debug.WriteLine(pReturnData.requestObject);
                                await playerConnections[_MatchRoom.playerB.PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                draw.cardList = _MatchRoom.FillCardListWithEmptyCards(_MatchRoom.playerBHand);
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw) };
                                Debug.WriteLine(pReturnData.requestObject);
                                await playerConnections[_MatchRoom.playerA.PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                _MatchRoom.currentGameHigh = 3;

                                _MatchRoom.playerAGameHigh = _MatchRoom.currentGameHigh;
                                _MatchRoom.playerBGameHigh = _MatchRoom.currentGameHigh;
                                //END - SEND FIRST PLAYER STARTER HAND 

                                _MatchRooms.Add(_MatchRoom);

                                break;
                            default://gambiarra para pendurar a websocket
                                matchfound = true;
                                break;
                        }
                        if (matchfound)
                        {
                            //////////////////////
                            //ROOM ALREADY EXISTS
                            //////////////////////

                            int PlayersRoomId = FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
                            MatchRoom cMatchRoom = _MatchRooms[PlayersRoomId];
                            int playerA = _MatchRooms[PlayersRoomId].startPlayer;
                            int playerB = _MatchRooms[PlayersRoomId].secondPlayer;
                            pReturnData = new RequestData();

                            switch (playerRequest.requestData.type)
                            {
                                case "AskForMulligan":
                                    // For Player A
                                    if (int.Parse(playerRequest.playerID) == playerA && !cMatchRoom.PAMulliganAsked)
                                    {
                                        await HandleMulligan(cMatchRoom, true, playerRequest, playerConnections[playerA.ToString()], playerConnections[playerB.ToString()]);
                                        cMatchRoom.PAMulliganAsked = true;
                                        cMatchRoom.currentGameHigh++;
                                    }

                                    // For Player B
                                    if (int.Parse(playerRequest.playerID) != playerA && !cMatchRoom.PBMulliganAsked)
                                    {
                                        await HandleMulligan(cMatchRoom, false, playerRequest, playerConnections[playerB.ToString()], playerConnections[playerA.ToString()]);
                                        cMatchRoom.PBMulliganAsked = true;
                                        cMatchRoom.currentGameHigh++;
                                    }

                                    if (cMatchRoom.currentGameHigh != 4)
                                        break;

                                    //mulligan PA
                                    for (int i = cMatchRoom.playerAHand.Count; i > 0; i--)
                                    {
                                        cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerAHand, "Debut");
                                        if (cardlist.Count > 0)
                                            break;

                                        int x = cMatchRoom.playerAHand.Count - 1;
                                        cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand);
                                        cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                                        getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, x);
                                        Console.WriteLine($"PA mulligan " + x);
                                    }
                                    //Sending to players PA mulligan hand
                                    draw = new Draw()
                                    {
                                        playerID = playerA,
                                        suffle = false,
                                        zone = "Deck",
                                        cardList = cMatchRoom.playerAHand
                                    };
                                    pReturnData = new RequestData { type = "duelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Debug.WriteLine(pReturnData.requestObject);
                                    // we are changing the line here
                                    draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);

                                    pReturnData = new RequestData { type = "duelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Debug.WriteLine(pReturnData.requestObject);

                                    /////////////
                                    /////////////
                                    //mulligan PB
                                    for (int i = cMatchRoom.playerBHand.Count; i > 0; i--)
                                    {
                                        cardlist = FileReader.QueryRecordsByNameAndBloom(cMatchRoom.playerBHand, "Debut");
                                        if (cardlist.Count > 0)
                                            break;
                                        int x = cMatchRoom.playerBHand.Count - 1;
                                        cMatchRoom.suffleHandToTheDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand);
                                        cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                                        getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, x);
                                        Console.WriteLine($"PB mulligan " + x);
                                    }


                                    //sending to players mulligan hand
                                    draw = new Draw()
                                    {
                                        playerID = playerB,
                                        suffle = false,
                                        zone = "Deck",
                                        cardList = cMatchRoom.playerBHand
                                    };

                                    pReturnData = new RequestData { type = "duelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Debug.WriteLine(pReturnData.requestObject);

                                    draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);

                                    pReturnData = new RequestData { type = "duelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Debug.WriteLine(pReturnData.requestObject);

                                    draw.cardList.Clear();

                                    cMatchRoom.currentGameHigh = 6;
                                    break;

                                case "DuelFieldReady":
                                    DuelFieldData _duelFieldData = JsonSerializer.Deserialize<DuelFieldData>(playerRequest.requestData.requestObject);

                                    if (int.Parse(playerRequest.playerID) == playerA)
                                    {
                                        FirstGameBoardSetup(_duelFieldData, int.Parse(playerRequest.playerID), cMatchRoom, CarList, _duelFieldData.playerAStage, _duelFieldData.playerABackPosition);
                                    }
                                    else
                                    {
                                        FirstGameBoardSetup(_duelFieldData, int.Parse(playerRequest.playerID), cMatchRoom, CarList, _duelFieldData.playerBStage, _duelFieldData.playerBBackPosition);
                                    }

                                    if (!(cMatchRoom.playerAInicialBoardSetup && cMatchRoom.playerBInicialBoardSetup))
                                        break;

                                    //preparing data to send to players

                                    //PA
                                    int oshiLifeNumberPA = 0;
                                    List<Card> addLifeToMatchPA = new List<Card>();
                                    int oshiLifeNumberPB = 0;
                                    List<Card> addLifeToMatchPB = new List<Card>();

                                    foreach (Record r in CarList)
                                    {
                                        if (r.CardNumber.Equals(cMatchRoom.playerAOshi.cardNumber))
                                        {
                                            oshiLifeNumberPA = 5;//CARDCODED NEED TO BE THE SAME AS THE CARD
                                        }
                                    }
                                    for (int n = 0; n < oshiLifeNumberPA; n++)
                                    {
                                        addLifeToMatchPA.Add(new Card());
                                    }

                                    //PB
                                    foreach (Record r in CarList)
                                    {
                                        if (r.CardNumber.Equals(cMatchRoom.playerBOshi.cardNumber))
                                        {
                                            oshiLifeNumberPB = 5;  //CARDCODED NEED TO BE THE SAME AS THE CARD
                                        }
                                    }
                                    for (int n = 0; n < oshiLifeNumberPB; n++)
                                    {
                                        addLifeToMatchPB.Add(new Card());
                                    }

                                    //place the life counter acording to the oshiiiiii
                                    _DuelFieldDataA = new DuelFieldData
                                    {
                                        playerAHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand),
                                        playerAArquive = new List<Card>(),
                                        playerADeck = new List<Card>(),
                                        playerAHoloPower = new List<Card>(),
                                        playerABackPosition = cMatchRoom.playerABackPosition,
                                        playerAFavourite = cMatchRoom.playerAOshi,
                                        playerAStage = cMatchRoom.playerAStage,
                                        playerACollaboration = new Card(),
                                        playerACardCheer = new List<Card>(),
                                        playerALife = cMatchRoom.FillCardListWithEmptyCards(addLifeToMatchPA),

                                        playerBHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand),
                                        playerBArquive = new List<Card>(),
                                        playerBDeck = new List<Card>(),
                                        playerBHoloPower = new List<Card>(),
                                        playerBBackPosition = cMatchRoom.playerBBackPosition,
                                        playerBFavourite = cMatchRoom.playerBOshi,
                                        playerBStage = cMatchRoom.playerBStage,
                                        playerBCollaboration = new Card(),
                                        playerBCardCheer = new List<Card>(),
                                        playerBLife = cMatchRoom.FillCardListWithEmptyCards(addLifeToMatchPB),
                                        firstPlayer = playerA,
                                        secondPlayer = playerB
                                    };

                                    _DuelFieldDataB = new DuelFieldData
                                    {
                                        playerAHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand),
                                        playerAArquive = new List<Card>(),
                                        playerADeck = new List<Card>(),
                                        playerAHoloPower = new List<Card>(),
                                        playerABackPosition = cMatchRoom.playerABackPosition,
                                        playerAFavourite = cMatchRoom.playerAOshi,
                                        playerAStage = cMatchRoom.playerAStage,
                                        playerACollaboration = new Card(),
                                        playerACardCheer = new List<Card>(),
                                        playerALife = cMatchRoom.FillCardListWithEmptyCards(addLifeToMatchPA),

                                        playerBHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand),
                                        playerBArquive = new List<Card>(),
                                        playerBDeck = new List<Card>(),
                                        playerBHoloPower = new List<Card>(),
                                        playerBBackPosition = cMatchRoom.playerBBackPosition,
                                        playerBFavourite = cMatchRoom.playerBOshi,
                                        playerBStage = cMatchRoom.playerBStage,
                                        playerBCollaboration = new Card(),
                                        playerBCardCheer = new List<Card>(),
                                        playerBLife = cMatchRoom.FillCardListWithEmptyCards(addLifeToMatchPB),
                                        firstPlayer = playerA,
                                        secondPlayer = playerB

                                    };

                                    //prepare the stages high and phases and stuff


                                    //since we were able to update the users table to lock the match, send both players to the match
                                    pReturnData = new RequestData { type = "BoardReadyToPlay", description = "BoardReadyToPlay", requestObject = JsonSerializer.Serialize(_DuelFieldDataA) };
                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    pReturnData = new RequestData { type = "BoardReadyToPlay", description = "BoardReadyToPlay", requestObject = JsonSerializer.Serialize(_DuelFieldDataB) };
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    //update the room phase, so the server can take it automaticaly from here
                                    cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
                                    cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.DrawStep;
                                    cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.DrawStep;
                                    cMatchRoom.currentGameHigh = 7;
                                    //devolver um synchigh com informações de quem vai comprar

                                    break;

                                /////////////////////////////
                                //START OF DUEL NORMAL FLOW//
                                /////////////////////////////
                                ///
                                case "syncHigh":
                                    Task task;
                                    // CURRENT GAME HIGH NOT SYNCHED
                                    if (int.Parse(playerRequest.playerID) == playerA)
                                        cMatchRoom.playerAGameHigh = playerRequest.requestData.sync;

                                    if (int.Parse(playerRequest.playerID) == playerB)
                                        cMatchRoom.playerBGameHigh = playerRequest.requestData.sync;

                                    switch (cMatchRoom.currentGamePhase)
                                    {
                                        case GAMEPHASE.ResetStep:

                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.ResetStep || cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.ResetStep)
                                            {
                                                Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                break;
                                            }
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());





                                            //DO ACTION 
                                            cMatchRoom.currentGameHigh++;

                                            cMatchRoom.playerAGameHigh = cMatchRoom.currentGameHigh;
                                            cMatchRoom.playerBGameHigh = cMatchRoom.currentGameHigh;

                                            //checking game highs
                                            //if (cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerAGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh)
                                            //    break;

                                            cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
                                            cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.DrawStep;
                                            cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.DrawStep;
                                            break;
                                        case GAMEPHASE.DrawStep:
                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.DrawStep || cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.DrawStep)
                                            {
                                                Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                break;
                                            }
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());

                                            if (cMatchRoom.currentPlayerTurn == playerA)
                                            {
                                                getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                                                task = GamePhaseDrawAsync(playerA, playerB, cMatchRoom.playerAHand, result.EndOfMessage);
                                            }
                                            else
                                            {
                                                getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                                                task = GamePhaseDrawAsync(playerA, playerB, cMatchRoom.playerBHand, result.EndOfMessage);
                                            }

                                            /*
                                            if (cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerAGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh)
                                                break;
                                            */

                                            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
                                            cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.CheerStep;
                                            cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.CheerStep;
                                            cMatchRoom.currentGameHigh++;
                                            break;
                                        case GAMEPHASE.CheerStep:
                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.CheerStep || cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.CheerStep)
                                            {
                                                Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                break;
                                            }
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());

                                            //COMPROU A CARTA E DEVOLVEU PARA O JOGADOR
                                            if (cMatchRoom.currentPlayerTurn == playerA)
                                            {
                                                getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerAHand, 1);
                                                task = GamePhaseCheerStepAsync(playerA, playerB, cMatchRoom.playerACardCheer, cMatchRoom.playerAHand, result.EndOfMessage);
                                            }
                                            else
                                            {
                                                getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBHand, 1);
                                                task = GamePhaseCheerStepAsync(playerB, playerA, cMatchRoom.playerBCardCheer, cMatchRoom.playerBHand, result.EndOfMessage);
                                            }

                                            /*
                                            if (cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerAGameHigh || cMatchRoom.currentGameHigh != cMatchRoom.playerBGameHigh )
                                                break;
                                            */
                                            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoose;
                                            cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.CheerStepChoose;
                                            cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.CheerStepChoose;
                                            cMatchRoom.currentGameHigh++;
                                            break;
                                        case GAMEPHASE.CheerStepChoose:


                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());
                                            RequestData ReturnData;
                                            //RECEBE ALVO DA ENERGIA
                                            //removing cheer from player hand, in the client we remove for the oponnent at start of the main phase!
                                            if (int.Parse(playerRequest.playerID) == cMatchRoom.currentPlayerTurn)
                                            {

                                                DuelAction _DuelAction = new();
                                                if (!string.IsNullOrEmpty(playerRequest.requestData.extraRequestObject))
                                                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

                                                bool hasAttached = false;

                                                if (cMatchRoom.playerA.PlayerID == int.Parse(playerRequest.playerID))
                                                {
                                                    if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.CheerStepChoose)
                                                    {
                                                        Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                    }
                                                    cMatchRoom.playerAHand.RemoveAt(cMatchRoom.playerAHand.Count - 1);
                                                    hasAttached = GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition); // we are saving attached to the list only the name of the Cheer, add other information later i needded 
                                                    cMatchRoom.playerACheerStep = true;
                                                    cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.CheerStepChoosed;
                                                }
                                                else
                                                {
                                                    if (cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.CheerStepChoose)
                                                    {
                                                        Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                    }
                                                    cMatchRoom.playerBHand.RemoveAt(cMatchRoom.playerBHand.Count - 1);
                                                    hasAttached = GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);
                                                    cMatchRoom.playerBCheerStep = true;
                                                    cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.CheerStepChoosed;
                                                }

                                                if (!hasAttached)
                                                    break;

                                            }
                                            else 
                                            {
                                                if (cMatchRoom.playerA.PlayerID == int.Parse(playerRequest.playerID))
                                                {
                                                    if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.CheerStepChoose)
                                                    {
                                                        Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                    }
                                                    cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.CheerStepChoosed;
                                                    cMatchRoom.playerACheerStep = true;
                                                }
                                                else
                                                {
                                                    if (cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.CheerStepChoose)
                                                    {
                                                        Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                    }
                                                    cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.CheerStepChoosed;
                                                    cMatchRoom.playerBCheerStep = true;
                                                }


                                            }

                                            if (cMatchRoom.currentPlayerAGamePhase == GAMEPHASE.CheerStepChoosed && cMatchRoom.currentPlayerBGamePhase == GAMEPHASE.CheerStepChoosed)
                                                cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoosed;

                                            /*
                                            if (cMatchRoom.currentGameHigh + 1 != cMatchRoom.playerBGameHigh || cMatchRoom.currentGameHigh + 1 != cMatchRoom.playerAGameHigh || cMatchRoom.currentGameHigh + 1 != cMatchRoom.playerBGameHigh)
                                                break;
                                            */
                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.CheerStepChoosed || cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.CheerStepChoosed)
                                            {
                                                Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                break;
                                            }

                                            ReturnData = new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" };
                                                ReturnData.requestObject = JsonSerializer.Serialize(ReturnData);
                                                await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                          
                                                ReturnData = new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" };
                                                await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
                                            cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.MainStep;
                                            cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.MainStep;
                                            cMatchRoom.currentGameHigh++;
                                            break;

                                        case GAMEPHASE.MainStep:
                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.MainStep || cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.MainStep)
                                            {
                                                Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                                break;
                                            }
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());
                                            switch (playerRequest.requestData.description) {
                                                case "StartMain":
                                                    cMatchRoom.playerBCheerStep = false;
                                                    cMatchRoom.playerACheerStep = false;


                                                    ReturnData = new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" };

                                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                                    break;
                                                case "doAction":
                                                    //NORMAL POSSIBLE ACTIONS FOR PLAYER
                                                    ReturnData = new RequestData { type = "GamePhase", description = "MainPhaseDoAction", requestObject = "" };

                                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                                    break;
                                                case "PerformanceStep":
                                                    cMatchRoom.currentGamePhase = GAMEPHASE.PerformanceStep;
                                                    break;
                                                case "UseArt":
                                                    cMatchRoom.currentGamePhase = GAMEPHASE.UseArt;
                                                    break;
                                                case "Endturn":
                                                    

                                                    int nextPlayerTurn = 0;

                                                    nextPlayerTurn = (cMatchRoom.currentPlayerTurn == playerA) ? playerA : playerB;

                                                    cMatchRoom.currentPlayerTurn = nextPlayerTurn;

                                                    ReturnData = new RequestData { type = "GamePhase", description = "EndStep", requestObject = "" };
                                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);




                                                    cMatchRoom.currentGamePhase = GAMEPHASE.ResetStep;
                                                    cMatchRoom.currentPlayerAGamePhase = GAMEPHASE.ResetStep;
                                                    cMatchRoom.currentPlayerBGamePhase = GAMEPHASE.ResetStep;
                                                    cMatchRoom.currentGameHigh++;
                                                    break;

                                            }
                                            cMatchRoom.currentGameHigh++;
                                            break;

                                        case GAMEPHASE.PerformanceStep:
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());
                                            cMatchRoom.currentGameHigh++;
                                            break;

                                        case GAMEPHASE.UseArt:
                                            Debug.WriteLine("current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName());
                                            cMatchRoom.currentGameHigh++;
                                            break;
                                    }
                                    break;
                                case "cancelMatch":
                                    if (playerConnections.ContainsKey(playerA.ToString()) &&
                                        playerConnections[playerA.ToString()] != null &&
                                        playerConnections[playerA.ToString()].State == WebSocketState.Open)
                                    {
                                        await playerConnections[playerA.ToString()].CloseAsync(WebSocketCloseStatus.NormalClosure, "Player Leaved The Room", CancellationToken.None);
                                    }

                                    if (playerConnections.ContainsKey(playerB.ToString()) &&
                                        playerConnections[playerB.ToString()] != null &&
                                        playerConnections[playerB.ToString()].State == WebSocketState.Open)
                                    {
                                        await playerConnections[playerB.ToString()].CloseAsync(WebSocketCloseStatus.NormalClosure, "Player Leaved The Room", CancellationToken.None);
                                    }

                                    if (_MatchRooms.Contains(cMatchRoom))
                                    {
                                        _MatchRooms.Remove(cMatchRoom);
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Handle invalid player request
                        Console.WriteLine($"Invalid player request: {receivedMessage}");
                        int m = FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
                        if (m != 0)
                        {
                            if (playerConnections[_MatchRooms[m].playerB.PlayerID.ToString()] != null)
                                await playerConnections[_MatchRooms[m].playerB.PlayerID.ToString()].CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid data", CancellationToken.None);
                            if (playerConnections[_MatchRooms[m].playerA.PlayerID.ToString()] != null)
                                await playerConnections[_MatchRooms[m].playerA.PlayerID.ToString()].CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid data", CancellationToken.None);
                            _MatchRooms.RemoveAt(m);
                            return;
                        }
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid data", CancellationToken.None);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");

            int m = MatchRoom.FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
            /*
            if (m < 0 || m >= _MatchRooms.Count)
            {
                Console.WriteLine("Error: Match room index out of range.");
                RequestData errorData = new() { type = "error", description = "Invalid match room index." };
                //Alert player that the room dont exist
                //await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(errorData))), WebSocketMessageType.Text, true, CancellationToken.None);

                if (playerConnections.ContainsKey(_MatchRooms[m].playerB.PlayerID.ToString()) &&
                    playerConnections[_MatchRooms[m].playerB.PlayerID.ToString()] != null)
                {
                    await playerConnections[_MatchRooms[m].playerB.PlayerID.ToString()].CloseAsync(WebSocketCloseStatus.PolicyViolation, "Error 235423", CancellationToken.None);
                }

                if (playerConnections.ContainsKey(_MatchRooms[m].playerA.PlayerID.ToString()) &&
                    playerConnections[_MatchRooms[m].playerA.PlayerID.ToString()] != null)
                {
                    await playerConnections[_MatchRooms[m].playerA.PlayerID.ToString()].CloseAsync(WebSocketCloseStatus.PolicyViolation, "Error 235423", CancellationToken.None);
                }

                _MatchRooms.RemoveAt(m);

                return; 
            }
            */
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
}

bool GamePhaseCheerChoosedAsync(DuelAction duelAction, MatchRoom matchRoom, Card stage, Card collab, List<Card> backStage) 
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
            hasAttached = true;
        }
        if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.local.Equals("Collaboration"))
        {
            collab.attachedEnergy.Add(duelAction.usedCard);
            hasAttached = true;
        }
        foreach (Card c in backStage)
        {
            if (c.cardNumber.Equals(duelAction.targetCard.cardNumber) && !duelAction.local.Equals("BackStage"))
            {
                c.attachedEnergy.Add(duelAction.usedCard);
                hasAttached = true;
            }
        }
    }
    return hasAttached;
}

async Task GamePhaseEndStepAsync()
{
}

    async Task GamePhaseDrawAsync(int playerid, int oponnentid, List<Card> PlayerHand, Boolean result)
{
    Draw newDraw = new Draw();
    newDraw.playerID = playerid;
    newDraw.zone = "Deck";

    RequestData ReturnData = new RequestData { type = "GamePhase", description = "DrawPhase", requestObject = "" };

    newDraw.cardList = new List<Card>() { PlayerHand[PlayerHand.Count - 1] };
    ReturnData.requestObject = JsonSerializer.Serialize(newDraw);
    await playerConnections[playerid.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);
    newDraw.cardList = new List<Card>() { new Card() };
    ReturnData.requestObject = JsonSerializer.Serialize(newDraw);
    await playerConnections[oponnentid.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);


}

async Task GamePhaseCheerStepAsync(int playerid, int oponnentid, List<Card> ExtraDeck, List<Card> PlayerHand, Boolean result) {

    Draw cardCheerDraw = new Draw();
    cardCheerDraw.playerID = playerid;
    cardCheerDraw.zone = "CardCheer";

    RequestData ReturnData = new RequestData { type = "GamePhase", description = "CheerStep", requestObject = "" };

    getCardFromDeck(ExtraDeck, PlayerHand, 1);
    cardCheerDraw.cardList = new List<Card>() { PlayerHand[PlayerHand.Count - 1] };
    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);

    await playerConnections[playerid.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);
    cardCheerDraw.cardList = new List<Card>() { new Card() };

    ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);
    await playerConnections[oponnentid.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);
}

bool ValidatePlayerRequest(PlayerRequest playerRequest)
{
    // Implement your validation logic here
    // For example, check if PlayerID and Password are valid
    return !string.IsNullOrEmpty(playerRequest.playerID) && !string.IsNullOrEmpty(playerRequest.password);
}


 async Task HandleMulligan(MatchRoom room, bool isFirstPlayer, PlayerRequest request, WebSocket socketA, WebSocket socketB)
{
    if (room.currentGameHigh == 3)
    {
        // Determine which player is acting (A or B)
        var playerHand = isFirstPlayer ? room.playerAHand : room.playerBHand;
        var playerDeck = isFirstPlayer ? room.playerADeck : room.playerBDeck;
        var opponentHand = isFirstPlayer ? room.playerBHand : room.playerAHand;
        var opponentDeck = isFirstPlayer ? room.playerBDeck : room.playerADeck;
        var playerID = isFirstPlayer ? room.playerA.PlayerID : room.playerB.PlayerID;
        var playerName = isFirstPlayer ? "PA" : "PB";  // For PANoMulligan, PBMulligan, etc.

        if (request.requestData.requestObject.Equals("t"))
        {
            // Shuffle and redraw cards
            room.suffleHandToTheDeck(playerDeck, playerHand);
            playerDeck = room.ShuffleCards(playerDeck);
            getCardFromDeck(playerDeck, playerHand, 7);
        }

        // Create Draw and DrawDummy objects
        var draw = new Draw
        {
            playerID = playerID,
            suffle = false,
            zone = "Deck",
            cardList = playerHand
        };

        var drawDummy = new Draw
        {
            playerID = playerID,
            suffle = false,
            zone = "Deck",
            cardList = room.FillCardListWithEmptyCards(playerHand)
        };

        // Handle response for acting player (actual cards)
        var playerResponse = new RequestData
        {
            type = "duelUpdate",
            description = request.requestData.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
            requestObject = JsonSerializer.Serialize(draw)  // Acting player gets the real hand
        };

        await socketA.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(playerResponse))), WebSocketMessageType.Text, true, CancellationToken.None);

        // Handle response for opponent (dummy hand)
        var opponentResponse = new RequestData
        {
            type = "duelUpdate",
            description = request.requestData.requestObject.Equals("t") ? $"{playerName}Mulligan" : $"{playerName}NoMulligan",
            requestObject = JsonSerializer.Serialize(drawDummy)  // Opponent gets the dummy hand
        };

        await socketB.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(opponentResponse))), WebSocketMessageType.Text, true, CancellationToken.None);

        // Debugging
        Debug.WriteLine(playerResponse.requestObject);
        Debug.WriteLine(opponentResponse.requestObject);

        // Increment game high for the player
        if (isFirstPlayer)
        {
            room.playerAGameHigh++;
        }
        else
        {
            room.playerBGameHigh++;
        }

    }

}
static bool HaveSameObjectCounts(List<Card> playedthisturn, List<Card> hand)
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
void RemovePlayedCardsFromHand(List<Card> handCards, List<Card> playedCards)
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

bool FirstGameBoardSetup(DuelFieldData _duelFieldData, int playerid, MatchRoom matchroom, List<Record> AllCardList, Card playerStage, List<Card> playerBackStage)
{


    //checking if there is card in the Stage
    if (playerStage == null)
    {
        Debug.WriteLine("Invalid play, no card at stage: " + playerid + matchroom.currentGameHigh);
        return false;
    }

    //check if card in the stage can be there
    List<Record> cardlist = FileReader.QueryRecordsByNameAndBloom(new List<Card>() { playerStage }, "Debut");
    if (cardlist.Count == 0)
    {
        Debug.WriteLine("Invalid play, no card suitable at stage: " + playerid + matchroom.currentGameHigh);
        return false;
    }
    List<Card> cardsPlayedThisTurn = new() { playerStage };
    //check if backposition is in the maximum limite
    if (playerBackStage.Count > 5)
    {
        Debug.WriteLine("Invalid play, more cards at the back stage than what it should: " + playerid + matchroom.currentGameHigh);
        return false;
    }

    //check if all cards at the backposition can be there
    if (playerBackStage.Count > 0) { 
    int n = 0;
        foreach (Card c in playerBackStage)
        {
            List<Record> subcardlist = FileReader.QueryRecordsByNameAndBloom(new List<Card>() { c }, "Debut");
            if (subcardlist.Count > 0)
                n++;
        }
        if (n != playerBackStage.Count)
        {
            Debug.WriteLine("Invalid play, there card in the backstage that shouldnt: " + playerid + matchroom.currentGameHigh);
            return false;
        }
    }
    // the duplicated cards are still around here... need to check why.

    //check if all played cards exists in the player hand
    cardsPlayedThisTurn.AddRange(playerBackStage);

    if (matchroom.firstPlayer == playerid) { 
        if (!(HaveSameObjectCounts(cardsPlayedThisTurn, matchroom.playerAHand)))
        {
            Debug.WriteLine("Invalid play, there card in the field that are not at player hand: " + playerid + matchroom.currentGameHigh);
            return false;
        }
    } else {
        if (!(HaveSameObjectCounts(cardsPlayedThisTurn, matchroom.playerBHand)))
        {
            Debug.WriteLine("Invalid play, there card in the field that are not at player hand: " + playerid + matchroom.currentGameHigh);
            return false;
        }
    }

    //since we get this far, we remove the played cards from the hand
    if (matchroom.playerA.PlayerID == playerid)
    {
        RemovePlayedCardsFromHand(matchroom.playerAHand, cardsPlayedThisTurn);

        //since the last code updated the hand, we only need to update the field now:
        matchroom.playerAStage = _duelFieldData.playerAStage;
        matchroom.playerABackPosition = _duelFieldData.playerABackPosition;
        matchroom.playerAInicialBoardSetup = true;
    }
    else
    {
        RemovePlayedCardsFromHand(matchroom.playerBHand, cardsPlayedThisTurn);

        //since the last code updated the hand, we only need to update the field now:
        matchroom.playerBStage = _duelFieldData.playerBStage;
        matchroom.playerBBackPosition = _duelFieldData.playerBBackPosition;
        matchroom.playerBInicialBoardSetup = true;
    }
    return true;
}

// Run the application
app.Run();
