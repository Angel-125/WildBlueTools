using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using ModuleWheels;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIFlexibleDockingPort : PartModule, IJointLockState
    {
        ConfigurableJoint joint;
        protected ConfigurableJoint savedJoint;
        protected Rigidbody jointRigidBody;

        [KSPEvent(guiActive = true)]
        public void SetupJoint()
        {
            savedJoint = part.attachJoint.Joint;

            // Catch reversed joint
            // Maybe there is a best way to do it?
            if (transform.position != part.attachJoint.Joint.connectedBody.transform.position)
            {
                joint = part.attachJoint.Joint.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = part.attachJoint.Joint.GetComponent<Rigidbody>();
            }
            else
            {
                joint = part.attachJoint.Joint.GetComponent<Rigidbody>().gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = part.attachJoint.Joint.connectedBody;

            }

            joint.breakForce = 1e15f;
            joint.breakTorque = 1e15f;
            // And to default joint
            part.attachJoint.Joint.breakForce = 1e15f;
            part.attachJoint.Joint.breakTorque = 1e15f;
            part.attachJoint.SetBreakingForces(1e15f, 1e15f);

            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            joint.projectionDistance = 0f;
            joint.projectionAngle = 0f;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;

            // Copy drives
            joint.linearLimit = part.attachJoint.Joint.linearLimit;
            joint.lowAngularXLimit = part.attachJoint.Joint.lowAngularXLimit;
            joint.highAngularXLimit = part.attachJoint.Joint.highAngularXLimit;
            joint.angularXDrive = part.attachJoint.Joint.angularXDrive;
            joint.angularYZDrive = part.attachJoint.Joint.angularYZDrive;
            joint.xDrive = part.attachJoint.Joint.xDrive;
            joint.yDrive = part.attachJoint.Joint.yDrive;
            joint.zDrive = part.attachJoint.Joint.zDrive;

            jointRigidBody = joint.GetComponent<Rigidbody>();

            // Set anchor position
            joint.anchor =
                jointRigidBody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
            joint.connectedAnchor = Vector3.zero;

            // Set correct axis
            joint.axis =
                jointRigidBody.transform.InverseTransformDirection(joint.connectedBody.transform.right);  //x axis
            joint.secondaryAxis =
                jointRigidBody.transform.InverseTransformDirection(joint.connectedBody.transform.up); //y axis

            joint.enableCollision = false;

            /*
            if (translateJoint)
            {
                //we need to get joint's translation along the translate axis
                var right = joint.axis; //x axis
                var up = joint.secondaryAxis; //y axis
                var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized; //z axis
                var r = Quaternion.LookRotation(forward, up);
                Vector3 f = r * (-translateAxis);

                startPosition = Vector3.Dot(jointRigidBody.transform.InverseTransformPoint(joint.connectedBody.transform.position) - joint.anchor, f);

                Logger.Log(servoName + ": right = " + right + ", forward = " + forward + ", up = " + up + ", trAxis=" + translateAxis + ", f=" + f + ", startposition=" + startPosition, Logger.Level.Debug);
                
                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;
            }

            if (rotateJoint)
            {
                startPosition = to180(AngleSigned(jointRigidBody.transform.up, joint.connectedBody.transform.up, joint.connectedBody.transform.right));

                joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;

                if (jointSpring > 0)
                {
                    if (rotateAxis == Vector3.right || rotateAxis == Vector3.left)
                    {
                        JointDrive drv = joint.angularXDrive;
                        drv.positionSpring = jointSpring;
                        drv.positionDamper = jointDamping;
                        joint.angularXDrive = drv;
                        joint.angularYMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                    }
                    else
                    {
                        JointDrive drv = joint.angularYZDrive;
                        drv.positionSpring = jointSpring;
                        drv.positionDamper = jointDamping;
                        joint.angularYZDrive = drv;

                        joint.angularXMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                    }
                }
            }
             */

            // Reset default joint drives
            var resetDrv = new JointDrive
            {
                positionSpring = 0,
                positionDamper = 0,
                maximumForce = 0
            };

            part.attachJoint.Joint.angularXDrive = resetDrv;
            part.attachJoint.Joint.angularYZDrive = resetDrv;
            part.attachJoint.Joint.xDrive = resetDrv;
            part.attachJoint.Joint.yDrive = resetDrv;
            part.attachJoint.Joint.zDrive = resetDrv;
            part.attachJoint.Joint.enableCollision = false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (joint == null)
                return;

            part.attachJoint.Joint.xMotion = ConfigurableJointMotion.Free;
            part.attachJoint.Joint.yMotion = ConfigurableJointMotion.Free;
            part.attachJoint.Joint.zMotion = ConfigurableJointMotion.Free;

            part.attachJoint.Joint.angularXMotion = ConfigurableJointMotion.Free;
            part.attachJoint.Joint.angularYMotion = ConfigurableJointMotion.Free;
            part.attachJoint.Joint.angularZMotion = ConfigurableJointMotion.Free;

        }

        public bool IsJointUnlocked()
        {
            return true;
        }
    }
}
