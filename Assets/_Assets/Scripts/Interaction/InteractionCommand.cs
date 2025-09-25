using UnityEngine;
public abstract class InteractionCommand : ScriptableObject
{
    public abstract bool CanExecute(IInteractable target, in InteractionContext ctx);
    public abstract void Execute(IInteractable target, in InteractionContext ctx);
}