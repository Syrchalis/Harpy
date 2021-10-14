using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace SyrHarpy
{
    public class DamageWorker_HarpyLightning : DamageWorker_AddInjury
    {
        protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
        {
            base.ExplosionDamageThing(explosion, t, damagedThings, ignoredThings, cell);
            Pawn pawn = explosion.instigator as Pawn;
            Pawn hitPawn = t as Pawn;
            if (pawn != null && hitPawn != null && hitPawn.HostileTo(pawn) && HarpyUtility.HarpyAmplifierLevel(pawn) >= 4)
            {
                Hediff hediff = HediffMaker.MakeHediff(HarpyDefOf.HarpyParalyzed, hitPawn, null);
                hediff.Severity = 0.5f;
                hitPawn.health.AddHediff(hediff, null, null, null);
            }
            if (pawn != null && t != null && HarpyUtility.HarpyAmplifierLevel(pawn) >= 5)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.EMP, 20f, instigator: pawn, weapon: explosion.weapon, category: DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget: explosion.intendedTarget);
                t.TakeDamage(dinfo).AssociateWithLog(new BattleLogEntry_ExplosionImpact(explosion.instigator, t, explosion.weapon, explosion.projectile, def));
            }
        }
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            if (def.explosionHeatEnergyPerCell > float.Epsilon)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, def.explosionHeatEnergyPerCell * cellsToAffect.Count);
            }
            FleckMaker.Static(explosion.Position, explosion.Map, FleckDefOf.ExplosionFlash, explosion.radius * 6f);
            ExplosionVisualEffectCenter(explosion);
        }
        protected override void ExplosionVisualEffectCenter(Explosion explosion)
        {
            if (def.explosionInteriorMote != null)
            {
                int num = Mathf.RoundToInt(3.14159274f * explosion.radius * explosion.radius / 6f);
                for (int j = 0; j < num; j++)
                {
                    MoteMaker.ThrowExplosionInteriorMote(explosion.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map, def.explosionInteriorMote);
                }
            }
        }
    }
}
