using UnityEngine;

[CreateAssetMenu(fileName = "new_item", menuName = "Loot/Items/New Item")]
public class Item : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    public string Description;
}
