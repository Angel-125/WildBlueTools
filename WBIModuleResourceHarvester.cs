using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.Localization;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Resource Harvester")]
    public class WBIModuleResourceHarvester: ModuleBreakableHarvester
    {
        /// <summary>
        /// List of harvest types: Planetary, Oceanic, Atmospheric, Exospheric. You can have more than one harvest type. Separate the types with a semicolon.
        /// This overrides HarversterType from the base class. There is a precedence based on vessel situation and supported harvest types:
        /// Landed: Atmospheric before Planetary
        /// Splashed: Oceanic before Planetary
        /// In space: Exospheric before Atmospheric
        /// </summary>
        #region Fields
        [KSPField]
        public string harvestTypes = "Planetary";
        #endregion

        #region Housekeeping
        public PartResourceDefinition outputDef;
        IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;
        string currentBiome = string.Empty;
        List<ResourceRatio> resourceRatios;
        List<HarvestTypes> harvestTypeList;
        HarvestTypes currentHarvestType;
        protected InfoView infoView;
        #endregion

        #region events
        [KSPEvent(guiActive = true, guiName = "Show Resource Outputs")]
        public void ShowResourceOutputs()
        {
            StringBuilder outputInfo = new StringBuilder();

            //Make sure we're up to date.
            this.OnUpdate();

            if (Utils.IsBiomeUnlocked(this.part.vessel) == false)
            {
                outputInfo.Append("<color=white><b>Unlock the biome to get the list of outputs.</b></color>");
            }

            else
            {
                if (IsSituationValid())
                {
                    int count = resourceRatios.Count;
                    ResourceRatio output;
                    for (int index = 0; index < count; index++)
                    {
                        output = resourceRatios[index];
                        outputInfo.Append("<color=white>");
                        outputInfo.Append(formatResource(output.ResourceName, output.Ratio));
                        outputInfo.AppendLine("</color>");
                    }
                }

                else
                {
                    outputInfo.AppendLine("<color=white><b>Requires one of:</b></color>");

                    foreach (HarvestTypes harvestType in this.harvestTypeList)
                    {
                        switch (harvestType)
                        {
                            case HarvestTypes.Atmospheric:
                                outputInfo.AppendLine("<color=white>Flying</color>");
                                break;

                            case HarvestTypes.Oceanic:
                                outputInfo.AppendLine("<color=white>In water</color>");
                                break;

                            case HarvestTypes.Exospheric:
                                outputInfo.AppendLine("<color=white>In Space</color>");
                                break;

                            default:
                                outputInfo.AppendLine("<color=white>On the ground</color>");
                                break;
                        }
                    }
                }
            }

            //Setup info view
            infoView = new InfoView();
            infoView.WindowTitle = this.part.partInfo.title;
            infoView.ModuleInfo = outputInfo.ToString();
            infoView.SetVisible(true);
        }
        #endregion

        #region Overrides
        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.Append(base.GetInfo());

            info.AppendLine(" ");

            info.AppendLine("Additional harvested resources and rates vary upon location.");

            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup the resource ratio list
            resourceRatios = new List<ResourceRatio>();

            //Setup the harvest types
            harvestTypeList = new List<HarvestTypes>();
            HarvestTypes type;
            string[] types = harvestTypes.Split(new char[] { ';' });
            for (int index = 0; index < types.Length; index++)
            {
                type = (HarvestTypes)Enum.Parse(typeof(HarvestTypes), types[index]);
                harvestTypeList.Add(type);
            }
            currentHarvestType = (HarvestTypes)HarvesterType;
            HarvesterType = (int)currentHarvestType;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Get the current biome
            string biomeName = string.Empty;
            biomeName = Utils.GetCurrentBiome(this.part.vessel).name;

            //Determine the harvest type from the current vessel situation
            HarvestTypes situationType = getHarvestSituation();

            //If our harvest types are mismatched then rebuild the resource ratios
            if (currentHarvestType != situationType)
            {
                currentHarvestType = situationType;
                HarvesterType = (int)currentHarvestType;
                rebuildResourceRatios();
            }

            //Check the biome
            else if (currentBiome != biomeName)
            {
                currentBiome = biomeName;
                rebuildResourceRatios();
            }

            //And make sure our abundance summary is up to date
            else if (resourceRatios.Count == 0)
                rebuildResourceRatios();
        }

        protected override void LoadRecipe(double harvestRate)
        {
            base.LoadRecipe(harvestRate);
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!IsActivated)
                return;

            //No abundance summary? Then we're done.
            if (resourceRatios.Count == 0)
                return;

            //Set dump excess flag
            ResourceRatio ratio;
            int ratioCount = this.recipe.Outputs.Count;
            for (int index = 0; index < ratioCount; index++)
            {
                ratio = this.recipe.Outputs[index];
                ratio.DumpExcess = true;
                this.recipe.Outputs[index] = ratio;
            }

            //Add our resource ratios to the output list
            ratioCount = resourceRatios.Count;
            for (int index = 0; index < ratioCount; index++)
                this.recipe.Outputs.Add(resourceRatios[index]);
        }
        #endregion

        #region Helpers
        string formatResource(string resourceName, double ratio)
        {
            if (ratio < 0.0001)
                return resourceName + string.Format(": {0:f2}/day", ratio * (double)KSPUtil.dateTimeFormatter.Day);
            else if (ratio < 0.01)
                return resourceName + string.Format(": {0:f2}/hr", ratio * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return resourceName + string.Format(": {0:f2}/sec", ratio);
        }

        protected HarvestTypes getHarvestSituation()
        {
            //The harvest type depends upon situation, and the default type is Planetary.
            //There is a percedence for one harvest type over another depending upon situation
            //and supported harvest types.

            //Atmospheric harvesting takes precedence over Planetary if the harvester supports
            //Amospheric processing and it's landed or flying.
            if (this.part.vessel.situation == Vessel.Situations.PRELAUNCH ||
                this.part.vessel.situation == Vessel.Situations.LANDED ||
                this.part.vessel.situation == Vessel.Situations.FLYING)
            {
                if (harvestTypeList.Contains(HarvestTypes.Atmospheric))
                    return HarvestTypes.Atmospheric;
                else
                    return HarvestTypes.Planetary;
            }

            //Oceanic harvesting takes precedence over Planetary if the harvester supports
            //Oceanic processing and it's splashed.
            else if (this.part.vessel.situation == Vessel.Situations.SPLASHED)
            {
                if (harvestTypeList.Contains(HarvestTypes.Oceanic))
                    return HarvestTypes.Oceanic;
                else
                    return HarvestTypes.Planetary;
            }

            //Exospheric harvesting takes precedence over Planetary if the harvester supports
            //Exospheric processing and it's in space.
            else if (harvestTypeList.Contains(HarvestTypes.Exospheric))
                return HarvestTypes.Exospheric;

            //Default
            return HarvestTypes.Planetary;
        }

        protected void rebuildResourceRatios()
        {
            PartResourceDefinition resourceDef = null;
            float abundance = 0f;
            float harvestEfficiency;
            ResourceRatio ratio;

            if (!ResourceMap.Instance.IsPlanetScanned(this.part.vessel.mainBody.flightGlobalsIndex) && !ResourceMap.Instance.IsBiomeUnlocked(this.part.vessel.mainBody.flightGlobalsIndex, currentBiome))
                return;

            abundanceCache = Utils.GetAbundances(this.part.vessel, currentHarvestType);

            debugLog("Rebuilding resource ratios... ");
            debugLog("abundanceCache count: " + abundanceCache.ToArray().Length);
            resourceRatios.Clear();
            this.recipe.Outputs.Clear();
            foreach (ResourceCache.AbundanceSummary summary in abundanceCache)
            {
                //Skip primary resource
                if (summary.ResourceName == ResourceName)
                    continue;

                //Get the resource definition
                debugLog("Getting abundance for " + summary.ResourceName);
                resourceDef = ResourceHelper.DefinitionForResource(summary.ResourceName);
                if (resourceDef == null)
                    continue;

                //Get the abundance
                abundance = ResourceMap.Instance.GetAbundance(new AbundanceRequest()
                {
                    Altitude = this.vessel.altitude,
                    BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                    CheckForLock = true,
                    Latitude = this.vessel.latitude,
                    Longitude = this.vessel.longitude,
                    ResourceType = HarvestTypes.Planetary,
                    ResourceName = summary.ResourceName
                });
                if (abundance < HarvestThreshold || abundance < 0.001f)
                    continue;

                //Now determine the harvest efficiency
                harvestEfficiency = abundance * Efficiency;

                //Setup the resource ratio
                ratio = new ResourceRatio();
                ratio.ResourceName = summary.ResourceName;
                ratio.Ratio = harvestEfficiency;
                ratio.DumpExcess = true;
                ratio.FlowMode = ResourceFlowMode.NULL;

                resourceRatios.Add(ratio);
                debugLog("Added resource ratio for " + summary.ResourceName + " abundance: " + abundance);
            }
            debugLog("Found abundances for " + resourceRatios.Count + " resources");
        }

        protected override void debugLog(string message)
        {
            if (WBIMainSettings.EnableDebugLogging)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }
        #endregion
    }
}
