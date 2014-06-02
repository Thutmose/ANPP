using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ANPP
{
    class ANPPPulseFuel : Part
    {
        [KSPField(guiActive = true, guiName = "Type")]
        public string bombTitle = "60GJPulseUnit";	// title that appears in pop-up menu for activating magazines
        [KSPField]
        public float bombEnergy = 60;

        // Initial call in VAB when picking up part
        // Also called when part comes into range of focussed ship (<2.5km)
        // And at initial part loading at program start
        protected override void onPartAwake()
        {

        }
        //=======
        // Called during initial part load at start of game
        protected override void onPartLoad()
        {
            base.onPartLoad();
        }

        public pulseUnit getUnit()
        {
            float num = RequestResource(bombTitle, 1);
            if (num < 1)
                return null;
            return pulseUnit.getPulseUnit(bombTitle, bombEnergy * 1000000, Utils.getResource(this, bombTitle).info.density);
        }
    }

    class pulseUnit
    {
        float energy;
        float mass;
        float impulse;
        String name;

        private static Dictionary<String, pulseUnit> pulseUnits = new Dictionary<String, pulseUnit>();

        public static pulseUnit getPulseUnit(String name, float energy, float mass)
        {
            if(pulseUnits.ContainsKey(name))
            {
                return pulseUnits[name];
            }
            pulseUnit ret = new pulseUnit(name, energy, mass);
            pulseUnits.Add(name, ret);
            Debug.Log(">>> Added " + name + " With mass of " + mass +" and energy of " +energy);
            ret.getImpulse();
            Debug.Log(">>> "+name+" has impulse of "+ret.getImpulse());
            return ret;
        }

        private pulseUnit(String name, float energy, float mass)
        {
            this.name = name;
            this.energy = energy;
            this.mass = mass;
        }

        public float getEnergy()
        {
            return energy;
        }

        public float getMass()
        {
            return mass;
        }

        public float getImpulse()
        {
            if (impulse == 0)
            {
                Debug.Log(">>> Calculated " + energy + " " + mass + " " + mass * Math.Sqrt(energy / mass));
                impulse = (float)Math.Sqrt(energy / mass) * mass;
                return impulse;
            }
            return impulse;
        }
    }
}
