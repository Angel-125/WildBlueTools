using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
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
    public class WBIAnimateRotate : ModuleAnimateGeneric
    {
        [KSPField()]
        public string rotationTransform = string.Empty;

        [KSPField()]
        public float rotationRate;

        [KSPField()]
        public string rotationAxis = string.Empty;

        [KSPField()]
        public bool showGUI = true;

        protected float rotationPerFrame;
        protected bool isDeployed;
        protected float currentAngle;
        protected Transform rotator;
        protected Vector3 axisRate = new Vector3(0,0,1);

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get rotations per frame
            rotationPerFrame = rotationRate * TimeWarp.fixedDeltaTime;

            //Get the rotation transform
            if (string.IsNullOrEmpty(rotationTransform) == false)
                rotator = this.part.FindModelTransform(rotationTransform);

            //Get the rotation axis
            if (string.IsNullOrEmpty(rotationAxis) == false)
            {
                string[] axisValues = rotationAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        axisRate.x = value * rotationPerFrame;
                    if (float.TryParse(axisValues[1], out value))
                        axisRate.y = value * rotationPerFrame;
                    if (float.TryParse(axisValues[2], out value))
                        axisRate.z = value * rotationPerFrame;
                }
            }

            else //Default is to rotate along the z-axis.
            {
                axisRate.z = 1 * rotationPerFrame;
            }

            //GUI
            Events["Toggle"].guiActive = showGUI;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Events["Toggle"].guiName == endEventGUIName)
                isDeployed = true;
            else
                isDeployed = false;

            if (isDeployed && rotator != null)
                rotator.Rotate(axisRate.x, axisRate.y, axisRate.z);
        }
    }
}
