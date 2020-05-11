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
    public class Comp_HarpyMoteEmitter : ThingComp
    {
        private CompProperties_HarpyMoteEmitter Props
        {
            get
            {
                return (CompProperties_HarpyMoteEmitter)this.props;
            }
        }

        public override void CompTick()
        {
            if (Props.emissionInterval != -1)
            {
                if (ticksSinceLastEmitted >= Props.emissionInterval)
                {
                    Emit();
                    ticksSinceLastEmitted = 0;
                }
                else
                {
                    ticksSinceLastEmitted++;
                }
            }
            else if (mote == null)
            {
                Emit();
            }
        }
        protected void Emit()
        {
            mote = HarpyMote(parent.DrawPos, parent.Map, Props.mote, Props.scale, Props.rotationRate, Props.rotation, Props.angle, Props.speed);
        }
        public static Mote HarpyMote(Vector3 loc, Map map, ThingDef moteDef, FloatRange scale, IntRange rotationRate, IntRange rotation, IntRange angle, FloatRange speed)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return null;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(moteDef, null);
            moteThrown.Scale = Rand.Range(scale.min, scale.max);
            moteThrown.rotationRate = Rand.Range(rotationRate.min, rotationRate.max);
            moteThrown.exactRotation = Rand.Range(rotation.min, rotation.max);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
            moteThrown.SetVelocity(Rand.Range(angle.min, angle.max), Rand.Range(speed.min, speed.max));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
            return moteThrown;
        }


        public int ticksSinceLastEmitted;
        protected Mote mote;
    }


    public class CompProperties_HarpyMoteEmitter : CompProperties
    {
        public CompProperties_HarpyMoteEmitter()
        {
            this.compClass = typeof(Comp_HarpyMoteEmitter);
        }
        public ThingDef mote;
        public int emissionInterval = -1;
        public FloatRange scale;
        public IntRange rotationRate;
        public IntRange rotation;
        public IntRange angle;
        public FloatRange speed;
    }
}
