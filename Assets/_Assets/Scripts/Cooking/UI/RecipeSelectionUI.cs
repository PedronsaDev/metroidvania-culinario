using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class RecipeSelectionUI : BaseUIWindow
{
    [Header("References")]
    [SerializeField] private Transform _listContainer;
    [SerializeField] private RecipeListEntry _entryPrefab;

    private InteractableCookingStation _currentStation;
    private IInventory _currentInventory;

    protected override void OnEnable()
    {
        base.OnEnable();

        InteractableCookingStation.OnStationInteracted += HandleStationInteracted;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        InteractableCookingStation.OnStationInteracted -= HandleStationInteracted;
    }

    private void HandleStationInteracted(InteractableCookingStation station, Transform user)
    {
        var inv = user ? user.GetComponentInParent<PlayerInventory>() : null;
        if (!inv) return;

        _currentStation = station;
        _currentInventory = inv;
        Show();
        Rebuild();
    }

    public override void Hide()
    {
        base.Hide();
        _currentStation = null;
        _currentInventory = null;
        Clear();
    }

    private void Rebuild()
    {
        Clear();
        var recipes = RecipesManager.UnlockedRecipes.OrderBy(r => r.Name);
        foreach (var recipe in recipes)
        {
            var entry = Instantiate(_entryPrefab, _listContainer);
            entry.Bind(recipe, _currentInventory, OnRecipeClicked);
        }

        EventSystem.current.SetSelectedGameObject(recipes.Any() ? _listContainer.GetChild(0).gameObject : null);
    }

    private void Clear()
    {
        for (int i = _listContainer.childCount - 1; i >= 0; i--)
            Destroy(_listContainer.GetChild(i).gameObject);
    }

    private void OnRecipeClicked(Recipe recipe)
    {
        if (_currentStation.TryStartCooking(recipe, _currentInventory))
            Hide();
    }
}
