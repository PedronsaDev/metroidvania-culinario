using DG.Tweening;
using UnityEngine;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    [SerializeField] private Tooltip _tooltip;
    private bool _canShow;

    private void Awake()
    {
        if (Instance)
        {
            Debug.LogWarning("More than one instance of the Tooltip System. Destroying the new instance");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Show(TooltipInfo info)
    {
        _tooltip.gameObject.SetActive(true);
        _tooltip.SetInfo(info);
        _tooltip.CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutExpo);

    }

    public void Hide() => _tooltip.CanvasGroup.DOFade(0f, 0.3f).SetEase(Ease.OutExpo).OnComplete(() =>
    {
        _tooltip.gameObject.SetActive(false);
    });

}