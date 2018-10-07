using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public delegate void BiomeUnlockedDelegate();

    public class HarvestView: InfoView
    {
        public Part part;
        public BiomeUnlockedDelegate onBiomeUnlocked;

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            WBIGeoLab geoLab = this.part.vessel.FindPartModuleImplementing<WBIGeoLab>();
            if (geoLab != null)
            {
                if (newValue)
                    WBIGeoLab.onBiomeUnlocked.Add(BiomeUnlocked);
                else
                    WBIGeoLab.onBiomeUnlocked.Remove(BiomeUnlocked);
            }
        }

        private void BiomeUnlocked()
        {
            if (this.onBiomeUnlocked != null)
                onBiomeUnlocked();
        }
    }
}
