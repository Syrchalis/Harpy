using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace SyrHarpy
{
    class SyrHarpyCore : Mod
    {
        public static SyrHarpySettings settings;

        public SyrHarpyCore(ModContentPack content) : base(content)
        {
            settings = GetSettings<SyrHarpySettings>();
        }

        public override string SettingsCategory() => "SyrHarpySettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            checked
            {
                Listing_Standard listing_Standard = new Listing_Standard();
                listing_Standard.Begin(inRect);
                listing_Standard.CheckboxLabeled("SyrHarpy_useStandardAI".Translate(), ref SyrHarpySettings.useStandardAI, "SyrHarpy_useStandardAITooltip".Translate());
                if (listing_Standard.ButtonText("SyrHarpy_defaultSettings".Translate(), "SyrHarpy_defaultSettingsTooltip".Translate()))
                {
                    SyrHarpySettings.useStandardAI = false;
                }
                listing_Standard.End();
                settings.Write();
            }
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }
}
