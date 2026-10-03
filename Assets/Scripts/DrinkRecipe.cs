using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct IngredientAmount
{
    public IngredientType ingredient;
    [Min(1)] public int amount;

    public IngredientAmount(IngredientType ingredient, int amount)
    {
        this.ingredient = ingredient;
        this.amount = amount;
    }
}

[CreateAssetMenu(menuName = "BarShift/Drink Recipe", fileName = "DrinkRecipe")]
public class DrinkRecipe : ScriptableObject
{
    [SerializeField] private string displayName = "New Drink";
    [SerializeField] private IngredientAmount[] ingredients;
    [SerializeField] private bool requiresShake;
    [SerializeField, Min(1)] private int basePrice = 12;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Color drinkColor = Color.white;

    public string DisplayName => displayName;
    public IReadOnlyList<IngredientAmount> Ingredients => ingredients;
    public bool RequiresShake => requiresShake;
    public int BasePrice => basePrice;
    public string Description => description;
    public Color DrinkColor => drinkColor;

    public int ExpectedTotal
    {
        get
        {
            int total = 0;
            if (ingredients == null) return total;
            for (int i = 0; i < ingredients.Length; i++)
                total += Mathf.Max(0, ingredients[i].amount);
            return total;
        }
    }

    public int GetExpectedAmount(IngredientType ingredient)
    {
        if (ingredients == null) return 0;

        for (int i = 0; i < ingredients.Length; i++)
        {
            if (ingredients[i].ingredient == ingredient)
                return ingredients[i].amount;
        }

        return 0;
    }

    public string BuildTicketText()
    {
        string text = string.Empty;
        if (ingredients != null)
        {
            for (int i = 0; i < ingredients.Length; i++)
            {
                IngredientAmount item = ingredients[i];
                text += $"{item.amount}x {IngredientInfo.DisplayName(item.ingredient)}";
                if (i < ingredients.Length - 1) text += "\n";
            }
        }

        text += requiresShake ? "\nShake before serving" : "\nDo not shake";
        return text;
    }

    public void ConfigureRuntime(string name, IngredientAmount[] amounts, bool shake, int price, string note, Color color)
    {
        displayName = name;
        ingredients = amounts;
        requiresShake = shake;
        basePrice = price;
        description = note;
        drinkColor = color;
    }
}
