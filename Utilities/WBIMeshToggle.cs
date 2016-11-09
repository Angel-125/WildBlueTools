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

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIMeshToggle : WBIMeshHelper
    {
        [KSPField(isPersistant = true)]
        public bool meshesVisible;

        [KSPField(isPersistant = true)]
        public string showMeshesName;

        [KSPField(isPersistant = true)]
        public string hideMeshesName;

        [KSPField(isPersistant = true)]
        public bool guiVisible;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Toggle Meshes")]
        public void ToggleMeshes()
        {
            meshesVisible = !meshesVisible;

            updateGui();
        }

        public override void OnStart(StartState state)
        {
            showGui = false;
            base.OnStart(state);

            updateGui();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            updateGui();
        }

        protected void updateGui()
        {            
            if (meshesVisible)
            {
                showAll();
                Events["ToggleMeshes"].guiName = hideMeshesName;
            }
            else
            {
                hideAll();
                Events["ToggleMeshes"].guiName = showMeshesName;
            }

            Events["ToggleMeshes"].guiActive = guiVisible;
            Events["ToggleMeshes"].guiActiveEditor = guiVisible;
            Events["ToggleMeshes"].guiActiveUnfocused = guiVisible;
        }
    }
}
