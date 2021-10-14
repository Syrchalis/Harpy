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
    public class HarpyLightning : Bullet
    {
        public override void Draw()
        {
            Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), DrawPos, Quaternion.Euler(0f, rotation, 0f), def.DrawMatSingle, 0);
            Comps_PostDraw();
        }

        public override void Tick()
        {
            base.Tick();
            rotation = Rand.Range(0, 360);
        }

        protected override void Impact(Thing hitThing)
        {
            //Visual
            Map map = Map;
            Vector3 pos = new Vector3();
            if (usedTarget.Thing != null)
            {
                pos = usedTarget.Thing.TrueCenter();
            }
            else
            {
                pos = Position.ToVector3();
            }
            HarpyMote moteThrown = (HarpyMote)ThingMaker.MakeThing(HarpyDefOf.HarpyMote_LightningThree, null);
            moteThrown.Scale = Rand.Range(1.0f, 1.4f);
            moteThrown.exactRotation = Rand.Range(-180, 180);
            moteThrown.exactPosition = pos;
            GenSpawn.Spawn(moteThrown, pos.ToIntVec3(), map, WipeMode.Vanish);
            MoteMaker.MakeStaticMote(pos.ToIntVec3(), map, HarpyDefOf.HarpyMote_DistortionImpact, 1f);
            SoundStarter.PlayOneShot(HarpyDefOf.Harpy_LightningImpact, new TargetInfo(pos.ToIntVec3(), map, false));
            //Projectile.Impact
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            Destroy(DestroyMode.Vanish);
            //Bullet.Impact plus changes
            float damage = 0f;
            int level = 0;
            if (launcher is Pawn pawn)
            {
                level = HarpyUtility.HarpyAmplifierLevel(pawn);
                damage = 14 + 2 * level;
            }
            if (level >= 6)
            {
                GenExplosion.DoExplosion(Position, map, 2.4f, HarpyDefOf.HarpyLightning, launcher, (int)(damage * 0.25f), 0.5f, null, equipmentDef, def, intendedTarget.Thing, null, 0f, 1, false, null, 0f, 1, 0f, false, null, null);
                damage *= 0.75f; //to avoid the main target taking too much damage (explosion + hit)
            }
            if (hitThing != null)
            {
                BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
                Find.BattleLog.Add(battleLogEntry_RangedImpact);
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, damage, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                if (hitThing is Pawn hitPawn)
                {
                    if (hitPawn.stances != null && hitPawn.BodySize <= def.projectile.StoppingPower + 0.001f)
                    {
                        hitPawn.stances.StaggerFor((int)(def.projectile.StoppingPower - hitPawn.BodySize * 30)); //custom stagger
                    }
                    if (level >= 4)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HarpyDefOf.HarpyParalyzed, hitPawn, null);
                        hediff.Severity = 1f;
                        hitPawn.health.AddHediff(hediff, null, null, null);
                    }
                }
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(Position, map, false));
            if (Position.GetTerrain(map).takeSplashes)
            {
                FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt((float)DamageAmount) * 1f, 4f);
                return;
            }
            FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
        }

        int rotation;
    }
    //Mote with random rotation every tick
    public class HarpyMote : MoteThrown
    {
        public override void Tick()
        {
            exactRotation = Rand.Range(-180, 180);
            base.Tick();
        }
    }

    //Makes harpies jitter when shooting lightning
    public class Verb_Lightning : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool flag = base.TryCastShot();
            Pawn casterPawn = CasterPawn;
            Thing thing = currentTarget.Thing;
            if (casterPawn.Spawned)
            {
                casterPawn.Drawer.Notify_MeleeAttackOn(thing);
            }
            return flag;
        }
        public override bool Available()
        {
            return base.Available() && !CasterPawn.InMentalState;
        }
    }

    public class CompUseEffect_LightningImplant : CompUseEffect_InstallImplant
    {
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);
            Hediff_Level hediffLevel = (Hediff_Level)GetExistingImplant(user);
            int level = hediffLevel.level;
            HarpyUtility.SwapLightningWeapon(user, level);
        }

        public override bool CanBeUsedBy(Pawn p, out string failReason)
        {
            if ((!p.IsFreeColonist || QuestUtility.HasExtraHomeFaction(p)) && !Props.allowNonColonists)
            {
                failReason = "InstallImplantNotAllowedForNonColonists".Translate();
                return false;
            }
            if (p.RaceProps.body.GetPartsWithDef(Props.bodyPart).FirstOrFallback(null) == null)
            {
                failReason = "InstallImplantNoBodyPart".Translate() + ": " + Props.bodyPart.LabelShort;
                return false;
            }
            if (p.def != HarpyDefOf.Harpy)
            {
                failReason = "HarpyLightningAmplifierNotHarpy".Translate();
                return false;
            }
            Hediff hediff = GetExistingImplant(p);
            if (hediff != null)
            {
                if (!Props.canUpgrade)
                {
                    failReason = "InstallImplantAlreadyInstalled".Translate();
                    return false;
                }
                Hediff_Level hediffLevel = (Hediff_Level)hediff;
                if (hediffLevel.level >= hediffLevel.def.maxSeverity)
                {
                    failReason = "InstallImplantAlreadyMaxLevel".Translate();
                    return false;
                }
                if (hediffLevel.level > 2 && parent.def == HarpyDefOf.LightningAmplifierBasic)
                {
                    failReason = "HarpyLightningAmplifierBetter".Translate();
                    return false;
                }
                if (hediffLevel.level < 3 && parent.def == HarpyDefOf.LightningAmplifierAdvanced)
                {
                    failReason = "HarpyLightningAmplifierWorse".Translate();
                    return false;
                }
            }
            if (hediff == null && parent.def == HarpyDefOf.LightningAmplifierAdvanced)
            {
                failReason = "HarpyLightningAmplifierWorse".Translate();
                return false;
            }
            failReason = null;
            return true;
        }
    }
    public class Recipe_ChangeLightningAmplifier : Recipe_ChangeImplantLevel
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Hediff_Level hediffLevel = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(HarpyDefOf.LightningAmplifierHediff);
            if (hediffLevel != null)
            {
                Thing thing = ThingMaker.MakeThing(hediffLevel.level > 3 ? HarpyDefOf.LightningAmplifierAdvanced : HarpyDefOf.LightningAmplifierBasic, null);
                GenPlace.TryPlaceThing(thing, billDoer.Position, billDoer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
            }
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        }
    }
}
