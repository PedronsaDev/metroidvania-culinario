using NaughtyAttributes;
using UnityEngine;

public class PowerDefinition : ScriptableObject
{
    [Header("Common")]
    public string DisplayName;
    [TextArea] public string Description;

    [Header("Category")]
    public bool IsPassive;

    public virtual void OnEquip(PlayerPowerController controller) { }
    public virtual void OnUnequip(PlayerPowerController controller) { }
    public virtual PowerRuntime CreateRuntime(PlayerPowerController controller) { return null; }
}
