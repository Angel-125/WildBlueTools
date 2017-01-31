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
        private static double DISTRIBUTION_TIMER = 1.0f;

        [KSPField]
        public bool debugMode;

        [KSPField]
        public bool canCreateExperiments;

        [KSPField]
        public string experimentCreationSkill = string.Empty;

        [KSPField]
        public int minimumCreationLevel;

        [KSPField]
        public string defaultExperiment = "WBIEmptyExperiment";

        [KSPField]
        public string opsButtonName = "Experiment Lab";

        [KSPField]
        public string creationTags = string.Empty;

        [KSPField]
        public string defaultCreationResource = string.Empty;

        [KSPField]
        public double minimumCreationAmount = 0f;

        [KSPField]
        public bool checkCreationResources;

        [KSPField(isPersistant = true)]
        public bool isGUIVisible = true;

        [KSPField]
        public bool isAvailable = true;

        [KSPField]
        public string unavailableMessage = "The lab is currently unavailable. Check back later.";

        WBIModuleScienceExperiment[] experimentSlots = null;

        public Dictionary<string, double> shareAmounts = new Dictionary<string, double>();
        public Dictionary<string, double> currentAmounts = new Dictionary<string, double>();

        private WBIResourceSwitcher switcher = null;
        private ExpManifestAdminView manifestAdmin = new ExpManifestAdminView();
        private double elapsedDistributionTime;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Show Manifest")]
        public void ShowManifestGUI()
        {
            //Setup the experiment slots
            GetExperimentSlots();
            manifestAdmin.experimentSlots = this.experimentSlots;

            //Show the admin view.
            manifestAdmin.SetVisible(true);
        }

        public bool HasAvailableSlots()
        {
            WBIModuleScienceExperiment experimentSlot;
            int index;

            //Find an available slot
            for (index = 0; index < experimentSlots.Length; index++)
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
            int index;

            //Find an available slot
            for (index = 0; index < experimentSlots.Length; index++)
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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GetExperimentSlots();
            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            manifestAdmin.SetupView(this.part, !HighLogic.LoadedSceneIsEditor, !HighLogic.LoadedSceneIsEditor, this);
            manifestAdmin.canCreateExperiments = this.canCreateExperiments;
            manifestAdmin.minimumCreationLevel = this.minimumCreationLevel;
            manifestAdmin.experimentCreationSkill = this.experimentCreationSkill;
            manifestAdmin.creationTags = this.creationTags;
            SetupGUI(isGUIVisible);
            setupExperimentResources();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Input.GetKeyDown(KeyCode.Escape))
                manifestAdmin.EscapeKeyPressed();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (manifestAdmin.loadExperimentView.IsVisible())
                manifestAdmin.loadExperimentView.DrawWindow();
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            //Below is a lengthy operation, make sure that sufficient time has passed.
            elapsedDistributionTime += deltaTime;
            if (elapsedDistributionTime < DISTRIBUTION_TIMER)
                return;

            List<SExperimentResource> requiredResources = null;
            Dictionary<string, List<WBIModuleScienceExperiment>> requiredResourceMap = new Dictionary<string, List<WBIModuleScienceExperiment>>();
            int totalSlots = experimentSlots.Length;
            int totalResources;
            WBIModuleScienceExperiment experimentSlot;
            SExperimentResource experimentResource;
            PartResource resource;
            double shareAmount;
            double remainder;
            List<WBIModuleScienceExperiment> experiments;

            //Reset distribution timer
            elapsedDistributionTime = 0f;

            //Make sure we have experiment slots
            GetExperimentSlots();

            //Check for completion and compile the list of resources that the experiments need.
            for (int index = 0; index < totalSlots; index++)
            {
                //Get the experiment slot
                experimentSlot = experimentSlots[index];

                //First, check for completion
                experimentSlot.CheckCompletion();

                //Now get all the resources that the experiment needs
                requiredResources = experimentSlot.GetRequiredResources();

                //Update the map
                totalResources = requiredResources.Count;
                if (totalResources > 0)
                {
                    for (int curResourceIndex = 0; curResourceIndex < totalResources; curResourceIndex++)
                    {
                        experimentResource = requiredResources[curResourceIndex];

                        //Create list if needed
                        if (requiredResourceMap.ContainsKey(experimentResource.name) == false)
                            requiredResourceMap.Add(experimentResource.name, new List<WBIModuleScienceExperiment>());

                        //Now update the list
                        requiredResourceMap[experimentResource.name].Add(experimentSlot);
                    }
                }
            }

            //Now that we know who needs what, divy up the resources.
            totalResources = this.part.Resources.Count;
            if (totalResources > 0)
            {
                for (int curResourceIndex = 0; curResourceIndex < totalResources; curResourceIndex++)
                {
                    resource = this.part.Resources[curResourceIndex];
                    if (requiredResourceMap.ContainsKey(resource.resourceName))
                    {
                        //Calculate share amount
                        shareAmount = resource.amount / requiredResourceMap[resource.resourceName].Count;
                        if (shareAmount < 0.0001f)
                            continue;

                        //Debugging: track the share amount
                        if (debugMode)
                        {
                            if (shareAmounts.ContainsKey(resource.resourceName) == false)
                                shareAmounts.Add(resource.resourceName, 0);
                            if (currentAmounts.ContainsKey(resource.resourceName) == false)
                                currentAmounts.Add(resource.resourceName, 0);
                            currentAmounts[resource.resourceName] = resource.amount;
                            shareAmounts[resource.resourceName] = shareAmount;
                        }

                        //Get the experiments
                        experiments = requiredResourceMap[resource.resourceName];

                        //Now go through all the experiments and tell them to take their share
                        totalSlots = experiments.Count;
                        remainder = 0;
                        for (int index = 0; index < totalSlots; index++)
                            remainder += experiments[index].TakeShare(resource.resourceName, shareAmount);

                        //Resource has been divied up.
                        resource.amount = remainder;
                        if (resource.amount < 0.0001f)
                            resource.amount = 0f;
                    }
                }
            }

            //Finally, pack the resources up
            totalSlots = experimentSlots.Length;
            for (int index = 0; index < totalSlots; index++)
            {
                //Get the experiment slot
                experimentSlot = experimentSlots[index];
                experimentSlot.PackResources();
            }
        }

        public void SetupGUI(bool guiVisible)
        {
            isGUIVisible = guiVisible;
            Events["ShowManifestGUI"].guiActive = isGUIVisible;
            Events["ShowManifestGUI"].guiActiveEditor = isGUIVisible;
        }

        public WBIModuleScienceExperiment[] GetExperimentSlots()
        {
            if (experimentSlots != null)
                return experimentSlots;
            List<WBIModuleScienceExperiment> experiments = this.part.FindModulesImplementing<WBIModuleScienceExperiment>();
            int totalCount = experiments.Count;
            int index;
            WBIModuleScienceExperiment experiment;

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

        protected void setupExperimentResources()
        {
            ConfigNode nodeResource = null;
            PartResource resource = null;
            string resourceName;
            string[] mapKeys;
            int index, experimentIndex;
            int totalExperiments = experimentSlots.Length;
            WBIModuleScienceExperiment experiment;
            List<string> addedResources = new List<string>();

            for (experimentIndex = 0; experimentIndex < totalExperiments; experimentIndex++)
            {
                experiment = experimentSlots[experimentIndex];
                if (experiment.experimentID != experiment.defaultExperiment)
                {
                    mapKeys = experiment.resourceMap.Keys.ToArray<string>();
                    for (index = 0; index < mapKeys.Length; index++)
                    {
                        resourceName = mapKeys[index];

                        //Add the resource if needed
                        if (this.part.Resources.Contains(resourceName) == false)
                        {
                            nodeResource = new ConfigNode("RESOURCE");
                            nodeResource.AddValue("name", resourceName);
                            nodeResource.AddValue("amount", "0");
                            nodeResource.AddValue("maxAmount", experiment.resourceMap[resourceName].targetAmount.ToString());
                            resource = this.part.Resources.Add(nodeResource);
                            resource.isVisible = false;
                            addedResources.Add(resourceName);
                        }

                        //Add to max amount to account for amount that the experiment needs
                        else if (addedResources.Contains(resourceName))
                        {
                            this.part.Resources[resourceName].maxAmount += experiment.resourceMap[resourceName].targetAmount;
                        }
                    }
                }
            }

            //Dirty the GUI
            if (addedResources.Count > 0)
                MonoUtilities.RefreshContextWindows(this.part);
        }

        protected void OnExperimentReceived(WBIModuleScienceExperiment transferRecipient)
        {
            ConfigNode nodeResource = null;
            PartResource resource = null;
            string resourceName;
            string[] mapKeys;
            int index;

            //If the experiment has resources then go through them and either add
            //the resources it needs or increase the max amount
            if (transferRecipient.resourceMap != null)
            {
                mapKeys = transferRecipient.resourceMap.Keys.ToArray<string>();
                for (index = 0; index < mapKeys.Length; index++)
                {
                    resourceName = mapKeys[index];

                    //Add the resource if needed
                    if (this.part.Resources.Contains(resourceName) == false)
                    {
                        nodeResource = new ConfigNode("RESOURCE");
                        nodeResource.AddValue("name", resourceName);
                        nodeResource.AddValue("amount", "0");
                        nodeResource.AddValue("maxAmount", transferRecipient.resourceMap[resourceName].targetAmount.ToString());
                        resource = this.part.Resources.Add(nodeResource);
                        resource.isVisible = false;
                    }

                    //Add to max amount to account for amount that the experiment needs
                    else
                    {
                        this.part.Resources[resourceName].maxAmount += transferRecipient.resourceMap[resourceName].targetAmount;
                    }
                }

                //Dirty the GUI
                MonoUtilities.RefreshContextWindows(this.part);
            }
        }

        protected void OnExperimentTransfered(WBIModuleScienceExperiment transferedExperiment)
        {
            PartResource resource = null;
            List<PartResource> doomedResources = new List<PartResource>();
            string resourceName;
            string[] mapKeys;
            int index;
            PartResource doomed;
            int totalCount;

            //If the resource map isn't null, then go through all the resources
            //and if we have them, then reduce the max amount by the amount required by
            //the experiment. If the new max amount is <= 0 then remove the resource.
            if (transferedExperiment.resourceMap != null)
            {
                mapKeys = transferedExperiment.resourceMap.Keys.ToArray<string>();
                for (index = 0; index < mapKeys.Length; index++)
                {
                    resourceName = mapKeys[index];

                    if (this.part.Resources.Contains(resourceName))
                    {
                        resource = this.part.Resources[resourceName];
                        resource.maxAmount -= transferedExperiment.resourceMap[resourceName].targetAmount;
                        if (resource.maxAmount <= 0.001f)
                            doomedResources.Add(resource);
                        else if (resource.amount > resource.maxAmount)
                            resource.amount = resource.maxAmount;
                    }
                }

                //Remove any resources we don't need.
                totalCount = doomedResources.Count;
                for (index = 0; index < totalCount; index++)
                {
                    doomed = doomedResources[index];

                    ResourceHelper.RemoveResource(doomed.resourceName, this.part);
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
            if (isAvailable == false)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("<color=yellow>" + unavailableMessage + "</color>");
                GUILayout.EndVertical();
                return;
            }

            //If we're in the editor or the manifest has no experiment slots then hook em up!
            if (HighLogic.LoadedSceneIsEditor || manifestAdmin.experimentSlots == null)
            {
                GetExperimentSlots();
                manifestAdmin.experimentSlots = this.experimentSlots;
            }

            manifestAdmin.DrawGUIControls();
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add(opsButtonName);
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
            int index;
            WBIModuleScienceExperiment experimentSlot;

            if (experimentSlots == null)
                return defaultCost;

            for (index = 0; index < experimentSlots.Length; index++)
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
            int index;
            WBIModuleScienceExperiment experimentSlot;

            if (experimentSlots == null)
                return 0;

            for (index = 0; index < experimentSlots.Length; index++)
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
