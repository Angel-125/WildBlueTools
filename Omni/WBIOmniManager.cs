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
        public double secondsPerCycle = 21600.0;
        #endregion

        #region Housekeeping
        public static WBIOmniManager Instance;

        public double cycleStartTime;
        public Dictionary<string, WBIBackgroundConverter> backgroundConverters;
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
            if (elapsedTime < secondsPerCycle)
                return;
            if (backgroundConverters.Count == 0)
                return;

            //Reset the timer
            cycleStartTime = Planetarium.GetUniversalTime();

            WBIBackgroundConverter[] converters = backgroundConverters.Values.ToArray();
            int count = converters.Length;
            WBIBackgroundConverter converter;
            for (int index = 0; index < count; index++)
            {
                converter = converters[index];

                if (converter.IsActivated && !converter.isMissingResources && !converter.isContainerFull)
                    StartCoroutine(runConverter(converter, elapsedTime));
            }
        }

        protected IEnumerator<YieldInstruction> runConverter(WBIBackgroundConverter converter, double elapsedTime)
        {
            //If the vessel is currently loaded, then we're done
            bool vesselIsLoaded = false;
            Vessel[] loadedVessels = FlightGlobals.VesselsLoaded.ToArray();
            for (int vesselIndex = 0; vesselIndex < loadedVessels.Length; vesselIndex++)
            {
                if (loadedVessels[vesselIndex].id.ToString() == converter.vesselID)
                {
                    vesselIsLoaded = true;
                    break;
                }
            }
            yield return new WaitForFixedUpdate();

            if (!vesselIsLoaded)
            {
                //Find the unloaded vessel
                int count = FlightGlobals.VesselsUnloaded.Count;
                Vessel[] unloadedVessels = FlightGlobals.VesselsUnloaded.ToArray();
                Vessel unloadedVessel = null;
                ProtoVessel protoVessel;
                string unloadedVesselID;
                for (int index = 0; index < count; index++)
                {
                    unloadedVessel = unloadedVessels[index];
                    unloadedVesselID = unloadedVessel.id.ToString();
                    if (unloadedVesselID == converter.vesselID)
                        break;
                    else
                        unloadedVessel = null;
                }
                yield return new WaitForFixedUpdate();

                //Process our resources if we found the vessel.
                if (unloadedVessel != null)
                {
                    //Get the proto vessel
                    protoVessel = unloadedVessel.protoVessel;

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
                }

                //We didn't find the vessel. Remove the converter from our list.
                else
                {
                    backgroundConverters.Remove(converter.converterID);
                }
            }

            yield return new WaitForFixedUpdate();
        }

        #endregion

        #region API
        public WBIBackgroundConverter GetBackgroundConverter(WBIOmniConverter converter)
        {
            if (backgroundConverters.ContainsKey(converter.ID))
                return backgroundConverters[converter.ID];

            return null;
        }

        public void UpdateBackgroundConverter(WBIBackgroundConverter converter)
        {
            if (backgroundConverters.ContainsKey(converter.converterID))
                backgroundConverters[converter.converterID] = converter;
        }

        public void RegisterBackgroundConverter(WBIOmniConverter converter)
        {
            WBIBackgroundConverter backgroundConverter = new WBIBackgroundConverter();

            if (IsRegistered(converter))
            {
                backgroundConverter = GetBackgroundConverter(converter);
                backgroundConverter.GetConverterData(converter);
                backgroundConverter.IsActivated = converter.IsActivated;
                backgroundConverter.isMissingResources = false;
                backgroundConverter.isContainerFull = false;
                backgroundConverter.vesselID = converter.part.vessel.id.ToString();

                UpdateBackgroundConverter(backgroundConverter);
                return;
            }

            backgroundConverter.vesselID = converter.part.vessel.id.ToString();
            backgroundConverter.GetConverterData(converter);

            backgroundConverters.Add(backgroundConverter.converterID, backgroundConverter);
        }

        public void UnregisterBackgroundConverter(WBIOmniConverter converter)
        {
            if (!IsRegistered(converter))
                return;

            if (backgroundConverters.ContainsKey(converter.ID))
                backgroundConverters.Remove(converter.ID);
        }

        public bool IsRegistered(WBIOmniConverter converter)
        {
            return backgroundConverters.ContainsKey(converter.ID);
        }
        #endregion

        #region Overrides
        internal void start()
        {
            Instance = this;
        }

        public override void OnAwake()
        {
            Instance = this;
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselDestroy.Add(onVesselDestroy);

            backgroundConverters = new Dictionary<string, WBIBackgroundConverter>();

            if (cycleStartTime <= 0)
                cycleStartTime = Planetarium.GetUniversalTime();
        }

        public override void OnLoad(ConfigNode node)
        {
            backgroundConverters = new Dictionary<string, WBIBackgroundConverter>();

            //Housekeeping
            double.TryParse(node.GetValue("cycleStartTime"), out cycleStartTime);

            //Converters
            ConfigNode[] configNodes = node.GetNodes(WBIBackgroundConverter.NodeName);
            WBIBackgroundConverter converter;
            for (int index = 0; index < configNodes.Length; index++)
            {
                converter = new WBIBackgroundConverter();
                converter.Load(configNodes[index]);

                if (!backgroundConverters.ContainsKey(converter.converterID))
                    backgroundConverters.Add(converter.converterID, converter);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            //Housekeeping
            node.AddValue("cycleStartTime", cycleStartTime);

            //Converters
            WBIBackgroundConverter[] converters = backgroundConverters.Values.ToArray();
            WBIBackgroundConverter converter;
            ConfigNode converterNode;
            for (int index = 0; index < converters.Length; index++)
            {
                converter = converters[index];
                converterNode = converter.Save();

                node.AddNode(converterNode);
            }
        }

        public void Destroy()
        {
            GameEvents.onVesselDestroy.Remove(onVesselDestroy);
            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        protected void onVesselChange(Vessel vessel)
        {

        }

        protected void onVesselDestroy(Vessel vessel)
        {

        }
        #endregion
    }
}
