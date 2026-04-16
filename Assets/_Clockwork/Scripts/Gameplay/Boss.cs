// Boss.cs — estende Enemy
//
// Diferenças do Enemy base:
//   - Carrega um pieceId (qual peça do relógio vai dropar)
//   - Ao morrer notifica RunManager.OnBossKilled(pieceId, drop)
//   - Visual diferenciado: escala maior (configurável no prefab)
//   - Stats configurados via SetBossStats() pelo WaveManager

using UnityEngine;

public class Boss : Enemy
{
    private int pieceId = 0;

    // ------------------------------------------------------------------
    // API pública — WaveManager usa SetBossStats em vez de SetStats
    // ------------------------------------------------------------------
    public void SetBossStats(int hp, float speed, int damage, int drop, int pieceId)
    {
        this.pieceId = pieceId;
        base.SetStats(hp, speed, damage, drop);
    }

    // ------------------------------------------------------------------
    // Override de morte — notifica RunManager com pieceId
    // ------------------------------------------------------------------
    protected override void HealthSystem_OnDied(object sender, System.EventArgs e)
    {
        OnDeathEffect();

        // RunManager registra peça + scraps + verifica win condition
        RunManager.Instance.OnBossKilled(pieceId, scrapDrop);

        Destroy(gameObject);
    }

    protected override void OnDeathEffect()
    {
        // Efeito visual de morte de boss (mais elaborado que inimigo normal)
        // SoundManager.Instance.PlaySound(SoundManager.Sound.BossDie);
        // Instantiate(bossDieParticlesPrefab, transform.position, Quaternion.identity);

        base.OnDeathEffect();
    }
}
