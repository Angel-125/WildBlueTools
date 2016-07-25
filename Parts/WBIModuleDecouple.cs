using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Module Decouple")]
    public class WBIModuleDecouple : ModuleDecouple
    {
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Type")]
        public string decouplerType = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isDecoupler;

        [KSPField()]
        public string decalTransform = string.Empty;

        [KSPField()]
        public string decals = string.Empty;

        string[] decalNames;

        [KSPEvent(guiActiveEditor = true, guiName = "Decoupler")]
        public void ToggleDecoupler()
        {
            isDecoupler = !isDecoupler;

            setup_decoupler();
        }

        public override void OnStart(StartState state)
        {
            if (!string.IsNullOrEmpty(decals))
                decalNames = decals.Split(new char[] { ';' });

            setup_decoupler();

            base.OnStart(state);

            //Handle surface-attached decouplers
            if (this.part.attachMode == AttachModes.SRF_ATTACH)
            {
                ExplosiveNode = this.part.srfAttachNode;
                return;
            }
        }

        private void setup_decoupler()
        {
            if (isDecoupler)
            {
                Events["ToggleDecoupler"].guiName = "Change To Separator";
                if (!string.IsNullOrEmpty(decals))
                    changeDecals(decalNames[0]);
                decouplerType = "Decoupler";
            }
            else
            {
                Events["ToggleDecoupler"].guiName = "Change To Decoupler";
                if (!string.IsNullOrEmpty(decals))
                    changeDecals(decalNames[1]);
                decouplerType = "Separator";
            }

            isOmniDecoupler = !isDecoupler;
        }

        protected void changeDecals(string decalName)
        {
            if (string.IsNullOrEmpty(decalTransform))
                return;
            if (string.IsNullOrEmpty(decals))
                return;

            Transform[] targets;
            Texture textureForDecal;
            Renderer rendererMaterial;

            //Sanity checks
            if (decalTransform == null)
            {
                return;
            }

            //Get the targets
            targets = part.FindModelTransforms(decalTransform);
            if (targets == null)
                return;

            //Now, replace the textures in each target
            foreach (Transform target in targets)
            {
                rendererMaterial = target.GetComponent<Renderer>();

                textureForDecal = GameDatabase.Instance.GetTexture(decalName, false);
                if (textureForDecal != null)
                    rendererMaterial.material.SetTexture("_MainTex", textureForDecal);
            }
        }
    }
}
