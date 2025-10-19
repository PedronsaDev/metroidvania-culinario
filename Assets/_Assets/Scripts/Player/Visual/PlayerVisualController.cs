using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPowerController _powerController;
    [SerializeField] private SpriteRenderer _mainSpriteRenderer;
    [SerializeField] private Animator _animator;

    [Header("Default Appearance")]
    [SerializeField] private RuntimeAnimatorController _defaultAnimator;
    [SerializeField] private Color _defaultColor = Color.white;

    [Header("Visual Effects")]
    [SerializeField] private GameObject[] _defaultVfx;

    private readonly Dictionary<PowerDefinition, PowerVisualData> _powerVisuals = new Dictionary<PowerDefinition, PowerVisualData>();

    private void Reset()
    {
        if (!_powerController) _powerController = GetComponent<PlayerPowerController>();
        if (!_mainSpriteRenderer) _mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (!_powerController) _powerController = GetComponent<PlayerPowerController>();
        if (!_mainSpriteRenderer) _mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();

        if (_mainSpriteRenderer)
        {
            if (_defaultColor == Color.clear)
                _defaultColor = _mainSpriteRenderer.color;
        }

        if (_animator && !_defaultAnimator)
            _defaultAnimator = _animator.runtimeAnimatorController;
    }

    private void OnEnable()
    {
        if (_powerController)
        {
            _powerController.PowerEquipped += OnPowerEquipped;
            _powerController.PowerUnequipped += OnPowerUnequipped;
        }
    }

    private void OnDisable()
    {
        if (_powerController)
        {
            _powerController.PowerEquipped -= OnPowerEquipped;
            _powerController.PowerUnequipped -= OnPowerUnequipped;
        }
    }

    private void OnPowerEquipped(PowerDefinition power)
    {
        if (power)
            ApplyVisuals(power);
    }

    private void OnPowerUnequipped(PowerDefinition power) => ResetToDefaultVisuals();

    public void ApplyVisuals(PowerDefinition visualDef)
    {
        if (visualDef.AnimatorController && _animator) _animator.runtimeAnimatorController = visualDef.AnimatorController;

        if (!visualDef.ColorTint.Equals(Color.clear) && _mainSpriteRenderer) _mainSpriteRenderer.color = visualDef.ColorTint;

        foreach (var vfx in _defaultVfx)
            if (vfx) vfx.SetActive(false);

        foreach (var vfx in visualDef.PowerVFX)
        {
            if (vfx)
            {
                GameObject vfxInstance = Instantiate(vfx, transform);
                PowerVisualData data = new PowerVisualData
                {
                    SpawnedVfx = new List<GameObject> { vfxInstance }
                };
                _powerVisuals[visualDef] = data;
            }
        }
    }

    public void ResetToDefaultVisuals()
    {
        if (_animator && _defaultAnimator) _animator.runtimeAnimatorController = _defaultAnimator;

        if (_mainSpriteRenderer) _mainSpriteRenderer.color = _defaultColor;

        foreach (var data in _powerVisuals.Values)
        {
            foreach (var vfx in data.SpawnedVfx)
            {
                if (vfx) Destroy(vfx);
            }
        }
        _powerVisuals.Clear();

        foreach (var vfx in _defaultVfx)
            if (vfx) vfx.SetActive(true);
    }

    private class PowerVisualData
    {
        public List<GameObject> SpawnedVfx = new List<GameObject>();
    }
}
