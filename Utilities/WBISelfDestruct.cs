using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
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
    [KSPModule("Self Destruct")]
    public class WBISelfDestruct : PartModule
    {
        [KSPField()]
        public bool poofNotBoom;

        [KSPField(guiName = "Self Destruct", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        [UI_Toggle(enabledText = "Armed", disabledText = "Disarmed")]
        public bool isArmed;

        [KSPField]
        public float explosionPotential = -1.0f;

        [KSPField]
        public bool explodeWhenStaged = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (explosionPotential > 0f)
                this.part.explosionPotential = explosionPotential;

            if (explodeWhenStaged)
                GameEvents.onStageActivate.Add(onStageActivate);
        }

        [KSPEvent(guiActive = true)]
        public void Detonate()
        {
            if (!isArmed)
            {
                //ScreenMessages.PostScreenMessage("Explosive charges are currently disarmed, cannot detonate.", 3.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            if (!poofNotBoom)
                this.part.explode();
            else
                this.part.Die();
        }

        [KSPAction("Detonate")]
        public void ActionDetonate(KSPActionParam param)
        {
            Detonate();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Events["Detonate"].guiActive = isArmed;
        }

        public void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(onStageActivate);
        }

        protected void onStageActivate(int stageID)
        {
            if (this.part.inverseStage == stageID)
                Detonate();
        }
    }
}
