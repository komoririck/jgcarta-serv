﻿using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Linq;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.SerializableObjects
{
    public class Art
    {
        public string Name { get; set; }
        public List<(string Color, int Amount)> Cost { get; set; } = new List<(string, int)>();
        public (string Color, int Amount) Damage { get; set; }
        public (string Color, int Amount) DamageMultiplier { get; set; }
        public (string Color, int Amount) ExtraColorDamage { get; set; }

        public static Art ParseArtFromString(string artString)
        {
            var parts = artString.Split(':');
            if (parts.Length != 5)
                return null; //throw new ArgumentException("Invalid art string format");

            // Create a new instance of Art to hold the parsed data
            var art = new Art();

            // Parse costs (example: "1x無色1x白")
            string costString = parts[0];
            var splitedCosts = costString.Split('*');

            foreach (string cost in splitedCosts)
            {
                var splitedcostString = cost.Split('x');
                // Ensure we have two parts
                if (splitedcostString.Length == 2)
                {
                    // Try to parse the first part as an integer
                    if (int.TryParse(splitedcostString[0], out int number))
                    {
                        string text = splitedcostString[1]; // Get the string part
                        art.Cost.Add((text, number));
                    }
                }
            }
            art.Name = parts[1];

            // Parse damage (example: "50x1白")
            var damageParts = parts[2].Split('x');
            int damageValue = int.Parse(damageParts[0]);
            string damageColor = damageParts[1];

            // Parse damage multiplier (example: "0x1白")
            var multiplierParts = parts[3].Split('x');
            int multiplierValue = int.Parse(multiplierParts[0]);
            string multiplierColor = multiplierParts[1];

            // Parse extra color damage (example: "青0")
            string extraColor = parts[4].Substring(0, 1);
            int extraColorDamage = 0;
            int numberStartIndex = parts[4].Length - 1;
            while (numberStartIndex > 0 && !char.IsDigit(parts[4][numberStartIndex]))
                numberStartIndex--;

            // Extract the color part and parse the damage part
            extraColor = parts[4].Substring(0, numberStartIndex);
            extraColorDamage = int.Parse(parts[4].Substring(numberStartIndex));

            // Set the parsed values to the Art object
            art.Damage = (damageColor, damageValue);
            art.DamageMultiplier = (multiplierColor, multiplierValue);
            art.ExtraColorDamage = (extraColor, extraColorDamage);

            // Return the populated Art object
            return art;
        }
    }
    public class ArtCalculator
    {
        public static int CalculateTotalDamage(Art art, Card attackingCard, Card AttackedCard, string playerWhoDeclaredAttack, string playerWhoWasTargeted, MatchRoom matchRoom)
        {

            string cardZone = attackingCard.cardPosition;
            // Count the occurrences of each color in the list of cost objects
            var colorCount = attackingCard.attachedEnergy.GroupBy(cost => cost.color).ToDictionary(g => g.Key, g => g.Count());
            var effectExtraDamage = 0;
            // Base damage calculation
            int baseDamage = 0;
            // public bool IsCostCovered(List<(string Color, int Amount)> cost, Dictionary<string, List<Card>> energyAmount, Card SkillOwnerCard)
            bool _AreColorRequirementsMet = IsCostCovered(art.Cost, attackingCard.attachedEnergy, attackingCard);

            List<CardEffect> currentActivatedTurnEffect = matchRoom.ActiveEffects;

            currentActivatedTurnEffect.AddRange(attackingCard.OnAttackEffects);
            currentActivatedTurnEffect.AddRange(AttackedCard.OnAttackEffects);

            //loop for collab active till turn effects
            foreach (CardEffect cardeffect in currentActivatedTurnEffect)
            {

                //BuffDamageToCardAtZone
                if (cardeffect.type == CardEffectType.BuffDamageToCardAtZone)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    //if this card is in the zone that the effect is active
                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffDamageToCardAtZoneMultiplyByBackstageCount) {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;
                    //if this card is in the zone that the effect is active
                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        int mulplier = cardeffect.playerWhoIsTheTargetOfEffect.Equals(matchRoom.firstPlayer) ? matchRoom.playerABackPosition.Count : matchRoom.playerBBackPosition.Count;
                        effectExtraDamage += cardeffect.Damage * mulplier;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffDamageToCardAtZoneIfHasATool)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    bool hasTool = false;
                    foreach (Card card in attackingCard.attachedEquipe) {
                        if (card.cardType.Equals("サポート・ツール")) { 
                            hasTool = true;
                            break;
                        }
                    }
                    
                    if (cardeffect.zoneTarget.Equals(cardZone) && hasTool)
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffDamageToCardAtZoneMultiplyByAmountOfToolAtYourSide)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        int mulplier = 0;
                        Card stage = cardeffect.playerWhoIsTheTargetOfEffect.Equals(matchRoom.firstPlayer) ? matchRoom.playerAStage : matchRoom.playerBStage;
                        Card collab = cardeffect.playerWhoIsTheTargetOfEffect.Equals(matchRoom.firstPlayer) ? matchRoom.playerACollaboration : matchRoom.playerBCollaboration;
                        List<Card> backstage = cardeffect.playerWhoIsTheTargetOfEffect.Equals(matchRoom.firstPlayer) ? matchRoom.playerABackPosition : matchRoom.playerBBackPosition;

                        foreach (Card card in stage.attachedEquipe) 
                        {
                            if (card.cardType.Equals("サポート・ツール"))
                                mulplier++;
                        }
                        foreach (Card card in collab.attachedEquipe)
                        {
                            if (card.cardType.Equals("サポート・ツール"))
                                mulplier++;
                        }
                        foreach (Card card in backstage) {
                            foreach (Card cardEquip in card.attachedEquipe)
                            {
                                if (card.cardType.Equals("サポート・ツール"))
                                    mulplier++;
                            }
                        }

                        effectExtraDamage += cardeffect.Damage * mulplier;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffThisCardDamageIfAtZoneAndMultplyByCheer)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        int mulplier = Math.Min(attackingCard.attachedEnergy.Count, 5);
                        effectExtraDamage += cardeffect.Damage * mulplier;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffDamageToCardAtZoneIfHaveTag)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    if (!attackingCard.cardTag.Contains(cardeffect.cardTag))
                        continue;

                    //if this card is in the zone that the effect is active
                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffDamageToCardAtZoneIfOtherCardNameAtZoneHaveTag)
                {
                    if (!(cardeffect.playerWhoUsedTheEffect.Equals(playerWhoDeclaredAttack)))
                        continue;

                    if (!(cardeffect.playerWhoIsTheTargetOfEffect.Equals(playerWhoWasTargeted)))
                        continue;

                    if (cardeffect.zoneThatShouldHaveTag.Equals("Stage"))
                    {
                        Card stage = cardeffect.playerWhoIsTheTargetOfEffect.Equals(matchRoom.firstPlayer) ? matchRoom.playerAStage : matchRoom.playerBStage;
                        if (!(!cardeffect.nameThatShouldntExistAtZone.Equals(stage.name) && stage.cardTag.Contains(cardeffect.cardTag))) {
                            continue;
                        }
                    }
                    //if this card is in the zone that the effect is active
                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                //BuffDamageToCardAtZoneIfNameMatch
                else if (cardeffect.type == CardEffectType.BuffThisCardDamageExistXAtZone)
                {
                    if (!cardeffect.artName.Equals(art.Name))
                        continue;

                    if (!cardeffect.cardNumber.Equals(attackingCard.cardNumber))
                        continue;

                    // if this card is in the same zone as the effect need another card, continue
                    if (cardeffect.zoneTarget.Equals(cardZone))
                        continue;

                    Card cardAtStage = matchRoom.firstPlayer.Equals(playerWhoDeclaredAttack) ? matchRoom.playerAStage : matchRoom.playerBStage;

                    // if the name didnt match what the effect need, continue
                    if (!cardeffect.ExistXAtZone_Name.Equals(cardAtStage.name))
                        continue;

                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffThisCardDamageExistXCOLORAtZone)
                {
                    Card cardAtStage = matchRoom.firstPlayer.Equals(playerWhoDeclaredAttack) ? matchRoom.playerAStage : matchRoom.playerBStage;
                    cardAtStage.GetCardInfo();
                    // if the name didnt match what the effect need, continue
                    if (!cardeffect.ExistXAtZone_Color.Equals(cardAtStage.color))
                        continue;

                    if (cardeffect.zoneTarget.Equals(cardZone))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffZoneCardDamageExistXCOLORAtZone)
                {
                    Card cardAtZone = null;
                    if (cardeffect.zoneTarget.Equals("Stage"))
                        cardAtZone = matchRoom.firstPlayer.Equals(playerWhoDeclaredAttack) ? matchRoom.playerAStage : matchRoom.playerBStage;
                    else if (cardeffect.zoneTarget.Equals("Collaboration"))
                        cardAtZone = matchRoom.firstPlayer.Equals(playerWhoDeclaredAttack) ? matchRoom.playerACollaboration : matchRoom.playerBCollaboration;

                    cardAtZone.GetCardInfo();
                    // if the name didnt match what the effect need, continue
                    if (!cardeffect.ExistXAtZone_Color.Equals(cardAtZone.color))
                        continue;

                    if (cardeffect.zoneTarget.Equals(cardAtZone.cardPosition))
                    {
                        effectExtraDamage += cardeffect.Damage;
                    }
                }
                else if (cardeffect.type == CardEffectType.BuffThisCardDamage)
                {
                    if (!cardeffect.cardNumber.Equals(attackingCard.cardNumber))
                        continue;

                    if (!cardeffect.zoneTarget.Equals(cardZone))
                        continue;

                    Card cardAtStage = matchRoom.firstPlayer.Equals(playerWhoDeclaredAttack) ? matchRoom.playerAStage : matchRoom.playerBStage;
                    effectExtraDamage += cardeffect.Damage;
                }
            }

            //CHECK FOR EQUIPMENTS EFFECTS
            foreach (Card card in AttackedCard.attachedEquipe) {
                switch (card.cardNumber)
                {
                    case "hBP01-121":
                        if (AttackedCard.cardPosition.Equals("Stage") || AttackedCard.cardPosition.Equals("Collaboration"))
                        effectExtraDamage -= 10;
                        break;
                    case "hBP01-126":
                        effectExtraDamage += 10;
                        break;
                }            
            }
            foreach (Card card in attackingCard.attachedEquipe)
            {
                switch (card.cardNumber)
                {
                    case "hBP01-120":
                        effectExtraDamage += 10;
                        break;
                    case "hBP01-115":
                        effectExtraDamage += 10;
                        break;
                    case "hBP01-114":
                        matchRoom.RecoilDuelActions.Add(
                        new DuelAction ()
                        {
                            playerID = matchRoom.currentPlayerTurn,
                            targetCard = attackingCard,
                            actionObject = "10"
                        });
                        effectExtraDamage += 20;
                        break;
                    case "hBP01-116":
                        effectExtraDamage += 10;
                        break;
                }
            }

            if (_AreColorRequirementsMet)
            {
                baseDamage += art.Damage.Amount * 1 + effectExtraDamage;
            }
            else
            {
                baseDamage = -100000;
            }

            Lib.WriteConsoleMessage($"Base Damage: {baseDamage}");

            // Multiplier calculation
            int totalDamage = baseDamage;
            if (colorCount.ContainsKey(art.DamageMultiplier.Color))
            {
                int multiplier = art.DamageMultiplier.Amount * colorCount[art.DamageMultiplier.Color];
                totalDamage += multiplier;
                Lib.WriteConsoleMessage($"Multiplier Applied: {multiplier} (Multiplier: {art.DamageMultiplier.Amount})");
            }

            // Extra color damage calculation
            if (art.ExtraColorDamage.Color.Equals(AttackedCard.color))
            {
                totalDamage += art.ExtraColorDamage.Amount;
                Lib.WriteConsoleMessage($"Extra Color Damage Added: {art.ExtraColorDamage.Amount} (Extra Color: {AttackedCard.color})");
            }

            return totalDamage;
        }
        public static bool IsCostCovered(List<(string Color, int Amount)> cost, List<Card> energyAmount, Card SkillOwnerCard)
        {
            List<(string Color, int Amount)> payment = energyAmount.GroupBy(card => card.color).Select(group => (Color: group.Key, Amount: group.Count())).ToList();

            foreach (Card card in SkillOwnerCard.attachedEquipe)
            {
                switch (card.cardNumber)
                {
                    case "hBP01-126":
                        int index = payment.FindIndex(p => p.Color == "赤");
                        payment[index] = (payment[index].Color, (payment[index].Amount + 1));
                        break;
                    case "hBP01-118":
                        index = payment.FindIndex(p => p.Color == "白");
                        payment[index] = (payment[index].Color, (payment[index].Amount + 1));
                        break;
                }
            }

            foreach (var (Color, Amount) in cost.ToList())
            {
                int remainingAmount = Amount;

                for (int i = 0; i < payment.Count && remainingAmount > 0; i++)
                {
                    if (payment[i].Color.Equals(Color))
                    {
                        int deductAmount = Math.Min(payment[i].Amount, remainingAmount);
                        payment[i] = (payment[i].Color, payment[i].Amount - deductAmount);
                        remainingAmount -= deductAmount;

                        if (payment[i].Amount == 0)
                            payment.RemoveAt(i--);
                    }
                }

                if (remainingAmount > 0)
                {
                    for (int i = 0; i < payment.Count && remainingAmount > 0; i++)
                    {
                        if (Color.Equals("無色"))
                        {
                            int deductAmount = Math.Min(payment[i].Amount, remainingAmount);
                            payment[i] = (payment[i].Color, payment[i].Amount - deductAmount);
                            remainingAmount -= deductAmount;

                            if (payment[i].Amount == 0)
                                payment.RemoveAt(i--);
                        }
                    }
                }

                if (remainingAmount > 0)
                    return false;

                cost.Remove((Color, Amount));
            }

            return true;
        }

    }
}
