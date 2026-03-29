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
        tankyAgent.AddReward(-0.25f); // Penalize the agent for taking damage
        currentHealth -= damageAmount;
        if (currentHealth <= 0.01f)
        {
            currentHealth = 0f;
            // Handle tank destruction here (e.g., play explosion, disable tank, etc.)
            tankyAgent.OnDestroyed(); // Notify the TankyAgent of the destruction
        }
    }
}
