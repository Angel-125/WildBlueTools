using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.Localization;

namespace WildBlueIndustries.Utilities
{
    public class WBIModuleUpdateHelper: PartModule
    {
        private ModuleResourceHarvester harvester = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            harvester = part.FindModuleImplementing<ModuleResourceHarvester>();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || harvester == null)
                return;

            Debug.Log("[WBIModuleUpdateHelper] - FixedUpdate called.");
        }
    }
}
