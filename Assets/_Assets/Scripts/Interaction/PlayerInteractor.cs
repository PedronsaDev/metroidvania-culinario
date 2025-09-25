using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour, IItemCollector
{
    [Header("Detection")]
    [SerializeField] private InteractionDetector2D _detector;
    [SerializeField] private InteractionCommand _command;

    [Header("Input")]
    [SerializeField] private InputActionReference _interactAction;

    [Header("Timing")]
    [SerializeField] private float _cooldown = 0.25f;

    private readonly List<IInteractable> _candidates = new();
    private float _nextAllowed;
    public IInteractable Current { get; private set; }

    public event System.Action<IInteractable> HoverChanged;
    public event System.Action<IInteractable> Interacted;

    private void OnEnable()
    {
        if (_interactAction)
        {
            _interactAction.action.Enable();
            _interactAction.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (_interactAction)
        {
            _interactAction.action.Disable();
            _interactAction.action.performed -= OnInteract;
        }
    }

    private void Update()
    {
        _detector.Collect(_candidates);

        IInteractable best = InteractionSelector.SelectBest(_candidates, transform.position);

        if (best != null)
        {
            var ctx = new InteractionContext(gameObject, transform.position, Time.time);
            if (!best.CanInteract(in ctx))
                best = null;
        }

        if (best != Current)
        {
            Current = best;
            HoverChanged?.Invoke(Current);
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryInteract();
    }

    private void TryInteract()
    {
        if (Time.time < _nextAllowed) return;
        if (Current == null) return;

        var context = new InteractionContext(gameObject, transform.position, Time.time);

        if (_command.CanExecute(Current, context))
        {
            _command.Execute(Current, context);
            Interacted?.Invoke(Current);
            _nextAllowed = Time.time + _cooldown;
        }
    }
    public bool CanAccept(Item item, int quantity)
    {
        return true;
    }

    public void AddItem(Item item, int quantity)
    {
        Debug.Log("Collected " + quantity + " x " + item.name);
    }
}
