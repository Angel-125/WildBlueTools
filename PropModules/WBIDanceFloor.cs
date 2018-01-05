using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIDanceFloor : InternalModule
    {
        [KSPField]
        public string danceFloorTransformName = "DanceFloor";

        [KSPField]
        public string danceFloorImagePath = "WildBlueIndustries/000WildBlueTools/Props/Assets/";

        [KSPField]
        public string danceFloorImageName = "DanceFloor";

        [KSPField]
        public float imageSwitchTime = 0.25f;

        static List<Texture2D> danceFloorImages = new List<Texture2D>();
        static int totalImages = 0;
        Transform danceFloorTransform;
        protected Texture2D floorTexture;
        protected Renderer rendererMaterial;
        protected double cycleStartTime;
        protected double elapsedTime;

        public void Start()
        {
            danceFloorTransform = internalProp.FindModelTransform(danceFloorTransformName);
            if (danceFloorTransform == null)
                return;
            rendererMaterial = danceFloorTransform.GetComponent<Renderer>();

            //Cycle start time
            cycleStartTime = Planetarium.GetUniversalTime();

            //Find the dance floor images
            if (totalImages == 0)
            {
                WWW www;
                string imagePath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + danceFloorImagePath;
                string[] imagePaths = Directory.GetFiles(imagePath);
                for (int index = 0; index < imagePaths.Length; index++)
                {
                    imagePath = imagePaths[index];
                    if (imagePath.Contains(danceFloorImageName))
                    {
                        Texture2D imageTexture = new Texture2D(1, 1);
                        www = new WWW("file://" + imagePath);
                        www.LoadImageIntoTexture(imageTexture);
                        danceFloorImages.Add(imageTexture);
                    }
                }

                totalImages = danceFloorImages.Count();
            }
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
//            if (totalImages == 0)
//                return;
            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;

            float completionRatio = (float)(elapsedTime / imageSwitchTime);
            if (completionRatio >= 1.0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();

                int index = UnityEngine.Random.Range(0, totalImages);
                floorTexture = danceFloorImages[index];
                rendererMaterial.material.SetTexture("_MainTex", floorTexture);
                rendererMaterial.material.SetTexture("_Emissive", floorTexture);
            }
        }
    }
}
