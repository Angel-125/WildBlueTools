using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public class WBIReturnKerbalHome : WBIReturnHomeParam
    {
        public string kerbalName;
        ProtoCrewMember kerbal;

        public WBIReturnKerbalHome()
        {
        }

        public WBIReturnKerbalHome(string titleText, string kerbalName)
        {
            GameEvents.onVesselSituationChange.Add(onVesselSituationChange);
            this.titleText = titleText;
            this.kerbalName = kerbalName;
        }

        protected override string GetTitle()
        {
            return this.titleText;
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasValue("kerbalName"))
                kerbalName = node.GetValue("kerbalName");
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (!string.IsNullOrEmpty(kerbalName))
                node.AddValue("kerbalName", kerbalName);
        }

        protected override void checkSituation()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex == FlightGlobals.GetHomeBodyIndex() &&
                (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED || 
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED || 
                FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH))
            {
                if (kerbal == null)
                {
                    KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
                    if (roster.Exists(kerbalName))
                        kerbal = roster[kerbalName];
                }
                if (kerbal == null)
                {
                    isCompleted = false;
                    SetIncomplete();
                }

                if (FlightGlobals.ActiveVessel.GetVesselCrew().Contains(kerbal))
                {
                    isCompleted = true;
                    base.SetComplete();
                }
                else
                {
                    isCompleted = false;
                    SetIncomplete();
                }

            }
            else
            {
                base.SetIncomplete();
            }
        }
    }
}
