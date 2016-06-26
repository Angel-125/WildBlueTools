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
    [KSPModule("Plasma TV")]
    public class WBIPlasmaTV : PartModule
    {
        [KSPField(isPersistant = true)]
        public string imagePath;

        [KSPField]
        public string screenTransform = "Screen";

        [KSPField]
        public string aspectRatio;

        protected PlasmaScreenView screenView = new PlasmaScreenView();

        [KSPEvent(guiName = "Toggle GUI", guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f)]
        public void ToggleGUI()
        {
            bool isVisible = !screenView.IsVisible();

            screenView.part = this.part;
            screenView.aspectRatio = this.aspectRatio;
            screenView.SetVisible(isVisible);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            screenView.part = this.part;
            screenView.showImageDelegate = ShowImage;

            if (string.IsNullOrEmpty(imagePath) == false)
            {
                Texture2D image = new Texture2D(1, 1);
                WWW www = new WWW("file://" + imagePath);
                www.LoadImageIntoTexture(image);

                ShowImage(image, imagePath);
            }
        }

        private void OnGUI()
        {
            try
            {
                if (screenView.IsVisible())
                    screenView.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.Log("Error in WBIPlasmaTV-OnGUI: " + ex.ToString());
            }
        }

        public void ShowImage(Texture texture, string textureFilePath)
        {
            Transform[] targets;
            Renderer rendererMaterial;

            //Save the image path
            imagePath = textureFilePath;

            //Get the targets
            targets = part.FindModelTransforms(screenTransform);
            if (targets == null)
            {
                Debug.Log("No targets found for " + screenTransform);
                return;
            }

            //Now, replace the textures in each target
            foreach (Transform target in targets)
            {
                rendererMaterial = target.GetComponent<Renderer>();
                rendererMaterial.material.SetTexture("_MainTex", texture);
                rendererMaterial.material.SetTexture("_Emissive", texture);
            }

        }
    }
}
