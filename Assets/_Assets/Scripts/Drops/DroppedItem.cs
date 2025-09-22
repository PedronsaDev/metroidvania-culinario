using UnityEngine;

namespace _Assets.Scripts.Drops
{
    public class DroppedItem : MonoBehaviour
    {
        public Item Payload;

        [SerializeField] private SpriteRenderer _renderer;

        [SerializeField, Min(0.1f)] private float _lifetimeSeconds = 120f;
        [SerializeField] private bool _autoDespawn = true;

        private float _timeAlive;

        private void OnEnable()
        {
            _timeAlive = 0f;
            DropManager.GetOrCreate().Register(this);
        }

        private void OnDisable()
        {
            if (DropManager.Instance != null)
                DropManager.Instance.Unregister(this);
        }

        public void Initialize(Item payload)
        {
            Payload = payload;

            _renderer.sprite = payload.Icon;
        }

        public void SetDefaultsIfNeeded(float defaultLifetime, float defaultToss)
        {
            if (_lifetimeSeconds <= 0f) _lifetimeSeconds = Mathf.Max(1f, defaultLifetime);
        }

        private void Update()
        {
            if (!_autoDespawn) return;
            _timeAlive += Time.deltaTime;
            if (_timeAlive >= _lifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }

        public void TryToss(float force)
        {
            Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
            if (rb2d)
            {
                float angle = Random.Range(-30f, 30f);
                Vector2 dir2d = Quaternion.Euler(0, 0, angle) * Vector2.up;
                //Vector2 dir2d = Random.insideUnitCircle.normalized;
                rb2d.AddForce(dir2d * force, ForceMode2D.Impulse);
                return;
            }
        }

        [ContextMenu("Despawn Now")]
        public void DespawnNow()
        {
            Destroy(gameObject);
        }
    }
}
