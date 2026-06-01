using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    [Header("Настройки плавности")]
    [Range(1f, 50f)] public float startSmoothness = 15f; // Скорость разгона
    [Range(1f, 50f)] public float stopSmoothness = 20f;  // Скорость торможения (инерция)

    private Rigidbody rb;
    public bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        // Возвращаем GetAxisRaw для мгновенного отклика на клавиши (убирает ватность)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;

        if (canMove && moveDirection.sqrMagnitude > 0)
        {
            // Игрок жмет клавиши: рассчитываем целевую скорость
            Vector3 targetVelocity = new Vector3(moveDirection.x * speed, rb.velocity.y, moveDirection.z * speed);

            // Плавно разгоняемся до целевой скорости
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, startSmoothness * Time.fixedDeltaTime);
        }
        else
        {
            // Игрок отпустил клавиши или движение запрещено: плавно гасим скорость до 0
            Vector3 targetVelocity = new Vector3(0f, rb.velocity.y, 0f);

            // stopSmoothness отвечает за инерцию (чем меньше значение, тем дольше скользит)
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, stopSmoothness * Time.fixedDeltaTime);
        }
    }
}
