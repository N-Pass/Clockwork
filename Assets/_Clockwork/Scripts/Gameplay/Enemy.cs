// Enemy.cs — sistema de waypoints por direção
//
// Fluxo de movimento:
//   1. WaveManager chama SetDirection() com a direção do spawn point
//   2. Start() monta a rota: [FirstPoint] + [2-6 intermediários] + [FinalPoint]
//   3. Update() move em direção ao waypoint atual
//   4. Ao chegar, avança para o próximo
//   5. FinalPoint é sempre próximo à torre — colisão finaliza a rota

using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Stats
    // ------------------------------------------------------------------
    [SerializeField] protected int   maxHP             = 2;
    [SerializeField] protected float moveSpeed         = 1.5f;
    [SerializeField] protected int   damageOnCollision = 1;
    [SerializeField] protected int   scrapDrop         = 3;

    [Header("Waypoints")]
    [SerializeField] private float arrivalDistance = 0.3f;

    // ------------------------------------------------------------------
    // Estado de rota
    // ------------------------------------------------------------------
    private WaypointGrid.Direction      direction;
    private WaypointGrid.HorizontalSide currentSide = WaypointGrid.HorizontalSide.Undefined;
    private List<Vector3>               route       = new List<Vector3>();
    private int                         routeIndex  = 0;
    private bool                        routeReady  = false;
    private bool                        directionSet = false;

    // ------------------------------------------------------------------
    // Componentes
    // ------------------------------------------------------------------
    private   Rigidbody2D  rb2D;
    protected HealthSystem healthSystem;

    // ------------------------------------------------------------------
    // API — WaveManager chama SetDirection() logo após Instantiate
    // ------------------------------------------------------------------
    public void SetDirection(WaypointGrid.Direction dir)
    {
        direction    = dir;
        directionSet = true;
    }

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

        if (!directionSet)
            direction = WaypointGrid.Direction.North;

        BuildRoute();
    }

    protected virtual void Update()
    {
        if (!routeReady) return;
        HandleMovement();
    }

    // ------------------------------------------------------------------
    // Construção da rota
    // ------------------------------------------------------------------
    private void BuildRoute()
    {
        route.Clear();
        routeIndex = 0;

        // 1. Primeiro ponto obrigatório
        Vector3 firstPoint = WaypointGrid.GetFirstPoint(direction);
        route.Add(firstPoint);

        // Determina lado inicial a partir do primeiro ponto
        int approxCol = Mathf.RoundToInt(
            (firstPoint.x - WaypointGrid.GridOrigin.x) / WaypointGrid.CellWidth - 0.5f
        );
        approxCol   = Mathf.Clamp(approxCol, 0, WaypointGrid.Cols - 1);
        currentSide = WaypointGrid.GetSide(approxCol);

        // 2. Waypoints intermediários (2 a 6)
        int count = Random.Range(2, 7);
        for (int i = 0; i < count; i++)
        {
            WaypointGrid.HorizontalSide newSide;
            Vector3 wp = WaypointGrid.GetRandomIntermediate(direction, currentSide, out newSide);
            currentSide = newSide;
            route.Add(wp);
        }

        // 3. Ponto final — próximo à torre
        route.Add(WaypointGrid.GetFinalPoint(direction));

        routeReady = true;
    }

    // ------------------------------------------------------------------
    // Movimento
    // ------------------------------------------------------------------
    private void HandleMovement()
    {
        if (routeIndex >= route.Count)
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 target = route[routeIndex];
        float   dist   = Vector3.Distance(transform.position, target);

        if (dist <= arrivalDistance)
        {
            routeIndex++;
            return;
        }

        MoveToward(target);
    }

    private void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        rb2D.linearVelocity = dir * moveSpeed;

        if (dir != Vector3.zero)
            transform.eulerAngles = new Vector3(0f, 0f, UtilsClass.GetAngleFromVector(dir));
    }

    // ------------------------------------------------------------------
    // Colisão com torre
    // ------------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Tower tower = collision.gameObject.GetComponent<Tower>();
        if (tower != null)
        {
            tower.GetComponent<HealthSystem>().Damage(damageOnCollision);
            healthSystem.Damage(9999);
        }
    }

    // ------------------------------------------------------------------
    // Eventos de HP
    // ------------------------------------------------------------------
    private void HealthSystem_OnDamaged(object sender, System.EventArgs e) { }

    protected virtual void HealthSystem_OnDied(object sender, System.EventArgs e)
    {
        OnDeathEffect();
        RunManager.Instance.OnEnemyKilled(scrapDrop);
        Destroy(gameObject);
    }

    protected virtual void OnDeathEffect() { }

    // ------------------------------------------------------------------
    // API pública
    // ------------------------------------------------------------------
    public virtual void SetStats(int hp, float speed, int damage, int drop)
    {
        maxHP             = hp;
        moveSpeed         = speed;
        damageOnCollision = damage;
        scrapDrop         = drop;
    }

    public HealthSystem GetHealthSystem() => healthSystem;
}
