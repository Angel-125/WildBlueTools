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

        /// <summary>
        /// Specificies the minimum number of crew that the part must contain in order for the converter to run. Default is 0.
        /// </summary>
        [KSPField]
        public int minimumCrew = 0;

        /// <summary>
        /// Flag that indicates that the converter needs a CommNet connection to the home world in order to run.
        /// </summary>
        [KSPField]
        public bool requiresCommNet = false;

        /// <summary>
        /// Flag that indicates that the converter's part needs to be splashed in order to run.
        /// </summary>
        [KSPField]
        public bool requiresSplashed = false;

        /// <summary>
        /// Flag that indicates that the converter's part needs to be splashed and submerged in order to run.
        /// </summary>
        [KSPField]
        public bool requiresSubmerged = false;

        [KSPField]
        /// <summary>
        /// Flag that indicates that the converter's part needs to be in orbit in order to run.
        /// </summary>
        public bool requiresOrbiting = false;

        /// <summary>
        /// List of anomalies that the converter must be next to in order to function.
        /// </summary>
        [KSPField]
        public string requiredAnomalies = string.Empty;

        /// <summary>
        /// Minimum range from the required anomalies that the converter must be next to in order to function.
        /// </summary>
        [KSPField]
        public float minAnomalyRange = 50.0f;

        /// <summary>
        /// The converter must orbit a star.
        /// </summary>
        [KSPField]
        public bool solarOrbitRequired = false;
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
        [KSPField(isPersistant = true)]
        public bool enableBackgroundProcessing;

        /// <summary>
        /// Unique ID of the converter.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string ID = "none";
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
        public bool situationValid;

        //Timestamp for when the module was started. We need to give mods like Snacks a chance to do their thing or we'll get false positives for our situation checks during initial startup.
        protected double initialStartTime;
        #endregion

        #region Overrides

        public override string GetInfo()
        {
            if (string.IsNullOrEmpty(templateNodes))
                return base.GetInfo();

            StringBuilder info = new StringBuilder();

            info.AppendLine("<color=white><b>" + managedName + "</b></color>");

            info.AppendLine(string.Format("<color=white><b>Base Efficiency: </b>{0:n2}%</color>", this.BaseEfficiency * 100));
            if (!string.IsNullOrEmpty(reconfigureSkill))
            {
                string skillsRequired = "Requires " + getRequiredTraits();
                if (reconfigureRank > 0)
                    skillsRequired = skillsRequired + "(" + reconfigureRank.ToString() + ")";
                info.AppendLine("<color=white>" + skillsRequired + " to reconfigure.</color>");
            }

            if (requiredAmount > 0 && !string.IsNullOrEmpty(requiredResource))
                info.AppendLine(string.Format("<color=white>Requires {0:n2} units of {1:s} to reconfigure</color>", requiredAmount, requiredResource));

            if (requiresCommNet)
                info.AppendLine("<color=white><b>Needs connection to KSC:</b> YES</color>");
            else
                info.AppendLine("<color=white><b>Needs connection to KSC:</b> NO</color>");

            if (requiresOrbiting)
                info.AppendLine("<color=white><b>Must be orbiting:</b> YES</color>");
            else
                info.AppendLine("<color=white><b>Must be orbiting:</b> NO</color>");

            if (requiresSubmerged)
                info.AppendLine("<color=white><b>Must be submerged:</b> YES</color>");
            else if (requiresSplashed)
                info.AppendLine("<color=white><b>Must be splashed:</b> YES</color>");
            else
                info.AppendLine("<b>Must be splashed:</b> NO</color>");

            //Required anomalies
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                info.AppendLine("<b>Anomalies: </b>" + requiredAnomalies.Replace(";", ", "));
                info.AppendLine(string.Format("<b>Minimum Range: </b>{0:f2}m", minAnomalyRange));
            }
            //Solar orbit
            if (solarOrbitRequired)
                info.AppendLine("Requires an orbit around a star");

            info.AppendLine("<color=white>Inputs and outputs vary depending upon current configuration.</color>");

            return info.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //Setup the template manager if needed
            if (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER)
                setupTemplateManager();
        }

        //This avoids a problem where you revert a flight and then create a new part of the same type as the host part, and you get a duplicate loadout for the converter instead of a blank converter.
        public virtual void ResetSettings()
        {
            //Create UUID
            ID = Guid.NewGuid().ToString();

            progress = "None";
            currentTemplateName = string.Empty;
            currentTemplate = null;
            cycleStartTime = 0;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Create unique ID if needed
            if (string.IsNullOrEmpty(ID) || ID == "none")
                ID = Guid.NewGuid().ToString();
            else if (HighLogic.LoadedSceneIsEditor && WBIOmniManager.Instance.WasRecentlyCreated(this.part))
                ResetSettings();

            //Setup the template manager if needed.
            setupTemplateManager();

            //Get the switcher
            switcher = this.part.FindModuleImplementing<WBIAffordableSwitcher>();
            if (switcher != null)
            {
                switcher.RemoveReconfigureDelegate(ID, GetRequiredResources);
                switcher.AddReconfigureDelegate(ID, GetRequiredResources);
            }

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
            Fields["showParticleEffects"].group.name = "Omni";
            Fields["showParticleEffects"].group.displayName = "Omni";
            Events["StartResourceConverter"].group.name = "Omni";
            Events["StartResourceConverter"].group.displayName = "Omni";
            Events["StopResourceConverter"].group.name = "Omni";
            Events["StopResourceConverter"].group.displayName = "Omni";
            if (string.IsNullOrEmpty(startEffect) && string.IsNullOrEmpty(stopEffect) && string.IsNullOrEmpty(runningEffect))
            {
                Fields["showParticleEffects"].guiActive = false;
                Fields["showParticleEffects"].guiActiveEditor = false;
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

            //Get initial start time
            initialStartTime = Planetarium.GetUniversalTime();

            if (!string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, IsActivated ? 1.0f : 0.0f);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
            GameEvents.onPartResourceListChange.Fire(this.part);
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

            if (!string.IsNullOrEmpty(startEffect))
                this.part.Effect(startEffect, 1.0f);
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            progress = "None";

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(stopEffect, 1.0f);
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 0.0f);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, IsActivated ? 1.0f : 0.0f);
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

            //Check initial start time. We need to give other mods like Snacks time to do their setup before we can trust that the converter's situation is valid.
            if (Planetarium.GetUniversalTime() - initialStartTime < 5.0f)
                return;

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
                    cycleStartTime = Planetarium.GetUniversalTime();
                    status = result.Status;
                    return;
                }
            }

            //Calculate elapsed time
            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;

            //Calculate progress
            CalculateProgress();

            //If we've elapsed time cycle then perform the analyis.
            float completionRatio = (float)(elapsedTime / secondsPerCycle);
            StringBuilder resultMessages = new StringBuilder();
            if (completionRatio > 1.0f && situationValid)
            {
                int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                int currentCycle;
                for (currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                    PerformAnalysis(currentCycle + 1);

                //Reset start time
                cycleStartTime = Planetarium.GetUniversalTime();
                elapsedTime = 0.0f;
            }

            //Update status
            if (yieldResources.Count > 0)
                status = "Progress: " + progress;
            else if (string.IsNullOrEmpty(status))
                status = "Running";
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            if (!HighLogic.LoadedSceneIsFlight || !IsActivated)
                return null;

            situationValid = checkSituation();
            if (!situationValid)
                return null;

            return base.PrepareRecipe(deltatime);
        }
        #endregion

        #region Resource Conversion
        protected bool checkSituation()
        {
            // Check situations
            // Submerged takes precedence over splashed.
            // A part can't be splashed or submerged if it requires orbiting
            if (requiresSubmerged)
            {
                if (!this.part.vessel.mainBody.ocean)
                {
                    status = string.Format("Needs to be submerged");
                    cycleStartTime = Planetarium.GetUniversalTime();
                    elapsedTime = 0.0f;
                    return false;
                }

                if (FlightGlobals.getAltitudeAtPos((Vector3d)this.part.transform.position, this.part.vessel.mainBody) > 0.0f)
                {
                    status = string.Format("Needs to be submerged");
                    cycleStartTime = Planetarium.GetUniversalTime();
                    elapsedTime = 0.0f;
                    return false;
                }
            }

            else if (requiresSplashed && !this.part.vessel.Splashed)
            {
                status = string.Format("Needs to be splashed");
                cycleStartTime = Planetarium.GetUniversalTime();
                elapsedTime = 0.0f;
                return false;
            }

            else if (requiresOrbiting && this.part.vessel.situation != Vessel.Situations.ORBITING && this.part.vessel.situation != Vessel.Situations.ESCAPING)
            {
                status = string.Format("Needs to be orbiting");
                cycleStartTime = Planetarium.GetUniversalTime();
                elapsedTime = 0.0f;
                return false;
            }

            //Check CommNet
            if (requiresCommNet && CommNet.CommNetScenario.CommNetEnabled && !this.part.vessel.connection.IsConnectedHome)
            {
                status = string.Format("Needs connection to home world");
                cycleStartTime = Planetarium.GetUniversalTime();
                elapsedTime = 0.0f;
                return false;
            }

            // Check minimum crew
            if (minimumCrew > 0)
            {
                int partCrewCount = this.part.protoModuleCrew.Count;
                if (partCrewCount < minimumCrew && string.IsNullOrEmpty(ExperienceEffect))
                {
                    status = string.Format("Needs crew: ({0}/{1})", partCrewCount, minimumCrew);
                    cycleStartTime = Planetarium.GetUniversalTime();
                    elapsedTime = 0.0f;
                    return false;
                }

                // Check required skill
                else if (!string.IsNullOrEmpty(ExperienceEffect) && minimumCrew > 0)
                {
                    ProtoCrewMember[] partCrew = this.part.protoModuleCrew.ToArray();
                    int requiredSkillCount = 0;
                    for (int index = 0; index < partCrew.Length; index++)
                    {
                        if (partCrew[index].HasEffect(ExperienceEffect))
                            requiredSkillCount += 1;
                    }

                    if (requiredSkillCount < minimumCrew)
                    {
                        status = string.Format("Needs {0} crew with ", minimumCrew);
                        status += ExperienceEffect;
                        cycleStartTime = Planetarium.GetUniversalTime();
                        elapsedTime = 0.0f;
                        return false;
                    }
                }
            }

            // Check anomalies
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                if (!isNearAnomaly())
                {
                    status = "Needs to be near " + requiredAnomalies.Replace(";", ", ");
                    return false;
                }
            }

            // Check solar orbit
            if (solarOrbitRequired)
            {
                if (this.part.vessel.mainBody.scaledBody.GetComponentsInChildren<SunShaderController>(true).Length == 0)
                    return false;
            }

            //Check resources
            int count = inputList.Count;
            double currentAmount, maxAmount;
            PartResourceDefinition resourceDefinition;
            for (int index = 0; index < count; index++)
            {
                resourceDefinition = ResourceHelper.DefinitionForResource(inputList[index].ResourceName);
                if (resourceDefinition == null)
                    continue;

                this.part.vessel.resourcePartSet.GetConnectedResourceTotals(resourceDefinition.id, out currentAmount, out maxAmount, true);

                if (currentAmount <= 0)
                {
                    status = "Missing " + resourceDefinition.displayName;
                    cycleStartTime = Planetarium.GetUniversalTime();
                    elapsedTime = 0.0f;
                    return false;
                }
            }

            return true;
        }

        protected bool isNearAnomaly()
        {
            CelestialBody mainBody = this.part.vessel.mainBody;
            PQSSurfaceObject[] anomalies = mainBody.pqsSurfaceObjects;
            double longitude;
            double latitude;
            double distance = 0f;

            if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                for (int index = 0; index < anomalies.Length; index++)
                {
                    if (requiredAnomalies.Contains(anomalies[index].SurfaceObjectName))
                    {
                        //Get the longitude and latitude of the anomaly
                        longitude = mainBody.GetLongitude(anomalies[index].transform.position);
                        latitude = mainBody.GetLatitude(anomalies[index].transform.position);

                        //Get the distance (in meters) from the anomaly.
                        distance = Utils.HaversineDistance(longitude, latitude,
                            this.part.vessel.longitude, this.part.vessel.latitude, this.part.vessel.mainBody) * 1000;

                        //If we're near the anomaly, then we're done
                        if (distance <= minAnomalyRange)
                            return true;
                    }
                }
            }

            //Not near anomaly or not close enough
            return false;
        }

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
                if (!string.IsNullOrEmpty(converterName) && !searchableTexts.ContainsKey(converterName))
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
            {
                ConverterName = currentTemplate.GetValue("ConverterName");
                Fields["status"].guiName = ConverterName;
            }

            //Action names
            if (currentTemplate.HasValue("StartActionName"))
            {
                StartActionName = currentTemplate.GetValue("StartActionName");
                Events["StartResourceConverter"].guiName = StartActionName;
                Actions["StartResourceConverterAction"].guiName = StartActionName;
            }
            if (currentTemplate.HasValue("StopActionName"))
            {
                StopActionName = currentTemplate.GetValue("StopActionName");
                Events["StopResourceConverter"].guiName = StopActionName;
                Actions["StopResourceConverterAction"].guiName = StopActionName;
            }
            if (currentTemplate.HasValue("ToggleActionName"))
            {
                ToggleActionName = currentTemplate.GetValue("ToggleActionName");
                Actions["ToggleResourceConverterAction"].guiName = ToggleActionName;
            }

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

            }

            //Crew requirements
            if (currentTemplate.HasValue("ExperienceEffect"))
                ExperienceEffect = currentTemplate.GetValue("ExperienceEffect");
            if (currentTemplate.HasValue("minimumCrew"))
                int.TryParse(currentTemplate.GetValue("minimumCrew"), out minimumCrew);

            //Situations
            if(currentTemplate.HasValue("minimumCrew"))
                int.TryParse(currentTemplate.GetValue("minimumCrew"), out minimumCrew);
            if (currentTemplate.HasValue("requiresCommNet"))
                bool.TryParse(currentTemplate.GetValue("requiresCommNet"), out requiresCommNet);
            if (currentTemplate.HasValue("requiresSplashed"))
                bool.TryParse(currentTemplate.GetValue("requiresSplashed"), out requiresSplashed);
            if (currentTemplate.HasValue("requiresSubmerged"))
                bool.TryParse(currentTemplate.GetValue("requiresSubmerged"), out requiresSubmerged);
            if (currentTemplate.HasValue("requiresOrbiting"))
                bool.TryParse(currentTemplate.GetValue("requiresOrbiting"), out requiresOrbiting);

            //Efficiency
            EfficiencyBonus = BaseEfficiency;

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
                    else if (resourceRatio.ResourceName == "Science")
                    {
                        info.AppendLine("<b>Science</b>: " + string.Format("{0:n2} units", resourceRatio.Ratio));
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
                if (templateNode.HasValue("ExperienceEffect"))
                    info.AppendLine("<color=white><b>Bonus Output Skill: </b>" + templateNode.GetValue("ExperienceEffect") + "</color>");
            }

            if (templateNode.HasValue("requiresCommNet") || templateNode.HasValue("requiresSplashed") || 
                templateNode.HasValue("requiresSubmerged") || templateNode.HasValue("requiresOrbiting") || 
                templateNode.HasValue("minimumCrew"))
            {
                info.AppendLine(" ");
                info.AppendLine("<color=white><b>Requirements</b></color>");
                if (templateNode.HasValue("minimumCrew"))
                {
                    info.AppendLine("<b>Minimum crew</b>: " + templateNode.GetValue("minimumCrew"));
                    if (templateNode.HasValue("ExperienceEffect"))
                        info.AppendLine("<b>Required skill: </b>" + templateNode.GetValue("ExperienceEffect"));
                }

                //Situations
                if (templateNode.HasValue("requiresCommNet") || templateNode.HasValue("requiresSplashed") || templateNode.HasValue("requiresSubmerged") || templateNode.HasValue("requiresOrbiting"))
                {
                    if (templateNode.HasValue("requiresCommNet"))
                        info.AppendLine("<b>Requires connection to home world: </b> Yes");
                    else
                        info.AppendLine("<b>Requires connection to home world: </b> Yes");

                    if (templateNode.HasValue("requiresSubmerged"))
                        info.AppendLine("<b>Must be submerged: </b>" + templateNode.GetValue("requiresSubmerged"));

                    else if (templateNode.HasValue("requiresSplashed"))
                        info.AppendLine("<b>Must be splashed: </b>" + templateNode.GetValue("requiresSplashed"));

                    else if (templateNode.HasValue("requiresOrbiting"))
                        info.AppendLine("<b>Must be orbiting: </b>" + templateNode.GetValue("requiresOrbiting"));
                }
            }

            //Set the information view string
            converterInfo = info.ToString();
        }
        #endregion

        #region IOpsView
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Setup Converter", groupName = "Omni", groupDisplayName = "Omni", groupStartCollapsed = true)]
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
                            confirmedReconfigure = false;
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
        public virtual void PerformAnalysis(int yieldNumber)
        {
            //If we have no minimum success then just produce the yield resources.
            if (minimumSuccess <= 0.0f)
            {
                produceYieldResources(1.0, string.Empty);
                return;
            }

            //Ok, go through the analysis.
            float analysisRoll = performAnalysisRoll();

            if (analysisRoll <= criticalFail)
                onCriticalFailure(yieldNumber);

            else if (analysisRoll >= criticalSuccess)
                onCriticalSuccess(yieldNumber);

            else if (analysisRoll >= minimumSuccess)
                onSuccess(yieldNumber);

            else
                onFailure(yieldNumber);

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

        protected virtual void onCriticalFailure(int yieldNumber)
        {
            lastAttempt = attemptCriticalFail;

            StopResourceConverter();

            //Show user message
            string message = string.Format("{0:s} Yield {1:n0}: {2:s}. ", ConverterName, yieldNumber, criticalFailMessage);
            ScreenMessages.PostScreenMessage(message, kMessageDuration);
        }

        protected virtual void onCriticalSuccess(int yieldNumber)
        {
            lastAttempt = attemptCriticalSuccess;
            string message = string.Format("{0:s} Yield {1:n0}: {2:s}. ", ConverterName, yieldNumber, criticalSuccessMessage);
            produceYieldResources(criticalSuccessMultiplier, message);
        }

        protected virtual void onFailure(int yieldNumber)
        {
            lastAttempt = attemptFail;
            string message = string.Format("{0:s} Yield {1:n0}: {2:s}. ", ConverterName, yieldNumber, failMessage);
            produceYieldResources(failureMultiplier, message);
        }

        protected virtual void onSuccess(int yieldNumber)
        {
            lastAttempt = attemptSuccess;
            string message = string.Format("{0:s} Yield {1:n0}: {2:s}. ", ConverterName, yieldNumber, successMessage);
            produceYieldResources(1.0, message);
        }

        protected virtual void produceYieldResources(double yieldMultiplier, string message)
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
            double totalScienceAdded = 0;
            for (int index = 0; index < count; index++)
            {
                yieldAmount = 0;
                resourceRatio = yieldResources[index];

                resourceName = resourceRatio.ResourceName;
                yieldAmount = resourceRatio.Ratio * (1.0 + (highestSkill * SpecialistEfficiencyFactor)) * yieldMultiplier;

                if (resourceName != "Science")
                {
                    this.part.RequestResource(resourceName, -yieldAmount, resourceRatio.FlowMode);
                }
                else if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    totalScienceAdded += yieldAmount;
                    ScreenMessages.PostScreenMessage(string.Format("{0:n2} Science added", yieldAmount), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                    ResearchAndDevelopment.Instance.AddScience((float)yieldAmount, TransactionReasons.ScienceTransmission);
                }
            }

            //Inform player
            if (totalScienceAdded > 0 && !string.IsNullOrEmpty(message))
                ScreenMessages.PostScreenMessage(message + string.Format("{0:n2} Science added", yieldAmount), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);

            else if (!string.IsNullOrEmpty(message))
                ScreenMessages.PostScreenMessage(message, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
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
        protected void GetRequiredResources(float materialModifier)
        {
            if (currentTemplate == null)
                return;
            if (string.IsNullOrEmpty(requiredResource))
                return;

            if (switcher.inputList.ContainsKey(requiredResource))
                switcher.inputList[requiredResource] += requiredAmount * materialModifier;
            else
                switcher.inputList.Add(requiredResource, requiredAmount * materialModifier);
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

            if (switcher != null)
                switcher.buildInputList(switcher.CurrentTemplateName);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
            GameEvents.onPartResourceListChange.Fire(this.part);
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
