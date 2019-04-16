using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;

namespace WildBlueIndustries
{
    public enum WBIBackroundEmailTypes
    {
        missingResources,
        missingRequiredResource,
        containerFull,
        yieldCriticalFail,
        yieldCriticalSuccess,
        yieldLower,
        yieldNominal
    }

    public class WBIBackgroundConverter
    {
        public static string NodeName = "WBIBackgroundConverter";
        public static string skipReources = "ElectricCharge;";

        #region Properties
        public string converterID;
        public string vesselID;
        double hoursPerCycle = 0.0f;
        float minimumSuccess = 0.0f;
        float criticalSuccess = 0.0f;
        float criticalFail = 0.0f;
        double criticalSuccessMultiplier = 1.0f;
        double failureMultiplier = 1.0f;
        #endregion

        #region Housekeeping
        List<ResourceRatio> inputResources = new List<ResourceRatio>();
        List<ResourceRatio> outputResources = new List<ResourceRatio>();
        List<ResourceRatio> requiredResources = new List<ResourceRatio>();
        List<ResourceRatio> yieldResources = new List<ResourceRatio>();
        string inputResourceNames;
        string outputResourceNames;
        string requiedResourceNames;
        string yieldResourceNames;

        public string ConverterName = string.Empty;
        public bool IsActivated = false;
        public bool isMissingResources = false;
        public bool isContainerFull = false;
        float BaseEfficiency = 1.0f;
        float EfficiencyBonus = 1.0f;
        bool UseSpecialistBonus = false;
        float SpecialistBonusBase = 0.05f;
        float specialistSkillLevel = 0f;
        float SpecialistEfficiencyFactor = 0f;
        string ExperienceEffect = string.Empty;
        double cycleStartTime = 0;

        ProtoPartSnapshot protoPart;
        ProtoPartModuleSnapshot moduleSnapshot;
        Dictionary<string, List<ProtoPartResourceSnapshot>> protoResources = new Dictionary<string, List<ProtoPartResourceSnapshot>>();
        double productionMultiplier = 1.0f;
        #endregion

        #region Load and Save
        public void Load(ConfigNode node)
        {
            converterID = node.GetValue("converterID");
            vesselID = node.GetValue("vesselID");
            ConverterName = node.GetValue("ConverterName");

            float.TryParse(node.GetValue("BaseEfficiency"), out BaseEfficiency);
            bool.TryParse(node.GetValue("UseSpecialistBonus"), out UseSpecialistBonus);
            if (node.HasValue("ExperienceEffect"))
                ExperienceEffect = node.GetValue("ExperienceEffect");
            float.TryParse(node.GetValue("SpecialistEfficiencyFactor"), out SpecialistEfficiencyFactor);
            float.TryParse(node.GetValue("SpecialistBonusBase"), out SpecialistBonusBase);            

            bool.TryParse(node.GetValue("IsActivated"), out IsActivated);
            bool.TryParse(node.GetValue("isMissingResources"), out isMissingResources);
            bool.TryParse(node.GetValue("isContainerFull"), out isContainerFull);

            double.TryParse(node.GetValue("hoursPerCycle"), out hoursPerCycle);
            float.TryParse(node.GetValue("minimumSuccess"), out minimumSuccess);
            float.TryParse(node.GetValue("criticalSuccess"), out criticalSuccess);
            float.TryParse(node.GetValue("criticalFail"), out criticalFail);
            double.TryParse(node.GetValue("criticalSuccessMultiplier"), out criticalSuccessMultiplier);
            double.TryParse(node.GetValue("failureMultiplier"), out failureMultiplier);
            double.TryParse(node.GetValue("cycleStartTime"), out cycleStartTime);

            inputResources = new List<ResourceRatio>();
            loadResourceNodes(node, "INPUT_RESOURCE", inputResources);

            outputResources = new List<ResourceRatio>();
            loadResourceNodes(node, "OUTPUT_RESOURCE", outputResources);

            requiredResources = new List<ResourceRatio>();
            loadResourceNodes(node, "REQUIRED_RESOURCE", requiredResources);

            yieldResources = new List<ResourceRatio>();
            loadResourceNodes(node, "YIELD_RESOURCE", yieldResources);

            if (node.HasValue("inputResourceNames"))
                inputResourceNames = node.GetValue("inputResourceNames");
            if (node.HasValue("outputResourceNames"))
                outputResourceNames = node.GetValue("outputResourceNames");
            if (node.HasValue("requiedResourceNames"))
                requiedResourceNames = node.GetValue("requiedResourceNames");
            if (node.HasValue("yieldResourceNames"))
                yieldResourceNames = node.GetValue("yieldResourceNames");
        }

        protected void loadResourceNodes(ConfigNode node, string nodeName, List<ResourceRatio> resources)
        {
            ConfigNode[] resourceNodes;
            ConfigNode resourceNode;
            ResourceRatio resourceRatio;

            if (node.HasNode(nodeName))
            {
                resourceNodes = node.GetNodes(nodeName);

                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceNode = resourceNodes[index];

                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNode);

                    resources.Add(resourceRatio);
                }
            }
        }

        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode(NodeName);

            node.AddValue("converterID", converterID);
            node.AddValue("vesselID", vesselID);
            node.AddValue("ConverterName", ConverterName);
            node.AddValue("hoursPerCycle", hoursPerCycle);
            node.AddValue("cycleStartTime", cycleStartTime);
            node.AddValue("minimumSuccess", minimumSuccess);
            node.AddValue("criticalSuccess", criticalSuccess);
            node.AddValue("criticalFail", criticalFail);
            node.AddValue("criticalSuccessMultiplier", criticalSuccessMultiplier);
            node.AddValue("failureMultiplier", failureMultiplier);
            node.AddValue("IsActivated", IsActivated);
            node.AddValue("isMissingResources", isMissingResources);
            node.AddValue("isContainerFull", isContainerFull);

            node.AddValue("BaseEfficiency", BaseEfficiency);
            node.AddValue("UseSpecialistBonus", UseSpecialistBonus);
            if (!string.IsNullOrEmpty(ExperienceEffect))
                node.AddValue("ExperienceEffect", ExperienceEffect);
            node.AddValue("SpecialistEfficiencyFactor", SpecialistEfficiencyFactor);
            node.AddValue("SpecialistBonusBase", SpecialistBonusBase);

            if (!string.IsNullOrEmpty(inputResourceNames))
                node.AddValue("inputResourceNames", inputResourceNames);
            if (!string.IsNullOrEmpty(outputResourceNames))
                node.AddValue("outputResourceNames", outputResourceNames);
            if (!string.IsNullOrEmpty(requiedResourceNames))
                node.AddValue("requiedResourceNames", requiedResourceNames);
            if (!string.IsNullOrEmpty(yieldResourceNames))
                node.AddValue("yieldResourceNames", yieldResourceNames);

            ConfigNode resourceNode;
            foreach (ResourceRatio resourceRatio in inputResources)
            {
                resourceNode = new ConfigNode("INPUT_RESOURCE");
                resourceRatio.Save(resourceNode);
                node.AddNode(resourceNode);
            }
            foreach (ResourceRatio resourceRatio in outputResources)
            {
                resourceNode = new ConfigNode("OUTPUT_RESOURCE");
                resourceRatio.Save(resourceNode);
                node.AddNode(resourceNode);
            }
            foreach (ResourceRatio resourceRatio in requiredResources)
            {
                resourceNode = new ConfigNode("REQUIRED_RESOURCE");
                resourceRatio.Save(resourceNode);
                node.AddNode(resourceNode);
            }
            foreach (ResourceRatio resourceRatio in yieldResources)
            {
                resourceNode = new ConfigNode("YIELD_RESOURCE");
                resourceRatio.Save(resourceNode);
                node.AddNode(resourceNode);
            }

            return node;
        }

        public void GetConverterData(WBIOmniConverter converter)
        {
            this.converterID = converter.ID;
            this.ConverterName = converter.ConverterName;
            this.vesselID = converter.part.vessel.id.ToString();
            this.hoursPerCycle = converter.hoursPerCycle;
            this.minimumSuccess = converter.minimumSuccess;
            this.criticalSuccess = converter.criticalSuccess;
            this.criticalFail = converter.criticalFail;
            this.criticalSuccessMultiplier = converter.criticalSuccessMultiplier;
            this.failureMultiplier = converter.failureMultiplier;

            this.BaseEfficiency = converter.BaseEfficiency;
            this.UseSpecialistBonus = converter.UseSpecialistBonus;
            if (converter.UseSpecialistBonus)
            {
                this.ExperienceEffect = converter.ExperienceEffect;
                this.SpecialistEfficiencyFactor = converter.SpecialistEfficiencyFactor;
                this.SpecialistBonusBase = converter.SpecialistBonusBase;
            }
            else
            {
                this.SpecialistBonusBase = 0f;
                this.SpecialistEfficiencyFactor = 0f;
                this.ExperienceEffect = string.Empty;
            }

            ResourceRatio ratio;
            inputResourceNames = "";
            inputResources.Clear();
            foreach (ResourceRatio resourceRatio in converter.inputList)
            {
                if (skipReources.Contains(resourceRatio.ResourceName))
                    continue;
                ratio = new ResourceRatio();
                ratio.ResourceName = resourceRatio.ResourceName;
                ratio.Ratio = resourceRatio.Ratio;
                ratio.FlowMode = resourceRatio.FlowMode;
                ratio.DumpExcess = resourceRatio.DumpExcess;

                this.inputResources.Add(ratio);
                inputResourceNames += ratio.ResourceName + ";";
            }
            outputResourceNames = "";
            outputResources.Clear();
            foreach (ResourceRatio resourceRatio in converter.outputList)
            {
                if (skipReources.Contains(resourceRatio.ResourceName))
                    continue;
                ratio = new ResourceRatio();
                ratio.ResourceName = resourceRatio.ResourceName;
                ratio.Ratio = resourceRatio.Ratio;
                ratio.FlowMode = resourceRatio.FlowMode;
                ratio.DumpExcess = resourceRatio.DumpExcess;

                this.outputResources.Add(ratio);
                outputResourceNames += ratio.ResourceName + ";";
            }
            requiedResourceNames = "";
            requiredResources.Clear();
            foreach (ResourceRatio resourceRatio in converter.reqList)
            {
                if (skipReources.Contains(resourceRatio.ResourceName))
                    continue;
                ratio = new ResourceRatio();
                ratio.ResourceName = resourceRatio.ResourceName;
                ratio.Ratio = resourceRatio.Ratio;
                ratio.FlowMode = resourceRatio.FlowMode;
                ratio.DumpExcess = resourceRatio.DumpExcess;

                this.requiredResources.Add(ratio);
                requiedResourceNames += ratio.ResourceName + ";";
            }
            yieldResourceNames = "";
            yieldResources.Clear();
            foreach (ResourceRatio resourceRatio in converter.yieldResources)
            {
                if (skipReources.Contains(resourceRatio.ResourceName))
                    continue;
                ratio = new ResourceRatio();
                ratio.ResourceName = resourceRatio.ResourceName;
                ratio.Ratio = resourceRatio.Ratio;
                ratio.FlowMode = resourceRatio.FlowMode;
                ratio.DumpExcess = resourceRatio.DumpExcess;

                this.yieldResources.Add(ratio);
                yieldResourceNames += ratio.ResourceName + ";";
            }
        }
        #endregion

        #region Converter Operations
        public void CheckRequiredResources(ProtoVessel vessel, double elapsedTime)
        {
            int count = requiredResources.Count;
            if (count == 0)
                return;

            ResourceRatio resourceRatio;
            double amount = 0;
            for (int index = 0; index < count; index++)
            {
                resourceRatio = requiredResources[index];
                amount = getAmount(resourceRatio.ResourceName, resourceRatio.FlowMode);
                if (amount < resourceRatio.Ratio)
                {
                    isMissingResources = true;

                    emailPlayer(resourceRatio.ResourceName, WBIBackroundEmailTypes.missingRequiredResource);

                    return;
                }
            }
        }

        public void ConsumeInputResources(ProtoVessel vessel, double elapsedTime)
        {
            int count = inputResources.Count;
            if (count == 0)
                return;
            if (isMissingResources)
                return;
            if (isContainerFull)
                return;

            //Check to make sure we have enough resources
            ResourceRatio resourceRatio;
            double amount = 0;
            double demand = 0;
            for (int index = 0; index < count; index++)
            {
                resourceRatio = inputResources[index];
                demand = resourceRatio.Ratio * productionMultiplier * elapsedTime;
                amount = getAmount(resourceRatio.ResourceName, resourceRatio.FlowMode);
                if (amount < demand)
                {
                    //Set the missing resources flag
                    isMissingResources = true;

                    //Email player
                    emailPlayer(resourceRatio.ResourceName, WBIBackroundEmailTypes.missingResources);
                    return;
                }
            }

            //Now consume the resources
            for (int index = 0; index < count; index++)
            {
                resourceRatio = inputResources[index];
                demand = resourceRatio.Ratio * productionMultiplier * elapsedTime;
                requestAmount(resourceRatio.ResourceName, demand, resourceRatio.FlowMode);
            }
        }

        public void ProduceOutputResources(ProtoVessel vessel, double elapsedTime)
        {
            int count = outputResources.Count;
            if (count == 0)
                return;
            if (isMissingResources)
                return;
            if (isContainerFull)
                return;

            ResourceRatio resourceRatio;
            double supply = 0;
            for (int index = 0; index < count; index++)
            {
                resourceRatio = outputResources[index];
                supply = resourceRatio.Ratio * productionMultiplier * elapsedTime;
                supplyAmount(resourceRatio.ResourceName, supply, resourceRatio.FlowMode, resourceRatio.DumpExcess);
            }
        }

        public void ProduceYieldResources(ProtoVessel vessel)
        {
            int count = yieldResources.Count;
            if (count == 0)
                return;
            if (isMissingResources)
                return;
            if (isContainerFull)
                return;

            //Check cycle start time
            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                return;
            }

            //Calculate elapsed time
            double elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;
            double secondsPerCycle = hoursPerCycle * 3600;

            //If we've elapsed time cycle then perform the analyis.
            float completionRatio = (float)(elapsedTime / secondsPerCycle);
            if (completionRatio > 1.0f)
            {
                //Reset start time
                cycleStartTime = Planetarium.GetUniversalTime();

                int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                int currentCycle;
                for (currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                {
                    if (minimumSuccess <= 0)
                    {
                        supplyYieldResources(1.0);
                    }

                    else
                    {
                        //Roll the die
                        float roll = 0.0f;
                        roll = UnityEngine.Random.Range(1, 6);
                        roll += UnityEngine.Random.Range(1, 6);
                        roll += UnityEngine.Random.Range(1, 6);
                        roll *= 5.5556f;

                        if (roll <= criticalFail)
                        {
                            //Deactivate converter
                            IsActivated = false;

                            //Email player
                            emailPlayer(null, WBIBackroundEmailTypes.yieldCriticalFail);

                            //Done
                            return;
                        }
                        else if (roll >= criticalSuccess)
                        {
                            supplyYieldResources(criticalSuccessMultiplier);
                        }
                        else if (roll >= minimumSuccess)
                        {
                            supplyYieldResources(1.0);
                        }
                        else
                        {
                            supplyYieldResources(failureMultiplier);
                        }
                    }
                }
            }
        }

        public void PrepareToProcess(ProtoVessel vessel)
        {
            //Find out proto part and module and resources
            int count = vessel.protoPartSnapshots.Count;
            int moduleCount;
            int resourceCount;
            ProtoPartSnapshot pps;
            ProtoPartModuleSnapshot protoPartModule;
            ProtoPartResourceSnapshot protoPartResource;
            List<ProtoPartResourceSnapshot> resourceList;

            //Clear our resource map.
            protoResources.Clear();

            for (int index = 0; index < count; index++)
            {
                //Get the proto part snapshot
                pps = vessel.protoPartSnapshots[index];

                //Now search through the modules for the converter module
                moduleCount = pps.modules.Count;
                for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
                {
                    protoPartModule = pps.modules[moduleIndex];
                    if (protoPartModule.moduleName == "WBIOmniConverter")
                    {
                        //Ok, we found an omni converter, now check its ID.
                        if (protoPartModule.moduleValues.HasValue("ID"))
                        {
                            if (protoPartModule.moduleValues.GetValue("ID") == this.converterID)
                            {
                                //Cache the part and module
                                protoPart = pps;
                                moduleSnapshot = protoPartModule;

                                //Get activation state
                                if (protoPartModule.moduleValues.HasValue("IsActivated"))
                                    bool.TryParse(protoPartModule.moduleValues.GetValue("IsActivated"), out IsActivated);

                                //Get EfficiencyBonus
                                if (protoPartModule.moduleValues.HasValue("EfficiencyBonus"))
                                    float.TryParse(protoPartModule.moduleValues.GetValue("EfficiencyBonus"), out EfficiencyBonus);

                                //Get cycleStartTime
                                if (protoPartModule.moduleValues.HasValue("cycleStartTime"))
                                    double.TryParse(protoPartModule.moduleValues.GetValue("cycleStartTime"), out cycleStartTime);

                                //Done
                                break;
                            }
                        }
                    }
                }

                //Next, sort through all the resources and add them to our buckets.
                resourceCount = pps.resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    protoPartResource = pps.resources[resourceIndex];

                    //Inputs
                    if (!string.IsNullOrEmpty(inputResourceNames) && !skipReources.Contains(protoPartResource.resourceName))
                    {
                        if (inputResourceNames.Contains(protoPartResource.resourceName))
                        {
                            if (protoResources.ContainsKey(protoPartResource.resourceName))
                            {
                                resourceList = protoResources[protoPartResource.resourceName];
                            }
                            else
                            {
                                protoResources.Add(protoPartResource.resourceName, new List<ProtoPartResourceSnapshot>());
                                resourceList = protoResources[protoPartResource.resourceName];
                            }

                            resourceList.Add(protoPartResource);
                            protoResources[protoPartResource.resourceName] = resourceList;
                        }
                    }

                    //Outputs
                    if (!string.IsNullOrEmpty(outputResourceNames) && !skipReources.Contains(protoPartResource.resourceName))
                    {
                        if (outputResourceNames.Contains(protoPartResource.resourceName))
                        {
                            if (protoResources.ContainsKey(protoPartResource.resourceName))
                            {
                                resourceList = protoResources[protoPartResource.resourceName];
                            }
                            else
                            {
                                protoResources.Add(protoPartResource.resourceName, new List<ProtoPartResourceSnapshot>());
                                resourceList = protoResources[protoPartResource.resourceName];
                            }

                            resourceList.Add(protoPartResource);
                            protoResources[protoPartResource.resourceName] = resourceList;
                        }
                    }

                    //Required
                    if (!string.IsNullOrEmpty(requiedResourceNames) && !skipReources.Contains(protoPartResource.resourceName))
                    {
                        if (requiedResourceNames.Contains(protoPartResource.resourceName))
                        {
                            if (protoResources.ContainsKey(protoPartResource.resourceName))
                            {
                                resourceList = protoResources[protoPartResource.resourceName];
                            }
                            else
                            {
                                protoResources.Add(protoPartResource.resourceName, new List<ProtoPartResourceSnapshot>());
                                resourceList = protoResources[protoPartResource.resourceName];
                            }

                            resourceList.Add(protoPartResource);
                            protoResources[protoPartResource.resourceName] = resourceList;
                        }
                    }

                    //Yield
                    if (!string.IsNullOrEmpty(yieldResourceNames) && !skipReources.Contains(protoPartResource.resourceName))
                    {
                        if (yieldResourceNames.Contains(protoPartResource.resourceName))
                        {
                            if (protoResources.ContainsKey(protoPartResource.resourceName))
                            {
                                resourceList = protoResources[protoPartResource.resourceName];
                            }
                            else
                            {
                                protoResources.Add(protoPartResource.resourceName, new List<ProtoPartResourceSnapshot>());
                                resourceList = protoResources[protoPartResource.resourceName];
                            }

                            resourceList.Add(protoPartResource);
                            protoResources[protoPartResource.resourceName] = resourceList;
                        }
                    }
                }
            }

            //If we give production bonuses due to experience, get the highest skill level for the required skill.
            if (UseSpecialistBonus)
            {
                ProtoCrewMember[] astronauts = vessel.GetVesselCrew().ToArray();
                ProtoCrewMember astronaut;
                float skillLevel = 0f;
                specialistSkillLevel = 0f;
                for (int crewIndex = 0; crewIndex < astronauts.Length; crewIndex++)
                {
                    astronaut = astronauts[crewIndex];
                    if (astronaut.HasEffect(ExperienceEffect))
                    {
                        skillLevel = astronaut.experienceTrait.CrewMemberExperienceLevel();
                        if (skillLevel > specialistSkillLevel)
                            specialistSkillLevel = skillLevel;
                    }
                }
            }

            //Calculate production multiplier
            float crewEfficiency = 1.0f;
            if (UseSpecialistBonus && specialistSkillLevel > 0)
                crewEfficiency = SpecialistBonusBase + (1.0f * specialistSkillLevel) * SpecialistEfficiencyFactor;
            productionMultiplier = crewEfficiency * BaseEfficiency * EfficiencyBonus;
        }

        public void PostProcess(ProtoVessel vessel)
        {
            //Update lastUpdateTime
            moduleSnapshot.moduleValues.SetValue("lastUpdateTime", Planetarium.GetUniversalTime());
        }
        #endregion

        #region Helpers
        protected void emailPlayer(string resourceName, WBIBackroundEmailTypes emailType)
        {
            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            string titleMessage;

            //From
            resultsMessage.AppendLine("From: " + protoPart.pVesselRef.vesselName);

            switch (emailType)
            {
                case WBIBackroundEmailTypes.missingResources:
                    resourceDef = definitions[resourceName];
                    titleMessage = "needs more resources";
                    resultsMessage.AppendLine("Subject: Missing Resources");
                    resultsMessage.AppendLine("There is no more " + resourceDef.displayName + " available to continue production. Operations cannot continue with the " + ConverterName + " until more resource becomes available.");
                    break;

                case WBIBackroundEmailTypes.missingRequiredResource:
                    resourceDef = definitions[resourceName];
                    titleMessage = "needs a resource";
                    resultsMessage.AppendLine("Subject: Missing Required Resource");
                    resultsMessage.AppendLine(ConverterName + " needs " + resourceDef.displayName + " in order to function. Operations halted until the resource becomes available.");
                    break;

                case WBIBackroundEmailTypes.containerFull:
                    resourceDef = definitions[resourceName];
                    titleMessage = " is out of storage space";
                    resultsMessage.AppendLine("Subject: Containers Are Full");
                    resultsMessage.AppendLine("There is no more storage space available for " + resourceDef.displayName + ". Operations cannot continue with the " + ConverterName + " until more space becomes available.");
                    break;

                case WBIBackroundEmailTypes.yieldCriticalFail:
                    titleMessage = "has suffered a critical failure in one of its converters";
                    resultsMessage.AppendLine("A " + ConverterName + " has failed! The production yield has been lost. It must be repaired and/or restarted before it can begin production again.");
                    break;

                default:
                    return;
            }

            msg = new MessageSystem.Message(protoPart.pVesselRef.vesselName + titleMessage, resultsMessage.ToString(),
                MessageSystemButton.MessageButtonColor.ORANGE, MessageSystemButton.ButtonIcons.ALERT);
            MessageSystem.Instance.AddMessage(msg);
        }

        protected void supplyYieldResources(double yieldMultiplier)
        {
            int count = yieldResources.Count;
            ResourceRatio resourceRatio;
            double supply = 0;

            for (int index = 0; index < count; index++)
            {
                resourceRatio = yieldResources[index];
                supply = resourceRatio.Ratio * productionMultiplier * yieldMultiplier;
                supplyAmount(resourceRatio.ResourceName, supply, resourceRatio.FlowMode, resourceRatio.DumpExcess);
            }
        }

        protected void supplyAmount(string resourceName, double supply, ResourceFlowMode flowMode, bool dumpExcess)
        {
            int count;
            double currentSupply = supply;
            if (flowMode != ResourceFlowMode.NO_FLOW)
            {
                if (!protoResources.ContainsKey(resourceName))
                    return;
                List<ProtoPartResourceSnapshot> resourceShapshots = protoResources[resourceName];
                count = resourceShapshots.Count;

                //Distribute the resource throughout the resource snapshots.
                //TODO: find a way to evenly distribute the resource.
                for (int index = 0; index < count; index++)
                {
                    //If the current part resource snapshot has enough room, then we can store all of the currentSupply and be done.
                    if (resourceShapshots[index].amount + currentSupply < resourceShapshots[index].maxAmount)
                    {
                        resourceShapshots[index].amount += currentSupply;
                        return;
                    }

                    //The current snapshot can't hold all of the currentSupply, but we can whittle down what we currently have.
                    else
                    {
                        currentSupply -= resourceShapshots[index].maxAmount - resourceShapshots[index].amount;
                        resourceShapshots[index].amount = resourceShapshots[index].maxAmount;
                    }
                }

                //If we have any resource left over, then it means that our containers are full.
                //If we can't dump the excess, then we're done.
                if (currentSupply > 0.0001f && !dumpExcess)
                {
                    isContainerFull = true;

                    //Email player
                    emailPlayer(resourceName, WBIBackroundEmailTypes.containerFull);

                    //Done
                    return;
                }
            }
        }

        protected double requestAmount(string resourceName, double demand, ResourceFlowMode flowMode)
        {
            double supply = 0;
            int count;

            //Check vessel
            if (flowMode != ResourceFlowMode.NO_FLOW)
            {
                if (!protoResources.ContainsKey(resourceName))
                    return 0f;
                List<ProtoPartResourceSnapshot> resourceShapshots = protoResources[resourceName];
                count = resourceShapshots.Count;

                double currentDemand = demand;
                for (int index = 0; index < count; index++)
                {
                    if (resourceShapshots[index].amount > currentDemand)
                    {
                        resourceShapshots[index].amount -= currentDemand;
                        supply += currentDemand;
                        currentDemand = 0;
                    }
                    else //Current demand > what the part has.
                    {
                        supply += resourceShapshots[index].amount;
                        currentDemand -= resourceShapshots[index].amount;
                        resourceShapshots[index].amount = 0;
                    }
                }
            }
            else //Check the part
            {
                count = protoPart.resources.Count;
                for (int index = 0; index < count; index++)
                {
                    if (protoPart.resources[index].resourceName == resourceName)
                    {
                        supply = protoPart.resources[index].amount;
                        if (supply >= demand)
                        {
                            protoPart.resources[index].amount = supply - demand;
                            return demand;
                        }
                        else
                        {
                            //Supply < demand
                            protoPart.resources[index].amount = 0;
                            return supply;
                        }
                    }
                }
            }

            return supply;
        }

        protected double getAmount(string resourceName, ResourceFlowMode flowMode)
        {
            double amount = 0;
            int count;

            if (flowMode != ResourceFlowMode.NO_FLOW)
            {
                if (!protoResources.ContainsKey(resourceName))
                    return 0f;
                List<ProtoPartResourceSnapshot> resourceShapshots = protoResources[resourceName];
                count = resourceShapshots.Count;
                for (int index = 0; index < count; index++)
                {
                    amount += resourceShapshots[index].amount;
                }
            }
            else //Check the part
            {
                count = protoPart.resources.Count;
                for (int index = 0; index < count; index++)
                {
                    if (protoPart.resources[index].resourceName == resourceName)
                        return protoPart.resources[index].amount;
                }
            }

            return amount;
        }
        #endregion
    }
}
