// WaypointGrid.cs
// Define a grade 12x6 de waypoints e retorna listas válidas por direção.
//
// Layout da grade (câmera ortográfica size 8):
//   Colunas: A(0) até L(11) — horizontal
//   Linhas:  1(0) até 6(5)  — vertical
//   Torre:   F3-G3-F4-G4 (colunas 5-6, linhas 2-3 em índice 0-based)
//
// Uso:
//   WaypointGrid.GetFirstRow(Direction.North)     → pontos A1–L1
//   WaypointGrid.GetRandomZone(Direction.North)   → pontos A1–L3 exceto F/G na linha 3-4
//   WaypointGrid.GetFinalPoint(Direction.North)   → F3 ou G3
//   WaypointGrid.GetNextSafeWaypoint(current, dir, side) → respeita regra de colisão

using System.Collections.Generic;
using UnityEngine;

public static class WaypointGrid
{
    // ------------------------------------------------------------------
    // Direções — inferidas do nome do SpawnPoint
    // ------------------------------------------------------------------
    public enum Direction { North, South, East, West }

    // Lado horizontal — usado para regra anti-colisão Norte/Sul
    public enum HorizontalSide { Left, Right, Undefined }

    // ------------------------------------------------------------------
    // Dimensões da grade
    // Ajuste cellSize conforme o tamanho real do seu mapa no Unity
    // ------------------------------------------------------------------
    public static readonly int Cols = 12; // A=0 .. L=11
    public static readonly int Rows = 6;  // 1=0 .. 6=5

    // Posição do canto inferior esquerdo da grade (A6 em world space)
    // Ajuste para o seu mapa — com câmera size 8 e centro (0,0):
    //   largura visível ≈ 8 * aspect (≈14 para 16:9) → cellWidth ≈ 14/12 ≈ 1.16
    //   altura visível  ≈ 16 → cellHeight ≈ 16/6 ≈ 2.66
    // Esses valores são expostos para ajuste no WaypointManager (MonoBehaviour)
    public static float CellWidth  = 2.39f;
    public static float CellHeight = -2.69f;
    public static Vector3 GridOrigin = new Vector3(-14.34f, 8f, 0f); // A1 world pos (top-left)

    // Colunas da torre (índice 0-based): F=5, G=6
    private static readonly int TowerColMin = 5;
    private static readonly int TowerColMax = 6;

    // Linhas da torre (índice 0-based): linha3=2, linha4=3
    private static readonly int TowerRowMin = 2;
    private static readonly int TowerRowMax = 3;

    // ------------------------------------------------------------------
    // Converter coluna/linha para world position
    // ------------------------------------------------------------------
    public static Vector3 GridToWorld(int col, int row)
    {
        return GridOrigin + new Vector3(
            col * CellWidth  + CellWidth  * 0.5f,
            row * CellHeight + CellHeight * 0.5f,
            0f
        );
    }

    // ------------------------------------------------------------------
    // Inferir direção a partir do nome do SpawnPoint
    // ------------------------------------------------------------------
    public static Direction GetDirection(string spawnPointName)
    {
        if (spawnPointName.Contains("_N")) return Direction.North;
        if (spawnPointName.Contains("_S")) return Direction.South;
        if (spawnPointName.Contains("_E")) return Direction.East;
        if (spawnPointName.Contains("_W")) return Direction.West;
        return Direction.North; // fallback
    }

    // ------------------------------------------------------------------
    // Inferir lado horizontal a partir de uma coluna
    // ------------------------------------------------------------------
    public static HorizontalSide GetSide(int col)
    {
        if (col < TowerColMin)  return HorizontalSide.Left;
        if (col > TowerColMax)  return HorizontalSide.Right;
        return HorizontalSide.Undefined; // coluna da torre — não deve ocorrer
    }

    // ------------------------------------------------------------------
    // Primeira linha obrigatória por direção
    // Norte → linha 1 (row=0), colunas A–L
    // Sul   → linha 6 (row=5), colunas A–L
    // Leste → coluna L (col=11), linhas 1–6
    // Oeste → coluna A (col=0),  linhas 1–6
    // ------------------------------------------------------------------
    public static Vector3 GetFirstPoint(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
                return GridToWorld(Random.Range(0, Cols), 0);
            case Direction.South:
                return GridToWorld(Random.Range(0, Cols), Rows - 1);
            case Direction.East:
                return GridToWorld(Cols - 1, Random.Range(0, Rows));
            case Direction.West:
            default:
                return GridToWorld(0, Random.Range(0, Rows));
        }
    }

    // ------------------------------------------------------------------
    // Ponto final por direção
    // Norte → F3 ou G3 (row=2)
    // Sul   → F4 ou G4 (row=3)
    // Leste → G3 ou G4 (col=6)
    // Oeste → F3 ou F4 (col=5)
    // ------------------------------------------------------------------
    public static Vector3 GetFinalPoint(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
                return GridToWorld(Random.Range(TowerColMin, TowerColMax + 1), TowerRowMin);
            case Direction.South:
                return GridToWorld(Random.Range(TowerColMin, TowerColMax + 1), TowerRowMax);
            case Direction.East:
                return GridToWorld(TowerColMax, Random.Range(TowerRowMin, TowerRowMax + 1));
            case Direction.West:
            default:
                return GridToWorld(TowerColMin, Random.Range(TowerRowMin, TowerRowMax + 1));
        }
    }

    // ------------------------------------------------------------------
    // Waypoint intermediário aleatório válido
    // Respeita zona por direção + regra anti-colisão Norte/Sul
    // ------------------------------------------------------------------
    public static Vector3 GetRandomIntermediate(
        Direction dir,
        HorizontalSide side,    // lado atual do inimigo (relevante para N/S)
        out HorizontalSide newSide)
    {
        int col, row;

        switch (dir)
        {
            case Direction.North:
                // Zona: A1–L3, exceto F–G nas linhas 3–4
                row = Random.Range(0, TowerRowMin + 1); // linhas 1–3 (0–2)
                col = GetSafeCol(side, row);
                break;

            case Direction.South:
                // Zona: A4–L6, exceto F–G nas linhas 3–4
                row = Random.Range(TowerRowMax, Rows); // linhas 4–6 (3–5)
                col = GetSafeCol(side, row);
                break;

            case Direction.East:
                // Zona: H–K, linhas 1–6 (colunas 7–10)
                col = Random.Range(TowerColMax + 1, Cols - 1);
                row = Random.Range(0, Rows);
                break;

            case Direction.West:
            default:
                // Zona: A–E, linhas 1–6 (colunas 0–4)
                col = Random.Range(0, TowerColMin);
                row = Random.Range(0, Rows);
                break;
        }

        newSide = GetSide(col);
        return GridToWorld(col, row);
    }

    // ------------------------------------------------------------------
    // Seleciona coluna segura respeitando lado (regra Norte/Sul)
    // Se a linha é da zona da torre (TowerRowMin/Max), mantém o lado
    // ------------------------------------------------------------------
    private static int GetSafeCol(HorizontalSide side, int row)
    {
        bool isTowerRow = (row >= TowerRowMin && row <= TowerRowMax);

        if (!isTowerRow)
        {
            // Fora da zona da torre — qualquer coluna exceto F e G
            return RandomColExcludingTower();
        }

        // Na zona da torre — respeta o lado
        switch (side)
        {
            case HorizontalSide.Left:
                return Random.Range(0, TowerColMin); // A–E (0–4)

            case HorizontalSide.Right:
                return Random.Range(TowerColMax + 1, Cols); // H–L (7–11)

            case HorizontalSide.Undefined:
            default:
                // Ainda sem lado definido — sorteia e define
                bool goLeft = Random.value < 0.5f;
                return goLeft
                    ? Random.Range(0, TowerColMin)
                    : Random.Range(TowerColMax + 1, Cols);
        }
    }

    private static int RandomColExcludingTower()
    {
        // Sorteia coluna de 0–11 excluindo 5 e 6
        int col;
        do { col = Random.Range(0, Cols); }
        while (col >= TowerColMin && col <= TowerColMax);
        return col;
    }
}
