using UnityEngine;

namespace _Assets.Scripts.Drops
{
    public interface IDropper
    {
        ItemLootTable LootTable { get; }
        float LuckModifier { get; }
        Transform DropOrigin { get; }
    }
}

