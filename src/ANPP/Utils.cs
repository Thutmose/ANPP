using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ANPP
{
    public class Utils
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
}
