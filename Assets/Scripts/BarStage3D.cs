using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BarStage3D : MonoBehaviour
{
    private readonly Color woodDark = new Color(0.20f, 0.075f, 0.035f);
    private readonly Color wood = new Color(0.40f, 0.16f, 0.065f);
    private readonly Color woodLight = new Color(0.61f, 0.29f, 0.10f);
    private readonly Color wall = new Color(0.10f, 0.055f, 0.045f);
    private readonly Color metal = new Color(0.63f, 0.66f, 0.68f);
    private readonly Color cream = new Color(0.96f, 0.86f, 0.68f);

    private BarGameController controller;
    private GameObject root;
    private Camera sceneCamera;

    private GameObject characterPrefab;
    private GameObject bottlePrefab;
    private GameObject glassPrefab;
    private Texture2D maleSkin;
    private Texture2D femaleSkin;
    private AnimationClip idleClip;
    private AnimationClip jumpClip;

    private CharacterActor bartender;
    private CharacterActor customer;
    private readonly Dictionary<IngredientType, BottleClickable> bottles = new Dictionary<IngredientType, BottleClickable>();
    private readonly Dictionary<IngredientType, Transform> bottleVisuals = new Dictionary<IngredientType, Transform>();

    private Transform shaker;
    private Transform glass;
    private Transform glassLiquid;
    private Transform pourStream;
    private SimpleObjectPool pourDropPool;
    private Vector3 shakerBasePosition;
    private Quaternion shakerBaseRotation;
    private Vector3 glassBasePosition;
    private Quaternion glassBaseRotation;

    private bool interactive;
    private bool actionBusy;
    private BottleClickable hoveredBottle;

    private class CharacterActor
    {
        public Transform root;
        public Transform model;
        public Transform sampleTarget;
        public Transform rightHand;
        public Transform leftHand;
        public Vector3 basePosition;
        public Quaternion baseRotation;
        public float idleTime;
        public bool manualAnimation;
        public Material bodyMaterial;
    }

    public void Initialize(BarGameController gameController)
    {
        controller = gameController;
        LoadReadyAssets();
        BuildStage();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
        if (!visible) ClearHover();
    }

    public void SetGameplayInteractive(bool value)
    {
        interactive = value;
        if (!value) ClearHover();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;
        UpdateCharacterAnimation(bartender);
        UpdateCharacterAnimation(customer);
        HandleBottleHoverAndClick();
    }

    private void LoadReadyAssets()
    {
        characterPrefab = Resources.Load<GameObject>("Models/Characters/characterMedium");
        bottlePrefab = Resources.Load<GameObject>("Models/Bar/soda-bottle");
        glassPrefab = Resources.Load<GameObject>("Models/Bar/glass");
        maleSkin = Resources.Load<Texture2D>("Models/Characters/Skins/skaterMaleA");
        femaleSkin = Resources.Load<Texture2D>("Models/Characters/Skins/skaterFemaleA");
        idleClip = LoadAnimationByKeyword("Models/Characters/Animations/idle", "idle");
        jumpClip = LoadAnimationByKeyword("Models/Characters/Animations/jump", "jump");
    }

    private static AnimationClip LoadAnimationByKeyword(string resourcePath, string keyword)
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath);
        if (clips == null || clips.Length == 0) return null;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) continue;
            string n = clip.name ?? string.Empty;
            if (n.IndexOf("Targeting", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (n.IndexOf("Pose", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (n.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0) return clip;
        }
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) continue;
            string n = clip.name ?? string.Empty;
            if (n.IndexOf("Targeting", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (n.IndexOf("Pose", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            return clip;
        }
        return clips[0];
    }

    public void SetCustomer(CustomerData data)
    {
        if (customer == null) return;
        customer.root.localPosition = customer.basePosition;
        customer.root.localRotation = customer.baseRotation;
        customer.idleTime = 0f;
        if (customer.bodyMaterial != null)
            customer.bodyMaterial.color = Color.Lerp(Color.white, data.color, 0.16f);
        ResetDrinkObjects();
    }

    public void SetDrink(DrinkMixer mixer, int maxUnits)
    {
        if (glassLiquid == null) return;
        float ratio = maxUnits > 0 ? Mathf.Clamp01((float)mixer.TotalUnits / maxUnits) : 0f;
        glassLiquid.gameObject.SetActive(mixer.TotalUnits > 0);
        if (mixer.TotalUnits <= 0) return;

        Vector3 s = glassLiquid.localScale;
        s.y = Mathf.Lerp(0.025f, 0.25f, ratio);
        glassLiquid.localScale = s;
        Vector3 p = glassLiquid.localPosition;
        p.y = -0.19f + s.y;
        glassLiquid.localPosition = p;
        SetAllRendererColors(glassLiquid.gameObject, MixColor(mixer));
    }

    public IEnumerator AnimatePour(IngredientType ingredient)
    {
        if (!bottleVisuals.TryGetValue(ingredient, out Transform shelfVisual))
        {
            yield return new WaitForSeconds(0.25f);
            yield break;
        }

        actionBusy = true;
        ClearHover();

        GameObject animated = Instantiate(shelfVisual.gameObject, root.transform);
        animated.name = "Pour Bottle - " + IngredientInfo.DisplayName(ingredient);
        RemoveColliders(animated);
        Transform t = animated.transform;
        t.position = shelfVisual.position;
        t.rotation = shelfVisual.rotation;
        t.localScale = shelfVisual.lossyScale;

        Vector3 start = t.position;
        Quaternion startRot = t.rotation;
        Vector3 hand = GetHandPosition(bartender, true, new Vector3(0.45f, 1.35f, -0.35f));
        Vector3 pour = glass.position + new Vector3(0.00f, 0.55f, -0.02f);

        yield return LeanActor(bartender, new Vector3(0.10f, 0.02f, 0f), Quaternion.Euler(0f, -8f, -3f), 0.16f);
        yield return MoveArc(t, start, hand, startRot, Quaternion.Euler(-90f, 0f, -8f), 0.26f, 0.16f);
        yield return MoveArc(t, hand, pour, t.rotation, Quaternion.Euler(-90f, 0f, -64f), 0.20f, 0.08f);

        if (pourStream != null)
        {
            SetAllRendererColors(pourStream.gameObject, IngredientInfo.Color(ingredient));
            pourStream.position = glass.position + new Vector3(0.00f, 0.28f, -0.02f);
            pourStream.gameObject.SetActive(true);
        }
        if (pourDropPool != null) StartCoroutine(PlayPooledPourDrops(ingredient));
        yield return new WaitForSeconds(0.32f);
        if (pourStream != null) pourStream.gameObject.SetActive(false);

        yield return MoveArc(t, pour, hand, t.rotation, Quaternion.Euler(-90f, 0f, -8f), 0.16f, 0.05f);
        yield return MoveArc(t, hand, start, t.rotation, startRot, 0.25f, 0.12f);
        yield return ResetActorPose(bartender, 0.14f);

        Destroy(animated);
        actionBusy = false;
    }

    public IEnumerator AnimateShake()
    {
        actionBusy = true;
        ClearHover();

        Vector3 counter = glass.position;
        Quaternion counterRot = glass.rotation;
        Vector3 hands = bartender.root.position + new Vector3(0.05f, 1.48f, -0.52f);

        yield return LeanActor(bartender, new Vector3(0.04f, 0.02f, -0.03f), Quaternion.Euler(-4f, 0f, 0f), 0.17f);
        yield return MoveArc(glass, counter, hands, counterRot, Quaternion.Euler(0f, 0f, 8f), 0.22f, 0.12f);

        float duration = 1.05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float wave = Mathf.Sin(elapsed * 30f);
            float wave2 = Mathf.Sin(elapsed * 19f);
            glass.position = hands + new Vector3(wave * 0.20f, wave2 * 0.07f, 0f);
            glass.rotation = Quaternion.Euler(0f, wave2 * 4f, 8f + wave * 20f);
            bartender.root.localRotation = bartender.baseRotation * Quaternion.Euler(0f, 0f, -wave * 1.5f);
            yield return null;
        }

        yield return MoveArc(glass, glass.position, counter, glass.rotation, counterRot, 0.22f, 0.08f);
        yield return ResetActorPose(bartender, 0.14f);
        glass.position = glassBasePosition;
        glass.rotation = glassBaseRotation;
        actionBusy = false;
    }

    public IEnumerator AnimateServe()
    {
        actionBusy = true;
        ClearHover();

        Vector3 start = glass.position;
        Quaternion startRot = glass.rotation;
        Vector3 hand = GetHandPosition(bartender, true, bartender.root.position + new Vector3(0.48f, 1.25f, -0.45f));
        Vector3 destination = customer.root.position + new Vector3(-0.32f, 1.15f, -0.46f);

        yield return LeanActor(bartender, new Vector3(0.08f, 0f, -0.02f), Quaternion.Euler(0f, -8f, -2f), 0.16f);
        yield return MoveArc(glass, start, hand, startRot, Quaternion.identity, 0.22f, 0.14f);
        yield return MoveArc(glass, hand, destination, glass.rotation, Quaternion.identity, 0.34f, 0.22f);
        yield return new WaitForSeconds(0.18f);
        yield return ResetActorPose(bartender, 0.14f);

        actionBusy = false;
    }

    public void PlayReaction(ReactionTier tier)
    {
        if (customer != null) StartCoroutine(ReactionRoutine(tier));
    }

    private IEnumerator ReactionRoutine(ReactionTier tier)
    {
        actionBusy = true;
        if ((tier == ReactionTier.Perfect || tier == ReactionTier.Good) && jumpClip != null)
        {
            customer.manualAnimation = true;
            float playLength = tier == ReactionTier.Perfect ? Mathf.Min(1.0f, jumpClip.length) : Mathf.Min(0.60f, jumpClip.length);
            float elapsed = 0f;
            while (elapsed < playLength)
            {
                elapsed += Time.deltaTime;
                jumpClip.SampleAnimation(customer.sampleTarget.gameObject, Mathf.Min(elapsed, jumpClip.length));
                if (tier == ReactionTier.Perfect)
                    customer.root.localPosition = customer.basePosition + Vector3.up * Mathf.Sin((elapsed / playLength) * Mathf.PI) * 0.08f;
                yield return null;
            }
            customer.root.localPosition = customer.basePosition;
            customer.manualAnimation = false;
            customer.idleTime = 0f;
        }
        else
        {
            Quaternion from = customer.root.localRotation;
            Quaternion to = customer.baseRotation * Quaternion.Euler(0f, tier == ReactionTier.Bad ? 9f : 16f, tier == ReactionTier.Bad ? 3f : 7f);
            float elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.deltaTime;
                customer.root.localRotation = Quaternion.Slerp(from, to, Smooth(Mathf.Clamp01(elapsed / 0.32f)));
                yield return null;
            }
            yield return new WaitForSeconds(0.32f);
            yield return ResetActorPose(customer, 0.18f);
        }
        actionBusy = false;
    }

    public void ResetDrinkObjects()
    {
        if (shaker != null)
        {
            shaker.position = shakerBasePosition;
            shaker.rotation = shakerBaseRotation;
        }
        if (glass != null)
        {
            glass.position = glassBasePosition;
            glass.rotation = glassBaseRotation;
        }
        if (glassLiquid != null) glassLiquid.gameObject.SetActive(false);
        if (pourStream != null) pourStream.gameObject.SetActive(false);
        if (bartender != null)
        {
            bartender.root.localPosition = bartender.basePosition;
            bartender.root.localRotation = bartender.baseRotation;
        }
        if (customer != null)
        {
            customer.root.localPosition = customer.basePosition;
            customer.root.localRotation = customer.baseRotation;
        }
    }

    private void BuildStage()
    {
        root = new GameObject("BarShift 3D Stage");
        root.transform.SetParent(transform, false);
        BuildCamera();
        BuildEnvironment();
        BuildCharacters();
        BuildTools();
        BuildBottles();
    }

    private void BuildCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(0f, 2.72f, -10.4f);
        cameraObject.transform.rotation = Quaternion.Euler(1.5f, 0f, 0f);
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.fieldOfView = 39f;
        sceneCamera.nearClipPlane = 0.1f;
        sceneCamera.farClipPlane = 60f;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.055f, 0.035f, 0.045f);
        cameraObject.AddComponent<AudioListener>();
    }

    private void BuildEnvironment()
    {
        // A simple, readable bar set. The gameplay objects are kept in the
        // middle of the screen so the HUD does not cover them.
        CreateBox("Back Wall", new Vector3(0f, 3.15f, 2.85f), new Vector3(12f, 6.5f, 0.25f), new Color(0.075f, 0.045f, 0.055f), root.transform);
        CreateBox("Floor", new Vector3(0f, -0.05f, 0.95f), new Vector3(12f, 0.15f, 7f), new Color(0.105f, 0.065f, 0.055f), root.transform);

        // Counter
        CreateBox("Counter Body", new Vector3(0f, 0.53f, -0.12f), new Vector3(10.8f, 1.00f, 1.20f), woodDark, root.transform);
        CreateBox("Counter Top", new Vector3(0f, 1.10f, -0.28f), new Vector3(11.0f, 0.17f, 1.52f), woodLight, root.transform);
        CreateBox("Counter Front Strip", new Vector3(0f, 0.82f, -0.94f), new Vector3(10.5f, 0.07f, 0.08f), new Color(0.87f, 0.39f, 0.08f), root.transform);

        // One main ingredient shelf. Keeping the shelf high leaves a clean
        // silhouette around the bartender and makes every bottle clickable.
        CreateBox("Ingredient Shelf", new Vector3(-1.05f, 3.52f, 1.67f), new Vector3(7.9f, 0.13f, 0.66f), woodLight, root.transform);
        CreateBox("Ingredient Shelf Back", new Vector3(-1.05f, 3.80f, 1.98f), new Vector3(8.05f, 0.58f, 0.10f), woodDark, root.transform);

        // A little decoration on the right, without world-space text (which
        // was visually noisy and could appear mirrored depending on import).
        CreateBox("Back Sign", new Vector3(3.75f, 3.38f, 2.60f), new Vector3(1.90f, 1.25f, 0.10f), new Color(0.035f, 0.040f, 0.045f), root.transform);
        CreateBox("Sign Accent", new Vector3(3.75f, 3.38f, 2.52f), new Vector3(1.35f, 0.07f, 0.04f), new Color(0.96f, 0.42f, 0.10f), root.transform);

        BuildLamp(new Vector3(-3.7f, 4.85f, -0.05f));
        BuildLamp(new Vector3(0.15f, 4.85f, -0.05f));
        BuildLamp(new Vector3(3.70f, 4.85f, -0.05f));

        GameObject keyLight = new GameObject("Warm Key Light");
        keyLight.transform.SetParent(root.transform, false);
        keyLight.transform.position = new Vector3(-1.2f, 5.0f, -2.5f);
        Light directional = keyLight.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.color = new Color(1.0f, 0.84f, 0.72f);
        directional.intensity = 0.95f;
        directional.shadows = LightShadows.Soft;
        keyLight.transform.rotation = Quaternion.Euler(46f, -20f, 0f);

        CreatePointLight(new Vector3(-2.9f, 4.0f, -0.2f), new Color(1f, 0.48f, 0.22f), 1.7f, 4.4f);
        CreatePointLight(new Vector3(3.0f, 3.9f, 0.4f), new Color(0.38f, 0.50f, 0.85f), 1.1f, 4.0f);
    }

    private void BuildLamp(Vector3 position)
    {
        CreateCylinder("Lamp Stem", position + Vector3.up * 0.45f, new Vector3(0.035f, 0.48f, 0.035f), new Color(0.08f, 0.07f, 0.065f), root.transform);
        Transform shade = CreateCylinder("Lamp Shade", position, new Vector3(0.40f, 0.20f, 0.40f), new Color(0.07f, 0.06f, 0.055f), root.transform);
        shade.localScale = new Vector3(0.48f, 0.15f, 0.48f);
        CreateSphere("Lamp Glow", position + Vector3.down * 0.16f, new Vector3(0.22f, 0.15f, 0.22f), new Color(1f, 0.68f, 0.25f), root.transform);
    }

    private void BuildCharacters()
    {
        bartender = CreateReadyCharacter("Bartender", new Vector3(-0.85f, 0.44f, 0.95f), Quaternion.Euler(0f, 180f, 0f), maleSkin, 1.02f);
        customer = CreateReadyCharacter("Customer", new Vector3(3.50f, 0.44f, 0.92f), Quaternion.Euler(0f, 188f, 0f), femaleSkin, 0.98f);
    }

    private CharacterActor CreateReadyCharacter(string name, Vector3 position, Quaternion rotation, Texture2D skinTexture, float scale)
    {
        CharacterActor actor = new CharacterActor();
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(root.transform, false);
        holder.transform.position = position;
        holder.transform.rotation = rotation;
        actor.root = holder.transform;
        actor.basePosition = holder.transform.localPosition;
        actor.baseRotation = holder.transform.localRotation;

        if (characterPrefab == null)
        {
            Transform fallback = CreateCapsule(name + " Fallback", new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 0.80f, 0.35f), new Color(0.18f, 0.22f, 0.28f), holder.transform, true);
            actor.model = fallback;
            actor.sampleTarget = fallback;
            actor.rightHand = NewAnchor("Right Hand Anchor", new Vector3(-0.45f, 1.25f, -0.25f), holder.transform);
            actor.leftHand = NewAnchor("Left Hand Anchor", new Vector3(0.45f, 1.25f, -0.25f), holder.transform);
            return actor;
        }

        GameObject model = Instantiate(characterPrefab, holder.transform);
        model.name = name + " Ready Character";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * scale;
        actor.model = model.transform;

        foreach (AudioListener listener in model.GetComponentsInChildren<AudioListener>(true)) Destroy(listener);
        foreach (Collider col in model.GetComponentsInChildren<Collider>(true)) Destroy(col);

        Material material = MakeTexturedMaterial(skinTexture);
        actor.bodyMaterial = material;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] mats = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int m = 0; m < mats.Length; m++) mats[m] = material;
            renderer.materials = mats;
        }

        Transform rigRoot = model.transform.Find("Root");
        actor.sampleTarget = rigRoot != null ? rigRoot : model.transform;
        EnsureSampleAnimator(actor.sampleTarget.gameObject);
        actor.rightHand = FindNamedBone(model.transform, true);
        actor.leftHand = FindNamedBone(model.transform, false);
        if (actor.rightHand == null) actor.rightHand = NewAnchor("Right Hand Anchor", new Vector3(-0.43f, 1.22f, -0.22f), holder.transform);
        if (actor.leftHand == null) actor.leftHand = NewAnchor("Left Hand Anchor", new Vector3(0.43f, 1.22f, -0.22f), holder.transform);

        if (idleClip != null) idleClip.SampleAnimation(actor.sampleTarget.gameObject, 0f);
        return actor;
    }

    private static void EnsureSampleAnimator(GameObject target)
    {
        Animator[] animators = target.transform.root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].runtimeAnimatorController = null;
            animators[i].applyRootMotion = false;
            animators[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        Animator onTarget = target.GetComponent<Animator>();
        if (onTarget == null) onTarget = target.AddComponent<Animator>();
        onTarget.runtimeAnimatorController = null;
        onTarget.applyRootMotion = false;
        onTarget.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void UpdateCharacterAnimation(CharacterActor actor)
    {
        if (actor == null || actor.manualAnimation || idleClip == null || actor.sampleTarget == null) return;
        actor.idleTime += Time.deltaTime;
        float len = Mathf.Max(0.01f, idleClip.length);
        float t = actor.idleTime % len;
        idleClip.SampleAnimation(actor.sampleTarget.gameObject, t);
    }

    private Transform FindNamedBone(Transform rootTransform, bool right)
    {
        Transform[] all = rootTransform.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name.ToLowerInvariant();
            if (!n.Contains("hand")) continue;
            bool side = right
                ? n.Contains("right") || n.Contains("hand.r") || n.Contains("hand_r") || n.Contains("r_hand")
                : n.Contains("left") || n.Contains("hand.l") || n.Contains("hand_l") || n.Contains("l_hand");
            if (side) return all[i];
        }
        return null;
    }

    private void BuildTools()
    {
        // One main mixing cup on the counter. It is used for pouring, shaking
        // and serving so the interaction stays visually clear.
        glass = new GameObject("Mixing Cup").transform;
        glass.SetParent(root.transform, false);
        glass.position = new Vector3(1.00f, 1.38f, -0.55f);

        if (glassPrefab != null)
        {
            GameObject readyGlass = Instantiate(glassPrefab, glass);
            readyGlass.name = "Ready Glass Mesh";
            readyGlass.transform.localPosition = Vector3.zero;
            readyGlass.transform.localRotation = Quaternion.identity;
            readyGlass.transform.localScale = Vector3.one * 1.35f;
            RemoveColliders(readyGlass);
            SetAllMaterials(readyGlass, MakeMaterial(new Color(0.74f, 0.86f, 0.92f), 0.03f, 0.70f));
        }
        else
        {
            CreateCylinder("Glass Outer", Vector3.zero, new Vector3(0.25f, 0.45f, 0.25f), new Color(0.45f, 0.57f, 0.62f), glass);
        }

        glassLiquid = CreateCylinder("Drink Liquid", new Vector3(0f, -0.14f, 0f), new Vector3(0.18f, 0.05f, 0.18f), new Color(0.95f, 0.55f, 0.15f), glass);
        glassLiquid.gameObject.SetActive(false);

        // For the animation code, the mixing cup also acts as the shaker.
        shaker = glass;
        shakerBasePosition = glass.position;
        shakerBaseRotation = glass.rotation;
        glassBasePosition = glass.position;
        glassBaseRotation = glass.rotation;

        pourStream = CreateCylinder("Pour Stream", new Vector3(1.00f, 2.00f, -0.55f), new Vector3(0.028f, 0.26f, 0.028f), new Color(0.95f, 0.55f, 0.15f), root.transform);
        pourStream.gameObject.SetActive(false);

        // Object-pool example from the course: small pour droplets are
        // pre-created and recycled instead of Instantiate/Destroy on every pour.
        Transform dropPrototype = CreateSphere("Pour Drop Prototype", Vector3.zero, new Vector3(0.055f, 0.055f, 0.055f), Color.white, root.transform);
        dropPrototype.gameObject.SetActive(false);
        pourDropPool = gameObject.AddComponent<SimpleObjectPool>();
        pourDropPool.Initialize(dropPrototype.gameObject, 8, root.transform);

        CreateCylinder("Jigger", new Vector3(1.72f, 1.35f, -0.55f), new Vector3(0.13f, 0.20f, 0.13f), metal, root.transform);
    }

    private void BuildBottles()
    {
        IngredientType[] types = (IngredientType[])System.Enum.GetValues(typeof(IngredientType));
        float startX = -4.15f;
        float spacing = 0.98f;

        for (int i = 0; i < types.Length; i++)
        {
            IngredientType type = types[i];
            Vector3 pos = new Vector3(startX + spacing * i, 3.60f, 1.34f);
            GameObject holder = new GameObject("Bottle - " + IngredientInfo.DisplayName(type));
            holder.transform.SetParent(root.transform, false);
            holder.transform.position = pos;

            Transform visual = BuildBottleVisual(holder.transform, type);
            bottleVisuals[type] = visual;

            // Large forgiving hit-box: the player should not need pixel-perfect
            // mouse placement to select an ingredient.
            BoxCollider collider = holder.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.55f, -0.03f);
            collider.size = new Vector3(0.72f, 1.35f, 0.85f);

            BottleClickable clickable = holder.AddComponent<BottleClickable>();
            clickable.ingredient = type;
            clickable.visual = visual;
            bottles[type] = clickable;

            CreateText3D((i + 1) + "  " + ShortIngredientName(type),
                pos + new Vector3(0f, 1.04f, -0.48f), 0.026f, cream);
        }
    }

    private Transform BuildBottleVisual(Transform parent, IngredientType ingredient)
    {
        GameObject visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(parent, false);
        Color color = IngredientInfo.Color(ingredient);

        if (bottlePrefab != null)
        {
            GameObject model = Instantiate(bottlePrefab, visualRoot.transform);
            model.name = "Ready Bottle Mesh";
            // Kenney OBJ assets are already Y-up. The previous -90° rotation
            // made them appear like tiny horizontal blocks.
            model.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 1.82f;
            RemoveColliders(model);
            SetAllMaterials(model, MakeMaterial(color, 0.05f, 0.50f));
        }
        else
        {
            CreateCylinder("Body", new Vector3(0f, 0.35f, 0f), new Vector3(0.20f, 0.36f, 0.20f), color, visualRoot.transform);
            CreateCylinder("Shoulder", new Vector3(0f, 0.72f, 0f), new Vector3(0.15f, 0.12f, 0.15f), color * 0.92f, visualRoot.transform);
            CreateCylinder("Neck", new Vector3(0f, 0.90f, 0f), new Vector3(0.085f, 0.18f, 0.085f), color * 0.85f, visualRoot.transform);
            CreateCylinder("Cap", new Vector3(0f, 1.10f, 0f), new Vector3(0.095f, 0.06f, 0.095f), new Color(0.08f, 0.07f, 0.06f), visualRoot.transform);
        }

        return visualRoot.transform;
    }

    private IEnumerator PlayPooledPourDrops(IngredientType ingredient)
    {
        Color color = IngredientInfo.Color(ingredient);
        for (int i = 0; i < 6; i++)
        {
            GameObject drop = pourDropPool.Get();
            drop.transform.position = glass.position + new Vector3(Random.Range(-0.055f, 0.055f), 0.48f, -0.02f);
            drop.transform.localScale = Vector3.one * Random.Range(0.045f, 0.070f);
            SetAllRendererColors(drop, color);
            StartCoroutine(AnimatePooledDrop(drop));
            yield return new WaitForSeconds(0.045f);
        }
    }

    private IEnumerator AnimatePooledDrop(GameObject drop)
    {
        Vector3 start = drop.transform.position;
        Vector3 end = start + Vector3.down * 0.30f;
        Vector3 startScale = drop.transform.localScale;
        float elapsed = 0f;
        const float duration = 0.20f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            drop.transform.position = Vector3.Lerp(start, end, t);
            drop.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        pourDropPool.Release(drop);
    }

    private void HandleBottleHoverAndClick()
    {
        if (!interactive || actionBusy || controller == null || sceneCamera == null)
        {
            ClearHover();
            return;
        }

        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        BottleClickable target = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            target = hit.collider.GetComponentInParent<BottleClickable>();

        if (target != hoveredBottle)
        {
            if (hoveredBottle != null) hoveredBottle.SetHovered(false);
            hoveredBottle = target;
            if (hoveredBottle != null) hoveredBottle.SetHovered(true);
        }

        if (hoveredBottle != null && Input.GetMouseButtonDown(0))
            controller.AddIngredient(hoveredBottle.ingredient);
    }

    private Vector3 GetHandPosition(CharacterActor actor, bool right, Vector3 fallbackWorld)
    {
        if (actor == null) return fallbackWorld;
        Transform hand = right ? actor.rightHand : actor.leftHand;
        if (hand != null && hand.parent != actor.root) return hand.position;
        return fallbackWorld;
    }

    private IEnumerator LeanActor(CharacterActor actor, Vector3 localOffset, Quaternion extraRotation, float duration)
    {
        if (actor == null) yield break;
        Vector3 fromPos = actor.root.localPosition;
        Quaternion fromRot = actor.root.localRotation;
        Vector3 toPos = actor.basePosition + localOffset;
        Quaternion toRot = actor.baseRotation * extraRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            actor.root.localPosition = Vector3.Lerp(fromPos, toPos, t);
            actor.root.localRotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
    }

    private IEnumerator ResetActorPose(CharacterActor actor, float duration)
    {
        if (actor == null) yield break;
        Vector3 fromPos = actor.root.localPosition;
        Quaternion fromRot = actor.root.localRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            actor.root.localPosition = Vector3.Lerp(fromPos, actor.basePosition, t);
            actor.root.localRotation = Quaternion.Slerp(fromRot, actor.baseRotation, t);
            yield return null;
        }
        actor.root.localPosition = actor.basePosition;
        actor.root.localRotation = actor.baseRotation;
    }

    private IEnumerator MoveArc(Transform target, Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float duration, float arc)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            target.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arc;
            target.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
        target.position = to;
        target.rotation = toRot;
    }

    private Transform NewAnchor(string name, Vector3 localPosition, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private Transform CreateBox(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        return CreatePrimitive(PrimitiveType.Cube, name, position, scale, color, parent, false);
    }

    private Transform CreateSphere(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        return CreatePrimitive(PrimitiveType.Sphere, name, position, scale, color, parent, false);
    }

    private Transform CreateCylinder(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        return CreatePrimitive(PrimitiveType.Cylinder, name, position, scale, color, parent, false);
    }

    private Transform CreateCapsule(string name, Vector3 position, Vector3 scale, Color color, Transform parent, bool localPosition)
    {
        return CreatePrimitive(PrimitiveType.Capsule, name, position, scale, color, parent, localPosition);
    }

    private Transform CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, Transform parent, bool localPosition)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        if (localPosition) go.transform.localPosition = position;
        else if (parent == root.transform) go.transform.position = position;
        else go.transform.localPosition = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.material = MakeMaterial(color, type == PrimitiveType.Cylinder ? 0.05f : 0f, type == PrimitiveType.Sphere ? 0.35f : 0.22f);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        return go.transform;
    }

    private Material MakeMaterial(Color color, float metallic = 0f, float smoothness = 0.28f)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    private Material MakeTexturedMaterial(Texture2D texture)
    {
        Material mat = MakeMaterial(Color.white, 0f, 0.24f);
        if (texture != null)
        {
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            mat.mainTexture = texture;
        }
        return mat;
    }

    private void CreateText3D(string text, Vector3 position, float characterSize, Color color)
    {
        GameObject go = new GameObject("Text - " + text);
        go.transform.SetParent(root.transform, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.identity;
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = characterSize;
        tm.fontSize = 64;
        tm.color = color;
    }

    private void CreatePointLight(Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject("Accent Light");
        go.transform.SetParent(root.transform, false);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private void ClearHover()
    {
        if (hoveredBottle != null) hoveredBottle.SetHovered(false);
        hoveredBottle = null;
    }

    private static void SetAllMaterials(GameObject go, Material material)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) renderers[i].material = material;
    }

    private static void SetAllRendererColors(GameObject go, Color color)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = color;
    }

    private static void RemoveColliders(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static string ShortIngredientName(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Pineapple: return "PINE";
            case IngredientType.Strawberry: return "BERRY";
            default: return IngredientInfo.DisplayName(type).ToUpperInvariant();
        }
    }

    private static Color MixColor(DrinkMixer mixer)
    {
        if (mixer.TotalUnits <= 0) return Color.clear;
        Color sum = Color.black;
        int total = 0;
        foreach (IngredientType type in System.Enum.GetValues(typeof(IngredientType)))
        {
            int amount = mixer.GetAmount(type);
            if (amount <= 0) continue;
            sum += IngredientInfo.Color(type) * amount;
            total += amount;
        }
        if (total <= 0) return Color.white;
        Color result = sum / total;
        result.a = 1f;
        return result;
    }
}