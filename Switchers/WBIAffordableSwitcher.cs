using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIAffordableSwitcher : WBIModuleSwitcher
    {
        public const string kIgnoreMaterialModField = "ignoreMaterialModifier";
        public const string kReconfigureSkillField = "reconfigureSkill";
        public const string kRequiredResourceField = "requiredResource";
        public const string kRequiredAmountField = "requiredAmount";
        public const string kDefaultSkill = "ConverterSkill";

        private const string kInsufficientParts = "Insufficient resources to reconfigure/assemble the module. You need a total of {0:f2} {1:s} to reconfigure.";
        private const string kInsufficientSkill = "Insufficient skill to reconfigure/assemble the module. You need one of: ";
        private const string kInsufficientCrew = "Cannot reconfigure. Either crew the module or perform an EVA.";

        [KSPField]
        public float materialCostModifier = 1.0f;

        protected float recycleBase = 0.7f;
        protected float baseSkillModifier = 0.05f;
        protected double reconfigureCost;
        protected float reconfigureCostModifier;
        protected string requriredResource;

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

        protected override bool payPartsCost(int templateIndex)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;
            if (reconfigureCost == 0f)
                return true;
            if (!templateManager[templateIndex].HasValue(kRequiredResourceField))
                return true;

            float remodelCost = calculateRemodelCost(templateIndex);
            string resourceName = templateManager[templateIndex].GetValue(kRequiredResourceField);
            PartResourceDefinition definition = ResourceHelper.DefinitionForResource(resourceName);
            double partsPaid = this.part.RequestResource(definition.id, remodelCost, ResourceFlowMode.ALL_VESSEL);

            //Could we afford it?
            if (Math.Abs(partsPaid) / Math.Abs(reconfigureCost) < 0.999f)
            {
                //Put back what we took
                this.part.RequestResource(definition.id, -partsPaid, ResourceFlowMode.ALL_VESSEL);
                return false;
            }

            return true;
        }

        protected float calculateRemodelCost(int templateIndex)
        {
            //string value;
            string requiredAmount = templateManager[templateIndex].GetValue(kRequiredAmountField);
            float remodelCost = 0f;
            string requiredSkill = kDefaultSkill;
            float materialModifier = materialCostModifier;

            if (templateManager[templateIndex].HasValue(kReconfigureSkillField))
                requiredSkill = templateManager[templateIndex].GetValue(kReconfigureSkillField);
            calculateRemodelCostModifier(requiredSkill);

            if (templateManager[templateIndex].HasValue(kIgnoreMaterialModField))
                materialModifier = 1.0f;

            if (string.IsNullOrEmpty(requiredAmount) == false)
            {
                remodelCost = float.Parse(requiredAmount) * materialModifier * reconfigureCostModifier;
            }

            return remodelCost;
        }

        protected override bool canAffordReconfigure(string templateName, bool deflatedModulesAutoPass = true)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;
            //string value;
            bool canAffordCost = false;
            string requiredName = templateManager[templateName].GetValue(kRequiredResourceField);
            string requiredSkill = kDefaultSkill;
            float materialModifier = materialCostModifier;

            if (templateManager[templateName].HasValue(kReconfigureSkillField))
                requiredSkill = templateManager[templateName].GetValue(kReconfigureSkillField);
            calculateRemodelCostModifier(requiredSkill);

            //If we don't have the required resource defined in the template then we can
            //automatically afford to reconfigure.
            if (templateManager[templateName].HasValue(kRequiredAmountField) == false)
            {
                reconfigureCost = 0f;
                return true;
            }

            if (templateManager[templateName].HasValue(kIgnoreMaterialModField))
                materialModifier = 1.0f;

            requriredResource = templateManager[templateName].GetValue(kRequiredAmountField);
            if (string.IsNullOrEmpty(requriredResource) == false)
            {
                //An inflatable part that hasn't been inflated yet is an automatic pass.
                if ((isInflatable && !isDeployed) && deflatedModulesAutoPass)
                    return true;

                reconfigureCost = float.Parse(requriredResource) * materialModifier * reconfigureCostModifier;
                double totalResources = ResourceHelper.GetTotalResourceAmount(requiredName, this.part.vessel);

                //now check to make sure the vessel has enough parts.
                if (totalResources < reconfigureCost)
                    canAffordCost =  false;

                else
                    canAffordCost = true;
            }

            if (!canAffordCost)
            {
                string notEnoughPartsMsg = string.Format(kInsufficientParts, reconfigureCost, requiredName);
                ScreenMessages.PostScreenMessage(notEnoughPartsMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            return true;
        }

        protected override bool hasSufficientSkill(string templateName)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.RequiresSkillCheck)
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
                    ScreenMessages.PostScreenMessage(kInsufficientSkill + Utils.GetTraitsWithEffect(skillRequired), 5.0f, ScreenMessageStyle.UPPER_CENTER);
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
