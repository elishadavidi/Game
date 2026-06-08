using BecomingLegend;

namespace BecomingLegend.Combat
{
    public readonly struct DamageResult
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly bool IsCritical;
        public readonly object Source;
        public readonly object Target;

        public DamageResult(float amount, DamageType type, bool isCritical, object source, object target)
        {
            Amount = amount;
            Type = type;
            IsCritical = isCritical;
            Source = source;
            Target = target;
        }

        public DamageResult WithAmount(float newAmount) =>
            new(newAmount, Type, IsCritical, Source, Target);
    }
}
