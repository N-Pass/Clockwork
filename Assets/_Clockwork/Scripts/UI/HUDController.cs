// HUDController.cs
// Controla a Cena 2 — HUD entre runs.
//
// Hierarquia esperada no Canvas:
//   Canvas
//   ├─ LeftPanel (2/3 da tela)
//   │   ├─ PageContainer
//   │   │   ├─ Page1 (arvore de upgrades)
//   │   │   ├─ Page2 (vazia)
//   │   │   └─ Page3 (vazia)
//   │   └─ NavButtons
//   │       ├─ BtnPage1, BtnPage2, BtnPage3
//   ├─ RightPanel (1/3 da tela)
//   │   ├─ StatsPanel → StatsText
//   │   ├─ ScrapsText
//   │   └─ ActionPanel → RunButton, MenuButton
//   └─ ConfirmModal
//       ├─ ModalMessage, YesButton, NoButton

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Paginas")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;

    [Header("Botoes de navegacao")]
    [SerializeField] private Button btnPage1;
    [SerializeField] private Button btnPage2;
    [SerializeField] private Button btnPage3;

    [Header("Arvore de upgrades")]
    [SerializeField] private UpgradeTreeSO  upgradeTree;
    [SerializeField] private RectTransform  treeContainer;
    [SerializeField] private GameObject     nodeButtonPrefab;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI scrapsText;

    [Header("Botoes de acao")]
    [SerializeField] private Button runButton;
    [SerializeField] private Button menuButton;

    [Header("Modal de confirmacao")]
    [SerializeField] private GameObject      confirmModal;
    [SerializeField] private TextMeshProUGUI modalMessage;
    [SerializeField] private Button          modalYesButton;
    [SerializeField] private Button          modalNoButton;

    private System.Action pendingConfirmAction;
    private Dictionary<string, NodeButtonUI> nodeButtons = new Dictionary<string, NodeButtonUI>();

    private void Awake()
    {
        Instance = this;

        btnPage1.onClick.AddListener(() => ShowPage(1));
        btnPage2.onClick.AddListener(() => ShowPage(2));
        btnPage3.onClick.AddListener(() => ShowPage(3));

        modalYesButton.onClick.AddListener(OnModalYes);
        modalNoButton.onClick.AddListener(OnModalNo);

        runButton.onClick.AddListener(() => ShowConfirm(
            "Iniciar uma nova run?",
            () => GameManager.Instance.StartRun()
        ));

        menuButton.onClick.AddListener(() => ShowConfirm(
            "Voltar ao menu principal?\nProgresso sera salvo.",
            () => GameManager.Instance.GoToMainMenu()
        ));
    }

    private void Start()
    {
        confirmModal.SetActive(false);
        ShowPage(1);
        BuildUpgradeTree();
        RefreshStats();
        RefreshScraps();
    }

    // ------------------------------------------------------------------
    // Navegacao
    // ------------------------------------------------------------------
    private void ShowPage(int page)
    {
        page1.SetActive(page == 1);
        page2.SetActive(page == 2);
        page3.SetActive(page == 3);
        SetNavActive(btnPage1, page == 1);
        SetNavActive(btnPage2, page == 2);
        SetNavActive(btnPage3, page == 3);
    }

    private void SetNavActive(Button btn, bool active)
    {
        ColorBlock cb   = btn.colors;
        cb.normalColor  = active ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
        btn.colors      = cb;
    }

    // ------------------------------------------------------------------
    // Arvore dinamica
    // ------------------------------------------------------------------
    private void BuildUpgradeTree()
    {
        if (upgradeTree == null || upgradeTree.rootNode == null)
        {
            Debug.LogWarning("[HUDController] UpgradeTreeSO nao atribuido.");
            return;
        }

        foreach (Transform child in treeContainer)
            Destroy(child.gameObject);
        nodeButtons.Clear();

        CreateNodeButton(upgradeTree.rootNode, Vector2.zero, null);
    }

    private void CreateNodeButton(UpgradeNodeSO node, Vector2 position, UpgradeNodeSO parent)
    {
        if (node == null || nodeButtons.ContainsKey(node.nodeID)) return;

        GameObject    go  = Instantiate(nodeButtonPrefab, treeContainer);
        RectTransform rt  = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        NodeButtonUI btn = go.GetComponent<NodeButtonUI>();
        btn.Initialize(node, this);
        nodeButtons[node.nodeID] = btn;

        bool isPurchased  = IsNodePurchased(node);
        bool isRoot       = parent == null;
        bool parentBought = parent == null || IsNodePurchased(parent);

        btn.SetState(
            purchased:  isPurchased,
            visible:    isRoot || parentBought,
            affordable: CanAfford(node)
        );

        if (isPurchased || isRoot)
        {
            foreach (UpgradeNodeSO child in node.children)
                CreateNodeButton(child, position + child.positionOffset, node);
        }
    }

    // ------------------------------------------------------------------
    // Compra
    // ------------------------------------------------------------------
    public void RequestPurchase(UpgradeNodeSO node)
    {
        if (node == null) return;
        ShowConfirm(
            $"Comprar {node.nodeName} por {node.cost} scraps?",
            () => ExecutePurchase(node)
        );
    }

    private void ExecutePurchase(UpgradeNodeSO node)
    {
        ProfileData profile = GameManager.Instance.CurrentProfile;
        int         slot    = GameManager.Instance.CurrentSlot;

        if (UpgradeSystem.Purchase(node, profile, slot))
        {
            BuildUpgradeTree();
            RefreshStats();
            RefreshScraps();
        }
    }

    // ------------------------------------------------------------------
    // Modal
    // ------------------------------------------------------------------
    public void ShowConfirm(string message, System.Action onYes)
    {
        pendingConfirmAction = onYes;
        modalMessage.SetText(message);
        confirmModal.SetActive(true);
    }

    private void OnModalYes()
    {
        confirmModal.SetActive(false);
        pendingConfirmAction?.Invoke();
        pendingConfirmAction = null;
    }

    private void OnModalNo()
    {
        confirmModal.SetActive(false);
        pendingConfirmAction = null;
    }

    // ------------------------------------------------------------------
    // Stats
    // ------------------------------------------------------------------
    public void RefreshStats()
    {
        if (statsText == null) return;

        ProfileData p   = GameManager.Instance?.CurrentProfile ?? new ProfileData();
        RunContext  ctx = RunContext.FromProfile(p, upgradeTree);

        statsText.SetText(
            "<b>STATUS</b>\n\n" +
            $"Vida da Torre:         {ctx.towerHP}\n" +
            $"Dano:                  {ctx.clickDamage}\n" +
            $"Velocidade do Tiro:    {ctx.projectileSpeed:F1}\n" +
            $"Tiros por Segundo:     {ctx.fireRate:F1}\n" +
            $"Duracao da Run:        {ctx.runDuration:F0}s\n" +
            $"Slots SubTorre:        {ctx.subTowerSlots}\n" +
            $"Slots Metralhadora:    {ctx.machineGunSlots}"
        );
    }

    public void RefreshScraps()
    {
        if (scrapsText == null) return;
        int scraps = GameManager.Instance?.CurrentProfile?.totalScraps ?? 0;
        scrapsText.SetText($"Scraps: {scraps}");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private bool IsNodePurchased(UpgradeNodeSO node)
        => GameManager.Instance?.CurrentProfile?.purchasedNodeIDs.Contains(node.nodeID) ?? false;

    private bool CanAfford(UpgradeNodeSO node)
        => (GameManager.Instance?.CurrentProfile?.totalScraps ?? 0) >= node.cost;
}
