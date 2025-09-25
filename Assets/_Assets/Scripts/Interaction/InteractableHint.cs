using UnityEngine;
[DisallowMultipleComponent]
public class InteractableHint : MonoBehaviour
{
    [SerializeField] private string _overrideText;
    [SerializeField] private float _verticalOffset = 0.75f;

    public bool HasOverride => !string.IsNullOrWhiteSpace(_overrideText);
    public string OverrideText => _overrideText;
    public float VerticalOffset => _verticalOffset;
}
