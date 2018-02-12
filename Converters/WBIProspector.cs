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

        [KSPField]
        public bool guiVisible = false;

        [KSPField]
        public string harvestTypes = "Planetary";

        [KSPField]
        public string outputsGuiName = "Show Resource Outputs";

        [KSPField(isPersistant = true)]
        public int previousSituationID = -1;

        protected float inputMass;
        protected float byproductMass;
        protected float yieldMass;
        protected PartResourceDefinition byproductDef = null;
        protected string inputSources = string.Empty;
        protected string currentBiome = string.Empty;
        protected InfoView infoView = new InfoView();
        HarvestTypes[] harvestEnvironments;

        protected void Log(string message)
        {
            if (WBIMainSettings.EnableDebugLogging == false)
                return;

            Debug.Log("[WBIProspector] - " + message);
        }

        [KSPEvent(guiActive = false, guiName = "Show Resource Outputs")]
        public void GetModuleInfo()
        {
            if (outputList.Count == 0)
                prepareOutputs();

            infoView.WindowTitle = "Prospector";
            infoView.ModuleInfo = base.GetInfo();
            infoView.SetVisible(true);
        }

        public override void OnStart(StartState state)
        {
            Events["GetModuleInfo"].guiActive = guiVisible;
            Events["GetModuleInfo"].guiName = outputsGuiName;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.vessel.situation == Vessel.Situations.LANDED || 
                    this.part.vessel.situation == Vessel.Situations.SPLASHED || 
                    this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                    currentBiome = Utils.GetCurrentBiome(this.part.vessel).name;

                //Get the allowed harvest types
                string[] types = harvestTypes.Split(new char[] { ';' });
                HarvestTypes harvestType = HarvestTypes.Planetary;
                List<HarvestTypes> typeList = new List<HarvestTypes>();
                for (int index = 0; index < types.Length; index++)
                {
                    harvestType = (HarvestTypes)Enum.Parse(typeof(HarvestTypes), types[index]);
                    typeList.Add(harvestType);
                }
                harvestEnvironments = typeList.ToArray();

                prepareOutputs();
            }

            base.OnStart(state);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight)
            {
                int situationID = (int)this.part.vessel.situation;
                if (situationID != previousSituationID)
                {
                    previousSituationID = situationID;
                    prepareOutputs();
                }

                //Watch for biome changes
            }
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

        protected virtual void prepareOutputs()
        {
            Log("prepareOutputs called");
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

            Log("inputMass: " + inputMass);
            Log("byproductMass: " + byproductMass);
            Log("yieldMass: " + yieldMass);

            outputList.Clear();
            prepareOutputsByLocale();
        }

        protected virtual void prepareOutputsByLocale()
        {
            for (int index = 0; index < harvestEnvironments.Length; index++)
            {
                switch (harvestEnvironments[index])
                {
                    default:
                    case HarvestTypes.Planetary:
                        if (this.part.Landed)
                            prepareOutputsByLocale(HarvestTypes.Planetary);
                        break;

                    case HarvestTypes.Oceanic:
                        if (this.part.Splashed)
                            prepareOutputsByLocale(HarvestTypes.Oceanic);
                        break;

                    case HarvestTypes.Atmospheric:
                        if (this.part.vessel.mainBody.atmosphere && this.part.vessel.atmDensity > 0.0f)
                            prepareOutputsByLocale(HarvestTypes.Atmospheric);
                        break;

                    case HarvestTypes.Exospheric:
                            if (this.part.vessel.atmDensity <= 0.0f)
                                prepareOutputsByLocale(HarvestTypes.Exospheric);
                        break;
                }
            }
        }

        protected virtual void prepareOutputsByLocale(HarvestTypes harvestType)
        {
            Log("prepareOutputsByLocale called");
            ResourceRatio outputSource;
            PartResourceDefinition outputDef = null;
            float totalAbundance = 0f;
            float abundance = 0f;
            float outputMass = 0f;
            float outputUnits = 0f;
            IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;

            if (harvestType == HarvestTypes.Planetary)
                abundanceCache = ResourceCache.Instance.AbundanceCache.
                    Where(a => a.HarvestType == harvestType && a.BodyId == this.part.vessel.mainBody.flightGlobalsIndex && a.BiomeName == currentBiome);
            else
                abundanceCache = ResourceCache.Instance.AbundanceCache.
                    Where(a => a.HarvestType == harvestType && a.BodyId == this.part.vessel.mainBody.flightGlobalsIndex);

            foreach (ResourceCache.AbundanceSummary summary in abundanceCache)
            {
                Log("checking " + summary.ResourceName);
                outputDef = ResourceHelper.DefinitionForResource(summary.ResourceName);

                abundance = ResourceMap.Instance.GetAbundance(new AbundanceRequest() {
                    Altitude = this.vessel.altitude,
                    BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                    CheckForLock = true,
                    Latitude = this.vessel.latitude,
                    Longitude = this.vessel.longitude,
                    ResourceType = harvestType,
                    ResourceName = summary.ResourceName
                });

                outputMass = abundance * yieldMass;
                if (outputDef.density > 0)
                    outputUnits = outputMass / outputDef.density;
                else
                    outputUnits = outputMass;
                Log("abundance: " + abundance);
                Log("outputUnits: " + outputUnits);

                //If the resource is an input resource then add its output mass to the byproductMass.
                if (inputSources.Contains(summary.ResourceName))
                {
                    Log(summary.ResourceName + " added to byproductMass");
                    byproductMass += outputMass;
                }

                //If the resource is on our ignore list, then add the output mass to the byproductMass.
                else if (!string.IsNullOrEmpty(ignoreResources) && ignoreResources.Contains(summary.ResourceName))
                {
                    Log(summary.ResourceName + " ignored and added to byproductMass");
                    byproductMass += outputMass;
                }

                //Legit!
                else if (abundance > 0.0001f)
                {
                    totalAbundance += abundance;
                    Log(summary.ResourceName + " abundance: " + abundance + " Ratio: " + outputUnits);
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
                Log("added " + byproduct + " to output list");
            }

            Log("totalAbundance: " + totalAbundance);
            Log("Byproduct Units: " + outputUnits);
            Log("output resources added: " + outputList.Count);
        }
    }
}
