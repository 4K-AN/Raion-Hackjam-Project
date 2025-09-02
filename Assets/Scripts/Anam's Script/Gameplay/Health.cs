using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 3;
    public int currentHP;
    public event System.Action<int> OnHealthChanged;
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP == 0 ? maxHP : currentHP, 0, maxHP);
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0) return;
        int prev = currentHP;
        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        if (currentHP != prev)
        {
            OnHealthChanged?.Invoke(currentHP);
            if (currentHP == 0) OnDeath?.Invoke();
        }
    }

    public void Heal(int amount = 1) { /* sama pola */ }
    public void SetHealth(int hp) { /* sama pola */ }
}
