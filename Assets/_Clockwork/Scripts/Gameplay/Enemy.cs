// Enemy.cs  — adaptado do projeto anterior
//
// Mudanças em relação ao original:
//   1. Alvo vem de RunManager.GetNearestTower() (não BuildingManager)
//   2. OnCollisionEnter2D acessa Tower diretamente (não Building)
//   3. HealthSystem_OnDied notifica RunManager.OnEnemyKilled()
//   4. Stats configuráveis pelo WaveManager via SetStats()
//   5. Removido: CinemachineShake, ChromaticAberrationEffect (opcionais)
//   6. SoundManager mantido — adaptar enum Sound para sons do Clockwork

using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Stats — definidos pelo WaveManager antes do Instantiate
    // ------------------------------------------------------------------
    [SerializeField] protected int   maxHP             = 10;
    [SerializeField] protected float moveSpeed         = 4f;
    [SerializeField] protected int   damageOnCollision = 1;
    [SerializeField] protected int   scrapDrop         = 3;

    // ------------------------------------------------------------------
    // Interno
    // ------------------------------------------------------------------
    protected Transform    targetTransform;
    private   Rigidbody2D  rb2D;
    protected HealthSystem healthSystem;

    private float lookForTargetTimer;
    private const float LOOK_TIMER_MAX = 0.2f;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    protected virtual void Start()
    {
        rb2D         = GetComponent<Rigidbody2D>();
        healthSystem = GetComponent<HealthSystem>();

        healthSystem.SetHealthAmountMax(maxHP, updateHealthAmount: true);
        healthSystem.OnDamaged += HealthSystem_OnDamaged;
        healthSystem.OnDied    += HealthSystem_OnDied;

        // Alvo inicial = torre principal
        Tower main = RunManager.Instance.GetMainTower();
        if (main != null) targetTransform = main.transform;

        // Stagger para evitar que todos os inimigos refreshem no mesmo frame
        lookForTargetTimer = Random.Range(0f, LOOK_TIMER_MAX);
    }

    protected virtual void Update()
    {
        HandleMovement();
        HandleTargeting();
    }

    // ------------------------------------------------------------------
    // Movimento — idêntico ao original, só muda a fonte do alvo
    // ------------------------------------------------------------------
    private void HandleMovement()
    {
        if (targetTransform == null) return;

        Vector3 dir = (targetTransform.position - transform.position).normalized;
        rb2D.linearVelocity = dir * moveSpeed;

        // Rotaciona o sprite na direção do movimento (opcional)
        if (dir != Vector3.zero)
        {
            float angle = UtilsClass.GetAngleFromVector(dir);
            transform.eulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    // ------------------------------------------------------------------
    // Targeting — igual ao original
    // ------------------------------------------------------------------
    private void HandleTargeting()
    {
        lookForTargetTimer -= Time.deltaTime;
        if (lookForTargetTimer < 0f)
        {
            lookForTargetTimer += LOOK_TIMER_MAX;
            RefreshTarget();
        }
    }

    private void RefreshTarget()
    {
        Tower nearest = RunManager.Instance.GetNearestTower(transform.position);
        if (nearest != null)
            targetTransform = nearest.transform;
    }

    // ------------------------------------------------------------------
    // Colisão com torre — dano + auto-destruição
    // ------------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Tower tower = collision.gameObject.GetComponent<Tower>();
        if (tower != null)
        {
            tower.GetComponent<HealthSystem>().Damage(damageOnCollision);
            healthSystem.Damage(9999); // suicida após colidir
        }
    }

    // ------------------------------------------------------------------
    // Eventos de HP
    // ------------------------------------------------------------------
    private void HealthSystem_OnDamaged(object sender, System.EventArgs e)
    {
        // SoundManager.Instance.PlaySound(SoundManager.Sound.EnemyHit);
        // Substituir pelo som correto após atualizar o enum
    }

    protected virtual void HealthSystem_OnDied(object sender, System.EventArgs e)
    {
        OnDeathEffect();
        RunManager.Instance.OnEnemyKilled(scrapDrop);
        Destroy(gameObject);
    }

    protected virtual void OnDeathEffect()
    {
        // SoundManager.Instance.PlaySound(SoundManager.Sound.EnemyDie);
        // Instantiate(dieParticlesPrefab, transform.position, Quaternion.identity);
    }

    // ------------------------------------------------------------------
    // API pública — WaveManager configura os stats antes do spawn
    // ------------------------------------------------------------------
    public virtual void SetStats(int hp, float speed, int damage, int drop)
    {
        maxHP             = hp;
        moveSpeed         = speed;
        damageOnCollision = damage;
        scrapDrop         = drop;
    }

    // Acesso para raio de hit (WeaponController usa isso para detectar clique)
    public HealthSystem GetHealthSystem() => healthSystem;
}
