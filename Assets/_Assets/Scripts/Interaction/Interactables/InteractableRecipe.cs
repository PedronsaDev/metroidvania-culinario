using DG.Tweening;
using UnityEngine;

public class InteractableRecipe : InteractableBase
{
    [SerializeField] private Recipe _recipe;
    //[SerializeField] private AudioClip _unlockSound;

    [Header("Hover Animation")]
    [SerializeField] private float _hoverAmplitude = 0.25f;
    [SerializeField] private float _hoverDuration = 1.5f;
    [SerializeField] private float _rotationSpeed = 40f; // degrees per second
    [SerializeField] private Ease _hoverEase = Ease.InOutSine;

    private Tween _hoverTween;
    private Tween _rotateTween;

    private void Start()
    {
        float baseY = transform.localPosition.y;

        _hoverTween = transform
            .DOLocalMoveY(baseY + _hoverAmplitude, _hoverDuration)
            .SetEase(_hoverEase)
            .SetLoops(-1, LoopType.Yoyo);

        if (_rotationSpeed > 0f)
        {
            float rotationDuration = 360f / _rotationSpeed;
            _rotateTween = transform
                .DORotate(new Vector3(0f, 360f, 0f), rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    private void OnDisable()
    {
        _hoverTween?.Kill();
        _rotateTween?.Kill();
    }

    public override bool CanInteract(in InteractionContext context) => !RecipesManager.IsUnlocked(_recipe);

    public override void Interact(in InteractionContext context)
    {
        if (RecipesManager.Unlock(_recipe))
        {
            AudioManager.Instance.PlaySFX("recipe_unlock");
            Destroy(this.gameObject);
        }

    }
}