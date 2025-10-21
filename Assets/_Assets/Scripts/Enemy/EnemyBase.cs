using _Assets.Scripts.Drops;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

[RequireComponent(typeof(Dropper))]
public class EnemyBase : Damageable
{
    private Dropper _dropper;

    [Header("Hit FX - Squash & Stretch"), Foldout("Squash & Stretch")]
    [SerializeField] private Transform _squashTarget;
    [SerializeField, Min(0.5f), Foldout("Squash & Stretch")] private float _squashXMult = 1.6f;
    [SerializeField, Min(0.2f), Foldout("Squash & Stretch")] private float _squashYMult = 0.6f;
    [SerializeField, Min(0.01f), Foldout("Squash & Stretch")] private float _squashInDuration = 0.06f;
    [SerializeField, Min(0.01f), Foldout("Squash & Stretch")] private float _squashOutDuration = 0.10f;
    [SerializeField, Foldout("Squash & Stretch")] private Ease _squashInEase = Ease.OutQuad;
    [SerializeField, Foldout("Squash & Stretch")] private Ease _squashOutEase = Ease.OutBack;
    [SerializeField, Foldout("Squash & Stretch")] private bool _squashUseUnscaledTime = false;

    private Tween _squashTween;

    protected override void OnDamageApplied(Vector3 hitDirection, int damage)
    {
        base.OnDamageApplied(hitDirection, damage);
        SquashAndStretch();
    }

    protected void SquashAndStretch()
    {
        if (!_squashTarget)
            _squashTarget = transform;

        _squashTween?.Kill(false);

        Vector3 s = _squashTarget.localScale;
        float signX = Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Sign(s.x);
        Vector3 abs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        Vector3 hitScale = new Vector3(abs.x*Mathf.Max(0.01f, _squashXMult)*signX,
            abs.y*Mathf.Max(0.01f, _squashYMult),
            abs.z);
        Vector3 restScale = new Vector3(abs.x*signX, abs.y, abs.z);

        var seq = DOTween.Sequence();
        seq.Append(_squashTarget.DOScale(hitScale, _squashInDuration).SetEase(_squashInEase));
        seq.Append(_squashTarget.DOScale(restScale, _squashOutDuration).SetEase(_squashOutEase));

        seq.SetUpdate(_squashUseUnscaledTime);
        _squashTween = seq;
    }

    protected override void Awake()
    {
        base.Awake();
        _dropper = GetComponent<Dropper>();
        if (!_squashTarget)
            _squashTarget = transform;
    }

    protected override void Die()
    {
        _dropper.DropNow();
        CameraManager.Instance.ShakeCamera(1f);
        HitPause.Instance?.Do(0.13f);
        base.Die();
    }

    protected virtual void OnDisable()
    {
        if (_squashTween != null && _squashTween.IsActive())
        {
            _squashTween.Kill(false);
            if (_squashTarget)
            {
                Vector3 s = _squashTarget.localScale;
                float signX = Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Sign(s.x);
                Vector3 abs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                _squashTarget.localScale = new Vector3(abs.x*signX, abs.y, abs.z);
            }
        }
    }
}
