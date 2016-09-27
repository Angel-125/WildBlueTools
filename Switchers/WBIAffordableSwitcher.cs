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
        private const string kInsufficientParts = "Insufficient resources to reconfigure/assemble the module. You need a total of {0:f2} {1:s} to reconfigure.";
        private const string kInsufficientSkill = "Insufficient skill to reconfigure/assemble the module.";
        private const string kInsufficientCrew = "Cannot reconfigure. Either crew the module or perform an EVA.";

        [KSPField]
        public float materialCostModifier = 1.0f;

        //Should the player pay to reconfigure the module?
        public static bool payForReconfigure = true;

        //Should we check for the required skill to redecorate?
        public static bool checkForSkill = true;

        protected float recycleBase = 0.7f;
        protected float baseSkillModifier = 0.05f;
        protected double reconfigureCost;
        protected float reconfigureCostModifier;
        protected string requriredResource;

        protected override bool payPartsCost(int templateIndex)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!payForReconfigure)
                return true;
            if (reconfigureCost == 0f)
                return true;
            if (!templateManager[templateIndex].HasValue("requiredResource"))
                return true;

            float remodelCost = calculateRemodelCost(templateIndex);
            string resourceName = templateManager[templateIndex].GetValue("requiredResource");
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
            string requiredAmount = templateManager[templateIndex].GetValue("requiredAmount");
            float remodelCost = 0f;
            string requiredSkill = "Engineer";
            float materialModifier = materialCostModifier;

            if (templateManager[templateIndex].HasValue("requiredSkill"))
                requiredSkill = templateManager[templateIndex].GetValue("requiredSkill");
            calculateRemodelCostModifier(requiredSkill);

            if (templateManager[templateIndex].HasValue("ignoreMaterialModifier"))
                materialModifier = 1.0f;

            if (string.IsNullOrEmpty(requiredAmount) == false)
            {
                remodelCost = float.Parse(requiredAmount) * materialModifier * reconfigureCostModifier;

                /*
                //Get the current template's rocket part cost.
                value = CurrentTemplate.GetValue("requiredAmount");
                if (string.IsNullOrEmpty(value) == false)
                {
                    float recycleAmount = float.Parse(value) * materialCostModifier;

                    //calculate the amount of parts that we can recycle.
                    recycleAmount *= calculateRecycleAmount();

                    //Now recalculate rocketPartCost, accounting for the parts we can recycle.
                    //A negative value means we'll get parts back, a positive number means we pay additional parts.
                    //Ex: current configuration takes 1200 parts. new configuration takes 900.
                    //We recycle 90% of the current configuration (1080 parts).
                    //The reconfigure cost is: 900 - 1080 = -180 parts
                    //If we reverse the numbers so new configuration takes 1200: 1200 - (900 * .9) = 390
                    remodelCost = reconfigureAmount - recycleAmount;
                }
                 */
            }

            return remodelCost;
        }

        protected override bool canAffordReconfigure(string templateName, bool deflatedModulesAutoPass = true)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!payForReconfigure)
                return true;
            //string value;
            bool canAffordCost = false;
            string requiredName = templateManager[templateName].GetValue("requiredResource");
            string requiredSkill = "Engineer";
            float materialModifier = materialCostModifier;

            if (templateManager[templateName].HasValue("requiredSkill"))
                requiredSkill = templateManager[templateName].GetValue("requiredSkill");
            calculateRemodelCostModifier(requiredSkill);

            //If we don't have the required resource defined in the template then we can
            //automatically afford to reconfigure.
            if (templateManager[templateName].HasValue("requiredAmount") == false)
            {
                reconfigureCost = 0f;
                return true;
            }

            if (templateManager[templateName].HasValue("ignoreMaterialModifier"))
                materialModifier = 1.0f;

            requriredResource = templateManager[templateName].GetValue("requiredAmount");
            if (string.IsNullOrEmpty(requriredResource) == false)
            {
                //An inflatable part that hasn't been inflated yet is an automatic pass.
                if ((isInflatable && !isDeployed) && deflatedModulesAutoPass)
                    return true;

                reconfigureCost = float.Parse(requriredResource) * materialModifier * reconfigureCostModifier;
                double totalResources = ResourceHelper.GetTotalResourceAmount(requiredName, this.part.vessel);

                /*
                //Get the current template's rocket part cost.
                value = CurrentTemplate.GetValue("requiredAmount");
                if (string.IsNullOrEmpty(value) == false)
                {
                    float recycleAmount = float.Parse(value) * materialCostModifier;

                    //calculate the amount of parts that we can recycle.
                    recycleAmount *= calculateRecycleAmount();

                    //Now recalculate rocketPartCost, accounting for the parts we can recycle.
                    //A negative value means we'll get parts back, a positive number means we pay additional parts.
                    //Ex: current configuration takes 1200 parts. new configuration takes 900.
                    //We recycle 90% of the current configuration (1080 parts).
                    //The reconfigure cost is: 900 - 1080 = -180 parts
                    //If we reverse the numbers so new configuration takes 1200: 1200 - (900 * .9) = 390
                    reconfigureCost = reconfigureAmount - recycleAmount;
                }
                 */

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
            if (!checkForSkill)
                return true;
            string skillRequired = templateManager[templateName].GetValue("reconfigureSkill");
            if (string.IsNullOrEmpty(skillRequired))
                return true;
            if (Utils.IsExperienceEnabled() == false)
                return true;
            bool hasAtLeastOneCrew = false;

            //Tearing down the current configuration returns 70% of the current configuration's resource, plus 5% per skill point
            //of the highest ranking kerbal in the module with the appropriate skill required to reconfigure, or 5% per skill point
            //of the kerbal on EVA if the kerbal has the required skill.
            //If anybody can reconfigure the module to the desired template, then get the highest ranking Engineer and apply his/her skill bonus.
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
                    ScreenMessages.PostScreenMessage(kInsufficientSkill, 5.0f, ScreenMessageStyle.UPPER_CENTER);
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

        protected void calculateRemodelCostModifier(string skillRequired = "ConverterSkill")
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
            //return recycleBase + reconfigureCostModifier;
        }
    }
}
