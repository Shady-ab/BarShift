using UnityEngine;

public enum ReactionTier
{
    Perfect,
    Good,
    Bad,
    Terrible
}

public class DrinkResult
{
    public float accuracy;
    public ReactionTier tier;
    public bool techniqueCorrect;
    public int payment;
    public int tip;
    public int totalEarned;
    public string reaction;
    public string detail;
}

public static class DrinkEvaluator
{
    public static DrinkResult Evaluate(DrinkRecipe recipe, DrinkMixer mixer, BarGameConfig config, float patienceRatio)
    {
        int expectedTotal = Mathf.Max(1, recipe.ExpectedTotal);
        int playerTotal = Mathf.Max(0, mixer.TotalUnits);
        int matchedUnits = 0;

        foreach (IngredientType ingredient in System.Enum.GetValues(typeof(IngredientType)))
        {
            int expected = recipe.GetExpectedAmount(ingredient);
            int actual = mixer.GetAmount(ingredient);
            matchedUnits += Mathf.Min(expected, actual);
        }

        int comparisonTotal = Mathf.Max(expectedTotal, playerTotal);
        float ingredientAccuracy = comparisonTotal > 0 ? (float)matchedUnits / comparisonTotal : 0f;

        bool techniqueCorrect = mixer.WasShaken == recipe.RequiresShake;
        float techniqueMultiplier = techniqueCorrect ? 1f : config.wrongTechniqueMultiplier;
        float finalAccuracy = Mathf.Clamp01(ingredientAccuracy * techniqueMultiplier);

        ReactionTier tier;
        if (finalAccuracy >= config.perfectThreshold) tier = ReactionTier.Perfect;
        else if (finalAccuracy >= config.goodThreshold) tier = ReactionTier.Good;
        else if (finalAccuracy >= config.badThreshold) tier = ReactionTier.Bad;
        else tier = ReactionTier.Terrible;

        int payment = CalculatePayment(recipe.BasePrice, tier);
        int tip = CalculateTip(tier, config, Mathf.Clamp01(patienceRatio));

        return new DrinkResult
        {
            accuracy = finalAccuracy,
            tier = tier,
            techniqueCorrect = techniqueCorrect,
            payment = payment,
            tip = tip,
            totalEarned = payment + tip,
            reaction = ReactionText(tier),
            detail = BuildDetail(tier, techniqueCorrect, patienceRatio)
        };
    }

    private static int CalculatePayment(int basePrice, ReactionTier tier)
    {
        switch (tier)
        {
            case ReactionTier.Perfect:
            case ReactionTier.Good:
                return basePrice;
            case ReactionTier.Bad:
                return Mathf.Max(1, Mathf.RoundToInt(basePrice * 0.65f));
            default:
                return Mathf.Max(1, Mathf.RoundToInt(basePrice * 0.30f));
        }
    }

    private static int CalculateTip(ReactionTier tier, BarGameConfig config, float patienceRatio)
    {
        int tip = 0;
        if (tier == ReactionTier.Perfect) tip = config.perfectTip;
        else if (tier == ReactionTier.Good) tip = config.goodTip;

        if ((tier == ReactionTier.Perfect || tier == ReactionTier.Good) && patienceRatio >= config.fastBonusThreshold)
            tip += config.fastBonusTip;

        return tip;
    }

    private static string ReactionText(ReactionTier tier)
    {
        switch (tier)
        {
            case ReactionTier.Perfect: return "That's exactly right!";
            case ReactionTier.Good: return "Nice. I'd order that again.";
            case ReactionTier.Bad: return "Hmm... something is off.";
            default: return "Nope. That is definitely not my drink.";
        }
    }

    private static string BuildDetail(ReactionTier tier, bool techniqueCorrect, float patienceRatio)
    {
        string detail = tier == ReactionTier.Perfect ? "Recipe accuracy was excellent." : "The recipe could be closer to the ticket.";

        if (!techniqueCorrect)
            detail += " The preparation technique was wrong.";
        else if (patienceRatio < 0.2f)
            detail += " The customer waited a long time.";
        else if (patienceRatio > 0.55f && (tier == ReactionTier.Perfect || tier == ReactionTier.Good))
            detail += " Fast service earned a bonus tip.";

        return detail;
    }
}
