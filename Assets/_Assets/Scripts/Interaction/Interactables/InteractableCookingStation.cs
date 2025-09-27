using System.Collections;
using _Assets.Scripts.Drops;
using UnityEngine;

public class InteractableCookingStation : InteractableBase
{
    [Header("Cooking")]
    [SerializeField] private float _cookDuration = 5f;
    [SerializeField] private bool _autoReleaseIfInterrupted = true;
    [SerializeField] private bool _refundOnCancel = true;

    [Header("Cancellation")]
    [SerializeField] private float _cancelDistance = 2.5f;
    [SerializeField] private bool _cancelOnPlayerDisabled = true;

    private bool _busy;
    private Coroutine _cookRoutine;
    private Transform _currentUser;
    private Recipe _activeRecipe;
    private IInventory _activeInventory;

    public static event System.Action<InteractableCookingStation, Transform> OnStationInteracted;
    public event System.Action<InteractableCookingStation, Recipe> OnCookingStarted;
    public event System.Action<InteractableCookingStation, Recipe> OnCookingCompleted;
    public event System.Action<InteractableCookingStation, Recipe> OnCookingCanceled;

    public override bool CanInteract(in InteractionContext context) => !_busy && IsEnabled;

    public override void Interact(in InteractionContext context)
    {
        if (_busy)
            return;

        _currentUser = context.Initiator ? context.Initiator.transform : null;

        OnStationInteracted?.Invoke(this, _currentUser);
    }

    public bool TryStartCooking(Recipe recipe, IInventory inventory)
    {
        if (_busy || !recipe || inventory == null)
            return false;

        if (!RecipesManager.IsUnlocked(recipe))
            return false;

        if (!inventory.ConsumeIngredients(recipe.Ingredients))
            return false;

        _activeRecipe = recipe;
        _activeInventory = inventory;
        _cookRoutine = StartCoroutine(CookRoutine());
        return true;
    }

    public void DebugCook(Recipe recipe, IInventory inv = null)
    {
        if (_busy)
            return;
        _activeRecipe = recipe;
        _activeInventory = inv;
        _cookRoutine = StartCoroutine(CookRoutine());
    }

    private IEnumerator CookRoutine()
    {
        _busy = true;
        float elapsed = 0f;
        OnCookingStarted?.Invoke(this, _activeRecipe);

        while (elapsed < _cookDuration)
        {
            if (!IsUserStillValid())
            {
                CancelCooking(internalCancel: true);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        var mgr = DropManager.GetOrCreate();
        mgr.StartDropSequence(_activeRecipe.ResultItem, _activeRecipe.ResultQuantity, transform.position);

        OnCookingCompleted?.Invoke(this, _activeRecipe);
        ClearState();
    }

    private bool IsUserStillValid()
    {
        if (!_currentUser) return !_cancelOnPlayerDisabled;
        float dSqr = (transform.position - _currentUser.position).sqrMagnitude;
        return dSqr <= _cancelDistance * _cancelDistance;
    }

    public void CancelCooking(bool internalCancel = false)
    {
        if (!_busy)
            return;

        if (_cookRoutine != null)
            StopCoroutine(_cookRoutine);

        if (_refundOnCancel && _activeRecipe && _activeInventory != null)
        {
            foreach (var ing in _activeRecipe.Ingredients)
                _activeInventory.AddItem(ing.Item, ing.Quantity);
        }

        OnCookingCanceled?.Invoke(this, _activeRecipe);

        if (_autoReleaseIfInterrupted || internalCancel)
            ClearState();
    }

    private void ClearState()
    {
        _cookRoutine = null;
        _currentUser = null;
        _activeRecipe = null;
        _activeInventory = null;
        _busy = false;
    }

    private void OnDisable()
    {
        if (_busy)
            CancelCooking();
    }
}
