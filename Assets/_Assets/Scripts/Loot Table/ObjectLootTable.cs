using UnityEngine;

namespace _Assets.Scripts.Loot_Table
{
    [CreateAssetMenu(fileName = "New Object Loot Table", menuName = "Loot/Loot Table (Object)")]
    public class ObjectLootTable : GenericLootTable<ObjectLootTableItem, UnityEngine.Object>
    {
    }
}
