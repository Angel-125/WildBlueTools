using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using KSP.Localization;

namespace WildBlueIndustries
{
    /// <summary>
    /// The purpose of this class is to run converters in the background, meaning that the vessel is currently unloaded. This is primarily to drive life support systems,
    /// but other types of converters also benefit.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class WBIOmniManager : ScenarioModule
    {
        #region Constants
        public double secondsPerCycle = 3600;
        #endregion

        #region Housekeeping
        public static WBIOmniManager Instance;

        public double cycleStartTime;
        public Dictionary<Vessel, List<WBIBackgroundConverter>> backgroundConverters;
        public List<Part> createdParts;
        #endregion

        #region Background processing
        public void FixedUpdate()
        {
            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                return;
            }
            double elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;
            elapsedTime = secondsPerCycle;
            if (elapsedTime < secondsPerCycle)
                return;
            if (backgroundConverters.Count == 0)
                return;

            //Reset the timer
            cycleStartTime = Planetarium.GetUniversalTime();

            Vessel vessel;
            int count = FlightGlobals.Vessels.Count;
            for (int vesselIndex = 0; vesselIndex < count; vesselIndex++)
            {
                vessel = FlightGlobals.Vessels[vesselIndex];

                //Skip vessel types that we're not interested in.
                if (vessel.vesselType == VesselType.Debris ||
                    vessel.vesselType == VesselType.Flag ||
                    vessel.vesselType == VesselType.SpaceObject ||
                    vessel.vesselType == VesselType.Unknown)
                    continue;

                //Run background converters
                if (!vessel.loaded)
                    runBackgroundConverters(vessel, elapsedTime);
            }
        }

        protected void runBackgroundConverters(Vessel vessel, double elapsedTime)
        {
            List<WBIBackgroundConverter> converters;
            if (backgroundConverters.ContainsKey(vessel))
                converters = backgroundConverters[vessel];
            else
                return;
            int count = converters.Count;
            WBIBackgroundConverter converter;

            for (int index = 0; index < count; index++)
            {
                converter = converters[index];

                if (converter.IsActivated && !converter.isMissingResources && !converter.isContainerFull)
                    StartCoroutine(runConverter(converter, elapsedTime, vessel.protoVessel));
            }
        }

        protected IEnumerator<YieldInstruction> runConverter(WBIBackgroundConverter converter, double elapsedTime, ProtoVessel protoVessel)
        {
            //Get ready to process
            converter.PrepareToProcess(protoVessel);
            yield return new WaitForFixedUpdate();

            //Check required
            converter.CheckRequiredResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Consume inputs
            converter.ConsumeInputResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Produce outputs
            converter.ProduceOutputResources(protoVessel, elapsedTime);
            yield return new WaitForFixedUpdate();

            //Produce yields
            converter.ProduceYieldResources(protoVessel);
            yield return new WaitForFixedUpdate();

            //Post process
            converter.PostProcess(protoVessel);
            yield return new WaitForFixedUpdate();
        }

        #endregion

        #region API
        public bool WasRecentlyCreated(Part part)
        {
            if (createdParts == null)
                createdParts = new List<Part>();
            return createdParts.Contains(part);
        }
        #endregion

        #region Overrides
        internal void Start()
        {
            Instance = this;
            backgroundConverters = WBIBackgroundConverter.GetBackgroundConverters();
        }

        public override void OnAwake()
        {
            Instance = this;
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselDestroy.Add(onVesselDestroy);
            GameEvents.onEditorPartEvent.Add(onEditorPartEvent);

            if (cycleStartTime <= 0)
                cycleStartTime = Planetarium.GetUniversalTime();
        }

        public override void OnLoad(ConfigNode node)
        {
            //Housekeeping
            double.TryParse(node.GetValue("cycleStartTime"), out cycleStartTime);
        }

        public override void OnSave(ConfigNode node)
        {
            //Housekeeping
            node.AddValue("cycleStartTime", cycleStartTime);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
        }

        protected void onVesselChange(Vessel vessel)
        {

        }

        protected void onVesselDestroy(Vessel vessel)
        {

        }

        public void onEditorPartEvent(ConstructionEventType eventType, Part part)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            if (createdParts == null)
                createdParts = new List<Part>();

            switch (eventType)
            {
                case ConstructionEventType.PartCreated:
                    if (!createdParts.Contains(part))
                        createdParts.Add(part);
                    break;

                case ConstructionEventType.PartDeleted:
                    if (createdParts.Contains(part))
                        createdParts.Remove(part);
                    break;
            }
        }

        #endregion
    }
}
