using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace SyrHarpy
{
    public class JobGiver_HarpyGetJoy : ThinkNode_JobGiver
    {
        [Unsaved(false)]
        private DefMap<JoyGiverDef, float> joyGiverChances;

        public override void ResolveReferences()
        {
            joyGiverChances = new DefMap<JoyGiverDef, float>();
        }

        protected virtual Job TryGiveJobFromJoyGiverDefDirect(JoyGiverDef def, Pawn pawn)
        {
            return def.Worker.TryGiveJob(pawn);
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            List<JoyGiverDef> defsListForReading = DefDatabase<JoyGiverDef>.AllDefsListForReading;
            for (int i = 0; i < defsListForReading.Count; ++i)
            {
                JoyGiverDef def = defsListForReading[i];
                joyGiverChances[def] = 0.0f;
                if (def.Worker.CanBeGivenTo(pawn) && def.giverClass != typeof(JoyGiver_TakeDrug))
                {
                    if (def.pctPawnsEverDo < 1.0)
                    {
                        Rand.PushState(pawn.thingIDNumber ^ 63216713);
                        if (Rand.Value >= def.pctPawnsEverDo)
                        {
                            Rand.PopState();
                            continue;
                        }
                        Rand.PopState();
                    }
                    joyGiverChances[def] = def.Worker.GetChance(pawn);
                    if (def.giverClass == typeof(JoyGiver_SocialRelax))
                    {
                        joyGiverChances[def] *= 3;
                    }
                }
            }
            for (int i = 0; i < joyGiverChances.Count && defsListForReading.TryRandomElementByWeight(d => joyGiverChances[d], out JoyGiverDef result); i++)
            {
                Job job = TryGiveJobFromJoyGiverDefDirect(result, pawn);
                if (job != null)
                {
                    return job;
                }
                joyGiverChances[result] = 0.0f;
            }
            return null;
        }
    }
    public class JobGiver_HarpyGetJoyInGatheringArea : JobGiver_HarpyGetJoy
    {
        protected override Job TryGiveJobFromJoyGiverDefDirect(JoyGiverDef def, Pawn pawn)
        {
            if (pawn.mindState.duty == null)
            {
                return null;
            }
            IntVec3 cell = pawn.mindState.duty.focus.Cell;
            return def.Worker.TryGiveJobInGatheringArea(pawn, cell);
        }
    }
    public class JobGiver_HarpyIdleJoy : JobGiver_HarpyGetJoy
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (Find.TickManager.TicksGame < 60000)
            {
                return null;
            }
            if (JoyUtility.LordPreventsGettingJoy(pawn) || JoyUtility.TimetablePreventsGettingJoy(pawn))
            {
                return null;
            }
            return base.TryGiveJob(pawn);
        }
    }
    public class ThinkNode_ConditionalRace : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.def.defName == race;
        }
        public string race;
    }
}
