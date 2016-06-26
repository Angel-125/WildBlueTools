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
    public class InternalPlasmaScreen : WBIInternalModule
    {
        private const float kMinimumSwitchTime = 30.0f;

        [KSPField]
        public float screenSwitchTime = 30f;

        [KSPField]
        public string screenTransform = "Screen";

        public string imagePath;

        protected PlasmaScreenView screenView = new PlasmaScreenView();
        protected WBIPropStateHelper propStateHelper;
        protected bool enableRandomImages;
        protected double cycleStartTime;
        protected double elapsedTime;

        protected void OnGUI()
        {
            try
            {
                if (screenView.IsVisible())
                    screenView.DrawWindow();
            }
            catch (Exception ex)
            {
                Debug.Log("Error in InternalPlasmaScreen-OnGUI: " + ex.ToString());
            }
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            propStateHelper = this.part.FindModuleImplementing<WBIPropStateHelper>();
            if (propStateHelper != null)
            {
                imagePath = propStateHelper.LoadProperty(internalProp.propID, "imagePath");
                if (string.IsNullOrEmpty(imagePath) == false && System.IO.File.Exists(imagePath))
                {
                    Texture2D image = new Texture2D(1, 1);
                    WWW www = new WWW("file://" + imagePath);

                    www.LoadImageIntoTexture(image);

                    ShowImage(image, imagePath);
                }

                string value = propStateHelper.LoadProperty(internalProp.propID, "enableRandomScreens");
                if (string.IsNullOrEmpty(value) == false)
                    enableRandomImages = bool.Parse(value);
                if (enableRandomImages)
                    cycleStartTime = Planetarium.GetUniversalTime();

                value = propStateHelper.LoadProperty(internalProp.propID, "screenSwitchTime");
                if (string.IsNullOrEmpty(value) == false)
                    screenSwitchTime = float.Parse(value);
            }

            Transform trans = internalProp.FindModelTransform("ScreenTrigger");
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
                    clickWatcher.clickDelegate = OnScreenClick;
                }
            }

            screenView.showImageDelegate = ShowImage;
        }

        public void FixedUpdate()
        {
            if (enableRandomImages == false)
                return;

            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;

            float completionRatio = (float)(elapsedTime / screenSwitchTime);
            if (completionRatio >= 1.0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                screenView.GetRandomImage();
            }
        }

        public void OnScreenClick()
        {
            screenView.part = this.part;
            screenView.enableRandomImages = enableRandomImages;
            screenView.screenSwitchTime = screenSwitchTime;
            screenView.SetVisible(true);
        }

        public void ShowImage(Texture texture, string textureFilePath)
        {
            Transform[] targets;
            Renderer rendererMaterial;

            //Save the image path
            imagePath = textureFilePath;
            if (propStateHelper != null)
                propStateHelper.SaveProperty(internalProp.propID, "imagePath", imagePath);

            //Get the targets
            targets = internalProp.FindModelTransforms(screenTransform);
            if (targets == null)
            {
                return;
            }

            //Now, replace the textures in each target
            foreach (Transform target in targets)
            {
                rendererMaterial = target.GetComponent<Renderer>();
                rendererMaterial.material.SetTexture("_MainTex", texture);
                rendererMaterial.material.SetTexture("_Emissive", texture);
            }

            //Finally, record the random screen switch state
            enableRandomImages = screenView.enableRandomImages;
            screenSwitchTime = screenView.screenSwitchTime;
            if (propStateHelper != null)
            {
                if (screenSwitchTime < kMinimumSwitchTime)
                    screenSwitchTime = kMinimumSwitchTime;

                propStateHelper.SaveProperty(internalProp.propID, "enableRandomImages", enableRandomImages.ToString());
                propStateHelper.SaveProperty(internalProp.propID, "screenSwitchTime", screenSwitchTime.ToString());

                if (enableRandomImages)
                    cycleStartTime = Planetarium.GetUniversalTime();
            }

        }

    }
}
