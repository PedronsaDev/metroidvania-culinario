using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(menuName = "Movement/Movement Config", fileName = "movement_config")]
public class MovementConfig : ScriptableObject
{
    [Header("Horizontal")]
    [Range(1f, 40f)] public float WalkSpeed = 10f;
    [Range(1f, 40f)] public float RunSpeed = 16f;
    [Range(1f, 200f)] public float GroundAcceleration = 120f;
    [Range(1f, 200f)] public float GroundDeceleration = 80f;
    [Range(1f, 200f)] public float AirAcceleration = 60f;
    [Range(1f, 200f)] public float AirDeceleration = 55f;
    [Range(0f, 1f)] public float ApexHorizontalAssist = 0.65f;

    [Header("Jump Design (authoritative)")]
    public float JumpHeight = 6f;
    public float TimeToApex = 0.32f;
    [Range(1f, 5f)] public float GravityReleaseMultiplier = 2.2f;
    [Range(0, 4)] public int MaxAirJumps = 0;
    public float MinReleaseUpVelocity = 4f;
    [Range(0f, 50f)] public float MaxFallSpeed = 28f;
    [Range(0f, 60f)] public float FastFallSpeed = 34f;

    [Header("Ledge Fall Ease")]
    [Range(0f,1f)] public float LedgeWalkInitialGravityMultiplier = 0.2f;
    [Range(0f,0.6f)] public float LedgeWalkGravityRampTime = 0.25f;

    [Header("Advanced Horizontal Tweaks")]
    [Range(1f,4f)] public float TurnDecelMultiplier = 2f;

    [Header("Timing Windows")]
    [Range(0f, 0.25f)] public float CoyoteTime = 0.1f;
    [Range(0f, 0.25f)] public float JumpBufferTime = 0.12f;
    [Range(0f, 0.3f)] public float ApexEaseTime = 0.08f;

    [Header("Environment")]
    public LayerMask GroundMask;
    public float GroundProbeDistance = 0.06f;
    public float CeilingProbeDistance = 0.06f;
    public float GroundProbeWidthMultiplier = 0.9f;

    [Header("Debug")]
    public bool DebugProbes;

    [Header("Derived (readonly)")]
    [ReadOnly] public float Gravity;
    [ReadOnly] public float JumpVelocity;

    private void OnValidate() => Recalculate();
    private void OnEnable() => Recalculate();

    public void Recalculate()
    {
        Gravity = -(2f * JumpHeight) / (TimeToApex * TimeToApex);
        JumpVelocity = Mathf.Abs(Gravity) * TimeToApex;
    }
}
