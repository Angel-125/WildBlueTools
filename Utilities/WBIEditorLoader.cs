using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.Localization;

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
    public class WBIEditorLoader: PartModule
    {
        [KSPField]
        public bool loadVAB = true;

        [KSPEvent(guiActive = true, guiName = "Jump To VAB")]
        public void loadEditor()
        {

            //Save the game.
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);

            // Setup facility
            EditorDriver.StartupBehaviour = EditorDriver.StartupBehaviours.START_CLEAN;

            if (loadVAB)
                EditorDriver.StartEditor(EditorFacility.VAB);
            else
                EditorDriver.StartEditor(EditorFacility.SPH);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (loadVAB)
                Events["loadEditor"].guiName = "Jump To VAB";
            else
                Events["loadEditor"].guiName = "Jump To SPH";
        }
    }
}
