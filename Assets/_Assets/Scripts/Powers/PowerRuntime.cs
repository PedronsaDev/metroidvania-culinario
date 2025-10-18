using System;

public abstract class PowerRuntime
{
    protected PlayerPowerController _controller;

    public static event Action OnPowerEnded;

    protected PowerRuntime(PlayerPowerController controller)
    {
        _controller = controller;
    }

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
    public virtual void ActivatePower() { }
    public virtual void Tick() { }
    public virtual void FixedTick() { }

    public virtual void End()
    {
        OnPowerEnded?.Invoke();
    }
}
