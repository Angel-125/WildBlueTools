using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
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
    [KSPModule("Experiment Lab")]
    public class WBIExperimentLab : WBIBasicScienceLab, IOpsView, IPartMassModifier, IPartCostModifier
    {
        [KSPField]
        public string defaultExperiment = "WBIEmptyExperiment";

        [KSPField(isPersistant = true)]
        public bool isGUIVisible = true;

        public List<WBIModuleScienceExperiment> experimentSlots = null;

        private WBIResourceSwitcher switcher = null;
        private ExpManifestAdminView manifestAdmin = new ExpManifestAdminView();

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Show Manifest")]
        public void ShowManifestGUI()
        {
            GetExperimentSlots();
            manifestAdmin.experimentSlots = this.experimentSlots;
            manifestAdmin.SetVisible(true);
        }

        public bool HasAvailableSlots()
        {
            //Find an available slot
            foreach (WBIModuleScienceExperiment experimentSlot in experimentSlots)
            {
                if (experimentSlot.experimentID == experimentSlot.defaultExperiment)
                {
                    return true;
                }
            }

            return false;
        }

        public void TransferExperiment(WBIModuleScienceExperiment experiment)
        {
            //Find an available slot
            foreach (WBIModuleScienceExperiment experimentSlot in experimentSlots)
            {
                if (experimentSlot.experimentID == experimentSlot.defaultExperiment)
                {
                    experimentSlot.TransferExperiment(experiment);
                    ScreenMessages.PostScreenMessage(experimentSlot.title + " transfered to " + this.part.partInfo.title, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GetExperimentSlots();
            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            manifestAdmin.SetupView(this.part, !HighLogic.LoadedSceneIsEditor, !HighLogic.LoadedSceneIsEditor, this);
            SetupGUI(isGUIVisible);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            GetExperimentSlots();

            foreach (WBIModuleScienceExperiment experimentSlot in experimentSlots)
                experimentSlot.CheckCompletion();

            if (Input.GetKeyDown(KeyCode.Escape))
                manifestAdmin.EscapeKeyPressed();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (manifestAdmin.loadExperimentView.IsVisible())
                manifestAdmin.loadExperimentView.DrawWindow();
        }

        public void SetupGUI(bool guiVisible)
        {
            isGUIVisible = guiVisible;
            Events["ShowManifestGUI"].guiActive = isGUIVisible;
            Events["ShowManifestGUI"].guiActiveEditor = isGUIVisible;
        }

        public List<WBIModuleScienceExperiment> GetExperimentSlots()
        {
            if (experimentSlots != null)
                return experimentSlots;

            experimentSlots = this.part.FindModulesImplementing<WBIModuleScienceExperiment>();
            Log("experimentSlots found: " + experimentSlots.Count.ToString());

            foreach (WBIModuleScienceExperiment experiment in experimentSlots)
            {
                experiment.defaultExperiment = this.defaultExperiment;
                experiment.onExperimentReceived += this.OnExperimentReceived;
                experiment.onExperimentTransfered += this.OnExperimentTransfered;

                //If the experiment slot is a default one then load its definition.
                if (experiment.experimentID == experiment.defaultExperiment)
                    experiment.LoadFromDefinition(defaultExperiment);
            }

            return experimentSlots;
        }

        protected void OnExperimentReceived(WBIModuleScienceExperiment transferRecipient)
        {
            ConfigNode nodeResource = null;

            //If the experiment has resources then go through them and either add
            //the resources it needs or increase the max amount
            if (transferRecipient.resourceMap != null)
            {
                foreach (string resourceName in transferRecipient.resourceMap.Keys)
                {
                    //Add the resource if needed
                    if (this.part.Resources.Contains(resourceName) == false)
                    {
                        nodeResource = new ConfigNode("RESOURCE");
                        nodeResource.AddValue("name", resourceName);
                        nodeResource.AddValue("amount", "0");
                        nodeResource.AddValue("maxAmount", transferRecipient.resourceMap[resourceName].ToString());
                        this.part.Resources.Add(nodeResource);
                    }

                    //Add to max amount to account for amount that the experiment needs
                    else
                    {
                        this.part.Resources[resourceName].maxAmount += transferRecipient.resourceMap[resourceName];
                    }
                }

                //Dirty the GUI
                MonoUtilities.RefreshContextWindows(this.part);
            }

            //Setup the IVA props

        }

        protected void OnExperimentTransfered(WBIModuleScienceExperiment transferedExperiment)
        {
            PartResource resource = null;
            List<PartResource> doomedResources = new List<PartResource>();

            //If the resource map isn't null, then go through all the resources
            //and if we have them, then reduce the max amount by the amount required by
            //the experiment. If the new max amount is <= 0 then remove the resource.
            if (transferedExperiment.resourceMap != null)
            {
                foreach (string resourceName in transferedExperiment.resourceMap.Keys)
                {
                    if (this.part.Resources.Contains(resourceName))
                    {
                        resource = this.part.Resources[resourceName];
                        resource.maxAmount -= transferedExperiment.resourceMap[resourceName];
                        resource.amount -= transferedExperiment.resourceMap[resourceName];
                        if (resource.maxAmount <= 0.001f)
                            doomedResources.Add(resource);
                    }
                }

                //Remove any resources we don't need.
                foreach (PartResource doomed in doomedResources)
                {
                    this.part.Resources.list.Remove(doomed);
                }

                //Dirty the GUI
                MonoUtilities.RefreshContextWindows(this.part);
            }

            //Setup the IVA props
        }

        #region IOpsView
        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            //If we're in the editor or the manifest has no experiment slots then hook em up!
            if (HighLogic.LoadedSceneIsEditor || manifestAdmin.experimentSlots == null)
            {
                GetExperimentSlots();
                manifestAdmin.experimentSlots = this.experimentSlots;
            }

            //Let the manifest admin draw the GUI.
            manifestAdmin.DrawGUIControls();
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add("Experiment Lab");
            return buttonLabels;
        }

        public void SetContextGUIVisible(bool isVisible)
        {
            SetGuiVisible(isVisible);
        }

        #endregion

        #region IPartCostModifier

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            float moduleCost = defaultCost;

            if (experimentSlots == null)
                return defaultCost;

            foreach (WBIModuleScienceExperiment experimentSlot in experimentSlots)
                moduleCost += experimentSlot.cost;

            return moduleCost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        #endregion

        #region IPartMassModifier
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            float moduleMass = defaultMass;

            if (experimentSlots == null)
                return defaultMass;

            if (switcher != null)
                moduleMass = switcher.CalculatePartMass(defaultMass, switcher.partMass);

            foreach (WBIModuleScienceExperiment experimentSlot in experimentSlots)
                moduleMass += experimentSlot.partMass;

            return moduleMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
        #endregion
    }
}
