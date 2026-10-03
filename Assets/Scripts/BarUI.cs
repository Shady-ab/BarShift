using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class BarUI : MonoBehaviour
{
    private readonly Color bg = new Color(0.08f, 0.055f, 0.065f);
    private readonly Color panel = new Color(0.15f, 0.11f, 0.13f);
    private readonly Color cream = new Color(0.96f, 0.91f, 0.82f);
    private readonly Color accent = new Color(0.95f, 0.57f, 0.26f);
    private readonly Color green = new Color(0.37f, 0.78f, 0.50f);
    private readonly Color red = new Color(0.89f, 0.32f, 0.34f);

    private BarGameController controller;
    private Font font;
    private GameObject menu, instructions, game, result, shift;
    private Text menuBest, earnings, customerCounter, best;
    private Text customerName, customerBio, customerInitial;
    private Image customerPortrait;
    private Text orderName, orderDetails, orderNote;
    private Slider patience;
    private Image patienceFill;
    private Text patienceText, glassContents, technique, hint;
    private Image glassFill;
    private Button shakeButton;
    private Text shakeLabel;
    private Text resultTitle, resultCustomer, resultReaction, resultAccuracy, resultMoney, resultDetail, resultContinue;
    private Text shiftTitle, shiftMoney, shiftStats, shiftBest;
    private Coroutine hintRoutine;

    public void Initialize(BarGameController c)
    {
        controller = c;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Build();
    }

    public void ShowMainMenu(int value)
    {
        SetScreen(menu);
        result.SetActive(false);
        shift.SetActive(false);
        menuBest.text = value > 0 ? "BEST SHIFT   $" + value : "BEST SHIFT   --";
    }

    public void ShowInstructions()
    {
        SetScreen(instructions);
        result.SetActive(false);
        shift.SetActive(false);
    }

    public void ShowGame()
    {
        SetScreen(game);
        result.SetActive(false);
        shift.SetActive(false);
    }

    public void SetHud(int money, int current, int total, int bestValue)
    {
        earnings.text = "EARNINGS   $" + money;
        customerCounter.text = "CUSTOMER   " + current + "/" + total;
        best.text = "BEST   $" + bestValue;
    }

    public void SetCustomer(CustomerData c)
    {
        customerName.text = c.name;
        customerBio.text = c.shortBio;
        customerInitial.text = c.initial;
        customerPortrait.color = c.color;
    }

    public void SetOrder(DrinkRecipe recipe)
    {
        orderName.text = recipe.DisplayName.ToUpperInvariant();
        orderDetails.text = recipe.BuildTicketText();
        orderNote.text = "$" + recipe.BasePrice + " base price\n" + recipe.Description;
    }

    public void SetPatience(float ratio, float seconds)
    {
        ratio = Mathf.Clamp01(ratio);
        patience.value = ratio;
        patienceFill.color = ratio > 0.45f ? green : ratio > 0.2f ? accent : red;
        patienceText.text = "PATIENCE   " + Mathf.CeilToInt(Mathf.Max(0, seconds)) + "s";
    }

    public void SetMix(DrinkMixer mixer, int maxUnits)
    {
        glassContents.text = mixer.BuildContentsText();
        technique.text = mixer.WasShaken ? "Technique: SHAKEN" : "Technique: not shaken";
        technique.color = mixer.WasShaken ? green : cream;

        float amount = maxUnits > 0 ? Mathf.Clamp01((float)mixer.TotalUnits / maxUnits) : 0f;
        RectTransform r = glassFill.rectTransform;
        r.anchorMin = new Vector2(0.08f, 0.06f);
        r.anchorMax = new Vector2(0.92f, Mathf.Lerp(0.08f, 0.92f, amount));
        r.offsetMin = r.offsetMax = Vector2.zero;
        glassFill.color = MixColor(mixer);

        shakeButton.interactable = mixer.TotalUnits > 0 && !mixer.WasShaken;
        shakeLabel.text = mixer.WasShaken ? "SHAKEN ✓" : "SHAKE [S]";
    }

    public void ShowResult(CustomerData customer, DrinkRecipe recipe, DrinkResult r, bool last)
    {
        result.SetActive(true);

        switch (r.tier)
        {
            case ReactionTier.Perfect: resultTitle.text = "PERFECT"; resultTitle.color = green; break;
            case ReactionTier.Good: resultTitle.text = "GOOD"; resultTitle.color = green; break;
            case ReactionTier.Bad: resultTitle.text = "NOT QUITE"; resultTitle.color = accent; break;
            default: resultTitle.text = "ROUGH ONE"; resultTitle.color = red; break;
        }

        resultCustomer.text = customer.name + " tastes the " + recipe.DisplayName + "...";
        resultReaction.text = "“" + r.reaction + "”";
        resultAccuracy.text = "Accuracy   " + Mathf.RoundToInt(r.accuracy * 100f) + "%";
        resultMoney.text = "Drink $" + r.payment + "   Tip $" + r.tip + "   Total $" + r.totalEarned;
        resultDetail.text = r.detail;
        resultContinue.text = last ? "SEE SHIFT RESULTS" : "NEXT CUSTOMER";
    }

    public void ShowShiftComplete(int money, int bestValue, int perfect, int good, int bad, int terrible, bool newBest)
    {
        result.SetActive(false);
        shift.SetActive(true);
        shiftTitle.text = newBest ? "NEW BEST SHIFT!" : "SHIFT COMPLETE";
        shiftTitle.color = newBest ? accent : cream;
        shiftMoney.text = "YOU EARNED   $" + money;
        shiftStats.text = "Perfect " + perfect + "    Good " + good + "\nNot quite " + bad + "    Rough " + terrible;
        shiftBest.text = "BEST SHIFT   $" + bestValue;
    }

    public void FlashHint(string message)
    {
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(Hint(message));
    }

    private IEnumerator Hint(string message)
    {
        hint.text = message;
        hint.color = cream;
        yield return new WaitForSeconds(2f);
        hint.color = new Color(cream.r, cream.g, cream.b, 0.5f);
    }

    private void Build()
    {
        GameObject canvasGo = new GameObject("BarShift Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = ImageBox(canvasGo.transform, "Background", bg);
        Stretch(background.rectTransform);

        menu = BuildMenu(canvasGo.transform);
        instructions = BuildInstructions(canvasGo.transform);
        game = BuildGame(canvasGo.transform);
        result = BuildResult(canvasGo.transform);
        shift = BuildShift(canvasGo.transform);
    }

    private GameObject BuildMenu(Transform parent)
    {
        GameObject root = Panel(parent, "Main Menu", Color.clear);
        Stretch(root.GetComponent<RectTransform>());
        TextAt(root.transform, "BARSHIFT", 82, new Vector2(0.5f, 0.68f), new Vector2(850, 110), cream, FontStyle.Bold);
        TextAt(root.transform, "MIX • SERVE • GET PAID", 24, new Vector2(0.5f, 0.58f), new Vector2(650, 50), cream);
        ButtonAt(root.transform, "START SHIFT", new Vector2(0.5f, 0.44f), new Vector2(360, 72), accent, controller.StartShift);
        ButtonAt(root.transform, "HOW TO PLAY", new Vector2(0.5f, 0.35f), new Vector2(360, 62), panel, controller.ShowInstructions);
        ButtonAt(root.transform, "QUIT", new Vector2(0.5f, 0.27f), new Vector2(360, 58), panel, controller.QuitGame);
        menuBest = TextAt(root.transform, "BEST SHIFT   --", 21, new Vector2(0.5f, 0.16f), new Vector2(500, 45), cream, FontStyle.Bold);
        return root;
    }

    private GameObject BuildInstructions(Transform parent)
    {
        GameObject root = Panel(parent, "Instructions", Color.clear);
        Stretch(root.GetComponent<RectTransform>());
        TextAt(root.transform, "HOW TO PLAY", 50, new Vector2(0.5f, 0.76f), new Vector2(700, 80), cream, FontStyle.Bold);
        TextAt(root.transform,
            "1. Read the customer's ticket.\n\n2. Add the exact ingredient amounts with buttons or keys 1–7.\n\n3. Shake only when the ticket says SHAKE.\n\n4. Serve with ENTER before patience runs out.\n\n5. Accuracy and speed determine payment and tip.",
            25, new Vector2(0.5f, 0.49f), new Vector2(980, 430), cream);
        ButtonAt(root.transform, "BACK", new Vector2(0.5f, 0.17f), new Vector2(300, 62), accent, controller.BackFromInstructions);
        return root;
    }

    private GameObject BuildGame(Transform parent)
    {
        GameObject root = Panel(parent, "Game Screen", Color.clear);
        Stretch(root.GetComponent<RectTransform>());

        earnings = TextAt(root.transform, "EARNINGS $0", 22, new Vector2(0.12f, 0.95f), new Vector2(320, 42), cream, FontStyle.Bold);
        customerCounter = TextAt(root.transform, "CUSTOMER 1/6", 22, new Vector2(0.5f, 0.95f), new Vector2(320, 42), cream, FontStyle.Bold);
        best = TextAt(root.transform, "BEST $0", 22, new Vector2(0.88f, 0.95f), new Vector2(320, 42), cream, FontStyle.Bold);

        GameObject left = Panel(root.transform, "Customer", panel);
        SetRect(left.GetComponent<RectTransform>(), new Vector2(0.16f, 0.55f), new Vector2(480, 650));
        customerPortrait = ImageBox(left.transform, "Portrait", accent);
        SetRect(customerPortrait.rectTransform, new Vector2(0.5f, 0.73f), new Vector2(190, 190));
        customerInitial = TextAt(left.transform, "?", 72, new Vector2(0.5f, 0.73f), new Vector2(170, 120), cream, FontStyle.Bold);
        customerName = TextAt(left.transform, "Customer", 34, new Vector2(0.5f, 0.51f), new Vector2(380, 55), cream, FontStyle.Bold);
        customerBio = TextAt(left.transform, "", 18, new Vector2(0.5f, 0.42f), new Vector2(390, 70), cream);
        patienceText = TextAt(left.transform, "PATIENCE", 18, new Vector2(0.5f, 0.25f), new Vector2(360, 38), cream, FontStyle.Bold);
        patience = SliderAt(left.transform, new Vector2(0.5f, 0.18f), new Vector2(350, 26), out patienceFill);

        GameObject center = Panel(root.transform, "Order", panel);
        SetRect(center.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(580, 650));
        orderName = TextAt(center.transform, "ORDER", 36, new Vector2(0.5f, 0.86f), new Vector2(500, 60), accent, FontStyle.Bold);
        orderDetails = TextAt(center.transform, "", 26, new Vector2(0.5f, 0.65f), new Vector2(470, 240), cream, FontStyle.Bold);
        orderNote = TextAt(center.transform, "", 18, new Vector2(0.5f, 0.45f), new Vector2(450, 85), cream);

        Image glass = ImageBox(center.transform, "Glass", new Color(1, 1, 1, 0.1f));
        SetRect(glass.rectTransform, new Vector2(0.5f, 0.23f), new Vector2(180, 220));
        glassFill = ImageBox(glass.transform, "Fill", Color.clear);
        glassContents = TextAt(center.transform, "Glass is empty", 16, new Vector2(0.5f, 0.07f), new Vector2(500, 50), cream);
        technique = TextAt(center.transform, "Technique: not shaken", 16, new Vector2(0.5f, 0.13f), new Vector2(500, 35), cream);

        GameObject right = Panel(root.transform, "Controls", panel);
        SetRect(right.GetComponent<RectTransform>(), new Vector2(0.84f, 0.55f), new Vector2(480, 650));
        TextAt(right.transform, "INGREDIENTS", 30, new Vector2(0.5f, 0.9f), new Vector2(400, 55), cream, FontStyle.Bold);

        IngredientType[] types = (IngredientType[])System.Enum.GetValues(typeof(IngredientType));
        for (int i = 0; i < types.Length; i++)
        {
            IngredientType captured = types[i];
            float y = 0.79f - i * 0.09f;
            ButtonAt(right.transform, (i + 1) + ". " + IngredientInfo.DisplayName(captured), new Vector2(0.5f, y), new Vector2(370, 52), IngredientInfo.Color(captured), () => controller.AddIngredient(captured));
        }

        ButtonAt(right.transform, "RESET [R]", new Vector2(0.30f, 0.08f), new Vector2(170, 58), panel, controller.ResetGlass);
        shakeButton = ButtonAt(right.transform, "SHAKE [S]", new Vector2(0.70f, 0.08f), new Vector2(170, 58), panel, controller.ShakeDrink);
        shakeLabel = shakeButton.GetComponentInChildren<Text>();
        ButtonAt(root.transform, "SERVE [ENTER]", new Vector2(0.5f, 0.09f), new Vector2(430, 72), accent, controller.ServeDrink);
        hint = TextAt(root.transform, "", 18, new Vector2(0.5f, 0.025f), new Vector2(900, 35), cream);

        return root;
    }

    private GameObject BuildResult(Transform parent)
    {
        GameObject overlay = Panel(parent, "Result Overlay", new Color(0.02f, 0.01f, 0.02f, 0.92f));
        Stretch(overlay.GetComponent<RectTransform>());
        GameObject card = Panel(overlay.transform, "Card", panel);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(820, 650));

        resultTitle = TextAt(card.transform, "PERFECT", 55, new Vector2(0.5f, 0.83f), new Vector2(700, 80), green, FontStyle.Bold);
        resultCustomer = TextAt(card.transform, "", 20, new Vector2(0.5f, 0.69f), new Vector2(700, 45), cream);
        resultReaction = TextAt(card.transform, "", 26, new Vector2(0.5f, 0.56f), new Vector2(700, 85), cream, FontStyle.Bold);
        resultAccuracy = TextAt(card.transform, "", 22, new Vector2(0.5f, 0.42f), new Vector2(700, 45), cream);
        resultMoney = TextAt(card.transform, "", 25, new Vector2(0.5f, 0.33f), new Vector2(700, 50), accent, FontStyle.Bold);
        resultDetail = TextAt(card.transform, "", 18, new Vector2(0.5f, 0.23f), new Vector2(680, 70), cream);
        Button b = ButtonAt(card.transform, "NEXT CUSTOMER", new Vector2(0.5f, 0.08f), new Vector2(340, 64), accent, controller.ContinueAfterResult);
        resultContinue = b.GetComponentInChildren<Text>();
        overlay.SetActive(false);
        return overlay;
    }

    private GameObject BuildShift(Transform parent)
    {
        GameObject overlay = Panel(parent, "Shift Overlay", new Color(0.02f, 0.01f, 0.02f, 0.92f));
        Stretch(overlay.GetComponent<RectTransform>());
        GameObject card = Panel(overlay.transform, "Card", panel);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(820, 650));

        shiftTitle = TextAt(card.transform, "SHIFT COMPLETE", 52, new Vector2(0.5f, 0.8f), new Vector2(700, 80), cream, FontStyle.Bold);
        shiftMoney = TextAt(card.transform, "", 36, new Vector2(0.5f, 0.62f), new Vector2(700, 60), accent, FontStyle.Bold);
        shiftStats = TextAt(card.transform, "", 23, new Vector2(0.5f, 0.45f), new Vector2(700, 100), cream);
        shiftBest = TextAt(card.transform, "", 20, new Vector2(0.5f, 0.31f), new Vector2(600, 45), cream, FontStyle.Bold);
        ButtonAt(card.transform, "PLAY AGAIN", new Vector2(0.35f, 0.12f), new Vector2(250, 62), accent, controller.RestartShift);
        ButtonAt(card.transform, "MAIN MENU", new Vector2(0.65f, 0.12f), new Vector2(250, 62), panel, controller.GoToMainMenu);
        overlay.SetActive(false);
        return overlay;
    }

    private void SetScreen(GameObject active)
    {
        menu.SetActive(active == menu);
        instructions.SetActive(active == instructions);
        game.SetActive(active == game);
    }

    private GameObject Panel(Transform parent, string name, Color color)
    {
        return ImageBox(parent, name, color).gameObject;
    }

    private Image ImageBox(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text TextAt(Transform parent, string value, int size, Vector2 anchor, Vector2 dimensions, Color color, FontStyle style = FontStyle.Normal)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        SetRect(go.GetComponent<RectTransform>(), anchor, dimensions);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private Button ButtonAt(Transform parent, string label, Vector2 anchor, Vector2 dimensions, Color color, UnityAction action)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetRect(go.GetComponent<RectTransform>(), anchor, dimensions);
        go.GetComponent<Image>().color = color;
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(action);
        Text t = TextAt(go.transform, label, 19, new Vector2(0.5f, 0.5f), dimensions - new Vector2(12, 8), cream, FontStyle.Bold);
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 12;
        t.resizeTextMaxSize = 20;
        return b;
    }

    private Slider SliderAt(Transform parent, Vector2 anchor, Vector2 dimensions, out Image fill)
    {
        GameObject root = new GameObject("Patience", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        SetRect(root.GetComponent<RectTransform>(), anchor, dimensions);
        Image back = ImageBox(root.transform, "Background", new Color(1, 1, 1, 0.12f));
        Stretch(back.rectTransform);
        fill = ImageBox(root.transform, "Fill", green);
        Stretch(fill.rectTransform);
        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.interactable = false;
        return slider;
    }

    private Color MixColor(DrinkMixer mixer)
    {
        if (mixer.TotalUnits == 0) return new Color(1, 1, 1, 0.03f);
        Color sum = Color.black;

        foreach (IngredientType type in System.Enum.GetValues(typeof(IngredientType)))
        {
            int n = mixer.GetAmount(type);
            Color c = IngredientInfo.Color(type);
            sum.r += c.r * n;
            sum.g += c.g * n;
            sum.b += c.b * n;
        }

        return new Color(sum.r / mixer.TotalUnits, sum.g / mixer.TotalUnits, sum.b / mixer.TotalUnits, 0.82f);
    }

    private static void SetRect(RectTransform r, Vector2 anchor, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = size;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
