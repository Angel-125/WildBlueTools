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
        public bool playerEmailed = false;
        double hoursPerCycle = 0.0f;
        float minimumSuccess = 0.0f;
        float criticalSuccess = 0.0f;
        float criticalFail = 0.0f;
        double criticalSuccessMultiplier = 1.0f;
        double failureMultiplier = 1.0f;
        #endregion

        #region Housekeeping
        List<ResourceRatio> inputList = new List<ResourceRatio>();
        List<ResourceRatio> outputList = new List<ResourceRatio>();
        List<ResourceRatio> requiredList = new List<ResourceRatio>();
        List<ResourceRatio> yieldsList = new List<ResourceRatio>();
        string inputResourceNames;
        string outputResourceNames;
        string requiredResourceNames;
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
        float SpecialistEfficiencyFactor = 0.1f;
        string ExperienceEffect = string.Empty;
        double cycleStartTime = 0;

        ProtoPartSnapshot protoPart;
        ProtoPartModuleSnapshot moduleSnapshot;
        Dictionary<string, List<ProtoPartResourceSnapshot>> protoResources = new Dictionary<string, List<ProtoPartResourceSnapshot>>();
        double productionMultiplier = 1.0f;
        #endregion

        #region Constructors
        public static Dictionary<Vessel, List<WBIBackgroundConverter>> GetBackgroundConverters()
        {
            Dictionary<Vessel, List<WBIBackgroundConverter>> backgroundConverters = new Dictionary<Vessel, List<WBIBackgroundConverter>>();
            List<WBIBackgroundConverter> converters;
            ProtoVessel protoVessel;
            Vessel vessel;
            ProtoPartSnapshot protoPart;
            ProtoPartModuleSnapshot protoModule;
            int partCount;
            int moduleCount;
            bool isActivated;
            bool enableBackgroundProcessing;

            int unloadedCount = FlightGlobals.VesselsUnloaded.Count;
            for (int index = 0; index < unloadedCount; index++)
            {
                vessel = FlightGlobals.VesselsUnloaded[index];
                //Skip vessel types that we're not interested in.
                if (vessel.vesselType == VesselType.Debris ||
                    vessel.vesselType == VesselType.Flag ||
                    vessel.vesselType == VesselType.SpaceObject ||
                    vessel.vesselType == VesselType.Unknown)
                    continue;

                protoVessel = vessel.protoVessel;

                partCount = protoVessel.protoPartSnapshots.Count;
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    protoPart = protoVessel.protoPartSnapshots[partIndex];
                    moduleCount = protoPart.modules.Count;
                    for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
                    {
                        protoModule = protoPart.modules[moduleIndex];
                        if (protoModule.moduleName == "WBIOmniConverter")
                        {
                            //Skip if not a background processor
                            if (protoModule.moduleValues.HasValue("enableBackgroundProcessing"))
                            {
                                enableBackgroundProcessing = false;
                                bool.TryParse(protoModule.moduleValues.GetValue("enableBackgroundProcessing"), out enableBackgroundProcessing);
                                if (!enableBackgroundProcessing)
                                    continue;
                            } else
                            {
                                continue;
                            }

                            //Skip if not active
                            isActivated = false;
                            if (protoModule.moduleValues.HasValue("IsActivated"))
                            {
                                bool.TryParse(protoModule.moduleValues.GetValue("IsActivated"), out isActivated);
                                if (isActivated)
                                {
                                    if (!backgroundConverters.ContainsKey(vessel))
                                        backgroundConverters.Add(vessel, new List<WBIBackgroundConverter>());
                                    converters = backgroundConverters[vessel];

                                    //Create a background converter
                                    converters.Add(new WBIBackgroundConverter(protoPart, protoModule, moduleIndex));
                                    backgroundConverters[vessel] = converters;
                                }
                            }
                        }
                    }
                }
            }

            return backgroundConverters;
        }

        public WBIBackgroundConverter(ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule, int moduleIndex)
        {
            ConfigNode[] moduleNodes;
            ConfigNode node = null;
            string currentTemplateName = string.Empty;

            //OmniConverter can be set up in two different ways:
            //first as a traditional converter like ModuleResourceConverter,
            //and second, using an omni converter template.
            //Additionally, the converter can be built-into the part or included in a dynamic template.
            //So we have three scenarios to cover:
            //1) build into part, no omni template.
            //2) built into part, with omni template.
            //3) dynamic template, with omni template.

            //Record the part and module snapshot.
            this.moduleSnapshot = protoModule;
            this.protoPart = protoPart;

            //Get the config node. Module index must match.
            moduleNodes = protoPart.partInfo.partConfig.GetNodes("MODULE");
            if (moduleIndex <= moduleNodes.Length - 1)
                node = moduleNodes[moduleIndex];

            //If we have a node, then the converter is built into the part. Load its settings.
            if (node != null)
                loadTemplateSettings(node, protoModule);

            //Now check for omni template.
            if (protoModule.moduleValues.HasValue("currentTemplateName"))
            {
                currentTemplateName = protoModule.moduleValues.GetValue("currentTemplateName");

                node = getOmniconverterTemplate(currentTemplateName);
                if (node != null)
                    loadTemplateSettings(node, protoModule);
            }
        }

        public WBIBackgroundConverter()
        {
        }
        #endregion

        #region Converter Operations
        public void CheckRequiredResources(ProtoVessel vessel, double elapsedTime)
        {
            int count = requiredList.Count;
            if (count == 0)
                return;

            ResourceRatio resourceRatio;
            double amount = 0;
            for (int index = 0; index < count; index++)
            {
                resourceRatio = requiredList[index];
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
            int count = inputList.Count;
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
                resourceRatio = inputList[index];

                //Skip EC for simplicity
                if (resourceRatio.ResourceName == "ElectricCharge")
                    continue;

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
                resourceRatio = inputList[index];

                //Skip EC for simplicity
                if (resourceRatio.ResourceName == "ElectricCharge")
                    continue;

                demand = resourceRatio.Ratio * productionMultiplier * elapsedTime;
                requestAmount(resourceRatio.ResourceName, demand, resourceRatio.FlowMode);
            }
        }

        public void ProduceOutputResources(ProtoVessel vessel, double elapsedTime)
        {
            int count = outputList.Count;
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
                resourceRatio = outputList[index];
                supply = resourceRatio.Ratio * productionMultiplier * elapsedTime;
                supplyAmount(resourceRatio.ResourceName, supply, resourceRatio.FlowMode, resourceRatio.DumpExcess);
            }
        }

        public void ProduceYieldResources(ProtoVessel vessel)
        {
            int count = yieldsList.Count;
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
            int resourceCount;
            ProtoPartSnapshot pps;
            ProtoPartResourceSnapshot protoPartResource;
            List<ProtoPartResourceSnapshot> resourceList;

            //Clear our resource map.
            protoResources.Clear();

            for (int index = 0; index < count; index++)
            {
                //Get the proto part snapshot
                pps = vessel.protoPartSnapshots[index];

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
                    if (!string.IsNullOrEmpty(requiredResourceNames) && !skipReources.Contains(protoPartResource.resourceName))
                    {
                        if (requiredResourceNames.Contains(protoPartResource.resourceName))
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
        protected void loadTemplateSettings(ConfigNode node, ProtoPartModuleSnapshot protoModule)
        {
            int count;

            //We've got a config node, but is it a converter? if so, get its resource lists.
            if (node.HasValue("ConverterName"))
            {
                bool.TryParse(protoModule.moduleValues.GetValue("IsActivated"), out IsActivated);
                if (node.HasValue("playerEmailed"))
                    bool.TryParse(moduleSnapshot.moduleValues.GetValue("playerEmailed"), out playerEmailed);

                //Get input resources
                if (node.HasNode("INPUT_RESOURCE"))
                    getConverterResources("INPUT_RESOURCE", inputList, node);
                count = inputList.Count;
                for (int index = 0; index < count; index++)
                    inputResourceNames += inputList[index].ResourceName;

                //Get output resources
                if (node.HasNode("OUTPUT_RESOURCE"))
                    getConverterResources("OUTPUT_RESOURCE", outputList, node);
                count = outputList.Count;
                for (int index = 0; index < count; index++)
                    outputResourceNames += outputList[index].ResourceName;

                //Get required resources
                if (node.HasNode("REQUIRED_RESOURCE"))
                    getConverterResources("YIELD_RESOURCE", requiredList, node);
                count = requiredList.Count;
                for (int index = 0; index < count; index++)
                    requiredResourceNames += requiredList[index].ResourceName;

                //Get yield resources
                if (node.HasNode("YIELD_RESOURCE"))
                    getConverterResources("YIELD_RESOURCE", yieldsList, node);
                count = yieldsList.Count;
                for (int index = 0; index < count; index++)
                    yieldResourceNames += yieldsList[index].ResourceName;

                if (node.HasValue("hoursPerCycle"))
                    double.TryParse(node.GetValue("hoursPerCycle"), out hoursPerCycle);

                if (node.HasValue("minimumSuccess"))
                    float.TryParse(node.GetValue("minimumSuccess"), out minimumSuccess);

                if (node.HasValue("criticalSuccess"))
                    float.TryParse(node.GetValue("criticalSuccess"), out criticalSuccess);

                if (node.HasValue("criticalFail"))
                    float.TryParse(node.GetValue("criticalFail"), out criticalFail);

                if (node.HasValue("criticalSuccessMultiplier"))
                    double.TryParse(node.GetValue("criticalSuccessMultiplier"), out criticalSuccessMultiplier);

                if (node.HasValue("failureMultiplier"))
                    double.TryParse(node.GetValue("failureMultiplier"), out failureMultiplier);

                if (node.HasValue("UseSpecialistBonus"))
                    bool.TryParse(node.GetValue("UseSpecialistBonus"), out UseSpecialistBonus);

                if (node.HasValue("SpecialistBonusBase"))
                    float.TryParse(node.GetValue("SpecialistBonusBase"), out SpecialistBonusBase);

                if (node.HasValue("SpecialistEfficiencyFactor"))
                    float.TryParse(node.GetValue("SpecialistEfficiencyFactor"), out SpecialistEfficiencyFactor);

                if (node.HasValue("ExperienceEffect"))
                    ExperienceEffect = node.GetValue("ExperienceEffect");

                if (protoModule.moduleValues.HasValue("cycleStartTime"))
                    double.TryParse(protoModule.moduleValues.GetValue("cycleStartTime"), out cycleStartTime);
            }
        }

        protected ConfigNode getOmniconverterTemplate(string templateName)
        {
            ConfigNode node = null;
            ConfigNode[] omniconverterNodes = GameDatabase.Instance.GetConfigNodes("OMNICONVERTER");
            string converterName = string.Empty;

            //Get the omniconverter template
            for (int templateIndex = 0; templateIndex < omniconverterNodes.Length; templateIndex++)
            {
                if (omniconverterNodes[templateIndex].HasValue("ConverterName"))
                {
                    converterName = omniconverterNodes[templateIndex].GetValue("ConverterName");
                    if (converterName == templateName)
                    {
                        node = omniconverterNodes[templateIndex];
                        break;
                    }
                }
            }

            return node;
        }

        protected void getConverterResources(string nodeName, List<ResourceRatio> resourceList, ConfigNode node)
        {
            ConfigNode[] resourceNodes;
            ConfigNode resourceNode;
            string resourceName;
            ResourceRatio ratio;

            resourceNodes = node.GetNodes(nodeName);
            for (int resourceIndex = 0; resourceIndex < resourceNodes.Length; resourceIndex++)
            {
                //Resource name
                resourceNode = resourceNodes[resourceIndex];
                if (resourceNode.HasValue("ResourceName"))
                    resourceName = resourceNode.GetValue("ResourceName");
                else
                    resourceName = "";
                //Skip electric charge
                if (resourceName == "ElectricCharge")
                    continue;

                //Ratio
                ratio = new ResourceRatio();
                ratio.ResourceName = resourceName;
                if (resourceNode.HasValue("Ratio"))
                    double.TryParse(resourceNode.GetValue("Ratio"), out ratio.Ratio);

                //Flow mode
                if (resourceNode.HasValue("FlowMode"))
                {
                    switch (resourceNode.GetValue("FlowMode"))
                    {
                        case "NO_FLOW":
                        case "NULL":
                            ratio.FlowMode = ResourceFlowMode.NO_FLOW;
                            break;

                        default:
                            ratio.FlowMode = ResourceFlowMode.ALL_VESSEL;
                            break;
                    }
                }

                //Add to the list
                resourceList.Add(ratio);
            }
        }

        protected void emailPlayer(string resourceName, WBIBackroundEmailTypes emailType)
        {
            StringBuilder resultsMessage = new StringBuilder();
            MessageSystem.Message msg;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            string titleMessage;

            //Spam check
            if (playerEmailed || !WBIMainSettings.EmailConverterResults)
            {
                return;
            }
            else
            {
                playerEmailed = true;
                moduleSnapshot.moduleValues.SetValue("playerEmailed", playerEmailed);
            }

            //From
            resultsMessage.AppendLine("From: " + protoPart.pVesselRef.vesselName);

            switch (emailType)
            {
                case WBIBackroundEmailTypes.missingResources:
                    resourceDef = definitions[resourceName];
                    titleMessage = " needs more resources";
                    resultsMessage.AppendLine("Subject: Missing Resources");
                    resultsMessage.AppendLine("There is no more " + resourceDef.displayName + " available to continue production. Operations cannot continue with the " + ConverterName + " until more resource becomes available.");
                    break;

                case WBIBackroundEmailTypes.missingRequiredResource:
                    resourceDef = definitions[resourceName];
                    titleMessage = " needs a resource";
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
            int count = yieldsList.Count;
            ResourceRatio resourceRatio;
            double supply = 0;

            for (int index = 0; index < count; index++)
            {
                resourceRatio = yieldsList[index];
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
                    //Skip locked resources
                    if (!resourceShapshots[index].flowState)
                        continue;

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
                    //Skip locked resources
                    if (!protoPart.resources[index].flowState)
                        continue;

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
                    if (resourceShapshots[index].flowState)
                        amount += resourceShapshots[index].amount;
                }
            }
            else //Check the part
            {
                count = protoPart.resources.Count;
                for (int index = 0; index < count; index++)
                {
                    if (protoPart.resources[index].resourceName == resourceName && protoPart.resources[index].flowState)
                        return protoPart.resources[index].amount;
                }
            }

            return amount;
        }
        #endregion
    }
}
