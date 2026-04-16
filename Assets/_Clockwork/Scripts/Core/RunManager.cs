// RunManager.cs
// Singleton da Cena 3. Orquestra timer, kills, scraps,
// pathfinding de inimigos e transição para GameManager ao fim da run.
//
// State machine: Setup → Running → End
//
// Dependências: GameManager (persiste entre cenas), Tower, HealthSystem

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
    // Eventos — WaveManager e GameplayUI escutam esses
    // ------------------------------------------------------------------
    public event Action           OnRunStarted;
    public event Action<int>      OnEnemyLevelUp;    // novo nível
    public event Action<int>      OnScrapsChanged;   // scraps totais da run
    public event Action<float>    OnTimerChanged;    // 0..1 normalizado
    public event Action<bool>     OnRunEnded;        // true = vitória

    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Referências")]
    [SerializeField] private Tower mainTower;

    [Header("Configuração de dificuldade")]
    [SerializeField] private int killsPerEnemyLevel = 10;

    [Header("Delay antes de trocar de cena")]
    [SerializeField] private float endRunDelay = 1.5f;

    // ------------------------------------------------------------------
    // Estado interno
    // ------------------------------------------------------------------
    private float runTimer;
    private float runDuration;

    private int  scrapsEarned;
    private int  killCount;

    public int EnemyLevel { get; private set; } = 0;

    private readonly List<int>   piecesEarnedThisRun = new List<int>();
    private readonly List<Tower> allTowers           = new List<Tower>();

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Lê duração da run do upgrade
        runDuration = GameManager.Instance.GetRunDuration();
        runTimer    = runDuration;

        // Aplica HP da torre vindo do upgrade
        HealthSystem towerHS = mainTower.GetComponent<HealthSystem>();
        towerHS.SetHealthAmountMax(GameManager.Instance.GetTowerHP(), updateHealthAmount: true);
        towerHS.OnDied += (s, e) => TowerDestroyed();

        // Torre principal é sempre o primeiro alvo
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
    // Estados
    // ------------------------------------------------------------------
    private void EnterSetup()
    {
        CurrentState = RunState.Setup;
        // SubTowerPlacement.cs só permite colocar torres quando Setup
    }

    // Chamado pelo botão "Start" na UI da fase de Setup
    public void BeginRun()
    {
        if (CurrentState != RunState.Setup) return;

        CurrentState = RunState.Running;
        OnRunStarted?.Invoke();
    }

    // ------------------------------------------------------------------
    // Gerenciamento de torres (para pathfinding dos inimigos)
    // ------------------------------------------------------------------
    public void RegisterSubTower(Tower subTower)
    {
        if (subTower == null || allTowers.Contains(subTower)) return;

        allTowers.Add(subTower);

        HealthSystem hs = subTower.GetComponent<HealthSystem>();
        if (hs != null)
            hs.OnDied += (s, e) => UnregisterTower(subTower);
    }

    private void UnregisterTower(Tower tower)
    {
        allTowers.Remove(tower);
    }

    // ------------------------------------------------------------------
    // Pathfinding — Enemy chama isso a cada 0.2s para atualizar alvo
    // ------------------------------------------------------------------
    public Tower GetNearestTower(Vector3 fromPosition)
    {
        Tower nearest  = null;
        float minDist  = float.MaxValue;

        foreach (Tower tower in allTowers)
        {
            if (tower == null) continue;

            float dist = Vector3.Distance(fromPosition, tower.transform.position);
            if (dist < minDist)
            {
                minDist  = dist;
                nearest  = tower;
            }
        }

        // Fallback para torre principal se lista estiver vazia
        return nearest != null ? nearest : mainTower;
    }

    public Tower GetMainTower() => mainTower;

    // ------------------------------------------------------------------
    // Scraps — chamado por Enemy.OnDied e MiningNode.Collect
    // ------------------------------------------------------------------
    public void AddScraps(int amount)
    {
        scrapsEarned += amount;
        OnScrapsChanged?.Invoke(scrapsEarned);
    }

    // ------------------------------------------------------------------
    // Eventos de inimigos — chamados por Enemy e Boss ao morrer
    // ------------------------------------------------------------------
    public void OnEnemyKilled(int rawDrop)
    {
        // Aplica multiplicador de scrap do upgrade
        int scraps = Mathf.RoundToInt(rawDrop * GameManager.Instance.GetScrapDropRate());
        AddScraps(scraps);

        killCount++;

        // Sobe o nível de inimigos a cada X kills
        if (killCount % killsPerEnemyLevel == 0)
        {
            EnemyLevel++;
            OnEnemyLevelUp?.Invoke(EnemyLevel);
        }
    }

    // Boss dropa uma peça do relógio além dos scraps normais
    public void OnBossKilled(int pieceId, int rawDrop)
    {
        if (!piecesEarnedThisRun.Contains(pieceId))
            piecesEarnedThisRun.Add(pieceId);

        OnEnemyKilled(rawDrop);

        // Se todas as peças foram coletadas, desbloqueia estágio final
        // Para a jam: simplesmente termina a run como vitória total
        if (piecesEarnedThisRun.Count + GetTotalPiecesAlreadyOwned() >= GameManager.TOTAL_CLOCK_PIECES)
        {
            EndRun(success: true);
        }
    }

    private int GetTotalPiecesAlreadyOwned()
    {
        return GameManager.Instance.CurrentProfile?.clockPiecesCollected.Count ?? 0;
    }

    // ------------------------------------------------------------------
    // Torre destruída — derrota
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

        // Passa dados para o GameManager persistente
        GameManager.Instance.RunScrapsEarned  = scrapsEarned;
        GameManager.Instance.RunWasSuccess    = success;
        GameManager.Instance.RunPiecesEarned  = piecesEarnedThisRun;

        OnRunEnded?.Invoke(success);

        // Delay para animação de vitória/derrota antes de trocar de cena
        StartCoroutine(DelayedSceneChange());
    }

    private IEnumerator DelayedSceneChange()
    {
        yield return new WaitForSeconds(endRunDelay);
        GameManager.Instance.EndRun();
    }

    // ------------------------------------------------------------------
    // Utilitários
    // ------------------------------------------------------------------
    public float GetTimerNormalized() => runDuration > 0 ? runTimer / runDuration : 0f;
    public float GetTimerSeconds()    => runTimer;
    public int   GetScrapsEarned()    => scrapsEarned;
    public int   GetKillCount()       => killCount;
}
