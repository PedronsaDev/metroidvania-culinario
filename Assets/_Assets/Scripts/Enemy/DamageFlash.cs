using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    private static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashTime = 0.25f;
    [SerializeField] private Ease _flashSpeedCurve;

    private SpriteRenderer[] _spriteRenderers;
    private Material[] _materials;

    private Coroutine _damageFlashCoroutine;

    private Sequence _damageFlashSequence;

    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        Init();
    }

    private void Init()
    {
        _materials = new Material[_spriteRenderers.Length];

        for (int i = 0; i < _spriteRenderers.Length; i++)
            _materials[i] = _spriteRenderers[i].material;
    }

    public void Flash()
    {
        _damageFlashSequence?.Kill();

        if (_materials == null || _materials.Length == 0)
            Init();

        SetFlashColor();
        SetFlashAmount(1f);

        _damageFlashSequence = DOTween.Sequence();
        if (_materials == null)
            return;

        foreach (Material material in _materials)
        {
            _damageFlashSequence.Join(
                material.DOFloat(0f, "_FlashAmount", _flashTime)
                    .SetEase(_flashSpeedCurve)
            );
        }
    }

    private void SetFlashColor()
    {
        foreach (Material material in _materials)
            material.SetColor(FlashColor, _flashColor);
    }

    private void SetFlashAmount(float amount)
    {
        for (int i = 0; i < _materials.Length; i++)
        {
            _materials[i].SetFloat(FlashAmount, amount);
        }
    }
}
