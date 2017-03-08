using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

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
    }

    [KSPModule("Science Experiment")]
    public class WBIModuleScienceExperiment : ModuleScienceExperiment
    {
        [KSPField(isPersistant = true)]
        public string overrideExperimentID = string.Empty;

        [KSPField]
        public int minCrew;

        [KSPField]
        public string celestialBodies = string.Empty;

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

        public event ExperimentTransferedEvent onExperimentTransfered;
        public event TransferReceivedEvent onExperimentReceived;

        public Dictionary<string, SExperimentResource> resourceMap = null;
        public string[] resourceMapKeys;

        protected string[] requiredParts = null;
        protected int currentPartCount;
        protected bool hasRequiredParts;
        protected ConfigNode nodeCompletionHandler = null;
        protected string partsList = string.Empty;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!string.IsNullOrEmpty(overrideExperimentID))
                LoadFromDefinition(overrideExperimentID);
        }

        public override void OnSave(ConfigNode node)
        {
            PackResources();

            //Now call the base class.
            base.OnSave(node);
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
                base.OnStart(state);
            SetGUIVisible(isGUIVisible);

            //Required resources
            rebuildResourceMap();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

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
            if (string.IsNullOrEmpty(celestialBodies) == false)
                requirements.Append("<b>Allowed Planets: </b>" + celestialBodies + "\r\n");
            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
                requirements.Append("<b>Allowed Sitiations: </b>" + situations + "\r\n");
            //Mininum Crew
            if (minCrew > 0)
                requirements.Append("<b>Minimum Crew: </b>" + minCrew + "\r\n");
            //Min Altitude
            if (minAltitude > 0.001f)
                requirements.Append(string.Format("<b>Min altitude: </b>{0:f2}m\r\n", minAltitude));
            //Max Altitude
            if (maxAltitude > 0.001f)
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

            string requirementsInfo = requirements.ToString();
            if (string.IsNullOrEmpty(requirementsInfo) == false)
            {
                info.Append("<b>Requirements</b>\r\n\r\n");
                info.Append(requirementsInfo);
            }
            return info.ToString();
        }

        public void SetGUIVisible(bool guiVisible)
        {
            isGUIVisible = guiVisible;

            //Base class actions and events are always disabled.
            Events["DeployExperiment"].active = false;
            Events["DeployExperimentExternal"].active = false;
            Actions["DeployAction"].active = false;

            //Our events and actions
        }

        public bool CheckCompletion()
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

            //Mininum Crew
            if (minCrew > 0)
            {
                if (this.part.vessel.GetCrewCount() < minCrew)
                {
                    status = "Needs " + minCrew.ToString() + " crew";
                    return false;
                }
            }

            //Celestial bodies
            if (string.IsNullOrEmpty(celestialBodies) == false)
            {
                if (celestialBodies.Contains(this.part.vessel.mainBody.name) == false)
                {
                    status =  "Needs one: " + celestialBodies;
                    return false;
                }
            }

            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
            {
                string situation = this.part.vessel.situation.ToString();
                if (situations.Contains(situation) == false)
                {
                    status = "Needs one: " + situations;
                    return false;
                }
            }

            //Min altitude
            if (minAltitude > 0.001f)
            {
                if (this.part.vessel.altitude < minAltitude)
                {
                    status = string.Format("Needs min altitude: {0:f2}m", minAltitude);
                    return false;
                }
            }

            //Max altitude
            if (maxAltitude > 0.001f)
            {
                if (this.part.vessel.altitude > maxAltitude)
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

            //celestialBodies
            if (nodeDefinition.HasValue("celestialBodies"))
                celestialBodies = nodeDefinition.GetValue("celestialBodies");

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
                builder.Append(resourceMapKeys[index] + "," + experimentResource.currentAmount.ToString() + ";");
            }
            accumulatedResources = builder.ToString();
            accumulatedResources = accumulatedResources.Substring(0, accumulatedResources.Length - 1);
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
                experimentResource.name = resourceAmount[0];
                experimentResource.targetAmount = double.Parse(resourceAmount[1]);
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
                        resourceMap[resourceAmount[0]] = experimentResource;
                    }
                }
            }
        }
    }
}
