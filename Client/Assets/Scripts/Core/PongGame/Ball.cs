using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody2D rb;
    private bool isDragging = false;
    private Camera mainCamera;
    private Collider2D ballCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        ballCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        Vector2 mousePosition = GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.OverlapPoint(mousePosition);
            if (hit != null && hit == ballCollider)
            {
                StartDragging();
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDragging();
        }

        if (isDragging)
        {
            transform.position = new Vector3(mousePosition.x, mousePosition.y, 0f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();
        }
    }

    Vector2 GetMouseWorldPosition()
    {
        return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    void StartDragging()
    {
        isDragging = true;
        rb.linearVelocity = Vector2.zero;  // Changed from velocity
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void EndDragging()
    {
        isDragging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        LaunchBall();
    }

    void LaunchBall()
    {
        float randomAngle = Random.Range(-45f, 45f);
        Vector2 direction = Quaternion.Euler(0, 0, randomAngle) * Vector2.right;
        
        if (Random.value > 0.5f) 
            direction.x *= -1;
        
        rb.linearVelocity = direction.normalized * speed;  // Changed from velocity
    }

    void ResetBall()
    {
        transform.position = Vector3.zero;
        LaunchBall();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDragging)
        {
            float randomness = Random.Range(-0.2f, 0.2f);
            Vector2 newVelocity = rb.linearVelocity + new Vector2(randomness, randomness);  // Changed from velocity
            rb.linearVelocity = newVelocity.normalized * speed;  // Changed from velocity
        }
    }
}
