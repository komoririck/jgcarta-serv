using hololive_oficial_cardgame_server;
using Microsoft.OpenApi.Extensions;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Utilities.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using ZstdSharp.Unsafe;
using static hololive_oficial_cardgame_server.MatchRoom;

var builder = WebApplication.CreateBuilder(args);
//
//
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

List<Record> CardList = FileReader.ReadFile("CardList.xlsx");
//List<Record> CardList = FileReader.ReadFile("CardList.csv");

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

                    } catch (Exception ex) { Console.WriteLine(ex); }
                    //process victory
                    int matchnumber = MatchRoom.FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
                    if (_MatchRooms[matchnumber] != null)
                        _MatchRooms.Remove(_MatchRooms[matchnumber]);
                    Console.WriteLine("\nRoom "+ matchnumber + " closed");
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

                                _MatchRooms.Add(_MatchRoom);

                                int PlayersRoomId = FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
                                MatchRoom cMatchRoom = _MatchRooms[PlayersRoomId];
                                
                                // SUFFLE ENERGY
                                cMatchRoom.playerACardCheer = cMatchRoom.ShuffleCards(cMatchRoom.playerACardCheer);
                                cMatchRoom.playerBCardCheer = cMatchRoom.ShuffleCards(cMatchRoom.playerBCardCheer);
                                //SUFFLE DECK
                                cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
                                cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);

                                _DuelFieldDataA = new DuelFieldData
                                {
                                    playerAHand = new List<Card>(),
                                    playerAArquive = new List<Card>(),
                                    playerADeck = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerADeck),
                                    playerAHoloPower = new List<Card>(),
                                    playerABackPosition = new List<Card>(),
                                    playerAFavourite = cMatchRoom.playerAFavourite,
                                    playerAStage = new Card(),
                                    playerACollaboration = new Card(),
                                    playerACardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerACardCheer),
                                    playerALife = new List<Card>(),

                                    playerBHand = new List<Card>(),
                                    playerBArquive = new List<Card>(),
                                    playerBDeck = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBDeck),
                                    playerBHoloPower = new List<Card>(),
                                    playerBBackPosition = new List<Card>(),
                                    playerBFavourite = new Card(),
                                    playerBStage = new Card(),
                                    playerBCollaboration = new Card(),
                                    playerBCardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBCardCheer),
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
                                    playerADeck = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerADeck),
                                    playerAHoloPower = new List<Card>(),
                                    playerABackPosition = new List<Card>(),
                                    playerAFavourite = new Card(),
                                    playerAStage = new Card(),
                                    playerACollaboration = new Card(),
                                    playerACardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerACardCheer),
                                    playerALife = new List<Card>(),

                                    playerBHand = new List<Card>(),
                                    playerBArquive = new List<Card>(),
                                    playerBDeck = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBDeck),
                                    playerBHoloPower = new List<Card>(),
                                    playerBBackPosition = new List<Card>(),
                                    playerBFavourite = decksinfo[5][0],
                                    playerBStage = new Card(),
                                    playerBCollaboration = new Card(),
                                    playerBCardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBCardCheer),
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


                                cMatchRoom.currentGameHigh = 1;
                                //END OF LOADING GAME SCENE


                                //GET PLAYERS STARTER HAND
                                getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 7);
                                getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 7);


                                //BEGIN - SEND FIRST PLAYER STARTER HAND 
                                draw = new Draw()
                                {
                                    playerID = cMatchRoom.startPlayer,
                                    suffle = false,
                                    zone = "Deck",
                                    cardList = cMatchRoom.playerAHand
                                };
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw) };
                                Console.WriteLine(pReturnData.requestObject);
                                await playerConnections[cMatchRoom.startPlayer.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw) };
                                Console.WriteLine(pReturnData.requestObject);
                                await playerConnections[GetOtherPlayer(cMatchRoom, cMatchRoom.startPlayer).ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                cMatchRoom.currentGameHigh = 2;

                                //P2 START HAND
                                draw = new Draw()
                                {
                                    playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.startPlayer),
                                    suffle = false,
                                    zone = "Deck",
                                    cardList = cMatchRoom.playerBHand
                                };

                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw) };
                                Console.WriteLine(pReturnData.requestObject);
                                await playerConnections[cMatchRoom.playerB.PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);
                                pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw) };
                                Console.WriteLine(pReturnData.requestObject);
                                await playerConnections[cMatchRoom.playerA.PlayerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                cMatchRoom.currentGameHigh = 3;

                                cMatchRoom.playerAGameHigh = cMatchRoom.currentGameHigh;
                                cMatchRoom.playerBGameHigh = cMatchRoom.currentGameHigh;
                                //END - SEND FIRST PLAYER STARTER HAND 


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
                            Task task;
                            DuelAction _DuelAction = new();
                            RequestData _ReturnData;

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
                                    Console.WriteLine(pReturnData.requestObject);
                                    // we are changing the line here
                                    draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);

                                    pReturnData = new RequestData { type = "duelUpdate", description = "PAMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Console.WriteLine(pReturnData.requestObject);

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
                                    Console.WriteLine(pReturnData.requestObject);

                                    draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);

                                    pReturnData = new RequestData { type = "duelUpdate", description = "PBMulliganF", requestObject = JsonSerializer.Serialize(draw) };
                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    Console.WriteLine(pReturnData.requestObject);

                                    draw.cardList.Clear();

                                    cMatchRoom.currentGameHigh = 6;
                                    break;

                                case "DuelFieldReady":
                                    DuelFieldData _duelFieldData = JsonSerializer.Deserialize<DuelFieldData>(playerRequest.requestData.requestObject);

                                    if (int.Parse(playerRequest.playerID) == playerA)
                                    {
                                        FirstGameBoardSetup(_duelFieldData, int.Parse(playerRequest.playerID), cMatchRoom, CardList, _duelFieldData.playerAStage, _duelFieldData.playerABackPosition);
                                    }
                                    else
                                    {
                                        FirstGameBoardSetup(_duelFieldData, int.Parse(playerRequest.playerID), cMatchRoom, CardList, _duelFieldData.playerBStage, _duelFieldData.playerBBackPosition);
                                    }

                                    if (!(cMatchRoom.playerAInicialBoardSetup && cMatchRoom.playerBInicialBoardSetup))
                                        break;

                                    //preparing data to send to players

                                    //PA
                                    /*
                                    int oshiLifeNumberPA = 0;
                                    List<Card> addLifeToMatchPA = new List<Card>();
                                    int oshiLifeNumberPB = 0;
                                    List<Card> addLifeToMatchPB = new List<Card>();
                                    */

                                    cMatchRoom.playerAOshi.GetCardInfo(cMatchRoom.playerAOshi.cardNumber);
                                    cMatchRoom.playerBOshi.GetCardInfo(cMatchRoom.playerBOshi.cardNumber);
                                    getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerALife, int.Parse(cMatchRoom.playerAOshi.life));
                                    getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBLife, int.Parse(cMatchRoom.playerBOshi.life));


                                    /*
                                    foreach (Record r in CardList)
                                    {
                                        if (r.CardNumber.Equals(cMatchRoom.playerAOshi.cardNumber))
                                        {
                                            oshiLifeNumberPA = int.Parse(cMatchRoom.playerAOshi.life);
                                        }
                                    }
                                    for (int n = 0; n < oshiLifeNumberPA; n++)
                                    {
                                        addLifeToMatchPA.Add(new Card());
                                    }

                                    //PB
                                    foreach (Record r in CardList)
                                    {
                                        if (r.CardNumber.Equals(cMatchRoom.playerBOshi.cardNumber))
                                        {
                                            oshiLifeNumberPB = int.Parse(cMatchRoom.playerBOshi.life);
                                        }
                                    }
                                    for (int n = 0; n < oshiLifeNumberPB; n++)
                                    {
                                        addLifeToMatchPB.Add(new Card());
                                    }*/

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
                                        playerALife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerALife),

                                        playerBHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand),
                                        playerBArquive = new List<Card>(),
                                        playerBDeck = new List<Card>(),
                                        playerBHoloPower = new List<Card>(),
                                        playerBBackPosition = cMatchRoom.playerBBackPosition,
                                        playerBFavourite = cMatchRoom.playerBOshi,
                                        playerBStage = cMatchRoom.playerBStage,
                                        playerBCollaboration = new Card(),
                                        playerBCardCheer = new List<Card>(),
                                        playerBLife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBLife),
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
                                        playerALife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerALife),

                                        playerBHand = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand),
                                        playerBArquive = new List<Card>(),
                                        playerBDeck = new List<Card>(),
                                        playerBHoloPower = new List<Card>(),
                                        playerBBackPosition = cMatchRoom.playerBBackPosition,
                                        playerBFavourite = cMatchRoom.playerBOshi,
                                        playerBStage = cMatchRoom.playerBStage,
                                        playerBCollaboration = new Card(),
                                        playerBCardCheer = new List<Card>(),
                                        playerBLife = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBLife),
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
                                    cMatchRoom.currentGameHigh = 7;
                                    //devolver um synchigh com informações de quem vai comprar
                                    break;

                                /////////////////////////////
                                //START OF DUEL NORMAL FLOW//
                                /////////////////////////////
                                ///
                                case "DrawRequest":


                                    if (int.Parse( playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;

                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.DrawStep)
                                    {
                                        Console.WriteLine("\ncurrent game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }

                                    if (cMatchRoom.currentPlayerTurn == playerA) { 
                                        getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                                        task = GamePhaseDrawAsync(playerA, cMatchRoom.playerAHand, result.EndOfMessage, cMatchRoom);
                                    }
                                    else
                                    {
                                        getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                                        task = GamePhaseDrawAsync(playerB, cMatchRoom.playerBHand, result.EndOfMessage, cMatchRoom);
                                    }

                                    cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
                                    cMatchRoom.currentGameHigh++;
                                    break;
                                case "CheerRequest":

                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;

                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStep)
                                    {
                                        Console.WriteLine(playerRequest.playerID + " current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }

                                    Draw cardCheerDraw = new Draw();
                                    cardCheerDraw.playerID = cMatchRoom.currentPlayerTurn;
                                    cardCheerDraw.zone = "CardCheer";

                                    RequestData ReturnData = new RequestData { type = "GamePhase", description = "CheerStep", requestObject = "" };

                                    if (cMatchRoom.currentPlayerTurn.Equals(playerA))
                                    {
                                        getCardFromDeck(cMatchRoom.playerACardCheer, cMatchRoom.playerAHand, 1);
                                        cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerAHand[cMatchRoom.playerAHand.Count - 1] };
                                        ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);
                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                        cardCheerDraw.cardList = new List<Card>() { new Card() };
                                        ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    }
                                    else
                                    {
                                        getCardFromDeck(cMatchRoom.playerBCardCheer, cMatchRoom.playerBHand, 1);
                                        cardCheerDraw.cardList = new List<Card>() { cMatchRoom.playerBHand[cMatchRoom.playerBHand.Count - 1] };
                                        ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                        cardCheerDraw.cardList = new List<Card>() { new Card() };
                                        ReturnData.requestObject = JsonSerializer.Serialize(cardCheerDraw);
                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    }


                                    cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoose;
                                    cMatchRoom.currentGameHigh++;
                                    break;
                                case "CheerChooseRequest":

                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;

                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStepChoose)
                                    {
                                        Console.WriteLine("\ncurrent game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }
                                   
                                    //removing cheer from player hand, in the client we remove for the oponnent at start of the main phase!
                                    if (int.Parse(playerRequest.playerID) == cMatchRoom.currentPlayerTurn)
                                    {
                                        try
                                        {
                                            _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                                        }
                                        catch (Exception e) { Console.WriteLine("\n7 pele: " + playerRequest.playerID); }
                                        bool hasAttached = false;

                                        if (cMatchRoom.playerA.PlayerID == int.Parse(playerRequest.playerID))
                                        {
                                            if (cMatchRoom.currentPlayerAGamePhase != GAMEPHASE.CheerStepChoose)
                                            {
                                                Console.WriteLine(playerRequest.playerID + " current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                            }
                                            cMatchRoom.playerAHand.RemoveAt(cMatchRoom.playerAHand.Count - 1);
                                            hasAttached = GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition); // we are saving attached to the list only the name of the Cheer, add other information later i needded 
                                        }
                                        if (cMatchRoom.playerB.PlayerID == int.Parse(playerRequest.playerID))
                                        {
                                            if (cMatchRoom.currentPlayerBGamePhase != GAMEPHASE.CheerStepChoose)
                                            {
                                                Console.WriteLine(playerRequest.playerID + " current game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                            }
                                            cMatchRoom.playerBHand.RemoveAt(cMatchRoom.playerBHand.Count - 1);
                                            hasAttached = GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);
                                        }

                                        if (!hasAttached)
                                            break;

                                        cMatchRoom.currentGamePhase = GAMEPHASE.CheerStepChoosed;
                                    }

                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStepChoosed)
                                    {
                                        Console.WriteLine("\ncurrent game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }


                                    _ReturnData = new RequestData { type = "GamePhase", description = "CheerStepEnd", requestObject = JsonSerializer.Serialize(_DuelAction) };
                                    Console.WriteLine("\nEnvio " + playerRequest.playerID + JsonSerializer.Serialize(_ReturnData));

                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
    
                                    cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
                                    cMatchRoom.currentGameHigh++;
                                    break;

                                case "MainStartRequest":
                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;
                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                                    {
                                        Console.WriteLine("\ncurrent game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }

                                    _ReturnData = new RequestData { type = "GamePhase", description = "MainPhase", requestObject = "" };
                                    Console.WriteLine("\nEnvio " + playerRequest.playerID + JsonSerializer.Serialize(_ReturnData));

                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    break;
                                case "MainDoActionRequest":
                                    
                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                                        break;
                                    
                                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                                    _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
                                    _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

                                    int handPos = -1;
                                    ////////////////////////////
                                    //NOTHING BETTER THAN A SALAD 
                                    ////////////////////////////
                                    bool infoSend = false;

                                    switch (_DuelAction.actionType) {
                                        case "PlayHolomem":
                                            //checking if the card can be played

                                            List<Record> avaliableCards = FileReader.QueryRecords(null, null, null, _DuelAction.usedCard.cardNumber);//  QueryRecordsByNameAndBloom(new List<Card>() { _DuelAction.usedCard }, "ホロメン");
                                            
                                            //avaliableCards.AddRange(FileReader.QueryRecordsByNameAndBloom(new List<Card>() { _DuelAction.usedCard }, "Buzzホロメン"));

                                            //if not break
                                            if (avaliableCards.Count < 1)
                                                break;


                                            bool canContinue = false;

                                            if (avaliableCards[0].CardType.Equals("ホロメン") || avaliableCards[0].CardType.Equals("Buzzホロメン"))
                                                canContinue = true;

                                            if (!canContinue)
                                                break;

                                            //checking if the player has the card in the hand and getting the pos
                                            handPos = -1;
                                            if (int.Parse(playerRequest.playerID) == playerA)
                                            {
                                                int handPosCounter = 0;
                                                foreach (Card inHand in cMatchRoom.playerAHand) {
                                                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                        handPos = handPosCounter;
                                                    handPosCounter++;
                                                }
                                            }
                                            else 
                                            {
                                                int handPosCounter = 0;
                                                foreach (Card inHand in cMatchRoom.playerBHand)
                                                {
                                                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                        handPos = handPosCounter;
                                                    handPosCounter++;
                                                }
                                            }
                                            if (handPos == -1)
                                                break;

                                            //checking of the card can be played at the spot
                                            switch (_DuelAction.local) {
                                                case "Stage":
                                                    if (int.Parse(playerRequest.playerID) == playerA) {
                                                        if (cMatchRoom.playerAStage != null) 
                                                        {
                                                            cMatchRoom.playerAStage = _DuelAction.usedCard; 
                                                            cMatchRoom.playerAStage.cardPosition = _DuelAction.usedCard.cardPosition;
                                                            cMatchRoom.playerAStage.playedFrom = _DuelAction.usedCard.playedFrom;
                                                            cMatchRoom.playerAHand.RemoveAt(handPos);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (cMatchRoom.playerBStage != null)
                                                        {
                                                            cMatchRoom.playerBStage = _DuelAction.usedCard;
                                                            cMatchRoom.playerBStage.cardPosition = _DuelAction.usedCard.cardPosition;
                                                            cMatchRoom.playerBStage.playedFrom = _DuelAction.usedCard.playedFrom;
                                                            cMatchRoom.playerBHand.RemoveAt(handPos);
                                                        }
                                                    }
                                                break;
                                                case "BackStage1":
                                                case "BackStage2":
                                                case "BackStage3":
                                                case "BackStage4":
                                                case "BackStage5":
                                                    List<Card> actionCardList = new List<Card>();
                                                    if (int.Parse(playerRequest.playerID) == playerA) {
                                                        foreach (Card cartinha in cMatchRoom.playerABackPosition)
                                                        {
                                                            if (cartinha.cardPosition.Equals(_DuelAction.local))
                                                                break;
                                                        }

                                                        _DuelAction.usedCard.cardPosition = _DuelAction.local;
                                                        _DuelAction.usedCard.playedFrom = _DuelAction.playedFrom;

                                                        cMatchRoom.playerABackPosition.Add(_DuelAction.usedCard);
                                                        cMatchRoom.playerAHand.RemoveAt(handPos);
                                                    } else {
                                                        foreach (Card cartinha in cMatchRoom.playerBBackPosition)
                                                        {
                                                            if (cartinha.cardPosition.Equals(_DuelAction.local))
                                                                break;
                                                        }
                                                        _DuelAction.usedCard.cardPosition = _DuelAction.local;
                                                        _DuelAction.usedCard.playedFrom = _DuelAction.playedFrom;

                                                        cMatchRoom.playerBBackPosition.Add(_DuelAction.usedCard);
                                                        cMatchRoom.playerBHand.RemoveAt(handPos);
                                                    }
                                                break;
                                            }
                                            break;
                                        case "BloomHolomem":
                                            canContinue = false;

                                            if (!_DuelAction.usedCard.bloomLevel.Equals("Debut") || !_DuelAction.usedCard.bloomLevel.Equals("1st"))
                                                canContinue = true;

                                            if (!canContinue)
                                                break;

                                            List<Record> validCardToBloom = FileReader.QueryBloomableCard(_DuelAction.targetCard.name, _DuelAction.targetCard.bloomLevel);

                                            //if not break possible to bloom, break
                                            if (validCardToBloom.Count < 1)
                                                break;

                                            //checking if the player has the card in the hand and getting the pos
                                            handPos = -1;
                                            if (int.Parse(playerRequest.playerID) == playerA)
                                            {
                                                int nn = 0;
                                                foreach (Card inHand in cMatchRoom.playerAHand)
                                                {
                                                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                        handPos = nn;
                                                    nn++;
                                                }
                                            }
                                            else
                                            {
                                                int nn = 0;
                                                foreach (Card inHand in cMatchRoom.playerBHand)
                                                {
                                                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                        handPos = nn;
                                                    nn++;
                                                }
                                            }
                                            if (handPos == -1)
                                                break;

                                            int validCardPos = -1;
                                            int n = 0;
                                            foreach (Record record in validCardToBloom)
                                            {
                                                if (record.CardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                {
                                                    validCardPos = n;
                                                }
                                                n++;
                                            }

                                            if (validCardPos == -1)
                                                break;

                                            switch (_DuelAction.local)
                                            {
                                                case "Stage":
                                                    if (int.Parse(playerRequest.playerID) == playerA)
                                                    {
                                                            _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerAStage);
                                                            _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerAStage.attachedEnergy;
                                                            cMatchRoom.playerAStage.attachedEnergy = null;
                                                            cMatchRoom.playerAStage = _DuelAction.usedCard;

                                                            cMatchRoom.playerAHand.RemoveAt(handPos);
                                                    }
                                                    else
                                                    {
                                                            _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerBStage);
                                                            _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerBStage.attachedEnergy;
                                                            cMatchRoom.playerBStage.attachedEnergy = null;
                                                            cMatchRoom.playerBStage = _DuelAction.usedCard;

                                                            cMatchRoom.playerBHand.RemoveAt(handPos);
                                                    }
                                                break;
                                                case "Collaboration":
                                                    if (int.Parse(playerRequest.playerID) == playerA)
                                                    {
                                                            _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerACollaboration);
                                                            _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerACollaboration.attachedEnergy;
                                                            cMatchRoom.playerACollaboration.attachedEnergy = null;
                                                            cMatchRoom.playerACollaboration = _DuelAction.usedCard;

                                                            cMatchRoom.playerAHand.RemoveAt(handPos);
                                                    }
                                                    else
                                                    {
                                                            _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerBCollaboration);
                                                            _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerBCollaboration.attachedEnergy;
                                                            cMatchRoom.playerBCollaboration.attachedEnergy = null;
                                                            cMatchRoom.playerBCollaboration = _DuelAction.usedCard;

                                                            cMatchRoom.playerBHand.RemoveAt(handPos);
                                                    }
                                                break;
                                                case "BackStage1":
                                                case "BackStage2":
                                                case "BackStage3":
                                                case "BackStage4":
                                                case "BackStage5":
                                                    List<Card> actionCardList = new List<Card>();

                                                    if (int.Parse(playerRequest.playerID) == playerA)
                                                        actionCardList = cMatchRoom.playerABackPosition;
                                                    else
                                                        actionCardList = cMatchRoom.playerBBackPosition;

                                                    int x = 0;
                                                    int y = 0;
                                                    foreach (Card cartinha in actionCardList)
                                                    {
                                                        if (cartinha.cardPosition.Equals(_DuelAction.local))
                                                        {
                                                            if (!cartinha.playedThisTurn)
                                                                x = y;
                                                        }
                                                        y++;
                                                    }

                                                    _DuelAction.usedCard.bloomChild.Add(actionCardList[x]);
                                                    _DuelAction.usedCard.attachedEnergy = actionCardList[x].attachedEnergy;
                                                    actionCardList[x].attachedEnergy = null;
                                                    actionCardList[x].cardNumber = _DuelAction.usedCard.cardNumber;

                                                    if (int.Parse(playerRequest.playerID) == playerA)
                                                    {
                                                        cMatchRoom.playerABackPosition.Add(_DuelAction.usedCard);
                                                        cMatchRoom.playerAHand.RemoveAt(handPos);
                                                    }
                                                    else
                                                    {
                                                        cMatchRoom.playerBBackPosition.Add(_DuelAction.usedCard);
                                                        cMatchRoom.playerBHand.RemoveAt(handPos);
                                                    }
                                                    break;
                                                }
                                            break;
                                        case "DoCollab":

                                            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                                                break;
                                            _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                                            _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
                                            _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

                                            if (playerA.Equals(_DuelAction.playerID)) {
                                                if (cMatchRoom.playerADeck.Count == 0)
                                                    break;
                                                if (cMatchRoom.playerACollaboration.cardNumber == "") {
                                                    int x = 0;
                                                    foreach (Card c in cMatchRoom.playerABackPosition) {
                                                        if (c.cardPosition.Equals(_DuelAction.usedCard.cardPosition) && c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.suspended == false)
                                                        {
                                                            cMatchRoom.playerAHoloPower.Add(cMatchRoom.playerADeck.Last());
                                                            cMatchRoom.playerADeck.RemoveAt(cMatchRoom.playerADeck.Count - 1);
                                                            cMatchRoom.playerACollaboration = _DuelAction.usedCard;
                                                            cMatchRoom.playerABackPosition.RemoveAt(x);
                                                            break;
                                                        }
                                                        x++;
                                                    }
                                                    break;
                                                }
                                            } else {
                                                if (cMatchRoom.playerBDeck.Count == 0)
                                                    break;
                                                if (cMatchRoom.playerBCollaboration.cardNumber == "")
                                                {
                                                    int x = 0;
                                                    foreach (Card c in cMatchRoom.playerBBackPosition)
                                                    {
                                                        if (c.cardPosition.Equals(_DuelAction.usedCard.cardPosition) && c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.suspended == false)
                                                        {
                                                            cMatchRoom.playerBHoloPower.Add(cMatchRoom.playerBDeck.Last());
                                                            cMatchRoom.playerBDeck.RemoveAt(cMatchRoom.playerBDeck.Count - 1);
                                                            cMatchRoom.playerBCollaboration = _DuelAction.usedCard;
                                                            cMatchRoom.playerBBackPosition.RemoveAt(x);
                                                            break;
                                                        }
                                                        x++;
                                                    }
                                                }
                                            }
                                            break;
                                        case "UseSuportStaffMember":
                                            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                                                break;

                                            _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                                            _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
                                            _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

                                            foreach (Card cPlayed in cMatchRoom.playerALimiteCardPlayed)
                                                if (cPlayed.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                                    break;

                                            switch (_DuelAction.usedCard.cardNumber)
                                            {
                                                case "hSD01-016":
                                                    infoSend = true;
                                                    UseCardEffectDrawAsync(cMatchRoom, playerA, playerB, 3, "hSD01-016", true, result.EndOfMessage);
                                                    break;
                                                case "XXXXXXXXXX":
                                                    break;
                                            }
                                            break;
                                        case "next":
                                            break;
                                    }
                                    if (!infoSend) { 
                                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                                        _ReturnData = new RequestData { type = "GamePhase", description = _DuelAction.actionType, requestObject = JsonSerializer.Serialize(_DuelAction)};

                                        Console.WriteLine("\nEnvio " + playerRequest.playerID + JsonSerializer.Serialize(_ReturnData));
                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                        cMatchRoom.currentGameHigh++;
                                    }
                                    ////////////////////////////
                                    //NOTHING BETTER THAN A SALAD 
                                    ////////////////////////////
                                    break;
                                case "MainPerformanceRequest":
                                    cMatchRoom.currentGamePhase = GAMEPHASE.PerformanceStep;
                                    break;
                                case "MainUseArtRequest":
                                    cMatchRoom.currentGamePhase = GAMEPHASE.UseArt;
                                    break;
                                case "MainEndturnRequest":
                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;

                                    if (cMatchRoom.currentPlayerTurn == playerA)
                                        cMatchRoom.currentPlayerTurn = playerB;
                                    else
                                        cMatchRoom.currentPlayerTurn = playerA;

                                    cMatchRoom.currentGamePhase = GAMEPHASE.ResetStep;


                                    _ReturnData = new RequestData { type = "GamePhase", description = "Endturn", requestObject = "" };
                                    Console.WriteLine("\nEnvio "+ playerRequest.playerID + JsonSerializer.Serialize(_ReturnData));
                                    await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    cMatchRoom.currentGameHigh++;
                                    break;
                                case "ResetRequest":
                                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                                        break;

                                    if (cMatchRoom.currentGamePhase != GAMEPHASE.ResetStep)
                                    {
                                        Console.WriteLine("\ncurrent game phase: " + cMatchRoom.currentGamePhase.GetDisplayName() + " Player A game phase " + cMatchRoom.currentPlayerAGamePhase.GetDisplayName() + " Player B game phase " + cMatchRoom.currentPlayerBGamePhase.GetDisplayName());
                                        break;
                                    }


                                    DuelAction duelAction = new DuelAction();

                                    if (cMatchRoom.currentPlayerTurn.Equals(playerA))
                                    {

                                        ResetCardTurnStatusForPlayer(cMatchRoom.playerABackPosition, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration);
                                        if (cMatchRoom.playerACollaboration.cardNumber != "") 
                                        {
                                            List<bool> places = new List<bool>() { false, false, false, false, false };
                                            foreach (Card _card in cMatchRoom.playerABackPosition)
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

                                            cMatchRoom.playerACollaboration.suspended = true;

                                            if (places[0] == false)
                                            {
                                                cMatchRoom.playerACollaboration.cardPosition = "BackStage1";
                                                cMatchRoom.playerABackPosition.Add(cMatchRoom.playerACollaboration);
                                            }
                                            else if (places[1] == false)
                                            {
                                                cMatchRoom.playerACollaboration.cardPosition = "BackStage2";
                                                cMatchRoom.playerABackPosition.Add(cMatchRoom.playerACollaboration);
                                            }
                                            else if (places[2] == false)
                                            {
                                                cMatchRoom.playerACollaboration.cardPosition = "BackStage3";
                                                cMatchRoom.playerABackPosition.Add(cMatchRoom.playerACollaboration);
                                            }
                                            else if (places[3] == false)
                                            {
                                                cMatchRoom.playerACollaboration.cardPosition = "BackStage4";
                                                cMatchRoom.playerABackPosition.Add(cMatchRoom.playerACollaboration);
                                            }
                                            else if (places[4] == false)
                                            {
                                                cMatchRoom.playerACollaboration.cardPosition = "BackStage5";
                                                cMatchRoom.playerABackPosition.Add(cMatchRoom.playerACollaboration);
                                            }

                                            duelAction = new DuelAction
                                            {
                                                playerID = playerA,
                                                usedCard = cMatchRoom.playerACollaboration,
                                                playedFrom = "Collaboration",
                                                local = cMatchRoom.playerACollaboration.playedFrom,
                                                actionType = "UndoCollab"
                                            };
                                            cMatchRoom.playerACollaboration = new Card();
                                        }
                                    }
                                    else
                                    {
                                        ResetCardTurnStatusForPlayer(cMatchRoom.playerBBackPosition, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration);
                                        if (cMatchRoom.playerBCollaboration.cardNumber != "")
                                        {
                                            List<bool> places = new List<bool>() { false, false, false, false, false };
                                            foreach (Card _card in cMatchRoom.playerBBackPosition)
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

                                            cMatchRoom.playerBCollaboration.suspended = true;

                                            if (places[0] == false)
                                            {
                                                cMatchRoom.playerBCollaboration.cardPosition = "BackStage1";
                                                cMatchRoom.playerBBackPosition.Add(cMatchRoom.playerBCollaboration);
                                            }
                                            else if (places[1] == false)
                                            {
                                                cMatchRoom.playerBCollaboration.cardPosition = "BackStage2";
                                                cMatchRoom.playerBBackPosition.Add(cMatchRoom.playerBCollaboration);
                                            }
                                            else if (places[2] == false)
                                            {
                                                cMatchRoom.playerBCollaboration.cardPosition = "BackStage3";
                                                cMatchRoom.playerBBackPosition.Add(cMatchRoom.playerBCollaboration);
                                            }
                                            else if (places[3] == false)
                                            {
                                                cMatchRoom.playerBCollaboration.cardPosition = "BackStage4";
                                                cMatchRoom.playerBBackPosition.Add(cMatchRoom.playerBCollaboration);
                                            }
                                            else if (places[4] == false)
                                            {
                                                cMatchRoom.playerBCollaboration.cardPosition = "BackStage5";
                                                cMatchRoom.playerBBackPosition.Add(cMatchRoom.playerBCollaboration);
                                            }

                                            duelAction = new DuelAction
                                            {
                                                playerID = playerA,
                                                usedCard = cMatchRoom.playerBCollaboration,
                                                playedFrom = "Collaboration",
                                                local = cMatchRoom.playerBCollaboration.playedFrom,
                                                actionType = "ReturnCollab"
                                            };
                                            cMatchRoom.playerACollaboration = new Card();
                                        }
                                    }

                                     cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;

                                    _ReturnData = new RequestData { type = "GamePhase", description = "ResetStep", requestObject = JsonSerializer.Serialize(duelAction) };

                                    //_ReturnDataDummy = new RequestData { type = "GamePhase", description = _DuelAction.actionType, requestObject = "" };
                                    Console.WriteLine("\nEnvio " + playerRequest.playerID + JsonSerializer.Serialize(_ReturnData));
                                    //Console.WriteLine("\nEnvio " + playerRequest.playerID + JsonSerializer.Serialize(_ReturnDataDummy));
                                    if (int.Parse(playerRequest.playerID) == playerA)
                                    {
                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                    }
                                    else
                                    {
                                        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                                        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_ReturnData))), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);

                                    }
                                    cMatchRoom.currentGameHigh++;
                                    break;
                                case "cancelMatch":
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
            Console.WriteLine($"Exception: " + ex);

            int m = MatchRoom.FindPlayerMatchRoom(_MatchRooms, playerRequest.playerID);
            /*
            if (m < 0 || m >= _MatchRooms.Count)
            {
                Console.WriteLine("\nError: Match room index out of range.");
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

async Task UseCardEffectDrawAsync(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, Boolean result)
{
    Draw _Draw = new Draw()
    {
        playerID = cMatchRoom.currentPlayerTurn,
        suffle = false,
        zone = "Deck",
        cardList = (cMatchRoom.currentPlayerTurn == playerA) ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
    };

    DuelAction DuelActionResponse = new DuelAction()
    {
        playerID = cMatchRoom.currentPlayerTurn,
        actionType = "doDraw"
    };


    if (playerA == cMatchRoom.currentPlayerTurn)
    {
        DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw.cardList = cMatchRoom.FillCardListWithEmptyCards(_Draw.cardList));
        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DuelActionResponse))), WebSocketMessageType.Text, result, CancellationToken.None);
        DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw);
        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DuelActionResponse))), WebSocketMessageType.Text, result, CancellationToken.None);
        if (LimiteUseCard)
            cMatchRoom.playerALimiteCardPlayed.Add(new Card() { cardNumber = cUsedNumber });
    }
    else
    {
        DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw.cardList = cMatchRoom.FillCardListWithEmptyCards(_Draw.cardList));
        await playerConnections[playerA.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DuelActionResponse))), WebSocketMessageType.Text, result, CancellationToken.None);
        DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw);
        await playerConnections[playerB.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DuelActionResponse))), WebSocketMessageType.Text, result, CancellationToken.None);
        if (LimiteUseCard)
            cMatchRoom.playerBLimiteCardPlayed.Add(new Card() { cardNumber = cUsedNumber });
    }
}
void ResetCardTurnStatusForPlayer(List<Card> backstg, Card stage, Card collab)
{
    foreach (Card eachCard in backstg) {
        eachCard.playedThisTurn = false;
        if (eachCard.suspended)
            eachCard.suspended = false;
    }
    stage.playedThisTurn = false;
    if (stage.suspended)
        stage.suspended = false;
    collab.playedThisTurn = false;
    if (collab.suspended)
        collab.suspended = false;
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
            return hasAttached = true;
        }
        if (duelAction.targetCard.cardNumber.Equals(collab.cardNumber) && duelAction.local.Equals("Collaboration"))
        {
            collab.attachedEnergy.Add(duelAction.usedCard);
            return hasAttached = true;
        }
        int n = 0;
        for (int y = 0; y < backStage.Count; y++) {
            for (int z = 1; z < 6; z++)
            {
                if (backStage[y].cardPosition.Equals("BackStage" + z))
                {
                    n = y;
                    break;

                }
            }
        }
        if (duelAction.targetCard.cardNumber.Equals(backStage[n].cardNumber))
        {
            backStage[n].attachedEnergy.Add(duelAction.usedCard);
            return hasAttached = true;
        }
    }
    return false;
}

async Task GamePhaseDrawAsync(int playerid, List<Card> PlayerHand, Boolean result, MatchRoom mr)
{
    Draw newDraw = new Draw();
    newDraw.playerID = playerid;
    newDraw.zone = "Deck";

    RequestData ReturnData = new RequestData { type = "GamePhase", description = "DrawPhase", requestObject = "" };

    newDraw.cardList = new List<Card>() { PlayerHand[PlayerHand.Count - 1] };
    ReturnData.requestObject = JsonSerializer.Serialize(newDraw);

    Console.WriteLine("\nEnvio " + newDraw.playerID + JsonSerializer.Serialize(ReturnData));

    await playerConnections[newDraw.playerID.ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);

    newDraw.cardList = new List<Card>() { new Card() };
    ReturnData.requestObject = JsonSerializer.Serialize(newDraw);
    Console.WriteLine("\nEnvio " + GetOtherPlayer(mr, newDraw.playerID) + JsonSerializer.Serialize(ReturnData));
    await playerConnections[GetOtherPlayer(mr, newDraw.playerID).ToString()].SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ReturnData))), WebSocketMessageType.Text, result, CancellationToken.None);
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

        // Consoleging
        Console.WriteLine(playerResponse.requestObject);
        Console.WriteLine(opponentResponse.requestObject);

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
        Console.WriteLine("\nInvalid play, no card at stage: " + playerid + matchroom.currentGameHigh);
        return false;
    }

    //check if card in the stage can be there
    List<Record> cardlist = FileReader.QueryRecordsByNameAndBloom(new List<Card>() { playerStage }, "Debut");
    if (cardlist.Count == 0)
    {
        Console.WriteLine("\nInvalid play, no card suitable at stage: " + playerid + matchroom.currentGameHigh);
        return false;
    }
    List<Card> cardsPlayedThisTurn = new() { playerStage };
    //check if backposition is in the maximum limite
    if (playerBackStage.Count > 5)
    {
        Console.WriteLine("\nInvalid play, more cards at the back stage than what it should: " + playerid + matchroom.currentGameHigh);
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
            Console.WriteLine("\nInvalid play, there card in the backstage that shouldnt: " + playerid + matchroom.currentGameHigh);
            return false;
        }
    }
    // the duplicated cards are still around here... need to check why.

    //check if all played cards exists in the player hand
    cardsPlayedThisTurn.AddRange(playerBackStage);

    if (matchroom.firstPlayer == playerid) { 
        if (!(HaveSameObjectCounts(cardsPlayedThisTurn, matchroom.playerAHand)))
        {
            Console.WriteLine("\nInvalid play, there card in the field that are not at player hand: " + playerid + matchroom.currentGameHigh);
            return false;
        }
    } else {
        if (!(HaveSameObjectCounts(cardsPlayedThisTurn, matchroom.playerBHand)))
        {
            Console.WriteLine("\nInvalid play, there card in the field that are not at player hand: " + playerid + matchroom.currentGameHigh);
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
