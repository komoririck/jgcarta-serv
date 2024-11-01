﻿using Org.BouncyCastle.Crypto.Digests;
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
    public List<CardEffect> cardEffects { get; set; } = new List<CardEffect>();
    [JsonIgnore]
    public List<Card> attachedEnergy { get; set; } = new List<Card>();
    [JsonIgnore]
    public List<Card> bloomChild { get; set; } = new List<Card>();
    [JsonIgnore]
    public List<Art> Arts = new List<Art>();

    public void GetCardInfo(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
            return;

        if (cardNumber.Equals("0"))
            return;

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
                life = record.Life;

                List<string> words = arts.Split(';').ToList();
                foreach (string art in words)
                {
                    Arts.Add(Art.ParseArtFromString(art));
                }
            }
        }
    }
}
[Serializable]
public class CardEffect
{
    internal int listIndex;

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
}

[Flags]
public enum CardEffectType : byte
{
    None = 0,
    BuffDamageToCardAtZone = 1,
    BuffThisCardDamageExistXAtZone = 2,
    BuffThisCardDamage = 3
}