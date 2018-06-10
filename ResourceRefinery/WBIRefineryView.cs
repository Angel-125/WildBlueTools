using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIRefineryView : Dialog<WBIRefineryView>
    {
        const int DialogWidth = 415;
        const int DialogHeight = 325;

        WBIRefinery refinery;
        private GUILayoutOption[] infoPanelOptions = new GUILayoutOption[] { GUILayout.Height(150) };
        private GUILayoutOption[] refineryPanelOptions = new GUILayoutOption[] { GUILayout.Width(430) };
        private GUILayoutOption[] upgradeStatusWidth = new GUILayoutOption[] { GUILayout.Width(125) };
        private GUILayoutOption[] upgradeStatusHeight = new GUILayoutOption[] { GUILayout.Height(45) };
        private GUILayoutOption[] productionStatusOptions = new GUILayoutOption[] { GUILayout.Width(300) };
        private GUILayoutOption[] wideLabelOptions = new GUILayoutOption[] { GUILayout.Width(120) };
        private GUILayoutOption[] unitCostOptions = new GUILayoutOption[] { GUILayout.Width(250) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(26), GUILayout.Height(26) };
        private Vector2 scrollPos;
        private Dictionary<string, GUIStyle> guiStyles = new Dictionary<string, GUIStyle>();

        public WBIRefineryView() :
        base("Refinery", DialogWidth, DialogHeight)
        {
            Resizable = false;
            setupStyles();
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            refinery = WBIRefinery.Instance;
        }

        protected void setupStyles()
        {
            //Description text
            GUIStyle style = new GUIStyle(HighLogic.Skin.textArea);
            style.active = style.hover = style.normal;
            style.padding = new RectOffset(0, 0, 0, 0);
            guiStyles.Add("blockBackground", style);

            //Upgrades Status panel background
            style = new GUIStyle(HighLogic.Skin.textArea);
            style.active = style.hover = style.normal;
            style.padding = new RectOffset(5, 5, 5, 5);
            guiStyles.Add("statusBackground", style);

            //Standard text for labels and such
            style = new GUIStyle(HighLogic.Skin.label);
            style.fontSize = 11;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = new Color(192f / 255f, 196f / 255f, 176f / 255f);
            guiStyles.Add("blockText", style);

            //Standard text for labels and such
            style = new GUIStyle(HighLogic.Skin.label);
            style.fontSize = 12;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = new Color(192f / 255f, 196f / 255f, 176f / 255f);
            guiStyles.Add("stdText", style);

            //Upgrade status
            style = new GUIStyle(HighLogic.Skin.label);
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            guiStyles.Add("upgradeStatusText", style);

            //Production stats
            style = new GUIStyle(HighLogic.Skin.label);
            style.fontSize = 12;
            style.normal.textColor = new Color(107f / 255f, 201f / 255f, 238f / 255f);
            guiStyles.Add("productionStat", style);

            //Standard text for labels and such
            style = new GUIStyle(HighLogic.Skin.label);
            style.fontSize = 12;
            style.normal.textColor = new Color(192f / 255f, 196f / 255f, 176f / 255f);
            guiStyles.Add("upgradeText", style);

            //Toggle button
            style = new GUIStyle(HighLogic.Skin.toggle);
            style.normal.textColor = style.normal.textColor;
            style.fontSize = 12;
            guiStyles.Add("toggleButton", style);

            //Button
            style = new GUIStyle(HighLogic.Skin.button);
            style.normal.textColor = style.normal.textColor;
            guiStyles.Add("button", style);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            //Description
            GUILayout.BeginHorizontal(guiStyles["blockBackground"]);
            GUILayout.Label(WBIRefinery.kRefineryDescription, guiStyles["blockText"]);
            GUILayout.EndHorizontal();

            //Refinery panel
            scrollPos = GUILayout.BeginScrollView(scrollPos, refineryPanelOptions);

            WBIRefineryResource refineryResource;
            PartResourceDefinition resourceDef;
            for (int index = 0; index < refinery.refineryResources.Length; index++)
            {
                refineryResource = refinery.refineryResources[index];
                resourceDef = refineryResource.resourceDef;

                //Space center: show info about the resource, start/stop buttons, and upgrade button
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    drawSpaceCenterRefinery(refineryResource, resourceDef);

                //Flight: Show current/max amounts and controls to fill/drain the vessel's tanks.
                else if (HighLogic.LoadedSceneIsFlight)
                    drawFlightRefinery(ref refineryResource, resourceDef);

                refinery.refineryResources[index] = refineryResource;
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        protected void drawFlightRefinery(ref WBIRefineryResource refineryResource, PartResourceDefinition resourceDef)
        {
            GUILayout.BeginVertical(guiStyles["blockBackground"]);

            //Resource name
            GUILayout.Label(string.Format(refineryResource.kResourceName, resourceDef.displayName), guiStyles["stdText"]);

            //Send to refinery
            refineryResource.sendToRefinery = GUILayout.Toggle(refineryResource.sendToRefinery, WBIRefinery.ksendToRefinery, guiStyles["toggleButton"]);

            //Current/Max amounts
            GUILayout.Label(string.Format(WBIRefinery.kStorage, refineryResource.amount, refineryResource.maxAmount), guiStyles["productionStat"], unitCostOptions);

            GUILayout.BeginHorizontal();

            //Fill tanks
            if (FlightGlobals.ActiveVessel.LandedInKSC)
            {
                if (GUILayout.Button(WBIRefinery.kFillTanks))
                    fillTanks(refineryResource);

                //Empty tanks
                if (GUILayout.Button(WBIRefinery.kEmptyTanks))
                    emptyTanks(refineryResource);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        protected void fillTanks(WBIRefineryResource refineryResource)
        {
            double amountAvailable = 0f;
            double maxAmount = 0;

            //Determine how much space the vessel has
            FlightGlobals.ActiveVessel.rootPart.GetConnectedResourceTotals(refineryResource.resourceDef.id, out amountAvailable, out maxAmount, true);
            if (maxAmount <= 0)
                return;

            //Calculate how much to pull from the refinery
            double amountToAdd = maxAmount - amountAvailable;
            if (amountToAdd > refineryResource.amount)
                amountToAdd = refineryResource.amount;

            //Fill the tanks
            FlightGlobals.ActiveVessel.rootPart.RequestResource(refineryResource.resourceDef.id, -amountToAdd, ResourceFlowMode.ALL_VESSEL);

            //Update the refinery
            refineryResource.amount -= amountToAdd;
        }

        protected void emptyTanks(WBIRefineryResource refineryResource)
        {
            double amountAvailable = 0f;
            double maxAmount = 0;

            //Determine how much resource the vessel has
            FlightGlobals.ActiveVessel.rootPart.GetConnectedResourceTotals(refineryResource.resourceDef.id, out amountAvailable, out maxAmount, true);
            if (maxAmount <= 0)
                return;

            //Calculate how much to add to the refinery
            double amountToAdd = amountAvailable;
            double spaceAvailable = refineryResource.maxAmount - refineryResource.amount;
            if (amountToAdd > spaceAvailable)
                amountToAdd = spaceAvailable;

            //Fill the tanks
            FlightGlobals.ActiveVessel.rootPart.RequestResource(refineryResource.resourceDef.id, amountToAdd, ResourceFlowMode.ALL_VESSEL);

            //Update the refinery
            refineryResource.amount += amountToAdd;
        }

        protected bool validNumber = true;
        protected void drawSpaceCenterRefinery(WBIRefineryResource refineryResource, PartResourceDefinition resourceDef)
        {
            GUILayout.BeginVertical(guiStyles["blockBackground"]);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            //Resource name
            GUILayout.Label(string.Format(refineryResource.kResourceName, resourceDef.displayName), guiStyles["stdText"]);

            //Production Tier
            GUILayout.Label(string.Format(refineryResource.kProductionLevel, refineryResource.currentTier + 1), guiStyles["stdText"]);

            //Send to refinery
            refineryResource.sendToRefinery = GUILayout.Toggle(refineryResource.sendToRefinery, WBIRefinery.ksendToRefinery, guiStyles["toggleButton"]);

            //Limited production run
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && refineryResource.IsUnlocked)
            {
                //Enabled?
                refineryResource.limitProduction = GUILayout.Toggle(refineryResource.limitProduction, WBIRefinery.kLimitProduction, guiStyles["toggleButton"]);

                GUILayout.BeginHorizontal();

                //Production amount
                if (GUILayout.RepeatButton("<<<", buttonOptions))
                {
                    refineryResource.unitsToProduce -= 100;
                    if (refineryResource.unitsToProduce <= 0)
                        refineryResource.unitsToProduce = 0;
                }
                if (GUILayout.RepeatButton("<<", buttonOptions))
                {
                    refineryResource.unitsToProduce -= 10;
                    if (refineryResource.unitsToProduce <= 0)
                        refineryResource.unitsToProduce = 0;
                }
                if (GUILayout.Button("<", buttonOptions))
                {
                    refineryResource.unitsToProduce -= 1;
                    if (refineryResource.unitsToProduce <= 0)
                        refineryResource.unitsToProduce = 0;
                }

                //Label
                string unitsFormat = "{0:f2}";
                if (!validNumber)
                    unitsFormat = "<color=red>{0:f2}</color>";
                string productionUnitsStr = string.Format(unitsFormat, refineryResource.unitsToProduce);
                productionUnitsStr = GUILayout.TextField(productionUnitsStr, wideLabelOptions);
                double productionUnits;
                validNumber = double.TryParse(productionUnitsStr, out productionUnits);
                if (validNumber)
                    refineryResource.unitsToProduce = productionUnits;

                //Increment buttons
                if (GUILayout.Button(">", buttonOptions))
                    refineryResource.unitsToProduce += 1;
                if (GUILayout.RepeatButton(">>", buttonOptions))
                    refineryResource.unitsToProduce += 10;
                if (GUILayout.RepeatButton(">>>", buttonOptions))
                    refineryResource.unitsToProduce += 100;

                GUILayout.EndHorizontal();

                //Cost
                double productionCost = refineryResource.unitsToProduce * refineryResource.resourceDef.unitCost;
                GUILayout.Label(string.Format(WBIRefinery.kProductionCost, productionCost), guiStyles["productionStat"], unitCostOptions);
            }

            //Enable production
            if (refineryResource.unitsPerDay > 0f && refineryResource.IsUnlocked)
                refineryResource.isRunning = GUILayout.Toggle(refineryResource.isRunning, WBIRefinery.kEnableRefinery, guiStyles["toggleButton"]);

            GUILayout.EndVertical();

            //Upgrade button: max tier
            bool showNextTierStats = false;
            WBIRefineryResource nextRefineryResource = new WBIRefineryResource(refineryResource.GetNextTier());
            
            //Need to account for the first tier unlock status as well as the next tier.
            bool isTechUnlocked = refineryResource.IsTechUnlocked();
            if (refineryResource.IsUnlocked && nextRefineryResource.productionTiers.Count > 0)
                isTechUnlocked = nextRefineryResource.IsTechUnlocked();
            
            //Need to account for the first tier afforadbility as well as the next tier.
            bool canAffordUnlock = refineryResource.CanAffordUnlock();
            if (refineryResource.IsUnlocked && nextRefineryResource.productionTiers.Count > 0)
                canAffordUnlock = nextRefineryResource.CanAffordUnlock();

            if (refineryResource.IsMaxTier())
            {
                GUILayout.BeginHorizontal(guiStyles["statusBackground"], upgradeStatusWidth);
                GUILayout.FlexibleSpace();
                GUILayout.Label(WBIRefinery.kFullyUpgraded, guiStyles["upgradeStatusText"], upgradeStatusHeight);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            //Upgrade button: tech unlocked
            else if (!isTechUnlocked)
            {
                GUILayout.BeginHorizontal(guiStyles["statusBackground"], upgradeStatusWidth);
                GUILayout.FlexibleSpace();
                GUILayout.Label(WBIRefinery.kInsufficientTech, guiStyles["upgradeStatusText"], upgradeStatusHeight);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            //Upgarde button: can afford
            else if (!canAffordUnlock)
            {
                GUILayout.BeginHorizontal(guiStyles["statusBackground"], upgradeStatusWidth);
                GUILayout.FlexibleSpace();
                GUILayout.Label(WBIRefinery.kInsufficientFunds, guiStyles["upgradeStatusText"], upgradeStatusHeight);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            //Allow purchase/upgrade
            else
            {
                string buttonLabel = string.Format(WBIRefinery.kUpgradeButton, refineryResource.unlockCost);
                if (!refineryResource.IsUnlocked)
                    buttonLabel = string.Format(WBIRefinery.kPurchaseButton, refineryResource.unlockCost);
                if (GUILayout.Button(buttonLabel, guiStyles["button"]))
                    refineryResource.UnlockNextTier();
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    showNextTierStats = true;
            }

            GUILayout.EndHorizontal();

            //Production values
            GUILayout.BeginVertical(guiStyles["blockBackground"]);
            GUILayout.BeginHorizontal();

            //If we moused over the upgrade button, get the next tier and fake it's running so we can see its stats.
            if (showNextTierStats)
            {
                //Set current amount so players don't freak out.
                nextRefineryResource.amount = refineryResource.amount;

                //Fake the running state so we can get cost and such.
                nextRefineryResource.isRunning = true;
            }

            //Production rate
            if (!showNextTierStats && refineryResource.unitsPerDay > 0f)
                GUILayout.Label(formatRate(refineryResource.UnitsPerSec), guiStyles["productionStat"], wideLabelOptions);
            else if (showNextTierStats && nextRefineryResource.unitsPerDay > 0f)
                GUILayout.Label(formatRate(nextRefineryResource.UnitsPerSec), guiStyles["upgradeText"], wideLabelOptions);

            //Current/Max units
            GUILayout.FlexibleSpace();
            if (!showNextTierStats)
                GUILayout.Label(string.Format(WBIRefinery.kStorage, refineryResource.amount, refineryResource.maxAmount), guiStyles["productionStat"], wideLabelOptions);
            else
                GUILayout.Label(string.Format(WBIRefinery.kStorage, nextRefineryResource.amount, nextRefineryResource.maxAmount), guiStyles["upgradeText"], wideLabelOptions);
            GUILayout.EndHorizontal();

            //Production cost
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (!showNextTierStats)
                    GUILayout.Label(WBIRefinery.kCost + formatRate(refineryResource.CostPerSec, "£"), guiStyles["productionStat"], wideLabelOptions);
                else
                    GUILayout.Label(WBIRefinery.kCost + formatRate(nextRefineryResource.CostPerSec, "£"), guiStyles["upgradeText"], wideLabelOptions);
            }
            
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        string formatRate(double amount, string units = "u")
        {
            if (amount < 0.0001)
                return string.Format("{0:f2}{1}/day", amount * (double)KSPUtil.dateTimeFormatter.Day, units);
            else if (amount < 0.01)
                return string.Format("{0:f2}{1}/hr", amount * (double)KSPUtil.dateTimeFormatter.Hour, units);
            else
                return string.Format("{0:f2}{1}/sec", amount, units);
        }
    }
}
