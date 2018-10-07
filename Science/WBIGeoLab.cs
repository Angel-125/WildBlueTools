using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    [KSPModule("Geology Lab")]
    public class WBIGeoLab : PartModule, IOpsView
    {
        const string kNoCrew = "At least one cremember must staff the lab in order to perform the analysis.";
        const string kScienceGenerated = "You gained {0:f2} Bonus Science!";
        const float kMessageDuration = 5f;
        const float kBiomeAnalysisFactor = 1.0f;

        [KSPField]
        public string researchSkill = "ScienceSkill";

        public static EventVoid onBiomeUnlocked = new EventVoid("OnBiomeUnlocked");

        protected ModuleBiomeScanner biomeScanner = null;
        protected ModuleGPS gps;
        protected List<PlanetaryResource> resourceList;
        protected GeoLabView geoLabView;
        IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;
        string currentBiome = string.Empty;
        Dictionary<string, float> abundanceSummary = new Dictionary<string, float>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            geoLabView = new GeoLabView();

            setupPartModules();
            geoLabView.performBiomAnalysisDelegate = this.perfomBiomeAnalysys;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Abundance Report")]
        public virtual void ToggleLabGUI()
        {
            if (!geoLabView.IsVisible())
            {
                if (HighLogic.LoadedSceneIsFlight)
                    rebuildAbundanceSummary();

                geoLabView.gps = this.gps;
                geoLabView.abundanceSummary = this.abundanceSummary;
                geoLabView.part = this.part;
            }
            geoLabView.SetVisible(!geoLabView.IsVisible());
        }

        protected void rebuildAbundanceSummary()
        {
            Log("rebuildAbundanceSummary called");
            PartResourceDefinition resourceDef = null;
            float abundance = 0f;

            if (this.part.vessel.situation == Vessel.Situations.LANDED ||
                this.part.vessel.situation == Vessel.Situations.SPLASHED ||
                this.part.vessel.situation == Vessel.Situations.PRELAUNCH)
                currentBiome = Utils.GetCurrentBiome(this.part.vessel).name;

            if (!ResourceMap.Instance.IsPlanetScanned(this.part.vessel.mainBody.flightGlobalsIndex) && !ResourceMap.Instance.IsBiomeUnlocked(this.part.vessel.mainBody.flightGlobalsIndex, currentBiome))
            {
                Log("Planet not scanned or biome still locked. Exiting");
                return;
            }
            abundanceSummary.Clear();
            abundanceCache = ResourceCache.Instance.AbundanceCache.
                Where(a => a.HarvestType == HarvestTypes.Planetary && a.BodyId == this.part.vessel.mainBody.flightGlobalsIndex && a.BiomeName == currentBiome);

            foreach (ResourceCache.AbundanceSummary summary in abundanceCache)
            {
                Log("Getting abundance for " + summary.ResourceName);
                resourceDef = ResourceHelper.DefinitionForResource(summary.ResourceName);
                if (resourceDef == null)
                    continue;

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

                abundanceSummary.Add(resourceDef.displayName, abundance);
                Log("Added abundance for " + summary.ResourceName + ": " + abundance);
            }
            Log("Found abundances for " + abundanceSummary.Keys.Count + " resources");
        }

        protected virtual bool perfomBiomeAnalysys()
        {
            //We need at least one crewmember in the lab.
            if (this.part.protoModuleCrew.Count == 0)
            {
                ScreenMessages.PostScreenMessage(kNoCrew, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //We need at least one scientist in the lab.
            float scienceBonus = getBiomeAnalysisBonus();

            //We can run the analysis, add the science bonus
            if (scienceBonus > 0.0f)
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    ScreenMessages.PostScreenMessage(string.Format(kScienceGenerated, scienceBonus), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                    ResearchAndDevelopment.Instance.AddScience(scienceBonus, TransactionReasons.RnDs);
                }
            }

            //Run the analysis
            biomeScanner.RunAnalysis();
            rebuildAbundanceSummary();
            geoLabView.abundanceSummary = this.abundanceSummary;
            onBiomeUnlocked.Fire();
            return true;
        }

        protected virtual float getBiomeAnalysisBonus()
        {
            float bonus = 0f;

            foreach (ProtoCrewMember crewMember in this.part.protoModuleCrew)
                if (crewMember.HasEffect(researchSkill))
                {
                    //One point for being a scientist.
                    bonus += 1.0f;

                    //One point for each experience level.
                    bonus += crewMember.experienceLevel;
                }

            return bonus * kBiomeAnalysisFactor;
        }

        protected virtual void setupPartModules()
        {
            //GPS
            if (gps == null)
                gps = this.part.FindModuleImplementing<ModuleGPS>();

            if (biomeScanner == null)
            {
                biomeScanner = this.part.FindModuleImplementing<ModuleBiomeScanner>();
                biomeScanner.Events["RunAnalysis"].guiActive = false;
                biomeScanner.Events["RunAnalysis"].guiActiveEditor = false;
                biomeScanner.Events["RunAnalysis"].guiActiveUnfocused = false;
            }

            //Resource list
            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    if (resourceList == null)
                        resourceList = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, this.part.vessel.mainBody);
                    else if (resourceList.Count == 0)
                        resourceList = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, this.part.vessel.mainBody);
                }
                catch { }
            }

            geoLabView.gps = this.gps;
            geoLabView.part = this.part;
        }

        public virtual List<string> GetButtonLabels()
        {
            List<string> labels = new List<string>();

            labels.Add("Geology Lab");

            return labels;
        }

        public virtual void DrawOpsWindow(string buttonLabel)
        {
            if (geoLabView.performBiomAnalysisDelegate == null)
                geoLabView.performBiomAnalysisDelegate = this.perfomBiomeAnalysys;
            geoLabView.DrawView();
        }

        public virtual void SetParentView(IParentView parentView)
        {
            //We're using SetParentView as a kind of initializer...
            if (HighLogic.LoadedSceneIsFlight)
                rebuildAbundanceSummary();

            geoLabView.gps = this.gps;
            geoLabView.abundanceSummary = this.abundanceSummary;
            geoLabView.part = this.part;
        }

        public virtual void SetContextGUIVisible(bool isVisible)
        {
            Events["ToggleLabGUI"].guiActive = isVisible;
        }

        public virtual string GetPartTitle()
        {
            return this.part.partInfo.title;
        }

        protected void Log(string message)
        {
            if (WBIMainSettings.EnableDebugLogging == false)
                return;

            Debug.Log("[WBIGeoLab] - " + message);
        }

    }
}
