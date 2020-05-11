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

namespace SyrHarpy
{
    public static class HarpyUtility
    {
        public static bool FlightCapabable(Pawn pawn)
        {
            if (pawn != null && pawn.def == HarpyDefOf.Harpy)
            {
                return FlightCapability(pawn) > 1f;
            }
            return false;
        }

        public static float FlightCapability(Pawn pawn)
        {
            if (pawn != null && pawn.def == HarpyDefOf.Harpy)
            {
                BodyDef body = pawn.RaceProps.body;
                float capacity = 0f;
                foreach (BodyPartRecord bodyPartRecord in body.GetPartsWithDef(HarpyDefOf.WingHarpy))
                {
                    capacity += PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, bodyPartRecord, false, null);
                }
                return capacity;
            }
            return 0f;
        }

        public static int HarpyAmplifierLevel(Pawn pawn)
        {
            Hediff_ImplantWithLevel hediffLevel = (Hediff_ImplantWithLevel)pawn.health.hediffSet.GetFirstHediffOfDef(HarpyDefOf.LightningAmplifierHediff);
            if (hediffLevel != null)
            {
                return hediffLevel.level;
            }
            else
            {
                return 0;
            }
            
        }
        public static bool IsHarpyLightningWeapon(ThingDef thingDef)
        {
            return thingDef == HarpyDefOf.Harpy_LightningWeaponZero || 
                 thingDef == HarpyDefOf.Harpy_LightningWeaponOne ||
                 thingDef == HarpyDefOf.Harpy_LightningWeaponTwo ||
                 thingDef == HarpyDefOf.Harpy_LightningWeaponThree ||
                 thingDef == HarpyDefOf.Harpy_LightningWeaponFour ||
                 thingDef == HarpyDefOf.Harpy_LightningWeaponFive ||
                 thingDef == HarpyDefOf.Harpy_LightningWeaponSix;
        }

        public static void SwapLightningWeapon(Pawn pawn, int level)
        {
            ThingDef weaponDef = null;
            switch (level)
            {
                case 0: weaponDef = HarpyDefOf.Harpy_LightningWeaponZero; break;
                case 1: weaponDef = HarpyDefOf.Harpy_LightningWeaponOne; break;
                case 2: weaponDef = HarpyDefOf.Harpy_LightningWeaponTwo; break;
                case 3: weaponDef = HarpyDefOf.Harpy_LightningWeaponThree; break;
                case 4: weaponDef = HarpyDefOf.Harpy_LightningWeaponFour; break;
                case 5: weaponDef = HarpyDefOf.Harpy_LightningWeaponFive; break;
                case 6: weaponDef = HarpyDefOf.Harpy_LightningWeaponSix; break;
            }
            if (weaponDef == null)
            {
                return;
            }
            ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(weaponDef, null);
            if (pawn.equipment.Primary != null)
            {
                pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
            }
            pawn.equipment.AddEquipment(thingWithComps);
        }

        public static void ThrowFeathersMote (Vector3 loc, Map map, Pawn pawn)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(HarpyDefOf.HarpyMote_Feathers, null);
            moteThrown.Scale = Rand.Range(0.3f, 0.6f);
            moteThrown.rotationRate = Rand.Range(-180, 180);
            moteThrown.exactRotation = Rand.Range(-180, 180);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
            moteThrown.SetVelocity(Rand.Range(0, 360), Rand.Range(0.2f, 2f));
            if (pawn != null)
            {
                Color color = Color.Lerp(pawn.story.hairColor, new Color(1, 1, 1, 1), Rand.Value);
                if (Rand.Value < 0.5f)
                {
                    color = Color.Lerp(pawn.story.hairColor, color, Rand.Value);
                }
                moteThrown.instanceColor = color;
            }
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
        }

        [DebugAction("Pawns", "Bloodlust -20%", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OffsetBloodlustNegative20()
        {
            OffsetNeed(DefDatabase<NeedDef>.GetNamed("Bloodlust", true), -0.2f);
        }
        private static void OffsetNeed(NeedDef nd, float offsetPct)
        {
            foreach (Pawn pawn in (from t in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell())
                                   where t is Pawn
                                   select t).Cast<Pawn>())
            {
                Need need = pawn.needs.TryGetNeed(nd);
                if (need != null)
                {
                    need.CurLevel += offsetPct * need.MaxLevel;
                    BloodlustAlertUtility.Notify_BloodlustChanged(pawn, need.CurLevel);
                    DebugActionsUtility.DustPuffFrom(pawn);
                }
            }
        }
    }
}
