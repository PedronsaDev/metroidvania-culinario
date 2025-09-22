using _Assets.Scripts.Loot_Tables.Generics;
using _Assets.Scripts.Loot_Tables.Item_Types;
using UnityEngine;

namespace _Assets.Scripts.Loot_Tables.Table_Types
{
    [CreateAssetMenu(fileName = "new_generic_loot_table", menuName = "Loot/Tables/Generic Loot Table")]
    public class ObjectLootTable : GenericLootTable<ObjectLootTableItem, UnityEngine.Object>
    {
    }
}
