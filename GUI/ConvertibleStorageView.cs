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
    public delegate void PreviewTemplate(string templateName);
    public delegate void SetTemplate(string template);
    public delegate void SetupView();

    public class ConvertibleStorageView : Window<ConvertibleStorageView>
    {
        public string info;
        public Texture decal;
        public string requiredResource = string.Empty;
        public float resourceCost = 100f;
        public string templateName;
        public int templateCount = -1;
        public string requiredSkill = string.Empty;
        public TemplateManager templateManager;

        public PreviewTemplate previewTemplate;
        public SetTemplate setTemplate;
        public SetupView setupView;

        private Vector2 _scrollPos;
        private Vector2 _scrollPosTemplates;
        private bool confirmReconfigure;
        private GUILayoutOption[] buttonOption = new GUILayoutOption[] { GUILayout.Width(48), GUILayout.Height(48) };

        public ConvertibleStorageView() :
        base("Configure Storage", 640, 480)
        {
            Resizable = false;
            _scrollPos = new Vector2(0, 0);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
        }

        protected override void DrawWindowContents(int windowId)
        {
            DrawView();
        }

        public void DrawView()
        {
            if (templateCount == -1 && setupView != null)
                setupView();
            string buttonLabel;
            string panelName;
            Texture buttonDecal;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(250) });

            GUILayout.Label("<color=white>Configuration: " + templateName + "</color>");

            if (string.IsNullOrEmpty(requiredResource) == false && resourceCost != 0f)
                GUILayout.Label(string.Format("<color=white>Cost: {0:s} ({1:f2})</color>", requiredResource, resourceCost));
            else
                GUILayout.Label("<color=white>Cost: NONE</color>");

            if (string.IsNullOrEmpty(requiredSkill) == false)
                GUILayout.Label("<color=white>Reconfigure Skill: " + requiredSkill + "</color>");
            else
                GUILayout.Label("<color=white>Reconfigure Skill: NONE</color>");

            //Templates
            _scrollPosTemplates = GUILayout.BeginScrollView(_scrollPosTemplates);
            foreach (ConfigNode nodeTemplate in this.templateManager.templateNodes)
            {
                //Button label
                if (nodeTemplate.HasValue("title"))
                    buttonLabel = nodeTemplate.GetValue("title");
                else if (nodeTemplate.HasValue("shortName"))
                    buttonLabel = nodeTemplate.GetValue("shortName");
                else
                    buttonLabel = nodeTemplate.GetValue("name");

                //Icon
                panelName = nodeTemplate.GetValue("logoPanel");
                if (panelName != null)
                    buttonDecal = GameDatabase.Instance.GetTexture(panelName, false);
                else
                    buttonDecal = null;

                //Button
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(buttonDecal, buttonOption))
                {
                    previewTemplate(nodeTemplate.GetValue("shortName"));
                }
                if (TemplateManager.TemplateTechResearched(nodeTemplate))
                    GUILayout.Label("<color=white>" + buttonLabel + "</color>");
                else
                    GUILayout.Label("<color=#959595>" + buttonLabel + "\r\nNeeds " + TemplateManager.GetTechTreeTitle(nodeTemplate) + "</color>");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Reconfigure") && setTemplate != null)
            {
                ConfigNode node = templateManager[templateName];
                if (confirmReconfigure || HighLogic.LoadedSceneIsEditor)
                {
                    if (TemplateManager.TemplateTechResearched(node))
                    {
                        setTemplate(templateName);
                        confirmReconfigure = false;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Unable to use " + templateName + ". Research " + TemplateManager.GetTechTreeTitle(node) + " first.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }

                else
                {
                    ScreenMessages.PostScreenMessage("Click again to confirm reconfiguration.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    confirmReconfigure = true;
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(390) });
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            if (decal != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(decal, new GUILayoutOption[] { GUILayout.Width(128), GUILayout.Height(128) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(info);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
