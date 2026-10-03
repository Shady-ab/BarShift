# BarShift — Code Walkthrough Notes

These notes are for explaining the project during a demo or code review.

## The main idea

BarGameController is the coordinator. It does not calculate drink accuracy itself and it does not create UI elements itself. It asks smaller classes to do those jobs.

## Why ScriptableObjects?

A drink is data: name, ingredient amounts, shake rule, price and colour. DrinkRecipe stores that data in an asset. Adding or balancing a recipe does not require changing the controller.

BarGameConfig does the same for values that are likely to change during playtesting, such as patience time and score thresholds.

## How accuracy works

DrinkMixer stores how many units of every ingredient are currently in the glass.

DrinkEvaluator calculates:

matched units / max(expected units, player units)

This means both missing ingredients and extra ingredients reduce the score. After that, the wrong shake technique applies a multiplier. Thresholds convert the final value into Perfect, Good, Bad or Terrible.

## Why one scene?

This game is a short repeated service loop, not an exploration game. One scene keeps the menu, gameplay and results fast and avoids scene-loading code that does not add anything useful to the assignment.

## Why is the UI built at runtime?

The UI is still normal Unity uGUI. Building it in BarUI keeps the repository small and makes all generated controls easy to reproduce. The gameplay data itself is not hard-coded into the UI; recipes remain separate assets.

## Persistence

Only Best Shift is saved with PlayerPrefs. A larger save system would be unnecessary for this scope.
