using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool _staticInfo;

    [SerializeField, ShowIf("_staticInfo")] private TooltipInfo _info;

    private Tween _call;

    public void OnPointerEnter(PointerEventData eventData) => _call = DOVirtual.DelayedCall(1.3f, () => { TooltipSystem.Instance.Show(_info); });

    public void OnPointerExit(PointerEventData eventData) => KillTooltip();
    public void KillTooltip()
    {
        if (_call == null)
            return;

        _call.Kill();
        TooltipSystem.Instance.Hide();
    }

    public void SetTooltipInfo(TooltipInfo info) => _info = info;
}