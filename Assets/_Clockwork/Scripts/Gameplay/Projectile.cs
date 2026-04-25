// Projectile.cs
// Baseado no ArrowProjectile do projeto anterior.
//
// Diferenças:
//   - Dano vem do GameManager.GetClickDamage() (upgrade)
//   - Modo tracking (persegue inimigo) OU direction (voa em linha reta)
//   - Sem Resources.Load — instanciado pelo WeaponController via prefab serializado

using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 18f;
    [SerializeField] private float lifetime  = 2.5f;

    private Enemy     targetEnemy;
    private Vector3   direction;
    private bool      isTracking = false;
    private int       damage;

    private float lifeTimer;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        lifeTimer  = lifetime;
        damage     = RunManager.Instance != null
            ? RunManager.Instance.GetClickDamage()
            : 1;
        moveSpeed  = RunManager.Instance != null
            ? RunManager.Instance.GetProjectileSpeed()
            : moveSpeed;
    }

    private void Update()
    {
        Vector3 moveDir;

        if (isTracking && targetEnemy != null)
        {
            moveDir   = (targetEnemy.transform.position - transform.position).normalized;
            direction = moveDir; // atualiza para usar como lastDirection se alvo morrer
        }
        else
        {
            moveDir = direction;
        }

        transform.position   += moveDir * moveSpeed * Time.deltaTime;
        transform.eulerAngles = new Vector3(0f, 0f, UtilsClass.GetAngleFromVector(moveDir));

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.GetHealthSystem().Damage(damage);
            Destroy(gameObject);
            return;
        }

        // Ignora colisão com a torre e outros projéteis
    }

    // ------------------------------------------------------------------
    // API — WeaponController chama um dos dois antes de soltar
    // ------------------------------------------------------------------

    // Projétil rastreador — busca o inimigo
    public void SetTarget(Enemy enemy)
    {
        targetEnemy = enemy;
        isTracking  = true;

        if (enemy != null)
            direction = (enemy.transform.position - transform.position).normalized;
    }

    // Projétil direcional — voa em linha reta
    public void SetDirection(Vector3 dir)
    {
        direction  = dir.normalized;
        isTracking = false;
    }

    public void SetDamage(int dmg) => damage = dmg;
}
