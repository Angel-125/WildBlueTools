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

        [KSPField]
        public bool showAlphaControl;

        [KSPField]
        public string alphaTransforms = string.Empty;

        public string imagePath;

        protected PlasmaScreenView screenView = new PlasmaScreenView();
        protected WBIPropStateHelper propStateHelper;
        protected bool enableRandomImages;
        protected double cycleStartTime;
        protected double elapsedTime;
        protected Texture defaultTexture;
        protected Renderer rendererMaterial;
        protected bool screenIsVisible = true;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            getDefaultTexture();

            //Get the screen's render material
            Transform target = internalProp.FindModelTransform(screenTransform);
            if (target == null)
                return;
            rendererMaterial = target.GetComponent<Renderer>();
            if (showAlphaControl)
                SetScreenVisible(screenIsVisible);

            //Get the prop state helper.
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

                value = propStateHelper.LoadProperty(internalProp.propID, "screenIsVisible");
                if (string.IsNullOrEmpty(value) == false)
                    screenIsVisible = bool.Parse(value);
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
           
            //Setup screen view
            screenView.showImageDelegate = ShowImage;
            screenView.toggleScreenDelegate = SetScreenVisible;
            screenView.showAlphaControl = this.showAlphaControl;
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
            screenView.screenIsVisible = this.screenIsVisible;
            screenView.SetVisible(true);
        }

        public void SetScreenVisible(bool isVisible)
        {
            if (string.IsNullOrEmpty(alphaTransforms))
                return;

            string[] transforms = alphaTransforms.Split(new char[] { ';' });
            Transform target = null;

            foreach (string transform in transforms)
            {
                target = internalProp.FindModelTransform(transform);
                if (target !=  null)
                    target.gameObject.SetActive(isVisible);
            }

            screenIsVisible = isVisible;
            if (propStateHelper != null)
                propStateHelper.SaveProperty(internalProp.propID, "screenIsVisible", screenIsVisible.ToString());
        }

        public void ShowImage(Texture texture, string textureFilePath)
        {
            //Save the image path
            imagePath = textureFilePath;
            if (propStateHelper != null)
                propStateHelper.SaveProperty(internalProp.propID, "imagePath", imagePath);

            //Now, replace the textures in each target
            rendererMaterial.material.SetTexture("_MainTex", texture);
            rendererMaterial.material.SetTexture("_Emissive", texture);

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

        protected void getDefaultTexture()
        {
            Transform target;
            Renderer rendererMaterial;

            //Get the target
            target = internalProp.FindModelTransform(screenTransform);
            if (target == null)
            {
                return;
            }

            rendererMaterial = target.GetComponent<Renderer>();
            defaultTexture = rendererMaterial.material.GetTexture("_MainTex");
            screenView.defaultTexture = defaultTexture;
        }

    }
}
