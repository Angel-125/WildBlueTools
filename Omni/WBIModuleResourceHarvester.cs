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
    public class WBIModuleResourceHarvester: ModuleResourceHarvester
    {
        #region Fields
        [KSPField]
        /// <summary>
        /// List of harvest types: Planetary, Oceanic, Atmospheric, Exospheric. You can have more than one harvest type. Separate the types with a semicolon.
        /// This overrides HarversterType from the base class. There is a precedence based on vessel situation and supported harvest types:
        /// Landed: Atmospheric before Planetary
        /// Splashed: Oceanic before Planetary
        /// In space: Exospheric before Atmospheric
        /// </summary>
        public string harvestTypes = string.Empty;

        /// <summary>
        /// Name of the animation for the drill.
        /// </summary>
        [KSPField]
        public string drillAnimationName = string.Empty;

        [KSPField(isPersistant = true)]
        public string drillResources = string.Empty;

        /// <summary>
        /// List of anomalies that the converter must be next to in order to function.
        /// </summary>
        [KSPField]
        public string requiredAnomalies = string.Empty;

        /// <summary>
        /// Minimum range from the required anomalies that the harvester must be next to in order to function.
        /// </summary>
        [KSPField]
        public float minAnomalyRange = 50.0f;

        /// <summary>
        /// The harvester must orbit a star.
        /// </summary>
        [KSPField]
        public bool solarOrbitRequired = false;

        #endregion

        #region Housekeeping
        public PartResourceDefinition outputDef;
        IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;
        string currentBiome = string.Empty;
        List<ResourceRatio> resourceRatios;
        List<ResourceRatio> outputRatios;
        List<HarvestTypes> harvestTypeList;
        HarvestTypes currentHarvestType;
        protected HarvestView infoView;
        #endregion

        #region events
        [KSPEvent(guiActive = true, guiName = "Show Resource Outputs")]
        public void ShowResourceOutputs()
        {
            setupHarvester();
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

            info.AppendLine(" ");
            info.AppendLine("<b>Requirements</b>");

            //Required anomalies
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                info.AppendLine("<b>Anomalies: </b>" + requiredAnomalies.Replace(";", ", "));
                info.AppendLine(string.Format("<b>Minimum Range: </b>{0:f2}m", minAnomalyRange));
            }

            //Solar orbit
            if (solarOrbitRequired)
                info.AppendLine("Requires an orbit around a star");

            return info.ToString();
        }

        public override void OnSave(ConfigNode node)
        {
            StringBuilder resourceBuilder = new StringBuilder();
            if (resourceRatios == null || outputRatios == null || resourceBuilder == null)
                return;

            //Add our resource ratios to the output list
            int ratioCount = resourceRatios.Count;
            for (int index = 0; index < ratioCount; index++)
                resourceBuilder.Append(resourceRatios[index].ResourceName + "," + resourceRatios[index].Ratio + ";");

            //Add any supplementary outputs
            ratioCount = outputRatios.Count;
            for (int index = 0; index < ratioCount; index++)
                resourceBuilder.Append(outputRatios[index].ResourceName + "," + outputRatios[index].Ratio + ";");

            drillResources = resourceBuilder.ToString();
            base.OnSave(node);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup info view
            infoView = new HarvestView();
            infoView.WindowTitle = this.part.partInfo.title;
            infoView.part = this.part;
            infoView.onBiomeUnlocked = setupHarvester;

            //Setup the resource ratio list
            resourceRatios = new List<ResourceRatio>();

            //Setup supplementary output ratios
            setupSupplementaryOutputs();

            //Setup the harvest types
            harvestTypeList = new List<HarvestTypes>();
            if (!string.IsNullOrEmpty(harvestTypes))
            {
                HarvestTypes type;
                string[] types = harvestTypes.Split(new char[] { ';' });
                for (int index = 0; index < types.Length; index++)
                {
                    type = (HarvestTypes)Enum.Parse(typeof(HarvestTypes), types[index]);
                    harvestTypeList.Add(type);
                }
            }

            //Use HarvesterType
            else
            {
                harvestTypeList.Add((HarvestTypes)this.HarvesterType);
            }

            //If in flight then setup the harvester
            if (HighLogic.LoadedSceneIsFlight)
            {
                this.currentHarvestType = getHarvestSituation();
                setupHarvester();
            }
        }

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Get the current biome
            string biomeName = Utils.GetCurrentBiomeName(this.part.vessel);

            //Determine the harvest type from the current vessel situation
            HarvestTypes situationType = getHarvestSituation();

            //If our harvest types are mismatched then rebuild the resource ratios
            if (currentHarvestType != situationType)
            {
                currentHarvestType = situationType;
                setupHarvester();
            }

            //Check the biome
            else if (currentBiome != biomeName)
            {
                currentBiome = biomeName;
                setupHarvester();
            }

            //And make sure our abundance summary is up to date
            else if (resourceRatios.Count == 0)
            {
                setupHarvester();
            }

            base.OnUpdate();
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

            // Check anomalies
            string anomalyName = findNearestAnomaly();
            if (!string.IsNullOrEmpty(requiredAnomalies))
            {
                if (string.IsNullOrEmpty(anomalyName) || !requiredAnomalies.Contains(anomalyName))
                {
                    status = "Needs to be near " + requiredAnomalies.Replace(";", ", ");
                    return;
                }
            }

            // Check solar orbit
            if (solarOrbitRequired)
            {
                if (this.part.vessel.mainBody.scaledBody.GetComponentsInChildren<SunShaderController>(true).Length == 0)
                    return;
            }

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

            //Add any supplementary outputs
            ratioCount = outputRatios.Count;
            for (int index = 0; index < ratioCount; index++)
                this.recipe.Outputs.Add(outputRatios[index]);

            //Add anomaly node resources
            if (!string.IsNullOrEmpty(anomalyName))
                addAnomalyResources(anomalyName, harvestRate);
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
            toggleDrillAnimation();
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            toggleDrillAnimation();
        }
        #endregion

        #region Helpers
        void addAnomalyResources(string anomalyName, double harvestRate)
        {
            if (!WBIOmniManager.Instance.anomalyResources.ContainsKey(anomalyName))
                return;

            Dictionary<string, AnomalyResource> anomalyResources = WBIOmniManager.Instance.anomalyResources[anomalyName];
            string[] keys = anomalyResources.Keys.ToArray();

            AnomalyResource resource;
            ResourceRatio ratio;
            PartResourceDefinition resourceDef = null;
            double rate;
            for (int index = 0; index < keys.Length; index++)
            {
                resource = anomalyResources[keys[index]];
                resourceDef = PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
                if (resourceDef == null)
                    continue;

                ratio = new ResourceRatio();
                ratio.ResourceName = string.Empty;
                ratio.FlowMode = resourceDef.resourceFlowMode;

                // The anomaly has a limited amount of the resource.
                if (resource.currentAmount > 0)
                {
                    ratio.ResourceName = resource.resourceName;
                    rate = harvestRate;
                    if (rate <= resource.currentAmount)
                    {
                        resource.currentAmount -= rate;
                        if (resource.currentAmount <= 0)
                            resource.currentAmount = 0;
                    }
                    else
                    {
                        rate = resource.currentAmount;
                        resource.currentAmount = 0;
                    }
                    ratio.Ratio = rate;
                }

                // Abundance implies an unlimited amount of the resource
                else if (resource.abundance > 0)
                {
                    ratio.ResourceName = resource.resourceName;
                    ratio.Ratio = resource.abundance * harvestRate;
                }

                if (!string.IsNullOrEmpty(ratio.ResourceName))
                {
                    recipe.Outputs.Add(ratio);
                }
            }
        }

        string findNearestAnomaly()
        {
            CelestialBody mainBody = this.part.vessel.mainBody;
            PQSSurfaceObject[] anomalies = mainBody.pqsSurfaceObjects;
            double longitude;
            double latitude;
            double distance = 0f;

            if (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                for (int index = 0; index < anomalies.Length; index++)
                {
                    //Get the longitude and latitude of the anomaly
                    longitude = mainBody.GetLongitude(anomalies[index].transform.position);
                    latitude = mainBody.GetLatitude(anomalies[index].transform.position);

                    //Get the distance (in meters) from the anomaly.
                    distance = Utils.HaversineDistance(longitude, latitude,
                        this.part.vessel.longitude, this.part.vessel.latitude, this.part.vessel.mainBody) * 1000;

                    //If we're near the anomaly, then we're done
                    if (distance <= minAnomalyRange)
                    {
                        return anomalies[index].SurfaceObjectName;
                    }
                }
            }

            //Not near anomaly or not close enough
            return string.Empty;
        }

        protected virtual void toggleDrillAnimation()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (string.IsNullOrEmpty(drillAnimationName))
                return;

            List<WBIAnimation> animators = this.part.FindModulesImplementing<WBIAnimation>();
            int count = animators.Count;
            for (int index = 0; index < count; index++)
            {
                if (animators[index].animationName == drillAnimationName)
                {
                    animators[index].ToggleAnimation();
                    break;
                }
            }
        }
        public string GetMinedResources()
        {
            setupHarvester();
            return infoView.ModuleInfo;
        }

        protected void setupHarvester()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            this.HarvesterType = (int)currentHarvestType;

            rebuildResourceRatios();

            if (resourceRatios.Count > 0)
            {
                this.ResourceName = resourceRatios[0].ResourceName;
                this.Fields["ResourceStatus"].guiName = Localizer.Format("#autoLOC_6001918", 
                    new string[1] { PartResourceLibrary.Instance.GetDefinition(this.ResourceName.GetHashCode()).displayName });
            }

            //Now update the info view
            StringBuilder outputInfo = new StringBuilder();

            if (Utils.IsBiomeUnlocked(this.part.vessel) == false)
            {
                outputInfo.Append("<color=white><b>Unlock the biome to get the list of outputs.</b></color>");
            }

            else
            {
                if (IsSituationValid())
                {

                    switch (currentHarvestType)
                    {
                        case HarvestTypes.Atmospheric:
                            outputInfo.AppendLine("<color=white><b>Environment: </b>Atmospheric</color>");
                            break;

                        case HarvestTypes.Oceanic:
                            outputInfo.AppendLine("<color=white><b>Environment: </b>Hydrospheric</color>");
                            break;

                        case HarvestTypes.Exospheric:
                            outputInfo.AppendLine("<color=white><b>Environment: </b>Exospheric</color>");
                            break;

                        default:
                            outputInfo.AppendLine("<color=white><b>Environment: </b>Ground</color>");
                            break;
                    }

                    int count = resourceRatios.Count;
                    ResourceRatio output;
                    double harvestRate;
                    for (int index = 0; index < count; index++)
                    {
                        output = resourceRatios[index];
                        harvestRate = output.Ratio * getIntakeMultiplier();
                        outputInfo.Append("<color=white>");
                        outputInfo.Append(formatResource(output.ResourceName, harvestRate));
                        outputInfo.AppendLine("</color>");
                    }

                    //Don't forget supplementary resources
                    count = outputRatios.Count;
                    for (int index = 0; index < count; index++)
                    {
                        output = outputRatios[index];
                        harvestRate = output.Ratio * getIntakeMultiplier();
                        outputInfo.Append("<color=white>");
                        outputInfo.Append(formatResource(output.ResourceName, harvestRate));
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

            infoView.ModuleInfo = outputInfo.ToString();
        }

        double getIntakeMultiplier()
        {
            if (this.part.ShieldedFromAirstream)
                return 0.0f;
            if (this.intakeTransform == null)
                return 1.0f;
            if (this.currentHarvestType == HarvestTypes.Planetary)
                return 1.0f;

            double density = this.part.vessel.atmDensity;
            if (currentHarvestType == HarvestTypes.Exospheric)
                density = 1.0f;

            double speed = this.part.vessel.srfSpeed + this.airSpeedStatic;

            double intakeAngle = UtilMath.Clamp01((double)Vector3.Dot((Vector3)this.vessel.srf_vel_direction, this.intakeTransform.forward));

            return density * speed * intakeAngle;
        }

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
                {
                    //Intake needs to be underwater. If it isn't then set to atmospheric if we can do atmospheric
                    if (isUnderwater())
                        return HarvestTypes.Oceanic;
                    else if (harvestTypeList.Contains(HarvestTypes.Atmospheric))
                        return HarvestTypes.Atmospheric;
                    else
                        return HarvestTypes.Planetary;
                }
                else
                {
                    return HarvestTypes.Planetary;
                }
            }

            //Exospheric harvesting takes precedence over Planetary if the harvester supports
            //Exospheric processing and it's in space.
            else if (harvestTypeList.Contains(HarvestTypes.Exospheric))
                return HarvestTypes.Exospheric;

            //Default
            return HarvestTypes.Planetary;
        }

        protected bool isUnderwater()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return false;
            if (!this.part.vessel.mainBody.ocean)
                return false;
            if (!this.part.vessel.Splashed)
                return false;

            if (FlightGlobals.getAltitudeAtPos((Vector3d)this.intakeTransform.position, this.part.vessel.mainBody) <= 0.0f)
                return true;

            return false;
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
                //Get the resource definition
                debugLog("Getting abundance for " + summary.ResourceName);
                resourceDef = ResourceHelper.DefinitionForResource(summary.ResourceName);
                if (resourceDef == null)
                {
                    debugLog("No definition found!");
                    continue;
                }

                //Get the abundance
                abundance = ResourceMap.Instance.GetAbundance(new AbundanceRequest()
                {
                    Altitude = this.vessel.altitude,
                    BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                    CheckForLock = true,
                    Latitude = this.vessel.latitude,
                    Longitude = this.vessel.longitude,
                    ResourceType = currentHarvestType,
                    ResourceName = summary.ResourceName
                });
                if (abundance < HarvestThreshold || abundance < 0.001f)
                {
                    debugLog("Abundance is below HarvestThreshold or minimum abundance (0.001)");
                    continue;
                }

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

        protected void setupSupplementaryOutputs()
        {
            outputRatios = new List<ResourceRatio>();

            if (this.part.partInfo.partConfig == null)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode harvesterNode = null;
            ConfigNode node = null;
            string moduleName;
            List<string> optionNamesList = new List<string>();

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        harvesterNode = node;
                        break;
                    }
                }
            }
            if (harvesterNode == null)
                return;

            if (harvesterNode.HasNode("OUTPUT_RESOURCE"))
            {
                ConfigNode[] outputNodes = node.GetNodes("OUTPUT_RESOURCE");
                ConfigNode outputNode;
                ResourceRatio ratio;
                double value;
                bool boolValue;
                for (int index = 0; index < outputNodes.Length; index++)
                {
                    outputNode = outputNodes[index];
                    if (outputNode.HasValue("ResourceName") && outputNode.HasValue("Ratio"))
                    {
                        ratio = new ResourceRatio();
                        ratio.ResourceName = outputNode.GetValue("ResourceName");

                        if (double.TryParse(outputNode.GetValue("Ratio"), out value))
                            ratio.Ratio = value;

                        if (outputNode.HasValue("FlowMode"))
                            ratio.FlowMode = (ResourceFlowMode)Enum.Parse(typeof(ResourceFlowMode), outputNode.GetValue("FlowMode"));

                        if (outputNode.HasValue("DumpExcess"))
                        {
                            if (bool.TryParse(outputNode.GetValue("DumpExcess"), out boolValue))
                                ratio.DumpExcess = boolValue;
                        }

                        outputRatios.Add(ratio);
                    }
                }
            }
        }

        protected void debugLog(string message)
        {
            if (WBIMainSettings.EnableDebugLogging)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }
        #endregion
    }
}
