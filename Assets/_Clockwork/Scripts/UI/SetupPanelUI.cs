// SetupPanelUI.cs
// Gerencia a UI de subarmas na fase de Setup.
// Aparece antes do jogador clicar Start na cena Gameplay.
//
// Hierarquia esperada:
//   SetupPanel
//   ├─ SubWeaponsPanel (canto inferior direito)
//   │   ├─ SlotCountText  ("1/4 slots")
//   │   ├─ TurretRow
//   │   │   ├─ TurretLabel
//   │   │   └─ TurretSlots (4x SubWeaponSlot prefab)
//   │   └─ GunRow
//   │       ├─ GunLabel
//   │       └─ GunSlots (4x SubWeaponSlot prefab)
//   └─ StartButton

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupPanelUI : MonoBehaviour
{
    public static SetupPanelUI Instance { get; private set; }

    [Header("Slots de Torre")]
    [SerializeField] private GameObject  turretRow;
    [SerializeField] private SubWeaponSlot[] turretSlots; // 4 slots

    [Header("Slots de Metralhadora")]
    [SerializeField] private GameObject  gunRow;
    [SerializeField] private SubWeaponSlot[] gunSlots; // 4 slots

    [Header("Contador de slots")]
    [SerializeField] private TextMeshProUGUI slotCountText;

    [Header("Botão Start")]
    [SerializeField] private Button startButton;

    [Header("Modal de confirmação")]
    [SerializeField] private GameObject      confirmModal;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button          confirmYes;
    [SerializeField] private Button          confirmNo;

    [Header("Prefabs")]
    [SerializeField] private Tower subTowerPrefab;

    [Header("Pool de SubTorres")]
    [SerializeField] private Tower[] towerPool;

    // ------------------------------------------------------------------
    // Estado interno
    // ------------------------------------------------------------------
    private int maxSlots       = 0; // total de slots desbloqueados
    private int usedSlots      = 0; // slots em uso

    private int availableTurrets    = 0; // torres desbloqueadas
    private int availableGuns       = 0; // metralhadoras desbloqueadas

    // Torres colocadas: slot index → (col, row)
    private Dictionary<int, (int col, int row)> placedTurrets
        = new Dictionary<int, (int, int)>();
    private Dictionary<int, Tower> slotToTower = new Dictionary<int, Tower>();

    // Metralhadoras ativas: slot index → ativada
    private HashSet<int> activeGuns = new HashSet<int>();

    // Slot sendo configurado no momento
    private SubWeaponSlot pendingSlot;
    private bool          pendingIsRemoval;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        Instance = this;

        startButton.onClick.AddListener(OnStartClicked);
        confirmYes.onClick.AddListener(OnConfirmYes);
        confirmNo.onClick.AddListener(OnConfirmNo);
        confirmModal.SetActive(false);

        // Inicializa slots
        for (int i = 0; i < turretSlots.Length; i++)
            turretSlots[i].Initialize(i, this);
        for (int i = 0; i < gunSlots.Length; i++)
            gunSlots[i].Initialize(i, this);
    }

    private void Start()
    {
        if (RunManager.Instance?.Context == null)
        {
            StartCoroutine(WaitForContext());
            return;
        }

        Initialize();

        /*// Lê RunContext para saber quantos slots/torres/guns estão disponíveis
        RunContext ctx = RunManager.Instance?.Context;
        if (ctx == null)
        {
            Debug.LogError("[SetupPanelUI] Context null.");
            return;
        }
        availableTurrets = ctx.subTowerSlots;
        availableGuns    = ctx.machineGunSlots;

        // Primeiro slot é dado automaticamente ao ter qualquer subarma
        //maxSlots = Mathf.Clamp((availableTurrets > 0 || availableGuns > 0) ? 1 : 0, 0, 4);

        maxSlots = Mathf.Clamp(availableTurrets + availableGuns, 0, 4);

        // Slots adicionais vêm de upgrades (já incluídos no ctx)
        // ctx.subTowerSlots e ctx.machineGunSlots já representam o total comprado

        // Mostra/esconde linhas baseado no que está disponível
        turretRow.SetActive(availableTurrets > 0);
        gunRow.SetActive(availableGuns    > 0);

        RefreshSlots();
        RefreshCounter();*/

    }

    private System.Collections.IEnumerator WaitForContext()
    {
        // Espera até o Context estar pronto
        while (RunManager.Instance?.Context == null)
            yield return null;

        Initialize();
    }

    private void Initialize()
    {
        RunContext ctx = RunManager.Instance.Context;

        availableTurrets = ctx.subTowerSlots;
        availableGuns = ctx.machineGunSlots;
        maxSlots = Mathf.Clamp(availableTurrets + availableGuns, 0, 4);

        turretRow.SetActive(availableTurrets > 0);
        gunRow.SetActive(availableGuns > 0);

        RefreshSlots();
        RefreshCounter();
    }

    // ------------------------------------------------------------------
    // Refresh visual dos slots
    // ------------------------------------------------------------------
    private void RefreshSlots()
    {
        // Turret slots
        for (int i = 0; i < turretSlots.Length; i++)
        {
            if (i >= availableTurrets)
            {
                turretSlots[i].SetState(SubWeaponSlot.SlotState.Locked);
            }
            else if (placedTurrets.ContainsKey(i))
            {
                turretSlots[i].SetState(SubWeaponSlot.SlotState.Turret);
            }
            else if (usedSlots < maxSlots)
            {
                turretSlots[i].SetState(SubWeaponSlot.SlotState.Empty);
            }
            else
            {
                turretSlots[i].SetState(SubWeaponSlot.SlotState.Locked);
            }
        }

        // Gun slots
        for (int i = 0; i < gunSlots.Length; i++)
        {
            if (i >= availableGuns)
            {
                gunSlots[i].SetState(SubWeaponSlot.SlotState.Locked);
            }
            else if (activeGuns.Contains(i))
            {
                gunSlots[i].SetState(SubWeaponSlot.SlotState.Gun);
            }
            else if (usedSlots < maxSlots)
            {
                gunSlots[i].SetState(SubWeaponSlot.SlotState.Empty);
            }
            else
            {
                gunSlots[i].SetState(SubWeaponSlot.SlotState.Locked);
            }
        }
    }

    private void RefreshCounter()
    {
        slotCountText.SetText($"{usedSlots}/{maxSlots} slots");
    }

    // ------------------------------------------------------------------
    // Clique num slot — chamado pelo SubWeaponSlot
    // ------------------------------------------------------------------
    public void OnSlotClicked(SubWeaponSlot slot)
    {
        pendingSlot = slot;

        bool isTurretSlot = IsTurretSlot(slot);
        bool isActive     = slot.CurrentState == SubWeaponSlot.SlotState.Turret ||
                            slot.CurrentState == SubWeaponSlot.SlotState.Gun;

        if (isActive)
        {
            // Slot ativo — pergunta se quer remover
            pendingIsRemoval = true;
            string type = isTurretSlot ? "torre" : "metralhadora";
            ShowConfirm($"Remover {type} do slot {slot.SlotIndex + 1}?");
        }
        else
        {
            // Slot vazio — ativa
            pendingIsRemoval = false;

            if (isTurretSlot)
            {
                // Torre — abre overlay de posicionamento
                var occupied = GetOccupiedCells();
                PlacementOverlay.Instance.Show(occupied, (col, row) =>
                {
                    PlacementOverlay.Instance.Hide();
                    ShowConfirmPlacement(col, row);
                });
            }
            else
            {
                // Metralhadora — ativa diretamente
                ShowConfirm($"Ativar metralhadora no slot {slot.SlotIndex + 1}?");
            }
        }
    }

    // ------------------------------------------------------------------
    // Confirmação de posicionamento de torre
    // ------------------------------------------------------------------
    private int pendingCol, pendingRow;

    private void ShowConfirmPlacement(int col, int row)
    {
        pendingCol = col;
        pendingRow = row;
        string cellName = $"{(char)('A' + col)}{row + 1}";
        ShowConfirm($"Colocar torre em {cellName}?");
    }

    // ------------------------------------------------------------------
    // Modal genérico
    // ------------------------------------------------------------------
    private void ShowConfirm(string message)
    {
        confirmText.SetText(message);
        confirmModal.SetActive(true);
    }

    private void OnConfirmYes()
    {
        confirmModal.SetActive(false);

        if (pendingIsRemoval)
            RemoveSubWeapon(pendingSlot);
        else
            PlaceSubWeapon(pendingSlot);

        pendingSlot = null;
    }

    private void OnConfirmNo()
    {
        confirmModal.SetActive(false);
        pendingSlot = null;
    }

    // ------------------------------------------------------------------
    // Colocar subarma
    // ------------------------------------------------------------------
    private void PlaceSubWeapon(SubWeaponSlot slot)
    {
        if (slot == null || usedSlots >= maxSlots) return;

        if (IsTurretSlot(slot))
        {
            // Instancia a torre na posição do grid
            Vector3 worldPos = WaypointGrid.GridToWorld(pendingCol, pendingRow);
            Tower tower = GetNextAvailableTower();
            if (tower == null) { Debug.LogWarning("[SetupPanelUI] Nenhuma torre disponivel."); return; }
            tower.transform.position = worldPos;
            tower.gameObject.SetActive(true);
            RunManager.Instance.RegisterSubTower(tower, pendingCol, pendingRow);

            placedTurrets[slot.SlotIndex] = (pendingCol, pendingRow);
            slotToTower[slot.SlotIndex]   = tower;
        }
        else
        {
            // Ativa a metralhadora correspondente
            ActivateMachineGun(slot.SlotIndex);
            activeGuns.Add(slot.SlotIndex);
        }

        usedSlots++;
        RefreshSlots();
        RefreshCounter();
    }

    // ------------------------------------------------------------------
    // Remover subarma
    // ------------------------------------------------------------------
    private void RemoveSubWeapon(SubWeaponSlot slot)
    {
        if (slot == null) return;

        if (IsTurretSlot(slot) && placedTurrets.ContainsKey(slot.SlotIndex))
        {
            // Remove torre do mapa
            Vector3 pos = WaypointGrid.GridToWorld(
                placedTurrets[slot.SlotIndex].col,
                placedTurrets[slot.SlotIndex].row
            );
            // Encontra e destrói a torre nessa posição
            Collider2D col = Physics2D.OverlapPoint(pos);
            if (col != null)
            {
                Tower t = col.GetComponent<Tower>();
                if (t != null) Destroy(t.gameObject);
            }
            placedTurrets.Remove(slot.SlotIndex);
        }
        else if (!IsTurretSlot(slot) && activeGuns.Contains(slot.SlotIndex))
        {
            DeactivateMachineGun(slot.SlotIndex);
            activeGuns.Remove(slot.SlotIndex);
        }

        usedSlots = Mathf.Max(0, usedSlots - 1);
        RefreshSlots();
        RefreshCounter();
    }

    // ------------------------------------------------------------------
    // Metralhadoras — ativa/desativa filhos da MainTower
    // Posições fixas: slot 0=F3, 1=G4, 2=F4, 3=G3
    // ------------------------------------------------------------------
    private static readonly (int col, int row)[] gunPositions =
    {
        (5, 2), // F3 — slot 0
        (6, 3), // G4 — slot 1
        (5, 3), // F4 — slot 2
        (6, 2), // G3 — slot 3
    };

    private void ActivateMachineGun(int index)
    {
        MachineGunController[] guns = RunManager.Instance
            .GetMainTower().GetComponentsInChildren<MachineGunController>(true);

        if (index < guns.Length)
            guns[index].gameObject.SetActive(true);
    }

    private void DeactivateMachineGun(int index)
    {
        MachineGunController[] guns = RunManager.Instance
            .GetMainTower().GetComponentsInChildren<MachineGunController>(true);

        if (index < guns.Length)
            guns[index].gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private bool IsTurretSlot(SubWeaponSlot slot)
    {
        foreach (var s in turretSlots)
            if (s == slot) return true;
        return false;
    }

    private Tower GetNextAvailableTower()
    {
        foreach (Tower t in towerPool)
            if (t != null && !t.gameObject.activeSelf)
                return t;
        return null;
    }

    private HashSet<(int, int)> GetOccupiedCells()
    {
        var occupied = new HashSet<(int, int)>();
        foreach (var kv in placedTurrets)
            occupied.Add(kv.Value);
        return occupied;
    }

    // ------------------------------------------------------------------
    // Start button
    // ------------------------------------------------------------------
    private void OnStartClicked()
    {
        RunManager.Instance.BeginRun();
        gameObject.SetActive(false);
    }
}
