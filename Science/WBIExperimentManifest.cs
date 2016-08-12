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
    [KSPModule("Experiment Manifest")]
    public class WBIExperimentManifest : ExtendedPartModule, IPartMassModifier, IPartCostModifier, IOpsView
    {
        [KSPField]
        public string defaultExperiment = "WBIEmptyExperiment";

        [KSPField(isPersistant = true)]
        public bool isGUIVisible;

        public WBIModuleScienceExperiment[] experimentSlots = null;

        private ExpManifestAdminView manifestAdmin = new ExpManifestAdminView();

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Show Manifest")]
        public void ShowManifestGUI()
        {
            GetExperimentSlots();
            manifestAdmin.experimentSlots = this.experimentSlots;
            manifestAdmin.SetVisible(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GetExperimentSlots();
            manifestAdmin.SetupView(this.part, false, false);
        }

        public bool HasAvailableSlots()
        {
            WBIModuleScienceExperiment experimentSlot;

            //Find an available slot
            for (int index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];

                if (experimentSlot.experimentID == experimentSlot.defaultExperiment)
                {
                    return true;
                }
            }

            return false;
        }

        public void TransferExperiment(WBIModuleScienceExperiment experiment)
        {
            WBIModuleScienceExperiment experimentSlot;

            //Find an available slot
            for (int index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];

                if (experimentSlot.experimentID == experimentSlot.defaultExperiment)
                {
                    experimentSlot.TransferExperiment(experiment);
                    ScreenMessages.PostScreenMessage(experimentSlot.title + " transfered to " + this.part.partInfo.title, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }
        }

        public WBIModuleScienceExperiment[] GetExperimentSlots()
        {
            if (experimentSlots != null)
            {
                if (experimentSlots.Length > 0)
                    return experimentSlots;
            }
            WBIModuleScienceExperiment experiment;
            List<WBIModuleScienceExperiment> experiments = this.part.FindModulesImplementing<WBIModuleScienceExperiment>();
            int totalCount;
            int index;

            totalCount = experiments.Count;
            Log("experimentSlots found: " + totalCount.ToString());

            for (index = 0; index < totalCount; index++)
            {
                experiment = experiments[index];

                experiment.defaultExperiment = this.defaultExperiment;
                experiment.onExperimentReceived += this.OnExperimentReceived;
                experiment.onExperimentTransfered += this.OnExperimentTransfered;

                //If the experiment slot is a default one then load its definition.
                if (experiment.experimentID == experiment.defaultExperiment)
                    experiment.LoadFromDefinition(defaultExperiment);
            }

            experimentSlots = experiments.ToArray<WBIModuleScienceExperiment>();
            return experimentSlots;
        }

        public void OnGUI()
        {
            if (manifestAdmin.IsVisible())
                manifestAdmin.DrawWindow();
            if (manifestAdmin.loadExperimentView.IsVisible())
                manifestAdmin.loadExperimentView.DrawWindow();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                manifestAdmin.EscapeKeyPressed();
            }
        }

        protected void OnExperimentReceived(WBIModuleScienceExperiment transferRecipient)
        {
        }

        protected void OnExperimentTransfered(WBIModuleScienceExperiment transferedExperiment)
        {
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
            buttonLabels.Add("Experiments");
            return buttonLabels;
        }

        public void SetContextGUIVisible(bool isVisible)
        {
            Events["ShowManifestGUI"].guiActive = isVisible;
            Events["ShowManifestGUI"].guiActiveEditor = isVisible;
        }

        #endregion
        #region IPartCostModifier

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            float moduleCost = defaultCost;
            WBIModuleScienceExperiment experimentSlot;

            if (experimentSlots == null)
                return defaultCost;

            for (int index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];
                moduleCost += experimentSlot.cost;
            }

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
            float moduleMass = 0;
            WBIModuleScienceExperiment experimentSlot;

            if (experimentSlots == null)
                return 0;

            for (int index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];
                moduleMass += experimentSlot.partMass;
            }

            return moduleMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
        #endregion
    
    }
}
