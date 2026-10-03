using System.Collections.Generic;

public class DrinkMixer
{
    private readonly Dictionary<IngredientType, int> amounts = new Dictionary<IngredientType, int>();

    public bool WasShaken { get; private set; }
    public int TotalUnits { get; private set; }

    public DrinkMixer()
    {
        Reset();
    }

    public bool TryAdd(IngredientType ingredient, int maxUnits)
    {
        if (TotalUnits >= maxUnits)
            return false;

        if (!amounts.ContainsKey(ingredient))
            amounts[ingredient] = 0;

        amounts[ingredient]++;
        TotalUnits++;
        return true;
    }

    public int GetAmount(IngredientType ingredient)
    {
        return amounts.TryGetValue(ingredient, out int value) ? value : 0;
    }

    public IReadOnlyDictionary<IngredientType, int> GetAmounts()
    {
        return amounts;
    }

    public void Shake()
    {
        WasShaken = true;
    }

    public void Reset()
    {
        amounts.Clear();
        foreach (IngredientType ingredient in System.Enum.GetValues(typeof(IngredientType)))
            amounts[ingredient] = 0;

        TotalUnits = 0;
        WasShaken = false;
    }

    public string BuildContentsText()
    {
        if (TotalUnits == 0)
            return "Glass is empty";

        string result = string.Empty;
        foreach (IngredientType ingredient in System.Enum.GetValues(typeof(IngredientType)))
        {
            int amount = GetAmount(ingredient);
            if (amount <= 0) continue;

            if (result.Length > 0) result += "   ";
            result += $"{IngredientInfo.DisplayName(ingredient)} {amount}";
        }

        if (WasShaken) result += "   •   SHAKEN";
        return result;
    }
}
