using System;
using NaughtyAttributes;
using UnityEngine;

namespace _Assets.Scripts.Loot_Tables.Generics
{
    [Serializable]
    public abstract class GenericLootTableItem<T>
    {
        public T Item;
        public float ProbabilityWeight;

        [MinMaxSlider(1,10)]
        public Vector2Int QuantityRange = new(1,10);

        [HideInInspector]
        public float ProbabilityRangeFrom;
        [HideInInspector]
        public float ProbabilityRangeTo;
    }
}
