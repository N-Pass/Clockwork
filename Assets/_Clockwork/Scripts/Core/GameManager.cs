// GameManager.cs
// Singleton persistente (DontDestroyOnLoad).
// Responsabilidades:
//   - Gerenciar qual perfil está ativo
//   - Transportar dados entre cenas (HUD → Gameplay → HUD)
//   - Disparar transições de cena
//   - Compra de upgrades
//
// NÃO contém lógica de gameplay — isso fica no RunManager (Cena 3).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Singleton
    // ------------------------------------------------------------------
    public static GameManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // Índices de cena — ajuste conforme Build Settings
    // ------------------------------------------------------------------
    private const int SCENE_MAIN_MENU = 0;
    private const int SCENE_HUD       = 1;
    private const int SCENE_GAMEPLAY  = 2;

    // Número total de peças do relógio necessárias para o estágio final
    public const int TOTAL_CLOCK_PIECES = 5;

    // ------------------------------------------------------------------
    // Estado atual
    // ------------------------------------------------------------------
    public ProfileData CurrentProfile { get; private set; }
    public int CurrentSlot { get; private set; } = -1;

    // Dados coletados durante a run atual (preenchidos pelo RunManager)
    public int RunScrapsEarned   { get; set; } = 0;
    public bool RunWasSuccess    { get; set; } = false;
    public List<int> RunPiecesEarned { get; set; } = new List<int>();

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        Application.targetFrameRate = 60;
        // Garantia de singleton único entre cenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ------------------------------------------------------------------
    // Gerenciamento de perfis (chamado pela MainMenu)
    // ------------------------------------------------------------------

    public void CreateProfile(int slot)
    {
        CurrentSlot = slot;
        CurrentProfile = new ProfileData();
        SaveSystem.Save(slot, CurrentProfile);
        SceneManager.LoadScene(SCENE_HUD);
    }

    public void LoadProfile(int slot)
    {
        CurrentSlot = slot;
        CurrentProfile = SaveSystem.Load(slot);

        if (CurrentProfile == null)
        {
            Debug.LogError($"[GameManager] Falha ao carregar slot {slot}. Criando novo perfil.");
            CurrentProfile = new ProfileData();
        }

        SceneManager.LoadScene(SCENE_HUD);
    }

    public void DeleteProfile(int slot)
    {
        SaveSystem.Delete(slot);

        // Se deletou o perfil ativo, limpa o estado
        if (slot == CurrentSlot)
        {
            CurrentProfile = null;
            CurrentSlot = -1;
        }
    }

    public bool HasProfile(int slot) => SaveSystem.HasProfile(slot);

    // ------------------------------------------------------------------
    // Transições de cena
    // ------------------------------------------------------------------

    // HUD → Gameplay: reseta dados da run e carrega a cena
    public void StartRun()
    {
        RunScrapsEarned = 0;
        RunWasSuccess   = false;
        RunPiecesEarned = new List<int>();
        SceneManager.LoadScene(SCENE_GAMEPLAY);
    }

    // Gameplay → HUD: aplica resultados, salva e retorna
    // Chamado pelo RunManager ao final de cada run
    public void EndRun()
    {
        if (CurrentProfile == null) return;

        // Aplica scraps — derrota dá metade
        int scrapsToAdd = RunWasSuccess
            ? RunScrapsEarned
            : Mathf.FloorToInt(RunScrapsEarned * 0.5f);

        CurrentProfile.totalScraps            += scrapsToAdd;
        CurrentProfile.totalScrapsEverEarned  += scrapsToAdd;
        CurrentProfile.totalRuns++;

        // Registra peças coletadas
        foreach (int piece in RunPiecesEarned)
        {
            if (!CurrentProfile.HasClockPiece(piece))
                CurrentProfile.clockPiecesCollected.Add(piece);
        }

        // Verifica conclusão do jogo
        if (CurrentProfile.HasAllPieces(TOTAL_CLOCK_PIECES))
            CurrentProfile.gameCompleted = true;

        SaveSystem.Save(CurrentSlot, CurrentProfile);
        SceneManager.LoadScene(SCENE_HUD);
    }

    public void GoToMainMenu()
    {
        CurrentProfile = null;
        CurrentSlot    = -1;
        SceneManager.LoadScene(SCENE_MAIN_MENU);
    }

    // ------------------------------------------------------------------
    // Sistema de upgrades (chamado pelo HUDController)
    // ------------------------------------------------------------------

    public int GetUpgradeLevel(UpgradeType type)
    {
        if (CurrentProfile == null) return 0;
        return CurrentProfile.GetUpgradeLevel(type);
    }

    // Retorna true se a compra foi realizada, false se scraps insuficientes
    public bool TryPurchaseUpgrade(UpgradeType type)
    {
        if (CurrentProfile == null) return false;

        int currentLevel = CurrentProfile.GetUpgradeLevel(type);

        if (!UpgradeConfig.HasNextLevel(type, currentLevel))
        {
            Debug.Log($"[GameManager] {type} já está no nível máximo.");
            return false;
        }

        int cost = UpgradeConfig.GetCost(type, currentLevel);

        if (CurrentProfile.totalScraps < cost)
        {
            Debug.Log($"[GameManager] Scraps insuficientes. Precisa: {cost}, tem: {CurrentProfile.totalScraps}");
            return false;
        }

        CurrentProfile.totalScraps -= cost;
        CurrentProfile.SetUpgradeLevel(type, currentLevel + 1);
        SaveSystem.Save(CurrentSlot, CurrentProfile);

        Debug.Log($"[GameManager] Upgrade {type} → nível {currentLevel + 1}. Scraps restantes: {CurrentProfile.totalScraps}");
        return true;
    }

    // ------------------------------------------------------------------
    // Helpers de leitura rápida dos valores de upgrade (para outros scripts)
    // ------------------------------------------------------------------

    public int   GetClickDamage()     => UpgradeConfig.GetClickDamage(GetUpgradeLevel(UpgradeType.ClickDamage));
    public int   GetTowerHP()         => UpgradeConfig.GetTowerHP(GetUpgradeLevel(UpgradeType.TowerHP));
    public int   GetRunDuration()     => UpgradeConfig.GetRunDuration(GetUpgradeLevel(UpgradeType.RunDuration));
    public float GetScrapDropRate()   => UpgradeConfig.GetScrapDrop(GetUpgradeLevel(UpgradeType.ScrapDropRate));
    public int   GetSubTowerSlots()   => UpgradeConfig.GetSubTowerSlots(GetUpgradeLevel(UpgradeType.SubTowerSlots));
    public float GetClickArea()       => UpgradeConfig.GetClickArea(GetUpgradeLevel(UpgradeType.ClickArea));
}
