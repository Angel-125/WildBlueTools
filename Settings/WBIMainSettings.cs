using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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

        [GameParameters.CustomParameterUI("Require skills to reconfigure", toolTip = "If enabled, you need skills to reconfigure parts.", autoPersistance = true)]
        public bool requiresSkillCheck = true;

        [GameParameters.CustomParameterUI("Enable Debug Logging", toolTip = "If enabled, your logs will be spammed with debug info.", autoPersistance = true)]
        public bool enableDebugLogging = false;

        #region Properties
        public static bool EnableDebugLogging
        {
            get
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                return settings.enableDebugLogging;
            }

            set
            {
                WBIMainSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIMainSettings>();
                settings.enableDebugLogging = value;
            }
        }

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
                settings.requiresSkillCheck = value;
            }
        }
        #endregion

        #region CustomParameterNode
        public override string DisplaySection
        {
            get 
            {
                return Section;
            }
        }

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
}
