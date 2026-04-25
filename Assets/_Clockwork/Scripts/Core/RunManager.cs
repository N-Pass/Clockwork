// RunManager.cs
// Singleton da Cena 3. Orquestra timer, kills, scraps,
// pathfinding de inimigos e transição para GameManager ao fim da run.
//
// Usa RunContext para todos os valores de upgrade —
// não chama GameManager.GetX() diretamente.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // State machine
    // ------------------------------------------------------------------
    public enum RunState { Setup, Running, End }
    public RunState CurrentState { get; private set; }

    // ------------------------------------------------------------------
    // Eventos
    // ------------------------------------------------------------------
    public event Action        OnRunStarted;
    public event Action<int>   OnEnemyLevelUp;
    public event Action<int>   OnScrapsChanged;
    public event Action<float> OnTimerChanged;
    public event Action<bool>  OnRunEnded;

    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Referências")]
    [SerializeField] private Tower        mainTower;
    [SerializeField] private UpgradeTreeSO upgradeTree; // arrastar o UpgradeTreeSO aqui

    [Header("Configuração de dificuldade")]
    [SerializeField] private int killsPerEnemyLevel = 10;

    [Header("Override de teste — ignora upgrades quando marcado")]
    [SerializeField] private bool  overrideStats      = false;
    [SerializeField] private float overrideRunDuration = 30f;
    [SerializeField] private int   overrideTowerHP     = 5;
    [SerializeField] private int   overrideClickDamage = 1;
    [SerializeField] private float overrideFireRate    = 1.5f;
    [SerializeField] private float overrideProjectileSpeed = 18f;
    [SerializeField] private float overrideScrapDropRate   = 1f;
    [SerializeField] private int   overrideSubTowerSlots = 0;
    [SerializeField] private int   overrideMachineGunSlots = 0;

    // ------------------------------------------------------------------
    // RunContext — valores ativos nesta run
    // Acessível por WeaponController, Projectile, WaveManager etc.
    // ------------------------------------------------------------------
    public RunContext Context { get; private set; }

    // ------------------------------------------------------------------
    // Estado interno
    // ------------------------------------------------------------------
    private float runTimer;
    private float runDuration;
    private int   scrapsEarned;
    private int   killCount;

    public int EnemyLevel { get; private set; } = 0;

    private readonly List<int>   piecesEarnedThisRun = new List<int>();
    private readonly List<Tower> allTowers = new List<Tower>();

    // Posição de grade de cada subtorre registrada
    private readonly Dictionary<Tower, (int col, int row)> towerGridPositions
        = new Dictionary<Tower, (int col, int row)>();

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Monta o RunContext
        BuildContext();

        // Aplica duração da run
        runDuration = Context.runDuration;
        runTimer    = runDuration;

        // Aplica HP da torre
        HealthSystem towerHS = mainTower.GetComponent<HealthSystem>();
        towerHS.SetHealthAmountMax(Context.towerHP, updateHealthAmount: true);
        towerHS.OnDied += (s, e) => TowerDestroyed();

        allTowers.Clear();
        allTowers.Add(mainTower);

        EnterSetup();
    }

    private void Update()
    {
        if (CurrentState != RunState.Running) return;

        runTimer -= Time.deltaTime;
        OnTimerChanged?.Invoke(runTimer / runDuration);

        if (runTimer <= 0f)
            EndRun(success: true);
    }

    // ------------------------------------------------------------------
    // Construção do RunContext
    // ------------------------------------------------------------------
    private void BuildContext()
    {
        if (overrideStats)
        {
            // Modo de teste — usa valores do Inspector diretamente
            Context = new RunContext
            {
                towerHP          = overrideTowerHP,
                clickDamage      = overrideClickDamage,
                fireRate         = overrideFireRate,
                projectileSpeed  = overrideProjectileSpeed,
                runDuration      = overrideRunDuration,
                scrapDropRate    = overrideScrapDropRate,
                subTowerSlots    = overrideSubTowerSlots,   
                machineGunSlots  = overrideMachineGunSlots
            };
            return;
        }

        // Modo produção — lê perfil e aplica upgrades
        ProfileData profile = GameManager.Instance?.CurrentProfile ?? new ProfileData();
        Context = RunContext.FromProfile(profile, upgradeTree);
    }

    // ------------------------------------------------------------------
    // Acessores rápidos para outros scripts
    // ------------------------------------------------------------------
    public int   GetClickDamage()      => Context?.clickDamage     ?? 1;
    public float GetFireRate()         => Context?.fireRate         ?? 1.5f;
    public float GetProjectileSpeed()  => Context?.projectileSpeed  ?? 18f;
    public float GetScrapDropRate()    => Context?.scrapDropRate     ?? 1f;
    public int   GetSubTowerSlots()    => Context?.subTowerSlots    ?? 0;
    public int   GetMachineGunSlots()  => Context?.machineGunSlots  ?? 0;

    // ------------------------------------------------------------------
    // Estados
    // ------------------------------------------------------------------
    private void EnterSetup()
    {
        CurrentState = RunState.Setup;
    }

    public void BeginRun()
    {
        if (CurrentState != RunState.Setup) return;
        CurrentState = RunState.Running;
        OnRunStarted?.Invoke();
    }

    // ------------------------------------------------------------------
    // Torres
    // ------------------------------------------------------------------
    public void RegisterSubTower(Tower subTower)
    {
        RegisterSubTower(subTower, -1, -1);
    }

    public void RegisterSubTower(Tower subTower, int col, int row)
    {
        if (subTower == null || allTowers.Contains(subTower)) return;
        allTowers.Add(subTower);

        if (col >= 0 && row >= 0)
            towerGridPositions[subTower] = (col, row);

        HealthSystem hs = subTower.GetComponent<HealthSystem>();
        if (hs != null)
            hs.OnDied += (s, e) => UnregisterTower(subTower);
    }

    private void UnregisterTower(Tower tower)
    {
        allTowers.Remove(tower);
        towerGridPositions.Remove(tower);
    }

    // Retorna a subtorre no quadrante do inimigo, ou a torre principal se não houver
    public Tower GetPreferredTarget(WaypointGrid.Direction direction)
    {
        foreach (Tower tower in allTowers)
        {
            if (tower == null || tower == mainTower) continue;
            if (!towerGridPositions.ContainsKey(tower)) continue;

            var (col, row) = towerGridPositions[tower];

            bool inQuadrant = direction switch
            {
                WaypointGrid.Direction.West  => col >= 0 && col <= 4,
                WaypointGrid.Direction.East  => col >= 7 && col <= 11,
                WaypointGrid.Direction.North => row >= 0 && row <= 2,
                WaypointGrid.Direction.South => row >= 3 && row <= 5,
                _ => false
            };

            if (inQuadrant) return tower;
        }

        return mainTower;
    }

    public Tower GetNearestTower(Vector3 fromPosition)
    {
        Tower nearest = null;
        float minDist = float.MaxValue;

        foreach (Tower tower in allTowers)
        {
            if (tower == null) continue;
            float dist = Vector3.Distance(fromPosition, tower.transform.position);
            if (dist < minDist) { minDist = dist; nearest = tower; }
        }

        return nearest != null ? nearest : mainTower;
    }

    public Tower GetMainTower() => mainTower;

    // ------------------------------------------------------------------
    // Scraps
    // ------------------------------------------------------------------
    public void AddScraps(int amount)
    {
        scrapsEarned += amount;
        OnScrapsChanged?.Invoke(scrapsEarned);
    }

    // ------------------------------------------------------------------
    // Kills
    // ------------------------------------------------------------------
    public void OnEnemyKilled(int rawDrop)
    {
        float dropRate = Context?.scrapDropRate ?? 1f;
        int scraps     = Mathf.RoundToInt(rawDrop * dropRate);
        AddScraps(scraps);

        killCount++;

        if (killCount % killsPerEnemyLevel == 0)
        {
            EnemyLevel++;
            OnEnemyLevelUp?.Invoke(EnemyLevel);
        }
    }

    public void OnBossKilled(int pieceId, int rawDrop)
    {
        if (!piecesEarnedThisRun.Contains(pieceId))
            piecesEarnedThisRun.Add(pieceId);

        OnEnemyKilled(rawDrop);

        int totalPieces = GetTotalPiecesAlreadyOwned() + piecesEarnedThisRun.Count;
        if (totalPieces >= GameManager.TOTAL_CLOCK_PIECES)
            EndRun(success: true);
    }

    private int GetTotalPiecesAlreadyOwned()
    {
        return GameManager.Instance?.CurrentProfile?.clockPiecesCollected.Count ?? 0;
    }

    // ------------------------------------------------------------------
    // Torre destruída
    // ------------------------------------------------------------------
    private void TowerDestroyed()
    {
        if (CurrentState == RunState.End) return;
        EndRun(success: false);
    }

    // ------------------------------------------------------------------
    // Fim da run
    // ------------------------------------------------------------------
    private void EndRun(bool success)
    {
        if (CurrentState == RunState.End) return;

        CurrentState = RunState.End;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunScrapsEarned = scrapsEarned;
            GameManager.Instance.RunWasSuccess   = success;
            GameManager.Instance.RunPiecesEarned = piecesEarnedThisRun;
        }

        OnRunEnded?.Invoke(success);
    }

    // ------------------------------------------------------------------
    // Utilitários
    // ------------------------------------------------------------------
    public float GetTimerNormalized() => runDuration > 0 ? runTimer / runDuration : 0f;
    public float GetTimerSeconds()    => runTimer;
    public int   GetScrapsEarned()    => scrapsEarned;
    public int   GetKillCount()       => killCount;
}
