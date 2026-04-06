using UnityEngine;

public class TankShell : MonoBehaviour
{
    [Header("Shell Settings")]
    public float speed = 200f;
    public float lifetime = 3f;
    public float damage = 50f;
    public bool useGravity = false;

    [Header("Effects")]
    public GameObject explosionPrefab;

    private TankyAgent firingAgent;
    private Rigidbody rb;
    private bool hasHit = false;

    /// <summary>
    /// Called immediately after instantiation to configure and launch the shell.
    /// </summary>
    public void Initialize(TankyAgent agent, float shellSpeed, float shellDamage)
    {
        firingAgent = agent;
        speed = shellSpeed;
        damage = shellDamage;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = useGravity;
        rb.linearVelocity = transform.forward * speed;

        // Ignore collision with the tank that fired this shell so it doesn't
        // immediately clip its own colliders at the barrel tip.
        Collider shellCollider = GetComponent<Collider>();
        if (shellCollider != null)
        {
            foreach (Collider tankCollider in agent.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(shellCollider, tankCollider);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        if (explosionPrefab != null)
        {
            ContactPoint contact = collision.GetContact(0);
            Instantiate(explosionPrefab, contact.point, Quaternion.LookRotation(contact.normal));
        }

        if (collision.collider.CompareTag("Tank"))
        {
            TankHealth enemyHealth = collision.collider.GetComponent<TankHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);

                if (firingAgent != null)
                    firingAgent.AddReward(0.5f); // Reward for hitting an enemy
            }
        }
        else
        {
            if (firingAgent != null)
                firingAgent.AddReward(-0.05f); // Penalty for hitting an obstacle instead of a tank
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Penalise the firer if the shell expired without hitting anything at all.
        if (!hasHit && firingAgent != null)
            firingAgent.AddReward(-0.05f);
    }
}
