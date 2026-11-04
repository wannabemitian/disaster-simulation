using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthBar;
    public float fireDamagePerSecond = 20f;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (healthBar != null) healthBar.value = currentHealth;

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died!");
            // Phase 2: Add game over or respawn logic here
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Index 10 is your Hazard_Fire layer
        if (other.gameObject.layer == 10)
        {
            TakeDamage(fireDamagePerSecond * Time.deltaTime);
        }
    }
}
