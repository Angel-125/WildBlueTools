using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;
using FinePrint.Contracts.Parameters;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public class WBIFerryKerbalParam : ContractParameter
    {
        const string ParameterTitle = "Ferry {0} to {1}";

        public string vesselName = string.Empty;
        public string kerbalName = string.Empty;
//        public string hashString = string.Empty;
        public ProtoCrewMember kerbal;
        SpecificVesselParameter specificVesselParam;

        public WBIFerryKerbalParam()
        {
        }

        public WBIFerryKerbalParam(string vesselName, string kerbalName)
        {
            this.vesselName = vesselName;
            this.kerbalName = kerbalName;
        }

        protected void Log(string message)
        {
            if (WildBlueIndustries.WBIMainSettings.EnableDebugLogging)
            {
                Debug.Log("[WBIFerryKerbalParam] - " + message);
            }
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();

            /*
            if (string.IsNullOrEmpty(hashString))
                hashString = Guid.NewGuid().ToString();
            return hashString;
             */
        }

        protected override string GetTitle()
        {
            return string.Format(ParameterTitle, kerbalName, vesselName);
        }

        protected override void OnRegister()
        {
            GameEvents.onCrewKilled.Add(onCrewKilled);
        }

        protected override void OnUnregister()
        {
            GameEvents.onCrewKilled.Remove(onCrewKilled);
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("vesselName", vesselName);
            node.AddValue("kerbalName", kerbalName);
        }

        protected override void OnLoad(ConfigNode node)
        {
            vesselName = node.GetValue("vesselName");
            kerbalName = node.GetValue("kerbalName");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (state != ParameterState.Incomplete)
                return;

            //Get the kerbal object
            if (kerbal == null)
            {
                KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
                if (roster.Exists(kerbalName))
                    kerbal = roster[kerbalName];
            }
            if (kerbal == null)
            {
                SetIncomplete();
                return;
            }
            if (specificVesselParam == null)
                specificVesselParam = Root.GetParameter<SpecificVesselParameter>();
            if (specificVesselParam == null)
            {
                SetIncomplete();
                return;
            }

            //If we're at the desired vessel, then check to see if the kerbal is aboard.
            if (specificVesselParam.State == ParameterState.Complete)
            {
                if (FlightGlobals.ActiveVessel.GetVesselCrew().Contains(kerbal))
                    SetComplete();
                else
                    SetIncomplete();
            }
            else
            {
                SetIncomplete();
            }
        }

        private void onCrewKilled(EventReport report)
        {
            if (report.origin == null)
                return;

            List<ProtoCrewMember> crewMembers = report.origin.protoModuleCrew;
            foreach (ProtoCrewMember doomed in crewMembers)
            {
                if (doomed.name == kerbalName)
                {
                    WBIContractScenario.Instance.unregisterKerbal(kerbalName);
                    SetFailed();
                    return;
                }
            }

        }
    }
}
