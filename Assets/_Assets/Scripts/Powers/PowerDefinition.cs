using UnityEngine;

public class PowerDefinition : ScriptableObject
{
    [Header("Common")]
    public string DisplayName;
    [TextArea] public string Description;

    [Header("Visual Settings")]
    public Sprite HatSprite;
    public RuntimeAnimatorController AnimatorController;
    public Color ColorTint = Color.white;

    [Header("Visual Effects")]
    public GameObject[] PowerVFX;

    [Header("Category")]
    public bool IsPassive;

    public virtual void OnEquip(PlayerPowerController controller)
    {
        PlayerVisualController visualController = controller.GetComponent<PlayerVisualController>();
        if (visualController)
            visualController.ApplyVisuals(this);
    }
    public virtual void OnUnequip(PlayerPowerController controller)
    {
        PlayerVisualController visualController = controller.GetComponent<PlayerVisualController>();
        if (visualController)
            visualController.ResetToDefaultVisuals();
    }
    public virtual PowerRuntime CreateRuntime(PlayerPowerController controller) { return null; }
}
