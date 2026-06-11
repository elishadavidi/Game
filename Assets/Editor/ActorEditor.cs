using BecomingLegend.Actors;
using UnityEditor;
using UnityEngine;

namespace BecomingLegend.Editor
{
    [CustomEditor(typeof(Actor), true)]
    public class ActorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Actor actor = (Actor)target;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Derived Stats", EditorStyles.boldLabel);

            GUI.enabled = false;

            EditorGUILayout.FloatField("Max Health", actor.MaxHealth);
            EditorGUILayout.FloatField("Max MP", actor.MaxMP);
            EditorGUILayout.FloatField("Max Stamina", actor.MaxStamina);
            EditorGUILayout.FloatField("Attack Damage", actor.AttackDamage);
            EditorGUILayout.FloatField("Attack Cooldown", actor.AttackCooldown);
            EditorGUILayout.FloatField("Move Speed", actor.MoveSpeedDerived);
            EditorGUILayout.FloatField("Defense", actor.Defense);
            EditorGUILayout.FloatField("HP Regen /s", actor.HPRegenPerSec);
            EditorGUILayout.FloatField("MP Regen /s", actor.MPRegenPerSec);
            EditorGUILayout.FloatField("Stamina Regen /s", actor.StaminaRegenPerSec);

            GUI.enabled = true;
        }
    }
}
