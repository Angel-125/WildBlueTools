using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum BallastTankTypes
    {
        Ballast,
        ForwardTrim,
        AftTrim
    }

    public class WBIBallastTank: PartModule
    {
        [KSPField]
        public string ballastResource = "IntakeLqd";

        [KSPField(isPersistant = true)]
        public BallastTankTypes tankType;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Tank Type: Ballast")]
        public void ToggleTankType()
        {
            switch (tankType)
            {
                case BallastTankTypes.Ballast:
                default:
                    tankType = BallastTankTypes.ForwardTrim;
                    break;

                case BallastTankTypes.ForwardTrim:
                    tankType = BallastTankTypes.AftTrim;
                    break;

                case BallastTankTypes.AftTrim:
                    tankType = BallastTankTypes.Ballast;
                    break;
            }

            updateGUI();
        }

        [KSPEvent(guiActive = true, guiName = "Blow Ballast")]
        public void BlowBallast()
        {
            if (tankType != BallastTankTypes.Ballast)
                return;

            if (string.IsNullOrEmpty(ballastResource))
                return;

            if (this.part.Resources.Contains(ballastResource))
                this.part.Resources[ballastResource].amount = 0.0f;
        }

        [KSPAction]
        public void BlowBallastAction(KSPActionParam param)
        {
            BlowBallast();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            updateGUI();
        }

        protected void updateGUI()
        {
            switch (tankType)
            {
                case BallastTankTypes.Ballast:
                default:
                    Events["ToggleTankType"].guiName = "Tank Type: Ballast";
                    break;

                case BallastTankTypes.ForwardTrim:
                    Events["ToggleTankType"].guiName = "Tank Type: Forward Trim";
                    break;

                case BallastTankTypes.AftTrim:
                    Events["ToggleTankType"].guiName = "Tank Type: Aft Trim";
                    break;
            }
        }
    }
}
