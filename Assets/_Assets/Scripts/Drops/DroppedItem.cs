using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Assets.Scripts.Drops
{
    public class DroppedItem : MonoBehaviour
    {
        public Item Payload;

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Min(0.1f)] private float _lifetimeSeconds = 120f;
        [SerializeField] private bool _autoDespawn = true;

        [Header("Toss FX")]
        [SerializeField, Range(5f, 85f)] private float _maxTossAngle = 35f;
        [SerializeField] private Vector2 _tossForceRange = new Vector2(3f, 7f);
        [SerializeField] private Vector2 _spinImpulseRange = new Vector2(-25f, 25f);
        [SerializeField] private float _airDrag = 0.5f;
        [SerializeField] private float _angularDrag = 0.05f;
        [SerializeField] private float _gravityScale = 1f;
        [SerializeField, Range(0f, 1f)] private float _bounceDamping = 0.5f;
        [SerializeField, Min(0)] private int _maxBounces = 2;
        [SerializeField, Range(0f, 0.5f)] private float _squashAmount = 0.12f;
        [SerializeField, Min(0.01f)] private float _squashDuration = 0.08f;

        private float _timeAlive;
        private bool _tossed;
        private int _bounces;
        private Vector3 _baseScale;
        private Coroutine _squashRoutine;
        private Rigidbody2D _rb2d;
        private bool _landed;
        public event Action OnCanBePickedUp;

        public bool IsPickable => !_tossed && _landed;

        private void OnEnable()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _timeAlive = 0f;
            _tossed = false;
            _bounces = 0;
            _landed = false;
            _baseScale = transform.localScale;
            DropManager.GetOrCreate().Register(this);
        }

        private void OnDisable()
        {
            if (DropManager.Instance)
                DropManager.Instance.Unregister(this);

            if (_squashRoutine != null)
            {
                StopCoroutine(_squashRoutine);
                _squashRoutine = null;
            }
            transform.localScale = _baseScale;
        }

        public void Initialize(Item payload)
        {
            Reset();

            Payload = payload;
            if (_renderer)
                _renderer.sprite = payload.Icon;
        }

        public void SetDefaultsIfNeeded(float defaultLifetime)
        {
            if (_lifetimeSeconds <= 0f) _lifetimeSeconds = Mathf.Max(1f, defaultLifetime);
        }

        private void Update()
        {
            if (!_autoDespawn) return;
            _timeAlive += Time.deltaTime;

            if (_timeAlive >= _lifetimeSeconds)
                ObjectPoolManager.ReturnObjectToPool(this.gameObject);

            if (_timeAlive >= 3f)
            {
                _tossed = false;
                _landed = true;
            }
        }

        public void TryToss()
        {
            if (_rb2d)
            {
                _rb2d.linearDamping = _airDrag;
                _rb2d.angularDamping = _angularDrag;
                _rb2d.gravityScale = _gravityScale;
                _rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                float angle = Random.Range(-_maxTossAngle, _maxTossAngle);
                Vector2 dir2d = Quaternion.Euler(0, 0, angle) * Vector2.up;

                float magnitude = Random.Range(_tossForceRange.x, _tossForceRange.y);
                _rb2d.AddForce(dir2d * magnitude, ForceMode2D.Impulse);

                float spin = Random.Range(_spinImpulseRange.x, _spinImpulseRange.y);
                _rb2d.AddTorque(spin, ForceMode2D.Impulse);

                _tossed = true;
                _bounces = 0;
                _landed = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Ground"))
                return;

            if (!_tossed)
                return;

            if (_rb2d)
            {
                Vector2 v = _rb2d.linearVelocity;
                v.y = -v.y * _bounceDamping;
                v.x *= Mathf.Lerp(1f, _bounceDamping, 0.35f);
                _rb2d.linearVelocity = v;
            }

            PlaySquash();

            _bounces++;
            if (_bounces >= _maxBounces)
            {
                _tossed = false;
                _landed = true;

                OnCanBePickedUp?.Invoke();
            }
        }

        private void PlaySquash()
        {
            if (_squashAmount <= 0f) return;
            if (_squashRoutine != null)
                StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(SquashStretch());
        }

        private IEnumerator SquashStretch()
        {
            float half = Mathf.Max(0.01f, _squashDuration);
            Vector3 squashed = new Vector3(
                _baseScale.x * (1f + _squashAmount),
                _baseScale.y * (1f - _squashAmount),
                _baseScale.z
            );

            float t = 0f;
            Vector3 start = transform.localScale;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.Lerp(start, squashed, k);
                yield return null;
            }

            t = 0f;
            start = transform.localScale;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.Lerp(start, _baseScale, k);
                yield return null;
            }

            transform.localScale = _baseScale;
            _squashRoutine = null;
        }

        public void Reset()
        {
            _timeAlive = 0f;
            _tossed = false;
            _bounces = 0;
            _landed = false;
            transform.localScale = _baseScale;

            OnCanBePickedUp = null;
        }

        [ContextMenu("Despawn Now")]
        public void DespawnNow() => Destroy(gameObject);
    }
}