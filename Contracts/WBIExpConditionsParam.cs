using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public class WBIExpConditionsParam : ContractParameter
    {
        public int minCrew;

        public string celestialBodies = string.Empty;

        public double minAltitude;

        public double maxAltitude;

        public string requiredResources = string.Empty;

        public bool checkPartResources;

        public string accumulatedResources = string.Empty;

        public string situations = string.Empty;

        public float chanceOfSuccess;

        public float minimumAsteroidMass;

        protected string[] requiredParts = null;
        protected int currentPartCount;
        protected bool hasRequiredParts;
        protected ConfigNode nodeCompletionHandler = null;
        protected string partsList = string.Empty;

        string experimentID = string.Empty;

        public WBIExpConditionsParam()
        {
        }

        public WBIExpConditionsParam(string experiment)
        {
            this.experimentID = experiment;

            ConfigNode experimentNode = WBIResearchContract.GetExperimentNode(experimentID);
            loadFromDefinition(experimentNode);
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return "Satisfy all experiment conditions (check experiment for details)";
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("experimentID", experimentID);
        }

        protected override void OnLoad(ConfigNode node)
        {
            experimentID = node.GetValue("experimentID");

            ConfigNode experimentNode = WBIResearchContract.GetExperimentNode(experimentID);
            loadFromDefinition(experimentNode);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (checkConditions())
                base.SetComplete();
            else
                base.SetIncomplete();
        }

        protected void loadFromDefinition(ConfigNode nodeDefinition)
        {
            //requiredParts
            if (nodeDefinition.HasValue("requiredPart"))
            {
                requiredParts = nodeDefinition.GetValues("requiredPart");
                StringBuilder builder = new StringBuilder();
                for (int index = 0; index < requiredParts.Length; index++)
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
        }

        protected bool checkConditions()
        {
            int totalCount;
            int index;
            Part testPart;

            if (HighLogic.LoadedSceneIsFlight == false)
                return false;
            Vessel activeVessel = FlightGlobals.ActiveVessel;

            //Mininum Crew
            if (minCrew > 0)
            {
                if (activeVessel.GetCrewCount() < minCrew)
                {
                    return false;
                }
            }

            //Celestial bodies
            if (string.IsNullOrEmpty(celestialBodies) == false)
            {
                if (celestialBodies.Contains(activeVessel.mainBody.name) == false)
                {
                    return false;
                }
            }

            //Flight states
            if (string.IsNullOrEmpty(situations) == false)
            {
                string situation = activeVessel.situation.ToString();
                if (situations.Contains(situation) == false)
                {
                    return false;
                }
            }

            //Min altitude
            if (minAltitude > 0.001f)
            {
                if (activeVessel.altitude < minAltitude)
                {
                    return false;
                }
            }

            //Max altitude
            if (maxAltitude > 0.001f)
            {
                if (activeVessel.altitude > maxAltitude)
                {
                    return false;
                }
            }

            //Asteroid Mass
            if (minimumAsteroidMass > 0.001f)
            {
                List<ModuleAsteroid> asteroidList = activeVessel.FindPartModulesImplementing<ModuleAsteroid>();
                ModuleAsteroid[] asteroids = asteroidList.ToArray();
                ModuleAsteroid asteroid;
                float largestAsteroidMass = 0f;

                //No asteroids? That's a problem!
                if (asteroidList.Count == 0)
                {
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
                    return false;
                }
            }

            //Required parts
            if (string.IsNullOrEmpty(partsList) == false)
            {
                int partCount = activeVessel.Parts.Count;
                if (currentPartCount != partCount)
                {
                    currentPartCount = partCount;
                    hasRequiredParts = false;
                    totalCount = activeVessel.parts.Count;
                    for (index = 0; index < totalCount; index++)
                    {
                        testPart = activeVessel.parts[index];
                        if (partsList.Contains(testPart.partInfo.title))
                        {
                            hasRequiredParts = true;
                            break;
                        }
                    }
                }

                if (hasRequiredParts == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
