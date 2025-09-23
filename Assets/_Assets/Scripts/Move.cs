using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator animator;
    [SerializeField] private float velocidade = 5f;

    private float horizontalMove;

    void Awake()
    {
       
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
      
        rb.linearVelocity = new Vector2(horizontalMove * velocidade, rb.linearVelocity.y);

        
        float magnitude = Mathf.Abs(rb.linearVelocity.x); 
        animator.SetFloat("Magnitude", magnitude);

       
        if (horizontalMove > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1); 
        }
        else if (horizontalMove < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    public void Movement(InputAction.CallbackContext context)
    {
        horizontalMove = context.ReadValue<Vector2>().x;
    }
}

