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
    public class WBIReturnHomeParam : ContractParameter
    {
        protected bool isCompleted;
        public string titleText = "Land or splash completed experiment on ";
       
        public WBIReturnHomeParam()
        {
            GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
        }

        public WBIReturnHomeParam(string titleText)
        {
            this.titleText = titleText;
            GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return titleText + FlightGlobals.GetHomeBody().name;
        }

        protected void onVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> hfta)
        {
            if (FlightGlobals.ActiveVessel != hfta.host)
                return;
            checkCompletion();
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("isCompleted"))
                isCompleted = bool.Parse(node.GetValue("isCompleted"));
            if (node.HasValue("titleText"))
                titleText = node.GetValue("titleText");
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("isCompleted", isCompleted);
            node.AddValue("titleText", titleText);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            checkCompletion();
        }

        protected void checkCompletion()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (isCompleted)
                return;

            //If the experiment hasn't been completed then we can't be complete.
            if (Root is WBIResearchContract)
            {
                WBIResearchContract contract = (WBIResearchContract)Root;
                if (contract.versionNumber >= WBIResearchContract.CurrentContractVersion)
                {
                    if (contract.experimentCompleted == false)
                    {
                        base.SetIncomplete();
                        return;
                    }
                }
            }

            //Check situation
            checkSituation();
        }

        protected virtual void checkSituation()
        {
            if (FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex == FlightGlobals.GetHomeBodyIndex() &&
                (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED || FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED))
            {
                isCompleted = true;
                base.SetComplete();
            }
            else
            {
                base.SetIncomplete();
            }
        }
    }
}
