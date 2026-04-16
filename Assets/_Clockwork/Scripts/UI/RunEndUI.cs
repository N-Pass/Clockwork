// RunEndUI.cs
// Tela exibida ao fim de cada run — vitória ou derrota.
// Baseada no GameOverUI do projeto anterior.
//
// Exibe: resultado (sucesso/derrota), scraps ganhos, kills, botões de ação.
//
// Hierarquia esperada:
//   RunEndUI
//   ├─ ResultText         ("Run Complete!" ou "Tower Destroyed!")
//   ├─ ScrapsEarnedText   ("+ 42 scraps")
//   ├─ KillsText          ("Enemies defeated: 17")
//   ├─ RetryButton        → GameManager.StartRun()
//   └─ HubButton          → GameManager.EndRun() (já foi chamado, vai pro HUD)

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunEndUI : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI scrapsEarnedText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI halfScrapsNote; // aviso de "você recebe 50%"

    [Header("Botões")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button hubButton;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Awake()
    {
        retryButton.onClick.AddListener(OnRetry);
        hubButton.onClick.AddListener(OnGoToHub);

        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // API — GameplayUI.OnRunEnded() chama isso
    // ------------------------------------------------------------------
    public void Show(bool success, int scrapsEarned, int kills)
    {
        gameObject.SetActive(true);

        // Resultado
        if (resultText != null)
            resultText.SetText(success ? "Run Complete!" : "Tower Destroyed!");

        // Scraps (derrota = metade)
        int scrapsToReceive = success
            ? scrapsEarned
            : Mathf.FloorToInt(scrapsEarned * 0.5f);

        if (scrapsEarnedText != null)
            scrapsEarnedText.SetText("+" + scrapsToReceive + " scraps");

        if (halfScrapsNote != null)
            halfScrapsNote.gameObject.SetActive(!success);

        // Kills
        if (killsText != null)
            killsText.SetText("Enemies defeated: " + kills);

        // Cor do texto de resultado
        if (resultText != null)
            resultText.color = success
                ? new Color(0.4f, 0.9f, 0.4f)   // verde
                : new Color(0.9f, 0.3f, 0.3f);  // vermelho

        // Texto do botão de retry diferente se é a última run (jogo completo)
        bool gameCompleted = GameManager.Instance.CurrentProfile?.gameCompleted ?? false;
        retryButton.gameObject.SetActive(!gameCompleted);
    }

    // ------------------------------------------------------------------
    // Botões
    // ------------------------------------------------------------------

    // Retry — começa nova run sem passar pelo HUD
    // GameManager.EndRun() já salvou os dados antes desta tela aparecer
    private void OnRetry()
    {
        // Os dados da run já foram aplicados em GameManager.EndRun()
        // (chamado pelo RunManager com delay). Mas se o jogador clicar
        // Retry antes do delay terminar, forçamos a transição:
        GameManager.Instance.StartRun();
    }

    // Hub — vai para o HUD para gastar scraps em upgrades
    private void OnGoToHub()
    {
        // EndRun() já foi chamado pelo RunManager.DelayedSceneChange()
        // Se o jogador clicar antes do delay, garante a transição:
        GameManager.Instance.EndRun();
    }
}
