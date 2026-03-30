using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private RectTransform _heartContainer;
    [SerializeField] private Image _heartSlotPrefab;

    [Header("Heart Sprites")]
    [SerializeField] private Sprite _fullHeartSprite;
    [SerializeField] private Sprite _emptyHeartSprite;

    [Header("Fallback Layout (when no LayoutGroup is used)")]
    [SerializeField, Min(1f)] private float _heartWidth = 32f;
    [SerializeField, Min(0f)] private float _heartSpacing = 6f;

    [Header("Tween Animation")]
    [SerializeField] private bool _animateChanges = true;
    [SerializeField, Min(0.01f)] private float _gainDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float _lossDuration = 0.25f;
    [SerializeField, Min(0.1f)] private float _gainStartScale = 0.65f;
    [SerializeField, Min(0.05f)] private float _lossPunchStrength = 0.3f;
    [SerializeField] private Color _gainFlashColor = new Color(0.85f, 1f, 0.85f, 1f);
    [SerializeField] private Color _lossFlashColor = new Color(1f, 0.6f, 0.6f, 1f);

    private readonly List<Image> _heartSlots = new List<Image>();
    private bool _subscribed;
    private int _lastHealth = -1;

    private void Awake()
    {
        if (!_heartContainer)
            _heartContainer = transform as RectTransform;
    }

    private void OnEnable()
    {
        TryBindPlayerHealth();

        if (_playerHealth)
            RefreshHearts(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
    }

    private void Update()
    {
        if (!_subscribed)
            TryBindPlayerHealth();
    }

    private void OnDisable()
    {
        Unsubscribe();
        KillHeartTweens();
        _lastHealth = -1;
    }

    private void TryBindPlayerHealth()
    {
        if (_subscribed)
            return;

        if (!_playerHealth)
        {
            if (PlayerInstance.Instance)
                _playerHealth = PlayerInstance.Instance.GetComponent<PlayerHealth>();

            if (!_playerHealth)
                _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (!_playerHealth)
            return;

        _playerHealth.HealthChanged += RefreshHearts;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || !_playerHealth)
            return;

        _playerHealth.HealthChanged -= RefreshHearts;
        _subscribed = false;
    }

    private void RefreshHearts(int currentHealth, int maxHealth)
    {
        if (!_heartContainer || !_fullHeartSprite || !_emptyHeartSprite)
            return;

        int safeMax = Mathf.Max(0, maxHealth);
        int safeCurrent = Mathf.Clamp(currentHealth, 0, safeMax);
        int previousHealth = _lastHealth < 0 ? safeCurrent : Mathf.Clamp(_lastHealth, 0, safeMax);

        EnsureHeartSlots(safeMax);

        for (int i = 0; i < _heartSlots.Count; i++)
        {
            _heartSlots[i].sprite = i < safeCurrent ? _fullHeartSprite : _emptyHeartSprite;
            _heartSlots[i].enabled = true;
        }

        ArrangeFallbackLine();

        if (_animateChanges && _lastHealth >= 0 && previousHealth != safeCurrent)
            AnimateChangedHearts(previousHealth, safeCurrent);

        _lastHealth = safeCurrent;
    }

    private void EnsureHeartSlots(int targetCount)
    {
        while (_heartSlots.Count < targetCount)
        {
            _heartSlots.Add(CreateHeartSlot(_heartSlots.Count));
        }

        while (_heartSlots.Count > targetCount)
        {
            int lastIndex = _heartSlots.Count - 1;
            Image slot = _heartSlots[lastIndex];

            if (slot)
                Destroy(slot.gameObject);

            _heartSlots.RemoveAt(lastIndex);
        }
    }

    private Image CreateHeartSlot(int index)
    {
        Image heart = Instantiate(_heartSlotPrefab, _heartContainer);
        heart.name = "Heart " + (index + 1);
        heart.enabled = true;
        heart.color = Color.white;
        heart.rectTransform.localScale = Vector3.one;
        return heart;
    }

    private void ArrangeFallbackLine()
    {
        if (_heartSlots.Count == 0)
            return;

        if (_heartContainer.GetComponent<HorizontalOrVerticalLayoutGroup>())
            return;

        for (int i = 0; i < _heartSlots.Count; i++)
        {
            RectTransform slotRect = _heartSlots[i].rectTransform;
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(i * (_heartWidth + _heartSpacing), 0f);
            slotRect.sizeDelta = new Vector2(_heartWidth, _heartWidth);
        }
    }

    private void AnimateChangedHearts(int previousHealth, int currentHealth)
    {
        int from = Mathf.Min(previousHealth, currentHealth);
        int to = Mathf.Max(previousHealth, currentHealth);
        bool gainedHealth = currentHealth > previousHealth;

        for (int i = from; i < to; i++)
        {
            if (i < 0 || i >= _heartSlots.Count)
                continue;

            Image heart = _heartSlots[i];
            if (!heart)
                continue;

            DOTween.Kill(heart.rectTransform);
            DOTween.Kill(heart);

            heart.color = Color.white;

            if (gainedHealth)
            {
                heart.rectTransform.localScale = Vector3.one * _gainStartScale;
                Sequence gainSequence = DOTween.Sequence();
                gainSequence.Append(heart.rectTransform.DOScale(1f, _gainDuration).SetEase(Ease.OutBack));
                gainSequence.Join(heart.DOColor(_gainFlashColor, _gainDuration * 0.5f));
                gainSequence.Append(heart.DOColor(Color.white, _gainDuration * 0.5f));
                gainSequence.SetLink(heart.gameObject, LinkBehaviour.KillOnDisable);
            }
            else
            {
                Sequence lossSequence = DOTween.Sequence();
                lossSequence.Append(heart.DOColor(_lossFlashColor, _lossDuration * 0.35f));
                lossSequence.Append(heart.rectTransform.DOShakeRotation(_lossDuration * 0.5f, new Vector3(0f, 0f, 15f), 10, 90f));
                lossSequence.Join(heart.rectTransform.DOPunchScale(Vector3.one * _lossPunchStrength, _lossDuration, 8, 0.8f));
                lossSequence.Append(heart.DOColor(Color.white, _lossDuration * 0.65f));
                lossSequence.SetLink(heart.gameObject, LinkBehaviour.KillOnDisable);
            }
        }
    }

    private void KillHeartTweens()
    {
        for (int i = 0; i < _heartSlots.Count; i++)
        {
            Image heart = _heartSlots[i];
            if (!heart)
                continue;

            DOTween.Kill(heart.rectTransform);
            DOTween.Kill(heart);
            heart.rectTransform.localScale = Vector3.one;
            heart.color = Color.white;
        }
    }
}
