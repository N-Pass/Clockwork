// WaypointManager.cs
// MonoBehaviour singleton que configura e expõe a grade de waypoints.
// Permite ajustar cellSize e origem no Inspector com visualização de Gizmos.
//
// Colocar na cena Gameplay como GameObject "WaypointManager".
// Os inimigos não referenciam este script — usam WaypointGrid diretamente.
// Este script só existe para facilitar o ajuste visual da grade.

using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    [Header("Configuração da grade")]
    [SerializeField] private Vector3 gridOrigin  = new Vector3(-14.34f, 8f, 0f); // A1 = top-left
    [SerializeField] private float   cellWidth   = 2.39f;
    [SerializeField] private float   cellHeight  = -2.69f;

    [Header("Gizmos")]
    [SerializeField] private bool  showGrid      = true;
    [SerializeField] private bool  showLabels    = true;
    [SerializeField] private Color gridColor     = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color towerColor    = new Color(0.3f, 0.8f, 1f, 0.4f);
    [SerializeField] private Color firstRowColor = new Color(1f, 0.6f, 0.2f, 0.4f);

    private void Awake()
    {
        Instance = this;

        // Propaga valores para a classe estática
        WaypointGrid.GridOrigin  = gridOrigin;
        WaypointGrid.CellWidth   = cellWidth;
        WaypointGrid.CellHeight  = cellHeight;
    }

    // ------------------------------------------------------------------
    // Gizmos — visualização da grade no Scene View
    // ------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (!showGrid) return;

        // Propaga valores mesmo fora de play mode
        WaypointGrid.GridOrigin = gridOrigin;
        WaypointGrid.CellWidth  = cellWidth;
        WaypointGrid.CellHeight = cellHeight;

        for (int col = 0; col < WaypointGrid.Cols; col++)
        {
            for (int row = 0; row < WaypointGrid.Rows; row++)
            {
                Vector3 center = WaypointGrid.GridToWorld(col, row);
                Vector3 size   = new Vector3(cellWidth * 0.9f, cellHeight * 0.9f, 0f);

                bool isTower    = (col >= 5 && col <= 6 && row >= 2 && row <= 3);
                bool isFirstRow = (row == 0 || row == WaypointGrid.Rows - 1
                                || col == 0 || col == WaypointGrid.Cols - 1);

                Gizmos.color = isTower    ? towerColor    :
                               isFirstRow ? firstRowColor :
                                            gridColor;

                Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
                if (showLabels)
                {
                    string colLabel = ((char)('A' + col)).ToString();
                    string label    = colLabel + (row + 1);
                    UnityEditor.Handles.Label(center + Vector3.up * (cellHeight * 0.25f),
                        label,
                        new GUIStyle { fontSize = 8, normal = { textColor = Color.white * 0.6f } });
                }
#endif
            }
        }
    }
}
