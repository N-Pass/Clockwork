// WaveManager.cs  — adaptado do EnemyWaveManager do projeto anterior
//
// Mudanças em relação ao original:
//   1. Só spawna quando RunManager.State == Running
//   2. Enemy.SetStats() passa stats escalados pelo nível
//   3. A cada wavesBetweenBoss waves → spawna Boss no lugar da wave
//   4. Removido: nextWaveSpawnPosition indicator (câmera estática)
//   5. Removido: GetSpawnPosition() para UI (não necessário)
//   6. Usa Enemy e Boss prefabs serializados (não Resources.Load)

using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Spawn Points — 4 nas bordas N/S/L/O")]
    [SerializeField] private List<Transform> spawnPoints;

    [Header("Prefabs")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Boss  bossPrefab;

    [Header("Stats base (nível 0)")]
    [SerializeField] private int   baseHP        = 10;
    [SerializeField] private float baseSpeed     = 4f;
    [SerializeField] private int   baseDamage    = 1;
    [SerializeField] private int   baseDrop      = 3;

    [Header("Scaling")]
    [SerializeField] private float statMultiplierPerLevel = 1.3f;  // cada nível multiplica os stats
    [SerializeField] private int   wavesBetweenBoss       = 5;     // boss a cada N waves
    [SerializeField] private int   bossDropMultiplier     = 10;    // drop do boss = baseDrop * mult * level

    [Header("Timing")]
    [SerializeField] private float firstWaveDelay  = 2f;           // delay antes da primeira wave
    [SerializeField] private float minWaveInterval = 3f;           // intervalo mínimo entre waves
    [SerializeField] private float maxWaveInterval = 8f;           // intervalo máximo (inicial)
    [SerializeField] private float spawnInterval   = 0.25f;        // tempo entre spawns na mesma wave

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    private enum State { Idle, Waiting, Spawning }
    private State state = State.Idle;

    private int   waveNumber         = 0;
    private int   currentEnemyLevel  = 0;
    private bool  isBossWave         = false;

    private float waveTimer;
    private float spawnTimer;
    private int   remainingToSpawn;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RunManager.Instance.OnRunStarted    += OnRunStarted;
        RunManager.Instance.OnEnemyLevelUp  += OnEnemyLevelUp;
        RunManager.Instance.OnRunEnded      += OnRunEnded;
    }

    private void Update()
    {
        if (RunManager.Instance.CurrentState != RunManager.RunState.Running) return;

        switch (state)
        {
            case State.Waiting:
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f)
                    StartWave();
                break;

            case State.Spawning:
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f && remainingToSpawn > 0)
                {
                    spawnTimer = spawnInterval;
                    SpawnOne();
                    remainingToSpawn--;

                    if (remainingToSpawn <= 0)
                    {
                        state     = State.Waiting;
                        waveTimer = GetNextWaveInterval();
                    }
                }
                break;
        }
    }

    // ------------------------------------------------------------------
    // Eventos do RunManager
    // ------------------------------------------------------------------
    private void OnRunStarted()
    {
        state     = State.Waiting;
        waveTimer = firstWaveDelay;
    }

    private void OnEnemyLevelUp(int newLevel)
    {
        currentEnemyLevel = newLevel;
    }

    private void OnRunEnded(bool success)
    {
        state = State.Idle;
    }

    // ------------------------------------------------------------------
    // Wave logic
    // ------------------------------------------------------------------
    private void StartWave()
    {
        waveNumber++;
        isBossWave     = (waveNumber % wavesBetweenBoss == 0);
        remainingToSpawn = isBossWave ? 1 : GetEnemyCountForWave();
        spawnTimer     = 0f;
        state          = State.Spawning;
    }

    private void SpawnOne()
    {
        if (spawnPoints.Count == 0) return;

        // Ponto de spawn aleatório + pequeno offset
        Transform point    = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Vector3   spawnPos = point.position + (Vector3)UtilsClass.GetRandomDir() * Random.Range(0f, 0.8f);

        if (isBossWave)
        {
            Boss boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            int  bossId = waveNumber / wavesBetweenBoss; // ID único da peça do relógio
            boss.SetBossStats(
                hp:     GetScaledHP() * 5,
                speed:  GetScaledSpeed() * 0.6f,
                damage: GetScaledDamage() * 3,
                drop:   GetScaledDrop() * bossDropMultiplier,
                pieceId: bossId
            );
        }
        else
        {
            Enemy enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.SetStats(GetScaledHP(), GetScaledSpeed(), GetScaledDamage(), GetScaledDrop());
        }
    }

    // ------------------------------------------------------------------
    // Scaling de stats
    // ------------------------------------------------------------------
    private float GetMultiplier()    => Mathf.Pow(statMultiplierPerLevel, currentEnemyLevel);

    private int   GetScaledHP()      => Mathf.RoundToInt(baseHP    * GetMultiplier());
    private float GetScaledSpeed()   => baseSpeed  + currentEnemyLevel * 0.2f;
    private int   GetScaledDamage()  => Mathf.RoundToInt(baseDamage * GetMultiplier());
    private int   GetScaledDrop()    => Mathf.RoundToInt(baseDrop   * GetMultiplier());

    private int GetEnemyCountForWave()
        => 3 + waveNumber + currentEnemyLevel;

    // Intervalo entre waves diminui conforme progresso
    private float GetNextWaveInterval()
        => Mathf.Max(minWaveInterval, maxWaveInterval - waveNumber * 0.3f);
}
