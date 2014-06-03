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
        [KSPField]
        public float efficiency = 150f;


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

            currentUnit = fuelSupply.getUnit(false);
            if(currentUnit!=null)
            {
                float neededCharge = currentUnit.getEnergy()/(activationRatio * efficiency);
				float rechargeAmount = -currentUnit.getEnergy() / efficiency;
                float charge = getECInBank();
				Debug.Log(charge+":"+neededCharge);
                if(charge < neededCharge)
                {
                    Debug.Log(charge + " " + neededCharge+" "+(charge - neededCharge));
                    return;
                }
				fuelSupply.getUnit(true);
                consumeCharge(neededCharge);
                RequestResource("ElectricCharge", rechargeAmount);
                // add velocity
                this.vessel.ChangeWorldVelocity(base.transform.up * (this.currentUnit.getImpulse() / totalVesselMass));

                base.rigidbody.AddRelativeForce(new Vector3(0f, (this.currentUnit.getImpulse() / totalVesselMass), 0f), ForceMode.Force);

                if(this.Modules.Contains("HeatTransfer"))
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
			Debug.Log("test");
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
			Debug.Log("test2");
            this.explosionGroup = base.findFxGroup("explosionGroup");
        }

        //=======
        // Called during initial part load at start of game
        protected override void onPartLoad()
        {
            this.fxGroups.Add(new FXGroup("explosionGroup"));
            base.onPartLoad();
            ConfigNode node = new ConfigNode("MODULE");
            node.AddValue("name", "EngineModule");

            this.AddModule(node);
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

    public class EngineModule : PartModule
    {
        bool isActive = false;
        float time = 0;
        [KSPAction("Engine Off")]
        public void EngineOffAction(KSPActionParam param)
        {
			part.deactivate();
            EngineOff();
        }

        [KSPEvent(guiActive = true, guiName = "Engine Off", active = true)]
        public void EngineOff()
		{
			part.deactivate();
            part.Events["EngineOff"].active = false;
            part.Events["EngineOn"].active = true;
        }

        [KSPAction("Engine On")]
        public void EngineOnAction(KSPActionParam param)
        {
			part.activate(0, vessel);
            EngineOn();
        }

        [KSPEvent(guiActive = true, guiName = "Engine On")]
        public void EngineOn()
		{
			part.activate(0, vessel);
            part.Events["EngineOff"].active = true;
            part.Events["EngineOn"].active = false;
        }

        public override void OnUpdate()
        {
       //*/    
			if(Time.time - time > 1 && isActive)
            {
                time = Time.time;
                isActive = false;
            }
            else
            {
				Events["EngineOff"].active = isActive;
				Events["EngineOn"].active = !isActive;
            }
            //*/
        }

        public override void OnFixedUpdate()
        {
			isActive = true;
			Events["EngineOff"].active = true;
			Events["EngineOn"].active = false;

        }
    }
}
