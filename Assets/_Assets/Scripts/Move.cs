using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    private static readonly int _magnitude = Animator.StringToHash("Magnitude");

    public Rigidbody2D rb;
    public Animator animator;

    [SerializeField] private float velocidade = 5f;

    private float _rawHorizontal;
    private float _appliedHorizontal;

    void Awake()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();
        if (!animator)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        bool blocked = UIManager.Instance && UIManager.Instance.IsGameplayBlocked;
        _appliedHorizontal = blocked ? 0f : _rawHorizontal;

        var v = rb.linearVelocity;
        v.x = _appliedHorizontal*velocidade;
        rb.linearVelocity = v;

        float magnitude = Mathf.Abs(v.x);
        animator.SetFloat(_magnitude, magnitude);

        if (_appliedHorizontal > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (_appliedHorizontal < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    public void Movement(InputAction.CallbackContext context)
    {
        _rawHorizontal = context.ReadValue<Vector2>().x;
    }
}
