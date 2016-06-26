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
        void ExperimentRequirementsMet(float resultRoll);
    }

    [KSPModule("Science Experiment")]
    public class WBIModuleScienceExperiment : ModuleScienceExperiment
    {
        [KSPField(isPersistant = true)]
        public string overrideExperimentID = string.Empty;

        [KSPField]
        public string requiredParts = string.Empty;

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

        [KSPField(isPersistant = true)]
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

        public event ExperimentTransferedEvent onExperimentTransfered;
        public event TransferReceivedEvent onExperimentReceived;

        public Dictionary<string, double> resourceMap = null;

        protected int currentPartCount;
        protected bool hasRequiredParts;
        protected ConfigNode nodeCompletionHandler = null;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!string.IsNullOrEmpty(overrideExperimentID))
                LoadFromDefinition(overrideExperimentID);
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
                base.OnStart(state);
            SetGUIVisible(isGUIVisible);

            //Required resources
            rebuildResourceMap();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            if (HighLogic.LoadedSceneIsEditor)
            {
                return "";
            }
            
            info.Append(title + "\r\n\r\n");
            info.Append(description + "\r\n\r\n");
            info.Append("<b>Requirements</b>\r\n\r\n");

            //Celestial bodies
            if (string.IsNullOrEmpty(celestialBodies) == false)
                info.Append("<b>Allowed Planets: </b>" + celestialBodies + "\r\n");
            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
                info.Append("<b>Allowed Sitiations: </b>" + situations + "\r\n");
            //Mininum Crew
            if (minCrew > 0)
                info.Append("<b>Minimum Crew: </b>" + minCrew + "\r\n");
            //Min Altitude
            if (minAltitude > 0.001f)
                info.Append(string.Format("<b>Min altitude: </b>{0:f2}m\r\n", minAltitude));
            //Max Altitude
            if (maxAltitude > 0.001f)
                info.Append(string.Format("<b>Max altitude: </b>{0:f2}m\r\n", maxAltitude));
            //Required parts
            if (string.IsNullOrEmpty(requiredParts) == false)
                info.Append("<b>Parts: </b>" + requiredParts + "\r\n");
            //Required resources
            if (string.IsNullOrEmpty(requiredResources) == false)
            {
                info.Append("<b>Resources: </b>\r\n");

                foreach (string resourceName in resourceMap.Keys)
                {
                    info.Append(resourceName + string.Format(" ({0:f2})\r\n", resourceMap[resourceName]));
                }
            }
            return info.ToString();
        }

        public void SetGUIVisible(bool guiVisible)
        {
            isGUIVisible = guiVisible;
            Events["DeployExperiment"].guiActive = guiVisible;
            Events["DeployExperimentExternal"].guiActiveUnfocused = guiVisible;
        }

        public bool CheckCompletion()
        {
            float resultRoll = 100f;
            bool partHasRequiredResource = false;

            if (HighLogic.LoadedSceneIsFlight == false)
                return false;

            //Might already been completed or failed
            if (isCompleted)
                return true;
            else if (experimentFailed)
                return false;

            //Default experiment
            if (experimentID == defaultExperiment)
                return false;

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

            //Required parts
            if (string.IsNullOrEmpty(requiredParts) == false)
            {
                int partCount = this.part.vessel.Parts.Count;
                if (currentPartCount != partCount)
                {
                    currentPartCount = partCount;
                    hasRequiredParts = false;
                    foreach (Part vesselPart in this.part.vessel.Parts)
                    {
                        if (requiredParts.Contains(vesselPart.partInfo.title))
                        {
                            hasRequiredParts = true;
                            break;
                        }
                    }
                    if (hasRequiredParts == false)
                    {
                        status = "Needs " + requiredParts;
                        return false;
                    }
                }

                else if (hasRequiredParts == false)
                {
                    status = "Needs " + requiredParts;
                    return false;
                }
            }

            //Required resources
            if (string.IsNullOrEmpty(requiredResources) == false)
            {
                //for each resource, see if we still need more
                foreach (PartResource resource in this.part.Resources)
                {
                    if (resourceMap.ContainsKey(resource.resourceName))
                    {
                        partHasRequiredResource = true;
                        if (resource.amount < resource.maxAmount)
                        {
                            status = "Needs more " + resource.resourceName;
                            return false;
                        }
                    }
                }
            }

            //If the part has none of the required resources then we're done.
            if (partHasRequiredResource == false)
                return false;

            //chanceOfSuccess
            if (chanceOfSuccess > 0.001f && isCompleted == false && experimentFailed == false)
            {
                resultRoll = performAnalysisRoll();
                if (resultRoll < chanceOfSuccess)
                {
                    experimentFailed = true;
                    status = "Failed to yield results";
                    runCompletionHandler(resultRoll);
                    sendResultsMessage();
                    return false;
                }
            }

            //AOK
            isCompleted = true;
            status = "Completed";
            runCompletionHandler(resultRoll);
            sendResultsMessage();
            return true;
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

        protected void runCompletionHandler(float resultRoll)
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
                    resultsHandler.ExperimentRequirementsMet(resultRoll);
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
        }

        public void TransferExperiment(WBIModuleScienceExperiment sourceExperiment)
        {
            //Load parameters from experiment definition
            LoadFromDefinition(sourceExperiment.experimentID);

            //Let listeners know
            if (onExperimentReceived != null)
                onExperimentReceived(this);
            if (sourceExperiment.onExperimentTransfered != null)
                sourceExperiment.onExperimentTransfered(sourceExperiment);

            //Now set the source experiment to a dummy experiment
            sourceExperiment.LoadFromDefinition(defaultExperiment);

            //Do a quick check for completion
            CheckCompletion();

            //If this is the last transfer we can do then show experiment results
            if (isCompleted)
            {
                finalTransfer = true;
                DeployExperiment();
            }
        }

        public void LoadFromDefinition(string experimentIDCode, bool sendTransferEvent = false)
        {
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");
            ConfigNode nodeDefinition = null;

            //Find our desired experiment
            foreach (ConfigNode nodeExperiment in experiments)
            {
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
            if (nodeDefinition.HasValue("requiredParts"))
                requiredParts = nodeDefinition.GetValue("requiredParts");

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
            {
                requiredResources = nodeDefinition.GetValue("requiredResources");
                rebuildResourceMap();
            }

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

        protected void rebuildResourceMap()
        {
            if (string.IsNullOrEmpty(requiredResources))
                return;

            //Build resource map
            string[] resources = requiredResources.Split(new char[] { ';' });
            string[] resourceAmount = null;

            resourceMap = new Dictionary<string, double>();
            foreach (string resource in resources)
            {
                resourceAmount = resource.Split(new char[] { ',' });
                resourceMap.Add(resourceAmount[0], double.Parse(resourceAmount[1]));
            }
        }
    }
}
