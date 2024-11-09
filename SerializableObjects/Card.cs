using Org.BouncyCastle.Crypto.Digests;
using System.Diagnostics;
using System.Resources;
using System.Security.Policy;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace hololive_oficial_cardgame_server.SerializableObjects;

[Serializable]
public class Card
{
    public string cardNumber { get; set; } = "";

    [JsonIgnore]
    public int currentHp = 0;
    [JsonIgnore]
    public int effectDamageRecieved = 0;
    [JsonIgnore]
    public int normalDamageRecieved = 0;
    [JsonIgnore]
    public string cardLimit { get; set; }
    [JsonIgnore]
    public string playedFrom { get; set; }
    public string cardPosition { get; set; } = "";
    [JsonIgnore]
    public bool playedThisTurn { get; set; } = true;
    [JsonIgnore]
    public bool suspended { get; set; } = false;
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
    public string life { get; set; }
    [JsonIgnore]
    public string cardTag { get; set; }
    [JsonIgnore]
    public List<CardEffect> cardEffects { get; set; } = new List<CardEffect>();
    [JsonIgnore]
    public List<Card> attachedEnergy { get; set; } = new List<Card>();
    [JsonIgnore]
    public List<Card> bloomChild { get; set; } = new List<Card>();
    [JsonIgnore]
    public List<Card> attachedEquipe = new();
    [JsonIgnore]
    public List<Art> Arts = new List<Art>();
    [JsonIgnore]
    public List<CardEffect> OnAttackEffects = new();

    public Card(string cardNumber = "", string cardPosition = "")
    {
        this.cardNumber = cardNumber;
        if (!string.IsNullOrEmpty(cardPosition))
            this.cardPosition = cardPosition;
        if (!string.IsNullOrEmpty(this.cardNumber))
            GetCardInfo();
    }

    public Card GetCardInfo(bool forceUpdate = false)
    {

        if (!string.IsNullOrEmpty(cardType) && !forceUpdate)
            return null;

        if (cardNumber.Equals("0") || string.IsNullOrEmpty(cardNumber))
            return null;

        foreach (Record record in FileReader.result)
        {
            if (record.CardNumber == this.cardNumber)
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
                life = record.Life;
                cardTag = record.Tag;

                List<string> words = arts.Split(';').ToList();
                foreach (string art in words)
                {
                    Arts.Add(Art.ParseArtFromString(art));
                }
            }
        }
        return this;
    }
    public Card SetCardNumber(string numnber)
    {
        this.cardNumber = numnber;
        return this;
    }
    public Card CloneCard(Card originalCard)
    {
        if (originalCard == null)
        {
            return null;
        }

        Card clonedCard = new Card
        {
            cardNumber = originalCard.cardNumber,
            currentHp = originalCard.currentHp,
            effectDamageRecieved = originalCard.effectDamageRecieved,
            normalDamageRecieved = originalCard.normalDamageRecieved,
            cardLimit = originalCard.cardLimit,
            playedFrom = originalCard.playedFrom,
            cardPosition = originalCard.cardPosition,
            playedThisTurn = originalCard.playedThisTurn,
            suspended = originalCard.suspended,
            name = originalCard.name,
            cardType = originalCard.cardType,
            rarity = originalCard.rarity,
            product = originalCard.product,
            color = originalCard.color,
            hp = originalCard.hp,
            bloomLevel = originalCard.bloomLevel,
            arts = originalCard.arts,
            oshiSkill = originalCard.oshiSkill,
            spOshiSkill = originalCard.spOshiSkill,
            abilityText = originalCard.abilityText,
            illustrator = originalCard.illustrator,
            life = originalCard.life,
            cardTag = originalCard.cardTag,
            attachedEnergy = originalCard.attachedEnergy.Select(card => CloneCard(card)).ToList(),
            bloomChild = originalCard.bloomChild.Select(card => CloneCard(card)).ToList()
        };

        return clonedCard;
    }

}
[Serializable]
public class CardEffect
{
    internal int listIndex;
    internal int diceRollValue;

    public string playerWhoUsedTheEffect { get; set; }
    public string playerWhoIsTheTargetOfEffect { get; set; }
    public string cardNumber { get; set; }
    public string artName { get; set; } 
    public string zoneTarget { get; set; }
    public string cardTarget { get; set; }
    public CardEffectType type { get; set; }
    public int Damage { get; set; }
    public int damageType { get; set; }
    public string nameMatch { get; set; }
    //BuffThisCardDamageExistXAtZone
    public string ExistXAtZone_Name { get; set; }
    public string ExistXAtZone_Color { get; set; }
}

[Flags]
public enum CardEffectType : byte
{
    None = 0,
    BuffDamageToCardAtZone = 1,
    BuffThisCardDamageExistXAtZone = 2,
    BuffThisCardDamage = 3,
    BuffThisCardDamageExistXCOLORAtZone,
    FixedDiceRoll,
    BuffZoneCardDamageExistXCOLORAtZone
}