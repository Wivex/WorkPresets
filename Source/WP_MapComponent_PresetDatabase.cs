using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using Verse.AI;
using RimWorld;

namespace WorkPresets
{
    // MapComponent loads when new colony starts or if injected mid game
    public class PresetDatabase : MapComponent
    {
        public static List<Preset> presetsList;
        public static Dictionary<PawnData, Preset> assignedPresets;

        public PresetDatabase()
        {
            //Log.Message("Database initialized.");
            if (PresetDatabase.assignedPresets == null)
                PresetDatabase.assignedPresets = new Dictionary<PawnData, Preset>();
            if (PresetDatabase.presetsList == null)
                PresetDatabase.presetsList = new List<Preset>();
        }
        // LookMode.Deep is required to scribe reference types
        public override void ExposeData()
        {
            //Log.Message("Database Scribed: " + Scribe.mode);
            Scribe_Collections.LookList<Preset>(ref presetsList, "presetsList", LookMode.Deep, new object[0]);
            Scribe_Collections.LookDictionary<PawnData, Preset>(ref assignedPresets, "assignedPresets", LookMode.Deep, LookMode.Deep);
        }
    }
    // IExposable is required to scribe via ExposeData()
    public class Preset : IExposable
    {
        public string name;
        public Dictionary<WorkTypeDef, int> priorities;

        public Preset()
        {
            this.name = string.Empty;
            this.priorities = new Dictionary<WorkTypeDef, int>();
        }

        public Preset(Preset preset)
        {
            this.name = preset.Name;
            this.priorities = new Dictionary<WorkTypeDef,int>(preset.priorities);
        }
        // Property is unnesessary, but used to avoid empty string error
        public string Name
        {
            get
            {
                return name ?? string.Empty;
            }
            set
            {
                name = value;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.LookValue<string>(ref name, "presetName", "Unassigned", false);
            Scribe_Collections.LookDictionary<WorkTypeDef, int>(ref priorities, "presetPriorities", LookMode.DefReference, LookMode.Value);
        }
    }
    
    // IExposable is required to scribe via ExposeData()
    public class PawnData : IExposable
    {
        public string id;
        public Dictionary<WorkTypeDef, int> priorities;

        public PawnData()
        {
            this.id = string.Empty;
            this.priorities = new Dictionary<WorkTypeDef, int>();
        }

        public PawnData(Pawn pawn)
        {
            this.id = pawn.ThingID;
            this.priorities = new Dictionary<WorkTypeDef, int>();

            List<WorkTypeDef> VisibleWorkTypeDefsInPriorityOrder = (from def in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder
                                                  where def.visible
                                                  select def).ToList<WorkTypeDef>();

            foreach (WorkTypeDef job in VisibleWorkTypeDefsInPriorityOrder)
            {
                if (!pawn.story.WorkTypeIsDisabled(job))
                    priorities.Add(job, pawn.workSettings.GetPriority(job));
            }
        }
        // Property is unnesessary, but used to avoid empty string error
        public string ID
        {
            get
            {
                return id ?? string.Empty;
            }
            set
            {
                id = value;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.LookValue<string>(ref id, "colonistId", null, false);
            Scribe_Collections.LookDictionary<WorkTypeDef, int>(ref priorities, "pawnPriorities", LookMode.DefReference, LookMode.Value);
        }
    }
}
