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

    [KSPModule("Multi-Converter")]
    public class WBIMultiConverter : WBIOpsManager
    {
        [KSPField]
        public float productivity = 1.0f;

        [KSPField]
        public float efficiency = 1.0f;

        #region Module Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            opsManagerView.WindowTitle = this.part.partInfo.title + " Operations";
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Show/hide the inflate/deflate button depending upon whether or not crew is aboard
            if (isInflatable && HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.protoModuleCrew.Count() > 0)
                {
                    Events["ToggleInflation"].guiActive = false;
                    Events["ToggleInflation"].guiActiveUnfocused = false;
                }

                else
                {
                    Events["ToggleInflation"].guiActive = true;
                    Events["ToggleInflation"].guiActiveUnfocused = true;
                }
            }
        }

        public override void ToggleInflation()
        {
            if (CurrentTemplate.HasValue("requiredResource") == false)
            {
                base.ToggleInflation();
                return;
            }

            string requiredName = CurrentTemplate.GetValue("requiredResource");
            string requiredAmount = CurrentTemplate.GetValue("requiredAmount");
            float totalResources = (float)ResourceHelper.GetTotalResourceAmount(requiredName, this.part.vessel);

            if (string.IsNullOrEmpty(requiredAmount))
            {
                base.ToggleInflation();
                return;
            }
            float resourceCost = float.Parse(requiredAmount);

            calculateRemodelCostModifier();
            float adjustedPartCost = resourceCost;
            if (reconfigureCostModifier > 0f)
                adjustedPartCost *= reconfigureCostModifier;

            //Do we pay for resources? If so, either pay the resources if we're deploying the module, or refund the recycled parts
            if (payForReconfigure)
            {
                //If we aren't deployed then see if we can afford to pay the resource cost.
                if (!isDeployed)
                {
                    //Can we afford it?
                    if (canAffordReconfigure(CurrentTemplateName, false) == false)
                        return;
                    /*
                    if (totalResources < adjustedPartCost)
                    {
                        notEnoughParts();
                        string notEnoughPartsMsg = string.Format("Insufficient resources to assemble the module. You need a total of {0:f2} " + requiredName + " to assemble.", resourceCost);
                        ScreenMessages.PostScreenMessage(notEnoughPartsMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                     */

                    //Yup, we can afford it
                    //Pay the reconfigure cost
                    //reconfigureCost = adjustedPartCost;
                    payPartsCost(CurrentTemplateIndex);

                    // Toggle after payment.
                    base.ToggleInflation();
                }

                //We are deployed, calculate the amount of parts that can be refunded.
                else
                {
                    // Toggle first in case deflate confirmation is needed, we'll check the state after the toggle.
                    base.ToggleInflation();

                    // deflateConfirmed's logic seems backward.
                    if (!HasResources() || (HasResources() && deflateConfirmed == false))
                    {
                        // The part came from the factory configured which represents an additional resource cost. If reconfigured in the field, the difference was paid at
                        // that time. Deflating doesn't remove any functionality, so no refund beyond the original adjusted part cost.
                        double recycleAmount = adjustedPartCost;

                        //Do we have sufficient space in the vessel to store the recycled parts?
                        double availableStorage = ResourceHelper.GetTotalResourceSpaceAvailable(requiredName, this.part.vessel);

                        if (availableStorage < recycleAmount)
                        {
                            double amountLost = recycleAmount - availableStorage;
                            ScreenMessages.PostScreenMessage(string.Format("Module deflated, {0:f2} {1:s} lost due to insufficient storage.", amountLost, requiredName), 5.0f, ScreenMessageStyle.UPPER_CENTER);

                            //We'll only recycle what we have room to store.
                            recycleAmount = availableStorage;
                        }

                        //Yup, we have the space
                        reconfigureCost = -recycleAmount;
                        payPartsCost(CurrentTemplateIndex);
                    }
                }
            }

            // Not paying for reconfiguration, check for skill requirements
            else
            {
                if (checkForSkill)
                {
                    if (hasSufficientSkill(CurrentTemplateName))
                        base.ToggleInflation();
                    else
                        return;
                }

                else
                {
                    base.ToggleInflation();
                }
            }
        }

        protected virtual void notEnoughParts()
        {
        }

        #endregion

        #region Helpers
        protected string getTemplateCost(int templateIndex)
        {
            if (templateManager[templateIndex].HasValue("requiredAmount"))
            {
                float cost = calculateRemodelCost(templateIndex);
                return string.Format("{0:f2}", cost);
            }
            else
                return "0";
        }

        protected override void loadModulesFromTemplate(ConfigNode templateNode)
        {
            base.loadModulesFromTemplate(templateNode);

            List<ModuleResourceConverter> converters = this.part.FindModulesImplementing<ModuleResourceConverter>();
            foreach (ModuleResourceConverter converter in converters)
            {
                if (converter is WBIBasicScienceLab == false)
                    runHeadless(converter);
            }
            opsManagerView.UpdateButtonTabs();
        }

        public override void UpdateContentsAndGui(int templateIndex)
        {
            base.UpdateContentsAndGui(templateIndex);

            //Update productivity and efficiency
            updateProductivity();
        }

        protected virtual void updateProductivity()
        {
            //Find all the resource converters and set their productivity
            List<ModuleResourceConverter> resourceConverters = this.part.FindModulesImplementing<ModuleResourceConverter>();
            ModuleResourceConverter[] converters = resourceConverters.ToArray();
            ModuleResourceConverter converter;
            ResourceRatio[] outputRatios;

            for (int index = 0; index < converters.Length; index++)
            {
                converter = converters[index];
                converter.Efficiency = efficiency;

                //Now adjust the output.
                outputRatios = converter.outputList.ToArray();
                for (int ratioIndex = 0; ratioIndex < outputRatios.Length; ratioIndex++)
                    outputRatios[ratioIndex].Ratio *= productivity;
            }
        }

        protected virtual void runHeadless(ModuleResourceConverter converter)
        {
            foreach (BaseEvent baseEvent in converter.Events)
            {
                baseEvent.guiActive = false;
                baseEvent.guiActiveEditor = false;
            }

            foreach (BaseField baseField in converter.Fields)
            {
                baseField.guiActive = false;
                baseField.guiActiveEditor = false;
            }

            //Dirty the GUI
            UIPartActionWindow tweakableUI = Utils.FindActionWindow(this.part);
            if (tweakableUI != null)
                tweakableUI.displayDirty = true;
        }

        #endregion

    }
}