using UnityEngine;

public enum IngredientType
{
    Lime,
    Mint,
    Lemon,
    Orange,
    Pineapple,
    Strawberry,
    Soda
}

public static class IngredientInfo
{
    public static string DisplayName(IngredientType ingredient)
    {
        switch (ingredient)
        {
            case IngredientType.Lime: return "Lime";
            case IngredientType.Mint: return "Mint";
            case IngredientType.Lemon: return "Lemon";
            case IngredientType.Orange: return "Orange";
            case IngredientType.Pineapple: return "Pineapple";
            case IngredientType.Strawberry: return "Strawberry";
            case IngredientType.Soda: return "Soda";
            default: return ingredient.ToString();
        }
    }

    public static Color Color(IngredientType ingredient)
    {
        switch (ingredient)
        {
            case IngredientType.Lime: return new Color(0.52f, 0.86f, 0.31f);
            case IngredientType.Mint: return new Color(0.23f, 0.72f, 0.48f);
            case IngredientType.Lemon: return new Color(0.97f, 0.84f, 0.25f);
            case IngredientType.Orange: return new Color(0.96f, 0.52f, 0.18f);
            case IngredientType.Pineapple: return new Color(0.97f, 0.72f, 0.22f);
            case IngredientType.Strawberry: return new Color(0.91f, 0.25f, 0.36f);
            case IngredientType.Soda: return new Color(0.58f, 0.82f, 0.94f);
            default: return UnityEngine.Color.white;
        }
    }
}
