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
    public class WBIPowerMonitor : PartModule
    {
        [KSPField(guiName = "Power Output", guiActive = true)]
        public string powerOutputDisplay = string.Empty;

        protected double ecBaseOutput;
        protected ModuleResourceConverter converter;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            converter = this.part.FindModuleImplementing<ModuleResourceConverter>();
            if (converter != null)
            {
                ResourceRatio[] outputs = converter.outputList.ToArray();
                ResourceRatio output;
                for (int index = 0; index < outputs.Length; index++)
                {
                    output = outputs[index];
                    if (output.ResourceName == "ElectricCharge")
                    {
                        ecBaseOutput = output.Ratio;
                        break;
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (converter == null)
                return;
            if (!converter.IsActivated)
            {
                powerOutputDisplay = "n/a";
                return;
            }
            if (converter.status == null)
                return;

            //Power output
            if (converter.status.Contains("load"))
            {
                //Get the numerical value (*somebody* didn't seem to make this convenient to obtain :( )
                powerOutputDisplay = converter.status.Substring(0, converter.status.IndexOf("%"));
                double load;
                if (double.TryParse(powerOutputDisplay, out load))
                {
                    load = load / 100f;
                    load = load * ecBaseOutput;
                    powerOutputDisplay = string.Format("{0:f2}/sec", load);
                }

                else
                {
                    powerOutputDisplay = "n/a";
                }
            }
            else
            {
                powerOutputDisplay = "n/a";
            }
        }
    }
}
