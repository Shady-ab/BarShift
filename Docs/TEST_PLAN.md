# BarShift — Test Plan

## Smoke test

1. Open Assets/Scenes/Game.unity in Unity 6000.3.20f1.
2. Enter Play Mode.
3. Main menu should appear without Console errors.
4. Start Shift should load customer 1/6 with an order ticket.
5. Ingredient buttons and keys 1–7 should add units to the glass.
6. Reset should clear the glass; Shake should change the technique label once.
7. Serve should open the customer result screen.
8. Six completed customers should open Shift Complete.
9. Starting another shift should reset earnings and stats but preserve Best Shift.

## Gameplay cases

| Case | Steps | Expected |
|---|---|---|
| Perfect recipe | Match every ticket amount and technique | Perfect tier, full price, highest normal tip |
| Correct mix, wrong technique | Match ingredients but violate shake rule | Accuracy reduced by technique multiplier |
| One unit missing | Serve with one required unit missing | Accuracy below exact recipe |
| Extra ingredient | Add an ingredient not on ticket | Accuracy reduced |
| Empty glass | Serve immediately | Terrible result |
| Full glass | Add 12 units, then try one more | Extra unit rejected; hint says glass is full |
| Double shake | Shake twice | Second shake has no effect; hint appears |
| Slow service | Wait until patience reaches zero | Customer leaves, $0 payment, shift continues |
| Fast good service | Serve Good/Perfect while >55% patience | Speed bonus added to tip |
| New best | Finish above stored best | Best value updates and persists after restart |

## Data checks

- Resources/Data/Recipes contains six DrinkRecipe assets.
- Resources/Data/BarGameConfig.asset exposes the main tuning values.
- No external audio or image asset is required for the game to run.
- Docs/GDD.md contains the final scope and implementation mapping.
