using System.Text.Json.Serialization;

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
    public string? cardLimit { get; set; }
   
    public string? playedFrom { get; set; }
    public string cardPosition { get; set; } = "";
    [JsonIgnore]
    public bool playedThisTurn { get; set; } = true;
    [JsonIgnore]
    public bool suspended { get; set; } = false;
    [JsonIgnore]
    public string? cardName { get; set; }
    [JsonIgnore]
    public string? cardType { get; set; }
    [JsonIgnore]
    public string? rarity { get; set; }
    [JsonIgnore]
    public string? product { get; set; }
    [JsonIgnore]
    public string? color { get; set; }
    [JsonIgnore]
    public string? hp { get; set; }
    [JsonIgnore]
    public string? bloomLevel { get; set; }
    [JsonIgnore]
    public string? arts { get; set; }
    [JsonIgnore]
    public string? oshiSkill { get; set; }
    [JsonIgnore]
    public string? spOshiSkill { get; set; }
    [JsonIgnore]
    public string? abilityText { get; set; }
    [JsonIgnore]
    public string? illustrator { get; set; }
    [JsonIgnore]
    public string? life { get; set; }
    [JsonIgnore]
    public string? cardTag { get; set; }
    [JsonIgnore]
    public List<CardEffect> cardEffects { get; set; } = new ();
    [JsonIgnore]
    public List<Card> attachedEnergy { get; set; } = new ();
    [JsonIgnore]
    public List<Card> bloomChild { get; set; } = new ();
    [JsonIgnore]
    public List<Card> attachedEquipe = new();
    [JsonIgnore]
    public List<Art> Arts = new List<Art>();
    [JsonIgnore]
    public List<CardEffect> OnAttackEffects = new();

    public Card(string? cardNumber = "", string? cardPosition = "")
    {
        this.cardNumber = cardNumber;
        if (!string.IsNullOrEmpty(cardPosition))
            this.cardPosition = cardPosition;
        if (!string.IsNullOrEmpty(this.cardNumber))
            GetCardInfo();
    }

    public Card? GetCardInfo(bool forceUpdate = false)
    {
        if (cardNumber.Equals("0") || string.IsNullOrEmpty(cardNumber))
            return null;

        if (!string.IsNullOrEmpty(cardType) && !forceUpdate)
            return null;

        Record record = FileReader.result[this.cardNumber];

            if (record.CardNumber == this.cardNumber)
            {
                cardNumber = record.CardNumber;
                cardName = record.Name;
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
            cardName = originalCard.cardName,
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
    public int diceRollValue = 0;
    public int activatedTurn = 0;
    public string? cardTag;
    public string? playerWhoUsedTheEffect { get; set; }
    public string? playerWhoIsTheTargetOfEffect { get; set; }
    public string? cardNumber { get; set; }
    public string? artName { get; set; } 
    public string? zoneTarget { get; set; }
    public string? cardTarget { get; set; }
    public CardEffectType type { get; set; }
    public int Damage { get; set; }
    public int damageType { get; set; }
    public string? nameMatch { get; set; }
    //BuffThisCardDamageExistXAtZone
    public string? ExistXAtZone_Name { get; set; }
    public string? ExistXAtZone_Color { get; set; }
    public int IncreaseCostAmount { get; internal set; }
    //BuffDamageToCardAtZoneIfOtherCardNameAtZoneHaveTag
    public string? zoneThatShouldHaveTag { get; set; }
    public string? nameThatShouldntExistAtZone { get; set; }
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
    BuffZoneCardDamageExistXCOLORAtZone,
    BuffDamageToCardAtZoneIfHaveTag,
    BuffDamageToCardAtZoneIfOtherCardNameAtZoneHaveTag,
    BlockRetreat,
    OneUseFixedDiceRoll,
    ProtectFromOneLifeCostCharge,
    IncreaseLifeCostIfDamageSurpassX,
    BuffDamageToCardAtZoneMultiplyByBackstageCount,
    BuffDamageToCardAtZoneIfHasATool,
    BuffThisCardDamageIfAtZoneAndMultplyByCheer,
    BuffDamageToCardAtZoneMultiplyByAmountOfToolAtYourSide,
    BuffThisCardDamageMultplyByEachTag
}