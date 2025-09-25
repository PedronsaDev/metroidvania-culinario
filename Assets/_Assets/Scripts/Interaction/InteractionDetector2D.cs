using System.Collections.Generic;
using UnityEngine;
public class InteractionDetector2D : MonoBehaviour
{
    [SerializeField] private float _radius = 1.2f;
    [SerializeField] private LayerMask _mask;
    [SerializeField] private int _max = 8;
    private readonly Collider2D[] _hits = new Collider2D[16];

    public int Collect(List<IInteractable> buffer)
    {
        buffer.Clear();

        var filter = new ContactFilter2D();
        filter.SetLayerMask(_mask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int count = Physics2D.OverlapCircle(transform.position, _radius, filter, _hits);
        for (int i = 0; i < count && buffer.Count < _max; i++)
        {
            if (_hits[i].TryGetComponent<IInteractable>(out var interactable))
                buffer.Add(interactable);
        }
        return buffer.Count;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
#endif
}
