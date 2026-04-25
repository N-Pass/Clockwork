// PlacementOverlay.cs
// Overlay que cobre o mapa durante o modo de alocação de torres.
// Mostra células válidas brilhando e detecta clique do jogador.
//
// Hierarquia:
//   PlacementOverlay (fullscreen panel)
//   ├─ GridContainer (filho — contém os botões de célula gerados)
//   └─ CancelButton ("Cancelar")

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlacementOverlay : MonoBehaviour
{
    public static PlacementOverlay Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private Button        cancelButton;
    [SerializeField] private GameObject    cellButtonPrefab; // botão simples com Image

    [Header("Cores das células")]
    [SerializeField] private Color colorValid    = new Color(0.2f, 0.9f, 0.2f, 0.5f);
    [SerializeField] private Color colorOccupied = new Color(0.9f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color colorInvalid  = new Color(0.3f, 0.3f, 0.3f, 0.2f);

    // Células inválidas — zona da torre principal e bordas
    private static readonly HashSet<(int, int)> invalidCells = new HashSet<(int, int)>
    {
        // Torre principal F3-G4 (col 5-6, row 2-3)
        (5,2),(6,2),(5,3),(6,3),
        // Linha 1 (row 0) e Linha 6 (row 5)
        (0,0),(1,0),(2,0),(3,0),(4,0),(5,0),(6,0),(7,0),(8,0),(9,0),(10,0),(11,0),
        (0,5),(1,5),(2,5),(3,5),(4,5),(5,5),(6,5),(7,5),(8,5),(9,5),(10,5),(11,5),
        // Coluna A (col 0) e Coluna L (col 11)
        (0,0),(0,1),(0,2),(0,3),(0,4),(0,5),
        (11,0),(11,1),(11,2),(11,3),(11,4),(11,5),
    };

    private Action<int, int> onCellSelected; // callback: (col, row)
    private HashSet<(int,int)> occupiedCells = new HashSet<(int,int)>();

    private void Awake()
    {
        Instance = this;
        cancelButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Mostrar overlay — chamado pelo SetupPanelUI ao clicar num slot
    // ------------------------------------------------------------------
    public void Show(HashSet<(int,int)> occupied, Action<int,int> onSelected)
    {
        occupiedCells  = occupied ?? new HashSet<(int,int)>();
        onCellSelected = onSelected;
        gameObject.SetActive(true);
        BuildGrid();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        onCellSelected = null;
    }

    // ------------------------------------------------------------------
    // Gera botões para cada célula válida
    // ------------------------------------------------------------------
    private void BuildGrid()
    {
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        for (int row = 0; row < WaypointGrid.Rows; row++)
        {
            for (int col = 0; col < WaypointGrid.Cols; col++)
            {
                bool invalid  = invalidCells.Contains((col, row));
                bool occupied = occupiedCells.Contains((col, row));

                GameObject cell = Instantiate(cellButtonPrefab, gridContainer);

                // Posiciona no world space convertido para screen space
                Vector3 worldPos = WaypointGrid.GridToWorld(col, row);
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

                RectTransform rt = cell.GetComponent<RectTransform>();
                rt.position = screenPos;
                rt.sizeDelta = new Vector2(
                    WaypointGrid.CellWidth  * 100f, // ajustar para pixels
                    Mathf.Abs(WaypointGrid.CellHeight) * 100f
                );

                Image img = cell.GetComponent<Image>();
                Button btn = cell.GetComponent<Button>();

                if (invalid)
                {
                    img.color         = colorInvalid;
                    btn.interactable  = false;
                }
                else if (occupied)
                {
                    img.color         = colorOccupied;
                    btn.interactable  = true; // clicável para remover
                }
                else
                {
                    img.color         = colorValid;
                    btn.interactable  = true;
                }

                int c = col, r = row; // captura para closure
                btn.onClick.AddListener(() => OnCellClicked(c, r, occupied));
            }
        }
    }

    private void OnCellClicked(int col, int row, bool isOccupied)
    {
        onCellSelected?.Invoke(col, row);
        // SetupPanelUI decide se é colocar ou remover baseado no estado
    }
}
