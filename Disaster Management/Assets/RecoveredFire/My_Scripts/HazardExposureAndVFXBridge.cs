using UnityEngine;

namespace FireSimulation
{
    public class HazardExposureAndVFXBridge : MonoBehaviour
    {
        public float playerHealth = 100f;
        public float burnDamageRate = 10f; // Damage per second at max fire temp
        public float smokeChokeRate = 5f;

        [Header("Fog Settings")]
        public Color clearColor = Color.gray;
        public Color smokeColor = new Color(0.1f, 0.1f, 0.1f);
        public float maxFogDensity = 0.15f;

        private FireSimulationManager fireManager;
        private PlayerHealth health;

        private void Start()
        {
            fireManager = FindFirstObjectByType<FireSimulationManager>();
            health = GetComponentInParent<PlayerHealth>();
            if (health != null) playerHealth = health.currentHealth;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        private void Update()
        {
            if (fireManager == null || (health != null && health.IsDead)) return;

            HazardSample sample = fireManager.SampleHazardAtWorldPosition(transform.position);

            float damage = 0f;
            if (sample.temperature > 60f) // Start burning above 60C
            {
                float tempFactor = (sample.temperature - 60f) / (fireManager.simParams.maxFireTemp - 60f);
                damage += Mathf.Clamp01(tempFactor) * burnDamageRate * Time.deltaTime;
            }

            if (sample.smokeLower > 1.0f) // Taking smoke damage
            {
                damage += sample.smokeLower * smokeChokeRate * Time.deltaTime;
            }

            if (health != null)
            {
                health.TakeDamage(damage);
                playerHealth = health.currentHealth;
            }
            else playerHealth = Mathf.Max(0f, playerHealth - damage);

            // 2. Drive Visuals (Unity Global Fog)
            float totalSmokeHere = sample.smokeUpper + sample.smokeLower;
            float targetDensity = Mathf.Lerp(0f, maxFogDensity, totalSmokeHere / 20f);

            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, Time.deltaTime * 2f);
            RenderSettings.fogColor = Color.Lerp(clearColor, smokeColor, totalSmokeHere / 10f);

        }
    }
}
