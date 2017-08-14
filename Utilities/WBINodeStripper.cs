using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBINodeStripper : PartModule
    {
        [KSPField(isPersistant = true, guiName = "Strip Unused Nodes", guiActive = true, guiActiveEditor = true)]
        [UI_Toggle(enabledText = "YES", disabledText = "NO")]
        public bool stripUnusedNodes;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!stripUnusedNodes)
                return;
           
            stripNodes();

            this.enabled = false;
            this.isEnabled = false;
        }

        protected void stripNodes()
        {
            List<AttachNode> doomedNodes = new List<AttachNode>();

            foreach (AttachNode node in this.part.attachNodes)
            {
                if (node.attachedPart == null)
                    doomedNodes.Add(node);
            }

            foreach (AttachNode doomed in doomedNodes)
                this.part.attachNodes.Remove(doomed);
        }
    }
}
