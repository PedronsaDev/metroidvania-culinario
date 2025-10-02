using UnityEngine;
using UnityEngine.InputSystem;
public class GlobalUIInput : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _cancel;
    [SerializeField] private BaseUIWindow _defaultWindow;

    private void OnEnable()
    {
        if (_cancel)
        {
            _cancel.action.Enable();
            _cancel.action.performed += OnCancel;
        }
    }

    private void OnDisable()
    {
        if (_cancel)
        {
            _cancel.action.performed -= OnCancel;
            _cancel.action.Disable();
        }
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        var ui = UIManager.Instance;
        if (!ui) return;

        if (ui.HasAnyOpen)
        {
            ui.CloseTop();
        }
        else if (_defaultWindow)
        {
            if (ctx.control.device is Keyboard)
                _defaultWindow.Show();
        }
    }
}
