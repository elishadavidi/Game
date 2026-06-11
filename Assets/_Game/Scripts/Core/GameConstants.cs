namespace BecomingLegend
{
    public static class GameConstants
    {
        public const int MaxLevel = 100;
        public const int BaseXPToLevel = 100;
        public const float XPLevelMultiplier = 1.5f;
        public const float CritMultiplier = 2f;
        public const float BaseCritChance = 0.05f;

        // Derived stat multipliers
        public const float BaseDamagePerStrength = 2f;
        public const float MoveSpeedBase = 1f;
        public const float MoveSpeedPerSpeed = 0.1f;
        public const float BaseAttackCooldown = 1f;
        public const float AtkSpeedPerSpeed = 0.05f;
        public const float MinAttackCooldown = 0.15f;
        public const float HpRegenPerCore = 0.5f;
        public const float MpRegenPerStamina = 0.3f;
        public const float StaminaRegenPerStamina = 0.5f;
        public const float DefPerCore = 1f;
    }
}
