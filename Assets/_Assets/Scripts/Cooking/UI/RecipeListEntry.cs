using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class RecipeListEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _ingredientsText;
    [SerializeField] private Button _button;
    [SerializeField] private Image _availabilityIndicator;
    [SerializeField] private Color _canColor = Color.green;
    [SerializeField] private Color _cantColor = Color.red;

    private Recipe _recipe;
    private System.Action<Recipe> _onClick;

    public void Bind(Recipe recipe, IInventory inventory, System.Action<Recipe> onClick)
    {
        _recipe = recipe;
        _onClick = onClick;
        _nameText.text = recipe.Name;

        System.Text.StringBuilder sb = new();
        foreach (var ing in recipe.Ingredients)
            sb.AppendLine($"{ing.Item.name} x{ing.Quantity}");
        _ingredientsText.text = sb.ToString().TrimEnd();

        bool can = recipe.CanCook(inventory);
        if (_availabilityIndicator)
            _availabilityIndicator.color = can ? _canColor : _cantColor;

        _button.interactable = can;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => _onClick?.Invoke(_recipe));
    }
}