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
    [StaticConstructorOnStartup]
    public class Command_HarpyFly : Command, ITargetingSource
    {
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            HarpyComp comp = pawn.TryGetComp<HarpyComp>();
            SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_Tiny, null);
            Find.Targeter.BeginTargeting(this, null);
        }

        public TargetingParameters targetParams
        {
            get
            {
                TargetingParameters targetingParameters = new TargetingParameters();
                targetingParameters.canTargetLocations = true;
                targetingParameters.canTargetSelf = false;
                targetingParameters.canTargetPawns = false;
                targetingParameters.canTargetFires = false;
                targetingParameters.canTargetBuildings = false;
                targetingParameters.canTargetItems = false;
                targetingParameters.validator = (TargetInfo x) => DropCellFinder.IsGoodDropSpot(x.Cell, x.Map, true, false, false);
                return targetingParameters;
            }
        }

        public override void GizmoUpdateOnMouseover()
        {
            if (Find.CurrentMap != null)
            {
                if (abilityRange > 0f)
                {
                    GenDraw.DrawRadiusRing(pawn.Position, abilityRange);
                }
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            disabled = false;
            HarpyComp comp = pawn.TryGetComp<HarpyComp>();
            if (comp.cooldownTicks > 180)
            {
                DisableWithReason("HarpyFly_Cooldown".Translate());
            }
            if (pawn == null)
            {
                DisableWithReason("HarpyFly_NoPawn".Translate());
            }
            if (FireUtility.IsBurning(pawn))
            {;
                DisableWithReason("HarpyFly_OnFire".Translate());
            }
            if (pawn.Dead)
            {
                DisableWithReason("HarpyFly_Dead".Translate());
            }
            if (pawn.InMentalState)
            {
                DisableWithReason("HarpyFly_MentalState".Translate());
            }
            if (pawn.Downed || pawn.stances.stunner.Stunned || !RestUtility.Awake(pawn))
            {
                DisableWithReason("HarpyFly_Downed".Translate());
            }
            if (!HarpyUtility.FlightCapabable(pawn))
            {
                DisableWithReason("HarpyFly_Wings".Translate());
            }
            GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth);
            if (comp != null && comp.cooldownTicks > 0)
            {
                CompProperties_HarpyComp props = comp.props as CompProperties_HarpyComp;
                float num = Mathf.InverseLerp(0f, (float)props.cooldown * 60f, (float)comp.cooldownTicks);
                Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
                Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, false);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect, (comp.cooldownTicks / 60).ToStringByStyle(ToStringStyle.Integer));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            return result;
        }

        protected void DisableWithReason(string reason)
        {
            disabledReason = reason;
            disabled = true;
        }

        //Interface
        public bool MultiSelect
        {
            get
            {
                return false;
            }
        }
        public Thing Caster
        {
            get
            {
                return pawn;
            }
        }
        public Pawn CasterPawn
        {
            get
            {
                return pawn;
            }
        }
        public Verb GetVerb
        {
            get
            {
                return null;
            }
        }
        public bool CasterIsPawn
        {
            get
            {
                return true;
            }
        }
        public bool IsMeleeAttack
        {
            get
            {
                return false;
            }
        }
        public bool Targetable
        {
            get
            {
                return true;
            }
        }
        public Texture2D UIIcon
        {
            get
            {
                return icon;
            }
        }
        public ITargetingSource DestinationSelector
        {
            get
            {
                return null;
            }
        }
        public bool CanHitTarget(LocalTargetInfo target)
        {
            return target.Cell.DistanceTo(pawn.Position) <= abilityRange
            && target.Cell.Standable(pawn.Map)
            && target.Cell.InBounds(pawn.Map)
            && !target.Cell.Roofed(pawn.Map);
            
        }
        public bool ValidateTarget(LocalTargetInfo target)
        {
            if (!target.Cell.Standable(pawn.Map) || !target.Cell.InBounds(pawn.Map))
            {
                Messages.Message("HarpyFly_InvalidCell".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (target.Cell.DistanceTo(pawn.Position) > abilityRange)
            {
                Messages.Message("HarpyFly_Distance".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (target.Cell.Roofed(pawn.Map))
            {
                Messages.Message("HarpyFly_RoofTarget".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
        public void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(pawn.Position, abilityRange);
            if (target.IsValid && !target.Cell.Roofed(pawn.Map))
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public void OnGUI(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenUI.DrawMouseAttachment(UIIcon);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
            
        }
        public void OrderForceTarget(LocalTargetInfo target)
        {
            SetTarget(target);
            QueueCastingJob(target);
        }

        public virtual void QueueCastingJob(LocalTargetInfo target)
        {
            HarpyComp comp = pawn.TryGetComp<HarpyComp>();
            if (comp != null && comp.cooldownTicks > 0)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HarpyDefOf.HarpyJob_FlyAbility);
            job.targetA = target;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public void SetTarget(LocalTargetInfo target)
        {
            targetCell = target.Cell;
        }

        public virtual void SelectDestination()
        {
            Find.Targeter.BeginTargeting(this, null);
        }

        public IntVec3 targetCell;

        public Pawn pawn;

        public float abilityRange;

        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color32(192, 192, 192, 64));

        [NoTranslate]
        public static string IconPath = "Things/Special/HarpyFlyIcon";
    }
}
