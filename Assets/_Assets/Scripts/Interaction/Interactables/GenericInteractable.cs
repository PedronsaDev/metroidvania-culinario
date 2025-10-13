using System;
using UnityEngine;
using UnityEngine.Events;

public class GenericInteractor : InteractableBase
{

    [SerializeField] private UnityEvent _onInteract;
    public event Action  OnInteract;

    public override bool CanInteract(in InteractionContext context) => true;
    public override void Interact(in InteractionContext context)
    {
        _onInteract?.Invoke();
        OnInteract?.Invoke();
    }
}
