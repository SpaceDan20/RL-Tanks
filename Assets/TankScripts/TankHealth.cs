using UnityEngine;

public class TankHealth : MonoBehaviour
{
    private TankyAgent tankyAgent; // Reference to the TankAgent script

    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    private void Awake()
    {
        tankyAgent = GetComponent<TankyAgent>(); // Get the TankAgent component
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            // Handle tank destruction here (e.g., play explosion, disable tank, etc.)
            tankyAgent.OnDestroyed(); // Notify the TankyAgent of the destruction
        }
    }
}
