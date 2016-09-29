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
    public class WBICamera : PartModule
    {
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Camera Name", isPersistant = true)]
        public string cameraName = "Camera";

        [KSPField]
        public string cameraTransform = "viewTransform";

        protected ExternalCameraView externalCameraView = new ExternalCameraView();

        [KSPEvent(guiName = "Rename Camera", guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f)]
        public void RenameCamera()
        {
            externalCameraView.cameraName = cameraName;
            externalCameraView.setCameraNameDelegate = SetCameraName;
            externalCameraView.SetVisible(true);
        }

        public void SetCameraName(string newCameraName)
        {
            cameraName = newCameraName;
        }

        public Transform GetCameraTransform()
        {
            Transform[] targets;

            targets = part.FindModelTransforms(cameraTransform);
            if (targets == null)
            {
                Debug.Log("No targets found for " + cameraTransform);
                return null;
            }

            return targets[0];
        }
    }
}
