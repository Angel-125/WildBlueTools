using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    internal class WBIKISInventoryView : Dialog<WBIKISInventoryView>
    {
        private Vector2 scrollPos;
        public List<WBIKISInventoryWrapper> inventories;
        public Part part;

        public WBIKISInventoryView() :
        base("Configure Storage", 640, 480)
        {
            Resizable = false;
            scrollPos = new Vector2(0, 0);
        }

        protected override void DrawWindowContents(int windowId)
        {
            DrawView();
        }

        public void DrawView()
        {
            if (inventories == null || inventories.Count == 0)
            {
                GUILayout.Label("No inventories");
                return;
            }

            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            int totalSeats = inventories.Count;
            string seatName;
            int seatIndex;
            if (HighLogic.LoadedSceneIsEditor)
            {
                for (int index = 0; index < totalSeats; index++)
                {
                    //Get seat name
                    seatName = "Seat " + index + " Inventory";

                    //Show inventory
                    if (GUILayout.Button(seatName))
                    {
                        inventories[index].inventoryModule.Events["ToggleInventoryEvent"].Invoke();
                    }
                }
            }

            else if (HighLogic.LoadedSceneIsFlight)
            {
                for (int index = 0; index < totalSeats; index++)
                {
                    //Get the seat index
                    seatIndex = inventories[index].podSeat;

                    //Make sure there is a crew member in the seat
                    if (this.part.internalModel.seats[seatIndex].crew == null)
                        continue;

                    //Get seat name
                    seatName = this.part.internalModel.seats[seatIndex].crew.name + "'s Inventory";

                    //Show inventory
                    if (GUILayout.Button(seatName))
                        inventories[index].inventoryModule.Events["ToggleInventoryEvent"].Invoke();
                }
            }

            else
            {
                GUILayout.Label("No inventories");
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }
    }
}
