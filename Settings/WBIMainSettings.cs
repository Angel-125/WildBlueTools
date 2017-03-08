using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIMainSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Require resources to reconfigure", toolTip = "If enabled, you'll need resources to reconfigure your parts.", autoPersistance = true)]
        public bool payToReconfigure = true;

        [GameParameters.CustomParameterUI("Require skills to reconfigure or repair", toolTip = "If enabled, you need skills to reconfigure or repair parts.", autoPersistance = true)]
        public bool requiresSkillCheck = true;

        [GameParameters.CustomParameterUI("Parts can break", toolTip = "If enabled, parts can break.", autoPersistance = true)]
        public bool partsCanBreak = true;

        [GameParameters.CustomParameterUI("Require resources to repair ", toolTip = "If enabled, you need resources to repair broken parts.", autoPersistance = true)]
        public bool repairsRequireResources = true;

        #region Properties
        public static bool PayToReconfigure
        {
            get
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                return settings.payToReconfigure;
            }

            set
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                settings.payToReconfigure = value;
            }
        }

        public static bool RequiresSkillCheck
        {
            get
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                return settings.requiresSkillCheck;
            }

            set
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                settings.repairsRequireResources = value;
            }
        }

        public static bool PartsCanBreak
        {
            get
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                return settings.partsCanBreak;
            }

            set
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                settings.partsCanBreak = value;
            }
        }

        public static bool RepairsRequireResources
        {
            get
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                return settings.repairsRequireResources;
            }

            set
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                settings.repairsRequireResources = value;
            }
        }
        #endregion

        #region CustomParameterNode
        public override string Section
        {
            get
            {
                return "Wild Blue";
            }
        }

        public override string Title
        {
            get
            {
                return "Reconfigure & Repair";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
        #endregion
    }


    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class WBISettingsMonitor : MonoBehaviour
    {
        public void Awake()
        {
            GameEvents.OnGameSettingsApplied.Add(UpdateSettings);
        }

        public void Destroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(UpdateSettings);
        }

        public void UpdateSettings()
        {
        }
    }
}
