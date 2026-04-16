// HUDController.cs
// Controla a Cena 2 — HUD de upgrades entre runs.
//
// Lê o ProfileData do GameManager, exibe estado de cada upgrade,
// permite compra com scraps e inicia a run via GameManager.StartRun().
//
// Hierarquia esperada no Canvas:
//   HUDController
//   ├─ ScrapsText              (total de scraps disponíveis)
//   ├─ ClockPiecesText         (X / TOTAL_CLOCK_PIECES)
//   ├─ RunButton               ("Hold the Line")
//   └─ UpgradeGrid
//       └─ UpgradeItem (×6)    — prefab com UpgradeItemUI

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("HUD superior")]
    [SerializeField] private TextMeshProUGUI scrapsText;
    [SerializeField] private TextMeshProUGUI clockPiecesText;

    [Header("Botão de run")]
    [SerializeField] private Button          runButton;
    [SerializeField] private TextMeshProUGUI runButtonText;

    [Header("Upgrades — arrastar os 6 itens")]
    [SerializeField] private UpgradeItemUI[] upgradeItems;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Start()
    {
        runButton.onClick.AddListener(OnRunButtonClicked);

        // Configura cada UpgradeItemUI com seu tipo correspondente
        UpgradeType[] types = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
        for (int i = 0; i < upgradeItems.Length && i < types.Length; i++)
        {
            upgradeItems[i].Initialize(types[i], this);
        }

        // Verifica se todas as peças foram coletadas (jogo completo)
        bool gameCompleted = GameManager.Instance.CurrentProfile?.gameCompleted ?? false;
        runButton.interactable = !gameCompleted;
        runButtonText.SetText(gameCompleted ? "Restored" : "Hold the Line");

        RefreshUI();
    }

    // ------------------------------------------------------------------
    // Atualização da UI
    // ------------------------------------------------------------------
    public void RefreshUI()
    {
        if (GameManager.Instance.CurrentProfile == null) return;

        int scraps = GameManager.Instance.CurrentProfile.totalScraps;
        int pieces = GameManager.Instance.CurrentProfile.clockPiecesCollected.Count;

        scrapsText.SetText("Scraps: " + scraps);
        clockPiecesText.SetText("Clock Pieces: " + pieces + " / " + GameManager.TOTAL_CLOCK_PIECES);

        foreach (UpgradeItemUI item in upgradeItems)
            item.Refresh();
    }

    // ------------------------------------------------------------------
    // Compra de upgrade — chamada pelo UpgradeItemUI
    // ------------------------------------------------------------------
    public void TryBuyUpgrade(UpgradeType type)
    {
        bool purchased = GameManager.Instance.TryPurchaseUpgrade(type);

        if (purchased)
            RefreshUI();
        // Se não comprou: feedback visual (shake no botão, som de erro, etc.)
    }

    // ------------------------------------------------------------------
    // Run button
    // ------------------------------------------------------------------
    private void OnRunButtonClicked()
    {
        GameManager.Instance.StartRun();
    }
}

// ------------------------------------------------------------------
// UpgradeItemUI — representa um upgrade individual na grade
// Attach no prefab de cada item de upgrade
// ------------------------------------------------------------------
public class UpgradeItemUI : MonoBehaviour
{
    [Header("Componentes do prefab")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button          buyButton;

    private UpgradeType    upgradeType;
    private HUDController  hud;

    private static readonly string[] upgradeNames =
    {
        "Click Damage",
        "Tower HP",
        "Run Duration",
        "Scrap Drop",
        "Sub Towers",
        "Click Area"
    };

    public void Initialize(UpgradeType type, HUDController controller)
    {
        upgradeType = type;
        hud         = controller;

        if (nameText != null)
            nameText.SetText(upgradeNames[(int)type]);

        buyButton.onClick.AddListener(() => hud.TryBuyUpgrade(upgradeType));

        Refresh();
    }

    public void Refresh()
    {
        int  currentLevel = GameManager.Instance.GetUpgradeLevel(upgradeType);
        int  maxLevel     = UpgradeConfig.GetMaxLevel(upgradeType);
        bool isMaxed      = currentLevel >= maxLevel;
        int  cost         = isMaxed ? 0 : UpgradeConfig.GetCost(upgradeType, currentLevel);
        int  scraps       = GameManager.Instance.CurrentProfile?.totalScraps ?? 0;

        if (levelText != null)
            levelText.SetText(isMaxed ? "MAX" : "Lv " + currentLevel);

        if (costText != null)
            costText.SetText(isMaxed ? "—" : cost + " scraps");

        if (valueText != null)
            valueText.SetText(GetValueString(currentLevel));

        // Desabilita botão se maxado ou sem scraps
        buyButton.interactable = !isMaxed && scraps >= cost;
    }

    private string GetValueString(int level)
    {
        return upgradeType switch
        {
            UpgradeType.ClickDamage   => "DMG " + UpgradeConfig.GetClickDamage(level),
            UpgradeType.TowerHP       => "HP " + UpgradeConfig.GetTowerHP(level),
            UpgradeType.RunDuration   => UpgradeConfig.GetRunDuration(level) + "s",
            UpgradeType.ScrapDropRate => "x" + UpgradeConfig.GetScrapDrop(level).ToString("F2"),
            UpgradeType.SubTowerSlots => UpgradeConfig.GetSubTowerSlots(level) + " slots",
            UpgradeType.ClickArea     => "R " + UpgradeConfig.GetClickArea(level).ToString("F1"),
            _                         => ""
        };
    }
}
