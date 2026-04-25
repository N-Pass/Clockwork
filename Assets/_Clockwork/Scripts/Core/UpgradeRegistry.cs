// UpgradeRegistry.cs
// Carrega todos os UpgradeNodeSO da pasta Resources/UpgradeNodes
// e mantém um dicionário nodeID → SO para lookup rápido.
//
// Uso: UpgradeRegistry.Get("dmg_01")
//
// Para funcionar: salvar todos os UpgradeNodeSO em
// Assets/Resources/UpgradeNodes/

using System.Collections.Generic;
using UnityEngine;

public static class UpgradeRegistry
{
    private static Dictionary<string, UpgradeNodeSO> registry;

    private static void EnsureLoaded()
    {
        if (registry != null) return;

        registry = new Dictionary<string, UpgradeNodeSO>();
        UpgradeNodeSO[] nodes = Resources.LoadAll<UpgradeNodeSO>("UpgradeNodes");

        foreach (UpgradeNodeSO node in nodes)
        {
            if (string.IsNullOrEmpty(node.nodeID))
            {
                Debug.LogWarning($"[UpgradeRegistry] Nó '{node.name}' sem nodeID — ignorado.");
                continue;
            }

            if (registry.ContainsKey(node.nodeID))
            {
                Debug.LogWarning($"[UpgradeRegistry] nodeID duplicado: '{node.nodeID}' — verificar assets.");
                continue;
            }

            registry[node.nodeID] = node;
        }

        Debug.Log($"[UpgradeRegistry] {registry.Count} nós carregados.");
    }

    public static UpgradeNodeSO Get(string nodeID)
    {
        EnsureLoaded();
        registry.TryGetValue(nodeID, out UpgradeNodeSO node);
        return node;
    }

    public static IEnumerable<UpgradeNodeSO> GetAll()
    {
        EnsureLoaded();
        return registry.Values;
    }

    // Limpa cache — chamar se assets forem modificados em runtime (editor only)
    public static void Invalidate() => registry = null;
}
