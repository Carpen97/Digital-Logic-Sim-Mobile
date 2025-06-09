using Seb.Vis.UI;
using Seb.Vis;
using UnityEngine;
using static DLS.Graphics.DrawSettings;
using System;
using System.Collections.Generic;
using DLS.Game;
using System.Linq;
using DLS.Description;
using static UnityEngine.LightTransport.IProbeIntegrator;
using UnityEditor.SearchService;



namespace DLS.Graphics
{
    public static class SpecialChipMakerMenu
    {
        public static List<int> PinBitCountsMade = new();
        public static List<KeyValuePair<int, int>> MergeSplitsMade = new();


        const float textSpacing = 0.25f;
        const float entrySpacing = 0.5f;
        const float menuWidth = 55;
        const float verticalOffset = 22;

        static readonly Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
        public static readonly Vector2 settingFieldSize = new(entrySize.x / 3, entrySize.y);


        static readonly string[] SpecialChipTypes =
        {
            "Pins",
            "Merge/Split"
        };
        const int OPTION_PIN = 0;
        const int OPTION_MERGE_SPLIT = 1;


        static readonly UIHandle ID_SpecialChipTypes = new("SPEC_SpecialChipTypes");
        static readonly UIHandle ID_PinSize = new("SPEC_PinSize");

        static readonly UIHandle ID_FirstMergeSplit = new("SPEC_MergeSplitA");
        static readonly UIHandle ID_SecondMergeSplit = new("SPEC_MergeSplitB");



        static readonly Func<string, bool> pinSizeInputValidator = ValidatePinSizeInput;

        static bool canAddChip;
        static int currentlyAddingPinBitOfSize;

        static KeyValuePair<int, int> currentlyAddingMergeSplit;

        public static void DrawMenu()
        {
            UI.DrawFullscreenPanel(ActiveUITheme.MenuBackgroundOverlayCol);

            UIThemeDLS theme = ActiveUITheme;
            InputFieldTheme inputTheme = ActiveUITheme.ChipNameInputField;
            Draw.ID panelID = UI.ReservePanel();


            const int inputTextPad = 1;
            const float headerSpacing = 1.5f;
            Vector2 topLeft = UI.Centre + new Vector2(-menuWidth / 2, verticalOffset);
            Vector2 labelPosCurr = topLeft;
            Color labelCol = Color.white;
            Color headerCol = new(0.46f, 1, 0.54f);
            Color errorCol = new(1, 0.4f, 0.45f);



            using (UI.BeginBoundsScope(true))
            {
                DrawHeader("SPECIAL CHIPS:");
                int mainPinNamesMode = DrawNextWheel("Special chip type:", SpecialChipTypes, ID_SpecialChipTypes);

                if (mainPinNamesMode == OPTION_PIN)
                {
                    DrawSpecialPinMenu();
                }
                else if (mainPinNamesMode == OPTION_MERGE_SPLIT)
                {
                    DrawSpecialMergeSplitMenu();
                }

                Vector2 buttonTopLeft = new(labelPosCurr.x, UI.PrevBounds.Bottom - 2.5f);
                int addOrClose = UI.VerticalButtonGroup(new[] { "Add special chip", "Save and close" }, new[] {canAddChip, true },
                ActiveUITheme.ButtonTheme, buttonTopLeft + (menuWidth / 2) * Vector2.right, entrySize, false, false, entrySpacing);

                if(mainPinNamesMode == OPTION_PIN && canAddChip && addOrClose == 0)
                {
                    AddNewBitSize(currentlyAddingPinBitOfSize);
                }
                if(mainPinNamesMode == OPTION_MERGE_SPLIT && canAddChip && addOrClose == 0)
                {
                    AddNewMergeSplit(currentlyAddingMergeSplit.Key, currentlyAddingMergeSplit.Value);
                }


                if (addOrClose == 1)
                {
                    // Save changes
                    Main.ActiveProject.SaveCurrentProjectDescription() ;
                    UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
                }

                MenuHelper.DrawReservedMenuPanel(panelID, UI.GetCurrentBoundsScope());
            }

            void DrawSpecialMergeSplitMenu()
            {
                DrawHeader("NEW MERGE/SPLIT CHIP:");
                InputFieldState firstPinSize = MenuHelper.LabeledInputField("First pin size:", labelCol, labelPosCurr, entrySize, ID_FirstMergeSplit, pinSizeInputValidator, settingFieldSize.x);
                AddSpacing();
                InputFieldState secondPinSize = MenuHelper.LabeledInputField("Second pin size:", labelCol, labelPosCurr, entrySize, ID_SecondMergeSplit, pinSizeInputValidator, settingFieldSize.x);
                int firstPinSizeAttempt = int.TryParse(firstPinSize.text, out int a) ? a : -1;
                int secondPinSizeAttempt = int.TryParse(secondPinSize.text, out int b) ? b : -1;
                (bool valid, string reason) confirmation = RealMergeSplitConfirmation(firstPinSizeAttempt, secondPinSizeAttempt);

                if (firstPinSizeAttempt != -1 && secondPinSizeAttempt != -1 && !confirmation.valid)
                {
                    AddSpacing();
                    DrawErrorSection(confirmation.reason);
                    canAddChip = false;
                }

                else if (firstPinSizeAttempt != -1 && secondPinSizeAttempt != -1 && confirmation.valid)
                {
                    canAddChip = true;
                    currentlyAddingMergeSplit = new (Math.Max(firstPinSizeAttempt, secondPinSizeAttempt), Math.Min(firstPinSizeAttempt, secondPinSizeAttempt));
                    return;
                }
                canAddChip = false;
            }

            void DrawSpecialPinMenu()
            {
                DrawHeader("NEW PIN:");
                InputFieldState pinSizeInput = MenuHelper.LabeledInputField("Size of new pin:", labelCol, labelPosCurr,entrySize,ID_PinSize, pinSizeInputValidator, settingFieldSize.x);
                int pinSizeAttempt = int.TryParse(pinSizeInput.text, out int a) ? a : -1;
                (bool valid, string reason) confirmation = RealSizeConfirmation(pinSizeAttempt);
                if (!confirmation.valid && pinSizeAttempt != -1)
                {
                    AddSpacing();
                    DrawErrorSection(confirmation.reason);
                    canAddChip = false;
                }
                else if (confirmation.valid && pinSizeAttempt != 1) {
                    canAddChip = true;
                    currentlyAddingPinBitOfSize = pinSizeAttempt;
                    return;
                }
                canAddChip = false;
            }

            void DrawErrorSection(string reason)
            {
                AddHeaderSpacing();
                UI.DrawText("ERROR", theme.FontBold, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, errorCol);
                AddHeaderSpacing();

                AddTextSpacing();
                UI.DrawText("   "+reason, theme.FontRegular, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, errorCol);
                AddTextSpacing();


            }

            int DrawNextWheel(string label, string[] options, UIHandle id)
            {
                int index = MenuHelper.LabeledOptionsWheel(label, labelCol, labelPosCurr, entrySize, id, options, settingFieldSize.x, true);
                AddSpacing();
                return index;
            }

            void DrawHeader(string text)
            {
                AddHeaderSpacing();
                UI.DrawText(text, theme.FontBold, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, headerCol);
                AddHeaderSpacing();
            }

            void DrawLineOfText(string text)
            {
                AddTextSpacing();
                UI.DrawText(text, theme.FontRegular, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, labelCol);
                AddTextSpacing();
            }


            void AddSpacing()
            {
                labelPosCurr.y -= entrySize.y + entrySpacing;
            }

            void AddHeaderSpacing()
            {
                labelPosCurr.y -= headerSpacing;
            }

            void AddTextSpacing()
            {
                labelPosCurr.y -= textSpacing;
            }

        }

        public static void OnMenuOpened()
        {
            RefreshPinBitCounts();
            RefreshMergeSplits();
        }

        public static void RefreshPinBitCounts()
        {
            PinBitCountsMade = new();
            foreach(var pinBit in Main.ActiveProject.description.pinBitCounts)
            {
                PinBitCountsMade.Add(pinBit.BitCount);
            }
        }

        public static void RefreshMergeSplits()
        {
            MergeSplitsMade = new();
            foreach (var PinBitPair in Main.ActiveProject.description.SplitMergePairs)
            {
                MergeSplitsMade.Add(new(PinBitPair.Key, PinBitPair.Value));
            }
        }
        
        public static bool ValidatePinSizeInput(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            if (s.Contains(" ")) return false;
            if (int.TryParse(s, out int a))
            {
                if(a < 0) return false;
                if(a > 4096) return false;
                return true;
            }
            return false;
        }

        public static (bool valid, string reason) RealSizeConfirmation(int a)
        {
            if(a < 1) { return (false, "Pin size must be at least 1 bit."); }
            if (a > 64 && a % 8 != 0 && a< 512) { return (false, "Pin size > 64 and not a multiple of 8."); }
            if(a > 512 && a % 64 != 0) { return (false, "Pin size > 512 and not a multiple of 64."); }
            if (PinBitCountsMade.Contains(a)) { return (false, "Pins with this count already exist."); }

            return (true, "");
        }

        public static (bool valid, string reason) RealMergeSplitConfirmation(int a, int b)
        {
            if (MergeSplitsMade.Any(k => (k.Key == a && k.Value == b )||(k.Value == a && k.Key == b))){ return (false, "These Merge/Split chips already exist."); }
            if (!PinBitCountsMade.Contains(a) && !PinBitCountsMade.Contains(b) ) { return (false, $"No pins with pinsize {a} and {b} exist. Create them first."); }
            if (!PinBitCountsMade.Contains(a)) { return (false, $"No pin with pinsize {a} exist. Create it first, if valid."); }
            if (!PinBitCountsMade.Contains(b)) { return (false, $"No pin with pinsize {b} exist. Create it first, if valid."); }
            int bigger = Math.Max(a, b);
            int smaller = Math.Min(a, b);

            if(bigger%smaller != 0) { return (false, $"{bigger} / {smaller} isn't an integer."); }

            return (true, "");
        }

        public static void AddNewBitSize(int a)
        {
            Main.ActiveProject.AddNewPinSize(a);
            RefreshPinBitCounts();
        }

        public static void AddNewMergeSplit(int a, int b)
        {
            Main.ActiveProject.AddNewMergeSplit(a, b);
            RefreshMergeSplits();
        }
    }
}