using System.Diagnostics;
using System.Resources;
using System.Security.Policy;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace hololive_oficial_cardgame_server;

[Serializable]
public class Card
{
    public string cardNumber { get; set; } = "";

    [JsonIgnore]
    public string cardLimit { get; set; } = "";

    [JsonIgnore]
    public string playedFrom { get; set; } = "";

    public string cardPosition { get; set; } = "";

    [JsonIgnore]
    public string name { get; set; }

    [JsonIgnore]
    public string cardType { get; set; }

    [JsonIgnore]
    public string rarity { get; set; }

    [JsonIgnore]
    public string product { get; set; }

    [JsonIgnore]
    public string color { get; set; }

    [JsonIgnore]
    public string hp { get; set; }

    [JsonIgnore]
    public string bloomLevel { get; set; }

    [JsonIgnore]
    public string arts { get; set; }

    [JsonIgnore]
    public string oshiSkill { get; set; }

    [JsonIgnore]
    public string spOshiSkill { get; set; }

    [JsonIgnore]
    public string abilityText { get; set; }
    [JsonIgnore]
    public string illustrator { get; set; }

    [JsonIgnore]
    public List<CardEffect> cardEffects { get; set; } = new List<CardEffect>();

    [JsonIgnore]
    public List<Card> attachedEnergy { get; set; } = new List<Card>();

    public void GetCardInfo(string cardNumber)
    {

        if (string.IsNullOrEmpty(cardNumber))
        {
            return;
        }

        if (cardNumber.Equals("0"))
        {
            return;
        }

        foreach (Record record in FileReader.result)
        {
            if (record.CardNumber == cardNumber)
            {
                cardNumber = record.CardNumber;
                name = record.Name;
                cardType = record.CardType;
                rarity = record.Rarity;
                product = record.Product;
                color = record.Color;
                hp = record.HP;
                bloomLevel = record.BloomLevel;
                arts = record.Arts;
                oshiSkill = record.OshiSkill;
                spOshiSkill = record.SPOshiSkill;
                abilityText = record.AbilityText;
                illustrator = record.Illustrator;
            }
        }
    }
}
[Serializable]
public class CardEffect
{
    public string effectTrigger { get; set; } = "";
    public string text { get; set; } = "";
    public int usageLimit { get; set; } = 0;

    [JsonIgnore] // Ignore this during serialization
    public Card target { get; set; }
    public int continuousEffect { get; set; } = 0;
    public string responseType { get; set; } = "";
    public string activationPhase { get; set; } = "";
}

