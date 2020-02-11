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

    [KSPModule("Multi-Converter")]
    public class WBIMultiConverter : WBIOpsManager
    {
        [KSPField]
        public float productivity = 1.0f;

        [KSPField]
        public float efficiency = 1.0f;

        protected bool canDeploy;

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
            if (HighLogic.LoadedSceneIsEditor)
            {
                canDeploy = true;
                base.ToggleInflation();
                return;
            }

            //Do we pay for resources? If so, either pay the resources if we're deploying the module, or refund the recycled parts
            if (WBIMainSettings.PayToReconfigure)
            {
                //If we aren't deployed then see if we can afford to pay the resource cost.
                if (!isDeployed)
                {
                    //Can we afford it?
                    canDeploy = false;
                    if (canAffordReconfigure(CurrentTemplateName, false) == false)
                        return;

                    //Do we have the skill?
                    if (!hasSufficientSkill(CurrentTemplateName))
                        return;

                    //Yup, we can afford it
                    //Pay the reconfigure cost
                    //reconfigureCost = adjustedPartCost;
                    canDeploy = true;
                    payPartsCost(CurrentTemplateIndex, false);

                    // Toggle after payment.
                    base.ToggleInflation();
                }

                //We are deployed, calculate the amount of parts that can be refunded.
                else
                {
                    // Toggle first in case deflate confirmation is needed, we'll check the state after the toggle.
                    canDeploy = true;
                    base.ToggleInflation();

                    //Recycle what we can.
                    if (deflateConfirmed == false)
                    {
                        //Rebuild input list
                        buildInputList(templateName);

                        string[] keys = inputList.Keys.ToArray();
                        for (int index = 0; index < keys.Length; index++)
                            recoverResourceCost(keys[index], inputList[keys[index]] * recycleBase);
                    }
                }
            }

            // Not paying for reconfiguration, check for skill requirements
            else
            {
                if (WBIMainSettings.RequiresSkillCheck)
                {
                    if (hasSufficientSkill(CurrentTemplateName))
                    {
                        canDeploy = true;
                        base.ToggleInflation();
                    }
                    else
                    {
                        canDeploy = false;
                        return;
                    }
                }

                else
                {
                    canDeploy = true;
                    base.ToggleInflation();
                }
            }
        }

        protected virtual void notEnoughParts()
        {
        }

        #endregion

        #region Helpers
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
            MonoUtilities.RefreshContextWindows(this.part);
        }

        #endregion

    }
}