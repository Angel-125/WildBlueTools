using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public class WBIKerbalStayParam : ContractParameter
    {
        const string ParameterTitle = "Give {0} a tour of {1} for {2:f1} days";
        const string ParameterTitleComplete = " Give {0} a tour of {1} Completed.";
        const string ParameterTitleFail = " Give {0} a tour of {1} FAILED.";

        public string vesselName = string.Empty;
        public string kerbalName = string.Empty;
        public double totalStayTime;
        public double timeRemaining;
        public double lastUpdate;
        public bool isAtLocation;
        WBIFerryKerbalParam ferryParam;

        public WBIKerbalStayParam()
        {
        }

        public WBIKerbalStayParam(string vesselName, string kerbalName, int totalDays)
        {
            double secondsPerDay = GameSettings.KERBIN_TIME ? 21600 : 86400;
            this.vesselName = vesselName;
            this.kerbalName = kerbalName;
            this.totalStayTime = (double)totalDays * secondsPerDay;
            timeRemaining = totalStayTime;
            lastUpdate = Planetarium.GetUniversalTime();
        }

        public void ResetTimer()
        {
            lastUpdate = Planetarium.GetUniversalTime();
            timeRemaining = totalStayTime;
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
        }

        protected override string GetTitle()
        {
            double secondsPerDay = GameSettings.KERBIN_TIME ? 21600 : 86400;
            double totalDays = totalStayTime / secondsPerDay;

            if (state == ParameterState.Complete)
            {
                return string.Format(ParameterTitleComplete, kerbalName, vesselName);
            }
            else if (state == ParameterState.Failed)
            {
                return string.Format(ParameterTitleFail, kerbalName, vesselName);
            }
            else
            {
                totalDays = timeRemaining / secondsPerDay;
                return string.Format(ParameterTitle, kerbalName, vesselName, totalDays);
            }
        }

        protected override string GetMessageFailed()
        {
            double secondsPerDay = GameSettings.KERBIN_TIME ? 21600 : 86400;
            double totalDays = totalStayTime / secondsPerDay;
            return string.Format(ParameterTitleFail, kerbalName, vesselName);
        }

        protected override string GetMessageComplete()
        {
            double secondsPerDay = GameSettings.KERBIN_TIME ? 21600 : 86400;
            double totalDays = totalStayTime / secondsPerDay;
            return string.Format(ParameterTitleComplete, kerbalName, vesselName);
        }

        protected override string GetMessageIncomplete()
        {
            double secondsPerDay = GameSettings.KERBIN_TIME ? 21600 : 86400;
            double totalDays = timeRemaining / secondsPerDay;
            return string.Format(ParameterTitle, kerbalName, vesselName, totalDays);
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
            node.AddValue("totalStayTime", totalStayTime);
            node.AddValue("timeRemaining", timeRemaining);
            node.AddValue("lastUpdate", lastUpdate);
            node.AddValue("isAtLocation", isAtLocation);
        }

        protected override void OnLoad(ConfigNode node)
        {
            vesselName = node.GetValue("vesselName");
            kerbalName = node.GetValue("kerbalName");
            totalStayTime = double.Parse(node.GetValue("totalStayTime"));
            timeRemaining = double.Parse(node.GetValue("timeRemaining"));
            lastUpdate = double.Parse(node.GetValue("lastUpdate"));
            isAtLocation = bool.Parse(node.GetValue("isAtLocation"));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (State == ParameterState.Complete || State == ParameterState.Failed)
                return;
            if (ferryParam == null)
                ferryParam = GetParameter<WBIFerryKerbalParam>();
            if (ferryParam == null)
                return;
            if (ferryParam.State == ParameterState.Complete && !isAtLocation)
            {
                isAtLocation = true;
                ResetTimer();
            }
            else if (ferryParam.State == ParameterState.Incomplete)
            {
                isAtLocation = false;
                return;
            }

            //Calculate elapsed time
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdate;
            lastUpdate = Planetarium.GetUniversalTime();

            //Update the stay time
            timeRemaining -= elapsedTime;
            if (timeRemaining <= 0.0001)
            {
                timeRemaining = 0f;
                WBIContractScenario.Instance.registerKerbal(kerbalName);
                SetComplete();
            }
            else
            {
                SetIncomplete();
            }

            //GUI update
            GameEvents.Contract.onParameterChange.Fire(Root, this);
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
                    base.SetFailed();
                    return;
                }
            }

        }
    }
}
