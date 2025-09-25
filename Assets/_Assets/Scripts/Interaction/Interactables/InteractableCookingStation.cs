using System.Collections;
using _Assets.Scripts.Drops;
using UnityEngine;

public class InteractableCookingStation : InteractableBase
{
    [Header("Cooking")]
    [SerializeField] private float _cookDuration = 5f;
    [SerializeField] private bool _autoReleaseIfInterrupted = true;

    [Header("Cancellation")]
    [SerializeField] private float _cancelDistance = 2.5f;
    [SerializeField] private bool _cancelOnPlayerDisabled = true;

    private bool _busy;
    private Coroutine _cookRoutine;
    private Transform _currentUser;

    [SerializeField] private Recipe _selectedRecipe;

    public override bool CanInteract(in InteractionContext context)
    {
        return !_busy && IsEnabled;
    }

    public override void Interact(in InteractionContext context)
    {
        if (_busy)
            return;

        _currentUser = context.Initiator ? context.Initiator.transform : null;
        _cookRoutine = StartCoroutine(CookRoutine());
    }

    private void RecipeSelected(Recipe recipe)
    {
        _selectedRecipe = recipe;
        StartCoroutine(CookRoutine());
    }

    public void DebugCook()
    {
        StartCoroutine(CookRoutine());
    }

    private IEnumerator CookRoutine()
    {
        _busy = true;
        float elapsed = 0f;
        Debug.Log("Cooking started.");

        while (elapsed < _cookDuration)
        {
            if (!IsUserStillValid())
            {
                Debug.Log("Cooking canceled: player walked away.");
                CancelCooking();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log("Cooking finished.");

        var mgr = DropManager.GetOrCreate();

        mgr.StartDropSequence(_selectedRecipe.ResultItem,_selectedRecipe.ResultQuantity, transform.position);

        _busy = false;
        _cookRoutine = null;
        _currentUser = null;
        _selectedRecipe = null;
    }

    private bool IsUserStillValid()
    {
        if (!_currentUser) return !_cancelOnPlayerDisabled;
        float dSqr = (transform.position - _currentUser.position).sqrMagnitude;
        return dSqr <= _cancelDistance * _cancelDistance;
    }

    public void CancelCooking()
    {
        if (!_busy) return;
        if (_cookRoutine != null)
            StopCoroutine(_cookRoutine);

        _cookRoutine = null;
        _currentUser = null;

        if (_autoReleaseIfInterrupted)
            _busy = false;
    }

    private void OnDisable()
    {
        if (_busy)
            CancelCooking();
    }
}
