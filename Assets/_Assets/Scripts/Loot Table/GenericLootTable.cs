using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Assets.Scripts.Loot_Table
{
    public class GenericLootTable<T,U> : LootTableBase where T: GenericLootTableItem<U>
    {
        [SerializeField] private List<GaranteedLootTableItem<U>> _garanteedItems;
        [SerializeField] private List<T> _additionalItems;


        private float _probabilityTotalWeight;

        private float _luckModifier;

        public void ValidateTable()
        {
            if(_additionalItems != null && _additionalItems.Count > 0)
            {
                float currentProbabilityWeightMaximum = 0f;

                foreach(T lootDropItem in _additionalItems)
                {
                    if(lootDropItem.ProbabilityWeight < 0f)
                    {
                        // keep zero/negative as non-contributing
                    }
                    else
                    {
                        lootDropItem.ProbabilityRangeFrom = currentProbabilityWeightMaximum;
                        currentProbabilityWeightMaximum += lootDropItem.ProbabilityWeight;
                        lootDropItem.ProbabilityRangeTo = currentProbabilityWeightMaximum;
                    }
                }

                _probabilityTotalWeight = currentProbabilityWeightMaximum;
            }
            else
            {
                _probabilityTotalWeight = 0f;
            }
        }

        public List<U> GetLootDropItem(float luckModifier = 0f)
        {
            List<U> selectedDrops = new List<U>();

            _luckModifier = Mathf.Clamp(luckModifier, -1f, 1f);

            if (_probabilityTotalWeight <= 0f)
            {
                // Only guaranteed items if there are no weighted items or total is zero
                foreach (GaranteedLootTableItem<U> guaranteedItem in _garanteedItems)
                {
                    int quantityG = Random.Range(guaranteedItem.QuantityRange.x, guaranteedItem.QuantityRange.y + 1);
                    for (int i = 0; i < quantityG; i++)
                        selectedDrops.Add(guaranteedItem.Item);
                }
                return selectedDrops;
            }

            float pickedNumber = Random.Range(0, _probabilityTotalWeight);

            pickedNumber += _luckModifier * (_probabilityTotalWeight / 4f);
            pickedNumber = Mathf.Clamp(pickedNumber, 0, _probabilityTotalWeight);

            foreach(T lootDropItem in _additionalItems)
            {
                if (pickedNumber > lootDropItem.ProbabilityRangeFrom && pickedNumber < lootDropItem.ProbabilityRangeTo)
                {
                    int quantity = Random.Range(lootDropItem.QuantityRange.x, lootDropItem.QuantityRange.y + 1);
                    for (int i = 0; i < quantity; i++)
                        selectedDrops.Add(lootDropItem.Item);
                    break;
                }
            }

            foreach(GaranteedLootTableItem<U> guaranteedItem in _garanteedItems)
            {
                int quantity = Random.Range(guaranteedItem.QuantityRange.x, guaranteedItem.QuantityRange.y + 1);
                for (int i = 0; i < quantity; i++)
                    selectedDrops.Add(guaranteedItem.Item);
            }

            return selectedDrops;
        }

        // Ensure probability ranges are always up to date in the editor
        private void OnValidate()
        {
            ValidateTable();
        }
    }

    [Serializable]
    public class GaranteedLootTableItem<T>
    {
        public T Item;

        [MinMaxSlider(1,10)]
        public Vector2Int QuantityRange = new Vector2Int(1,1);
    }

    public class DropItem<T>
    {
        public T Item;
        public int Quantity;
    }
}
