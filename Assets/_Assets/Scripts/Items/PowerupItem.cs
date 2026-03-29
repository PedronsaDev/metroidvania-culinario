
using UnityEngine;
[CreateAssetMenu (fileName = "new_powerup_item", menuName = "Loot/Items/New Powerup Item")]
public class PowerupItem : Item
{
    public PowerDefinition Power;

    public override void OnPickup()
    {
        base.OnPickup();
        if (Power) PlayerInstance.Instance.GetComponent<PlayerPowerController>().Equip(Power);
    }
}
