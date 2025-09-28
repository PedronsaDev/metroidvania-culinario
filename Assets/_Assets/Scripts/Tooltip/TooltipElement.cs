using TMPro;
using UnityEngine;

public class TooltipElement : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _valueText;


    public void SetElementInfo(TooltipElementInfo info)
    {
        info.Name += ":";
        _nameText.SetText(info.Name);
        _valueText.SetText(info.Value);
    }
}
