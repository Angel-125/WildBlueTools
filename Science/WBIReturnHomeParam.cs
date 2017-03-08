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
        public WBIReturnHomeParam()
        {
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return "Return experiment to " + FlightGlobals.GetHomeBody().name;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex == FlightGlobals.GetHomeBodyIndex())
                base.SetComplete();
            else
                base.SetIncomplete();
        }
    }
}
