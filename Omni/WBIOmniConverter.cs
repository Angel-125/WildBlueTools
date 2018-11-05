using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Omni Converter")]
    public class WBIOmniConverter : WBIModuleResourceConverterFX, IOpsView
    {
        #region constants
        private const float kminimumSuccess = 80f;
        private const float kCriticalSuccess = 95f;
        private const float kCriticalFailure = 33f;
        private const float kDefaultHoursPerCycle = 1.0f;

        //Summary messages for lastAttempt
        protected string attemptCriticalFail = "Critical Failure";
        protected string attemptCriticalSuccess = "Critical Success";
        protected string attemptFail = "Fail";
        protected string attemptSuccess = "Success";

        //User messages for last attempt
        public float kMessageDuration = 5.0f;
        public string criticalFailMessage = "Production yield lost!";
        public string criticalSuccessMessage = "Production yield higher than expected.";
        public string failMessage = "Production yield lower than expected.";
        public string successMessage = "Production completed.";
        #endregion

        #region Template management fields
        /// <summary>
        /// Name of the converter's PAW button
        /// </summary>
        [KSPField]
        public string managedName = "OmniConverter";

        /// <summary>
        /// What config nodes to use for the converter
        /// </summary>
        [KSPField]
        public string templateNodes = string.Empty;

        /// <summary>
        /// Tags associated with the converter; serves as a filter for templateNodes
        /// </summary>
        [KSPField]
        public string templateTags = string.Empty;

        /// <summary>
        /// The current converter template
        /// </summary>
        [KSPField(isPersistant = true)]
        public string currentTemplateName = string.Empty;

        /// <summary>
        /// The skill required to reconfigure the converter
        /// </summary>
        [KSPField]
        public string reconfigureSkill = "ConverterSkill";

        /// <summary>
        /// The skill rank required to reconfigure the converter.
        /// </summary>
        [KSPField]
        public int reconfigureRank = 0;

        /// <summary>
        /// The resource required to reconfigure the converter. This is optional.
        /// </summary>
        [KSPField]
        public string requiredResource = "Equipment";

        /// <summary>
        /// The amount of required resource needed in order to reconfigure the converter.
        /// </summary>
        [KSPField]
        public float requiredAmount;

        /// <summary>
        /// Used in place of BonusEfficiency, BaseEfficiency is used to determine how well an Omni Converter works. The standard converter is assumed but be a 1-cubic meter 
        /// piece of machinery; smaller converters are less efficient, larger converters are more efficient.
        /// </summary>
        [KSPField]
        public float BaseEfficiency = 1.0f;

        /// <summary>
        /// Title of the GUI window.
        /// </summary>
        [KSPField]
        public string opsViewTitle = "Reconfigure Converter";

        /// <summary>
        /// Label for the Ops Manager button button
        /// </summary>
        [KSPField]
        public string opsButtonLabel = "Converters";

        /// <summary>
        /// Flag to indicate whether or not to show the built-in display window. The converter supports IOpsView so it can be integrated with an existing Ops Manager.
        /// </summary>
        [KSPField]
        public bool showOpsView = false;
        #endregion

        #region Timed Resource Fields
        /// <summary>
        /// On a roll of 1 - 100, the minimum roll required to declare a successful resource yield. Set to 0 if you don't want to roll for success.
        /// </summary>
        [KSPField]
        public float minimumSuccess;

        /// <summary>
        /// On a roll of 1 - 100, minimum roll for a resource yield to be declared a critical success.
        /// </summary>
        [KSPField]
        public float criticalSuccess;

        /// <summary>
        /// On a roll of 1 - 100, the maximum roll for a resource yield to be declared a critical failure.
        /// </summary>
        [KSPField]
        public float criticalFail;

        /// <summary>
        /// How many hours to wait before producing resources defined by YIELD_RESOURCE nodes.
        /// </summary>
        [KSPField]
        public double hoursPerCycle;

        /// <summary>
        /// The time at which we started a new resource production cycle.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double cycleStartTime;

        /// <summary>
        /// Current progress of the production cycle
        /// </summary>
        [KSPField(guiActive = true, guiName = "Progress", isPersistant = true)]
        public string progress = string.Empty;

        /// <summary>
        /// Results of the last production cycle attempt.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Last Attempt", isPersistant = true)]
        public string lastAttempt = string.Empty;

        /// <summary>
        /// If the yield check is a critical success, multiply the units produced by this number. Default is 1.0.
        /// </summary>
        [KSPField]
        public double criticalSuccessMultiplier = 1.0;

        /// <summary>
        /// If the yield check is a failure, multiply the units produced by this number. Default is 1.0.
        /// </summary>
        [KSPField]
        public double failureMultiplier = 1.0;

        #endregion

        #region FX fields
        /// <summary>
        /// Name of the effect to play when the converter starts.
        /// </summary>
        [KSPField]
        public string startEffect = string.Empty;

        /// <summary>
        /// Name of the effect to play when the converter stops.
        /// </summary>
        [KSPField]
        public string stopEffect = string.Empty;

        /// <summary>
        /// Name of the effect to play while the converter is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;
        #endregion

        #region Background processing
        /// <summary>
        /// Enables the converter to run even while the vessel is unloaded. USE SPARINGLY! Too many converters run in the background will slow the game down.
        /// For performance reasons, converters will run in the background once every six hours.
        /// </summary>
        [KSPField]
        public bool enableBackgroundProcessing;

        /// <summary>
        /// Unique ID of the converter. Used to identify it during background processing.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string ID;
        #endregion

        #region Housekeeping
        [KSPField]
        public int minimumVesselPercentEC = 5;
        private double minECLevel = 0;
        private bool checkECLevel;
        PartResourceDefinition resourceDef = null;

        public ConfigNode currentTemplate;
        public ConfigNode viewedTemplate;
        public int templateIndex;

        protected TemplateManager templateManager;

        private Vector2 scrollPos, scrollPosInfo;
        private string converterInfo;
        private bool confirmedReconfigure;
        private WBIAffordableSwitcher switcher;
        private WBISingleOpsView opsView;
        private string searchText = string.Empty;
        private Dictionary<string, string> searchableTexts;

        //Timekeeping for producing resources after a set amount of time.
        public double elapsedTime;
        public double secondsPerCycle = 0f;
        public List<ResourceRatio> yieldResources = new List<ResourceRatio>();
        protected bool missingResources;
        #endregion

        #region Overrides

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("<b>" + managedName + "</b>");
            info.AppendLine("Inputs and outputs vary depending upon current configuration.");

            return info.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //Setup the template manager if needed
            if (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER)
                setupTemplateManager();
        }

        public void Destroy()
        {
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselDestroy.Add(onVesselDestroy);

            //Create unique ID if needed
            if (string.IsNullOrEmpty(ID))
                ID = Guid.NewGuid().ToString();

            //Setup the template manager if needed.
            setupTemplateManager();

            //Get the switcher
            switcher = this.part.FindModuleImplementing<WBIAffordableSwitcher>();
            if (switcher != null)
                switcher.OnGetReconfigureResources += GetRequiredResources;

            //GUI
            if (templateManager.templateNodes.Length > 0)
            {
                this.Events["ToggleOpsView"].guiName = "Setup " + managedName;
                this.Events["ToggleOpsView"].active = showOpsView;

                if (yieldResources.Count > 0)
                {
                    this.Fields["progress"].guiActive = showOpsView;
                    this.Fields["lastAttempt"].guiActive = showOpsView;
                }
                else
                {
                    this.Fields["progress"].guiActive = false;
                    this.Fields["lastAttempt"].guiActive = false;
                }
            }
            else //No templates, we're operating in single converter mode.
            {
                this.Events["ToggleOpsView"].active = false;
                if (yieldResources.Count > 0)
                {
                    this.Fields["progress"].guiActive = true;
                    this.Fields["lastAttempt"].guiActive = true;
                }
                else
                {
                    this.Fields["progress"].guiActive = false;
                    this.Fields["lastAttempt"].guiActive = false;
                }
            }

            //Setup defaults if needed
            progress = "None";
            if (hoursPerCycle == 0f)
                hoursPerCycle = kDefaultHoursPerCycle;
            secondsPerCycle = hoursPerCycle * 3600;

            if (minimumSuccess == 0)
                minimumSuccess = kminimumSuccess;
            if (criticalSuccess == 0)
                criticalSuccess = kCriticalSuccess;
            if (criticalFail == 0)
                criticalFail = kCriticalFailure;

            //Load yield resources if needed
            loadYieldResources();

            //Update background processing
            updateBackgroundConverter();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (node.HasValue("currentTemplateName"))
                node.SetValue("currentTemplateName", currentTemplateName);
            else
                node.AddValue("currentTemplateName", currentTemplateName);
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();

            cycleStartTime = Planetarium.GetUniversalTime();
            lastUpdateTime = cycleStartTime;
            elapsedTime = 0.0f;

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(startEffect, 1.0f);

            updateBackgroundConverter();
        }
        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            progress = "None";

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(stopEffect, 1.0f);
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 0.0f);

            updateBackgroundConverter();
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            if (FlightGlobals.ready == false)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (ModuleIsActive() == false)
                return;
            if (hoursPerCycle == 0f)
                return;
            if (yieldResources.Count == 0)
                return;
            if (!IsActivated)
                return;

            //Check minimum EC levels
            if (checkECLevel)
            {
                double amount = 0;
                double maxAmount = 0;
                this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);
                if ((amount / maxAmount) < minECLevel)
                {
                    StopResourceConverter();
                    return;
                }
            }

            //Play the runningEffect
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 1.0f);

            //Check cycle start time
            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                lastUpdateTime = cycleStartTime;
                elapsedTime = 0.0f;
                return;
            }

            //If we're missing resources then we're done.
            if (!string.IsNullOrEmpty(result.Status))
            {
                if (result.Status.ToLower().Contains("missing"))
                {
                    status = result.Status;
                    missingResources = true;
                    return;
                }
            }

            //Calculate elapsed time
            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;

            //Calculate progress
            CalculateProgress();

            //If we've elapsed time cycle then perform the analyis.
            float completionRatio = (float)(elapsedTime / secondsPerCycle);
            if (completionRatio > 1.0f && !missingResources)
            {
                int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                int currentCycle;
                for (currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                {
                    PerformAnalysis();

                    //Reset start time
                    cycleStartTime = Planetarium.GetUniversalTime();
                }
            }

            //Update status
            if (yieldResources.Count > 0)
                status = "Progress: " + progress;
            else if (string.IsNullOrEmpty(status))
                status = "Running";
        }
        #endregion

        #region Resource Conversion
        protected void setupSearchTexts()
        {
            if (searchableTexts != null)
                return;
            searchableTexts = new Dictionary<string, string>();

            ConfigNode templateNode;
            ConfigNode[] resourceNodes;
            ConfigNode resourceNode;
            StringBuilder searchableText;
            string converterName;
            for (int index = 0; index < templateManager.templateNodes.Length; index++)
            {
                searchableText = new StringBuilder();

                templateNode = templateManager[index];

                //converter name
                if (templateNode.HasValue("ConverterName"))
                {
                    converterName = templateNode.GetValue("ConverterName");
                    searchableText.Append(converterName.ToLower());
                }
                else
                {
                    converterName = string.Empty;
                }

                //Inputs
                if (templateNode.HasNode("INPUT_RESOURCE"))
                {
                    resourceNodes = templateNode.GetNodes("INPUT_RESOURCE");

                    for (int nodeIndex = 0; nodeIndex < resourceNodes.Length; nodeIndex++)
                    {
                        resourceNode = resourceNodes[nodeIndex];

                        if (resourceNode.HasValue("ResourceName"))
                            searchableText.Append(resourceNode.GetValue("ResourceName").ToLower());
                    }
                }

                //Outputs
                if (templateNode.HasNode("OUTPUT_RESOURCE"))
                {
                    resourceNodes = templateNode.GetNodes("OUTPUT_RESOURCE");

                    for (int nodeIndex = 0; nodeIndex < resourceNodes.Length; nodeIndex++)
                    {
                        resourceNode = resourceNodes[nodeIndex];

                        if (resourceNode.HasValue("ResourceName"))
                            searchableText.Append(resourceNode.GetValue("ResourceName").ToLower());
                    }
                }

                //Required
                if (templateNode.HasNode("REQUIRED_RESOURCE"))
                {
                    resourceNodes = templateNode.GetNodes("REQUIRED_RESOURCE");

                    for (int nodeIndex = 0; nodeIndex < resourceNodes.Length; nodeIndex++)
                    {
                        resourceNode = resourceNodes[nodeIndex];

                        if (resourceNode.HasValue("ResourceName"))
                            searchableText.Append(resourceNode.GetValue("ResourceName").ToLower());
                    }
                }

                //Yield
                if (templateNode.HasNode("YIELD_RESOURCE"))
                {
                    resourceNodes = templateNode.GetNodes("YIELD_RESOURCE");

                    for (int nodeIndex = 0; nodeIndex < resourceNodes.Length; nodeIndex++)
                    {
                        resourceNode = resourceNodes[nodeIndex];

                        if (resourceNode.HasValue("ResourceName"))
                            searchableText.Append(resourceNode.GetValue("ResourceName").ToLower());
                    }
                }

                //Experience trait
                if (templateNode.HasValue("ExperienceEffect"))
                    searchableText.Append(templateNode.GetValue("ExperienceEffect").ToLower());

                //Done
                if (!string.IsNullOrEmpty(converterName))
                    searchableTexts.Add(converterName, searchableText.ToString());
            }
        }

        protected void setupTemplateManager()
        {
            //Create the templateManager
            if (templateManager == null)
                templateManager = new TemplateManager(this.part, this.vessel, new LogDelegate(Log), templateNodes, templateTags);

            //Get the current template
            if (string.IsNullOrEmpty(currentTemplateName))
            {
                templateIndex = 0;
            }
            else
            {
                templateIndex = templateManager.FindIndexOfTemplate(currentTemplateName, "ConverterName");
                if (templateIndex != -1)
                    currentTemplate = templateManager.templateNodes[templateIndex];
                else
                    templateIndex = 0;
            }

            //Setup the inputs and outputs
            setupConverterTemplate();
        }

        protected virtual void setupConverterTemplate()
        {
            if (currentTemplate == null)
                return;

            //ConverterName
            if (currentTemplate.HasValue("ConverterName"))
                ConverterName = currentTemplate.GetValue("ConverterName");

            //Action names
            if (currentTemplate.HasValue("StartActionName"))
                StartActionName = currentTemplate.GetValue("StartActionName");
            if (currentTemplate.HasValue("StopActionName"))
                StopActionName = currentTemplate.GetValue("StopActionName");
            if (currentTemplate.HasValue("ToggleActionName"))
                ToggleActionName = currentTemplate.GetValue("ToggleActionName");

            //AutoShutdown
            if (currentTemplate.HasValue("AutoShutdown"))
                bool.TryParse(currentTemplate.GetValue("AutoShutdown"), out AutoShutdown);

            //Specialist stuff
            if (currentTemplate.HasValue("UseSpecialistBonus"))
                bool.TryParse(currentTemplate.GetValue("UseSpecialistBonus"), out UseSpecialistBonus);
            if (UseSpecialistBonus)
            {
                if (currentTemplate.HasValue("SpecialistEfficiencyFactor"))
                    float.TryParse(currentTemplate.GetValue("SpecialistEfficiencyFactor"), out SpecialistEfficiencyFactor);

                if (currentTemplate.HasValue("SpecialistBonusBase"))
                    float.TryParse(currentTemplate.GetValue("SpecialistBonusBase"), out SpecialistBonusBase);

                if (currentTemplate.HasValue("ExperienceEffect"))
                    ExperienceEffect = currentTemplate.GetValue("ExperienceEffect");
            }

            //Efficiency
            if (currentTemplate.HasValue("EfficiencyBonus"))
                float.TryParse(currentTemplate.GetValue("EfficiencyBonus"), out EfficiencyBonus);
            EfficiencyBonus *= BaseEfficiency;

            //Clear existing lists
            inputList.Clear();
            outputList.Clear();
            reqList.Clear();

            //Inputs
            ConfigNode[] resourceNodes;
            ResourceRatio resourceRatio;
            if (currentTemplate.HasNode("INPUT_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    inputList.Add(resourceRatio);

                    //Check for EC input
                    if (resourceRatio.ResourceName == "ElectricCharge" && minimumVesselPercentEC > 0)
                    {
                        PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                        resourceDef = definitions["ElectricCharge"];
                        minECLevel = (double)minimumVesselPercentEC / 100.0f;
                        checkECLevel = true;
                    }
                }
            }

            //Outputs
            if (currentTemplate.HasNode("OUTPUT_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("OUTPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    outputList.Add(resourceRatio);
                }
            }

            //Required
            if (currentTemplate.HasNode("REQUIRED_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("REQUIRED_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    reqList.Add(resourceRatio);
                }
            }

            //Yield
            loadYieldResources();
            if (yieldResources.Count > 0)
            {
                this.Fields["progress"].guiActive = showOpsView;
                this.Fields["lastAttempt"].guiActive = showOpsView;
            }
            else
            {
                this.Fields["progress"].guiActive = false;
                this.Fields["lastAttempt"].guiActive = false;
            }

            //Timed production fields
            if (currentTemplate.HasValue("hoursPerCycle"))
                double.TryParse(currentTemplate.GetValue("hoursPerCycle"), out hoursPerCycle);
            secondsPerCycle = hoursPerCycle * 3600;

            if (currentTemplate.HasValue("minimumSuccess"))
                float.TryParse(currentTemplate.GetValue("minimumSuccess"), out minimumSuccess);
            if (currentTemplate.HasValue("criticalSuccess"))
                float.TryParse(currentTemplate.GetValue("criticalSuccess"), out criticalSuccess);
            if (currentTemplate.HasValue("criticalFail"))
                float.TryParse(currentTemplate.GetValue("criticalFail"), out criticalFail);

            if (currentTemplate.HasValue("criticalSuccessMultiplier"))
                double.TryParse(currentTemplate.GetValue("criticalSuccessMultiplier"), out criticalSuccessMultiplier);
            if (currentTemplate.HasValue("failureMultiplier"))
                double.TryParse(currentTemplate.GetValue("failureMultiplier"), out failureMultiplier);

            //Background processing
            if (currentTemplate.HasValue("enableBackgroundProcessing"))
                bool.TryParse(currentTemplate.GetValue("enableBackgroundProcessing"), out enableBackgroundProcessing);
            else
                enableBackgroundProcessing = false;
            if (enableBackgroundProcessing)
                registerForBackgroundProcessing();
            else
                unregisterForBackgroundProcessing();

            //Reload the recipie
            this._recipe = this.LoadRecipe();
        }

        #endregion

        #region Templates
        protected void previewTemplate(ConfigNode templateNode)
        {
            if (templateNode == null)
                return;
            if (!string.IsNullOrEmpty(converterInfo))
                return;
            StringBuilder info = new StringBuilder();

            //Converter Name
            if (templateNode.HasValue("ConverterName"))
                info.AppendLine("<color=lightblue><b>" + templateNode.GetValue("ConverterName") + "</b></color>");

            //Description
            info.AppendLine(" ");
            if (templateNode.HasValue("description"))
                info.AppendLine(templateNode.GetValue("description"));

            ConfigNode[] resourceNodes;
            ResourceRatio resourceRatio;
            PartResourceDefinition definition;
            if (templateNode.HasNode("INPUT_RESOURCE"))
            {
                info.AppendLine(" ");
                info.AppendLine("<color=white><b>Inputs</b></color>");
                resourceNodes = templateNode.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + getFlowModeDescription(resourceRatio.FlowMode));
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Outputs
            if (templateNode.HasNode("OUTPUT_RESOURCE"))
            {
                info.AppendLine(" ");
                info.AppendLine("<color=lightblue><b>Outputs</b></color>");
                resourceNodes = templateNode.GetNodes("OUTPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + getFlowModeDescription(resourceRatio.FlowMode));
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Yield
            if (templateNode.HasNode("YIELD_RESOURCE"))
            {
                info.AppendLine("<color=lightblue><b>Produced over time</b></color>");
                if (templateNode.HasValue("hoursPerCycle"))
                {
                    double productionTime = 0;
                    double.TryParse(templateNode.GetValue("hoursPerCycle"), out productionTime);
                    info.AppendLine(string.Format("Production Time: {0:n2}hrs", productionTime));
                }

                resourceNodes = templateNode.GetNodes("YIELD_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + string.Format("{0:n2} units", resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + getFlowModeDescription(resourceRatio.FlowMode));
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Required
            if (templateNode.HasNode("REQUIRED_RESOURCE"))
            {
                info.AppendLine("<color=orange><b>Required</b></color>");
                resourceNodes = templateNode.GetNodes("REQUIRED_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + getFlowModeDescription(resourceRatio.FlowMode));
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Specialist info
            bool useBonus = false;
            if (templateNode.HasValue("UseSpecialistBonus"))
                bool.TryParse(templateNode.GetValue("UseSpecialistBonus"), out useBonus);
            if (useBonus)
            {
                info.AppendLine(" ");
                if (templateNode.HasValue("ExperienceEffect"))
                    info.AppendLine("<color=white><b>Bonus Output Skill: </b>" + templateNode.GetValue("ExperienceEffect") + "</color>");
            }

            //Set the information view string
            converterInfo = info.ToString();
        }
        #endregion

        #region IOpsView
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Setup Converter")]
        public void ToggleOpsView()
        {
            if (templateManager.templateNodes.Length == 0)
            {
                ScreenMessages.PostScreenMessage("No templates to choose from", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Events["ToggleOpsView"].active = false;
                return;
            }

            //Setup the ops view
            if (opsView == null)
            {
                opsView = new WBISingleOpsView();
                opsView.WindowTitle = opsViewTitle;
                opsView.buttonLabel = opsButtonLabel;
                opsView.opsView = this;
            }
            opsView.SetVisible(!opsView.IsVisible());
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add(managedName);
            return buttonLabels;
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            setupSearchTexts();
            GUILayout.BeginHorizontal();

            //Current configuration
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(250) });
            if (currentTemplate != null)
                GUILayout.Label("<color=white><b>Configuration: </b>" + currentTemplate.GetValue("ConverterName") + "</color>");
            else
                GUILayout.Label("<color=white><b>Configuration: </b>NONE</color>");

            //Reconfigure cost
            if (!string.IsNullOrEmpty(requiredResource))
                GUILayout.Label("<color=white><b>Cost: </b>" + requiredResource + string.Format("({0:f2})", requiredAmount) + "</color>");

            //Reconfigure skill
            if (!string.IsNullOrEmpty(reconfigureSkill))
                GUILayout.Label("<color=white><b>Reconfigure Trait(s): </b>" + getRequiredTraits() + "</color>");


            //Search field
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>Search</color>");
            searchText = GUILayout.TextField(searchText).ToLower();
            GUILayout.EndHorizontal();

            //Draw the converter options
            ConfigNode template;
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            string searchableText;
            string converterName;
            for (int index = 0; index < templateManager.templateNodes.Length; index++)
            {
                template = templateManager.templateNodes[index];

                converterName = template.GetValue("ConverterName");
                searchableText = searchableTexts[converterName];

                //Skip the template if it doesn't contain the search text.
                if (!string.IsNullOrEmpty(searchText) && !string.IsNullOrEmpty(searchableText))
                {
                    if (!searchableText.Contains(searchText))
                        continue;
                }

                template = templateManager.templateNodes[index];
                if (templateManager.TemplateTagsMatch(template))
                {
                    if (TemplateManager.TemplateTechResearched(template))
                    {
                        if (GUILayout.Button(converterName))
                        {
                            converterInfo = null;
                            previewTemplate(template);
                            viewedTemplate = template;
                        }
                    }
                    else
                    {
                        GUILayout.Label("<color=#959595>" + converterName + ":\r\nNeeds " + TemplateManager.GetTechTreeTitle(template) + "</color>");
                    }
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Reconfigure"))
            {
                //In flight mode, we need to make sure that we can afford to reconfigure the converter and that
                //the user has confirmed the operation.
                if (HighLogic.LoadedSceneIsFlight)
                {
                    //Can we afford to reconfigure the converter?
                    if (canAffordReconfigure() && hasSufficientSkill())
                    {
                        //Confirm reconfigure.
                        if (!confirmedReconfigure)
                        {
                            ScreenMessages.PostScreenMessage("Click again to confirm reconfiguration.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            confirmedReconfigure = true;
                        }

                        else
                        {
                            payForReconfigure();
                            reconfigureConverter();
                        }
                    }
                }

                //Not in flight so just reconfigure
                else
                {
                    reconfigureConverter();
                }
            }
            GUILayout.EndVertical();

            //Draw the info pane
            scrollPosInfo = GUILayout.BeginScrollView(scrollPosInfo);
            if (!string.IsNullOrEmpty(converterInfo))
                GUILayout.Label(converterInfo);
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
        #endregion

        #region Logging
        public virtual void Log(object message)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER ||
                HighLogic.LoadedScene == GameScenes.PSYSTEM || HighLogic.LoadedScene == GameScenes.SETTINGS)
                return;

            if (!WBIMainSettings.EnableDebugLogging)
                return;

            Debug.Log("[" + this.ClassName + "] - " + message);
        }
        #endregion

        #region Helpers
        protected void updateBackgroundConverter()
        {
            if (enableBackgroundProcessing)
            {
                WBIBackgroundConverter backgroundConverter = WBIOmniManager.Instance.GetBackgroundConverter(this);
                if (backgroundConverter != null)
                {
                    //Update vessel ID as that may have changed
                    backgroundConverter.vesselID = this.part.vessel.id.ToString();

                    //Reset the background processing flags since conditions may have changed.
                    backgroundConverter.IsActivated = this.IsActivated;
                    backgroundConverter.isMissingResources = false;
                    backgroundConverter.isContainerFull = false;

                    WBIOmniManager.Instance.UpdateBackgroundConverter(backgroundConverter);
                }

                //Background converter doesn't exist so create it.
                else
                {
                    WBIOmniManager.Instance.RegisterBackgroundConverter(this);
                }
            }
        }

        protected void onVesselChange(Vessel vessel)
        {
            updateBackgroundConverter();
        }

        protected void onVesselDestroy(Vessel vessel)
        {
            if (vessel == this.part.vessel)
            {
                WBIOmniManager.Instance.UnregisterBackgroundConverter(this);
            }
        }

        protected void registerForBackgroundProcessing()
        {
            WBIOmniManager.Instance.RegisterBackgroundConverter(this);
        }

        protected void unregisterForBackgroundProcessing()
        {
            WBIOmniManager.Instance.UnregisterBackgroundConverter(this);
        }

        protected void loadYieldResources()
        {
            if (this.part.partInfo.partConfig == null)
                return;
            if (currentTemplate == null)
                return;
            ConfigNode[] nodes = null;
            ConfigNode node = null;

            //Get the nodes we're interested in
            nodes = currentTemplate.GetNodes("YIELD_RESOURCE");
            if (nodes.Length == 0)
                return;

            //Ok, start processing the yield resources
            yieldResources.Clear();
            ResourceRatio yieldResource;
            string resourceName;
            double amount;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (!node.HasValue("ResourceName"))
                    continue;
                resourceName = node.GetValue("ResourceName");

                if (!node.HasValue("Ratio"))
                    continue;
                if (!double.TryParse(node.GetValue("Ratio"), out amount))
                    continue;

                yieldResource = new ResourceRatio(resourceName, amount, true);
                yieldResource.FlowMode = ResourceFlowMode.ALL_VESSEL;

                yieldResources.Add(yieldResource);
            }
        }
        public virtual void PerformAnalysis()
        {
            //If we have no minimum success then just produce the yield resources.
            if (minimumSuccess <= 0.0f)
            {
                produceYieldResources(1.0);
                return;
            }

            //Ok, go through the analysis.
            float analysisRoll = performAnalysisRoll();

            if (analysisRoll <= criticalFail)
                onCriticalFailure();

            else if (analysisRoll >= criticalSuccess)
                onCriticalSuccess();

            else if (analysisRoll >= minimumSuccess)
                onSuccess();

            else
                onFailure();

        }

        protected virtual float performAnalysisRoll()
        {
            float roll = 0.0f;

            //Roll 3d6 to approximate a bell curve, then convert it to a value between 1 and 100.
            roll = UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll *= 5.5556f;

            //Done
            return roll;
        }

        protected virtual void onCriticalFailure()
        {
            lastAttempt = attemptCriticalFail;

            if (qualityControl != null)
                qualityControl.DeclarePartBroken();
            StopResourceConverter();

            //Show user message
            ScreenMessages.PostScreenMessage(ConverterName + ": " + criticalFailMessage, kMessageDuration);
        }

        protected virtual void onCriticalSuccess()
        {
            lastAttempt = attemptCriticalSuccess;
            produceYieldResources(criticalSuccessMultiplier);

            //Show user message
            ScreenMessages.PostScreenMessage(ConverterName + ": " + criticalSuccessMessage, kMessageDuration);
        }

        protected virtual void onFailure()
        {
            lastAttempt = attemptFail;
            produceYieldResources(failureMultiplier);

            //Show user message
            ScreenMessages.PostScreenMessage(ConverterName + ": " + failMessage, kMessageDuration);
        }

        protected virtual void onSuccess()
        {
            lastAttempt = attemptSuccess;
            produceYieldResources(1.0);

            //Show user message
            ScreenMessages.PostScreenMessage(successMessage, kMessageDuration);
        }

        protected virtual void produceYieldResources(double yieldMultiplier)
        {
            int count = yieldResources.Count;
            ResourceRatio resourceRatio;
            double yieldAmount = 0;
            string resourceName;
            double highestSkill = 0;

            //Find highest skill bonus
            if (UseSpecialistBonus && !string.IsNullOrEmpty(ExperienceEffect))
            {
                List<ProtoCrewMember> crewMembers = this.part.vessel.GetVesselCrew();

                int crewCount = crewMembers.Count;
                for (int index = 0; index < crewCount; index++)
                {
                    if (crewMembers[index].HasEffect(ExperienceEffect))
                    {
                        if (crewMembers[index].experienceLevel > highestSkill)
                            highestSkill = crewMembers[index].experienceTrait.CrewMemberExperienceLevel();
                    }
                }
            }

            //Produce the yield resources
            for (int index = 0; index < count; index++)
            {
                yieldAmount = 0;
                resourceRatio = yieldResources[index];

                resourceName = resourceRatio.ResourceName;
                yieldAmount = resourceRatio.Ratio * (1.0 + (highestSkill * SpecialistEfficiencyFactor)) * yieldMultiplier;

                this.part.RequestResource(resourceName, -yieldAmount, resourceRatio.FlowMode);
            }
        }

        public virtual void CalculateProgress()
        {
            //Get elapsed time (seconds)
            progress = string.Format("{0:f1}%", ((elapsedTime / secondsPerCycle) * 100));
        }

        protected string getFlowModeDescription(ResourceFlowMode flowMode)
        {
            switch (flowMode)
            {
                default:
                case ResourceFlowMode.NO_FLOW:
                    return "None";

                case ResourceFlowMode.ALL_VESSEL:
                    return "All Vessel";

                case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                    return "Stage Priority Flow";

                case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                    return "Stage Priority Search";

                case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                    return "Stage Prioroity Flow (Balanced)";

                case ResourceFlowMode.ALL_VESSEL_BALANCE:
                    return "All Vessel (Balanced)";

                case ResourceFlowMode.STAGE_STACK_FLOW:
                    return "Stack Flow";

                case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                    return "Stack Flow (Balanced)";
            }
        }

        //Switchers will ask us for the resources needed to reconfigure the converter.
        protected void GetRequiredResources()
        {
            if (currentTemplate == null)
                return;
            if (string.IsNullOrEmpty(requiredResource))
                return;

            if (switcher.inputList.ContainsKey(requiredResource))
                switcher.inputList[requiredResource] += requiredAmount;
            else
                switcher.inputList.Add(requiredResource, requiredAmount);
        }

        protected void payForReconfigure()
        {
            //If we don't pay to reconfigure then we're done.
            if (!WBIMainSettings.PayToReconfigure)
                return;

            //If we have a switcher, tell it to pay the cost. It might be able to ask resource distributors.
            if (switcher != null)
            {
                switcher.payForReconfigure(requiredResource, requiredAmount);
                return;
            }

            //We have to pay for it ourselves.
            this.part.RequestResource(requiredResource, requiredAmount, ResourceFlowMode.ALL_VESSEL);
        }

        protected bool hasSufficientSkill()
        {
            if (string.IsNullOrEmpty(reconfigureSkill))
                return true;

            if (WBIMainSettings.RequiresSkillCheck)
            {
                //Check the crew roster
                ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
                for (int index = 0; index < vesselCrew.Length; index++)
                {
                    if (vesselCrew[index].HasEffect(reconfigureSkill))
                    {
                        if (reconfigureRank > 0)
                        {
                            int crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                            if (crewRank >= reconfigureRank)
                                return true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                //Insufficient skill
                ScreenMessages.PostScreenMessage("Insufficient skill to reconfigure the converter.");
                return false;
            }

            return true;
        }

        protected bool canAffordReconfigure()
        {
            if (string.IsNullOrEmpty(requiredResource))
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;

            if (switcher != null)
            {
                //If we're inflatable and not inflated then we're done.
                if (switcher.isInflatable && !switcher.isDeployed)
                    return true;

                //Ask the switcher if we can afford it. It might be able to ask distributed resource containers...
                return switcher.canAffordResource(requiredResource, requiredAmount);
            }

            //Check the available amount of the resource.
            double amountAvailable = ResourceHelper.GetTotalResourceAmount(requiredResource, this.part.vessel);
            if (amountAvailable >= requiredAmount)
                return true;

            //Can't afford it.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef = definitions[requiredResource];
            ScreenMessages.PostScreenMessage("Insufficient " + resourceDef.displayName + " to reconfigure the converter.");
            return false;
        }

        protected void reconfigureConverter()
        {
            currentTemplate = viewedTemplate;
            currentTemplateName = currentTemplate.GetValue("ConverterName");
            templateIndex = templateManager.FindIndexOfTemplate(currentTemplateName, "ConverterName");

            setupConverterTemplate();
        }

        string formatRate(double rate)
        {
            if (rate < 0.0001)
                return string.Format("{0:f2}/day", rate * (double)KSPUtil.dateTimeFormatter.Day);
            else if (rate < 0.01)
                return string.Format("{0:f2}/hr", rate * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return string.Format("{0:f2}/sec", rate);
        }

        protected string getRequiredTraits()
        {
            string[] traits = Utils.GetTraitsWithEffect(reconfigureSkill);
            string requiredTraits = "";

            if (traits.Length == 1)
            {
                requiredTraits = traits[0];
            }

            else if (traits.Length > 1)
            {
                for (int index = 0; index < traits.Length - 1; index++)
                    requiredTraits += traits[index] + ",";
                requiredTraits += " or " + traits[traits.Length - 1];
            }

            else
            {
                requiredTraits = reconfigureSkill;
            }

            return requiredTraits;
        }
        #endregion
    }
}
