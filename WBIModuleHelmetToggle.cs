using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace WildBlueIndustries
{
    public class WBIModuleHelmetToggle: PartModule
    {
        public KerbalEVA kerbalEVA;

        [KSPEvent(guiActive = true, guiName = "Acting! Toggle Helmet")]
        public void ToggleHelmet()
        {
            bool helmetVisible = kerbalEVA.helmetTransform.gameObject.activeSelf;
            helmetVisible = !helmetVisible;

            setupSuitMeshes(helmetVisible);

        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            kerbalEVA = this.part.vessel.FindPartModuleImplementing<KerbalEVA>();
        }

        void setupSuitMeshes(bool isVisible)
        {
            Collider collider;

            //Toggle helmet
            kerbalEVA.helmetTransform.gameObject.SetActive(isVisible);
            collider = kerbalEVA.helmetTransform.gameObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isVisible;

            //Toggle neck ring
            kerbalEVA.neckRingTransform.gameObject.SetActive(isVisible);
            collider = kerbalEVA.neckRingTransform.gameObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isVisible;

            //Fire event
            GameEvents.OnHelmetChanged.Fire(kerbalEVA, isVisible, isVisible);
        }
    }
}
