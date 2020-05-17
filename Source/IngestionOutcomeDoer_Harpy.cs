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
    /*public class IngestionOutcomeDoer_Harpy : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            if (pawn != null && !notAffected.Contains(pawn.def))
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, null);
                hediff.Severity = ingested.stackCount * 0.05f / pawn.BodySize;
                pawn.health.AddHediff(hediff, null, null, null);
            }
        }
        public List<ThingDef> notAffected;
        public HediffDef hediffDef;
    }*/
}
