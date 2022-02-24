using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;

namespace WildBlueIndustries
{
    #region AnomalyResource
    public class AnomalyResource
    {
        public string name;
        public string resourceName;
        public double minAbundance;
        public double maxAbundance;
        public double abundance;
        public double minAmount;
        public double maxAmount;
        public double currentAmount;

        public AnomalyResource(ConfigNode node)
        {
            if (node.HasValue("name"))
                name = node.GetValue("name");

            if (node.HasValue("resourceName"))
                resourceName = node.GetValue("resourceName");

            if (node.HasValue("minAbundance"))
                double.TryParse(node.GetValue("minAbundance"), out minAbundance);
            if (node.HasValue("maxAbundance"))
                double.TryParse(node.GetValue("maxAbundance"), out maxAbundance);

            if (node.HasValue("abundance"))
                double.TryParse(node.GetValue("abundance"), out abundance);
            else if (minAbundance > 0 && maxAbundance > 0)
            {
                abundance = UnityEngine.Random.Range((float)minAbundance, (float)maxAbundance);
            }
            else if (maxAbundance > 0)
            {
                abundance = maxAbundance;
            }
            else if (minAbundance > 0)
            {
                abundance = minAbundance;
            }

            if (node.HasValue("minAmount"))
                double.TryParse(node.GetValue("minAmount"), out minAmount);
            if (node.HasValue("maxAmount"))
                double.TryParse(node.GetValue("maxAmount"), out maxAmount);

            if (node.HasValue("currentAmount"))
            {
                double.TryParse(node.GetValue("currentAmount"), out currentAmount);
            }
            else if (minAmount > 0 && maxAmount > 0)
            {
                currentAmount = UnityEngine.Random.Range((float)minAmount, (float)maxAmount);
            }
            else if (maxAmount > 0)
            {
                currentAmount = maxAmount;
            }
            else if (minAmount > 0)
            {
                currentAmount = minAmount;
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("name", name, true);
            node.SetValue("resourceName", resourceName, true);
            node.SetValue("abundance", abundance.ToString(), true);
            node.SetValue("minAmount", minAmount.ToString(), true);
            node.SetValue("maxAmount", maxAmount.ToString(), true);
            node.SetValue("currentAmount", currentAmount.ToString(), true);
        }
    }
    #endregion

    /// <summary>
    /// The purpose of this class is to run converters in the background, meaning that the vessel is currently unloaded. This is primarily to drive life support systems,
    /// but other types of converters also benefit.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class WBIOmniManager : ScenarioModule
    {
        #region Constants
        public double secondsPerCycle = 3600;
        #endregion

        #region Housekeeping
        public static WBIOmniManager Instance;

        public bool debugMode = false;
        public double cycleStartTime;
        public Dictionary<Vessel, List<WBIBackgroundConverter>> backgroundConverters;
        public List<Part> createdParts;
        public Dictionary<string, float> originalResourceCosts;
        public Dictionary<string, Dictionary<string, AnomalyResource>> anomalyResources;
        #endregion

        #region Background processing
        public void FixedUpdate()
        {
            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                return;
            }
            double elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;
            elapsedTime = secondsPerCycle;
            if (elapsedTime < secondsPerCycle)
                return;
            if (backgroundConverters.Count == 0)
                return;

            //Reset the timer
            cycleStartTime = Planetarium.GetUniversalTime();

            Vessel vessel;
            int count = FlightGlobals.Vessels.Count;
            for (int vesselIndex = 0; vesselIndex < count; vesselIndex++)
            {
                vessel = FlightGlobals.Vessels[vesselIndex];

                //Skip vessel types that we're not interested in.
                if (vessel.vesselType == VesselType.Debris ||
                    vessel.vesselType == VesselType.Flag ||
                    vessel.vesselType == VesselType.SpaceObject ||
                    vessel.vesselType == VesselType.Unknown)
                    continue;

                //Run background converters
                if (!vessel.loaded)
                    runBackgroundConverters(vessel, elapsedTime);
            }
        }

        protected void runBackgroundConverters(Vessel vessel, double elapsedTime)
        {
            List<WBIBackgroundConverter> converters;
            if (backgroundConverters.ContainsKey(vessel))
                converters = backgroundConverters[vessel];
            else
                return;
            int count = converters.Count;
            WBIBackgroundConverter converter;

            for (int index = 0; index < count; index++)
            {
                converter = converters[index];

                if (converter.IsActivated && !converter.isMissingResources && !converter.isContainerFull)
                    StartCoroutine(runConverter(converter, elapsedTime, vessel.protoVessel));
            }
        }

        protected IEnumerator<YieldInstruction> runConverter(WBIBackgroundConverter converter, double elapsedTime, ProtoVessel protoVessel)
        {
            //Get ready to process
            converter.PrepareToProcess(protoVessel);
            yield return new WaitForFixedUpdate();

            //Check required
            converter.CheckRequiredResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Consume inputs
            converter.ConsumeInputResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Produce outputs
            converter.ProduceOutputResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Produce yields
            converter.ProduceYieldResources(protoVessel);
            yield return new WaitForFixedUpdate();

            //Post process
            converter.PostProcess(protoVessel);
            yield return new WaitForFixedUpdate();
        }

        #endregion

        #region API
        public float GetOriginalResourceCost(Part part)
        {
            if (originalResourceCosts.ContainsKey(part.partInfo.name))
            {
                float cost = originalResourceCosts[part.partInfo.name];

                if (debugMode)
                    Debug.Log(string.Format("[WBIOmniManager] original resource cost: {0:n2}", cost));

                return cost;
            }

            if (debugMode)
                Debug.Log(string.Format("[WBIOmniManager] {0:s} part cost: {1:n2}", part.partInfo.name, part.partInfo.cost));

            float resourceCost = ResourceHelper.GetResourceCost(part, true);
            if (debugMode)
                Debug.Log(string.Format("[WBIOmniManager] original resource cost: {0:n2}", resourceCost));

            originalResourceCosts.Add(part.partInfo.name, resourceCost);
            return resourceCost;
        }

        public bool WasRecentlyCreated(Part part)
        {
            if (createdParts == null)
                createdParts = new List<Part>();
            return createdParts.Contains(part);
        }
        #endregion

        #region Overrides
        internal void Start()
        {
            Instance = this;
            backgroundConverters = WBIBackgroundConverter.GetBackgroundConverters();
        }

        public override void OnAwake()
        {
            Instance = this;
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselDestroy.Add(onVesselDestroy);
            GameEvents.onEditorPartEvent.Add(onEditorPartEvent);

            if (cycleStartTime <= 0)
                cycleStartTime = Planetarium.GetUniversalTime();

            originalResourceCosts = new Dictionary<string, float>();
        }

        public override void OnLoad(ConfigNode node)
        {
            //Housekeeping
            double.TryParse(node.GetValue("cycleStartTime"), out cycleStartTime);

            if (node.HasNode("OriginalResourceCost"))
            {
                ConfigNode[] costNodes = node.GetNodes("OriginalResourceCost");
                string partName = string.Empty;
                float dryCost = 0f;
                foreach (ConfigNode costNode in costNodes)
                {
                    if (costNode.HasValue("partName") && costNode.HasValue("cost"))
                    {
                        partName = costNode.GetValue("partName");
                        float.TryParse(costNode.GetValue("cost"), out dryCost);
                        if (!originalResourceCosts.ContainsKey(partName))
                            originalResourceCosts.Add(partName, dryCost);
                    }
                }
            }

            // Anomaly resources
            anomalyResources = new Dictionary<string, Dictionary<string, AnomalyResource>>();
            Dictionary<string, AnomalyResource> resources;
            ConfigNode[] resourceNodes = GameDatabase.Instance.GetConfigNodes("ANOMALY_RESOURCE");
            AnomalyResource anomalyResource;
            for (int index = 0; index < resourceNodes.Length; index++)
            {
                anomalyResource = new AnomalyResource(resourceNodes[index]);

                if (!anomalyResources.ContainsKey(anomalyResource.name))
                {
                    resources = new Dictionary<string, AnomalyResource>();
                    anomalyResources.Add(anomalyResource.name, resources);
                }

                resources = anomalyResources[anomalyResource.name];
                if (!resources.ContainsKey(anomalyResource.resourceName))
                {
                    resources.Add(anomalyResource.resourceName, anomalyResource);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            //Housekeeping
            node.AddValue("cycleStartTime", cycleStartTime);

            foreach (string key in originalResourceCosts.Keys)
            {
                ConfigNode costNode = new ConfigNode("OriginalResourceCost");
                node.SetValue("partName", key);
                node.SetValue("cost", originalResourceCosts[key].ToString());
                node.AddNode(costNode);
            }

            Dictionary<string, AnomalyResource> resources;
            ConfigNode anomalyResourceNode;
            foreach (string key in anomalyResources.Keys)
            {
                resources = anomalyResources[key];
                foreach (string resourceKey in resources.Keys)
                {
                    anomalyResourceNode = new ConfigNode("ANOMALY_RESOURCE");
                    resources[resourceKey].Save(anomalyResourceNode);
                    node.AddNode(anomalyResourceNode);
                }
            }
        }

        public void OnDestroy()
        {
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
        }

        protected void onVesselChange(Vessel vessel)
        {

        }

        protected void onVesselDestroy(Vessel vessel)
        {

        }

        public void onEditorPartEvent(ConstructionEventType eventType, Part part)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            if (createdParts == null)
                createdParts = new List<Part>();

            switch (eventType)
            {
                case ConstructionEventType.PartCreated:
                    if (!createdParts.Contains(part))
                        createdParts.Add(part);
                    break;

                case ConstructionEventType.PartDeleted:
                    if (createdParts.Contains(part))
                        createdParts.Remove(part);
                    break;
            }
        }

        #endregion
    }
}
