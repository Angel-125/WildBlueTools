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
    public class WBITargetBodyParam : ContractParameter
    {
        const string DefaultTitle = "Transport experiment to ";
        public int targetBodyID;
        public string targetBodyName;
        public string targetTitle = "Transport experiment to ";

        public WBITargetBodyParam()
        {
            if (targetTitle == DefaultTitle)
                targetTitle += targetBodyName;
        }

        public WBITargetBodyParam(CelestialBody targetBody)
        {
            targetBodyID = targetBody.flightGlobalsIndex;
            targetBodyName = targetBody.name;
            targetTitle += targetBodyName;
        }

        public WBITargetBodyParam(CelestialBody targetBody, string bodyTitle)
        {
            targetBodyID = targetBody.flightGlobalsIndex;
            targetBodyName = targetBody.name;
            targetTitle = bodyTitle;
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return targetTitle;
        }

        protected override void OnRegister()
        {
            GameEvents.onDominantBodyChange.Add(onDominantBodyChange);
        }

        protected override void OnUnregister()
        {
            GameEvents.onDominantBodyChange.Remove(onDominantBodyChange);
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("targetBodyID", targetBodyID);
            node.AddValue("targetBodyName", targetBodyName);
            node.AddValue("targetTitle", targetTitle);
        }

        protected override void OnLoad(ConfigNode node)
        {
            targetBodyName = node.GetValue("targetBodyName");
            if (node.HasValue("targetTitle"))
                targetTitle = node.GetValue("targetTitle");
            targetBodyID = int.Parse(node.GetValue("targetBodyID"));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            if (FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex == targetBodyID)
                base.SetComplete();
            else
                base.SetIncomplete();
        }

        private void onDominantBodyChange(GameEvents.FromToAction<CelestialBody, CelestialBody> eventData)
        {
            if (eventData.to.flightGlobalsIndex == targetBodyID)
                base.SetComplete();
            else
                base.SetIncomplete();
        }
    }
}
