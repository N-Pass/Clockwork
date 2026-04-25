// UpgradeNodeSO.cs
// ScriptableObject que representa um nó individual da árvore de upgrades.
// Cada nó é um asset independente no Project.
//
// Criar via: Assets → Create → Clockwork → Upgrade Node
//
// Pós-jam: adicionar novos NodeEffectType sem quebrar nós existentes.
// Assets já criados continuam funcionando — só criar novos com os tipos novos.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Clockwork/Upgrade Node")]
public class UpgradeNodeSO : ScriptableObject
{
    // ------------------------------------------------------------------
    // Identificação — nome único usado para salvar no ProfileData
    // IMPORTANTE: nunca mudar o nodeID de um nó depois de criado
    // se mudar, saves existentes perdem o estado desse nó
    // ------------------------------------------------------------------
    [Header("Identificação")]
    public string nodeID;        // ex: "dmg_01" — único, nunca mudar após criado
    public string nodeName;      // ex: "Dano +"  — nome exibido na UI
    [TextArea(2, 4)]
    public string description;   // texto do tooltip ao passar o mouse

    // ------------------------------------------------------------------
    // Visual
    // ------------------------------------------------------------------
    [Header("Visual")]
    public Sprite icon;          // ícone do botão (opcional — usa cor se null)
    public Color  nodeColor = Color.white; // cor da borda quando ativo

    // ------------------------------------------------------------------
    // Custo
    // ------------------------------------------------------------------
    [Header("Custo")]
    public int cost;             // scraps necessários para comprar

    // ------------------------------------------------------------------
    // Efeito — o que este nó faz quando comprado
    // ------------------------------------------------------------------
    [Header("Efeito")]
    public NodeEffectType effectType;
    public float          effectValue; // quanto aumenta/modifica

    // ------------------------------------------------------------------
    // Árvore — filhos desbloqueados quando este nó é comprado
    // ------------------------------------------------------------------
    [Header("Posição relativa ao pai na UI")]
    public Vector2 positionOffset; // pixels de distância do nó pai


    // ------------------------------------------------------------------
    // Posicao na UI — relativa ao no pai
    // Ex: (0, 100) = 100px acima do pai
    // ------------------------------------------------------------------

    [Header("Filhos (desbloqueados ao comprar)")]
    public List<UpgradeNodeSO> children = new List<UpgradeNodeSO>();

    // ------------------------------------------------------------------
    // Validação — avisa no Inspector se nodeID está vazio
    // ------------------------------------------------------------------
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(nodeID))
            Debug.LogWarning($"[UpgradeNodeSO] '{name}' está sem nodeID. Defina um ID único.");
    }
}

// ------------------------------------------------------------------
// Tipos de efeito — jam + estrutura para pós-jam
// ------------------------------------------------------------------
public enum NodeEffectType
{
    // ------ JAM ------
    TowerHP           = 0,   // aumenta HP máximo da torre
    ClickDamage       = 1,   // aumenta dano por clique
    ProjectileSpeed   = 2,   // aumenta velocidade dos projéteis
    FireRate          = 3,   // aumenta tiros por segundo
    RunDuration       = 4,   // aumenta duração da run em segundos
    SubTowerSlot      = 5,   // desbloqueia +1 slot de SubTorre
    MachineGunSlot    = 6,   // desbloqueia +1 slot de Metralhadora

    // ------ PÓS-JAM (não implementados ainda — reservados) ------
    ScrapDropRate     = 10,  // multiplica scraps ganhos por kill
    ClickArea         = 11,  // aumenta raio de hitbox do clique

    // Tipos de projétil do jogador
    UnlockGrenade     = 20,  // desbloqueia projétil de granada
    UnlockLaser       = 21,  // desbloqueia projétil laser
    UnlockBlade       = 22,  // desbloqueia lâmina próxima ao jogador

    // Upgrades dos projéteis especiais
    GrenadeDamage     = 30,
    GrenadeRadius     = 31,
    LaserDamage       = 32,
    LaserCooldown     = 33,
    BladeDamage       = 34,
    BladeRadius       = 35,

    // Upgrades de SubTorre
    SubTowerDamage    = 40,
    SubTowerFireRate  = 41,

    // Upgrades de Metralhadora
    MachineGunDamage  = 50,
    MachineGunFireRate= 51,
    MachineGunRange   = 52,
}
