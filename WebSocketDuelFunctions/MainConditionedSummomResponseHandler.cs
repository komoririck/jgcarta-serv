using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class MainConditionedSummomResponseHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private PlayerRequest _ReturnData;
        private DuelAction _DuelAction;

        public MainConditionedSummomResponseHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }
    }
}