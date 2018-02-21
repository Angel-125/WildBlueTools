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

        protected ModuleBiomeScanner biomeScanner = null;
        protected ModuleGPS gps;
        protected List<PlanetaryResource> resourceList;
        protected GeoLabView geoLabView = new GeoLabView();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            setupPartModules();
            geoLabView.performBiomAnalysisDelegate = this.perfomBiomeAnalysys;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Abundance Report")]
        public virtual void ToggleLabGUI()
        {
            if (!geoLabView.IsVisible())
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (resourceList == null)
                        resourceList = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, this.part.vessel.mainBody);
                    else if (resourceList.Count == 0)
                        resourceList = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, this.part.vessel.mainBody);
                }

                geoLabView.gps = this.gps;
                geoLabView.resourceList = this.resourceList;
                geoLabView.part = this.part;
            }
            geoLabView.SetVisible(!geoLabView.IsVisible());
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
            resourceList = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, this.part.vessel.mainBody);
            geoLabView.resourceList = this.resourceList;
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
            geoLabView.resourceList = this.resourceList;
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
            geoLabView.DrawView();
        }

        public virtual void SetParentView(IParentView parentView)
        {
        }

        public virtual void SetContextGUIVisible(bool isVisible)
        {
            Events["ToggleLabGUI"].guiActive = isVisible;
        }

        public virtual string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
    }
}
