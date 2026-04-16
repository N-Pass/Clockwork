// SaveSystem.cs
// Classe estática para persistência de dados.
// Grava cada perfil como slot0.json / slot1.json / slot2.json
// em Application.persistentDataPath.
//
// Uso:
//   SaveSystem.Save(0, profileData);
//   ProfileData data = SaveSystem.Load(0);
//   SaveSystem.Delete(0);
//   bool exists = SaveSystem.HasProfile(0);

using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const int TOTAL_SLOTS = 3;

    // ------------------------------------------------------------------
    // Caminho do arquivo para cada slot
    // ------------------------------------------------------------------
    private static string GetPath(int slot)
        => Path.Combine(Application.persistentDataPath, $"slot{slot}.json");

    // ------------------------------------------------------------------
    // Salvar
    // ------------------------------------------------------------------
    public static void Save(int slot, ProfileData data)
    {
        ValidateSlot(slot);
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GetPath(slot), json);
        Debug.Log($"[SaveSystem] Slot {slot} salvo em {GetPath(slot)}");
    }

    // ------------------------------------------------------------------
    // Carregar — retorna null se o slot não existir
    // ------------------------------------------------------------------
    public static ProfileData Load(int slot)
    {
        ValidateSlot(slot);
        string path = GetPath(slot);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSystem] Slot {slot} não encontrado.");
            return null;
        }

        string json = File.ReadAllText(path);
        ProfileData data = JsonUtility.FromJson<ProfileData>(json);
        Debug.Log($"[SaveSystem] Slot {slot} carregado.");
        return data;
    }

    // ------------------------------------------------------------------
    // Deletar
    // ------------------------------------------------------------------
    public static void Delete(int slot)
    {
        ValidateSlot(slot);
        string path = GetPath(slot);

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveSystem] Slot {slot} deletado.");
        }
    }

    // ------------------------------------------------------------------
    // Verificar existência
    // ------------------------------------------------------------------
    public static bool HasProfile(int slot)
    {
        ValidateSlot(slot);
        return File.Exists(GetPath(slot));
    }

    // ------------------------------------------------------------------
    // Validação de slot
    // ------------------------------------------------------------------
    private static void ValidateSlot(int slot)
    {
        if (slot < 0 || slot >= TOTAL_SLOTS)
            throw new System.ArgumentOutOfRangeException(nameof(slot),
                $"Slot deve ser entre 0 e {TOTAL_SLOTS - 1}.");
    }
}
