using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InventoryItemEntry : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private Item _item;

    public Item Item => _item;

    public void Bind(Item item, int quantity)
    {
        _item = item;
        if (_nameText) _nameText.text = item ? item.name : "Unknown";
        if (_icon && item && item.Icon) _icon.sprite = item.Icon;
        UpdateQuantity(quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (_quantityText) _quantityText.text = quantity.ToString();
    }
}
