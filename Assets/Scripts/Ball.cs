using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    Vector2 direction;
    const float StartSpeed = 3f;
    const float MaxSpeed = 15f;
    const float AdditionalSpeedPerHit = 0.2f;
    float currentSpeed = StartSpeed;

    public AudioSource audioSource;
    public AudioClip clip;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        direction = (Vector2.left + Random.insideUnitCircle).normalized;
    }

    private void FixedUpdate()
    {
        if (!IsServer || !GameManager.Instance.IsGameActive)
        {
            return;
        }

        var distance = currentSpeed * Time.deltaTime;

        var hit = Physics2D.Raycast(transform.position, direction, distance);

        if (hit.collider == null)
        {
            transform.position += (Vector3)(direction * distance);
        }
        else if (hit.collider.CompareTag("ScoringZone"))
        {
            if (hit.point.x < 0f)
            {
                GameManager.Instance.AddScore(1, 1);

                direction = (Vector2.left + Random.insideUnitCircle).normalized;
            }
            else
            {
                GameManager.Instance.AddScore(0, 1);
                direction = (Vector2.right + Random.insideUnitCircle).normalized;
            }
            transform.position = new Vector3(0f, Random.Range(-3f, 3f), 0f);
            currentSpeed = StartSpeed;
        }
        else
        {
            transform.position = hit.point;

            distance -= hit.distance;

            if (hit.collider.CompareTag("Paddle"))
            {
                audioSource.PlayOneShot(clip);
            }

            direction = Vector2.Reflect(direction, hit.normal);
            direction = (direction + Random.insideUnitCircle * 0.05f).normalized;

            transform.position += (Vector3)direction * distance;

            currentSpeed = Mathf.Min(currentSpeed + AdditionalSpeedPerHit, MaxSpeed);
        }
    }
}

