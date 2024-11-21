using hololive_oficial_cardgame_server;
using hololive_oficial_cardgame_server.SerializableObjects;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

var builder = WebApplication.CreateBuilder(args);

FileReader.ReadFile("CardList.xlsx");

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
});
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
                    Lib.WriteConsoleMessage($"WebSocketException during ReceiveAsync: {wsEx}");
                    break;
                }
                catch (Exception ex)
                {
                    Lib.WriteConsoleMessage($"Exception during ReceiveAsync: {ex}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        }
                    }
                    catch (WebSocketException wsEx)
                    {
                        Lib.WriteConsoleMessage($"WebSocketException during CloseAsync: {wsEx}");
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage($"Exception during CloseAsync: {ex}");
                    }
                    finally
                    {
                        string player = Lib.GetKeyByValue(webSocket);
                        MatchRoom cMatchRoom = MatchRoom.FindPlayerMatchRoom(player);
                        if (cMatchRoom != null)
                        {
                            Lib.EndDuelAsync(cMatchRoom, GetOtherPlayer(cMatchRoom, player));
                        }
                    }
                }
                else
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var playerRequest = JsonSerializer.Deserialize<PlayerRequest>(receivedMessage);

                        if (playerRequest != null && Lib.ValidatePlayerRequest(playerRequest))
                        {
                            await MessageDispatcher.DispatchMessage(playerRequest, webSocket);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Lib.WriteConsoleMessage($"JSON Exception: {jsonEx}");
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage($"Exception during message processing: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Lib.WriteConsoleMessage($"Unhandled Exception: {ex}");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch (WebSocketException wsEx)
                {
                    Lib.WriteConsoleMessage($"WebSocketException during final CloseAsync: {wsEx}");
                }
                catch (Exception ex)
                {
                    Lib.WriteConsoleMessage($"Exception during final CloseAsync: {ex}");
                }
            }

            webSocket.Dispose();
        }
    }
}

// Run the application
app.Run();
