// UpgradeTreeSO.cs
// Define a raiz da árvore de upgrades.
// Um asset por página de upgrade (Página 1 = árvore do jogador).
// A UI lê este SO para saber qual é o nó central e montar a árvore.

using UnityEngine;

[CreateAssetMenu(menuName = "Clockwork/Upgrade Tree")]
public class UpgradeTreeSO : ScriptableObject
{
    [Header("Nó raiz — centro da árvore")]
    public UpgradeNodeSO rootNode;

    [Header("Identificação")]
    public string treeName; // ex: "Árvore do Jogador"
}
