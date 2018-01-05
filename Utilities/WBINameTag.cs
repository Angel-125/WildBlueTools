using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBINameTag : PartModule
    {
        [KSPField(isPersistant = true)]
        public string nameTagURL = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isVisible;

        [KSPField()]
        public string toggleTagName = "Toggle Name Tag";

        [KSPField()]
        public string changeTagName = "Change Name Tag";

        [KSPField()]
        public string nameTagTransforms = string.Empty;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Events["ToggleNameTag"].guiName = toggleTagName;
            Events["GetNameTag"].guiName = changeTagName;

            changeNameTag();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Name Tag")]
        public void ToggleNameTag()
        {
            isVisible = !isVisible;
            changeNameTag();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Change Name Tag")]
        public void GetNameTag()
        {
            nameTagURL = HighLogic.CurrentGame.flagURL;
            FlagBrowser flagBrowser = (UnityEngine.Object.Instantiate((UnityEngine.Object)(new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = onFlagSelected;
        }

        private void onFlagSelected(FlagBrowser.FlagEntry selected)
        {
            nameTagURL = selected.textureInfo.name;
            changeNameTag();
        }

        protected void changeNameTag()
        {
            string[] tagTransforms = nameTagTransforms.Split(';');
            Transform[] targets;
            Texture textureForDecal;
            Renderer rendererMaterial;

            foreach (string transform in tagTransforms)
            {
                //Get the targets
                targets = part.FindModelTransforms(transform);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transform);
                    return;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = isVisible;

                    if (string.IsNullOrEmpty(nameTagURL) == false)
                    {
                        rendererMaterial = target.GetComponent<Renderer>();

                        textureForDecal = GameDatabase.Instance.GetTexture(nameTagURL, false);
                        if (textureForDecal != null)
                            rendererMaterial.material.SetTexture("_MainTex", textureForDecal);
                    }
                }
            }
        }

    }
}
