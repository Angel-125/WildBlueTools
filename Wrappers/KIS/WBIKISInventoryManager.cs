using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

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
    public class WBIKISInventoryManager : PartModule, IOpsView
    {
        List<WBIKISInventoryWrapper> inventories = new List<WBIKISInventoryWrapper>();
        WBIKISInventoryView inventoryView;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, guiName = "View Crew Inventories")]
        public void ToggleInventories()
        {
            inventoryView.SetVisible(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight == false && HighLogic.LoadedSceneIsEditor == false)
                return;

            //Find all the seat inventories and hide their GUI.
            inventoryView = new WBIKISInventoryView();
            inventoryView.part = this.part;
            findInventories();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false && HighLogic.LoadedSceneIsEditor == false)
                return;

            //Hide inventories if needed.
            if (inventories.Count > 0)
            {
                if (inventories[0].inventoryModule.Events["ToggleInventory"].guiActive == true)
                {
                    for (int index = 0; index < inventories.Count; index++)
                        inventories[index].HideToggleInventory();
                }
            }
        }


        #region IOpsView

        protected void findInventories()
        {
            WBIKISInventoryWrapper wrapper;
            foreach (PartModule partModule in this.part.Modules)
            {
                if (partModule.moduleName == "ModuleKISInventory")
                {
                    wrapper = new WBIKISInventoryWrapper(partModule);
                    if (wrapper.invType == WBIKISInventoryWrapper.InventoryType.Pod)
                    {
                        inventories.Add(wrapper);
                        wrapper.HideToggleInventory();
                    }
                }
            }

            inventoryView.inventories = this.inventories;
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();

            buttonLabels.Add("Inventories");

            return buttonLabels;
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            if (this.inventories.Count == 0)
                findInventories();
            inventoryView.DrawView();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
        #endregion
    }
}
