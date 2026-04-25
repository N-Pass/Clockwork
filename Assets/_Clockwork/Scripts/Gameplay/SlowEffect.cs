// SlowEffect.cs
// Componente temporário adicionado ao Enemy pela metralhadora.
// Reduz a velocidade de movimento por um tempo determinado.
// Múltiplos hits renovam a duração sem stackar o slow.

using UnityEngine;

public class SlowEffect : MonoBehaviour
{
    private Enemy enemy;
    private float originalSpeed;
    private float timer;
    private bool  isApplied;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public void Apply(float slowAmount, float duration)
    {
        if (enemy == null) return;

        if (!isApplied)
        {
            // Primeiro slow — guarda velocidade original e aplica
            originalSpeed = enemy.GetCurrentSpeed();
            float newSpeed = originalSpeed * (1f - slowAmount);
            enemy.SetCurrentSpeed(newSpeed);
            isApplied = true;
        }

        // Renova o timer (sem stackar)
        timer = duration;
    }

    private void Update()
    {
        if (!isApplied) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            RemoveSlow();
    }

    private void RemoveSlow()
    {
        if (enemy != null)
            enemy.SetCurrentSpeed(originalSpeed);

        isApplied = false;
        Destroy(this);
    }

    private void OnDestroy()
    {
        // Garante que a velocidade é restaurada se o inimigo morrer com slow ativo
        if (isApplied && enemy != null)
            enemy.SetCurrentSpeed(originalSpeed);
    }
}
