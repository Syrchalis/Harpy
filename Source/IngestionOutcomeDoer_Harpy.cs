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
    public class IngestionOutcomeDoer_Harpy : IngestionOutcomeDoer_GiveHediff
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            if (pawn != null && !notAffected.Contains(pawn.def))
            {
                base.DoIngestionOutcomeSpecial(pawn, ingested);
            }
        }
        public List<ThingDef> notAffected;
    }
}
