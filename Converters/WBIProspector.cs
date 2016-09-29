using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIProspector : ModuleResourceConverter
    {
        [KSPField]
        public string byproduct = string.Empty;

        [KSPField]
        public float byproductMinPercent;

        [KSPField]
        public string ignoreResources = string.Empty;

        protected float inputMass;
        protected float byproductMass;
        protected float yieldMass;
        protected PartResourceDefinition byproductDef = null;
        protected string inputSources = string.Empty;
        protected string currentBiome = string.Empty;

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.SPLASHED)
                    currentBiome = Utils.GetCurrentBiome(this.part.vessel).name;
                prepareOutputs();
            }

            base.OnStart(state);
        }

        public override string GetInfo()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.vessel.situation == Vessel.Situations.SPLASHED || this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    if (Utils.IsBiomeUnlocked(this.part.vessel) == false)
                    {
                        outputList.Clear();
                        this.status = "Analyze biome";
                        StopResourceConverter();
                        return base.GetInfo() + "Unknown, unlock biome to get outputs.";
                    }

                    if (outputList.Count == 0)
                        prepareOutputs();
                }

                return base.GetInfo();
            }
            else
            {
                outputList.Clear();
                return base.GetInfo() + "Varies depending upon location.";
            }
        }

        protected void prepareOutputs()
        {
//            Debug.Log("FRED prepareOutputs called");
            PartResourceDefinition inputDef = null;

            //Get the input mass from the list of inputs. Ignore ElectricCharge.
            foreach (ResourceRatio input in inputList)
            {
                if (input.ResourceName != "ElectricCharge")
                {
                    inputDef = ResourceHelper.DefinitionForResource(input.ResourceName);
                    inputMass += (float)input.Ratio * inputDef.density;
                    inputSources += input.ResourceName + ";";
                }
            }
            if (!string.IsNullOrEmpty(byproduct))
            {
                byproductDef = ResourceHelper.DefinitionForResource(byproduct);
                byproductMass = inputMass * (byproductMinPercent / 100.0f);
                yieldMass = inputMass - byproductMass;
            }
            else
            {
                yieldMass = inputMass;
            }

//            Debug.Log("FRED inputMass: " + inputMass);
//            Debug.Log("FRED byproductMass: " + byproductMass);
//            Debug.Log("FRED yieldMass: " + yieldMass);

            prepareOutputsByLocale();
        }

        protected virtual void prepareOutputsByLocale()
        {
//            Debug.Log("FRED prepareOutputsByLocale called");
            ResourceRatio outputSource = null;
            string biomeName = Utils.GetCurrentBiome(this.part.vessel).name;
            PartResourceDefinition outputDef = null;
            float totalAbundance = 0f;
            float abundance = 0f;
            float outputMass = 0f;
            float outputUnits = 0f;
            IEnumerable<ResourceCache.AbundanceSummary> abundanceCache = ResourceCache.Instance.AbundanceCache.
                Where(a => a.HarvestType == HarvestTypes.Planetary && a.BodyId == this.part.vessel.mainBody.flightGlobalsIndex && a.BiomeName == biomeName);

            foreach (ResourceCache.AbundanceSummary summary in abundanceCache)
            {
//                Debug.Log("FRED checking " + summary.ResourceName);
                outputDef = ResourceHelper.DefinitionForResource(summary.ResourceName);
                abundance = summary.Abundance;
                outputMass = abundance * yieldMass;
                outputUnits = outputMass / outputDef.density;

                //If the resource is an input resource then add its output mass to the byproductMass.
                if (inputSources.Contains(summary.ResourceName))
                {
//                    Debug.Log("FRED " + summary.ResourceName + " added to byproductMass");
                    byproductMass += outputMass;
                }

                //If the resource is on our ignore list, then add the output mass to the byproductMass.
                else if (!string.IsNullOrEmpty(ignoreResources) && ignoreResources.Contains(summary.ResourceName))
                {
//                    Debug.Log("FRED " + summary.ResourceName + " ignored and added to byproductMass");
                    byproductMass += outputMass;
                }

                //Legit!
                else if (summary.Abundance > 0.001f)
                {
                    totalAbundance += abundance;
//                    Debug.Log("FRED " + summary.ResourceName + " abundance: " + abundance + " Ratio: " + outputUnits);
                    outputSource = new ResourceRatio { ResourceName = summary.ResourceName, Ratio = outputUnits, FlowMode = ResourceFlowMode.ALL_VESSEL_BALANCE, DumpExcess = true };
                    outputList.Add(outputSource);
                }
            }

            //Leftovers
            if (!string.IsNullOrEmpty(byproduct))
            {
                byproductMass += (1.0f - totalAbundance) * yieldMass;
                outputUnits = byproductMass / byproductDef.density;
                outputSource = new ResourceRatio { ResourceName = byproduct, Ratio = outputUnits, FlowMode = ResourceFlowMode.ALL_VESSEL_BALANCE, DumpExcess = true };
                outputList.Add(outputSource);
//                Debug.Log("FRED added " + byproduct + " to output list");
            }

//            Debug.Log("FRED totalAbundance: " + totalAbundance);
//            Debug.Log("FRED Slag Units: " + outputUnits);
//            Debug.Log("FRED output resources added: " + outputList.Count);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            //Has the biome been unlocked?
            if (Utils.IsBiomeUnlocked(this.part.vessel) == false)
            {
                this.status = "Analyze biome";
                StopResourceConverter();
                return base.PrepareRecipe(deltatime);
            }

            //Watch for biome changes.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.SPLASHED)
                {
                    string biomName = Utils.GetCurrentBiome(this.part.vessel).name;
                    if (biomName != currentBiome)
                    {
                        currentBiome = biomName;
                        outputList.Clear();
                        prepareOutputs();
                    }
                }
            }

            return base.PrepareRecipe(deltatime);
        }
    }
}
