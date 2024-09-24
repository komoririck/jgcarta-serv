using Newtonsoft.Json;
namespace hololive_oficial_cardgame_server;

public class DuelAction
{
    public int playerID { get; set; }
    public Card usedCard { get; set; } = new Card();
    public string playedFrom { get; set; }
    public string local { get; set; }
    public Card targetCard { get; set; } = new Card();

    [JsonIgnore]
    public List<CardEffect> cardEffects { get; set; } = new List<CardEffect>();
}