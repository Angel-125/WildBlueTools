using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSP;
using KSPAchievements;
using ContractsPlus.Contracts;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public struct WBIExperimentSituations
    {
        public bool any;
        public bool docked;
        public bool escaping;
        public bool flying;
        public bool landed;
        public bool prelaunch;
        public bool orbiting;
        public bool splashed;
        public bool subOrbital;
    }

    public class WBIResearchContract : Contract
    {
        public const int CurrentContractVersion = 2;

        const float fundsAdvance = 15000f;
        const float fundsCompleteBase = 50000f;
        const float fundsFailure = 35000f;
        const float repComplete = 150f;
        const float repFailure = 60f;
        const float rewardAdjustmentFactor = 0.4f;
        const string defaultExperiment = "WBIEmptyExperiment";

        public bool experimentCompleted = false;
        public int versionNumber = 1;

        CelestialBody targetBody = null;
        ConfigNode experimentNode = null;
        WBIExperimentSituations situations;
        string experimentID = string.Empty;

        protected override bool Generate()
        {
            Debug.Log("[WBIResearchContract] - Trying to generate a contract, available count: " + WBIContractScenario.Instance.contractsAvailable + "/" + WBIContractScenario.maxContracts);
            if (WBIContractScenario.Instance.contractsAvailable == WBIContractScenario.maxContracts)
                return false;

            generateExperimentIfNeeded();
//            Debug.Log("[WBIResearchContract] - Generating contract for " + experimentNode.GetValue("title"));

            //Add parameter: complete research of experiment, orbiting/landed/splashed, etc.
            this.AddParameter(new WBITargetBodyParam(targetBody), null);

            //Conditions met parameter
            this.AddParameter(new WBIExpConditionsParam(experimentID), null);

            //Experiment completed parameter
            string situationsString = "any";
            if (experimentNode.HasValue("situations"))
                situationsString = experimentNode.GetValue("situationsString");
            this.AddParameter(new WBIExpCompleteParam(experimentID, targetBody, situationsString), null);

            //Return to Kerbin parameter: just need landed or splashed.
            if (requiresReturn())
                this.AddParameter(new WBIReturnHomeParam(), null);

            base.SetExpiry();
            base.SetScience(getScienceReward(experimentNode, situations, targetBody), targetBody);
            base.SetDeadlineYears(1f, targetBody);
            base.SetReputation(repComplete, repFailure, targetBody);
            base.SetFunds(fundsAdvance, getCompletionReward(experimentNode, situations, targetBody), fundsFailure, targetBody);
            return true;
        }

        public override bool CanBeCancelled()
        {
            return true;
        }

        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
            return targetBody.bodyName;
        }

        protected override string GetTitle()
        {
            generateExperimentIfNeeded();

            return "Research " + experimentNode.GetValue("title") + " at " + targetBody.theName;
        }

        protected override string GetDescription()
        {
            generateExperimentIfNeeded();

            //those 3 strings appear to do nothing
            return TextGen.GenerateBackStories(Agent.Name, Agent.GetMindsetString(), "researching", "research", "it was aliens", new System.Random().Next());
        }

        protected override string GetSynopsys()
        {
            generateExperimentIfNeeded();

            string vicinity = GetVicinity(situations);

            return "Complete research on the " + experimentNode.GetValue("title") + " " + vicinity + " " + targetBody.theName;
        }

        protected override string MessageCompleted()
        {
            return "You have succesfully completed research on the " + experimentNode.GetValue("title") + " at " + targetBody.theName;
        }

        protected override void OnLoad(ConfigNode node)
        {
            int bodyID = int.Parse(node.GetValue("targetBody"));
            foreach (var body in FlightGlobals.Bodies)
            {
                if (body.flightGlobalsIndex == bodyID)
                    targetBody = body;
            }

            experimentID = node.GetValue("experimentID");
            experimentNode = GetExperimentNode(experimentID);
            situations = GetSituations(experimentNode);
            if (node.HasValue("experimentCompleted"))
                experimentCompleted = bool.Parse(node.GetValue("experimentCompleted"));
            if (node.HasValue("versionNumber"))
                versionNumber = int.Parse("versionNumber");
        }

        protected override void OnSave(ConfigNode node)
        {
            int bodyID = targetBody.flightGlobalsIndex;
            node.AddValue("targetBody", bodyID);
            node.AddValue("experimentID", experimentID);
            node.AddValue("experimentCompleted", experimentCompleted);
            node.AddValue("versionNumber", CurrentContractVersion);
        }

        protected override void OnParameterStateChange(ContractParameter p)
        {
            base.OnParameterStateChange(p);

            foreach (ContractParameter parameter in AllParameters)
            {
                if (parameter.State == ParameterState.Incomplete || parameter.State == ParameterState.Failed)
                    return;
            }

            //All parameters are complete
            SetState(State.Completed);
        }

        protected override void OnOffered()
        {
            base.OnOffered();
            WBIContractScenario.Instance.contractsAvailable += 1;
            if (WBIContractScenario.Instance.contractsAvailable > WBIContractScenario.maxContracts)
                WBIContractScenario.Instance.contractsAvailable = WBIContractScenario.maxContracts;
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
            WBIContractScenario.Instance.contractsAvailable -= 1;
            if (WBIContractScenario.Instance.contractsAvailable < 0)
                WBIContractScenario.Instance.contractsAvailable = 0;
        }

        protected override void OnFailed()
        {
            base.OnFailed();
            WBIContractScenario.Instance.contractsAvailable -= 1;
            if (WBIContractScenario.Instance.contractsAvailable < 0)
                WBIContractScenario.Instance.contractsAvailable = 0;
        }

        protected override void OnFinished()
        {
            base.OnFinished();
            WBIContractScenario.Instance.contractsAvailable -= 1;
            if (WBIContractScenario.Instance.contractsAvailable < 0)
                WBIContractScenario.Instance.contractsAvailable = 0;
        }

        protected override void OnDeclined()
        {
            base.OnDeclined();
            WBIContractScenario.Instance.contractsAvailable -= 1;
            if (WBIContractScenario.Instance.contractsAvailable < 0)
                WBIContractScenario.Instance.contractsAvailable = 0;
        }

        protected override void OnOfferExpired()
        {
            base.OnOfferExpired();
            WBIContractScenario.Instance.contractsAvailable -= 1;
            if (WBIContractScenario.Instance.contractsAvailable < 0)
                WBIContractScenario.Instance.contractsAvailable = 0;
        }

        public override bool MeetRequirements()
        {
            generateExperimentIfNeeded();

            //Has the target body been orbited?
            bool targetBodyOrbited = false;
            foreach (CelestialBodySubtree subtree in ProgressTracking.Instance.celestialBodyNodes)
            {
                if (subtree.Body == targetBody)
                {
                    targetBodyOrbited = true;
                    break;
                }
            }
            if (!targetBodyOrbited)
            {
                return false;
            }

            //If the experiment has a minimum tech requirement, make sure we've met the requirement.
            if (experimentNode.HasValue("techRequired") == false)
                return true;

            string value = experimentNode.GetValue("techRequired");

            if (ResearchAndDevelopment.GetTechnologyState(value) != RDTech.State.Available)
            {
                return false;
            }

            return true;
        }

        public static ConfigNode GetExperimentNode(string targetID)
        {
            //Go through all the experiments and find the one we're interested in.
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");
            ConfigNode experiment = null;

            for (int index = 0; index < experiments.Length; index++)
            {
                experiment = experiments[index];
                if (experiments[index].GetValue("id") == targetID)
                    return experiment;
            }

            return null;
        }

        public bool requiresReturn()
        {
            ConfigNode[] contractSettings = GameDatabase.Instance.GetConfigNodes("WBICONTRACTSETTINGS");
            StringBuilder exclusions = new StringBuilder();

            //If we don't have any settings to process then we're done.
            if (contractSettings.Length == 0)
                return true;

            for (int index = 0; index < contractSettings.Length; index++)
            {
                if (contractSettings[index].HasValue("ignoreReturnToHome"))
                    exclusions.Append(contractSettings[index].GetValue("ignoreReturnToHome"));
            }

            if (exclusions.ToString().Contains(experimentID))
                return false;

            return true;
        }

        public static ConfigNode getRandomExperiment()
        {
            ConfigNode experimentNode = null;

            //Find all the WBI science experiments, and randomly choose one of them.
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");
            ConfigNode experiment = null;
            string experimentID;
            List<ConfigNode> experimentSelection = new List<ConfigNode>();

            for (int index = 0; index < experiments.Length; index++)
            {
                experiment = experiments[index];
                experimentID = experiment.GetValue("id");

                if (experiment.HasValue("description") && experiment.HasValue("mass") && experimentID != defaultExperiment)
                {
                    experimentSelection.Add(experiment);
                }
            }

            if (experimentSelection.Count > 0)
                experimentNode = experimentSelection[UnityEngine.Random.Range(0, experimentSelection.Count - 1)];

            return experimentNode;
        }

        public static string GetVicinity(WBIExperimentSituations situations)
        {
            string vicinity = string.Empty;

            //Determine vicinity
            if (situations.any)
                vicinity = "on or while orbiting";
            else if (situations.landed && situations.prelaunch && situations.splashed && situations.orbiting)
                vicinity = "on or while orbiting";
            else if (situations.landed && situations.splashed && situations.orbiting)
                vicinity = "on or while orbiting";
            else if (situations.landed || situations.prelaunch || situations.splashed)
                vicinity = "on";
            else if (situations.orbiting)
                vicinity = "while orbiting";
            else if (situations.flying)
                vicinity = "flying around";
            else if (situations.escaping)
                vicinity = "while escaping";
            else if (situations.subOrbital)
                vicinity = "on sub-orbital trajectory around";
            else
                vicinity = "Unknown";

            return vicinity;
        }

        public static WBIExperimentSituations GetSituations(ConfigNode experimentNode)
        {
            WBIExperimentSituations situations = new WBIExperimentSituations();

            //DOCKED, ESCAPING, FLYING, LANDED, PRELAUNCH, ORBITING, SPLASHED, SUB_ORBITAL
            if (experimentNode.HasValue("situations"))
            {
                string[] situationRequirements = experimentNode.GetValue("situations").Split(new char[]{';'});
                for (int index = 0; index < situationRequirements.Length; index++)
                {
                    switch (situationRequirements[index])
                    {
                        case "DOCKED":
                            situations.docked = true;
                            break;

                        case "ESCAPING":
                            situations.escaping = true;
                            break;

                        case "FLYING":
                            situations.flying = true;
                            break;

                        case "LANDED":
                            situations.landed = true;
                            break;

                        case "PRELAUNCH":
                            situations.prelaunch = true;
                            break;

                        case "SUB_ORBITAL":
                            situations.subOrbital = true;
                            break;

                        case "ORBITING":
                        default:
                            situations.orbiting = true;
                            break;
                    }
                }
            }

            else
            {
                situations.any = true;
            }

            return situations;
        }

        protected void generateExperimentIfNeeded()
        {
            if (experimentNode == null)
            {
                targetBody = getNextTarget();
                if (targetBody == null)
                {
                    targetBody = Planetarium.fetch.Home;
                    Debug.LogWarning("targetBody could not be computed, using homeworld");
                }

                experimentNode = getRandomExperiment();
            }
            experimentID = experimentNode.GetValue("id");
            situations = GetSituations(experimentNode);
        }

        protected static float getScienceReward(ConfigNode experimentNode, WBIExperimentSituations situations, CelestialBody targetBody)
        {
            float reward = float.Parse(experimentNode.GetValue("baseValue"));

            //Base value * science multiplier
            reward *= getRewardMultiplier(situations, targetBody);

            return reward;
        }

        protected static float getCompletionReward(ConfigNode experimentNode, WBIExperimentSituations situations, CelestialBody targetBody)
        {
            float reward = fundsCompleteBase;

            //Base value * science multiplier
            //Landed > low > high > sub-orbital > flying
            reward *= getRewardMultiplier(situations, targetBody);

            return reward;
        }

        protected static float getRewardMultiplier(WBIExperimentSituations situations, CelestialBody targetBody)
        {
            float multiplier = 1.0f;

            //Base value * science multiplier
            //Landed > low > high > sub-orbital > flying
            if (situations.landed || situations.prelaunch || situations.splashed)
                multiplier = targetBody.scienceValues.LandedDataValue;

            else if (situations.orbiting)
                multiplier = targetBody.scienceValues.InSpaceLowDataValue;

            else if (situations.subOrbital || situations.escaping)
                multiplier = targetBody.scienceValues.InSpaceHighDataValue;

            else if (situations.flying)
                multiplier = targetBody.scienceValues.FlyingLowDataValue;

            multiplier *= rewardAdjustmentFactor;
            return multiplier;
        }

        protected static CelestialBody getNextTarget()
        {
            //We can do research at any body we've been to.
            var bodies = Contract.GetBodies_Reached(true, true);
            if (bodies != null && bodies.Count > 0)
                return bodies[UnityEngine.Random.Range(0, bodies.Count - 1)];
            return null;
        }
    }
}
