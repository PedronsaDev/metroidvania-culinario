using _Assets.Scripts.Loot_Tables.Generics;
using UnityEngine;

[CreateAssetMenu(fileName = "new_item_loot_table", menuName = "Loot/Tables/Item Loot Table")]
public class ItemLootTable : GenericLootTable<ItemLootTableItem, Item>
{

}
