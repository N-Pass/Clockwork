// WeaponController.cs
// Controla a arma montada no topo da MainTower.
//
// Comportamento:
//   - weaponPivot rotaciona para apontar ao mouse continuamente
//   - Clique/hold do botão esquerdo → dispara Projectile
//   - Tenta encontrar o inimigo mais próximo da linha de mira (cone 60°)
//   - Se achar inimigo: projétil tracking. Se não: projétil direcional
//   - Dano lido do GameManager.GetClickDamage() (upgrade)
//   - FireRate pode ser aumentada por upgrade (chamar SetFireRate())

using UnityEngine;

public class WeaponController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Referências")]
    [SerializeField] private Transform     weaponPivot;    // filho que rotaciona (o sprite da arma)
    [SerializeField] private Transform     firePoint;       // ponto de origem do projétil
    [SerializeField] private Projectile    projectilePrefab;

    [Header("Stats base")]
    [SerializeField] private float baseFireRate  = 1.5f;   // disparos por segundo
    [SerializeField] private float aimRange      = 12f;    // raio de busca de alvos
    [SerializeField] private float aimConeAngle  = 60f;    // ângulo do cone de mira (graus)

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    private float fireTimer = 0f;
    private float fireRate;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Start()
    {
        fireRate = baseFireRate;
    }

    private void Update()
    {
        if (RunManager.Instance == null) return;
        if (RunManager.Instance.CurrentState != RunManager.RunState.Running) return;

        HandleAiming();
        HandleFiring();
    }

    // ------------------------------------------------------------------
    // Mira — weaponPivot aponta para o mouse
    // ------------------------------------------------------------------
    private void HandleAiming()
    {
        Vector3 mouseWorld = UtilsClass.GetMouseWorldPosition();
        Vector3 dir        = (mouseWorld - weaponPivot.position).normalized;
        float   angle      = UtilsClass.GetAngleFromVector(dir) - 90f;

        weaponPivot.eulerAngles = new Vector3(0f, 0f, angle);
    }

    // ------------------------------------------------------------------
    // Disparo — segurar botão esquerdo
    // ------------------------------------------------------------------
    private void HandleFiring()
    {
        fireTimer -= Time.deltaTime;

        if (Input.GetMouseButton(0) && fireTimer <= 0f)
        {
            fireTimer = 1f / fireRate;
            Fire();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 aimDir  = (UtilsClass.GetMouseWorldPosition() - firePoint.position).normalized;
        Enemy   target  = FindBestTargetInCone(aimDir);

        Projectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        if (target != null)
            proj.SetTarget(target);
        else
            proj.SetDirection(aimDir);

        // SoundManager.Instance.PlaySound(SoundManager.Sound.WeaponFire);
    }

    // ------------------------------------------------------------------
    // Busca o inimigo mais alinhado com a mira dentro do cone
    // ------------------------------------------------------------------
    private Enemy FindBestTargetInCone(Vector3 aimDir)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aimRange);

        Enemy bestEnemy     = null;
        float bestAlignment = float.MinValue;
        float cosThreshold  = Mathf.Cos(aimConeAngle * 0.5f * Mathf.Deg2Rad);

        foreach (Collider2D col in hits)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector3 toEnemy   = (enemy.transform.position - firePoint.position).normalized;
            float   alignment = Vector3.Dot(aimDir, toEnemy);

            if (alignment >= cosThreshold && alignment > bestAlignment)
            {
                bestAlignment = alignment;
                bestEnemy     = enemy;
            }
        }

        return bestEnemy;
    }

    // ------------------------------------------------------------------
    // API — HUDController chama quando upgrade de fire rate é comprado
    // ------------------------------------------------------------------
    public void SetFireRate(float rate) => fireRate = rate;
    public void ResetToBase()           => fireRate = baseFireRate;
}
