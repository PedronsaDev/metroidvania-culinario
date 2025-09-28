using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Assets.Scripts.Drops
{
    [DefaultExecutionOrder(-200)]
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [Header("Spawn settings")]
        [SerializeField] private GameObject _defaultPickupPrefab;
        [SerializeField, Min(0f)] private float _spawnSpreadRadius = 0.35f;
        [SerializeField, Min(0.1f)] private float _defaultLifetimeSeconds = 120f;
        [SerializeField, Min(0f)]
        private float _perDropDelaySeconds = 0f;
        [SerializeField]
        private bool _useUnscaledTimeForDelay = false;

        private readonly HashSet<DroppedItem> _activeDrops = new HashSet<DroppedItem>();

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static DropManager GetOrCreate()
        {
            if (Instance)
                return Instance;

            DropManager found = FindFirstObjectByType<DropManager>();

            if (found)
            {
                Instance = found;
                return Instance;
            }

            GameObject go = new("[DROP MANAGER]");
            DropManager mgr = go.AddComponent<DropManager>();
            DontDestroyOnLoad(go);

            return mgr;
        }

        public void Register(DroppedItem item) => _activeDrops.Add(item);
        public void Unregister(DroppedItem item) => _activeDrops.Remove(item);

        [ContextMenu("Clear All Drops")]
        public void ClearAllDrops()
        {
            List<DroppedItem> copy = new List<DroppedItem>(_activeDrops);
            foreach (DroppedItem drop in copy)
            {
                if (drop) Destroy(drop.gameObject);
            }
            _activeDrops.Clear();
        }

        public int ClearDropsInRadius(Vector3 position, float radius)
        {
            int removed = 0;
            List<DroppedItem> toRemove = new List<DroppedItem>();
            foreach (DroppedItem drop in _activeDrops)
            {
                if (!drop)
                {
                    toRemove.Add(drop);
                    continue;
                }
                if ((drop.transform.position - position).sqrMagnitude <= radius*radius)
                {
                    Destroy(drop.gameObject);
                    removed++;
                }
            }
            foreach (DroppedItem d in toRemove) _activeDrops.Remove(d);
            return removed;
        }

        public void DropFrom(IDropper dropper)
        {
            DropFrom(dropper, _perDropDelaySeconds);
        }

        public void DropFrom(IDropper dropper, float perDropDelaySeconds)
        {
            if (dropper == null) return;
            if (!dropper.LootTable)
            {
                Debug.LogWarning($"[DROP MANAGER] Dropper {dropper} has no LootTable assigned.");
                return;
            }

            if (dropper.LootTable is { } objectLoot)
            {
                objectLoot.ValidateTable();
                List<Item> results = objectLoot.GetLootDropItem(dropper.LuckModifier);

                Vector3 origin = dropper.DropOrigin ? dropper.DropOrigin.position : Vector3.zero;

                StartDropSequence(results, origin, perDropDelaySeconds);
            }
            else
            {
                Debug.LogWarning($"[DROP MANAGER] Unsupported loot table type: {dropper.LootTable.GetType().Name}. Only ObjectLootTable is supported by default. You can extend DropManager to handle your type.");
            }
        }

        public void StartDropSequence(List<Item> items, Vector3 origin, float perDropDelaySeconds)
        {
            if (items == null || items.Count == 0)
                return;

            if (perDropDelaySeconds <= 0f)
            {
                foreach (Item loot in items)
                {
                    if (!loot) continue;
                    SpawnLootObject(loot, origin);
                }
            }
            else
            {
                StartCoroutine(DropSequence(items, origin, perDropDelaySeconds));
            }
        }

        public void StartDropSequence(Item item, int count, Vector3 origin)
        {
            if (!item)
                return;

            if (count > 1)
                StartCoroutine(DropSequence(item, count, origin, 0.25f));
            else
                SpawnLootObject(item, origin);

        }

        private IEnumerator DropSequence(List<Item> items, Vector3 origin, float perDropDelaySeconds)
        {
            YieldInstruction wait = new WaitForSeconds(perDropDelaySeconds);

            for (int i = 0; i < items.Count; i++)
            {
                Item loot = items[i];
                if (loot)
                    SpawnLootObject(loot, origin);

                if (i < items.Count - 1)
                    yield return wait;
            }
        }

        private IEnumerator DropSequence(Item item, int count, Vector3 origin, float perDropDelaySeconds)
        {
            bool useUnscaled = _useUnscaledTimeForDelay;
            YieldInstruction wait = new WaitForSeconds(perDropDelaySeconds);

            for (int i = 0; i < count; i++)
            {
                SpawnLootObject(item, origin);

                if (i < count - 1)
                    yield return wait;
            }
        }

        public GameObject SpawnLootObject(Item loot, Vector3 origin)
        {
            var offset = (Vector3)(Random.insideUnitCircle*_spawnSpreadRadius);
            Vector3 spawnPos = origin + offset;

            GameObject goInstance = null;

            if (_defaultPickupPrefab)
            {
                goInstance = ObjectPoolManager.SpawnGameObject(_defaultPickupPrefab, spawnPos, Quaternion.identity);

                var di = goInstance.GetComponent<DroppedItem>();

                if (!di)
                    di = goInstance.AddComponent<DroppedItem>();

                di.SetDefaultsIfNeeded(_defaultLifetimeSeconds);
                di.Initialize(loot);

                Register(di);
                di.TryToss();

                var pickup = goInstance.GetComponent<InteractablePickup>();
                if (!pickup)
                    goInstance.AddComponent<InteractablePickup>();

                pickup.Reset();
            }
            else
            {
                Debug.LogWarning($"[DropManager] Can't spawn loot {loot.name} because no defaultPickupPrefab is configured.");
            }

            return goInstance;
        }

        private static DroppedItem EnsureDroppedItem(GameObject go)
        {
            DroppedItem di = go.GetComponent<DroppedItem>();
            if (!di) di = go.AddComponent<DroppedItem>();
            return di;
        }
    }
}
