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
    [KSPModule("Template Converter")]
    public class WBITemplateConverter : ExtendedPartModule
    {
        private const string kNeedAdditionalParts = "Insufficient resources to reconfigure the module. You need an additional {0:f2} {1:s} to reconfigure.";
        private const string kInsufficientParts = "Insufficient resources to reconfigure the module. You need a total of {0:f2} {1:s} to reconfigure.";
        private const string kInsufficientSkill = "Insufficient skill to reconfigure the module.";
        private const string kInsufficientCrew = "Cannot reconfigure. Either crew the module or perform an EVA.";

        //Currently you can support multiple templateNodes, with each node separated by a semicolon.
        //This module requires you to pay to switch between template nodes. For instance, pay to switch between
        //storage and using the storage unit as a battery, or pay to switch between storage and greenhouse.
        [KSPField]
        public string primaryTemplate;

        [KSPField]
        public string secondaryTemplate;

        [KSPField]
        public string primaryTemplateGUIName;

        [KSPField]
        public string secondaryTemplateGUIName;

        [KSPField]
        public string skillRequired;

        [KSPField]
        public string resourceRequired = "";

        [KSPField]
        public float resourceCost;

        [KSPField(isPersistant = true)]
        public bool usePrimaryTemplate = true;

        [KSPField]
        public string techRequired = string.Empty;

        //Should the player pay to reconfigure the module?
        public static bool payForReconfigure = true;

        //Should we check for the required skill to redecorate?
        public static bool checkForSkill = true;

        protected float recycleBase = 0.7f;
        protected float baseSkillModifier = 0.05f;
        protected float reconfigureCost;
        protected float reconfigureCostModifier;
        protected WBIResourceSwitcher switcher;
        protected bool allowSwitch = true;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();

            //Tech check
            updateTemplate();
        }

        [KSPAction("Toggle Template")]
        public virtual void ToggleTemplateAction(KSPActionParam param)
        {
            if (allowSwitch)
                ToggleTemplate();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 3.0f, guiName = "Toggle Template")]
        public void ToggleTemplate()
        {
            if (canAffordReconfigure() && hasSufficientSkill())
            {
                usePrimaryTemplate = !usePrimaryTemplate;

                updateTemplate();
            }
        }

        protected void checkForUpgrade()
        {
            //If the player hasn't unlocked the upgradeTech node yet, then hide the RCS functionality.
            if (ResearchAndDevelopment.Instance != null && !string.IsNullOrEmpty(techRequired))
            {
                //If the tech node hasn't been researched yet then hide the switch capability
                if (ResearchAndDevelopment.GetTechnologyState(techRequired) != RDTech.State.Available)
                {
                    Events["ToggleTemplate"].active = false;
                    allowSwitch = false;
                }
            }
        }

        protected void updateTemplate()
        {
            string templateNodes;

            if (usePrimaryTemplate)
            {
                templateNodes = primaryTemplate;
                Events["ToggleTemplate"].guiName = secondaryTemplateGUIName;
            }

            else
            {
                templateNodes = secondaryTemplate;
                Events["ToggleTemplate"].guiName = primaryTemplateGUIName;
            }

            if (switcher.templateNodes != templateNodes)
            {
                switcher.templateNodes = templateNodes;
                switcher.initTemplates();
                switcher.CurrentTemplateIndex = 0;
                switcher.ReloadTemplate();
            }
        }

        protected bool payPartsCost()
        {
            double amount = resourceCost * (1.0 - reconfigureCostModifier);
            PartResourceDefinition definition = ResourceHelper.DefinitionForResource(resourceRequired);
            double partsPaid = this.part.RequestResource(definition.id, amount, ResourceFlowMode.ALL_VESSEL);

            //Could we afford it?
            if (Math.Abs(partsPaid) / Math.Abs(amount) < 0.999f)
            {
                //Put back what we took
                this.part.RequestResource(definition.id, -partsPaid, ResourceFlowMode.ALL_VESSEL);
                return false;
            }

            return true;
        }

        protected bool canAffordReconfigure()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!payForReconfigure)
                return true;
            bool canAffordCost = false;
            string notEnoughPartsMsg;

            calculateRemodelCostModifier();
            double amount = resourceCost * (1.0 - reconfigureCostModifier);
            double totalResources = ResourceHelper.GetTotalResourceAmount(resourceRequired, this.part.vessel);

            //An inflatable part that hasn't been inflated yet is an automatic pass.
            if (switcher.isInflatable && !switcher.isDeployed)
                return true;

            //now check to make sure the vessel has enough parts.
            if (totalResources >= amount)
                canAffordCost = true;

            if (!canAffordCost)
            {
                notEnoughPartsMsg = string.Format(kInsufficientParts, amount, resourceRequired);
                ScreenMessages.PostScreenMessage(notEnoughPartsMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            return true;
        }

        protected bool hasSufficientSkill()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!checkForSkill)
                return true;
            if (string.IsNullOrEmpty(skillRequired))
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
                Experience.ExperienceTrait experience = vessel.GetVesselCrew()[0].experienceTrait;

                if (experience.TypeName != skillRequired)
                {
                    ScreenMessages.PostScreenMessage(kInsufficientSkill, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }

                calculateRemodelCostModifier(skillRequired);
                return true;
            }

            //Now check the vessel itself
            foreach (ProtoCrewMember protoCrew in this.part.vessel.GetVesselCrew())
            {
                if (protoCrew.experienceTrait.TypeName == skillRequired)
                {
                    hasAtLeastOneCrew = true;
                    break;
                }
            }

            if (!hasAtLeastOneCrew)
            {
                ScreenMessages.PostScreenMessage(kInsufficientSkill, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //Yup, we have sufficient skill.
            calculateRemodelCostModifier(skillRequired);
            return true;
        }

        protected void calculateRemodelCostModifier(string skillRequired = "Engineer")
        {
            int highestLevel = 0;

            //Check for a kerbal on EVA
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                Experience.ExperienceTrait experience = vessel.GetVesselCrew()[0].experienceTrait;

                if (experience.TypeName == skillRequired)
                {
                    reconfigureCostModifier = baseSkillModifier * experience.CrewMemberExperienceLevel();
                    return;
                }
            }

            //No kerbal on EVA. Check the part for the highest ranking kerbal onboard with the required skill.
            if (this.part.CrewCapacity > 0)
            {
                foreach (ProtoCrewMember protoCrew in this.part.protoModuleCrew)
                {
                    if (protoCrew.experienceTrait.TypeName == skillRequired)
                        if (protoCrew.experienceLevel > highestLevel)
                            highestLevel = protoCrew.experienceLevel;
                }
            }

            reconfigureCostModifier = baseSkillModifier * highestLevel;
        }

        protected float calculateRecycleAmount()
        {
            calculateRemodelCostModifier();

            return recycleBase + reconfigureCostModifier;
        }
    }
}
