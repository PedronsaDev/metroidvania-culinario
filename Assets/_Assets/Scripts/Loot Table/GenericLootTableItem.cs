using System;
using NaughtyAttributes;
using UnityEngine;

namespace _Assets.Scripts.Loot_Table
{
    [Serializable]
    public abstract class GenericLootTableItem<T>
    {
        public T Item;
        public float ProbabilityWeight;

        [MinMaxSlider(1,10)]
        public Vector2Int QuantityRange = new Vector2Int(1,1);

        [HideInInspector]
        public float ProbabilityRangeFrom;
        [HideInInspector]
        public float ProbabilityRangeTo;
    }
}
