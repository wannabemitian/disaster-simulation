using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float fireDamagePerSecond = 20f;
    [Header("UI References")]
    public UnityEngine.UI.Slider healthBar;
    public UnityEngine.UI.Image fillImage;
    public GameObject retryScreen;
    public TMP_Text retryText;
    [Header("Visual Effects")]
    public float lerpSpeed = 10f;
    public bool IsDead { get; private set; }
    private bool restarting, pausedByDeath;
    private float previousTimeScale;
    private bool previousAudioPause, previousCursorVisible;
    private CursorLockMode previousCursorLock;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.interactable = false;
            if (fillImage == null && healthBar.fillRect != null)
                fillImage = healthBar.fillRect.GetComponent<UnityEngine.UI.Image>();
        }
        HealthGameOverUI.Ensure(this);
        retryScreen.SetActive(false);
        UpdateHealthDisplay(true);
    }

    void Update()
    {
        UpdateHealthDisplay(IsDead);
        if (!IsDead) return;
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            RetrySimulation();
#else
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)) RetrySimulation();
#endif
    }

    private void UpdateHealthDisplay(bool immediate)
    {
        if (healthBar != null)
            healthBar.value = immediate ? currentHealth : Mathf.Lerp(healthBar.value, currentHealth,
                1f - Mathf.Exp(-Mathf.Max(0f, lerpSpeed) * Time.unscaledDeltaTime));
        if (fillImage != null)
        {
            float fraction = Mathf.Clamp01(currentHealth / maxHealth);
            fillImage.color = fraction > 0.5f
                ? Color.Lerp(Color.yellow, Color.green, (fraction - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, fraction * 2f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f || float.IsNaN(amount)) return;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        pausedByDeath = true;
        UpdateHealthDisplay(true);
        retryScreen.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[PlayerHealth] Game over: simulation paused, restart available.", this);
    }

    public void RetrySimulation()
    {
        if (!IsDead || restarting) return;
        restarting = true;
        var scene = gameObject.scene;
        RestorePauseState();
        Debug.Log("[PlayerHealth] Restarting " + scene.path, this);
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
            new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
#endif
    }

    private void RestorePauseState()
    {
        if (!pausedByDeath) return;
        pausedByDeath = false;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    void OnDestroy() { RestorePauseState(); }

    void OnTriggerStay(Collider other)
    {
        // Avoid charging twice when grid simulation already supplies damage.
        if (GetComponent<FireSimulation.HazardExposureAndVFXBridge>() != null) return;
        if (other.gameObject.layer == 10) TakeDamage(fireDamagePerSecond * Time.deltaTime);
    }
}
