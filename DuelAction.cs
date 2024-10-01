using Mysqlx.Expr;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using ZstdSharp.Unsafe;
namespace hololive_oficial_cardgame_server;

public class DuelAction
{
    public int playerID { get; set; }
    public Card usedCard { get; set; } = new Card();
    public string playedFrom { get; set; }
    public string local { get; set; }
    public Card targetCard { get; set; } = new Card();
    public string actionType { get; set; }
    public string actionObject { get; set; }
    [JsonIgnore]
    public List<CardEffect> cardEffects { get; set; } = new List<CardEffect>();
}