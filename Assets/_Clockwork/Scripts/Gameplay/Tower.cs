// Tower.cs
// Componente que marca um GameObject como torre (alvo de inimigos).
// Usado pela MainTower e pela SubTower — ambos têm este componente.
//
// RunManager mantém uma lista de todas as Torres para Enemy.GetNearestTower().
// HealthSystem é configurado pelo RunManager.Start() com o valor do upgrade.

using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tipo")]
    [SerializeField] private bool isMainTower = false;

    // Acesso ao HealthSystem — Enemy e Projectile usam isso
    private HealthSystem healthSystem;

    public bool IsMainTower => isMainTower;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    // Chamado por Enemy.OnCollisionEnter2D
    public void TakeDamage(int amount)
    {
        healthSystem?.Damage(amount);
    }

    public HealthSystem GetHealthSystem() => healthSystem;
}
