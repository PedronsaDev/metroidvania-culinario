using UnityEngine;
public readonly struct InteractionContext
{
    public readonly GameObject Initiator;
    public readonly Vector2 Position;
    public readonly float Time;

    public InteractionContext(GameObject initiator, Vector2 position, float time)
    {
        Initiator = initiator;
        Position = position;
        Time = time;
    }
}
