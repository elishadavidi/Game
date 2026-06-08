using BecomingLegend;
using BecomingLegend.Actors;
using BecomingLegend.Stats;
using UnityEngine;

namespace BecomingLegend.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public DamageResult CalculateDamage(Actor attacker, IDamageable defender)
        {
            float strength = attacker.Stats.GetStat(StatType.Strength);
            float baseDamage = strength * GameConstants.BaseDamagePerStrength;
            bool isCrit = Random.value < GameConstants.BaseCritChance;
            float finalDamage = isCrit ? baseDamage * GameConstants.CritMultiplier : baseDamage;

            return new DamageResult(finalDamage, DamageType.Physical, isCrit, attacker, defender);
        }

        public bool IsInRange(Actor attacker, Actor target, float range)
        {
            float dist = Vector2.Distance(attacker.transform.position, target.transform.position);
            return dist <= range;
        }
    }
}
