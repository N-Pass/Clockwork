// UpgradeConfig.cs
// Tabela estática de custos e valores por nível de upgrade.
// Centraliza o balanceamento — editar aqui reflete em todo o jogo.

using UnityEngine;

public static class UpgradeConfig
{
    // -------------------------------------------------------------------------
    // Custo em scraps para comprar o próximo nível
    // índice = nível atual (0 = comprar nível 1, 1 = comprar nível 2, etc.)
    // -------------------------------------------------------------------------
    private static readonly int[][] costs = new int[][]
    {
        /* ClickDamage   */ new int[] { 10, 25, 50, 100, 200 },
        /* TowerHP       */ new int[] { 15, 35, 70, 140, 300 },
        /* RunDuration   */ new int[] { 20, 40, 80, 160, 320 },
        /* ScrapDropRate */ new int[] { 12, 30, 60, 120, 250 },
        /* SubTowerSlots */ new int[] { 30, 80, 150 },
        /* ClickArea     */ new int[] { 10, 25, 50, 100 },
    };

    // -------------------------------------------------------------------------
    // Valores por nível — o que o upgrade entrega em cada nível
    // índice = nível comprado (0 = sem upgrade, 1 = nível 1, etc.)
    // -------------------------------------------------------------------------

    // Dano por clique: nível 0 = 1, nível 1 = 2, ...
    private static readonly int[] clickDamageValues    = { 1, 2, 4, 7, 11, 16 };

    // HP máximo da torre: nível 0 = 5, nível 1 = 10, ...
    private static readonly int[] towerHPValues        = { 5, 10, 18, 30, 50, 80 };

    // Duração da run em segundos: nível 0 = 5s, ..., nível 5 = 60s
    private static readonly int[] runDurationValues    = { 5, 12, 22, 35, 50, 60 };

    // Multiplicador de drop (x1.0, x1.25, x1.5, x2.0, x2.5, x3.0)
    private static readonly float[] scrapDropValues    = { 1f, 1.25f, 1.5f, 2f, 2.5f, 3f };

    // SubTorre slots disponíveis
    private static readonly int[] subTowerSlotValues   = { 0, 1, 2, 3 };

    // Raio do hitbox de clique (unidades Unity)
    private static readonly float[] clickAreaValues    = { 0.5f, 0.75f, 1.1f, 1.6f, 2.2f };

    // -------------------------------------------------------------------------
    // API pública
    // -------------------------------------------------------------------------

    public static int GetMaxLevel(UpgradeType type)
    {
        return costs[(int)type].Length;
    }

    public static bool HasNextLevel(UpgradeType type, int currentLevel)
    {
        return currentLevel < GetMaxLevel(type);
    }

    public static int GetCost(UpgradeType type, int currentLevel)
    {
        int[] typeCosts = costs[(int)type];
        if (currentLevel >= typeCosts.Length) return int.MaxValue;
        return typeCosts[currentLevel];
    }

    public static int GetClickDamage(int level)
        => clickDamageValues[Mathf.Clamp(level, 0, clickDamageValues.Length - 1)];

    public static int GetTowerHP(int level)
        => towerHPValues[Mathf.Clamp(level, 0, towerHPValues.Length - 1)];

    public static int GetRunDuration(int level)
        => runDurationValues[Mathf.Clamp(level, 0, runDurationValues.Length - 1)];

    public static float GetScrapDrop(int level)
        => scrapDropValues[Mathf.Clamp(level, 0, scrapDropValues.Length - 1)];

    public static int GetSubTowerSlots(int level)
        => subTowerSlotValues[Mathf.Clamp(level, 0, subTowerSlotValues.Length - 1)];

    public static float GetClickArea(int level)
        => clickAreaValues[Mathf.Clamp(level, 0, clickAreaValues.Length - 1)];
}
