using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerRecoil : MonoBehaviour
{
    [Header("Config")]
    [SerializeField, Expandable] private RecoilConfig _config;

    private PlayerMovement _movement;

    public RecoilConfig Config => _config;

    private void Awake() => _movement = GetComponent<PlayerMovement>();

    public void SetConfig(RecoilConfig cfg) => _config = cfg;

    public void AttackHorizontalRecoil(bool attackToRight)
    {
        if (_movement == null || _config == null) return;

        float sign = attackToRight ? -1f : 1f;
        float hSpeed = _config.AttackHorizontalSpeed;
        float maxAllowed = _config.PogoUpVelocity*_config.AttackHorizontalVsVerticalRatio;
        if (hSpeed > maxAllowed) hSpeed = maxAllowed;

        Vector2 recoil = new(sign*hSpeed, _movement.VerticalSpeed);
        _movement.ApplyRecoil(recoil, _config.AttackHorizontalDuration, overrideX: true, overrideY: false);
    }

    public void PogoRecoil(float upVelocity)
    {
        if (!_movement || !_config) return;
        float up = Mathf.Max(0f, upVelocity);
        if (up <= 0f) up = _config.PogoUpVelocity;
        Vector2 recoil = new(_movement.HorizontalSpeed, up);
        _movement.ApplyRecoil(recoil, _config.PogoDuration, overrideX: false, overrideY: true);
    }

    public void ApplyHitRecoilFromSource(Vector2 sourcePosition)
    {
        if (!_movement || !_config) return;

        Vector2 toPlayer = (Vector2)transform.position - sourcePosition;
        if (toPlayer.sqrMagnitude < 0.0001f)
            toPlayer = _movement.FacingRight ? Vector2.right : Vector2.left;

        float absX = Mathf.Abs(toPlayer.x);
        float absY = Mathf.Abs(toPlayer.y);

        bool verticalBounce = absY > absX && toPlayer.y > 0f;
        if (verticalBounce)
        {
            float horizontalSign = Mathf.Sign(toPlayer.x);
            if (Mathf.Abs(toPlayer.x) < _config.TinySeparationThreshold)
            {
                horizontalSign = _movement.FacingRight ? 1f : -1f;
                horizontalSign *= _config.MinSeparationPushMultiplier;
            }

            Vector2 recoil = new(horizontalSign*_config.VerticalBounceHorizontalNudge, _config.VerticalBounceVelocity);
            _movement.ApplyRecoil(recoil, _config.VerticalBounceDuration, overrideX: true, overrideY: true);
        }
        else
        {
            float horizontalSign = Mathf.Sign(toPlayer.x);
            if (horizontalSign == 0f)
                horizontalSign = _movement.FacingRight ? -1f : 1f;

            float hSpeed = _config.HitHorizontalSpeed;
            if (Mathf.Abs(toPlayer.x) < _config.TinySeparationThreshold)
                hSpeed *= _config.MinSeparationPushMultiplier;

            float yVel = Mathf.Max(_movement.VerticalSpeed, _config.HitVerticalBoost);
            Vector2 recoil = new(horizontalSign*hSpeed, yVel);
            _movement.ApplyRecoil(recoil, _config.HitHorizontalDuration, overrideX: true, overrideY: true);
        }
    }
}
