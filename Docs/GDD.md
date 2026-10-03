# Game Design Document — BarShift: Mix & Serve

| | |
|---|---|
| **Working title** | BarShift: Mix & Serve |
| **Team** | Shadi — design and development |
| **Genre** | 2D bartending / time-management / score game |
| **Target platform** | PC (Windows), standalone build |
| **Engine / Unity version** | Unity 6 — 6000.3.20f1 |
| **Orientation & reference resolution** | Landscape, 1920 × 1080 |
| **Expected session length** | 4–7 minutes |
| **Document version** | v0.3 — 2026-10-01 |

## 1. High Concept

The player works one short bar shift serving six customers. Each customer orders a drink with exact ingredient amounts and a preparation rule. The player builds the drink, decides whether to shake it, and serves before patience runs out. The customer tastes it, reacts to its accuracy, pays, and may leave a tip.

### Design pillars

1. **Readable orders, meaningful mistakes** — every recipe is visible on the ticket, so a wrong drink comes from the player's input rather than hidden information.
2. **Fast service matters** — the patience timer creates pressure, but the player still has enough time to read and correct a drink.
3. **One polished shift** — six customers and six drinks are enough variety for replay without turning the project into restaurant management.

## 2. Reference & Inspiration

- **Primary reference:** Papa's Freezeria — https://www.flipline.com/games/papasfreezeria/index.html  
  Taking: the order → prepare → score → payment loop.  
  Not taking: multiple work stations, upgrades, long progression, inventory purchasing, or a large cast of unlockable customers.
- **Visual direction:** a dark late-night bar with warm cream and orange UI, simple customer cards and a large readable order ticket.
- **Main difference:** BarShift is much smaller and more system-focused. The recipe comparison is the main mechanic, not a long restaurant campaign.

### Gameplay layout

| Left | Center | Right |
|---|---|---|
| Customer portrait, name, patience | Order ticket, glass, current mix | Ingredient buttons |
| Earnings / best shift | Reset, Shake, Serve | Keyboard shortcuts |

## 3. Core Game Loop

~~~mermaid
stateDiagram-v2
    [*] --> MainMenu
    MainMenu --> Preparing: Start Shift
    Preparing --> Result: Serve drink
    Preparing --> Result: Patience reaches 0
    Result --> Preparing: Next customer
    Result --> ShiftComplete: Sixth customer finished
    ShiftComplete --> Preparing: Play Again
    ShiftComplete --> MainMenu: Main Menu
~~~

### Moment-to-moment rules

- A customer arrives with one randomly selected drink order.
- The ticket shows exact ingredient amounts and whether the drink should be shaken.
- Clicking an ingredient adds exactly one unit. Keyboard keys 1–7 do the same.
- The glass holds a maximum of 12 units. Reset clears it before serving.
- Shake changes the preparation state once; shaking twice has no extra effect.
- Missing units and extra units both reduce accuracy.
- The wrong shake/no-shake technique multiplies the ingredient score by a penalty.
- Good drinks receive the full base price. Better drinks also earn tips, with a fast-service bonus while enough patience remains.
- If patience reaches zero, that customer leaves and pays $0. The shift continues.
- After six customers, the summary shows earnings, reaction counts and the saved best shift.

### Parameters to tune

| Parameter | What it controls | First value |
|---|---|---:|
| customersPerShift | Number of orders in one run | 6 |
| customerPatienceSeconds | Time available per customer | 42 s |
| perfectThreshold | Accuracy needed for Perfect | 0.93 |
| goodThreshold | Accuracy needed for Good | 0.72 |
| badThreshold | Accuracy needed to avoid Terrible | 0.45 |
| wrongTechniqueMultiplier | Penalty for shake/no-shake mistake | 0.82 |
| perfectTip | Base tip for Perfect | $6 |
| goodTip | Base tip for Good | $3 |
| fastBonusTip | Extra tip for fast service | $2 |
| fastBonusThreshold | Patience ratio for speed bonus | 0.55 |
| maxUnitsInGlass | Maximum ingredient units | 12 |

**Where these live:** Assets/Resources/Data/BarGameConfig.asset, using the BarGameConfig ScriptableObject.

**Feel target:** after one customer, a first-time player should understand the full loop without opening the instructions again. A careful player should be able to achieve at least four Good/Perfect drinks in one six-customer shift.

## 4. Controls & Input

| Action | Mouse | Keyboard |
|---|---|---|
| Add ingredient | Click ingredient button | 1–7 |
| Reset glass | Reset button | R |
| Shake | Shake button | S |
| Serve | Serve button | Enter |
| Menu navigation | UI buttons | Mouse |

- Gameplay input is accepted only in the Preparing state.
- While a result or shift summary is open, ingredient shortcuts do nothing.
- Serving a poor mix is allowed because the result feedback is part of the loop.
- Shake is disabled when the glass is empty or already shaken.

## 5. Screens & UI

1. **Main Menu** — title, Start Shift, How To Play, Quit and saved best shift.
2. **How To Play** — explanation of the ticket, ingredients, shake rule, patience and shortcuts.
3. **Gameplay** — customer panel, patience bar, order ticket, glass, ingredient controls and earnings HUD.
4. **Customer Result** — reaction tier, spoken reaction, accuracy, payment, tip and short explanation.
5. **Shift Complete** — total earnings, reaction counts, best shift, Play Again and Main Menu.

**HUD during play:** earnings, customer number, best shift and current patience. There is deliberately no minimap, inventory, XP bar or unrelated information.

**Canvas setup:** Screen Space — Overlay, CanvasScaler set to Scale With Screen Size, 1920 × 1080 reference, match = 0.5.

## 6. Art & Audio

| Asset | Variants / frames | Source & licence | Use |
|---|---|---|---|
| UI panels / buttons | Runtime colours and shapes | Original project code | All screens |
| Customer portraits | 6 colour cards + initials | Original project code | Customer panel |
| Drink glass / fill | Runtime UI shapes | Original project code | Mixing feedback |
| Ingredient colours | 7 colour identities | Original project code | Buttons and mixed drink colour |
| SFX | Pour, click, shake, result, coin | Procedurally generated in GameAudio.cs | Feedback |

**Licence note:** the submitted game does not require third-party visual or audio assets. The UI and sound effects are generated by the project itself. If external music is added later, it must be documented with its licence before submission.

**Technical art rules:** warm dark bar palette; ingredient colours stay consistent between buttons and glass; reaction colours stay consistent (green = strong result, orange = warning, red = poor result).

## 7. Technical Design

**Scenes:** one scene, Assets/Scenes/Game.unity.

**Packages / systems used:** Unity uGUI, ScriptableObjects, PlayerPrefs, AudioClip generation, coroutines and standard keyboard input.

**Target device:** Windows desktop/laptop, mouse + keyboard, 1920 × 1080 demo resolution.

### Architecture

~~~mermaid
graph TD
    GC[BarGameController - state and shift flow] --> UI[BarUI - runtime screens and HUD]
    GC --> MIX[DrinkMixer - current glass]
    GC --> EVA[DrinkEvaluator - accuracy and payment]
    GC --> AUD[GameAudio - procedural SFX]
    CFG[BarGameConfig - ScriptableObject] -.-> GC
    REC[DrinkRecipe assets - 6 recipes] -.-> GC
    MIX --> EVA
    REC --> EVA
~~~

| Script | Responsibility |
|---|---|
| BarGameController.cs | Game state, customer loop, patience, earnings and persistent best shift |
| BarUI.cs | Builds and updates the UI and forwards button input |
| DrinkMixer.cs | Stores current ingredient counts and shake state |
| DrinkEvaluator.cs | Calculates accuracy, reaction, payment and tip |
| DrinkRecipe.cs | ScriptableObject definition for recipe data |
| BarGameConfig.cs | ScriptableObject for tunable balancing values |
| IngredientType.cs | Ingredient enum, names and colour identity |
| CustomerData.cs | Customer presentation data |
| GameAudio.cs | Generates and plays simple sound effects |

### Course features implemented

1. **ScriptableObjects** — six drink recipes and the balance configuration are editable data assets.
2. **Dictionary data structure** — DrinkMixer stores a quantity for each IngredientType.
3. **Explicit game state** — menu, instructions, preparation, result and shift-complete states prevent gameplay input from leaking into other screens.
4. **Coroutine** — the short hint message uses a coroutine without blocking gameplay.
5. **PlayerPrefs** — only the best shift total is persisted.

## 8. Scope

### 8.1 MVP

- [x] Main menu and instructions
- [x] Six drink recipe assets
- [x] Ingredient quantity input
- [x] Reset glass
- [x] Shake / no-shake preparation rule
- [x] Recipe evaluator
- [x] Four reaction tiers
- [x] Patience timer
- [x] Payment and tip calculation
- [x] Six-customer shift
- [x] End-of-shift results
- [x] Persistent best shift
- [x] Mouse and keyboard controls
- [x] Basic sound feedback

### 8.2 Polish

- [x] Consistent visual palette
- [x] Dynamic drink colour in the glass
- [x] Customer colour identities
- [x] Fast-service tip bonus
- [x] Context hint line
- [ ] Optional background music
- [ ] More portrait art if time remains

### 8.3 Explicitly out of scope

- Alcohol brands or real cocktail simulation
- Multiplayer or online leaderboards
- Restaurant movement / character controller
- Inventory purchasing or stock management
- Staff management
- Story campaign or dialogue trees
- Mobile build
- Procedural recipes
- Multiple bar locations
- Save data beyond one local best score

## Changelog

| Version | Date | Change |
|---|---|---|
| v0.1 | 2026-10-01 | Initial bartender concept |
| v0.2 | 2026-10-01 | Reduced scope to one six-customer shift and fictional drinks |
| v0.3 | 2026-10-01 | Finalized recipe evaluation, patience, tips, UI layout and technical implementation |
