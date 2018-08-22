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
    public class WBIPressureOverride : VesselModule
    {
        #region Housekeeping
        public double maxPressureOverride;

        protected List<WBIDiveComputer> diveComputers;
        protected int partCount;
        #endregion

        #region Overrides
        public override Activation GetActivation()
        {
            return Activation.LoadedVessels;
        }

        public override bool ShouldBeActive()
        {
            return vessel.loaded;
        }

        protected override void OnStart()
        {
            base.OnStart();

            updateMaxPressure();
        }

        public void Update()
        {
            updateMaxPressure();
        }

        public void Destroy()
        {
            diveComputers = null;
        }
        #endregion

        #region Helpers
        protected virtual void updateMaxPressure()
        {
            //Update the list of dive computers
            if (partCount != this.vessel.parts.Count)
            {
                partCount = this.vessel.parts.Count;
                diveComputers = this.vessel.FindPartModulesImplementing<WBIDiveComputer>();
                if (diveComputers == null)
                    return;

                //If we don't have any dive coumputers then we're done.
                int count = diveComputers.Count;
                if (count == 0)
                    return;

                //Find the highest pressure override
                for (int index = 0; index < count; index++)
                {
                    if (diveComputers[index].maxPressureOverride > this.maxPressureOverride)
                        this.maxPressureOverride = diveComputers[index].maxPressureOverride;
                }

                //Now go through all the parts and override their max pressure
                Part part;
                for (int index = 0; index < partCount; index++)
                {
                    part = this.vessel.parts[index];
                    part.maxPressure = this.maxPressureOverride;
                }
            }
        }
        #endregion
    }
}
