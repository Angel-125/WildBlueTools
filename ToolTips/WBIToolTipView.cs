using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public delegate void OkButtonPressedDelegate();
    public delegate void CancelButtonPressedDelegate();

    public class WBIToolTipView : Dialog<WBIToolTipView>
    {
        public static string kOkText = "OK";
        public static string kYesText = "Yes";
        public static string kCancelText = "Cancel";
        public static string kNoText = "No";
        public static string kGotItText = "Got it, thanks!";

        public Texture image;
        public string title;
        public string toolTip;
        public bool showCancelButton;
        public string okButtonText = kOkText;
        public string cancelButtonText = kCancelText;
        public OkButtonPressedDelegate okDelegate;
        public CancelButtonPressedDelegate cancelDelegate;

        private Vector2 scrollPos;
        private GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Width(320) };

        public WBIToolTipView() :
        base("Tip", 320, 200)
        {
            Resizable = false;
            scrollPos = new Vector2(0, 0);
        }

        public void LoadConfig(ConfigNode node)
        {
            if (node.HasValue(WBIToolTipManager.kTitle))
            {
                title = node.GetValue(WBIToolTipManager.kTitle);
                WindowTitle = title;
            }

            if (node.HasValue(WBIToolTipManager.kDescription))
                toolTip = node.GetValue(WBIToolTipManager.kDescription);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            scrollPos = Vector2.zero;

            if (!string.IsNullOrEmpty(title))
                WindowTitle = title;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, panelOptions);

            //Image
            if (image != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(image);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            //Text
            GUILayout.Label(toolTip);

            GUILayout.EndScrollView();

            //Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(okButtonText))
            {
                SetVisible(false);
                if (okDelegate != null)
                    okDelegate();
            }

            if (showCancelButton)
            {
                if (GUILayout.Button(cancelButtonText))
                {
                    SetVisible(false);
                    if (cancelDelegate != null)
                        cancelDelegate();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
