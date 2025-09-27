using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasToggler : MonoBehaviour
{
    [Header("Canvas Toggler")]
    [SerializeField] private InputActionReference _input;
    [SerializeField] private bool _hasFade = true;
    [SerializeField, ShowIf("_hasFade")] private float _fadeDuration = 0.25f;

    [SerializeField] private bool _startVisible = true;
    private CanvasGroup _canvasGroup;
    private bool IsVisible => _canvasGroup.alpha > 0.5f;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        SetVisibility(_startVisible, false);
    }

    protected virtual void OnEnable()
    {
        if (!_input) return;

        _input.action.performed += OnToggle;
        _input.action.Enable();
    }

    protected virtual void OnDisable()
    {
        if (!_input) return;

        _input.action.performed -= OnToggle;
        _input.action.Disable();
    }

    private void OnToggle(InputAction.CallbackContext context) => Toggle();

    public void SetVisibility(bool visible, bool animate)
    {
        if (IsVisible == visible)
            return;

        if (_hasFade && animate)
        {
            float targetAlpha = visible ? 1f : 0f;

            DOTween.Kill(this);

            _canvasGroup.DOFade(targetAlpha, _fadeDuration).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                if (!visible)
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
            });

            if (visible)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }

    public virtual void Show() => SetVisibility(true, true);
    public virtual void Hide() => SetVisibility(false, true);

    public virtual void Toggle() => SetVisibility(!IsVisible, true);
}