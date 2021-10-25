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
using Verse.Grammar;
using System.Reflection.Emit;
using System.Xml;

namespace SyrHarpy
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("Syrchalis.Rimworld.Harpy");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (!ThrumkinActive && !NagaActive)
            {
                harmony.Patch(AccessTools.Method(typeof(GrammarUtility), nameof(GrammarUtility.RulesForPawn), new Type[]
                { typeof(string), typeof(Name), typeof(string), typeof(PawnKindDef), typeof(Gender), typeof(Faction), typeof(int), typeof(int), typeof(string),
                typeof(bool), typeof(bool), typeof(bool), typeof(List<RoyalTitle>), typeof(Dictionary<string, string>), typeof(bool) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(RulesForPawnPatch), nameof(RulesForPawnPatch.RulesForPawn_Postfix))), null, null);
            }
            if (!IndividualityActive)
            {
                harmony.Patch(AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight)),
                    new HarmonyMethod(AccessTools.Method(typeof(RandomSelectionWeightPatch), nameof(RandomSelectionWeightPatch.RandomSelectionWeight_Prefix))), null, null, null);
                harmony.Patch(AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor)),
                    new HarmonyMethod(AccessTools.Method(typeof(SecondaryLovinChanceFactorPatch), nameof(SecondaryLovinChanceFactorPatch.SecondaryLovinChanceFactor_Prefix))), null, null, null);
            }
        }
        public static bool ThrumkinActive => ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageId == "syrchalis.thrumkin");
        public static bool NagaActive => ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageId == "syrchalis.naga");
        public static bool IndividualityActive => ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageId == "syrchalis.individuality");
    }

    // Effects of eating chili or human meat
    // Has to be prefix to get the stackcount
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public class IngestedPatch
    {
        [HarmonyPrefix]
        public static void Ingested_Prefix(Thing __instance, Pawn ingester, float nutritionWanted, ref int __state)
        {
            if (ingester != null && __instance.def == HarpyDefOf.RawHarpyChilis && ingester.def != HarpyDefOf.Harpy)
            {
                __state = IngestedCalculateStackCount(__instance, ingester, nutritionWanted);
            }
        }

        [HarmonyPostfix]
        public static void Ingested_Postfix(Thing __instance, Pawn ingester, float nutritionWanted, int __state)
        {
            if (ingester != null)
            {
                CompIngredients compIngr = __instance.TryGetComp<CompIngredients>();
                Need_Bloodlust need = ingester.needs.TryGetNeed(HarpyDefOf.Bloodlust) as Need_Bloodlust;
                if (compIngr?.ingredients != null)
                {
                    bool chili = compIngr.ingredients.Contains(HarpyDefOf.RawHarpyChilis);
                    bool human = compIngr.ingredients.Contains(ThingDefOf.Meat_Human);
                    if (chili && ingester.def != HarpyDefOf.Harpy)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HarpyDefOf.HarpyChiliBurn, ingester, null);
                        hediff.Severity = 0.4f;
                        ingester.health.AddHediff(hediff);
                    }
                    else if (chili && human && ingester.def == HarpyDefOf.Harpy && need != null)
                    {
                        need.GainBloodlust(0.1f);
                        ingester.needs.mood.thoughts.memories.TryGainMemory(HarpyDefOf.AteHarpySpecial);
                    }
                    else if (chili && ingester.def == HarpyDefOf.Harpy && need != null)
                    {
                        need.GainBloodlust(0.05f);
                        ingester.needs.mood.thoughts.memories.TryGainMemory(HarpyDefOf.AteHarpyChili);
                    }
                    else if (human && ingester.def == HarpyDefOf.Harpy && need != null)
                    {
                        need.GainBloodlust(0.05f);
                        ingester.needs.mood.thoughts.memories.TryGainMemory(HarpyDefOf.AteHumanMeatAsHarpy);
                    }
                }
                if (__instance.def == HarpyDefOf.RawHarpyChilis && ingester.def != HarpyDefOf.Harpy && ingester.HealthScale > 0.4f)
                {
                    Hediff hediff = HediffMaker.MakeHediff(HarpyDefOf.HarpyChiliBurn, ingester, null);
                    hediff.Severity = __state * 0.05f / ingester.BodySize;
                    ingester.health.AddHediff(hediff);
                }
                else if (__instance?.def?.ingestible?.sourceDef?.race != null && __instance.def.ingestible.sourceDef == ThingDefOf.Human && ingester.def == HarpyDefOf.Harpy && need != null)
                {
                    need.GainBloodlust(0.05f);
                    ingester.needs.mood.thoughts.memories.TryGainMemory(HarpyDefOf.AteHumanMeatAsHarpy);
                }
                else if (__instance.def == HarpyDefOf.RawHarpyChilis && ingester.def == HarpyDefOf.Harpy && need != null)
                {
                    need.GainBloodlust(0.05f);
                    ingester.needs.mood.thoughts.memories.TryGainMemory(HarpyDefOf.AteHarpyChili);
                }
            }
        }
        public static int IngestedCalculateStackCount(Thing thing, Pawn ingester, float nutritionWanted)
        {
            int numTaken = Mathf.CeilToInt(nutritionWanted / thing.GetStatValue(StatDefOf.Nutrition, true));
            numTaken = Mathf.Min(new int[]
            {
                numTaken,
                thing.def.ingestible.maxNumToIngestAtOnce,
                thing.stackCount
            });
            return Mathf.Max(numTaken, 1);
        }
    }

    //Allow growing zone on low fertility
    [HarmonyPatch(typeof(Designator_ZoneAdd_Growing), nameof(Designator_ZoneAdd_Growing.CanDesignateCell))]
    public class GrowingZone_CanDesignateCellPatch
    {
        [HarmonyPostfix]
        public static void GrowingZone_CanDesignateCell_Postfix(ref AcceptanceReport __result, Designator_ZoneAdd_Growing __instance, IntVec3 c)
        {
            if (__instance.Map.fertilityGrid.FertilityAt(c) >= HarpyDefOf.Plant_HarpyChili.plant.fertilityMin)
            {
                __result = true;
            }
        }
    }

    //Social interaction modifiers
    [HarmonyPatch(typeof(NegativeInteractionUtility), nameof(NegativeInteractionUtility.NegativeInteractionChanceFactor))]
    public class NegativeInteractionChanceFactorPatch
    {
        [HarmonyPostfix]
        public static void NegativeInteractionChanceFactor_Postfix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator != null && recipient != null && initiator.def == HarpyDefOf.Harpy && recipient.def == HarpyDefOf.Harpy)
            {
                __result *= 2f;
            }
            else if (initiator != null && initiator.def == HarpyDefOf.Harpy)
            {
                __result *= 1.25f;
            }
            if (initiator.needs.TryGetNeed(HarpyDefOf.Bloodlust) is Need_Bloodlust need && need.CurLevel < 0.5f)
            {
                __result *= 1f / (need.CurLevel * 2f);
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.SocialFightChance))]
    public class SocialFightChancePatch
    {
        [HarmonyPostfix]
        public static void SocialFightChance_Postfix(ref float __result, InteractionDef interaction, Pawn initiator, Pawn ___pawn)
        {
            if (___pawn != null && initiator != null && ___pawn.def == HarpyDefOf.Harpy && initiator.def == HarpyDefOf.Harpy)
            {
                __result *= 4f;
            }
            else if (___pawn != null && ___pawn.def == HarpyDefOf.Harpy)
            {
                __result *= 2f;
            }
        }
    }

    //If flight capable harpies will not be slowed by terrain path cost
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(Pawn), typeof(IntVec3) })]
    public class CostToMoveIntoCellPatch
    {
        [HarmonyPostfix]
        public static void CostToMoveIntoCell_Postfix(ref int __result, Pawn pawn, IntVec3 c)
        {
            if (pawn?.Map != null && c != null && pawn.def == HarpyDefOf.Harpy && HarpyUtility.FlightCapabable(pawn))
            {
                __result -= pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(c, false, pawn.Position);
            }
        }
    }

    //If flight capable pathing AI will not be influenced by terrain path cost
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning) } )]
    public static class FindPathPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FindPath_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();
            var nonDraftedCost = AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.extraNonDraftedPerceivedPathCost));
            var draftedCost = AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.extraDraftedPerceivedPathCost));
            for (int i = 0; i < instructionList.Count; i++)
            {
                // Look for the section that checks the terrain grid pathCosts
                if (i >=3 && instructionList[i - 3].opcode == OpCodes.Ldfld && (instructionList[i - 3].OperandIs(nonDraftedCost) || instructionList[i - 3].OperandIs(draftedCost)))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 46); //total Pathcost to subtract from later

                    yield return new CodeInstruction(OpCodes.Ldloc_0); //pawn
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 46); // total Pathcost
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 12); // topGrid
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 43); // index
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref); //gets TerrainDef based on topGrid[index]
                    yield return new CodeInstruction(OpCodes.Call, typeof(FindPathPatch).GetMethod(nameof(FindPathPatch.TerrainPathCost))); // TerrainPathCost (num17, num15, topGrid, parms)

                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Stloc_S, 46);
                }
                yield return instructionList[i];
            }
        }

        public static int TerrainPathCost(Pawn pawn, int cost, TerrainDef terrainDef)
        {
            return (pawn != null && pawn.def == HarpyDefOf.Harpy && HarpyUtility.FlightCapabable(pawn) && terrainDef != null) ? terrainDef.pathCost : 0;
        }
    }

    //If flight capable harpies don't get wet over water/marsh
    [HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.MindStateTick))]
    public static class MindStateTickPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MindStateTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo FlightCapabable = AccessTools.Method(typeof(MindStateTickPatch), nameof(MindStateTickPatch.FlightCapabableOver));
            FieldInfo pawn = AccessTools.Field(typeof(Pawn_MindState), nameof(Pawn_MindState.pawn));
            bool found = false;
            bool done = false;
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Ldloc_1 && !done)
                {
                    found = true;
                }
                else if (i.opcode == OpCodes.Brfalse_S && found)
                {
                    yield return i;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);
                    yield return new CodeInstruction(OpCodes.Call, FlightCapabable);
                    yield return new CodeInstruction(OpCodes.Brtrue_S, i.operand);
                    done = true;
                    found = false;
                    continue;
                }
                yield return i;
            }
        }
        public static bool FlightCapabableOver(Pawn pawn)
        {
            if (pawn.Position.GetTerrain(pawn.Map).traversedThought != null)
            {
                return HarpyUtility.FlightCapabable(pawn);
            }
            return false;
        }
    }

    //Disallow Gloves/Boots
    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class HasPartsToWearPatch
    {
        [HarmonyPostfix]
        public static void HasPartsToWear_Postfix(ref bool __result, Pawn p, ThingDef apparel)
        {
            if (apparel?.apparel?.bodyPartGroups != null && p?.def != null)
            {
                if (apparel.apparel.bodyPartGroups.Contains(HarpyDefOf.Feet) && !apparel.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) && p.def == HarpyDefOf.Harpy)
                {
                    __result = false;
                }
                if (apparel.apparel.bodyPartGroups.Contains(HarpyDefOf.Hands) && !apparel.apparel.bodyPartGroups.Contains(HarpyDefOf.Arms) && p.def == HarpyDefOf.Harpy)
                {
                    __result = false;
                }
            }
        }
    }

    //Allow Wings to have their own efficiency even if attached to prosthetic arms
    [HarmonyPatch(typeof(PawnCapacityUtility), nameof(PawnCapacityUtility.CalculatePartEfficiency))]
    public class CalculatePartEfficiencyPatch
    {
        [HarmonyPostfix]
        public static void CalculatePartEfficiency_Postfix(ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts = false, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            if (part != null && (part.def == HarpyDefOf.WingHarpy || part.def == HarpyDefOf.HandClawsHarpy || part.def == HarpyDefOf.FootClawsHarpy))
            {
                if (part.parent != null && diffSet.PartIsMissing(part.parent))
                {
                    __result = 0f;
                }
                float num = 1f;
                if (!ignoreAddedParts)
                {
                    for (int i = 0; i < diffSet.hediffs.Count; i++)
                    {
                        Hediff_AddedPart hediff_AddedPart2 = diffSet.hediffs[i] as Hediff_AddedPart;
                        if (hediff_AddedPart2 != null && hediff_AddedPart2.Part == part)
                        {
                            num *= hediff_AddedPart2.def.addedPartProps.partEfficiency;
                            if (hediff_AddedPart2.def.addedPartProps.partEfficiency != 1f && impactors != null)
                            {
                                impactors.Add(new PawnCapacityUtility.CapacityImpactorHediff
                                {
                                    hediff = hediff_AddedPart2
                                });
                            }
                        }
                    }
                }
                float b = -1f;
                float num2 = 0f;
                bool flag = false;
                for (int j = 0; j < diffSet.hediffs.Count; j++)
                {
                    if (diffSet.hediffs[j].Part == part && diffSet.hediffs[j].CurStage != null)
                    {
                        HediffStage curStage = diffSet.hediffs[j].CurStage;
                        num2 += curStage.partEfficiencyOffset;
                        flag |= curStage.partIgnoreMissingHP;
                        if (curStage.partEfficiencyOffset != 0f && curStage.becomeVisible && impactors != null)
                        {
                            impactors.Add(new PawnCapacityUtility.CapacityImpactorHediff
                            {
                                hediff = diffSet.hediffs[j]
                            });
                        }
                    }
                }
                if (!flag)
                {
                    float num3 = diffSet.GetPartHealth(part) / part.def.GetMaxHealth(diffSet.pawn);
                    if (num3 != 1f)
                    {
                        if (DamageWorker_AddInjury.ShouldReduceDamageToPreservePart(part))
                        {
                            num3 = Mathf.InverseLerp(0.1f, 1f, num3);
                        }
                        if (impactors != null)
                        {
                            impactors.Add(new PawnCapacityUtility.CapacityImpactorBodyPartHealth
                            {
                                bodyPart = part
                            });
                        }
                        num *= num3;
                    }
                }
                num += num2;
                if (num > 0.0001f)
                {
                    num = Mathf.Max(num, b);
                }
                __result = Mathf.Max(num, 0f);
            }
        }
    }


    //Hide LightningWeapon in ITab
    [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow")]
    public static class DrawThingRowPatch
    {
        [HarmonyPrefix]
        public static bool DrawThingRow_Prefix(ITab_Pawn_Gear __instance, ref float y, float width, Thing thing, bool inventory = false)
        {
            Pawn pawn = (Pawn)AccessTools.Property(typeof(ITab_Pawn_Gear), "SelPawnForGear").GetGetMethod(true).Invoke(__instance, null);
            if (pawn != null && pawn.def == HarpyDefOf.Harpy && thing != null && HarpyUtility.IsHarpyLightningWeapon(thing.def))
            {
                return false;
            }
            return true;
        }
    }


    //Prevent LightningWeapon to be dropped when downed or stripped (does prevent any sort of dropping however)
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    public static class TryDropEquipmentPatch
    {
        [HarmonyPrefix]
        public static bool TryDropEquipment_Prefix(ThingWithComps eq)
        {
            if (eq != null && HarpyUtility.IsHarpyLightningWeapon(eq.def))
            {
                return false;
            }
            return true;
        }
    }
    //Hide LightningWeapon in MeleeDPS Hyperlinks
    [HarmonyPatch(typeof(StatWorker_MeleeDPS), nameof(StatWorker_MeleeDPS.GetInfoCardHyperlinks))]
    public static class GetInfoCardHyperlinksPatch
    {
        [HarmonyPrefix]
        public static bool GetInfoCardHyperlinks_Prefix(StatRequest statRequest)
        {
            if (statRequest.Thing.def == HarpyDefOf.Harpy)
            {
                return false;
            }
            return true;
        }
    }
    //Gizmo with values about the lightning weapon
    [HarmonyPatch(typeof(VerbTracker), "CreateVerbTargetCommand")]
    public static class CreateVerbTargetCommandPatch
    {
        [HarmonyPostfix]
        public static void CreateVerbTargetCommand_Postfix(Command_VerbTarget __result, VerbTracker __instance, Thing ownerThing, Verb verb)
        {
            if (ownerThing != null && HarpyUtility.IsHarpyLightningWeapon(ownerThing.def) && verb.caster != null)
            {
                Pawn pawn = verb.caster as Pawn;
                if (pawn != null)
                {
                    __result.defaultDesc =
                        ownerThing.def.description.CapitalizeFirst() + "\n\n" +
                        "HarpyLightning_Damage".Translate() + ": " + (14 + 2 * HarpyUtility.HarpyAmplifierLevel(pawn)).ToString() + 
                        " (" + verb.verbProps.defaultProjectile.projectile.damageDef.armorCategory.ToString() + ") " + "\n" +
                        "HarpyLightning_Range".Translate() + ": " + (verb.verbProps.range + 0.1f).ToString() + "\n" +
                        "HarpyLightning_Casttime".Translate() + ": " + verb.verbProps.warmupTime.ToString() + "\n" +
                        "HarpyLightning_Cooldown".Translate() + ": " + ownerThing.GetStatValue(StatDefOf.RangedWeapon_Cooldown, true).ToString();
                }
            }
        }
    }

    //Add lightningWeapon if harpy does not have one once recruited
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.DoRecruit), 
        new Type[] { typeof(Pawn), typeof(Pawn), typeof(string), typeof(string), typeof(bool), typeof(bool) }, 
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    public static class DoRecruitPatch
    {
        // Alternate way
        /*[HarmonyTargetMethod]
        static MethodBase CalculateMethod()
        {
            return AccessTools.Method(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.DoRecruit), new Type[] {
        typeof(Pawn), typeof(Pawn), typeof(float), typeof(string).MakeByRefType(), typeof(string).MakeByRefType(), typeof(bool), typeof(bool) }); ;
        }*/

        [HarmonyPostfix]
        public static void DoRecruit_Postfix(Pawn recruiter, Pawn recruitee)
        {
            if (recruitee != null && recruitee.def == HarpyDefOf.Harpy)
            {
                if (recruitee.equipment?.Primary?.def == null || !HarpyUtility.IsHarpyLightningWeapon(recruitee.equipment.Primary.def))
                {
                    ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(HarpyDefOf.Harpy_LightningWeaponZero, null);
                    recruitee.equipment.DestroyEquipment(recruitee.equipment.Primary);
                    recruitee.equipment.AddEquipment(thingWithComps);
                }
            }
        }
    }

    //Doubles fall rates of outdoor need for harpies
    [HarmonyPatch(typeof(Need_Outdoors), nameof(Need_Outdoors.NeedInterval))]
    public static class NeedOutdoors_NeedIntervalPatch
    {
        [HarmonyPostfix]
        public static void NeedOutdoors_NeedInterval_Postfix(Need_Outdoors __instance, Pawn ___pawn)
        {
            if (___pawn?.Position != null && ___pawn.def == HarpyDefOf.Harpy)
            {
                float b = 0.2f;
                RoofDef roofDef = ___pawn.Spawned ? ___pawn.Position.GetRoof(___pawn.Map) : null;
                float num;
                if (roofDef != null && !___pawn.Position.UsesOutdoorTemperature(___pawn.Map))
                {
                    if (!roofDef.isThickRoof)
                    {
                        num = -0.32f;
                    }
                    else
                    {
                        num = -0.45f;
                        b = 0f;
                    }
                }
                else if (roofDef != null && roofDef.isThickRoof)
                {
                    num = -0.4f;
                }
                else
                {
                    num = 0f;
                }
                if (___pawn.InBed() && num < 0f)
                {
                    num *= 0.2f;
                }
                num *= 0.0025f;
                float curLevel = __instance.CurLevel;
                if (num < 0f)
                {
                    __instance.CurLevel = Mathf.Min(__instance.CurLevel, Mathf.Max(__instance.CurLevel + num, b));
                }
                else
                {
                    __instance.CurLevel = Mathf.Min(__instance.CurLevel + num, 1f);
                }
            }
        }
    }

    //Allows harpies to do joy activities for 20sec
    [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd))]
    public static class JoyTickCheckEndPatch
    {
        [HarmonyPrefix]
        public static bool JoyTickCheckEnd_Prefix(Pawn pawn, JoyTickFullJoyAction fullJoyAction)
        {
            if (pawn != null && pawn.def == HarpyDefOf.Harpy)
            {
                Job curJob = pawn.CurJob;
                if (curJob.def.joyKind == null)
                {
                    Log.Warning("This method can only be called for jobs with joyKind.");
                    return false;
                }
                if (curJob.def.joySkill != null)
                {
                    pawn.skills.GetSkill(curJob.def.joySkill).Learn(curJob.def.joyXpPerTick, false);
                }
                if (!curJob.ignoreJoyTimeAssignment && !pawn.GetTimeAssignment().allowJoy)
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                }
                if (curJob.startTick + 1200 < Find.TickManager.TicksGame)
                {
                    if (fullJoyAction == JoyTickFullJoyAction.EndJob)
                    {
                        pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                    }
                    if (fullJoyAction == JoyTickFullJoyAction.GoToNextToil)
                    {
                        pawn.jobs.curDriver.ReadyForNextToil();
                    }
                }
                return false;
            }
            return true;
        }
    }

    //Prevents harpies from using nurseable drugs more than once a day
    [HarmonyPatch(typeof(JoyGiver_SocialRelax), "TryFindIngestibleToNurse")]
    public static class TryFindIngestibleToNursePatch
    {
        [HarmonyPostfix]
        public static void TryFindIngestibleToNurse_Postfix(ref bool __result, IntVec3 center, Pawn ingester, Thing ingestible)
        {
            if (ingester != null && ingester.def == HarpyDefOf.Harpy && __result && ingestible != null)
            {
                HarpyComp comp = ingester.TryGetComp<HarpyComp>();
                if (comp != null)
                {
                    foreach (KeyValuePair<ThingDef, int> keyValuePair in comp.drugsNursedToday)
                    {
                        Log.Message("Ingestible: " + keyValuePair.Key + " | Ticks: " + (Find.TickManager.TicksGame - keyValuePair.Value));
                    }
                    if (comp.drugsNursedToday.ContainsKey(ingestible.def))
                    {
                        ingestible = null;
                        __result = false;
                    }
                    else
                    {
                        comp.drugsNursedToday.Add(ingestible.def, Find.TickManager.TicksGame);
                    }
                }
            }
        }
    }
    //Fixes alert for pawns without joy need
    [HarmonyPatch(typeof(Alert_Boredom), "BoredPawns", MethodType.Getter)]
    public static class BoredPawnsPatch
    {
        [HarmonyPrefix]
        public static bool BoredPawns_Prefix(ref List<Pawn> __result, Alert_Boredom __instance, List<Pawn> ___boredPawnsResult)
        {
            ___boredPawnsResult.Clear();
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (pawn != null && pawn.def != HarpyDefOf.Harpy && (pawn.needs.joy.CurLevelPercentage < 0.24000001f || pawn.GetTimeAssignment() == TimeAssignmentDefOf.Joy) && pawn.needs.joy.tolerances.BoredOfAllAvailableJoyKinds(pawn))
                {
                    ___boredPawnsResult.Add(pawn);
                }
            }
            __result = ___boredPawnsResult;
            return false;
        }
    }
    //NREs during parties for harpies
    /*[HarmonyPatch(typeof(LordToil_Party), nameof(LordToil_Party.LordToilTick))]
    public static class LordToilTickPatch
    {
        [HarmonyPrefix]
        public static bool LordToilTick_Prefix(LordToil_Party __instance, IntVec3 ___spot, float ___joyPerTick)
        {
            List<Pawn> ownedPawns = __instance.lord.ownedPawns;
            for (int i = 0; i < ownedPawns.Count; i++)
            {
                if (GatheringsUtility.InGatheringArea(ownedPawns[i].Position, ___spot, __instance.Map))
                {
                    if (ownedPawns[i].needs.joy != null)
                    {
                        ownedPawns[i].needs.joy.GainJoy(___joyPerTick, JoyKindDefOf.Social);
                    }
                    LordToilData_Party dataParty = __instance.data as LordToilData_Party;
                    if (!dataParty.presentForTicks.ContainsKey(ownedPawns[i]))
                    {
                        dataParty.presentForTicks.Add(ownedPawns[i], 0);
                    }
                    Dictionary<Pawn, int> presentForTicks = dataParty.presentForTicks;
                    Pawn key = ownedPawns[i];
                    int num = presentForTicks[key];
                    presentForTicks[key] = num + 1;
                }
            }
            return false;
        }
    }*/
    [HarmonyPatch(typeof(JobGiver_GetJoy), "TryGiveJob")]
    public static class JobGiver_GetJoyPatch
    {
        [HarmonyPrefix]
        public static bool JobGiver_GetJoy_Prefix(Job __result, Pawn pawn)
        {
            if (pawn.def == HarpyDefOf.Harpy)
            {
                return false;
            }
            return true;
        }
    }
    //Disables joy need
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class ShouldHaveNeedPatch
    {
        [HarmonyPostfix]
        public static void ShouldHaveNeed_Postfix(ref bool __result, Pawn_NeedsTracker __instance, Pawn ___pawn, NeedDef nd)
        {
            if (___pawn != null && ___pawn.def != HarpyDefOf.Harpy && nd == HarpyDefOf.Bloodlust)
            {
                __result = false;
            }
            if (___pawn != null && ___pawn.def == HarpyDefOf.Harpy && nd == NeedDefOf.Joy)
            {
                __result = false;
            }
        }
    }

    //Allows adding of hediffs directly to pawnkinds
    public class SyrHarpyExtension : DefModExtension
    {
        public List<TechHediffCount> requiredProsthetics;
    }
    public class TechHediffCount
    {
        public TechHediffCount()
        {
        }
        public TechHediffCount(HediffDef hediffDef, int count)
        {
            this.hediffDef = hediffDef;
            this.count = count;
        }
        public HediffDef hediffDef;
        public int count;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1) Log.Error("");
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediffDef", xmlRoot.Name, null, null);
                count = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
            }
        }
    }

    [HarmonyPatch(typeof(PawnTechHediffsGenerator), nameof(PawnTechHediffsGenerator.GenerateTechHediffsFor))]
    public static class GenerateTechHediffsForPatch
    {
        [HarmonyPostfix]
        public static void GenerateTechHediffsFor_Postfix(Pawn pawn)
        {
            if (pawn?.kindDef != null)
            {
                PawnKindDef pawnkind = pawn.kindDef;
                SyrHarpyExtension extension = pawnkind.GetModExtension<SyrHarpyExtension>();
                if (extension?.requiredProsthetics != null)
                {
                    foreach (TechHediffCount thc in extension.requiredProsthetics)
                    {
                        IEnumerable<RecipeDef> source = from x in DefDatabase<RecipeDef>.AllDefs
                                                        where x.addsHediff == thc.hediffDef && pawn.def.AllRecipes.Contains(x)
                                                        select x;
                        if (source.Any())
                        {
                            RecipeDef recipeDef = source.RandomElement();
                            for (int i = 0; i < thc.count; i++)
                            {
                                if (recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).Any())
                                {
                                    recipeDef.Worker.ApplyOnPawn(pawn, recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).RandomElement(), null, new List<Thing>(), null);
                                }
                            }
                        }
                        if (thc.hediffDef.hediffClass == typeof(Hediff_Level))
                        {
                            IEnumerable<ThingDef> thingDefs = from t in DefDatabase<ThingDef>.AllDefs
                                                            where (t.HasComp(typeof(CompUseEffect_InstallImplant)) || t.HasComp(typeof(CompUseEffect_LightningImplant))) && t.GetCompProperties<CompProperties_UseEffectInstallImplant>().hediffDef == thc.hediffDef
                                                            select t;
                            if (thingDefs.Any())
                            {
                                ThingDef thingDef = thingDefs.RandomElement();
                                BodyPartRecord record = pawn.health.hediffSet.GetNotMissingParts().First((BodyPartRecord x) => x.def == thingDef.GetCompProperties<CompProperties_UseEffectInstallImplant>().bodyPart);
                                int level = (int)Mathf.Clamp(thc.count, thc.hediffDef.minSeverity, thc.hediffDef.maxSeverity);
                                if (level > 0)
                                {
                                    Hediff_Level hediffLevel = HediffMaker.MakeHediff(thc.hediffDef, pawn, record) as Hediff_Level;
                                    hediffLevel.level = level;
                                    pawn.health.AddHediff(hediffLevel);
                                }
                                if (thingDef.HasComp(typeof(CompUseEffect_LightningImplant)))
                                {
                                    HarpyUtility.SwapLightningWeapon(pawn, level);
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    //Manual patch if individuality not active - Makes harpies bisexual
    public class RandomSelectionWeightPatch
    {
        public static bool RandomSelectionWeight_Prefix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator != null && recipient != null && initiator.def == HarpyDefOf.Harpy)
            {
                if (TutorSystem.TutorialMode)
                {
                    __result = 0f;
                }
                if (LovePartnerRelationUtility.LovePartnerRelationExists(initiator, recipient))
                {
                    __result = 0f;
                }
                float baseChance = initiator.relations.SecondaryRomanceChanceFactor(recipient);
                if (baseChance < 0.15f)
                {
                    __result = 0f;
                }
                int opinionOfOther = initiator.relations.OpinionOf(recipient);
                if (opinionOfOther < 5)
                {
                    __result = 0f;
                }
                if (recipient.relations.OpinionOf(initiator) < 5)
                {
                    __result = 0f;
                }
                float adulteryFactor = 1f;
                Pawn pawn = LovePartnerRelationUtility.ExistingMostLikedLovePartner(initiator, false);
                if (pawn != null)
                {
                    float opinionOfSpouse = initiator.relations.OpinionOf(pawn);
                    adulteryFactor = Mathf.InverseLerp(50f, -50f, opinionOfSpouse);
                }
                float num = Mathf.InverseLerp(0.15f, 1f, baseChance);
                float opinionFactor = Mathf.InverseLerp(5f, 100f, opinionOfOther);
                __result = 1.15f * num * opinionFactor * adulteryFactor;
                return false;
            }
            return true;
        }
    }

    public class SecondaryLovinChanceFactorPatch
    {
        [HarmonyPrefix]
        public static bool SecondaryLovinChanceFactor_Prefix(ref float __result, Pawn otherPawn, Pawn ___pawn)
        {
            if (___pawn != null && otherPawn != null && ___pawn.def == HarpyDefOf.Harpy)
            {
                if (___pawn == otherPawn)
                {
                    __result = 0f;
                }
                float ageBiologicalYearsFloat = ___pawn.ageTracker.AgeBiologicalYearsFloat;
                float ageBiologicalYearsFloat2 = otherPawn.ageTracker.AgeBiologicalYearsFloat;
                if (ageBiologicalYearsFloat < 16f || ageBiologicalYearsFloat2 < 16f)
                {
                    __result = 0f;
                }
                float ageFactor = 1f;
                float min = ageBiologicalYearsFloat - 20f;
                float lower = ageBiologicalYearsFloat - 7f;
                float upper = ageBiologicalYearsFloat + 7f;
                float max = ageBiologicalYearsFloat + 20f;
                ageFactor = GenMath.FlatHill(0.2f, min, lower, upper, max, 0.2f, ageBiologicalYearsFloat2);
                float beauty = 0f;
                if (otherPawn.RaceProps.Humanlike)
                {
                    beauty = otherPawn.GetStatValue(StatDefOf.PawnBeauty, true);
                }
                float beautyFactor = 1f;
                if (beauty < 0f)
                {
                    beautyFactor = 0.3f;
                }
                else if (beauty > 0f)
                {
                    beautyFactor = 2.3f;
                }
                __result = ageFactor * beautyFactor;
                return false;
            }
            return true;
        }
    }

    //Manual patch if neither Thrumkin nor Naga active - Make joiner quests show race before title
    public static class RulesForPawnPatch
    {
        public static IEnumerable<Rule> RulesForPawn_Postfix(IEnumerable<Rule> __result, string pawnSymbol, string title, Gender gender, PawnKindDef kind)
        {
            List<Rule> ruleList = __result.ToList();
            string prefix = "";
            if (!pawnSymbol.NullOrEmpty())
            {
                prefix = prefix + pawnSymbol + "_";
            }
            for (int i = 0; i < ruleList.Count; i++)
            {
                Rule_String r = ruleList[i] as Rule_String;
                if (r != null && r.keyword == (prefix + "titleIndef"))
                {
                    ruleList[i] = new Rule_String(prefix + "titleIndef", Find.ActiveLanguageWorker.WithIndefiniteArticle(kind.race.label + " " + title, gender, false, false));
                }
                else if (r != null && r.keyword == (prefix + "titleDef"))
                {
                    ruleList[i] = new Rule_String(prefix + "titleDef", Find.ActiveLanguageWorker.WithDefiniteArticle(kind.race.label + " " + title, gender, false, false));
                }
                else if (r != null && r.keyword == (prefix + "title"))
                {
                    ruleList[i] = new Rule_String(prefix + "title", kind.race.label + " " + title);
                }
            }
            __result = ruleList;
            foreach (Rule rule in __result)
            {
                yield return rule;
            }
        }
    }
}
