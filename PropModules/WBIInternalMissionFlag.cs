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
    class WBIInternalMissionFlag : WBIInternalModule
    {
        [KSPField]
        public string flagTransformName = "flagTransform";

        Transform flagTransform;
        Texture flagTexture;
        protected WBIPropStateHelper propStateHelper;
        protected Renderer rendererMaterial;
        string imageURL;

        public void Start()
        {
            //Flag transform
            flagTransform = internalProp.FindModelTransform(flagTransformName);
            if (flagTransform == null)
                return;

            //Render material
            rendererMaterial = flagTransform.GetComponent<Renderer>();

            //Flag to show
            propStateHelper = this.part.FindModuleImplementing<WBIPropStateHelper>();
            if (propStateHelper != null)
            {
                imageURL = propStateHelper.LoadProperty(internalProp.propID, "imageURL");

                //Use mission flag if we have none set.
                if (string.IsNullOrEmpty(imageURL))
                    imageURL = HighLogic.CurrentGame.flagURL;
            }

            else //Use mission flag.
            {
                imageURL = HighLogic.CurrentGame.flagURL;
            }
            setFlagImage();

            //Trigger
            Transform trans = internalProp.FindModelTransform("Collider");
            if (trans != null)
            {
                GameObject goButton = trans.gameObject;
                if (goButton != null)
                {
                    ButtonClickWatcher clickWatcher = goButton.GetComponent<ButtonClickWatcher>();
                    if (clickWatcher == null)
                    {
                        clickWatcher = goButton.AddComponent<ButtonClickWatcher>();
                    }
                    clickWatcher.clickDelegate = OnFlagClick;
                }
            }
        }

        public void OnFlagClick()
        {
            imageURL = HighLogic.CurrentGame.flagURL;
            FlagBrowser flagBrowser = (UnityEngine.Object.Instantiate((UnityEngine.Object)(new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = onFlagSelected;
        }

        private void onFlagSelected(FlagBrowser.FlagEntry selected)
        {
            imageURL = selected.textureInfo.name;
            propStateHelper.SaveProperty(internalProp.propID, "imageURL", imageURL);
            setFlagImage();
        }

        private void setFlagImage()
        {
            flagTexture = GameDatabase.Instance.GetTexture(imageURL, false);
            if (flagTexture != null)
                rendererMaterial.material.SetTexture("_MainTex", flagTexture);
        }
    }
}
