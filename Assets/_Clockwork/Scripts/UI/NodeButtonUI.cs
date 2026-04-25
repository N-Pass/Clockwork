// NodeButtonUI.cs
// Componente do prefab de cada botão da arvore de upgrades.
//
// Estrutura do prefab NodeButton:
//   NodeButton (GameObject)
//   ├─ Background (Image) — borda colorida
//   ├─ Icon (Image) — sprite do upgrade (opcional)
//   ├─ NameText (TextMeshProUGUI)
//   ├─ CostText (TextMeshProUGUI)
//   └─ Button (componente)
//
// Estados:
//   Invisivel  — pai nao comprado ainda
//   Disponivel — pai comprado, pode comprar
//   Sem scraps — pai comprado, nao pode pagar
//   Comprado   — ja comprado, borda verde

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NodeButtonUI : MonoBehaviour
{
    [Header("Componentes do prefab")]
    [SerializeField] private Image           background;
    [SerializeField] private Image           iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button          button;

    [Header("Cores de estado")]
    [SerializeField] private Color colorPurchased  = new Color(0.2f, 0.8f, 0.2f); // verde
    [SerializeField] private Color colorAffordable = new Color(0.9f, 0.9f, 0.9f); // branco
    [SerializeField] private Color colorCantAfford = new Color(0.5f, 0.5f, 0.5f); // cinza
    [SerializeField] private Color colorBg         = new Color(0.1f, 0.1f, 0.1f); // fundo escuro

    private UpgradeNodeSO  node;
    private HUDController  hud;
    private bool           isPurchased;

    // ------------------------------------------------------------------
    // Inicialização — chamada pelo HUDController ao criar o botão
    // ------------------------------------------------------------------
    public void Initialize(UpgradeNodeSO nodeSO, HUDController hudController)
    {
        node = nodeSO;
        hud  = hudController;

        // Textos
        if (nameText != null) nameText.SetText(node.nodeName);
        if (costText != null) costText.SetText(node.cost > 0 ? $"{node.cost}" : "");

        // Icone (opcional)
        if (iconImage != null)
        {
            iconImage.sprite  = node.icon;
            iconImage.enabled = node.icon != null;
        }

        // Fundo
        if (background != null) background.color = colorBg;

        // Listener do clique
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        // Tooltip ao hover (usa MouseEnterExitEvents do projeto legado)
        MouseEnterExitEvents hover = GetComponent<MouseEnterExitEvents>();
        if (hover != null)
        {
            hover.OnMouseEnter += (s, e) =>
            {
                Debug.Log($"Hover: {node.nodeName} | desc: {node.description} | tooltip: {TooltipUI.Instance != null}");
                if (TooltipUI.Instance != null)
                    TooltipUI.Instance.Show($"{node.nodeName}\n{node.description}");
            };
            hover.OnMouseExit += (s, e) =>
            {
                if (TooltipUI.Instance != null)
                    TooltipUI.Instance.Hide();
            };
        }
    }

    // ------------------------------------------------------------------
    // Define estado visual do botão
    // ------------------------------------------------------------------
    public void SetState(bool purchased, bool visible, bool affordable)
    {
        isPurchased = purchased;
        gameObject.SetActive(visible);

        if (!visible) return;

        if (purchased)
        {
            // Comprado — borda verde, sem interacao
            if (background != null) background.color = colorPurchased;
            if (costText   != null) costText.SetText("OK");
            button.interactable = false;
        }
        else if (affordable)
        {
            // Disponivel e pode pagar
            if (background != null) background.color = colorAffordable;
            button.interactable = true;
        }
        else
        {
            // Disponivel mas sem scraps
            if (background != null) background.color = colorCantAfford;
            button.interactable = true; // ainda clicavel — modal mostrara o custo
        }
    }

    // ------------------------------------------------------------------
    // Clique — abre modal de confirmacao no HUD
    // ------------------------------------------------------------------
    private void OnClicked()
    {
        Debug.Log($"[NodeButtonUI] Clicou em {node?.nodeName} | hud: {hud != null} | purchased: {isPurchased}");
        if (isPurchased) return;
        hud.RequestPurchase(node);
    }
}
