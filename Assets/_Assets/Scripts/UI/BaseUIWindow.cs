using UnityEngine;
public abstract class BaseUIWindow : CanvasToggler
{
    [Header("Window")]
    [SerializeField] private string _windowId = "window";
    [SerializeField] private bool _blocksGameplay = true;
    public string WindowId => _windowId;
    public bool BlocksGameplay => _blocksGameplay;
    public bool IsOpen { get; private set; }

    public override void Show()
    {
        if (IsOpen)
            return;
        base.Show();
        IsOpen = true;
        UIManager.Instance?.NotifyWindowOpened(this);
    }

    public override void Hide()
    {
        if (!IsOpen)
            return;
        base.Hide();
        IsOpen = false;
        UIManager.Instance?.NotifyWindowClosed(this);
    }

    public override void Toggle()
    {
        if (IsOpen)
            Hide();
        else
            Show();
    }
}