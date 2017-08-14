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
        int targetBodyID;
        string targetBodyName;

        public WBITargetBodyParam()
        {
        }

        public WBITargetBodyParam(CelestialBody targetBody)
        {
            targetBodyID = targetBody.flightGlobalsIndex;
            targetBodyName = targetBody.name;
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return "Transport experiment to " + targetBodyName;
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
        }

        protected override void OnLoad(ConfigNode node)
        {
            targetBodyName = node.GetValue("targetBodyName");
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
