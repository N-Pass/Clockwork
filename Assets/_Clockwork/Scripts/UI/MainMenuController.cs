// MainMenuController.cs
// Controla a Cena 1 — MainMenu com sistema de perfis.
//
// Fluxo:
//   Botões Play/Quit → Play mostra ProfileSlotsPanel
//   Slot vazio → modal InitProfile → Yes → GameManager.CreateProfile(slot)
//   Slot cheio → GameManager.LoadProfile(slot)
//   Botão X → modal DeleteProfile → Yes → GameManager.DeleteProfile(slot) + refresh
//
// Hierarquia esperada no Canvas:
//   MainMenuController
//   ├─ MainMenuPanel
//   │   ├─ PlayButton
//   │   └─ QuitButton
//   ├─ ProfileSlotsPanel
//   │   ├─ Slot0, Slot1, Slot2
//   │   │   ├─ LoadButton      (clique principal)
//   │   │   ├─ DeleteButton    (X vermelho — ativo só se perfil existe)
//   │   │   └─ FilledIndicator (mostra info do perfil quando preenchido)
//   ├─ InitProfileModal
//   │   ├─ YesButton
//   │   └─ NoButton
//   └─ DeleteProfileModal
//       ├─ YesButton
//       └─ NoButton

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Painel principal
    // ------------------------------------------------------------------
    [Header("Painel principal")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button     playButton;
    [SerializeField] private Button     quitButton;

    // ------------------------------------------------------------------
    // Painel de perfis
    // ------------------------------------------------------------------
    [Header("Perfis")]
    [SerializeField] private GameObject profileSlotsPanel;
    [SerializeField] private SlotUI[]   slots;              // 3 elementos, arrastar no Inspector

    // ------------------------------------------------------------------
    // Modal — Inicializar perfil
    // ------------------------------------------------------------------
    [Header("Modal: inicializar perfil")]
    [SerializeField] private GameObject initModal;
    [SerializeField] private Button     initYesButton;
    [SerializeField] private Button     initNoButton;

    // ------------------------------------------------------------------
    // Modal — Deletar perfil
    // ------------------------------------------------------------------
    [Header("Modal: deletar perfil")]
    [SerializeField] private GameObject deleteModal;
    [SerializeField] private Button     deleteYesButton;
    [SerializeField] private Button     deleteNoButton;
    [SerializeField] private TextMeshProUGUI deleteWarningText;

    // ------------------------------------------------------------------
    // Estado interno
    // ------------------------------------------------------------------
    private int pendingSlot = -1;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        // Painel principal
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(() => Application.Quit());

        // Modal Inicializar
        initYesButton.onClick.AddListener(OnInitYes);
        initNoButton.onClick.AddListener(OnInitNo);

        // Modal Deletar
        deleteYesButton.onClick.AddListener(OnDeleteYes);
        deleteNoButton.onClick.AddListener(OnDeleteNo);

        // Configura os 3 slots
        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i; // captura para closure
            slots[i].loadButton.onClick.AddListener(()   => OnSlotClicked(slotIndex));
            slots[i].deleteButton.onClick.AddListener(() => OnDeleteClicked(slotIndex));
        }
    }

    private void Start()
    {
        ShowMainMenu();
    }

    // ------------------------------------------------------------------
    // Navegação
    // ------------------------------------------------------------------
    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        profileSlotsPanel.SetActive(false);
        initModal.SetActive(false);
        deleteModal.SetActive(false);
    }

    private void ShowProfileSlots()
    {
        mainMenuPanel.SetActive(false);
        profileSlotsPanel.SetActive(true);
        initModal.SetActive(false);
        deleteModal.SetActive(false);

        RefreshSlots();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool hasProfile = GameManager.Instance.HasProfile(i);
            slots[i].SetState(hasProfile);
        }
    }

    // ------------------------------------------------------------------
    // Eventos de botão
    // ------------------------------------------------------------------
    private void OnPlayClicked()
    {
        ShowProfileSlots();
    }

    private void OnSlotClicked(int slot)
    {
        if (GameManager.Instance.HasProfile(slot))
        {
            // Perfil existe → carrega diretamente
            GameManager.Instance.LoadProfile(slot);
        }
        else
        {
            // Perfil vazio → abre modal de inicialização
            pendingSlot = slot;
            initModal.SetActive(true);
        }
    }

    private void OnDeleteClicked(int slot)
    {
        pendingSlot = slot;
        deleteWarningText.SetText(
            "Delete profile " + (slot + 1) + "?\n" +
            "<size=80%><color=#FF4444>WARNING! Once deleted it cannot be recovered.</color></size>"
        );
        deleteModal.SetActive(true);
    }

    private void OnInitYes()
    {
        initModal.SetActive(false);
        GameManager.Instance.CreateProfile(pendingSlot);
        // GameManager.CreateProfile chama SceneManager.LoadScene(HUD) automaticamente
    }

    private void OnInitNo()
    {
        initModal.SetActive(false);
        pendingSlot = -1;
    }

    private void OnDeleteYes()
    {
        deleteModal.SetActive(false);
        GameManager.Instance.DeleteProfile(pendingSlot);
        pendingSlot = -1;
        RefreshSlots();
    }

    private void OnDeleteNo()
    {
        deleteModal.SetActive(false);
        pendingSlot = -1;
    }
}

// ------------------------------------------------------------------
// SlotUI — dados de um slot de perfil (arrastar componentes no Inspector)
// ------------------------------------------------------------------
[System.Serializable]
public class SlotUI
{
    public Button          loadButton;      // cobre o slot inteiro
    public Button          deleteButton;    // X vermelho no canto
    public GameObject      emptyIndicator;  // "+" cinza
    public GameObject      filledIndicator; // info do perfil (opcional)
    public TextMeshProUGUI filledText;      // ex: "Peças: 2/5 | Runs: 7"

    public void SetState(bool hasSave)
    {
        emptyIndicator?.SetActive(!hasSave);
        filledIndicator?.SetActive(hasSave);
        deleteButton.gameObject.SetActive(hasSave);

        if (hasSave && filledText != null)
        {
            // Texto básico — pode expandir com dados reais do SaveSystem
            filledText.SetText("Continue");
        }
    }
}
