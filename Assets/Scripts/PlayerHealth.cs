using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Can Ayalarý")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private string deathName = "Die";

    public NetworkVariable<int> currentHealth= new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Animator animator;
    private bool isdead=false;
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        UpdateHealthUI(currentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int previousValue ,int newValue)
    {
        UpdateHealthUI(newValue);
    }
    private void UpdateHealthUI(int health)
    {
        if(healthSlider != null)
        {
            healthSlider.maxValue=maxHealth;
            healthSlider.value=health;
        }
    }

    public void TakeDamge(int damage)
    {
        if (!IsServer || isdead) return;

        currentHealth.Value -= damage;

        if( currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }
    public void Die()
    {
        isdead= true;
        if(animator != null && string.IsNullOrEmpty(deathName))
        {
            animator.SetTrigger(deathName);
        }
        if (TryGetComponent<PlayerMovement>(out var movement)) movement.enabled = false;
        if (TryGetComponent<PlayerShooting>(out var shooting)) shooting.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        Debug.Log($"Oyuncu ({OwnerClientId}) öldü!");
    }
}
