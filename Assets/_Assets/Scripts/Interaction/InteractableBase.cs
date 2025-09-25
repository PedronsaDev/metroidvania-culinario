using UnityEngine;
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string _displayName = "Interactable";
    [SerializeField] private int _priority = 0;
    [SerializeField] private bool _enabled = true;

    public string DisplayName => _displayName;
    public int Priority => _priority;
    public bool IsEnabled => _enabled;
    public Vector2 WorldPosition => (Vector2)transform.position;

    public abstract bool CanInteract(in InteractionContext context);
    public abstract void Interact(in InteractionContext context);
}
