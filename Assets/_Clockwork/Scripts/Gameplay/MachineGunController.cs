// MachineGunController.cs
// Metralhadora automática — filho da MainTower, começa desativado.
// Ativada pelo SetupPanelUI quando o jogador aloca no slot.
//
// Características:
//   - Raio limitado (menor que a torre)
//   - Não atrai inimigos
//   - Rotaciona em direção ao inimigo mais próximo no raio
//   - Causa slow no inimigo ao acertar
//   - Dano menor que o jogador e a subtorre
//   - Posição fixa definida no SetupPanelUI

using UnityEngine;

public class MachineGunController : MonoBehaviour
{
    [Header("Stats base — sobrescritos por upgrades da Página 2")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float fireRate        = 3f;   // tiros por segundo
    [SerializeField] private int   damage          = 1;    // fixo por enquanto
    [SerializeField] private float slowAmount      = 0.5f; // reduz velocidade em 50%
    [SerializeField] private float slowDuration    = 1f;   // segundos do slow

    [Header("Referências")]
    [SerializeField] private Transform  gunPivot;          // sprite que rotaciona
    [SerializeField] private Transform  firePoint;         // origem do projétil
    [SerializeField] private Projectile projectilePrefab;

    private float fireTimer = 0f;
    private Enemy currentTarget;
    private float targetRefreshTimer = 0f;
    private const float TARGET_REFRESH = 0.3f;

    private void OnEnable()
    {
        fireTimer          = 1f / fireRate;
        targetRefreshTimer = 0f;
    }

    private void Update()
    {
        if (RunManager.Instance == null ||
            RunManager.Instance.CurrentState != RunManager.RunState.Running) return;

        HandleTargeting();
        HandleAiming();
        HandleFiring();
    }

    // ------------------------------------------------------------------
    // Targeting — busca inimigo mais próximo no raio
    // ------------------------------------------------------------------
    private void HandleTargeting()
    {
        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer > 0f) return;
        targetRefreshTimer = TARGET_REFRESH;

        currentTarget = null;
        float minDist = detectionRadius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (Collider2D hit in hits)
        {
            Enemy e = hit.GetComponent<Enemy>();
            if (e == null) continue;

            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist       = dist;
                currentTarget = e;
            }
        }
    }

    // ------------------------------------------------------------------
    // Mira — pivô rotaciona para o alvo
    // ------------------------------------------------------------------
    private void HandleAiming()
    {
        if (currentTarget == null || gunPivot == null) return;

        Vector3 dir   = (currentTarget.transform.position - gunPivot.position).normalized;
        float   angle = UtilsClass.GetAngleFromVector(dir) - 90f;
        gunPivot.eulerAngles = new Vector3(0f, 0f, angle);
    }

    // ------------------------------------------------------------------
    // Disparo automático
    // ------------------------------------------------------------------
    private void HandleFiring()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f || currentTarget == null || firePoint == null) return;

        fireTimer = 1f / fireRate;

        Projectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.SetTarget(currentTarget);
        proj.SetDamage(damage);

        // Aplica slow via componente SlowEffect no inimigo
        ApplySlow(currentTarget);
    }

    private void ApplySlow(Enemy enemy)
    {
        if (enemy == null) return;
        SlowEffect slow = enemy.GetComponent<SlowEffect>();
        if (slow == null)
            slow = enemy.gameObject.AddComponent<SlowEffect>();
        slow.Apply(slowAmount, slowDuration);
    }

    // ------------------------------------------------------------------
    // Gizmo — raio de detecção visível no Scene View
    // ------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.5f, 0.9f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    // ------------------------------------------------------------------
    // API pública — upgrades da Página 2 chamam esses
    // ------------------------------------------------------------------
    public void SetDamage(int d)          => damage          = d;
    public void SetFireRate(float r)      => fireRate        = r;
    public void SetDetectionRadius(float r) => detectionRadius = r;
    public void SetSlowAmount(float s)    => slowAmount      = s;
    public void SetSlowDuration(float d)  => slowDuration    = d;
}
