using UnityEngine;

public class BottleClickable : MonoBehaviour
{
    public IngredientType ingredient;
    public Transform visual;

    private Vector3 baseScale = Vector3.one;
    private Vector3 basePosition = Vector3.zero;

    private void Start()
    {
        if (visual == null) visual = transform;
        baseScale = visual.localScale;
        basePosition = visual.localPosition;
    }

    public void SetHovered(bool hovered)
    {
        if (visual == null) return;
        visual.localScale = baseScale * (hovered ? 1.12f : 1f);
        visual.localPosition = basePosition + (hovered ? Vector3.up * 0.10f : Vector3.zero);
    }
}