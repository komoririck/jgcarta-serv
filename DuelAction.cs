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
    public Card usedCard { get; set; }
    public string playedFrom { get; set; }
    public string local { get; set; }
    public Card targetCard { get; set; }
    public Card cheerCostCard { get; set; }
    public string actionType { get; set; }
    public string actionType_Two { get; set; }
    public string actionObject { get; set; }
    public string selectedSkill { get; set; }

    // START OF CONDITIONAL DRAW
    public List<string> SelectedCards { get; set; }
    public List<int> Order { get; set; }
    //END OF CONDITIONAL DRAW

    // START OF DRAW variables
    public bool suffle { get; set; }
    public string zone { get; set; }
    public List<Card> cardList { get; set; }
    // END OF DRAW variables
}