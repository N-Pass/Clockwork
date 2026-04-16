// MiningNode.cs
// Ponto de coleta de scraps no mapa da Cena 3.
//
// Comportamento:
//   - Clique do player → coleta scraps imediatamente
//   - Entra em cooldown visual após coleta
//   - Opcional: auto-gera scraps lentamente (elemento idle)
//     Ativado via EnableAutoCollect() por um upgrade de SubTorre
//
// Inspirado no ResourceGenerator do projeto anterior,
// mas dramaticamente simplificado — um recurso só, sem proximity check.

using UnityEngine;

public class MiningNode : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------
    [Header("Coleta manual")]
    [SerializeField] private int   scrapsPerClick    = 3;
    [SerializeField] private float clickCooldown     = 2f;

    [Header("Auto-coleta (upgrade)")]
    [SerializeField] private float autoCollectInterval = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer nodeSprite;
    [SerializeField] private Color readyColor    = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f);

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    private bool  isReady         = true;
    private float cooldownTimer   = 0f;

    private bool  autoCollectEnabled = false;
    private float autoCollectTimer   = 0f;

    // ------------------------------------------------------------------
    // Unity
    // ------------------------------------------------------------------
    private void Update()
    {
        // Cooldown após clique manual
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                UpdateVisual();
            }
        }

        // Auto-coleta (ativada por upgrade de SubTorre)
        if (autoCollectEnabled && RunManager.Instance.CurrentState == RunManager.RunState.Running)
        {
            autoCollectTimer -= Time.deltaTime;
            if (autoCollectTimer <= 0f)
            {
                autoCollectTimer = autoCollectInterval;
                Collect(isManual: false);
            }
        }
    }

    // Clique do mouse sobre o sprite do nó
    private void OnMouseDown()
    {
        if (!isReady) return;
        if (RunManager.Instance.CurrentState != RunManager.RunState.Running) return;

        Collect(isManual: true);
    }

    // ------------------------------------------------------------------
    // Coleta
    // ------------------------------------------------------------------
    private void Collect(bool isManual)
    {
        // Calcula scraps com multiplicador do upgrade
        int amount = Mathf.RoundToInt(scrapsPerClick * GameManager.Instance.GetScrapDropRate());
        RunManager.Instance.AddScraps(amount);

        // Partícula de feedback (opcional — substitua pelo seu prefab)
        // Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);

        if (isManual)
        {
            // Entra em cooldown somente no clique manual
            isReady       = false;
            cooldownTimer = clickCooldown;
            UpdateVisual();
        }
    }

    // ------------------------------------------------------------------
    // API pública
    // ------------------------------------------------------------------

    // Chamado por upgrade de SubTorre para ativar coleta automática
    public void EnableAutoCollect(float interval = 0f)
    {
        autoCollectEnabled   = true;
        autoCollectInterval  = interval > 0f ? interval : autoCollectInterval;
        autoCollectTimer     = autoCollectInterval;
    }

    public void DisableAutoCollect()
    {
        autoCollectEnabled = false;
    }

    // Normalizado (0 = pronto, 1 = em cooldown total) — para barra visual
    public float GetCooldownNormalized()
    {
        if (isReady) return 0f;
        return 1f - (cooldownTimer / clickCooldown);
    }

    public bool IsReady => isReady;

    // ------------------------------------------------------------------
    // Visual
    // ------------------------------------------------------------------
    private void UpdateVisual()
    {
        if (nodeSprite != null)
            nodeSprite.color = isReady ? readyColor : cooldownColor;
    }
}
