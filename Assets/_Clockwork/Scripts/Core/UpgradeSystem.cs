// UpgradeSystem.cs
// Lê os nós comprados do ProfileData e aplica os efeitos ao jogo.
// Chamado pelo HUDController ao comprar um nó e pelo RunManager no Start.
//
// Pós-jam: adicionar novos cases no switch sem tocar no resto.

using System.Collections.Generic;
using UnityEngine;

public static class UpgradeSystem
{
    // ------------------------------------------------------------------
    // Aplica todos os upgrades comprados do perfil
    // Chamado pelo RunManager.Start() para configurar a run
    // ------------------------------------------------------------------
    public static void ApplyAll(ProfileData profile, RunContext ctx)
    {
        if (profile == null || profile.purchasedNodeIDs == null) return;

        foreach (string nodeID in profile.purchasedNodeIDs)
        {
            UpgradeNodeSO node = UpgradeRegistry.Get(nodeID);
            if (node != null)
                Apply(node, ctx);
        }
    }

    // ------------------------------------------------------------------
    // Aplica um único nó — chamado ao comprar
    // ------------------------------------------------------------------
    public static void Apply(UpgradeNodeSO node, RunContext ctx)
    {
        if (node == null || ctx == null) return;

        switch (node.effectType)
        {
            // ------ JAM ------
            case NodeEffectType.TowerHP:
                ctx.towerHP += Mathf.RoundToInt(node.effectValue);
                break;

            case NodeEffectType.ClickDamage:
                ctx.clickDamage += Mathf.RoundToInt(node.effectValue);
                break;

            case NodeEffectType.ProjectileSpeed:
                ctx.projectileSpeed += node.effectValue;
                break;

            case NodeEffectType.FireRate:
                ctx.fireRate += node.effectValue;
                break;

            case NodeEffectType.RunDuration:
                ctx.runDuration += node.effectValue;
                break;

            case NodeEffectType.SubTowerSlot:
                ctx.subTowerSlots += Mathf.RoundToInt(node.effectValue);
                break;

            case NodeEffectType.MachineGunSlot:
                ctx.machineGunSlots += Mathf.RoundToInt(node.effectValue);
                break;

            // ------ PÓS-JAM (sem implementação ainda — não quebra) ------
            case NodeEffectType.ScrapDropRate:
                ctx.scrapDropRate += node.effectValue;
                break;

            case NodeEffectType.ClickArea:
                ctx.clickArea += node.effectValue;
                break;

            // Tipos de projétil — só desbloqueia a flag
            case NodeEffectType.UnlockGrenade:
                ctx.hasGrenade = true;
                break;

            case NodeEffectType.UnlockLaser:
                ctx.hasLaser = true;
                break;

            case NodeEffectType.UnlockBlade:
                ctx.hasBlade = true;
                break;

            default:
                // Tipos pós-jam não implementados — ignora silenciosamente
                break;
        }
    }

    // ------------------------------------------------------------------
    // Verifica se um nó pode ser comprado
    // ------------------------------------------------------------------
    public static bool CanPurchase(UpgradeNodeSO node, ProfileData profile)
    {
        if (node == null || profile == null) return false;
        if (profile.purchasedNodeIDs.Contains(node.nodeID)) return false;
        if (profile.totalScraps < node.cost) return false;
        return true;
    }

    // ------------------------------------------------------------------
    // Compra um nó — desconta scraps, salva, retorna true se sucesso
    // ------------------------------------------------------------------
    public static bool Purchase(UpgradeNodeSO node, ProfileData profile, int slot)
    {
        if (!CanPurchase(node, profile)) return false;

        profile.totalScraps -= node.cost;
        profile.purchasedNodeIDs.Add(node.nodeID);
        profile.totalScrapsEverEarned = profile.totalScrapsEverEarned; // mantém

        SaveSystem.Save(slot, profile);
        return true;
    }
}

// ------------------------------------------------------------------
// RunContext — valores calculados a partir dos upgrades
// RunManager lê isso no Start() para configurar a run
// ------------------------------------------------------------------
public class RunContext
{
    // Valores base (sem upgrades)
    public int   towerHP         = 5;
    public int   clickDamage     = 1;
    public float projectileSpeed = 18f;
    public float fireRate        = 1.5f;
    public float runDuration     = 5f;
    public int   subTowerSlots   = 0;
    public int   machineGunSlots = 0;
    public float scrapDropRate   = 1f;
    public float clickArea       = 0.5f;

    // Pós-jam
    public bool hasGrenade = false;
    public bool hasLaser   = false;
    public bool hasBlade   = false;

    // ------------------------------------------------------------------
    // Cria RunContext com valores base e aplica upgrades do perfil
    // ------------------------------------------------------------------
    public static RunContext FromProfile(ProfileData profile, UpgradeTreeSO tree)
    {
        RunContext ctx = new RunContext();
        if (tree != null)
            UpgradeSystem.ApplyAll(profile, ctx);
        return ctx;
    }
}
