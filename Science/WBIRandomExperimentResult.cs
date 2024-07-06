using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

namespace WildBlueIndustries
{
    public class WBIRandomExperimentResult : WBIUnlockTechResult
    {
        List<RandomOutcome> randomOutcomes;

        public override void ExperimentRequirementsMet(string experimentID, float chanceOfSuccess, float resultRoll)
        {
            //Career/Science mode only
            Log("ExperimentRequirementsMet called");

            if (HighLogic.LoadedSceneIsFlight == false)
            {
                Log("Not in flight scene.");
                return;
            }
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log("Current game is neither career nor science sandbox.");
                return;
            }

            // Get the config node for this experiment
            randomOutcomes = new List<RandomOutcome>();
            getExperimentOutcomes(experimentID);
            if (randomOutcomes.Count <= 0)
            {
                Log("No random outcomes found");
                return;
            }
            Log("Found " + randomOutcomes.Count + " random outcomes");

            // Get a kerbal from the part
            int count = part.protoModuleCrew.Count;
            if (count <= 0)
            {
                Log("No kerbals found in the part");
                return;
            }
            int kerbalIndex = UnityEngine.Random.Range(0, count - 1);
            if (count == 1)
                kerbalIndex = 0;

            ProtoCrewMember crewMember = part.protoModuleCrew[kerbalIndex];
            Log("Selected " + crewMember.name + " for potential stat adjustment.");

            // Roll the outcome
            int outcomeRoll = UnityEngine.Random.Range(1, dieRoll);
            if (crewMember.isBadass)
                outcomeRoll += 5;

            // Apply the outcome
            applyOutcome(outcomeRoll, experimentID, chanceOfSuccess, resultRoll);
        }

        void applyOutcome(int outcomeRoll, string experimentID, float chanceOfSuccess, float resultRoll)
        {
            Log("Outcome Roll: " + outcomeRoll);

            int count = randomOutcomes.Count;
            if (outcomeRoll < randomOutcomes[0].targetNumber)
            {
                Log("Outcome roll falls below lowest targetNumber, skipping.");
                return;
            }

            // Find the outcome
            RandomOutcome outcome = new RandomOutcome();
            if (count == 1)
            {
                outcome = randomOutcomes[0];
            }
            else
            {
                for (int index = 0; index < count; index++)
                {
                    if (outcomeRoll >= randomOutcomes[index].targetNumber)
                    {
                        outcome = randomOutcomes[index];
                    }
                }
            }

            if (!outcome.isValid())
            {
                Log("Outcome is not valid, skipping.");
                return;
            }

            Log("Outcome selected: " + outcome.name);
            switch (outcome.name)
            {
                case "loseCourage":
                    handleStatAdjustment(outcome);
                    break;

                case "gainStupidity":
                    handleStatAdjustment(outcome);
                    break;

                case "gainCourage":
                    handleStatAdjustment(outcome);
                    break;

                case "loseStupidity":
                    handleStatAdjustment(outcome);
                    break;

                case "shipUnlocked":
                    ScreenMessages.PostScreenMessage("A new prototype ship is available", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case "techNodeUnlocked":
                    base.ExperimentRequirementsMet(experimentID, chanceOfSuccess, resultRoll);
                    break;

                default:
                    break;
            }
        }

        void handleStatAdjustment(RandomOutcome outcome)
        {
            if (outcome.percentAmount <= 0)
            {
                Log("percentAmount <= 0");
                return;
            }

            // Get a kerbal from the part
            int count = part.protoModuleCrew.Count;
            int kerbalIndex = UnityEngine.Random.Range(0, count - 1);
            if (count == 1)
                kerbalIndex = 0;

            ProtoCrewMember crewMember = part.protoModuleCrew[kerbalIndex];
            Log("Selected " + crewMember.name + " for stat adjustment.");

            if (crewMember.isHero)
            {
                Log(crewMember.name + " is a Hero character, skipping.");
                return;
            }
            if (crewMember.veteran)
            {
                Log(crewMember.name + " is a Veteran, skipping.");
                return;
            }

            // Adjust the stat
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            ProtoCrewMember rosterAstronaut = roster[crewMember.name];
            float statAdjustment;
            switch (outcome.name)
            {
                case "loseCourage":
                    statAdjustment = 1 - outcome.percentAmount;
                    Log("Old courage: " + crewMember.courage);
                    crewMember.courage *= statAdjustment;
                    if (crewMember.courage < 0)
                        crewMember.courage = 0;
                    Log("New courage: " + crewMember.courage);
                    break;

                case "gainStupidity":
                    statAdjustment = 1 + outcome.percentAmount;
                    Log("Old stupidity: " + crewMember.stupidity);
                    crewMember.stupidity *= statAdjustment;
                    if (crewMember.stupidity > 1)
                        crewMember.stupidity = 1;
                    Log("New stupidity: " + crewMember.stupidity);
                    break;

                case "gainCourage":
                    statAdjustment = 1 + outcome.percentAmount;
                    Log("Old courage: " + crewMember.courage);
                    crewMember.courage *= statAdjustment;
                    if (crewMember.courage > 1)
                        crewMember.courage = 1;
                    Log("New courage: " + crewMember.courage);
                    break;

                case "loseStupidity":
                    statAdjustment = 1 - outcome.percentAmount;
                    Log("Old stupidity: " + crewMember.stupidity);
                    crewMember.stupidity *= statAdjustment;
                    if (crewMember.stupidity < 0)
                        crewMember.stupidity = 0;
                    Log("New stupidity: " + crewMember.stupidity);
                    break;

                default:
                    break;
            }

            // Update roster
            rosterAstronaut.courage = crewMember.courage;
            rosterAstronaut.stupidity = crewMember.stupidity;

            // Show screen message
            if (!string.IsNullOrEmpty(outcome.displayText))
            {
                string message = Localizer.Format(outcome.displayText, new string[1] { crewMember.name });
                ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }

            // Sanity Check
            if (crewMember.courage < 0.001 || crewMember.stupidity >= 1)
            {
                ScreenMessages.PostScreenMessage(crewMember.name + " has gone Insane!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        void getExperimentOutcomes(string experimentID)
        {
            List<ConfigNode> outcomes = new List<ConfigNode>();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("RANDOM_EXPERIMENT_OUTCOMES");
            ConfigNode[] outcomeNodes;
            if (nodes.Length <= 0)
            {
                Log("No RANDOM_EXPERIMENT_OUTCOMES found");
                return;
            }

            // Find the nodes with the same ID as the experiment
            string nodeId;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].HasValue("id"))
                {
                    nodeId = nodes[index].GetValue("id");
                    Log("RANDOM_EXPERIMENT_OUTCOMES has ID " + nodeId);
                    Log("Comparing nodeId to " + experimentID);
                    if (nodeId == experimentID && nodes[index].HasNode("OUTCOME"))
                    {
                        Log("Found OUTCOME nodes with ID " + experimentID);

                        outcomeNodes = nodes[index].GetNodes("OUTCOME");
                        for (int outcomeIndex = 0; outcomeIndex < outcomeNodes.Length; outcomeIndex++)
                        {
                            randomOutcomes.Add(RandomOutcome.CreateOutcome(outcomeNodes[outcomeIndex]));
                        }
                    }
                }
            }
            randomOutcomes.OrderBy(o => o.targetNumber).ToList();

            return;
        }
    }

    /*
    OUTCOME
    {
        name = loseCourage
        targetNumber = 50
        percentAmount = 0.02
        displayText = <<1>> feels less courageous.
    }
    */
    internal class RandomOutcome
    {
        public string name;
        public int targetNumber = 1000;
        public float percentAmount;
        public string displayText;

        public static RandomOutcome CreateOutcome(ConfigNode node)
        {
            RandomOutcome outcome = new RandomOutcome();

            if (node.HasValue("name"))
                outcome.name = node.GetValue("name");

            if (node.HasValue("targetNumber"))
                int.TryParse(node.GetValue("targetNumber"), out outcome.targetNumber);

            if (node.HasValue("percentAmount"))
                float.TryParse(node.GetValue("percentAmount"), out outcome.percentAmount);

            if (node.HasValue("displayText"))
                outcome.displayText = node.GetValue("displayText");

            return outcome;
        }

        public bool isValid()
        {
            if (targetNumber == 1000)
                return false;
            if (string.IsNullOrEmpty(name))
                return false;

            return true;
        }
    }
}
