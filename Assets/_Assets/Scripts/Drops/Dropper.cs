using _Assets.Scripts.Loot_Table;
using NaughtyAttributes;
using UnityEngine;

namespace _Assets.Scripts.Drops
{
    [DisallowMultipleComponent]
    public class Dropper : MonoBehaviour, IDropper
    {
        [Header("Dropper")]
        [Expandable]
        [SerializeField] private ItemLootTable _lootTable;
        [SerializeField, Range(-1f, 1f)] private float _luckModifier;
        [SerializeField] private Transform _dropOriginOverride;
        [SerializeField] private bool _dropOnDisable;
        [SerializeField] private bool _dropOnlyOnce = true;

        private bool _hasDropped;
        private static bool _applicationQuitting;

        public ItemLootTable LootTable => _lootTable;
        public float LuckModifier => _luckModifier;
        public Transform DropOrigin => _dropOriginOverride ? _dropOriginOverride : transform;

        private void OnDisable()
        {
            if (_applicationQuitting) return;
            if (!_dropOnDisable) return;
            TryDrop();
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        [Button("Test Drop")]
        public void DropNow() => TryDrop();

        public void TryDrop()
        {
            if (_dropOnlyOnce && _hasDropped)
                return;

            var mgr = DropManager.GetOrCreate();
            mgr.DropFrom(this);
            _hasDropped = true;
        }

        public void SetLootTable(ItemLootTable table) => _lootTable = table;
        public void SetLuck(float luck) => _luckModifier = Mathf.Clamp(luck, -1f, 1f);
        public void SetDropOrigin(Transform t) => _dropOriginOverride = t;
    }
}
