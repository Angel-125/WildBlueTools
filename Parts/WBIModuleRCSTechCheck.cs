using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIModuleRCSTechCheck : WBIMeshHelper
    {
        protected bool upgradeChecked = false;

        protected void checkForUpgrade()
        {
            ModuleRCSFX rcsModule = this.part.FindModuleImplementing<ModuleRCSFX>();
            if (rcsModule == null)
                return;

            //Set the upgrade checked flag
            upgradeChecked = true;

            //If the RCS module is enabled then show the RCS ports.
            if (rcsModule.moduleIsEnabled)
                setObject(0);
            else
                setObject(-1);

            //Switch ourself off.
            this.isEnabled = false;
            this.enabled = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
            {
                setObject(-1);
                checkForUpgrade();
            }
        }
    }
}
