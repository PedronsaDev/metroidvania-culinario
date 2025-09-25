using DG.Tweening;
using TMPro;
using UnityEngine;
public class InteractionHintUI : MonoBehaviour
{
    [SerializeField] private PlayerInteractor _interactor;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 20f);
    [SerializeField] private Camera _camera;

    private CanvasGroup _canvasGroup;
    private IInteractable _current;
    private InteractableHint _currentHintMeta;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        if (!_camera)
            _camera = Camera.main;
        Hide();
    }

    private void OnEnable()
    {
        if (_interactor)
            _interactor.HoverChanged += OnHoverChanged;
    }

    private void OnDisable()
    {
        if (_interactor)
            _interactor.HoverChanged -= OnHoverChanged;
    }

    private void LateUpdate()
    {
        if (_current == null) return;
        UpdatePosition();
    }

    private void OnHoverChanged(IInteractable target)
    {
        _current = target;
        if (_current == null)
        {
            Hide();
            return;
        }

        var go = (_current as Component)?.gameObject;
        _currentHintMeta = go ? go.GetComponent<InteractableHint>() : null;

        string text = _currentHintMeta && _currentHintMeta.HasOverride
            ? _currentHintMeta.OverrideText
            : _current.DisplayName;

        _label.text = text;
        Show();
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!_camera) return;
        var comp = _current as Component;
        if (!comp)
        {
            Hide();
            return;
        }

        float extraY = _currentHintMeta ? _currentHintMeta.VerticalOffset : 0.6f;
        Vector3 worldPos = comp.transform.position + Vector3.up * extraY;
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            Hide();
            return;
        }

        _panel.gameObject.SetActive(true);
        _panel.position = screenPos + (Vector3)_screenOffset;
    }

    private void Show()
    {
        DOTween.Kill(this);
        if (_panel)
        {
            _canvasGroup.DOFade(1f, 0.15f);
        }
    }

    private void Hide()
    {
        DOTween.Kill(this);
        if (_panel)
        {
            _canvasGroup.DOFade(0f, 0.15f);
        }
        _currentHintMeta = null;
    }
}
