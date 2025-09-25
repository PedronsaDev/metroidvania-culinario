using UnityEngine;
[CreateAssetMenu(menuName = "Interaction/Commands/Simple Invoke")]
public class SimpleInvokeCommand : InteractionCommand
{
    public override bool CanExecute(IInteractable target, in InteractionContext ctx) =>
        target.IsEnabled && target.CanInteract(ctx);

    public override void Execute(IInteractable target, in InteractionContext ctx) =>
        target.Interact(ctx);
}
