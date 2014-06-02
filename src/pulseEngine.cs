using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ANPP
{
    class ANPPPulseEngine : Part
    {
        ANPPPulseFuel fuelSupply;
        pulseUnit currentUnit;
        private float totalVesselMass;
        private FXGroup explosionGroup;				// explosion special effect  
        [KSPField]
        public float detonationDelay = 0.5f;
        [KSPField]
        public float activationRatio = 4.5f;
        [KSPField]
        public float drainRate = 0.2f;


        private float cooldown;
        private float prevTime;

		// Initial call in VAB when picking up part
		// Also called when part comes into range of focussed ship (<2.5km)
		// And at initial part loading at program start
        protected override void onPartAwake()
        {
            
        }

        protected void detonatePulse()
        {
            if (fuelSupply == null)
            {
                return;
            }
            totalVesselMass = 0f;
            foreach (Part current in this.vessel.parts)
            {
                totalVesselMass += current.mass;
            }

            currentUnit = fuelSupply.getUnit();
            if(currentUnit!=null)
            {
                float neededCharge = currentUnit.getEnergy()/activationRatio;
                float rechargeAmount = -currentUnit.getEnergy() ;
                float charge = getECInBank();
                if(charge > neededCharge)
                {
                    Debug.Log(charge + " " + neededCharge+" "+(charge - neededCharge));
                    return;
                }
                consumeCharge(neededCharge);
                RequestResource("ElectricCharge", rechargeAmount);
                // add velocity
                this.vessel.ChangeWorldVelocity(base.transform.up * (this.currentUnit.getImpulse() / totalVesselMass));

                base.rigidbody.AddRelativeForce(new Vector3(0f, (this.currentUnit.getImpulse() / totalVesselMass), 0f), ForceMode.Force);
                this.temperature += currentUnit.getEnergy()/50000;

                // FX: make explosion sound
                this.explosionGroup.Power = 10;
                this.explosionGroup.Burst();
                base.gameObject.audio.pitch = 1f;
                base.gameObject.audio.PlayOneShot(this.explosionGroup.sfx);

                //TODO replace this with a jet of plasma
                Vector3d groundZero = new Vector3d(base.transform.FindChild("model").localPosition.x, base.transform.FindChild("model").localPosition.y, base.transform.FindChild("model").localPosition.z);
                groundZero = base.transform.FindChild("model").TransformPoint(groundZero);
                FXMonger.Explode(this, groundZero, 10);
            }
        }

        private float getECInBank()
        {
            float ret = 0;
            foreach (PartResource resource in Resources)
            {
                if(resource.resourceName.Equals("ElectricCharge"))
                {
                    ret = (float)resource.amount;
                }
            }
            return ret;
        }
        private void consumeCharge(float amount)
        {
            foreach (PartResource resource in Resources)
            {
                if (resource.resourceName.Equals("ElectricCharge"))
                {
                    resource.amount -= amount;
                    if (resource.amount < 0)
                        resource.amount = 0;
                }
            }
        }


		// Called at beginning of flight scene after onPartStart()
		// also when part comes in range of focussed ship (<2.5km) after onPartStart()
        protected override void onFlightStart()
        {
            foreach (Part p in children)
            {
                if(p is ANPPPulseFuel && this.topNode.attachedPart == p)
                {
                    fuelSupply = p as ANPPPulseFuel;
                }
            }
            if (fuelSupply == null && parent is ANPPPulseFuel && this.topNode.attachedPart == parent)
            {
                fuelSupply = parent as ANPPPulseFuel;
            }
            this.explosionGroup = base.findFxGroup("explosionGroup");
        }

        //=======
        // Called during initial part load at start of game
        protected override void onPartLoad()
        {
            this.fxGroups.Add(new FXGroup("explosionGroup"));
            base.onPartLoad();

        }

        // called continously during flight scene if in active stage
        protected override void onActiveFixedUpdate()
        {

        }
        		
	    //=======
		// called continuously during flight scene if on focussed vessel
	    protected override void onPartFixedUpdate()
        {
            consumeCharge(drainRate * Utils.getWarpFactor());
            if(this.state == PartStates.ACTIVE)
            {
                cooldown = Time.time;
                if (vessel.ctrlState.mainThrottle > 0.01 && 
                    cooldown - prevTime > detonationDelay / vessel.ctrlState.mainThrottle)
                {
                    detonatePulse();
                    prevTime = cooldown;
                }
            }
            
        }
    }
}
