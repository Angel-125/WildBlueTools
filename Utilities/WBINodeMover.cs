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
    public class WBINodeMover : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool useAltNode;

        [KSPField]
        public string nodeName;

        [KSPField]
        public float altNodePosition;

        [KSPField]
        public float primaryNodePosition;

        AttachNode nodeToMove = null;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Node: Set to secondary")]
        public void ToggleNodePosition()
        {
            if (nodeToMove == null)
                return;
            if (nodeToMove.attachedPart != null)
            {
                ScreenMessages.PostScreenMessage("Remove parts before changing the node", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            useAltNode = !useAltNode;

            if (useAltNode)
                Events["ToggleNodePosition"].guiName = "Node: Primary";
            else
                Events["ToggleNodePosition"].guiName = "Node: Secondary";

            moveNode();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;
            if (string.IsNullOrEmpty(nodeName))
                return;

            //Find the node we want
            foreach (AttachNode attachNode in this.part.attachNodes)
            {
                if (attachNode.id == nodeName)
                    nodeToMove = attachNode;
            }
            if (nodeToMove == null)
                return;

            if (useAltNode)
                Events["ToggleNodePosition"].guiName = "Node: Primary";
            else
                Events["ToggleNodePosition"].guiName = "Node: Secondary";

            if (useAltNode)
                moveNode();
        }

        protected void moveNode()
        {
            //Update the node
            if (useAltNode)
                nodeToMove.position = new Vector3(0, altNodePosition, 0);
            else
                nodeToMove.position = new Vector3(0, primaryNodePosition, 0);
            nodeToMove.originalPosition = nodeToMove.position;
        }
    }
}
