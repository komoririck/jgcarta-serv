using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
Lib.WriteConsoleMessage($"Listening for WebSocket connections at: {webSocketUrl}");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


DBConnection.StartServerClearQueue();

// Add WebSocket handling middleware before other middlewares
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == webSocketPath && context.Request.Headers["Upgrade"] == "websocket")
    {
        await HandleWebSocketAsync(context);
    }
    else
    {
        await next(); // Continue to the next middleware
    }
});

async Task HandleWebSocketAsync(HttpContext context)
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];

        try
        {
            var messageDispatcher = new MessageDispatcher();

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = null;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException wsEx)
                {
                    // Log the exception and break the loop to clean up
                    Lib.WriteConsoleMessage($"WebSocketException during ReceiveAsync: {wsEx}");
                    break;
                }
                catch (Exception ex)
                {
                    // Log unexpected exceptions and break the loop
                    Lib.WriteConsoleMessage($"Exception during ReceiveAsync: {ex}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        // Attempt to close the WebSocket gracefully
                        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        }
                    }
                    catch (WebSocketException wsEx)
                    {
                        // Log the exception but continue to clean up
                        Lib.WriteConsoleMessage($"WebSocketException during CloseAsync: {wsEx}");
                    }
                    catch (Exception ex)
                    {
                        // Log unexpected exceptions
                        Lib.WriteConsoleMessage($"Exception during CloseAsync: {ex}");
                    }
                    finally
                    {
                        string palyer = Lib.GetKeyByValue(webSocket);
                        MatchRoom cMatchRoom = null;
                        int matchnumber = MatchRoom.FindPlayerMatchRoom(MessageDispatcher._MatchRooms, palyer);
                        if (matchnumber != -1) { 
                            cMatchRoom = MessageDispatcher._MatchRooms[matchnumber];
                            Lib.EndDuelAsync(true, cMatchRoom,  GetOtherPlayer(cMatchRoom, int.Parse(palyer)));
                        }
                    }
                }
                else
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        // Deserialize the received JSON message
                        var playerRequest = JsonSerializer.Deserialize<PlayerRequest>(receivedMessage);

                        if (playerRequest != null && Lib.ValidatePlayerRequest(playerRequest))
                        {
                            // Dispatch the message to the appropriate handler
                            await MessageDispatcher.DispatchMessage(playerRequest, webSocket);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        // Handle JSON deserialization errors
                        Lib.WriteConsoleMessage($"JSON Exception: {jsonEx}");
                    }
                    catch (Exception ex)
                    {
                        // Handle other unexpected exceptions
                        Lib.WriteConsoleMessage($"Exception during message processing: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred outside the receive loop
            Lib.WriteConsoleMessage($"Unhandled Exception: {ex}");
        }
        finally
        {
            // Ensure the WebSocket is closed if it's still open
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch (WebSocketException wsEx)
                {
                    // Log the exception but proceed to dispose
                    Lib.WriteConsoleMessage($"WebSocketException during final CloseAsync: {wsEx}");
                }
                catch (Exception ex)
                {
                    // Log unexpected exceptions
                    Lib.WriteConsoleMessage($"Exception during final CloseAsync: {ex}");
                }
            }

            // Dispose the WebSocket to free resources
            webSocket.Dispose();
        }
    }

}

// Run the application
app.Run();
