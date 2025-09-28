using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipAction : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _prompt;
    
    public void SetActionInfo(TooltipActionInfo info)
    {
        _nameText.SetText(info.Action);
        _prompt.sprite = info.Prompt;
    }
}
