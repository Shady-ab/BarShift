using UnityEngine;

[CreateAssetMenu(menuName = "BarShift/Game Config", fileName = "BarGameConfig")]
public class BarGameConfig : ScriptableObject
{
    [Header("Shift")]
    [Min(1)] public int customersPerShift = 6;
    [Min(10f)] public float customerPatienceSeconds = 42f;
    [Min(0.5f)] public float resultScreenSeconds = 2.2f;

    [Header("Evaluation")]
    [Range(0f, 1f)] public float perfectThreshold = 0.93f;
    [Range(0f, 1f)] public float goodThreshold = 0.72f;
    [Range(0f, 1f)] public float badThreshold = 0.45f;
    [Range(0f, 1f)] public float wrongTechniqueMultiplier = 0.82f;

    [Header("Tips")]
    [Min(0)] public int perfectTip = 6;
    [Min(0)] public int goodTip = 3;
    [Min(0)] public int fastBonusTip = 2;
    [Range(0f, 1f)] public float fastBonusThreshold = 0.55f;

    [Header("Mixing")]
    [Min(1)] public int maxUnitsInGlass = 12;

    public static BarGameConfig CreateRuntimeDefault()
    {
        BarGameConfig config = CreateInstance<BarGameConfig>();
        config.name = "Runtime Bar Config";
        return config;
    }
}
