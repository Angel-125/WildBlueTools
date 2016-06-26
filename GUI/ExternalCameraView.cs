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
    public delegate void SetCameraNameDelegate(string newCameraName);

    public class ExternalCameraView : Window<ExternalCameraView>
    {
        public string cameraName;
        public SetCameraNameDelegate setCameraNameDelegate;

        public ExternalCameraView() :
        base("Camera Name", 300, 100)
        {
            Resizable = false;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            cameraName = GUILayout.TextField(cameraName);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                if (setCameraNameDelegate != null)
                    setCameraNameDelegate(cameraName);

                SetVisible(false);
            }

            if (GUILayout.Button("Cancel"))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
