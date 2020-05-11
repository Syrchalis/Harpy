using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace SyrHarpy
{
    [DefOf]
    public static class HarpyDefOf
    {
        static HarpyDefOf()
        {
        }
        public static ThingDef Harpy;
        public static ThingDef FlyAbilitySkyfaller;
        public static ThingDef HarpyMote_Feathers;
        public static ThingDef HarpyMote_LightningOne;
        public static ThingDef HarpyMote_LightningTwo;
        public static ThingDef HarpyMote_LightningThree;
        public static ThingDef HarpyMote_DistortionImpact;
        public static ThingDef Bullet_HarpyLightning;
        public static ThingDef Bullet_HarpyLightningTwo;
        public static ThingDef Harpy_LightningWeaponZero;
        public static ThingDef Harpy_LightningWeaponOne;
        public static ThingDef Harpy_LightningWeaponTwo;
        public static ThingDef Harpy_LightningWeaponThree;
        public static ThingDef Harpy_LightningWeaponFour;
        public static ThingDef Harpy_LightningWeaponFive;
        public static ThingDef Harpy_LightningWeaponSix;
        public static ThingDef LightningAmplifierBasic;
        public static ThingDef LightningAmplifierAdvanced;
        public static ThingDef RawHarpyChilis;
        public static ThingDef Plant_HarpyChili;

        public static HediffDef LightningAmplifierHediff;
        public static HediffDef HarpyParalyzed;
        public static HediffDef HarpyChiliBurn;

        public static ThoughtDef AteHarpyChili;
        public static ThoughtDef AteHumanMeatAsHarpy;
        public static ThoughtDef AteHarpySpecial;

        public static JobDef HarpyJob_FlyAbility;

        public static JoyGiverDef SocialRelax;

        public static DamageDef HarpyLightning;

        public static NeedDef Bloodlust;

        public static BodyPartDef WingHarpy;
        public static BodyPartDef HandClawsHarpy;
        public static BodyPartDef FootClawsHarpy;

        public static BodyPartGroupDef Feet;
        public static BodyPartGroupDef Hands;
        public static BodyPartGroupDef Arms;

        public static SoundDef Harpy_LightningImpact;
        public static SoundDef Harpy_FlySound;
    }
}
