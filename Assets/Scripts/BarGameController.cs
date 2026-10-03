using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BarGameState
{
    MainMenu,
    Instructions,
    Preparing,
    ShowingResult,
    ShiftComplete
}

public class BarGameController : MonoBehaviour
{
    private const string BestShiftKey = "BarShift_BestEarnings";

    private BarUI ui;
    private GameAudio audioPlayer;
    private BarGameConfig config;
    private DrinkRecipe[] recipes;
    private readonly DrinkMixer mixer = new DrinkMixer();
    private readonly List<CustomerData> customers = new List<CustomerData>();

    private BarGameState state = BarGameState.MainMenu;
    private DrinkRecipe currentRecipe;
    private CustomerData currentCustomer;
    private float patienceRemaining;
    private int customersServed;
    private int totalEarnings;
    private int bestEarnings;
    private int perfectCount;
    private int goodCount;
    private int badCount;
    private int terribleCount;
    private int previousRecipeIndex = -1;
    private bool actionLocked;

    public BarGameConfig Config => config;
    public BarGameState State => state;

    private void Awake()
    {
        ui = GetComponent<BarUI>();
        if (ui == null) ui = gameObject.AddComponent<BarUI>();

        audioPlayer = GetComponent<GameAudio>();
        if (audioPlayer == null) audioPlayer = gameObject.AddComponent<GameAudio>();

        LoadData();
        BuildCustomers();
        bestEarnings = PlayerPrefs.GetInt(BestShiftKey, 0);
    }

    private void Start()
    {
        ui.Initialize(this);
        GoToMainMenu();
    }

    private void Update()
    {
        if (state != BarGameState.Preparing)
            return;

        if (!actionLocked)
        {
            patienceRemaining -= Time.deltaTime;

            if (patienceRemaining <= 0f)
            {
                patienceRemaining = 0f;
                ui.SetPatience(0f, 0f);
                CustomerTimedOut();
                return;
            }

            HandleKeyboardInput();
        }

        float ratio = Mathf.Clamp01(patienceRemaining / config.customerPatienceSeconds);
        ui.SetPatience(ratio, patienceRemaining);
    }

    private void LoadData()
    {
        config = Resources.Load<BarGameConfig>("Data/BarGameConfig");
        if (config == null)
            config = BarGameConfig.CreateRuntimeDefault();

        recipes = Resources.LoadAll<DrinkRecipe>("Data/Recipes");
        if (recipes == null || recipes.Length == 0)
            recipes = BuildRuntimeRecipes();
    }

    private void BuildCustomers()
    {
        customers.Clear();
        customers.Add(new CustomerData("Maya", "Knows exactly what she ordered.", new Color(0.86f, 0.44f, 0.53f), "M"));
        customers.Add(new CustomerData("Amir", "In a hurry after a long shift.", new Color(0.28f, 0.59f, 0.79f), "A"));
        customers.Add(new CustomerData("Lina", "Usually leaves a good tip.", new Color(0.64f, 0.47f, 0.82f), "L"));
        customers.Add(new CustomerData("Omar", "Very picky about preparation.", new Color(0.31f, 0.68f, 0.54f), "O"));
        customers.Add(new CustomerData("Noa", "Likes bright, fruity drinks.", new Color(0.93f, 0.58f, 0.28f), "N"));
        customers.Add(new CustomerData("Daniel", "A regular who notices mistakes.", new Color(0.48f, 0.55f, 0.68f), "D"));
    }

    public void StartShift()
    {
        audioPlayer.Click();
        customersServed = 0;
        totalEarnings = 0;
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        terribleCount = 0;
        previousRecipeIndex = -1;
        actionLocked = false;
        mixer.Reset();
        BeginNextCustomer();
    }

    public void RestartShift()
    {
        StartShift();
    }

    public void GoToMainMenu()
    {
        StopAllCoroutines();
        actionLocked = false;
        state = BarGameState.MainMenu;
        mixer.Reset();
        ui.ShowMainMenu(bestEarnings);
    }

    public void ShowInstructions()
    {
        audioPlayer.Click();
        state = BarGameState.Instructions;
        ui.ShowInstructions();
    }

    public void BackFromInstructions()
    {
        audioPlayer.Click();
        GoToMainMenu();
    }

    public void QuitGame()
    {
        audioPlayer.Click();
#if UNITY_EDITOR
        Debug.Log("Quit requested. Application.Quit only closes a built player.");
#else
        Application.Quit();
#endif
    }

    public void AddIngredient(IngredientType ingredient)
    {
        if (state != BarGameState.Preparing || actionLocked)
            return;

        if (!mixer.TryAdd(ingredient, config.maxUnitsInGlass))
        {
            ui.FlashHint("Glass is full - reset or serve it.");
            return;
        }

        StartCoroutine(PourSequence(ingredient));
    }

    private IEnumerator PourSequence(IngredientType ingredient)
    {
        actionLocked = true;
        audioPlayer.Pour();
        yield return ui.AnimatePour(ingredient);
        ui.SetMix(mixer, config.maxUnitsInGlass);
        actionLocked = false;
    }

    public void ResetGlass()
    {
        if (state != BarGameState.Preparing || actionLocked)
            return;

        audioPlayer.Click();
        mixer.Reset();
        ui.SetMix(mixer, config.maxUnitsInGlass);
        ui.FlashHint("Fresh shaker. Start again.");
    }

    public void ShakeDrink()
    {
        if (state != BarGameState.Preparing || actionLocked)
            return;

        if (mixer.TotalUnits == 0)
        {
            ui.FlashHint("Add ingredients before shaking.");
            return;
        }

        if (mixer.WasShaken)
        {
            ui.FlashHint("Already shaken.");
            return;
        }

        mixer.Shake();
        StartCoroutine(ShakeSequence());
    }

    private IEnumerator ShakeSequence()
    {
        actionLocked = true;
        audioPlayer.Shake();
        yield return ui.AnimateShake();
        ui.SetMix(mixer, config.maxUnitsInGlass);
        actionLocked = false;
    }

    public void ServeDrink()
    {
        if (state != BarGameState.Preparing || actionLocked)
            return;

        StartCoroutine(ServeSequence());
    }

    private IEnumerator ServeSequence()
    {
        actionLocked = true;
        float patienceRatio = Mathf.Clamp01(patienceRemaining / config.customerPatienceSeconds);
        DrinkResult result = DrinkEvaluator.Evaluate(currentRecipe, mixer, config, patienceRatio);
        yield return ui.AnimateServe();
        FinishCustomer(result);
        actionLocked = false;
    }

    public void ContinueAfterResult()
    {
        if (state != BarGameState.ShowingResult)
            return;

        audioPlayer.Click();
        BeginNextCustomer();
    }

    private void BeginNextCustomer()
    {
        if (customersServed >= config.customersPerShift)
        {
            FinishShift();
            return;
        }

        actionLocked = false;
        state = BarGameState.Preparing;
        currentCustomer = customers[customersServed % customers.Count];
        currentRecipe = PickRecipe();
        patienceRemaining = config.customerPatienceSeconds;
        mixer.Reset();

        ui.ShowGame();
        ui.SetHud(totalEarnings, customersServed + 1, config.customersPerShift, bestEarnings);
        ui.SetCustomer(currentCustomer);
        ui.SetOrder(currentRecipe);
        ui.SetPatience(1f, patienceRemaining);
        ui.SetMix(mixer, config.maxUnitsInGlass);
        ui.FlashHint("Pick bottles from the shelf, then shake if the ticket asks for it.");
    }

    private DrinkRecipe PickRecipe()
    {
        if (recipes.Length == 1)
            return recipes[0];

        int index = Random.Range(0, recipes.Length);
        int guard = 0;
        while (index == previousRecipeIndex && guard < 10)
        {
            index = Random.Range(0, recipes.Length);
            guard++;
        }

        previousRecipeIndex = index;
        return recipes[index];
    }

    private void CustomerTimedOut()
    {
        actionLocked = false;
        DrinkResult result = new DrinkResult
        {
            accuracy = 0f,
            tier = ReactionTier.Terrible,
            techniqueCorrect = false,
            payment = 0,
            tip = 0,
            totalEarned = 0,
            reaction = "I can't wait all night.",
            detail = "The customer left because the patience timer reached zero."
        };

        FinishCustomer(result);
    }

    private void FinishCustomer(DrinkResult result)
    {
        state = BarGameState.ShowingResult;
        customersServed++;
        totalEarnings += result.totalEarned;

        switch (result.tier)
        {
            case ReactionTier.Perfect: perfectCount++; break;
            case ReactionTier.Good: goodCount++; break;
            case ReactionTier.Bad: badCount++; break;
            default: terribleCount++; break;
        }

        audioPlayer.Result(result.tier);
        if (result.totalEarned > 0) audioPlayer.Coin();

        ui.SetHud(totalEarnings, Mathf.Min(customersServed + 1, config.customersPerShift), config.customersPerShift, bestEarnings);
        ui.ShowResult(currentCustomer, currentRecipe, result, customersServed >= config.customersPerShift);
    }

    private void FinishShift()
    {
        actionLocked = false;
        state = BarGameState.ShiftComplete;
        bool newBest = totalEarnings > bestEarnings;
        if (newBest)
        {
            bestEarnings = totalEarnings;
            PlayerPrefs.SetInt(BestShiftKey, bestEarnings);
            PlayerPrefs.Save();
        }

        ui.ShowShiftComplete(totalEarnings, bestEarnings, perfectCount, goodCount, badCount, terribleCount, newBest);
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) AddIngredient(IngredientType.Lime);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AddIngredient(IngredientType.Mint);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AddIngredient(IngredientType.Lemon);
        if (Input.GetKeyDown(KeyCode.Alpha4)) AddIngredient(IngredientType.Orange);
        if (Input.GetKeyDown(KeyCode.Alpha5)) AddIngredient(IngredientType.Pineapple);
        if (Input.GetKeyDown(KeyCode.Alpha6)) AddIngredient(IngredientType.Strawberry);
        if (Input.GetKeyDown(KeyCode.Alpha7)) AddIngredient(IngredientType.Soda);
        if (Input.GetKeyDown(KeyCode.R)) ResetGlass();
        if (Input.GetKeyDown(KeyCode.S)) ShakeDrink();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ServeDrink();
    }

    private static DrinkRecipe[] BuildRuntimeRecipes()
    {
        return new[]
        {
            MakeRecipe("Mint Spark", new[] { A(IngredientType.Lime, 2), A(IngredientType.Mint, 2), A(IngredientType.Soda, 3) }, false, 14, "Fresh and fizzy.", new Color(0.43f, 0.84f, 0.58f)),
            MakeRecipe("Sunset Fizz", new[] { A(IngredientType.Orange, 2), A(IngredientType.Strawberry, 2), A(IngredientType.Soda, 2) }, true, 16, "Sweet citrus with berry.", new Color(0.96f, 0.45f, 0.30f)),
            MakeRecipe("Island Cooler", new[] { A(IngredientType.Pineapple, 3), A(IngredientType.Lime, 1), A(IngredientType.Soda, 2) }, true, 17, "Tropical and sharp.", new Color(0.95f, 0.72f, 0.23f)),
            MakeRecipe("Pink Lemon", new[] { A(IngredientType.Lemon, 2), A(IngredientType.Strawberry, 2), A(IngredientType.Soda, 2) }, false, 15, "A bright pink lemonade.", new Color(0.94f, 0.49f, 0.57f)),
            MakeRecipe("Citrus Stack", new[] { A(IngredientType.Lime, 1), A(IngredientType.Lemon, 2), A(IngredientType.Orange, 2), A(IngredientType.Soda, 1) }, true, 18, "Three citrus layers.", new Color(0.91f, 0.66f, 0.24f)),
            MakeRecipe("Green Garden", new[] { A(IngredientType.Mint, 3), A(IngredientType.Lime, 2), A(IngredientType.Pineapple, 1), A(IngredientType.Soda, 1) }, false, 17, "Herbal, green and light.", new Color(0.35f, 0.76f, 0.45f))
        };
    }

    private static IngredientAmount A(IngredientType type, int amount)
    {
        return new IngredientAmount(type, amount);
    }

    private static DrinkRecipe MakeRecipe(string recipeName, IngredientAmount[] amounts, bool shake, int price, string description, Color color)
    {
        DrinkRecipe recipe = ScriptableObject.CreateInstance<DrinkRecipe>();
        recipe.name = recipeName;
        recipe.ConfigureRuntime(recipeName, amounts, shake, price, description, color);
        return recipe;
    }
}