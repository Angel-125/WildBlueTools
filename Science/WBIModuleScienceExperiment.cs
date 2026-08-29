using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

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
    public delegate void ExperimentTransferedEvent(WBIModuleScienceExperiment transferedExperiment);
    public delegate void TransferReceivedEvent(WBIModuleScienceExperiment transferRecipient);

    public interface IWBIExperimentResults
    {
        void ExperimentRequirementsMet(string experimentID, float chanceOfSuccess, float resultRoll);
    }

    public struct SExperimentResource
    {
        public string name;
        public double targetAmount;
        public double currentAmount;
        public bool transferFromVessel;
    }

    [KSPModule("Science Experiment")]
    public class WBIModuleScienceExperiment : ModuleScienceExperiment
    {
        #region Fields
        [KSPField]
        public bool debugMode;

        [KSPField(isPersistant = true)]
        public string overrideExperimentID = string.Empty;

        [KSPField]
        public int minCrew;

        [KSPField]
        public int minCrewInPart;

        [KSPField]
        public string celestialBodies = string.Empty;

        [KSPField]
        public bool showRequiredBodies = true;

        [KSPField]
        public string requiredCelestialBodiesMsg = string.Empty;

        [KSPField]
        public double minAltitude;

        [KSPField]
        public double maxAltitude;

        [KSPField]
        public string requiredResources = string.Empty;

        [KSPField]
        public bool checkPartResources;

        [KSPField(isPersistant = true)]
        public string accumulatedResources = string.Empty;

        [KSPField]
        public string situations = string.Empty;

        [KSPField]
        public float partMass;

        [KSPField]
        public string description = string.Empty;

        [KSPField]
        public string defaultExperiment = "WBIEmptyExperiment";

        [KSPField]
        public string techRequired = string.Empty;

        [KSPField]
        public string title = string.Empty;

        [KSPField]
        public float cost;

        [KSPField]
        public float chanceOfSuccess;

        [KSPField]
        public float minimumAsteroidMass;

        [KSPField]
        public bool resultsSafetyCheck;

        [KSPField]
        public string requiredAnomalies = string.Empty;

        [KSPField]
        public bool solarOrbitRequired = false;

        [KSPField]
        public float minAnomalyRange = 50.0f;

        [KSPField(isPersistant = true, guiName = "Status")]
        public string status = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isGUIVisible;

        [KSPField(isPersistant = true)]
        public bool isCompleted;

        [KSPField(isPersistant = true)]
        public bool experimentFailed;

        [KSPField(isPersistant = true)]
        public bool finalTransfer;

        [KSPField(isPersistant = true)]
        public bool notificationSent;

        [KSPField(isPersistant = true)]
        public bool isRunning = true;

        [KSPField()]
        public bool clearResourcesAfterCompleted;

        [KSPField(guiName = "Restart after completion", isPersistant = true)]
        [UI_Toggle(enabledText = "Yes", disabledText = "No")]
        public bool autoRestartExperiment;

        [KSPField]
        public string decalTransform = string.Empty;

        [KSPField]
        public string decalPath = string.Empty;

        [KSPField()]
        public string spaceSituations = string.Empty; //InSpaceHigh;InSpaceLow
        #endregion

        #region Housekeeping
        public event ExperimentTransferedEvent onExperimentTransfered;
        public event TransferReceivedEvent onExperimentReceived;

        public Dictionary<string, SExperimentResource> resourceMap = null;
        public string[] resourceMapKeys;

        protected string[] requiredParts = null;
        protected int currentPartCount;
        protected bool hasRequiredParts;
        protected ConfigNode nodeCompletionHandler = null;
        protected string partsList = string.Empty;
        protected InfoView infoView;
        protected string[] anomaliesRequired = null;
        protected double kscMonolithLong;
        protected double kscMonolithLat;
        #endregion

        #region PAW Events
        [KSPEvent(guiName = "Show Synopsis", guiActiveEditor = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ShowSynopsis()
        {
            //Set info text
            infoView.ModuleInfo = this.GetInfo();

            //Show view
            infoView.SetVisible(true);
        }

        [KSPEvent(guiName = "Start Experiment", guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StartExperiment()
        {
            isRunning = true;
            isCompleted = false;
            Events["StopExperiment"].active = true;
            Events["StartExperiment"].active = false;
            rebuildResourceMap();
        }

        [KSPEvent(guiName = "Stop Experiment", guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StopExperiment()
        {
            isRunning = false;
            status = "Ready to run";
            Events["StopExperiment"].active = false;
            Events["StartExperiment"].active = true;
        }
        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (!string.IsNullOrEmpty(overrideExperimentID))
                LoadFromDefinition(overrideExperimentID);
            else
                LoadFromDefinition(experimentID);

            setupDecal();
        }

        public override void OnSave(ConfigNode node)
        {
            PackResources();

            //Now call the base class.
            base.OnSave(node);

            //For some reason accumulatedResources isn't being updated properly when saved, so we'll do it explicitly.
            if (node.HasValue("accumulatedResources"))
                node.SetValue("accumulatedResources", accumulatedResources);

            if (node.HasValue("overrideExperimentID"))
                node.SetValue("overrideExperimentID", overrideExperimentID);
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
                base.OnStart(state);

            if (!string.IsNullOrEmpty(overrideExperimentID))
                LoadFromDefinition(overrideExperimentID);

            SetGUIVisible(isGUIVisible);
            infoView = new InfoView();

            //Required resources
            rebuildResourceMap();

            //Clear resources if needed
            if (clearResourcesAfterCompleted && isCompleted)
            {
                PartResource[] resources = this.part.Resources.ToArray();
                for (int resourceIndex = 0; resourceIndex < resources.Length; resourceIndex++)
                    resources[resourceIndex].amount = 0f;
            }

            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorPartPlaced.Add(onPartPlaced);
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorPartPlaced.Remove(onPartPlaced);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            updateExperimentState();

            if (!finalTransfer)
                return;

            //Safety check: make sure we deployed the experiment if it has been completed.
            ScienceData[] data = GetData();
            if ((data == null || data.Length == 0) && experimentID != defaultExperiment && !resultsSafetyCheck)
            {
                Debug.Log("[WBIModuleScienceExperiment] - Safety check trigger, running experiment.");
                resultsSafetyCheck = true;
                Deployed = false;
                Inoperable = false;
                DeployExperiment();
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            StringBuilder requirements = new StringBuilder();
           
            info.Append(title + "\r\n\r\n");
            info.Append(description + "\r\n\r\n");

            //Celestial bodies
            if (string.IsNullOrEmpty(celestialBodies) == false && showRequiredBodies)
                requirements.Append("<b>Allowed Planets: </b>" + celestialBodies + "\r\n");
            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
                requirements.Append("<b>Allowed Sitiations: </b>" + situations + "\r\n");
            //Mininum Crew
            if (minCrew > 0)
                requirements.Append("<b>Minimum Crew: </b>" + minCrew + "\r\n");
            //Min Altitude
            if (Math.Abs(minAltitude) > 0.001f)
                requirements.Append(string.Format("<b>Min altitude: </b>{0:f2}m\r\n", minAltitude));
            //Max Altitude
            if (Math.Abs(maxAltitude) > 0.001f)
                requirements.Append(string.Format("<b>Max altitude: </b>{0:f2}m\r\n", maxAltitude));
            //Asteroid Mass
            if (minimumAsteroidMass > 0.001f)
                requirements.Append(string.Format("<b>Asteroid Mass: </b>{0:f2}m\r\n", minimumAsteroidMass));
            //Required parts
            if (string.IsNullOrEmpty(partsList) == false)
            {
                requirements.Append("<b>Parts (needs one): </b>\r\n");
                for (int index = 0; index < requiredParts.Length; index++)
                    requirements.Append(requiredParts[index] + "\r\n");
            }
            //Required resources
            if (string.IsNullOrEmpty(requiredResources) == false)
            {
                if (resourceMap == null)
                    rebuildResourceMap();

                requirements.Append("<b>Resources: </b>\r\n");
                SExperimentResource experimentResource;

                for (int index = 0; index < resourceMapKeys.Length; index ++)
                {
                    experimentResource = resourceMap[resourceMapKeys[index]];
                    requirements.Append(resourceMapKeys[index] + string.Format(" {0:f2}\r\n", experimentResource.targetAmount));
                }
            }
            //Required anomalies
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                requirements.AppendLine("<b>Anomalies: </b>" + requiredAnomalies.Replace(";", ", "));
                requirements.AppendLine(string.Format("<b>Minimum Range: </b>{0:f2}m", minAnomalyRange));
            }
            //Solar orbit
            if (solarOrbitRequired)
                requirements.AppendLine("Requires an orbit around a star");

            string requirementsInfo = requirements.ToString();
            if (string.IsNullOrEmpty(requirementsInfo) == false)
            {
                info.Append("<b>Requirements</b>\r\n\r\n");
                info.Append(requirementsInfo);
            }
            return info.ToString();
        }
        #endregion

        #region API
        public void SetGUIVisible(bool guiVisible)
        {
            isGUIVisible = guiVisible;

            //Base class actions and events are always disabled.
            Events["DeployExperiment"].active = false;
            Events["DeployExperimentExternal"].active = false;
            Actions["DeployAction"].active = false;

            //Our events and actions
            if (isGUIVisible)
            {
                Fields["status"].guiActive = true;
                Fields["autoRestartExperiment"].guiActive = true;
                Fields["autoRestartExperiment"].guiActiveEditor = true;
                Events["ReviewDataEvent"].active = isCompleted;
                Events["TransferDataEvent"].active = isCompleted;
                Events["ShowSynopsis"].active = true;
                if (isRunning)
                {
                    Events["StartExperiment"].active = false;
                    Events["StopExperiment"].active = true;
                }

                else
                {
                    status = "Ready to run";
                    Events["StartExperiment"].active = true;
                    Events["StopExperiment"].active = false;
                }
            }
            else
            {
                Fields["status"].guiActive = false;
                Fields["autoRestartExperiment"].guiActive = false;
                Fields["autoRestartExperiment"].guiActiveEditor = false;
                Events["ReviewDataEvent"].active = false;
                Events["TransferDataEvent"].active = false;
                Events["StartExperiment"].active = false;
                Events["StopExperiment"].active = false;
                Events["ShowSynopsis"].active = false;
            }
        }

        public virtual bool CheckCompletion()
        {
            float resultRoll = 100f;
            int totalCount;
            int index;
            Part testPart;

            if (HighLogic.LoadedSceneIsFlight == false)
                return false;

            //Might already been completed or failed
            if (isCompleted)
            {
                status = "Completed";
                return true;
            }
            else if (experimentFailed)
            {
                status = "Failed to yield results";
                return false;
            }
            else if (isRunning == false)
            {
                status = "Paused";
                return false;
            }

            //Default experiment
            if (experimentID == defaultExperiment)
            {
                isCompleted = false;
                isRunning = false;
                experimentFailed = false;
                finalTransfer = false;
                notificationSent = false;
                return false;
            }

            //Mininum Crew (vessel)
            if (minCrew > 0)
            {
                if (this.part.vessel.GetCrewCount() < minCrew)
                {
                    status = "Needs " + minCrew.ToString() + " crew";
                    return false;
                }
            }

            //Mininum Crew (part)
            if (minCrewInPart > 0)
            {
                if (this.part.protoModuleCrew.Count < minCrewInPart)
                {
                    status = "Needs " + minCrewInPart.ToString() + " crew";
                    return false;
                }
            }

            //Celestial bodies
            if (string.IsNullOrEmpty(celestialBodies) == false)
            {
                if (celestialBodies.Contains(this.part.vessel.mainBody.displayName) == false)
                {
                    if (showRequiredBodies)
                        status = "Needs one: " + celestialBodies;
                    else if (!string.IsNullOrEmpty(requiredCelestialBodiesMsg))
                        status = Localizer.Format(requiredCelestialBodiesMsg);
                    else
                        status = "Failed to yield results";
                    return false;
                }
            }

            //Solar orbit
            if (solarOrbitRequired)
            {
                if (this.part.vessel.mainBody.scaledBody.GetComponentsInChildren<SunShaderController>(true).Length == 0)
                    return false;
            }

            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
            {
                string situation = this.part.vessel.situation.ToString().ToUpper();
                string experimentSituations = this.situations.ToUpper();
                if (experimentSituations.Contains(situation) == false)
                {
                    status = "Needs one: " + situations;
                    return false;
                }
            }

            //Space situations
            if (string.IsNullOrEmpty(spaceSituations) == false)
            {
                string expSit = ScienceUtil.GetExperimentSituation(this.part.vessel).ToString().ToUpper();
                if (spaceSituations.Contains(expSit) == false)
                {
                    if (spaceSituations.Contains("INSPACELOW"))
                        status = "Needs In Space Low";
                    else if (spaceSituations.Contains("INSPACEHIGH"))
                        status = "Needs In Space High";
                    return false;
                }
            }

            //Min altitude
            if (minAltitude > 0.001f)
            {
                if (this.part.vessel.altitude <= minAltitude)
                {
                    status = string.Format("Needs min altitude: {0:f2}m", minAltitude);
                    return false;
                }
            }

            //Max altitude
            if (maxAltitude > 0.001f)
            {
                if (this.part.vessel.altitude >= maxAltitude)
                {
                    status = string.Format("Max acceptable altitude: {0:f2}m", maxAltitude);
                    return false;
                }
            }

            //Asteroid Mass
            if (minimumAsteroidMass > 0.001f)
            {
                List<ModuleAsteroid> asteroidList = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();
                ModuleAsteroid[] asteroids = asteroidList.ToArray();
                ModuleAsteroid asteroid;
                float largestAsteroidMass = 0f;

                //No asteroids? That's a problem!
                if (asteroidList.Count == 0)
                {
                    status = string.Format("Needs Asteroid of {0:f2}mt", minimumAsteroidMass);
                    return false;
                }

                //Find the most massive asteroid
                for (index = 0; index < asteroids.Length; index++)
                {
                    asteroid = asteroids[index];
                    if (asteroid.part.mass > largestAsteroidMass)
                        largestAsteroidMass = asteroid.part.mass;
                }

                //Make sure we have an asteroid of sufficient mass.
                if (largestAsteroidMass < minimumAsteroidMass)
                {
                    status = string.Format("Needs Asteroid of {0:f2}mt", minimumAsteroidMass);
                    return false;
                }
            }

            //Required parts
            if (string.IsNullOrEmpty(partsList) == false)
            {
                int partCount = this.part.vessel.Parts.Count;
                if (currentPartCount != partCount)
                {
                    currentPartCount = partCount;
                    hasRequiredParts = false;
                    totalCount = this.part.vessel.parts.Count;
                    for (index = 0; index < totalCount; index++)
                    {
                        testPart = this.part.vessel.parts[index];
                        if (partsList.Contains(testPart.partInfo.title))
                        {
                            hasRequiredParts = true;
                            break;
                        }
                    }
                }

                if (hasRequiredParts == false)
                {
                    StringBuilder requirements = new StringBuilder();
                    requirements.Append("Needs one: ");
                    for (index = 0; index < requiredParts.Length; index++)
                        requirements.Append(requiredParts[index] + "\r\n");
                    status = requirements.ToString();
                    return false;
                }
            }

            //Required anomalies
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                if (!isNearRequiredAnomaly())
                {
                    status = "Needs to be near " + requiredAnomalies.Replace(";", ", ");
                    return false;
                }
            }

            //Required resources
            if (string.IsNullOrEmpty(requiredResources) == false)
            {
                //for each resource, see if we still need more
                totalCount = resourceMapKeys.Length;
                SExperimentResource experimentResource;
                PartResource partResource = null;
                for (index = 0; index < totalCount; index++)
                {
                    experimentResource = resourceMap[resourceMapKeys[index]];

                    if (!checkPartResources)
                    {
                        //If necessary, pull the resource from the vessel instead of waiting for the resource
                        //to come to the lab.
                        if (experimentResource.transferFromVessel && (experimentResource.currentAmount / experimentResource.targetAmount) < 0.999f)
                        {
                            double amountObtained = this.part.RequestResource(experimentResource.name,
                                experimentResource.targetAmount - experimentResource.currentAmount, ResourceFlowMode.ALL_VESSEL);
                            TakeShare(experimentResource.name, amountObtained);
                        }

                        //Check to see if we're done.
                        if ((experimentResource.currentAmount / experimentResource.targetAmount) < 0.999f)
                        {
                            status = "Needs " + experimentResource.name + string.Format(" ({0:f3}/{1:f3})", experimentResource.currentAmount, experimentResource.targetAmount);
                            return false;
                        }
                    }

                    else //Check the part for the required resources
                    {
//                        Debug.Log("[WBIModuleScienceExperiment] - Checking part for " + experimentResource.name);
                        if (this.part.Resources.Contains(experimentResource.name))
                        {
                            partResource = this.part.Resources[experimentResource.name];
                            if ((partResource.amount / experimentResource.targetAmount) < 0.999f)
                            {
                                status = "Needs " + experimentResource.name + string.Format(" ({0:f3}/{1:f3})", partResource.amount, experimentResource.targetAmount);
                                return false;
                            }
                        }
                    }
                }
            }

            //chanceOfSuccess
            if (chanceOfSuccess > 0.001f && isCompleted == false && experimentFailed == false)
            {
                resultRoll = performAnalysisRoll();
                if (resultRoll < chanceOfSuccess)
                {
                    experimentFailed = true;
                    status = "Failed to yield results";
                    runCompletionHandler(experimentID, chanceOfSuccess, resultRoll);
                    sendResultsMessage();
                    return false;
                }
            }

            //AOK
            isCompleted = true;
            if (xmitDataScalar < 0.001f)
                status = "Completed, send home for science";
            else
                status = "Completed";
            runCompletionHandler(experimentID, chanceOfSuccess, resultRoll);
            sendResultsMessage();

            if (checkPartResources)
            {
                totalCount = resourceMapKeys.Length;
                SExperimentResource experimentResource;
                for (index = 0; index < totalCount; index++)
                {
                    experimentResource = resourceMap[resourceMapKeys[index]];
                    if (this.part.Resources.Contains(experimentResource.name))
                    {
                        if (this.rerunnable)
                            this.part.Resources[experimentResource.name].amount = 0;
                        else
                            this.part.Resources.Remove(experimentResource.name);
                    }
                }

                //Dirty the GUI
                MonoUtilities.RefreshContextWindows(this.part);
            }

            if (Deployed == false)
                DeployExperiment();

            return true;
        }

        public double TakeShare(string resourceName, double shareAmount)
        {
            SExperimentResource experimentResource;
            double remainder = 0f;

            if (resourceMap.ContainsKey(resourceName))
            {
                experimentResource = resourceMap[resourceName];
                experimentResource.currentAmount += shareAmount;
                if (experimentResource.currentAmount > experimentResource.targetAmount)
                {
                    remainder = experimentResource.currentAmount - experimentResource.targetAmount;
                    experimentResource.currentAmount = experimentResource.targetAmount;
                }
                resourceMap[resourceName] = experimentResource;
            }

            return remainder;
        }

        public List<SExperimentResource> GetRequiredResources()
        {
            List<SExperimentResource> requiredResources = new List<SExperimentResource>();
            SExperimentResource experimentResource;

            if (experimentID == defaultExperiment)
                return requiredResources;
            if (isCompleted)
                return requiredResources;
            if (isRunning == false)
                return requiredResources;

            for (int index = 0; index < resourceMapKeys.Length; index++)
            {
                experimentResource = resourceMap[resourceMapKeys[index]];

                if (experimentResource.currentAmount <= experimentResource.targetAmount)
                    requiredResources.Add(experimentResource);
            }

            return requiredResources;
        }

        public void TransferExperiment(WBIModuleScienceExperiment sourceExperiment)
        {
            //Load parameters from experiment definition
            LoadFromDefinition(sourceExperiment.experimentID);

            //Set state variables
            this.isCompleted = sourceExperiment.isCompleted;
            this.isRunning = sourceExperiment.isRunning;
            this.experimentFailed = sourceExperiment.experimentFailed;
            this.status = sourceExperiment.status;
            this.notificationSent = sourceExperiment.notificationSent;
            this.accumulatedResources = sourceExperiment.accumulatedResources;

            //Rebuild resource map
            rebuildResourceMap();

            //Let listeners know
            if (onExperimentReceived != null)
                onExperimentReceived(this);
            if (sourceExperiment.onExperimentTransfered != null)
                sourceExperiment.onExperimentTransfered(sourceExperiment);

            //Now set the source experiment to a dummy experiment
            sourceExperiment.LoadFromDefinition(defaultExperiment);
            sourceExperiment.CheckCompletion();

            //Do a quick check for completion
            CheckCompletion();

            //If the experiment is completed then be sure to transfer its ScienceData.
            if (isCompleted)
            {
                finalTransfer = true;
                ScienceData[] scienceData = sourceExperiment.GetData();
                for (int index = 0; index < scienceData.Length; index++)
                    ReturnData(scienceData[index]);
                sourceExperiment.ResetExperiment();
            }
        }

        public void LoadFromDefinition(string experimentIDCode, bool sendTransferEvent = false)
        {
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");
            ConfigNode nodeDefinition = null;
            ConfigNode nodeExperiment;
            int index;

            //Find our desired experiment
            for (index = 0; index < experiments.Length; index++)
            {
                nodeExperiment = experiments[index];
                if (nodeExperiment.HasValue("id"))
                {
                    if (nodeExperiment.GetValue("id") == experimentIDCode)
                    {
                        nodeDefinition = nodeExperiment;
                        break;
                    }
                }
            }
            if (nodeDefinition == null)
            {
                Debug.Log("loadFromDefinition - unable to find the experiment definition for " + experimentIDCode);
                return;
            }

            //Now load the parameters
            experimentID = experimentIDCode;
            overrideExperimentID = experimentID;
            experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            status = "Ready";

            //Set defaults
            if (experimentID == defaultExperiment)
            {
                isCompleted = false;
                isRunning = false;
                experimentFailed = false;
                finalTransfer = false;
                notificationSent = false;
                status = "";
                accumulatedResources = string.Empty;
            }

            if (nodeDefinition.HasValue("experimentActionName"))
                experimentActionName = nodeDefinition.GetValue("experimentActionName");
            else
                experimentActionName = "Get Results";
            Events["DeployExperiment"].guiName = experimentActionName;
            Events["DeployExperimentExternal"].guiName = experimentActionName;

            if (nodeDefinition.HasValue("resetActionName"))
                resetActionName = nodeDefinition.GetValue("resetActionName");
            else
                resetActionName = "Reset Experiment";

            if (nodeDefinition.HasValue("reviewActionName"))
                reviewActionName = nodeDefinition.GetValue("reviewActionName");
            else
                reviewActionName = "Review Results";

            if (nodeDefinition.HasValue("collectActionName"))
                collectActionName = nodeDefinition.GetValue("collectActionName");
            else
                collectActionName = "Collect Data";

            //Title
            if (nodeDefinition.HasValue("title"))
                title = nodeDefinition.GetValue("title");

            //requiredParts
            if (nodeDefinition.HasValue("requiredPart"))
            {
                requiredParts = nodeDefinition.GetValues("requiredPart");
                StringBuilder builder = new StringBuilder();
                for (index = 0; index < requiredParts.Length; index++)
                {
                    builder.Append(requiredParts[index] + ";");
                }
                partsList = builder.ToString();
            }

            //minCrew
            if (nodeDefinition.HasValue("minCrew"))
                minCrew = int.Parse(nodeDefinition.GetValue("minCrew"));

            //minCrewInPart
            if (nodeDefinition.HasValue("minCrewInPart"))
                minCrewInPart = int.Parse(nodeDefinition.GetValue("minCrewInPart"));

            //celestialBodies
            if (nodeDefinition.HasValue("celestialBodies"))
                celestialBodies = nodeDefinition.GetValue("celestialBodies");

            //Solar orbit
            if (nodeDefinition.HasValue("solarOrbitRequired"))
                bool.TryParse(nodeDefinition.GetValue("solarOrbitRequired"), out solarOrbitRequired);

            //minAltitude
            if (nodeDefinition.HasValue("minAltitude"))
                minAltitude = double.Parse(nodeDefinition.GetValue("minAltitude"));

            //maxAltitude
            if (nodeDefinition.HasValue("maxAltitude"))
                maxAltitude = double.Parse(nodeDefinition.GetValue("maxAltitude"));

            //requiredResources
            if (nodeDefinition.HasValue("requiredResources"))
                requiredResources = nodeDefinition.GetValue("requiredResources");

            //situations
            if (nodeDefinition.HasValue("situations"))
                situations = nodeDefinition.GetValue("situations");

            //Space situations
            if (nodeDefinition.HasValue("spaceSituations"))
                spaceSituations = nodeDefinition.GetValue("spaceSituations");

            //mass
            if (nodeDefinition.HasValue("mass"))
                partMass = float.Parse(nodeDefinition.GetValue("mass"));

            //description
            if (nodeDefinition.HasValue("description"))
                description = nodeDefinition.GetValue("description");

            //Required tech
            if (nodeDefinition.HasValue("techRequired"))
                techRequired = nodeDefinition.GetValue("techRequired");

            //Cost
            if (nodeDefinition.HasValue("cost"))
                cost = float.Parse(nodeDefinition.GetValue("cost"));

            //Anomalies
            if (nodeDefinition.HasValue("requiredAnomalies"))
                requiredAnomalies = nodeDefinition.GetValue("requiredAnomalies");
            if (nodeDefinition.HasValue("minAnomalyRange"))
                minAnomalyRange = float.Parse(nodeDefinition.GetValue("minAnomalyRange"));
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                anomaliesRequired = requiredAnomalies.Split(new char[] { ';' });
                if (debugMode)
                {
                    if (anomaliesRequired != null)
                        Debug.Log("[WBIModuleScienceExperiment] - anomaliesRequired length: " + anomaliesRequired.Length);
                    else
                        Debug.Log("[WBIModuleScienceExperiment] - Can't split the anomaliesRequired for  " + requiredAnomalies);
                }

                // KSC anomaly
                ConfigNode[] kscMonolithNodes = GameDatabase.Instance.GetConfigNodes("KSCMONILITH");
                if (kscMonolithNodes.Length > 0)
                {
                    double.TryParse(kscMonolithNodes[0].GetValue("longitude"), out kscMonolithLong);
                    double.TryParse(kscMonolithNodes[0].GetValue("latitude"), out kscMonolithLat);
                }
            }

            //changeOfSuccess
            if (nodeDefinition.HasValue("chanceOfSuccess"))
                chanceOfSuccess = float.Parse(nodeDefinition.GetValue("chanceOfSuccess"));

            //nodeCompletionHandler
            if (nodeDefinition.HasNode("MODULE"))
                nodeCompletionHandler = nodeDefinition.GetNode("MODULE");

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);

            //Fire event
            if (onExperimentReceived != null && sendTransferEvent)
                onExperimentReceived(this);
        }

        public void PackResources()
        {
            if (string.IsNullOrEmpty(requiredResources))
                return;
            if (resourceMap == null)
                return;
            if (experimentID == defaultExperiment)
                return;

            StringBuilder builder = new StringBuilder();

            //Pack up the accumulated resources
            SExperimentResource experimentResource;
            for (int index = 0; index < resourceMapKeys.Length; index++)
            {
                experimentResource = resourceMap[resourceMapKeys[index]];
                builder.Append(resourceMapKeys[index] + "," + experimentResource.currentAmount.ToString() + "," + experimentResource.transferFromVessel + ";");
            }
            accumulatedResources = builder.ToString();
            accumulatedResources = accumulatedResources.Substring(0, accumulatedResources.Length - 1);
        }
        #endregion

        #region Helpers
        protected virtual void updateExperimentState()
        {
            if (isGUIVisible)
            {
                //Check auto restart field.
                if (!rerunnable && Fields["autoRestartExperiment"].guiActive && HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                {
                    Fields["autoRestartExperiment"].guiActive = false;
                    Fields["autoRestartExperiment"].guiActiveEditor = false;
                }
            }

            //If we're not in flight, then we're done.
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Base class actions and events are always disabled.
            Events["DeployExperiment"].active = false;
            Events["DeployExperimentExternal"].active = false;
            Actions["DeployAction"].active = false;

            //If the experiment isn't running then we're done.
            if (!isRunning && isGUIVisible)
            {
                //Check for reset
                if (isCompleted && !rerunnable && (resettable || resettableOnEVA) && !Events["ResetExperiment"].active && !Events["ResetExperimentExternal"].active && !Events["StartExperiment"].active)
                {
                    Events["StartExperiment"].active = true;
                }
                return;
            }

            //If the experiment has been completed then stop the experiment from running.
            if (isCompleted)
            {
                StopExperiment();
                if (isGUIVisible)
                {
                    Events["ReviewDataEvent"].active = true;
                    Events["TransferDataEvent"].active = true;
                    if (!rerunnable)
                        Events["StartExperiment"].active = false;
                }
                status = "Completed";

                if (autoRestartExperiment)
                {
                    ResetExperiment();
                    StartExperiment();
                }
                else
                {
                    ModuleResourceConverter converter = this.part.FindModuleImplementing<ModuleResourceConverter>();
                    if (converter != null && converter.IsActivated)
                        converter.StopResourceConverter();
                }
                return;
            }

            //Check for completion. We do this if the GUI is visible (we're not being managed by an experiment lab)
            if (isGUIVisible)
                CheckCompletion();
        }

        protected void rebuildResourceMap()
        {
            SExperimentResource experimentResource;
            if (string.IsNullOrEmpty(requiredResources))
                return;

            //Build resource map
            string[] resources = requiredResources.Split(new char[] { ';' });
            string[] resourceAmount = null;
            int index;
            string resource;

            resourceMap = new Dictionary<string, SExperimentResource>();
            for (index = 0; index < resources.Length; index++)
            {
                resource = resources[index];

                resourceAmount = resource.Split(new char[] { ',' });
                experimentResource = new SExperimentResource();
                // Default to allowing resource transfer from anywhere in the vessel.
                experimentResource.transferFromVessel = true; 
                experimentResource.name = resourceAmount[0];
                experimentResource.targetAmount = double.Parse(resourceAmount[1]);
                if (resourceAmount.Length > 2)
                {
                    experimentResource.transferFromVessel = bool.Parse(resourceAmount[2]);
                }
                resourceMap.Add(resourceAmount[0], experimentResource);
            }

            //Save the keys
            resourceMapKeys = resourceMap.Keys.ToArray<string>();

            //Unpack accumulated resources (if any)
            if (string.IsNullOrEmpty(accumulatedResources) == false)
            {
                resources = accumulatedResources.Split(new char[] { ';' });
                for (index = 0; index < resources.Length; index++)
                {
                    resource = resources[index];

                    resourceAmount = resource.Split(new char[] { ',' });
                    if (resourceMap.ContainsKey(resourceAmount[0]))
                    {
                        experimentResource = resourceMap[resourceAmount[0]];
                        experimentResource.currentAmount = double.Parse(resourceAmount[1]);
                        if (resourceAmount.Length > 2)
                        {
                            experimentResource.transferFromVessel = bool.Parse(resourceAmount[2]);
                        }
                        resourceMap[resourceAmount[0]] = experimentResource;
                    }
                }
            }
        }

        protected void onPartPlaced(Part partPlaced)
        {
            if (partPlaced == this.part)
            {
                //Set the tooltip.
                if (ToolTipScenario.Instance.HasDisplayedToolTip(this.part.partInfo.name))
                    return;
                ToolTipScenario.Instance.AddToolTipDisplayedFlag(this.part.partInfo.name);

                LoadFromDefinition(experimentID);
                StringBuilder requirements = new StringBuilder();
                string requirementsText = string.Empty;

                //Mininum Crew (vessel)
                if (minCrew > 0)
                    requirements.Append("<b>This vessel requires a minimum of </b>" + minCrew + " <b>crew.</b>\r\n");

                //Mininum Crew (part)
                if (minCrewInPart > 0)
                    requirements.Append("<b>This part requires a minimum of </b>" + minCrewInPart + " <b>crew.</b>\r\n");

                //Required parts
                if (requiredParts != null && requiredParts.Length > 0)
                {
                    //If our part is on the list, then we're done.
                    if (requiredParts.Contains(this.part.partInfo.title))
                        return;

                    //Build the list of required parts.
                    requirements.Append("<b>This part also requires at least one of: </b>\r\n");
                    for (int index = 0; index < requiredParts.Length; index++)
                        requirements.Append(requiredParts[index] + "\r\n");
                }

                requirementsText = requirements.ToString();
                if (string.IsNullOrEmpty(requirementsText) == false)
                {
                    infoView.ModuleInfo = requirementsText;
                    infoView.SetVisible(true);
                }
            }
        }

        protected void setupDecal()
        {
            //Setup decal
            if (string.IsNullOrEmpty(decalPath) == false && string.IsNullOrEmpty(decalTransform) == false)
            {
                Transform decal = this.part.FindModelTransform(decalTransform);
                if (decal == null)
                    return;

                Renderer renderer = decal.GetComponent<Renderer>();
                if (renderer == null)
                    return;

                Texture decalTexture = GameDatabase.Instance.GetTexture(decalPath, false);
                if (decalTexture == null)
                    return;
                renderer.material.SetTexture("_MainTex", decalTexture);
            }
        }

        protected void sendResultsMessage()
        {
            if (notificationSent)
                return;
            notificationSent = true;

            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;

            resultsMessage.AppendLine("Results from: " + this.part.vessel.vesselName);
            resultsMessage.AppendLine("Lab: " + this.part.partInfo.title);
            resultsMessage.AppendLine("Experiment: " + title);

            if (isCompleted)
            {
                resultsMessage.AppendLine("Conclusion: Success!");
                resultsMessage.AppendLine("Summary: We were able to produce usable results for this experiment. Bring it home to gain the science benefits and recover the economic cost, or send the data to an MPL for additional processing.");
            }
            else
            {
                resultsMessage.AppendLine("Conclusion: Failure!");
                resultsMessage.AppendLine("Summary: Unfortunately we were not able to produce viable results from this experiment. It can still be returned to recover the economic cost though.");
            }

            if (isCompleted)
                msg = new MessageSystem.Message("Experiment Results: Success!", resultsMessage.ToString(),
                    MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.COMPLETE);
            else
                msg = new MessageSystem.Message("Experiment Results: Failed!", resultsMessage.ToString(),
                    MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.FAIL);
            MessageSystem.Instance.AddMessage(msg);
        }

        protected void runCompletionHandler(string experimentID, float chanceOfSuccess, float resultRoll)
        {
            if (nodeCompletionHandler == null)
                return;

            try
            {
                PartModule moduleHandler = this.part.AddModule(nodeCompletionHandler.GetValue("name"));
                moduleHandler.Load(nodeCompletionHandler);

                if (moduleHandler is IWBIExperimentResults)
                {
                    IWBIExperimentResults resultsHandler = (IWBIExperimentResults)moduleHandler;
                    resultsHandler.ExperimentRequirementsMet(experimentID, chanceOfSuccess, resultRoll);
                }

                this.part.RemoveModule(moduleHandler);
            }
            catch (Exception ex)
            {
                Debug.Log("[WBIModuleScienceExperiment] error while trying to run experiment completion handler: " + ex.ToString());
            }
        }

        protected bool isNearRequiredAnomaly()
        {
            if (anomaliesRequired == null)
            {
                if (debugMode)
                    Debug.Log("[WBIModueScienceExperiment] - anomaliesRequired is null");
                return false;
            }

            string nearestAnomalyName = getNearestAnomalyName();
            if (string.IsNullOrEmpty(nearestAnomalyName))
                return false;
            if (debugMode)
                Debug.Log("[WBIModueScienceExperiment] - Nearest anomaly is " + nearestAnomalyName);

            string requiredAnomalyName;
            for (int index = 0; index < anomaliesRequired.Length; index++)
            {
                requiredAnomalyName = anomaliesRequired[index];
                if (debugMode)
                    Debug.Log("[WBIModueScienceExperiment] - trying to match " + requiredAnomalyName + "with " + nearestAnomalyName);

                if (nearestAnomalyName.Contains(requiredAnomalyName))
                {
                    if (debugMode)
                        Debug.Log("[WBIModueScienceExperiment] - " + requiredAnomalyName + " matches " + nearestAnomalyName);
                    return true;
                }
            }

            if (debugMode)
                Debug.Log("[WBIModueScienceExperiment] - No required anomalies match " + nearestAnomalyName);

            //Not near anomaly or not close enough
            return false;
        }

        string getNearestAnomalyName()
        {
            CelestialBody mainBody = this.part.vessel.mainBody;
            PQSSurfaceObject[] anomalies = mainBody.pqsSurfaceObjects;
            double longitude;
            double latitude;
            double distance;

            if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH || this.part.vessel.situation == Vessel.Situations.SPLASHED)
            {
                for (int index = 0; index < anomalies.Length; index++)
                {
                    //Get the longitude and latitude of the anomaly
                    longitude = mainBody.GetLongitude(anomalies[index].transform.position);
                    latitude = mainBody.GetLatitude(anomalies[index].transform.position);

                    //Get the distance (in meters) from the anomaly.
                    distance = Utils.HaversineDistance(longitude, latitude,
                        this.part.vessel.longitude, this.part.vessel.latitude, this.part.vessel.mainBody) * 1000;

                    //If we're near the anomaly, then we're done
                    if (debugMode)
                        Debug.Log("[WBIModueScienceExperiment] - distance to " + anomalies[index].SurfaceObjectName + ": " + distance);
                    if (distance <= minAnomalyRange)
                        return anomalies[index].SurfaceObjectName;
                }

                // Special case: The KSC Monolith
                longitude = kscMonolithLong;
                latitude = kscMonolithLat;
                distance = Utils.HaversineDistance(longitude, latitude,
                    this.part.vessel.longitude, this.part.vessel.latitude, this.part.vessel.mainBody) * 1000;
                if (distance <= minAnomalyRange)
                    return "KSCMonolith";
            }

            //Not near anomaly or not close enough
            Debug.Log("[WBIModueScienceExperiment] - Not near an anomaly");
            return "";
        }

        protected virtual float performAnalysisRoll()
        {
            float roll = 0.0f;

            //Roll 3d6 to approximate a bell curve, then convert it to a value between 1 and 100.
            roll = UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll *= 5.5556f;

            //Factor in crew
            //roll += totalCrewSkill;

            //Done
            return roll;
        }

        public void ClearExperiment()
        {
            if (onExperimentTransfered != null)
                onExperimentTransfered(this);

            LoadFromDefinition(defaultExperiment);
            rebuildResourceMap();
        }
        #endregion
    }
}
