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
    public class JobDriver_FlyAbility : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            HarpyComp comp = pawn.TryGetComp<HarpyComp>();
            if (comp != null)
            {
                if (pawn.Position.Roofed(pawn.Map))
                {
                    yield return Toils_Harpy.GoToNearUnroofedCell(TargetIndex.A, comp.AdjustedRange);
                }
                yield return Toils_Harpy.CastFlyAbility(TargetIndex.A);
            }
            yield break;
        }
    }

    public static class Toils_Harpy
    {
        public static Toil GoToNearUnroofedCell(TargetIndex ind, float range)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn pawn = toil.actor;
                Map map = pawn.Map;
                IntVec3 moveCell = IntVec3.Invalid;
                Predicate<IntVec3> passCheck = delegate (IntVec3 c)
                {
                    if (!c.Walkable(map))
                    {
                        return false;
                    }
                    return true;
                };
                Func<IntVec3, bool> processor = delegate (IntVec3 v)
                {
                    if (v.Roofed(map) || v.DistanceTo(pawn.jobs.curJob.GetTarget(ind).Cell) > range)
                    {
                        return false;
                    }
                    moveCell = v;
                    return true;
                };
                map.floodFiller.FloodFill(pawn.Position, passCheck, processor, 2000);
                if (moveCell != IntVec3.Invalid)
                {
                    pawn.pather.StartPath(moveCell, PathEndMode.OnCell);
                }
                else
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    Messages.Message("HarpyFly_RoofCaster".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
        public static Toil CastFlyAbility(TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn pawn = toil.actor;
                HarpyComp comp = pawn.TryGetComp<HarpyComp>();
                if (comp != null)
                {
                    comp.FlyAbility(pawn, pawn.jobs.curJob.GetTarget(ind).Cell);
                }
                else
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
    }
}
