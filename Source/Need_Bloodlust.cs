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
    public class Need_Bloodlust : Need
    {
        public Need_Bloodlust(Pawn pawn) : base(pawn)
        {
            threshPercents = new List<float>
            {
                0.2f,
                0.5f
            };
        }

        protected override bool IsFrozen
        {
            get
            {
                return base.IsFrozen || pawn.InBed() || pawn.IsCaravanMember();
            }
        }

        public override int GUIChangeArrow
        {
            get
            {
                if (IsFrozen)
                {
                    return 0;
                }
                if (!GainingBloodlust)
                {
                    return -1;
                }
                return 1;
            }
        }

        private bool GainingBloodlust
        {
            get
            {
                return Find.TickManager.TicksGame < lastGainTick + 150;
            }
        }

        public override void SetInitialLevel()
        {
            CurLevel = Rand.Range(0.8f, 1.0f);
        }

        public override void NeedInterval()
        {
            if (!IsFrozen)
            {
                float cachedLevel = CurLevel;
                if (pawn.LastAttackedTarget.Pawn != null && pawn.LastAttackTargetTick + 150 > Find.TickManager.TicksGame)
                {
                    CurLevel += 0.05f;
                    lastGainTick = Find.TickManager.TicksGame;
                }
                else
                {
                    if (CurLevel < 0.2f)
                    {
                        CurLevel -= 0.000375f;
                    }
                    else if (CurLevel < 0.5f)
                    {
                        CurLevel -= 0.0005f;
                    }
                    else
                    {
                        CurLevel -= 0.000625f;
                    }
                }
                if ((cachedLevel <= 0.2f && CurLevel >= 0.2f) || (cachedLevel >= 0.2f && CurLevel <= 0.2f) || (cachedLevel <= 0.5f && CurLevel >= 0.5f) || (cachedLevel >= 0.5f && CurLevel <= 0.5f))
                {
                    BloodlustAlertUtility.Notify_BloodlustChanged(pawn, CurLevel);
                } 
            }
            if (pawn.RaceProps.Humanlike && (pawn.Spawned || pawn.IsCaravanMember()) && pawn.MentalStateDef == null && CurLevel < 0.2f)
            {
                if (Rand.MTBEventOccurs(CurLevel * 5f, 60000f, 150f) && !pawn.Downed && pawn.Awake() && !pawn.InMentalState)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "HarpyNeed_BloodlustBreak".Translate(), false, true, null, false);
                    GainBloodlust(0.25f);
                }
            }
        }

        public void GainBloodlust(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            amount = Mathf.Min(amount, 1f - CurLevel);
            curLevelInt += amount;
            BloodlustAlertUtility.Notify_BloodlustChanged(pawn, curLevelInt);
            lastGainTick = Find.TickManager.TicksGame;
        }
        private int lastGainTick;
    }

    public class Alert_MinorBloodlust : Alert
    {
        public Alert_MinorBloodlust()
        {
            defaultPriority = AlertPriority.High;
        }

        public override string GetLabel()
        {
            return "MinorBloodlustAlert".Translate();
        }

        public override TaggedString GetExplanation()
        {
            return BloodlustAlertUtility.cachedBloodlustExplanation;
        }

        public override AlertReport GetReport()
        {
            if (!BloodlustAlertUtility.pawnsAtMajorBloodlust.Any())
            {
                return AlertReport.CulpritsAre(BloodlustAlertUtility.pawnsAtMinorBloodlust);
            }
            return false;
        }
    }

    public class Alert_MajorBloodlust : Alert_Critical
    {
        public override string GetLabel()
        {
            return "MajorBloodlustAlert".Translate();
        }

        public override void AlertActiveUpdate()
        {
        }

        public override TaggedString GetExplanation()
        {
            return BloodlustAlertUtility.cachedBloodlustExplanation;
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(BloodlustAlertUtility.pawnsAtMajorBloodlust);
        }
    }

    public static class BloodlustAlertUtility
    {
        public static void Notify_BloodlustChanged(Pawn pawn, float needLevel)
        {
            if (needLevel <= 0.2f)
            {
                if (pawnsAtMinorBloodlust.Contains(pawn))
                {
                    pawnsAtMinorBloodlust.Remove(pawn);
                }
                if (!pawnsAtMajorBloodlust.Contains(pawn))
                {
                    pawnsAtMajorBloodlust.Add(pawn);
                }
            }
            else if (needLevel <= 0.5f)
            {
                if (!pawnsAtMinorBloodlust.Contains(pawn))
                {
                    pawnsAtMinorBloodlust.Add(pawn);
                }
                if (pawnsAtMajorBloodlust.Contains(pawn))
                {
                    pawnsAtMajorBloodlust.Remove(pawn);
                }
            }
            else if (needLevel > 0.5f)
            {
                if (pawnsAtMinorBloodlust.Contains(pawn))
                {
                    pawnsAtMinorBloodlust.Remove(pawn);
                }
                if (pawnsAtMajorBloodlust.Contains(pawn))
                {
                    pawnsAtMajorBloodlust.Remove(pawn);
                }
            }
            cachedBloodlustExplanation = BloodlustAlertExplanation;
        }

        public static string BloodlustAlertExplanation
        {
            get
            {
                StringBuilder mainString = new StringBuilder();
                if (pawnsAtMinorBloodlust.Any())
                {
                    StringBuilder minorString = new StringBuilder();
                    foreach (Pawn pawn in pawnsAtMinorBloodlust)
                    {
                        minorString.AppendLine("  - " + pawn.NameShortColored.Resolve());
                    }
                    mainString.Append("MinorBloodlustAlertDesc".Translate(minorString).Resolve());
                }
                if (pawnsAtMajorBloodlust.Any())
                {
                    if (mainString.Length != 0)
                    {
                        mainString.AppendLine();
                    }
                    StringBuilder majorString = new StringBuilder();
                    foreach (Pawn pawn in pawnsAtMajorBloodlust)
                    {
                        majorString.AppendLine("  - " + pawn.NameShortColored.Resolve());
                    }
                    mainString.Append("MajorBloodlustAlertDesc".Translate(majorString).Resolve());
                }
                return mainString.ToString();
            }
        }
        public static string cachedBloodlustExplanation = "";
        public static List<Pawn> pawnsAtMinorBloodlust = new List<Pawn>();
        public static List<Pawn> pawnsAtMajorBloodlust = new List<Pawn>();

    }
}
