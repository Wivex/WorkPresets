using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace WorkPresets
{
    public class WP_MainTabWindow_Work : MainTabWindow_PawnList
    {
        public const string presetName_label = "Current Preset:";
        public string presetName_textField = string.Empty;
        public bool filterFlag = false;

        public List<WorkTypeDef> VisibleWorkTypeDefsInPriorityOrder;
        public Preset currentPreset;
        public int pawnsRowsCount;

        // letters, digits, " ", "'", "\", "-"
        public static Regex validNameRegex = new Regex("^[a-zA-Z0-9 '\\-]*$");

        // Overall tab size depends on pawns count
        public override Vector2 RequestedTabSize
        {
            get
            {
                pawnsRowsCount = (base.PawnsCount % 2 == 1) ? (base.PawnsCount / 2 + 1) : (base.PawnsCount / 2);
                return new Vector2(1010f, 120f + pawnsRowsCount * 30f + 36f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            VisibleWorkTypeDefsInPriorityOrder = (from def in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder
                                                  where def.visible
                                                  select def).ToList<WorkTypeDef>();
            // MapComponent Injector
            if (!Find.Map.components.Exists(component => component.GetType() == typeof(PresetDatabase)))
                Find.Map.components.Add(new PresetDatabase());
        }

        public override void DoWindowContents(Rect tab)
        {
            base.DoWindowContents(tab);

            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawBox(tab, 1);
            GUI.color = Color.white;
            
            // Header Field
            Rect headerRect = new Rect(0f, 0f, tab.width, 120f);
            DrawHeader(headerRect);

            // Pawns Field
            Rect pawnsTableRect = new Rect(0f, headerRect.height, tab.width, tab.height - headerRect.height);
            DrawPawnsTable(pawnsTableRect);
        }

        public void DrawHeader(Rect rect)
        {
            // Border line
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            // Background color
            GUI.DrawTexture(rect, Verse.SolidColorMaterials.NewSolidColorTexture(new Color(0.37f, 0.37f, 0.37f, 0.4f)));

            float currentY = 0f;
            float fieldHeight = 30f;

            Rect headerField1 = new Rect(0f, currentY, rect.width, fieldHeight);
            DrawFirstHeaderField(headerField1);

            currentY += fieldHeight;
            fieldHeight = rect.height - currentY;

            Rect headerField2 = new Rect(0f, currentY, rect.width, fieldHeight);
            DrawSecondHeaderField(headerField2);
        }

        public void DrawFirstHeaderField(Rect rect)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
            
            float currentX = 0f;
            float fieldWidth = 380f;
            float spacing = 5f;
            float margin = 1f;
            
            // Buttons Field
            {
                int buttonsCount = 3;
                float buttonHeight = rect.height;
                float buttonWidth = (fieldWidth - spacing * (buttonsCount - 1)) / 3;

                Rect selectRect = new Rect(currentX, 0f, buttonWidth, buttonHeight).ContractedBy(margin);
                if (Widgets.TextButton(selectRect, "Select Preset", true, false))
                {
                    if (PresetDatabase.presetsList.Count > 0)
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (Preset preset in PresetDatabase.presetsList)
                        {
                            list.Add(new FloatMenuOption(preset.Name, delegate
                            {
                                currentPreset = new Preset(preset);
                                presetName_textField = currentPreset.Name;
                            }, MenuOptionPriority.Medium, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, false));
                    }
                    else
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
                currentX += buttonWidth + spacing;

                Rect deleteRect = new Rect(currentX, 0f, buttonWidth, buttonHeight).ContractedBy(margin);
                if (Widgets.TextButton(deleteRect, "Delete Preset", true, false))
                {
                    if (PresetDatabase.presetsList.Count > 0)
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (Preset preset in PresetDatabase.presetsList)
                        {
                            list.Add(new FloatMenuOption(preset.Name, delegate
                            {
                                PresetDatabase.presetsList.Remove(preset);
                            }, MenuOptionPriority.Medium, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, false));
                    }
                    else
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
                currentX += buttonWidth + spacing;

                // Save preset button
                Rect saveRect = new Rect(currentX, 0f, buttonWidth, buttonHeight).ContractedBy(margin);
                if (Widgets.TextButton(saveRect, "Save Preset", true, false))
                {
                    if (currentPreset != null)
                    {
                        SavePreset();
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                    else
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
                currentX += buttonWidth + spacing;
            }

            // Manaul properties
            {
                currentX += 10f;
                fieldWidth = 150;
                Rect manualRect = new Rect(currentX, 3f, fieldWidth - spacing, rect.height - 3f);
                //GUI.DrawTexture(manualLabel_Rect, Verse.SolidColorMaterials.NewSolidColorTexture(Color.grey));
                Widgets.LabelCheckbox(manualRect, "Manual Priorities:", ref Find.Map.playSettings.useWorkPriorities, false);
                currentX += fieldWidth + spacing;
            }

            // Filter Flag
            {
                fieldWidth = 70;
                Rect filterRect = new Rect(currentX, 3f, fieldWidth - spacing, rect.height - 3f);
                //GUI.DrawTexture(manualLabel_Rect, Verse.SolidColorMaterials.NewSolidColorTexture(Color.grey));
                Widgets.LabelCheckbox(filterRect, "Filter:", ref filterFlag, false);
                currentX += fieldWidth + spacing;
            }

            // Work Label's Field
            {
                fieldWidth = 110;
                Rect labelRect = new Rect(currentX, 0f, fieldWidth - spacing, rect.height);
                //GUI.DrawTexture(labelfiled, Verse.SolidColorMaterials.NewSolidColorTexture(Color.gray));
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(labelRect, presetName_label);
                currentX += fieldWidth + spacing;
            }

            // Preset Text Field
            {
                fieldWidth = rect.width - currentX;
                Rect textRect = new Rect(currentX, 0f, fieldWidth, rect.height).ContractedBy(margin);

                string tempText = Widgets.TextField(textRect, presetName_textField);
                Vector2 tempText_size = Text.CalcSize(tempText);

                if (tempText_size.x <= textRect.width - 10f && validNameRegex.IsMatch(tempText))
                {
                    presetName_textField = tempText;
                }

                if (Mouse.IsOver(textRect) && (presetName_textField == "Unassigned" || presetName_textField == "ERROR, set proper name!"))
                {
                    SoundDefOf.CancelMode.PlayOneShotOnCamera();
                    presetName_textField = string.Empty;
                }
            }
        }

        public void DrawSecondHeaderField(Rect rect)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            // Used to mark current rect as 0 point for margins
            GUI.BeginGroup(rect);
            {
                // Work Field ********************************
                float workColumnSpacing = rect.width / ((float)VisibleWorkTypeDefsInPriorityOrder.Count + 1);

                float currentX = workColumnSpacing;
                float currentY = 5f;

                int workIndex = 0;
                foreach (WorkTypeDef work in VisibleWorkTypeDefsInPriorityOrder)
                {
                    Vector2 workName_size = Text.CalcSize(work.labelShort);

                    Rect workName_rect = new Rect(currentX - workName_size.x / 2, currentY, workName_size.x, workName_size.y);
                    // NOTE: move up?
                    if (workIndex % 2 == 1)
                    {
                        workName_rect.y += 20f;
                    }
                    if (Mouse.IsOver(workName_rect))
                    {
                        Widgets.DrawHighlight(workName_rect);
                    }

                    Widgets.Label(workName_rect, work.labelShort);
                    WorkTypeDef localDef = work;
                    TooltipHandler.TipRegion(workName_rect, new TipSignal(() => localDef.gerundLabel + "\n\n" + localDef.description, localDef.GetHashCode()));
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    Widgets.DrawLineVertical(workColumnSpacing * (workIndex + 1), workName_rect.yMax - 3f, 70f - workName_rect.yMax + 3f);
                    Widgets.DrawLineVertical(workColumnSpacing * (workIndex + 1) + 1f, workName_rect.yMax - 3f, 70f - workName_rect.yMax + 3f);
                    GUI.color = Color.white;

                    float workBox_width = 25f;
                    // Work Boxes
                    {
                        Vector2 workBox_topLeftCorner = new Vector2(currentX - workBox_width / 2 + 0.5f, (workName_rect.yMax - 3f) + (55f - workName_rect.yMax + 3f));

                        if (currentPreset != null)
                            DrawWorkBox(workBox_topLeftCorner, localDef, currentPreset);
                        else
                            DrawWorkBox(workBox_topLeftCorner, localDef);
                    }
                    currentX += workColumnSpacing;
                    workIndex++;
                }
            }
            GUI.EndGroup();
        }

        public void DrawPawnsTable(Rect rect)
        {
            float pawnField_Height = 30f;
            float currentX = 0f;
            float currentY = 120f;

            Rect viewRect;

            if (filterFlag == true)
            {
                int pawnsCount = PresetDatabase.assignedPresets.Count(pair => pair.Value.Name.StartsWith(presetName_textField));
                pawnsRowsCount = (pawnsCount % 2 == 1) ? (pawnsCount / 2 + 1) : (pawnsCount / 2);

                viewRect = new Rect(0f, 0f, rect.width, pawnsRowsCount * pawnField_Height);

                if (viewRect.height > rect.height)
                {
                    viewRect.width -= 16f;
                    Widgets.BeginScrollView(rect, ref this.scrollPosition, viewRect);
                    currentY = 0f;
                }

                int index = 0;
                Pawn currentPawn;

                foreach (KeyValuePair<PawnData, Preset> pair in PresetDatabase.assignedPresets)
                {
                    if (pair.Value.Name.StartsWith(presetName_textField))
                        currentPawn = pawns.Find(pawn => pawn.ThingID == pair.Key.ID);
                    else
                        continue;

                    Rect currentPawn_rect = new Rect(currentX, currentY, viewRect.width / 2, pawnField_Height);

                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    Widgets.DrawBox(currentPawn_rect, 1);
                    GUI.color = Color.white;

                    GUI.BeginGroup(currentPawn_rect);
                    {
                        Rect pawnLabel_rect = new Rect(0f, 0f, 175f, currentPawn_rect.height);
                        DrawPawnLabel(pawnLabel_rect, currentPawn);
                        Rect presetButtons_rect = new Rect(pawnLabel_rect.width, 0f, currentPawn_rect.width - pawnLabel_rect.width, currentPawn_rect.height);
                        DrawPawnRow(presetButtons_rect, currentPawn);
                    }
                    GUI.EndGroup();

                    if (index++ % 2 == 1)
                    {
                        currentY += 30f;
                        currentX = 0f;
                    }
                    else
                        currentX = viewRect.width / 2;
                }
            }
            else
            {

                viewRect = new Rect(0f, 0f, rect.width, pawnsRowsCount * pawnField_Height);

                if (viewRect.height > rect.height)
                {
                    viewRect.width -= 16f;
                    Widgets.BeginScrollView(rect, ref this.scrollPosition, viewRect);
                    currentY = 0f;
                }

                for (int i = 0; i < base.PawnsCount; i++)
                {
                    Pawn currentPawn = this.pawns[i];
                    Rect currentPawn_rect = new Rect(currentX, currentY, viewRect.width / 2, pawnField_Height);

                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    Widgets.DrawBox(currentPawn_rect, 1);
                    GUI.color = Color.white;

                    GUI.BeginGroup(currentPawn_rect);
                    {
                        Rect pawnLabel_rect = new Rect(0f, 0f, 175f, currentPawn_rect.height);
                        DrawPawnLabel(pawnLabel_rect, currentPawn);
                        Rect presetButtons_rect = new Rect(pawnLabel_rect.width, 0f, currentPawn_rect.width - pawnLabel_rect.width, currentPawn_rect.height);
                        DrawPawnRow(presetButtons_rect, currentPawn);
                    }
                    GUI.EndGroup();

                    if (i % 2 == 1)
                    {
                        currentY += 30f;
                        currentX = 0f;
                    }
                    else
                        currentX = viewRect.width / 2;
                }
            }
            if (viewRect.height > rect.height)
                Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // Pawn's Names Table Cell
        public void DrawPawnLabel(Rect rect, Pawn p)
        {
            GUI.BeginGroup(rect);
            {
                if (Mouse.IsOver(rect))
                {
                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                }
                if (p.health.summaryHealth.SummaryHealthPercent < 0.99f)
                {
                    Rect rect4 = new Rect(rect);
                    rect4.xMin -= 4f;
                    rect4.yMin += 4f;
                    rect4.yMax -= 6f;
                    Widgets.FillableBar(rect4, p.health.summaryHealth.SummaryHealthPercent, PawnUIOverlay.HealthTex, BaseContent.ClearTex, false);
                }
                if (Mouse.IsOver(rect))
                {
                    // add margin inside rect
                    GUI.DrawTexture(rect.ContractedBy(3f), TexUI.HighlightTex);
                }
                string label;
                if (!p.RaceProps.Humanlike && p.Name != null && !p.Name.Numerical)
                {
                    label = p.Name.ToStringShort.CapitalizeFirst() + ", " + p.KindLabel;
                }
                else
                {
                    label = p.LabelCap;
                }
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Rect rect5 = new Rect(rect);
                rect5.xMin += 15f;
                Widgets.Label(rect5, label);
                Text.WordWrap = true;
                if (Widgets.InvisibleButton(rect))
                {
                    Find.MainTabsRoot.EscapeCurrentTab(true);
                    Find.CameraMap.JumpTo(p.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (p.SpawnedInWorld)
                    {
                        Find.Selector.Select(p, true, true);
                    }
                    return;
                }
                TipSignal tooltip = p.GetTooltip();
                tooltip.text = "ClickToJumpTo".Translate() + "\n\n" + tooltip.text;
                TooltipHandler.TipRegion(rect, tooltip);

                if (p.Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(rect.x, rect.center.y, rect.width);
                    GUI.color = Color.white;
                }
            }
            GUI.EndGroup();
        }

        // Pawn's Buttons Table Cell
        protected override void DrawPawnRow(Rect rect, Pawn p)
        {
            GUI.BeginGroup(rect);
            {
                string currentButton_presetName;
                float margin = 2f;

                Rect presetRect = new Rect(0f, 0f, 240f, rect.height).ContractedBy(margin);
                Rect editRect = new Rect(presetRect.width, 0f, rect.width - presetRect.width, rect.height).ContractedBy(margin);

                PawnData pawnData;
                
                if (PresetDatabase.assignedPresets.Keys.Any(pawn => pawn.ID == p.ThingID))
                    currentButton_presetName = PresetDatabase.assignedPresets.First(pair => pair.Key.ID == p.ThingID).Value.Name;
                else
                    currentButton_presetName = "Unassigned";

                // Preset Selector Button
                if (Widgets.TextButton(presetRect, currentButton_presetName, true, false))
                {
                    if (PresetDatabase.presetsList.Count > 0)
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach (Preset current in PresetDatabase.presetsList)
                        {
                            // assign selected preset to pawn and make note in assigned presets list
                            list.Add(new FloatMenuOption(current.name, delegate
                            {
                                foreach (WorkTypeDef job in VisibleWorkTypeDefsInPriorityOrder)
                                {
                                    if (!p.story.WorkTypeIsDisabled(job))
                                        p.workSettings.SetPriority(job, current.priorities[job]);
                                }
                                if (PresetDatabase.assignedPresets.Keys.Any(pawn => pawn.ID == p.ThingID))
                                {
                                    pawnData = PresetDatabase.assignedPresets.Keys.First(pawn => pawn.ID == p.ThingID);
                                    PresetDatabase.assignedPresets[pawnData] = current;
                                }
                                else
                                    PresetDatabase.assignedPresets.Add(new PawnData(p), current);
                            }, MenuOptionPriority.Medium, null, null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list, false));
                    }
                    else
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }

                // Edit Current Pawn's Preset Button
                if (Widgets.TextButton(editRect, "Edit", true, false))
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    if (PresetDatabase.assignedPresets.Keys.Any(pawn => pawn.ID == p.ThingID))
                    {
                        pawnData = PresetDatabase.assignedPresets.Keys.First(pawn => pawn.ID == p.ThingID);
                        currentPreset = new Preset(PresetDatabase.assignedPresets[pawnData]);
                        presetName_textField = PresetDatabase.assignedPresets[pawnData].Name;
                    }
                    else
                    {
                        currentPreset = new Preset();
                        presetName_textField = "Unassigned";
                        foreach (WorkTypeDef job in VisibleWorkTypeDefsInPriorityOrder)
                        {
                            currentPreset.priorities[job] = p.workSettings.GetPriority(job);
                        }
                        // Used to unset focus of Text Field
                        GUI.FocusControl(null);
                    }
                }
            }
            GUI.EndGroup();
        }
        
        public void SavePreset()
        {
            if (currentPreset != null)
            {
                if (presetName_textField == string.Empty || presetName_textField == "Unassigned" || presetName_textField == "ERROR, set proper name!")
                {
                    SoundDefOf.MessageSeriousAlert.PlayOneShotOnCamera();
                    presetName_textField = "ERROR, set proper name!";
                    GUI.FocusControl(null);
                }
                else
                {
                    currentPreset.Name = presetName_textField;
                    if (PresetDatabase.presetsList.Exists(preset => preset.Name == currentPreset.Name))
                    {
                        Preset match = PresetDatabase.presetsList.Find(preset => preset.Name == currentPreset.Name);
                        match.priorities = new Dictionary<WorkTypeDef, int>(currentPreset.priorities);
                        AssignChanges(match);
                    }
                    else
                    {
                        PresetDatabase.presetsList.Add(new Preset(currentPreset));
                    }
                }
            }
        }

        public void AssignChanges(Preset preset)
        {
            foreach (KeyValuePair<PawnData, Preset> pair in PresetDatabase.assignedPresets)
            {
                if (!pawns.Exists(pawn => pawn.ThingID == pair.Key.ID))
                {
                    PresetDatabase.assignedPresets.Remove(pair.Key);
                }
                else if (pair.Value.Name == preset.Name)
                {
                    foreach (WorkTypeDef job in VisibleWorkTypeDefsInPriorityOrder)
                    {
                        pawns.Find(pawn => pawn.ThingID == pair.Key.ID);
                        if (!pawns.Find(pawn => pawn.ThingID == pair.Key.ID).story.WorkTypeIsDisabled(job))
                            pawns.Find(pawn => pawn.ThingID == pair.Key.ID).workSettings.SetPriority(job, preset.priorities[job]);
                    }
                }
            }
        }
        // No Preset chosen case
        public void DrawWorkBox(Vector2 topLeft, WorkTypeDef wType)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, 25f, 25f);
            DrawWorkBoxBackground(rect, wType);
        }

        public void DrawWorkBox(Vector2 topLeft, WorkTypeDef wType, Preset preset)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, 25f, 25f);
            DrawWorkBoxBackground(rect, wType);
            if (Find.PlaySettings.useWorkPriorities)
            {
                int priority = preset.priorities[wType];
                string label;
                if (priority > 0)
                {
                    label = priority.ToString();
                }
                else
                {
                    label = string.Empty;
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ColorOfPriority(priority);
                Rect rect2 = rect.ContractedBy(-3f);
                Text.Font = GameFont.Medium;
                Widgets.Label(rect2, label);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
                {
                    if (Event.current.button == 0)
                    {
                        int num = preset.priorities[wType] - 1;
                        if (num < 0)
                        {
                            num = 4;
                        }
                        // Preset change
                        preset.priorities[wType] = num;
                        SoundDefOf.AmountIncrement.PlayOneShotOnCamera();
                    }
                    if (Event.current.button == 1)
                    {
                        int num2 = preset.priorities[wType] + 1;
                        if (num2 > 4)
                        {
                            num2 = 0;
                        }
                        // Preset change
                        preset.priorities[wType] = num2;
                        SoundDefOf.AmountDecrement.PlayOneShotOnCamera();
                    }
                    //Event.current.Use();
                }
            }
            else
            {
                int priority2 = preset.priorities[wType];
                if (priority2 > 0)
                {
                    GUI.DrawTexture(rect, ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxCheck", true));
                }
                if (Widgets.InvisibleButton(rect))
                {
                    if (preset.priorities[wType] > 0)
                    {
                        preset.priorities[wType] = 0;
                        SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera();
                    }
                    else
                    {
                        preset.priorities[wType] = 3;
                        SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                    }
                }
            }
        }

        public void DrawWorkBoxBackground(Rect rect, WorkTypeDef workDef)
        {
            Texture2D image;
            image = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxBG_Bad", true);
            GUI.DrawTexture(rect, image);
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
        }

        public Color ColorOfPriority(int prio)
        {
            switch (prio)
            {
                case 1:
                    return Color.green;
                case 2:
                    return Color.yellow;
                case 3:
                    return new Color(1, 0.5f, 0f);
                case 4:
                    return Color.red;
                default:
                    return Color.grey;
            }
        }
    }
}