using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;

namespace SyrHarpy
{
    class SyrHarpySettings : ModSettings
    {
        public static bool useStandardAI = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref useStandardAI, "SyrHarpy_usestandardAI", false, true);
        }
    }
}
