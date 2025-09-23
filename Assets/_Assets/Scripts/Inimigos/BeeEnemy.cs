using UnityEngine;

public class BeeEnemy : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 2f;
    public float amplitude = 1f;
    public float frequency = 2f;
    public float moveRange = 5f;

    [Header("Ataque")]
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public GameObject lancaHitbox; 

    private Vector2 startPos;
    private bool movingRight = true;
    private Animator animator;
    private Transform player;
    private float lastAttackTime;

    void Start()
    {
        startPos = transform.position;
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (lancaHitbox != null)
            lancaHitbox.SetActive(false); 
    }

    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
        else
        {
            Move();
        }
    }

    void Move()
    {
        float x = transform.position.x + (movingRight ? 1 : -1) * speed * Time.deltaTime;
        float y = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;

        transform.position = new Vector2(x, y);

        if (movingRight && x >= startPos.x + moveRange)
        {
            movingRight = false;
            Flip();
        }
        else if (!movingRight && x <= startPos.x - moveRange)
        {
            movingRight = true;
            Flip();
        }

        animator.SetBool("Voando", true);
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger("Atacar"); 
    }

   
    public void EnableLancaHitbox()
    {
        if (lancaHitbox != null)
            lancaHitbox.SetActive(true);
    }

    public void DisableLancaHitbox()
    {
        if (lancaHitbox != null)
            lancaHitbox.SetActive(false);
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
