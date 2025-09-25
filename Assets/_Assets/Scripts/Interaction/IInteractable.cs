using UnityEngine;

public interface IInteractable
{
    string DisplayName { get; }
    int Priority { get; }
    bool IsEnabled { get; }
    Vector2 WorldPosition { get; }

    bool CanInteract(in InteractionContext context);
    void Interact(in InteractionContext context);
}