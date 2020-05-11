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
    public class FlyAbilitySkyfaller : Skyfaller, IActiveDropPod
    {
        public override void Tick()
        {
            innerContainer.ThingOwnerTick(true);
            ticksToImpact--;
            Vector3 drawLoc = base.DrawPos;
            Pawn pawn = GetThingForGraphic() as Pawn;
            drawLoc.z += (Mathf.Pow((2 * TimeInAnimation) - 1f, 2) * -1f + 1) * (ticksToImpactMax / 30);
            List<IntVec3> nearbyCells = GenAdjFast.AdjacentCells8Way(drawLoc.ToIntVec3());
            foreach (IntVec3 nearbyCell in nearbyCells)
            {
                Map.fogGrid.Unfog(nearbyCell);
            }
            Map.fogGrid.Unfog(drawLoc.ToIntVec3());
            if (ticksToImpact % 3 == 0)
            {
                int numMotes = Math.Max(1, def.skyfaller.motesPerCell);
                for (int i = 0; i < numMotes; i++)
                {
                    HarpyUtility.ThrowFeathersMote(drawLoc, Map, pawn);
                }
            }
            if (this.ticksToImpact == 15)
            {
                base.HitRoof();
            }
            if (!this.anticipationSoundPlayed && this.def.skyfaller.anticipationSound != null && this.ticksToImpact < this.def.skyfaller.anticipationSoundTicks)
            {
                this.anticipationSoundPlayed = true;
                SoundStarter.PlayOneShot(this.def.skyfaller.anticipationSound, new TargetInfo(base.Position, base.Map, false));
            }
            if (this.ticksToImpact == 3)
            {
                this.EjectPilot();
            }
            if (this.ticksToImpact == 0)
            {
                base.Impact();
                return;
            }
            if (this.ticksToImpact < 0)
            {
                Log.Error("ticksToImpact < 0. Was there an exception? Destroying skyfaller.", false);
                this.EjectPilot();
                this.Destroy(0);
            }
        }

        private void EjectPilot()
        {
            Thing thing = GetThingForGraphic();
            
            HarpyComp comp = thing.TryGetComp<HarpyComp>();

            if (thing != null)
            {
                GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near, delegate (Thing t, int count)
                {
                    PawnUtility.RecoverFromUnwalkablePositionOrKill(t.Position, t.Map);
                    if (t.def.Fillage == FillCategory.Full && def.skyfaller.CausesExplosion && def.skyfaller.explosionDamage.isExplosive && t.Position.InHorDistOf(Position, def.skyfaller.explosionRadius))
                    {
                        Map.terrainGrid.Notify_TerrainDestroyed(t.Position);
                    }
                    CheckDrafting(t);
                    comp.TriggerCooldown();
                }, null, default(Rot4));
            }
        }

        internal void CheckDrafting(Thing thing)
        {
            if (thing != null && thing is Pawn)
            {
                HarpyComp comp = thing.TryGetComp<HarpyComp>();
                if (comp != null && comp.drafted)
                {
                    comp.drafted = false;
                    (thing as Pawn).drafter.Drafted = true;
                }
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawDropSpotShadow(drawLoc, Rotation, ShadowMaterial, def.skyfaller.shadowSize, ticksToImpact);
            drawLoc.z += (Mathf.Pow((2 * TimeInAnimation) - 1f, 2) * -1f + 1) * (ticksToImpactMax / 30);
            if (skyfallerPawn != null)
            {
                skyfallerPawn.RenderPawnAt(drawLoc);
            }
        }

        public ActiveDropPodInfo Contents
        {
            get
            {
                return ((ActiveDropPod)innerContainer[0]).Contents;
            }
            set
            {
                ((ActiveDropPod)innerContainer[0]).Contents = value;
            }
        }

        private float TimeInAnimation
        {
            get
            {
                return 1f - (float)ticksToImpact / (float)ticksToImpactMax;
            }
        }

        private Material ShadowMaterial
        {
            get
            {
                if (cachedShadowMaterial == null && !def.skyfaller.shadow.NullOrEmpty())
                {
                    cachedShadowMaterial = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
                }
                return cachedShadowMaterial;
            }
        }

        private Thing GetThingForGraphic()
        {
            Thing thing = null;
            if (innerContainer.Any && innerContainer.Count > 0)
            {
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    Thing thingContainer = innerContainer[i];
                    if (thingContainer is Pawn)
                    {
                        thing = thingContainer;
                    }
                }
            }
            return thing as Pawn;
        }

        private bool anticipationSoundPlayed;
        public int ticksToImpactMax;
        private Material cachedShadowMaterial;
        public PawnRenderer skyfallerPawn;
    }
}
