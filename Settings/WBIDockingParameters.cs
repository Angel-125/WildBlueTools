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
    public class WBIDockingParameters : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Welding requires EVA", toolTip = "If enabled, an EVA is required to weld the port.", autoPersistance = true)]
        public bool weldRequiresEVA = true;

        [GameParameters.CustomParameterUI("Welding requires the repair skill", toolTip = "If enabled, only kerbals with the repair skill can perform the weld.", autoPersistance = true)]
        public bool weldRequiresRepairSkill = true;

        [GameParameters.CustomParameterUI("Keep docking ports after welding", toolTip = "If enabled, docking ports won't be deleted when welded together.", autoPersistance = true)]
        public bool keepDockingPorts = false;

        public static bool WeldRequiresEVA
        {
            get
            {
                WBIDockingParameters settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIDockingParameters>();
                return settings.weldRequiresEVA;
            }
        }

        public static bool WeldRequiresRepairSkill
        {
            get
            {
                WBIDockingParameters settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIDockingParameters>();
                return settings.weldRequiresRepairSkill;
            }
        }

        public static bool KeepDockingPorts
        {
            get
            {
                WBIDockingParameters settings = HighLogic.CurrentGame.Parameters.CustomParams<WBIDockingParameters>();
                return settings.keepDockingPorts;
            }
        }

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
                return "Welded Docking Ports";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 2;
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

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            bool experienceEnabled = Utils.IsExperienceEnabled();

            if (member.Name == "WeldRequiresRepairSkill" && experienceEnabled)
                return true;
            else if (member.Name == "WeldRequiresRepairSkill" && !experienceEnabled)
                return false;

            return base.Enabled(member, parameters);
        }
        #endregion
    }
}
