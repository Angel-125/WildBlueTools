using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Greenhouse")]
    public class WBIModuleGreenhouse : WBIResourceConverter, IOpsView
    {
        protected const string kCropFailed = "Your crops have failed! You gain no resources.";
        protected const string kCropYieldHigh = "Great crop yield! You gained {0:f2} ";
        protected const string kCropYield = "Harvest time! You gained {0:f2} ";
        protected const string kCropYieldLow = "Crop yield is lower than expected. You gained {0:f2} ";
        protected const string kInsufficientResources = "Crop failure! Not enough resources to grow crops.";
        protected const string kGrowingCrops = "Growing";
        protected const float kMessageDuration = 5.0f;

        [KSPField]
        public int harvestType = (int)HarvestTypes.Planetary;

        [KSPField]
        public string cropResource = "Food";

        [KSPField]
        public float cropYield = 155.5f;

        [KSPField]
        public float failureLoss = 0.5f;

        protected string biomeName;
        protected int planetID = -1;
        protected HarvestTypes harvestID;
        protected InfoView infoView = new InfoView();
        protected WBIModuleSwitcher moduleSwitcher = null;
        protected float originalCriticalSuccess;

        [KSPEvent(guiActive = true, guiName = "Greenhouse Info")]
        public void GetModuleInfo()
        {
            infoView.SetVisible(true);
        }

        public void OnGUI()
        {
            try
            {
                if (infoView.IsVisible())
                    infoView.DrawWindow();
            }
            catch { }
        }

        public override string GetInfo()
        {
            GetTotalCrewSkill();
            string moduleInfo = base.GetInfo();
            StringBuilder cropInfo = new StringBuilder();
            double secondsPerCycle = GetSecondsPerCycle();
            double daysPerCycle = secondsPerCycle / 21600.0f;

            if (outputList.Count == 0)
                moduleInfo = moduleInfo.Replace("Outputs:", "Outputs: Special");

            cropInfo.Append(moduleInfo + "\r\n");
            cropInfo.Append("Skill Needed: " + ExperienceEffect + "\r\n");
            cropInfo.Append("Crop Yield\r\n");
            cropInfo.Append("Growing Time: ");
            cropInfo.Append(string.Format("{0:f1} days\r\n", daysPerCycle));
            cropInfo.Append("Nominal Yield (");
            cropInfo.Append(cropResource);
            cropInfo.Append("): ");
            cropInfo.Append(string.Format("{0:f1}\r\n", cropYield));

            return cropInfo.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            CBAttributeMapSO.MapAttribute biome = Utils.GetCurrentBiome(this.part.vessel);
            biomeName = biome.name;

            if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.SPLASHED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                harvestID = (HarvestTypes)harvestType;
                planetID = this.part.vessel.mainBody.flightGlobalsIndex;
            }

            moduleSwitcher = this.part.FindModuleImplementing<WBIModuleSwitcher>();

            GameEvents.onCrewOnEva.Add(this.onCrewEVA);
            GameEvents.onCrewTransferred.Add(this.onCrewTransfer);
            GameEvents.onCrewBoardVessel.Add(this.onCrewBoardVessel);

            originalCriticalSuccess = criticalSuccess;
            setupModuleInfo();
        }

        public void OnDestroy()
        {
            GameEvents.onCrewBoardVessel.Remove(this.onCrewBoardVessel);
            GameEvents.onCrewTransferred.Remove(this.onCrewTransfer);
            GameEvents.onCrewOnEva.Remove(this.onCrewEVA);
        }

        protected void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> evnt)
        {
            totalCrewSkill = GetTotalCrewSkill();
            criticalSuccess = originalCriticalSuccess - (100 * SpecialistBonusBase * totalCrewSkill);
        }

        protected void onCrewTransfer(GameEvents.HostedFromToAction<ProtoCrewMember, Part> evnt)
        {
            totalCrewSkill = GetTotalCrewSkill();
            criticalSuccess = originalCriticalSuccess - (100 * SpecialistBonusBase * totalCrewSkill);
        }

        protected void onCrewEVA(GameEvents.FromToAction<Part, Part> evnt)
        {
            totalCrewSkill = GetTotalCrewSkill();
            criticalSuccess = originalCriticalSuccess - (100 * SpecialistBonusBase * totalCrewSkill);
        }

        public override double GetSecondsPerCycle()
        {
            return hoursPerCycle * 3600;
        }

        protected override void onSuccess()
        {
            string message = string.Format(kCropYield, cropYield) + cropResource;

            //normal yield
            harvestCrops(cropYield);

            ScreenMessages.PostScreenMessage(message, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected override void onCriticalSuccess()
        {
            float harvestAmount = (cropYield * (1.0f + (totalCrewSkill * SpecialistBonusBase)) * EfficiencyBonus);
            string message = string.Format(kCropYieldHigh, harvestAmount) + cropResource;

            //increased yield
            harvestCrops(harvestAmount);

            ScreenMessages.PostScreenMessage(message, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected override void onFailure()
        {
            float harvestAmount = (cropYield * failureLoss) * (1.0f + (totalCrewSkill * SpecialistEfficiencyFactor) * EfficiencyBonus);
            string message = string.Format(kCropYieldLow, harvestAmount) + cropResource;

            //decreased yield
            harvestCrops(harvestAmount);

            ScreenMessages.PostScreenMessage(message, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected override void onCriticalFailure()
        {
            //Lost the whole crop
            ScreenMessages.PostScreenMessage(kCropFailed, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected virtual void harvestCrops(float harvestAmount)
        {
            PartResourceDefinition definition;

            definition = ResourceHelper.DefinitionForResource(cropResource);
            if (definition == null)
            {
                Log("No definition for " + cropResource);
                return;
            }

            this.part.RequestResource(definition.id, -harvestAmount, ResourceFlowMode.ALL_VESSEL);
        }

        protected void setupModuleInfo()
        {
            string description = "";
            Texture moduleLogo = null;
            string panelName;

            totalCrewSkill = GetTotalCrewSkill();

            if (moduleSwitcher != null)
            {
                description = moduleSwitcher.CurrentTemplate.GetValue("description");

                panelName = moduleSwitcher.CurrentTemplate.GetValue("logoPanel");
                if (panelName != null)
                {
                    moduleLogo = GameDatabase.Instance.GetTexture(panelName, false);
                    if (moduleLogo != null)
                        infoView.moduleLabel = moduleLogo;
                }
            }

            infoView.ModuleInfo = description + "\r\n\r\n" + GetInfo();
        }

        #region IOpsView
        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }

        public virtual void SetContextGUIVisible(bool isVisible)
        {
            SetGuiVisible(isVisible);
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public virtual List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add(ConverterName);
            return buttonLabels;
        }

        public virtual void DrawOpsWindow(string buttonLabel)
        {
            string timeRemaining = Utils.formatTime(secondsPerCycle - elapsedTime);
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(new Vector2(0, 0), new GUIStyle(GUI.skin.textArea), GUILayout.Height(140));
            GUILayout.Label("<color=white><b>Status: </b>" + status + "</color>");
            GUILayout.Label("<color=white><b>Growing Time Remaining: </b>" + timeRemaining + "</color>");
            GUILayout.Label("<color=white><b>Last Attempt: </b>" + lastAttempt + "</color>");
            GUILayout.EndScrollView();

            if (ModuleIsActive())
            {
                if (GUILayout.Button(StopActionName))
                    StopConverter();
            }

            else if (GUILayout.Button(StartActionName))
            {
                StartConverter();
            }

            GUILayout.EndVertical();
        }        
        #endregion

    }
}
