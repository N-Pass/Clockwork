// GameplayUI.cs
// HUD da Cena 3 — atualiza em tempo real durante a run.
// Escuta eventos do RunManager: timer, scraps, enemy level, run end.
//
// Hierarquia esperada no Canvas:
//   GameplayUI
//   ├─ TimerBar           (Image com fillAmount)
//   ├─ TimerText          (ex: "23.4s")
//   ├─ TowerHPBar         (HealthBar.cs já cuida disso — opcional aqui)
//   ├─ ScrapsText         (ex: "Scraps: 42")
//   ├─ EnemyLevelText     (ex: "Enemy Lv 3")
//   ├─ WaveText           (ex: "Wave 7")
//   └─ SetupPanel         (visível só no estado Setup)
//       └─ StartRunButton ("Deploy")

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUI : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Timer")]
    [SerializeField] private Image           timerFillBar;     // Image com Fill Method = Horizontal
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Recursos")]
    [SerializeField] private TextMeshProUGUI scrapsText;
    [SerializeField] private TextMeshProUGUI enemyLevelText;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Setup phase")]
    [SerializeField] private GameObject      setupPanel;
    [SerializeField] private Button          startRunButton;

    [Header("Fim de run")]
    [SerializeField] private RunEndUI        runEndUI;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Start()
    {
        if (RunManager.Instance == null) return;
        // Escuta eventos do RunManager
        RunManager.Instance.OnRunStarted    += OnRunStarted;
        RunManager.Instance.OnTimerChanged  += OnTimerChanged;
        RunManager.Instance.OnScrapsChanged += OnScrapsChanged;
        RunManager.Instance.OnEnemyLevelUp  += OnEnemyLevelUp;
        RunManager.Instance.OnRunEnded      += OnRunEnded;

        // Botão de início da run (fase Setup)
        startRunButton?.onClick.AddListener(() => RunManager.Instance.BeginRun());

        // Estado inicial
        RefreshScraps(0);
        RefreshEnemyLevel(0);
        SetupPanel_Show(true);
    }

    private void OnDestroy()
    {
        if (RunManager.Instance == null) return;
        RunManager.Instance.OnRunStarted    -= OnRunStarted;
        RunManager.Instance.OnTimerChanged  -= OnTimerChanged;
        RunManager.Instance.OnScrapsChanged -= OnScrapsChanged;
        RunManager.Instance.OnEnemyLevelUp  -= OnEnemyLevelUp;
        RunManager.Instance.OnRunEnded      -= OnRunEnded;
    }

    // ------------------------------------------------------------------
    // Callbacks do RunManager
    // ------------------------------------------------------------------
    private void OnRunStarted()
    {
        SetupPanel_Show(false);
        RefreshTimer(1f); // começa cheio
    }

    private void OnTimerChanged(float normalized)
    {
        RefreshTimer(normalized);
    }

    private void OnScrapsChanged(int total)
    {
        RefreshScraps(total);
    }

    private void OnEnemyLevelUp(int newLevel)
    {
        RefreshEnemyLevel(newLevel);
    }

    private void OnRunEnded(bool success)
    {
        runEndUI?.Show(success, RunManager.Instance.GetScrapsEarned(), RunManager.Instance.GetKillCount());
    }

    // ------------------------------------------------------------------
    // Atualização de elementos visuais
    // ------------------------------------------------------------------
    private void RefreshTimer(float normalized)
    {
        if (timerFillBar != null)
            timerFillBar.fillAmount = normalized;

        if (timerText != null)
        {
            float seconds = RunManager.Instance.GetTimerSeconds();
            timerText.SetText(seconds.ToString("F1") + "s");
        }
    }

    private void RefreshScraps(int total)
    {
        if (scrapsText != null)
            scrapsText.SetText("Scraps: " + total);
    }

    private void RefreshEnemyLevel(int level)
    {
        if (enemyLevelText != null)
            enemyLevelText.SetText("Enemy Lv " + level);
    }

    private void SetupPanel_Show(bool show)
    {
        setupPanel?.SetActive(show);
    }
}
