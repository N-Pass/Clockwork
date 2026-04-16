// UpgradeType.cs
// Enum de todos os upgrades disponíveis no HUD.
// Índice usado como posição no array upgradeLevels do ProfileData.

public enum UpgradeType
{
    ClickDamage    = 0,   // Dano por clique no inimigo
    TowerHP        = 1,   // HP máximo da torre principal
    RunDuration    = 2,   // Duração da run em segundos
    ScrapDropRate  = 3,   // Multiplicador de scraps por kill
    SubTowerSlots  = 4,   // Slots disponíveis para SubTorres
    ClickArea      = 5    // Raio do hitbox de clique
}
