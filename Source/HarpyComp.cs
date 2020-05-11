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
    public class HarpyComp : ThingComp
    {
        public CompProperties_HarpyComp Props
        {
            get
            {
                return (CompProperties_HarpyComp)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (cooldownTicks > 0)
            {
                cooldownTicks--;
            }
            if (soundQueued)
            {
                HarpyDefOf.Harpy_FlySound.PlayOneShot(new TargetInfo(parent.Position, Find.CurrentMap, false));
                soundQueued = false;
            }
        }

        public float AdjustedRange
        {
            get
            {
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    return Props.range * HarpyUtility.FlightCapability(pawn) / 2;
                }
                return Props.range;
            }
        }
        public void TriggerCooldown()
        {
            cooldownTicks = Props.cooldown * 60;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = parent as Pawn;
            if (pawn?.Map != null && pawn.IsColonistPlayerControlled &&!pawn.Downed && pawn.Spawned)
            {
                if (Find.Selector.SingleSelectedThing == pawn)
                {
                    string label = "HarpyFlyLabel".Translate();
                    string desc = "HarpyFlyDescription".Translate();
                    Texture2D HarpyFlyIcon = ContentFinder<Texture2D>.Get(Command_HarpyFly.IconPath, true);
                    yield return new Command_HarpyFly
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        icon = HarpyFlyIcon,
                        pawn = pawn,
                        abilityRange = AdjustedRange,
                        hotKey = KeyBindingDefOf.Misc3,
                    };
                }
            }
            yield break;
        }

        public void FlyAbility(Pawn pawn, IntVec3 targetCell)
        {
            if (cooldownTicks > 0)
            {
                Messages.Message("HarpyFly_Cooldown".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            soundQueued = true;
            RotateFlyer(pawn, targetCell, out float angle);
            float speed = HarpyDefOf.FlyAbilitySkyfaller.skyfaller.speed;
            float distance = IntVec3Utility.DistanceTo(pawn.Position, targetCell);
            int timeToLand = (int)(distance / speed);
            angle += 270f;
            if (angle >= 360f)
            {
                angle -= 360f;
            }
            Skyfaller skyfaller = SkyfallerMaker.SpawnSkyfaller(HarpyDefOf.FlyAbilitySkyfaller, targetCell, pawn.Map);
            FlyAbilitySkyfaller FAskyfaller = skyfaller as FlyAbilitySkyfaller;
            skyfaller.ticksToImpact = timeToLand;
            FAskyfaller.ticksToImpactMax = timeToLand;
            FAskyfaller.skyfallerPawn = new PawnRenderer(pawn);
            skyfaller.angle = angle;
            drafted = pawn.Drafted;
            pawn.DeSpawn(0);
            skyfaller.innerContainer.TryAdd(pawn, false);
        }

        internal void RotateFlyer(Pawn pawn, IntVec3 targetCell, out float angle)
        {
            angle = Vector3Utility.AngleToFlat(pawn.Position.ToVector3(), targetCell.ToVector3());
            float offsetAngle = angle + 90f;
            if (offsetAngle > 360f)
            {
                offsetAngle -= 360f;
            }
            Rot4 facing = Rot4.FromAngleFlat(offsetAngle);
            pawn.Rotation = facing;
        }

        public override void CompTickRare()
        {
            drugsNursedToday.RemoveAll(kvp => kvp.Value < Find.TickManager.TicksGame - GenDate.TicksPerDay);
        }

        public float cooldownTicks;
        public bool drafted;
        public int timeToImpactMax;
        public bool soundQueued = false;
        public Dictionary<ThingDef, int> drugsNursedToday = new Dictionary<ThingDef, int>();
    }

    public class CompProperties_HarpyComp : CompProperties
    {
        public CompProperties_HarpyComp()
        {
            this.compClass = typeof(HarpyComp);
        }

        public float cooldown = 5f;
        public float range = 40f;
    }
}
