using hololive_oficial_cardgame_server.EffectControllers;

namespace hololive_oficial_cardgame_server.SerializableObjects
{
    public class Art
    {
        public string Name { get; set; }
        public List<(string Color, int Amount)> Cost { get; set; } = new List<(string, int)>();
        public (string Color, int Amount) Damage { get; set; }
        public (string Color, int Amount) DamageMultiplier { get; set; }
        public (string Color, int Amount) ExtraColorDamage { get; set; }

        public static List<CardEffect> ArtEffectList = null;

        public static Art ParseArtFromString(string artString)
        {
            if (ArtEffectList == null)
                GenerateArtEffectData();

            var parts = artString.Split(':');
            if (parts.Length != 5)
                return null; //throw new ArgumentException("Invalid art string format");

            // Create a new instance of Art to hold the parsed data
            var art = new Art();

            // Parse costs (example: "1x無色1x白")
            string costString = parts[0];
            for (int i = 0; i < costString.Length; i++)
            {
                if (char.IsDigit(costString[i]))
                {
                    // Extract the amount
                    int amount = int.Parse(costString[i].ToString());
                    i++;
                    // Extract the color
                    string color = costString[i].ToString();
                    while (i + 1 < costString.Length && !char.IsDigit(costString[i + 1]))
                    {
                        i++;
                        color += costString[i];
                    }
                    // Add to the cost list
                    art.Cost.Add((color.Substring(1), amount));
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
            int extraColorDamage = int.Parse(parts[4].Substring(1));

            // Set the parsed values to the Art object
            art.Damage = (damageColor, damageValue);
            art.DamageMultiplier = (multiplierColor, multiplierValue);
            art.ExtraColorDamage = (extraColor, extraColorDamage);

            // Return the populated Art object
            return art;
        }

        private static void GenerateArtEffectData()
        {
            ArtEffectList = new();

            //hSD01-006
            ArtEffectList.Add(new CardEffect()
            {
                artName = "SorAZ シンパシー",
                cardNumber = "hSD01-006",
                zoneTarget = "Stage",
                ExistXAtZone_Name = "AZKi",
                type = CardEffectType.BuffThisCardDamageExistXAtZone,
                damageType = 50,
                listIndex = 0
            });
        }
    }
    public class ArtCalculator
    {
        public static int CalculateTotalDamage(Art art, List<Card> costs, string extraColor, Card attackingCard, Card AttackedCard, string playerWhoDeclaredAttack, string playerWhoWasTargeted, MatchRoom matchRoom)
        {
            string cardZone = attackingCard.cardPosition;
            // Count the occurrences of each color in the list of cost objects
            var colorCount = costs.GroupBy(cost => cost.color).ToDictionary(g => g.Key, g => g.Count());
            var effectExtraDamage = 0;
            // Base damage calculation
            int baseDamage = 0;
            bool _AreColorRequirementsMet = AreColorRequirementsMet(colorCount, costs);

            List<CardEffect> currentActivatedTurnEffect = new();
            currentActivatedTurnEffect.AddRange(CollabEffects.currentActivatedTurnEffect);
            currentActivatedTurnEffect.AddRange(ArtEffects.currentActivatedTurnEffect);

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
            if (art.ExtraColorDamage.Color == extraColor)
            {
                totalDamage += art.ExtraColorDamage.Amount;
                Lib.WriteConsoleMessage($"Extra Color Damage Added: {art.ExtraColorDamage.Amount} (Extra Color: {extraColor})");
            }

            return totalDamage;
        }

        public static bool AreColorRequirementsMet(Dictionary<string, int> colorCount, List<Card> availableColors)
        {
            // Group and count the available colors
            var availableColorCounts = availableColors
                .GroupBy(item => item.color)
                .ToDictionary(g => g.Key, g => g.Count());

            // Check if the available colors meet the required counts
            bool genericColor = availableColorCounts.ContainsKey("無色");

            foreach (var kvp in colorCount)
            {
                string color = kvp.Key;
                int requiredCount = kvp.Value;

                if (genericColor)
                {
                    continue; // Skip checking this color requirement
                }

                // Regular check for other colors
                if (!availableColorCounts.TryGetValue(color, out int availableCount) || availableCount < requiredCount)
                {
                    return false; // Requirement not met
                }
            }

            return true; // All requirements are met
        }


    }
}
