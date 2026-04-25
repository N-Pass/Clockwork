// SubWeaponSlot.cs
// Componente de cada botão de slot na UI de Setup.
// Estado: Locked (cinza), Empty (disponível), Turret (ativo/verde), Gun (ativo/azul)

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SubWeaponSlot : MonoBehaviour
{
    public enum SlotState { Locked, Empty, Turret, Gun }

    [Header("Componentes")]
    [SerializeField] private Image           background;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Button          button;

    [Header("Cores")]
    [SerializeField] private Color colorLocked = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color colorEmpty  = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color colorTurret = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color colorGun    = new Color(0.2f, 0.5f, 0.9f);

    public SlotState CurrentState { get; private set; } = SlotState.Locked;
    public int SlotIndex { get; private set; }

    private SetupPanelUI setupPanel;

    public void Initialize(int index, SetupPanelUI panel)
    {
        SlotIndex  = index;
        setupPanel = panel;
        button.onClick.AddListener(OnClicked);
        SetState(SlotState.Locked);
    }

    public void SetState(SlotState state)
    {
        CurrentState = state;

        switch (state)
        {
            case SlotState.Locked:
                background.color     = colorLocked;
                labelText.SetText("—");
                button.interactable  = false;
                break;
            case SlotState.Empty:
                background.color     = colorEmpty;
                labelText.SetText("+");
                button.interactable  = true;
                break;
            case SlotState.Turret:
                background.color     = colorTurret;
                labelText.SetText("T");
                button.interactable  = true;
                break;
            case SlotState.Gun:
                background.color     = colorGun;
                labelText.SetText("G");
                button.interactable  = true;
                break;
        }
    }

    private void OnClicked()
    {
        setupPanel.OnSlotClicked(this);
    }
}
