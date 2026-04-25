// ProfileData.cs
// Estrutura serializada que representa um perfil de save.
// Gravada em JSON pelo SaveSystem.
// NUNCA referencie MonoBehaviour aqui — é só dados.

using System;
using System.Collections.Generic;

[Serializable]
public class ProfileData
{
    // Moeda acumulada entre runs
    public int totalScraps = 0;

    // Nós de upgrade comprados — guardados por nodeID
    public List<string> purchasedNodeIDs = new List<string>();

    // Nível de cada upgrade — índice corresponde ao enum UpgradeType
    // Inicializado com zeros (sem upgrades)
    public int[] upgradeLevels = new int[Enum.GetValues(typeof(UpgradeType)).Length];

    // IDs das peças do relógio coletadas (drops de boss)
    // 0 = peça 1, 1 = peça 2, etc.
    public List<int> clockPiecesCollected = new List<int>();

    // Jogo finalizado (estágio final completo)
    public bool gameCompleted = false;

    // Estatísticas opcionais (para tela de resultados / Steam achievements futuros)
    public int totalRuns = 0;
    public int totalKills = 0;
    public int totalScrapsEverEarned = 0;

    // -------------------------------------------------------------------------
    // Helpers de acesso rápido
    // -------------------------------------------------------------------------

    public int GetUpgradeLevel(UpgradeType type)
        => upgradeLevels[(int)type];

    public void SetUpgradeLevel(UpgradeType type, int level)
        => upgradeLevels[(int)type] = level;

    public bool HasClockPiece(int pieceId)
        => clockPiecesCollected.Contains(pieceId);

    public bool HasAllPieces(int totalPieces)
        => clockPiecesCollected.Count >= totalPieces;
}
