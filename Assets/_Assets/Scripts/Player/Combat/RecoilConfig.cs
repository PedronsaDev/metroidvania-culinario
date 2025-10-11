using UnityEngine;

[CreateAssetMenu(fileName = "new_recoil_config", menuName = "Movement/Recoil Config", order = 10)]
public class RecoilConfig : ScriptableObject
{
    [Header("Attack Recoil (Player Attacks)")]
    [Min(0f)] public float AttackHorizontalSpeed = 12f;
    [Min(0f)] public float AttackHorizontalDuration = 0.08f;

    [Tooltip("Caps horizontal recoil relative to vertical pogo velocity: h <= v * ratio")]
    [Range(0f, 2f)] public float AttackHorizontalVsVerticalRatio = 0.5f;

    [Header("Pogo (Down Attack) Recoil")]
    [Min(0f)] public float PogoUpVelocity = 22f;
    [Min(0f)] public float PogoDuration = 0.04f;

    [Header("On-Hit Recoil (Player takes damage)")]
    [Min(0f)] public float HitHorizontalSpeed = 14f;
    [Min(0f)] public float HitHorizontalDuration = 0.12f;
    [Min(0f)] public float HitVerticalBoost = 6f;

    [Header("Vertical Bounce Case (enemy above player)")]
    [Min(0f)] public float VerticalBounceVelocity = 23f;
    [Min(0f)] public float VerticalBounceHorizontalNudge = 6f;
    [Min(0f)] public float VerticalBounceDuration = 0.08f;

    [Header("Separation Tweaks")]
    [Tooltip("Multiplies push when player and source are extremely close in X")]
    [Range(1f, 3f)] public float MinSeparationPushMultiplier = 1.6f;
    [Tooltip("If abs(dx) < threshold, consider it a tiny separation in X")]
    [Range(0f, 1f)] public float TinySeparationThreshold = 0.15f;
}
