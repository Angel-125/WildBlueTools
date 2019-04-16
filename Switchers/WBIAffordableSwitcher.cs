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
    public delegate void GetReconfigureResourcesDelegate(float materialModifier);

    public class WBIAffordableSwitcher : WBIModuleSwitcher
    {
        public const string kIgnoreMaterialModField = "ignoreMaterialModifier";
        public const string kReconfigureSkillField = "reconfigureSkill";
        public const string kRequiredResourceField = "requiredResource";
        public const string kRequiredAmountField = "requiredAmount";
        public const string kDefaultSkill = "ConverterSkill";

        public const string kInsufficientParts = "Insufficient resources to reconfigure/assemble the module. You need a total of {0:f2} {1:s} to reconfigure.";
        public const string kInsufficientSkill = "Insufficient skill to reconfigure/assemble the module. You need one of: ";
        public const string kInsufficientCrew = "Cannot reconfigure. Either crew the module or perform an EVA.";

        [KSPField]
        public float materialCostModifier = 1.0f;

        protected float recycleBase = 0.7f;
        protected float baseSkillModifier = 0.05f;
        protected double reconfigureCost;
        protected float reconfigureCostModifier;
        protected string requriredResource;
        protected bool showInsufficientResourcesMsg = true;
        protected Dictionary<string, GetReconfigureResourcesDelegate> reconfigureResourceDelegates = null;
        public Dictionary<string, double> inputList = new Dictionary<string, double>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            updatePartMass();
        }

        public override void UpdateContentsAndGui(int templateIndex)
        {
            base.UpdateContentsAndGui(templateIndex);
            updatePartMass();
        }

        public override void UpdateContentsAndGui(string templateName)
        {
            base.UpdateContentsAndGui(templateName);
            updatePartMass();
        }

        public void AddReconfigureDelegate(string key, GetReconfigureResourcesDelegate reconfigureDelegate)
        {
            if (reconfigureResourceDelegates == null)
                reconfigureResourceDelegates = new Dictionary<string, GetReconfigureResourcesDelegate>();
            if (reconfigureResourceDelegates.ContainsKey(key) == false)
                reconfigureResourceDelegates.Add(key, reconfigureDelegate);
        }

        public void RemoveReconfigureDelegate(string key, GetReconfigureResourcesDelegate reconfigureDelegate)
        {
            if (reconfigureResourceDelegates == null)
                return;
            if (reconfigureResourceDelegates.ContainsKey(key))
                reconfigureResourceDelegates.Remove(key);
        }

        protected virtual void updatePartMass()
        {
            if (CurrentTemplate.HasValue("mass"))
            {
                partMass = float.Parse(CurrentTemplate.GetValue("mass"));
            }

            else
            {
                PartResourceDefinition definition;
                buildInputList(CurrentTemplateName);
                string[] keys = inputList.Keys.ToArray();
                string resourceName;

                partMass = 0;
                for (int index = 0; index < keys.Length; index++)
                {
                    resourceName = keys[index];
                    definition = ResourceHelper.DefinitionForResource(resourceName);

                    partMass += definition.density * (float)inputList[resourceName];
                }
            }
        }

        protected override void recoverResourceCost(string resourceName, double recycleAmount)
        {
            //Do we have sufficient space in the vessel to store the recycled parts?
            double availableStorage = ResourceHelper.GetTotalResourceSpaceAvailable(resourceName, this.part.vessel);

            if (availableStorage < recycleAmount)
            {
                double amountLost = recycleAmount - availableStorage;
                ScreenMessages.PostScreenMessage(string.Format("Module deflated, {0:f2} {1:s} lost due to insufficient storage.", amountLost, resourceName), 5.0f, ScreenMessageStyle.UPPER_CENTER);

                //We'll only recycle what we have room to store.
                recycleAmount = availableStorage;
            }

            //Yup, we have the space
            this.part.RequestResource(resourceName, -recycleAmount, ResourceFlowMode.ALL_VESSEL);
        }

        protected override bool payPartsCost(int templateIndex, bool deflatedModulesAutoPass = true)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;
            if (inputList.Keys.Count == 0)
            {
                Log("No resources to pay");
                return true;
            }
            if (isInflatable && !isDeployed && deflatedModulesAutoPass)
                return true;

            //Double check that we have enough resources
            string[] keys = inputList.Keys.ToArray();
            string resourceName;
            double currentAmount, maxAmount;
            PartResourceDefinition resourceDefiniton;
            for (int index = 0; index < keys.Length; index++)
            {
                resourceName = keys[index];

                resourceDefiniton = ResourceHelper.DefinitionForResource(resourceName);
                this.part.vessel.resourcePartSet.GetConnectedResourceTotals(resourceDefiniton.id, out currentAmount, out maxAmount, true);

                if (currentAmount < inputList[resourceName])
                {
                    Log("Not enough " + resourceName);
                    return false;
                }
            }

            //Now pay the cost.
            for (int index = 0; index < keys.Length; index++)
            {
                resourceName = keys[index];
                this.part.RequestResource(resourceName, inputList[resourceName]);
                Log("Paid " + inputList[resourceName] + " units of " + resourceName);
            }

            return true;
        }

        public void buildInputList(string templateName)
        {
            //Calculate material modifier
            string requiredSkill = kDefaultSkill;
            float materialModifier = materialCostModifier;
            if (templateManager[templateName] == null)
                return;
            if (templateManager[templateName].HasValue(kReconfigureSkillField))
                requiredSkill = templateManager[templateName].GetValue(kReconfigureSkillField);
            if (templateManager[templateName].HasValue(kIgnoreMaterialModField))
                materialModifier = 1.0f;

            //Calculate remodel cost modifier
            calculateRemodelCostModifier(requiredSkill);

            //Get the legacy cost values first.
            inputList.Clear();
            ConfigNode templateNode = templateManager[templateName];
            string resourceName;
            double amount;
            if (templateNode.HasValue(kRequiredResourceField) && templateNode.HasValue(kRequiredAmountField))
            {
                resourceName = templateNode.GetValue(kRequiredResourceField);
                amount = double.Parse(templateNode.GetValue(kRequiredAmountField)) * materialModifier * reconfigureCostModifier;
                inputList.Add(resourceName, amount);
            }

            //Add the input list (if any)
            if (templateNode.HasNode("INPUT_RESOURCE"))
            {
                ConfigNode[] inputs = templateNode.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < inputs.Length; index++)
                {
                    resourceName = inputs[index].GetValue("ResourceName");
                    amount = double.Parse(inputs[index].GetValue("Ratio")) * materialModifier * reconfigureCostModifier;
                    if (inputList.ContainsKey(resourceName))
                        inputList[resourceName] = inputList[resourceName] + amount;
                    else
                        inputList.Add(resourceName, amount);
                }
            }

            //Check our part for input resources as well
            if (this.part.partInfo.partConfig == null)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode node = null;
            string moduleName;
            List<string> optionNamesList = new List<string>();

            //Get the config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        if (node.HasNode("INPUT_RESOURCE"))
                        {
                            ConfigNode[] buildResourceNodes = node.GetNodes("INPUT_RESOURCE");
                            for (int resourceIndex = 0; resourceIndex < buildResourceNodes.Length; resourceIndex++)
                            {
                                resourceName = buildResourceNodes[resourceIndex].GetValue("ResourceName");
                                amount = double.Parse(buildResourceNodes[resourceIndex].GetValue("Ratio")) * materialModifier * reconfigureCostModifier;
                                if (inputList.ContainsKey(resourceName))
                                    inputList[resourceName] = inputList[resourceName] + amount;
                                else
                                    inputList.Add(resourceName, amount);
                            }
                        }
                        break;
                    }
                }
            }

            //Ask listeners to contribute their resource requirements
            if (reconfigureResourceDelegates != null)
            {
                int count = reconfigureResourceDelegates.Keys.Count;
                string[] keys = reconfigureResourceDelegates.Keys.ToArray();
                GetReconfigureResourcesDelegate reconfigureDelegate;
                for (int index = 0; index < count; index++)
                {
                    reconfigureDelegate = reconfigureResourceDelegates[keys[index]];
                    reconfigureDelegate(materialModifier);
                }
            }
        }

        public virtual bool payForReconfigure(string resourceName, double resourceCost)
        {
            double amountPaid = this.part.RequestResource(resourceName, resourceCost);

            if (amountPaid >= resourceCost)
                Log("Paid " + resourceCost + " units of " + resourceName);
            else
                return false;

            return true;
        }

        public virtual bool canAffordResource(string resourceName, double resourceCost, bool deflatedModulesAutoPass = true)
        {
            PartResourceDefinition resourceDefiniton;
            double currentAmount, maxAmount;
            if (isInflatable && !isDeployed && deflatedModulesAutoPass)
                return true;

            resourceDefiniton = ResourceHelper.DefinitionForResource(resourceName);
            this.part.vessel.resourcePartSet.GetConnectedResourceTotals(resourceDefiniton.id, out currentAmount, out maxAmount, true);

            if (currentAmount >= resourceCost)
                return true;
            else
                return false;
        }

        protected override bool canAffordReconfigure(string templateName, bool deflatedModulesAutoPass = true)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;
            if (isInflatable && !isDeployed && deflatedModulesAutoPass)
                return true;

            //Build the input list
            buildInputList(templateName);
            int totalKeys = inputList.Keys.Count;
            if (totalKeys == 0)
            {
                reconfigureCost = 0f;
                Log("canAffordReconfigure: no resources so we can afford it.");
                return true;
            }

            //Go through the input list and see if we can affort the resources
            double currentAmount, maxAmount;
            string resourceName;
            string[] keys = inputList.Keys.ToArray();
            PartResourceDefinition resourceDefiniton;
            for (int index = 0; index < totalKeys; index++)
            {
                resourceName = keys[index];
                resourceDefiniton = ResourceHelper.DefinitionForResource(resourceName);
                this.part.vessel.resourcePartSet.GetConnectedResourceTotals(resourceDefiniton.id, out currentAmount, out maxAmount, true);

                if (currentAmount < inputList[resourceName])
                {
                    if (showInsufficientResourcesMsg)
                    {
                        string notEnoughPartsMsg = string.Format(kInsufficientParts, inputList[resourceName], resourceName);
                        ScreenMessages.PostScreenMessage(notEnoughPartsMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    return false;
                }
            }

            Log("We can afford the reconfigure.");
            return true;
        }

        protected override bool hasSufficientSkill(string templateName)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.RequiresSkillCheck)
                return true;
            if (templateManager[templateName] == null)
                return true;
            string skillRequired = templateManager[templateName].GetValue(kReconfigureSkillField);
            if (string.IsNullOrEmpty(skillRequired))
                return true;
            bool hasAtLeastOneCrew = false;

            if (string.IsNullOrEmpty(skillRequired))
            {
                calculateRemodelCostModifier();
                return true;
            }

            //Make sure we have an experienced person either out on EVA performing the reconfiguration, or inside the module.
            //Check EVA first
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                ProtoCrewMember astronaut = vessel.GetVesselCrew()[0];

                if (astronaut.HasEffect(skillRequired) == false)
                {
                    string[] traits = Utils.GetTraitsWithEffect(skillRequired);
                    StringBuilder traitsList = new StringBuilder();
                    foreach (string trait in traits)
                        traitsList.Append(trait + ",");

                    ScreenMessages.PostScreenMessage(kInsufficientSkill + traitsList.ToString().TrimEnd(new char[] {','}), 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }

                calculateRemodelCostModifier(skillRequired);
                return true;
            }

            //Now check the vessel
            foreach (ProtoCrewMember protoCrew in this.part.vessel.GetVesselCrew())
            {
                if (protoCrew.HasEffect(skillRequired))
                {
                    hasAtLeastOneCrew = true;
                    break;
                }
            }

            if (!hasAtLeastOneCrew)
            {
                ScreenMessages.PostScreenMessage(kInsufficientCrew, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //Yup, we have sufficient skill.
            calculateRemodelCostModifier(skillRequired);
            return true;
        }

        protected void calculateRemodelCostModifier(string skillRequired = kDefaultSkill)
        {
            int highestLevel = 0;
            if (HighLogic.LoadedSceneIsFlight == false)
            {
                reconfigureCostModifier = 1.0f;
                return;
            }

            //Check for a kerbal on EVA
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                ProtoCrewMember astronaut = vessel.GetVesselCrew()[0];

                if (astronaut.HasEffect(skillRequired))
                {
                    reconfigureCostModifier = 1 - (baseSkillModifier * astronaut.experienceTrait.CrewMemberExperienceLevel());
                    return;
                }
            }

            //No kerbal on EVA. Check the vessel for the highest ranking kerbal onboard with the required skill.
            foreach (ProtoCrewMember protoCrew in this.vessel.GetVesselCrew())
            {
                if (protoCrew.HasEffect(skillRequired))
                    if (protoCrew.experienceLevel > highestLevel)
                        highestLevel = protoCrew.experienceLevel;
            }

            reconfigureCostModifier = 1 - (baseSkillModifier * highestLevel);
        }

        protected float calculateRecycleAmount()
        {
            calculateRemodelCostModifier();

            return 0f;
        }
    }
}
