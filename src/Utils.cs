using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ANPP
{
    class Utils
    {
        public static float factor = 1;

        public static PartResource getResource(Part part, String name)
        {
            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName.Equals(name))
                {
                    return resource;
                }
            }
            return null;
        }
        public static void AddResource(Part part, String name, float amount, float max)
        {
            if(getResource(part, name)!=null)
                return;

            ConfigNode node = new ConfigNode("RESOURCE");
            node.AddValue("name", name);
            node.AddValue("maxAmount", max);
            node.AddValue("amount", amount);
            part.AddResource(node);
        }

        public static float getWarpFactor()
        {
            return TimeWarp.CurrentRate * factor;
        }


    }

    public class HeatTransfer : PartModule
    {
        List<Part> heatParts = new List<Part>();
        [KSPField]
        float conductivityFactor = 0.02f;
        [KSPField]
        float SBConstant = (float)1e-15;
        [KSPField]
        float dissipationFactor = 1f;
        [KSPField(guiActive = true, guiName = "Temperature")]
        float temperature;
        bool tick = false;
        float averageT = 20;
        float time = 0;
        // Fired first - this is at KSP load-time (When the loading bar hits a part with this mod)
        public override void OnAwake()
        {

        }

        public override void OnLoad(ConfigNode node)
        {


        }

        // Fired when scene containing part Saves (Ends)
        public override void OnSave(ConfigNode node)
        {

        }


        // Fired once, when a scene starts containing the part
        public void OnStart()
        {
            updateAverageT();
            time = Time.time;
        }

        private void updateAverageT()
        {
            int count = 0;
            float temp = 0;
            if(vessel!=null && vessel.parts!=null)
            foreach (Part p in vessel.parts)
            {
                if (p == null) continue;
                count++;
                temp += (p.temperature);
            }
            if (count != 0)
                averageT = temp / count;
        }

        private void populateHeatParts()
        {
            heatParts.Clear();
            foreach (Part p in vessel.parts)
            {
                if (p == null) continue;
                if(Math.Abs(p.temperature - averageT) > 100)
                {
                    heatParts.Add(p);
                }
            }
        }

        private void spreadHeat()
        {
            populateHeatParts();
            foreach (Part p in heatParts)
            {
                List<Part> parts = new List<Part>();
                parts.AddRange(p.children);
                float diff = Math.Abs(p.temperature - averageT) * conductivityFactor * Utils.getWarpFactor();
                parts.Add(p.parent);
                foreach(Part connected in parts)
                {
                    if (connected == null) continue;

                    if(connected.temperature > p.temperature)
                    {
                        connected.temperature -= (diff / parts.Count);
                        p.temperature += (diff / parts.Count);
                    }
                    else
                    {
                        connected.temperature += (diff / parts.Count);
                        p.temperature -= (diff / parts.Count);
                    }
                }
            }
            updateAverageT();
        }

        public override void OnFixedUpdate()
        {
            if(!tick)
            {
                tick = !tick;
                return;
            }
            float dt = (Time.time - time)/0.02f;
            Utils.factor = dt;
            if (vessel != null && vessel.parts != null)
                spreadHeat();
            double externalT = FlightGlobals.getExternalTemperature(vessel.GetWorldPos3D()) + 273;
            double partT = part.temperature + 273;
            temperature = part.temperature;
            double externalT4 = externalT * externalT * externalT * externalT;
            double partT4 = partT * partT * partT * partT;

            double diffT4 = (partT4 - externalT4) * SBConstant * TimeWarp.CurrentRate;

            double diff = partT - externalT;

            if (Math.Abs(diff) < 30) return;
            if (Math.Abs(diff) > Math.Abs(diffT4)) diff = diffT4;

            diff *= dissipationFactor * TimeWarp.CurrentRate;
            part.temperature -= (float)diff;

            time = Time.time;
        }
    }
}
