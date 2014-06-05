using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MagneticFields
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class MagneticFields : MonoBehaviour
	{
		public static Dictionary<String, BodyField> fields = new Dictionary<String, BodyField> ();

		static bool check = true;
		public void Start()
		{
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER && check)
			{
				check = false;
				Debug.Log("Loading Magentic Field Data");
				Dictionary<ConfigNode, CelestialBody> planets = new Dictionary<ConfigNode, CelestialBody> ();
				foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("MAGFIELDSETTINGS")) {
					foreach (CelestialBody body in FlightGlobals.Bodies) {
						ConfigNode n = node.GetNode (body.GetName ());
						if (n != null)
						{
							Debug.Log("found config for "+body.GetName());
							planets.Add (n, body);
						}
					}
				}

				foreach (ConfigNode node in planets.Keys) {
					if (!fields.ContainsKey (planets[node].GetName())) {
						Debug.Log("Adding Field for "+planets[node].GetName () +" from config");
						fields.Add (planets [node].GetName (), new BodyField (node, planets [node]));
					}
				}
				foreach (CelestialBody body in FlightGlobals.Bodies) {
					if (!fields.ContainsKey (body.GetName ())) {
						Debug.Log("Generating Default Field for "+body.GetName ());
						fields.Add (body.GetName (), new BodyField (body));
					}
				}
			}
		}

		public void Update()
		{
			if( Input.GetKeyDown(KeyCode.Z) )
			{
				foreach(String s in fields.Keys)
				{

				}
			}
		}

		public static Vector3 getField(Vector3 absLoc)
		{
			Vector3 ret = new Vector3(0,0,0);
			if(absLoc.x == absLoc.y && absLoc.y == absLoc.z)
			{
				Debug.LogError("Wrong Location");
			}
			foreach(BodyField b in fields.Values)
			{
				ret += b.getField(absLoc);
			}
			return ret;
		}

		public static Vector3 getField(Vessel vessel)
		{
			return getField(vessel.flightIntegrator.integratorTransform.position);
		}

	}

	public class MagneticModule : PartModule
	{
		[KSPField(guiActive = true, guiName = "B")] 
		public float B;
		[KSPField(guiActive = true, guiName = "Bx")] 
		public float x;
		[KSPField(guiActive = true, guiName = "By")] 
		public float y;
		[KSPField(guiActive = true, guiName = "Bz")] 
		public float z;
		[KSPField]
		public float sampleTime = 0.5f;
		float time = 0;

		public override void OnUpdate()
		{
			if(Time.time < time + sampleTime)
				return;

			time = Time.time;

			Vector3 here = vessel.flightIntegrator.integratorTransform.position;
			Vector3 field = MagneticFields.getField(here);
			Vector3 planet = vessel.orbit.referenceBody.GetTransform().position;
			Vector3 diff = here - planet;
			B = field.magnitude;
			x = field.x;
			y = field.y;
			z = field.z;
		}
	}

	public class BodyField
	{
		public CelestialBody body;
		public Vector3 momentDir = new Vector3 (0, 1, 0);
		public Vector3 moment = new Vector3(0, (float)1e20, 0);
		public Vector3 centre = new Vector3 (0, 0, 0);
		public double noise = 0.002;
		public double momentMag = 1e20;
		private static double konstant = 1e-12;
		private Vector3[] moments = new Vector3[10];

		private Vector3 bodyAbsLoc;

		System.Random r = new System.Random ();

		public BodyField (CelestialBody body)
		{
			this.body = body;

			double period = body.rotationPeriod;
			double mass = body.Mass;
			momentMag = mass*10e14/(period*period*period);

		//	Debug.Log(body.GetName()+" "+period+" "+mass);

			populateArrays ();
		}

		public BodyField (ConfigNode node, CelestialBody body)
		{
			this.body = body;
			string val = node.GetValue ("momentDir");
			string[] vals = val.Split (',');
			momentDir = new Vector3 (float.Parse (vals [0]), float.Parse (vals [1]), float.Parse (vals [2]));
			val = node.GetValue ("centre");
			vals = val.Split (',');
			centre = new Vector3 (float.Parse (vals [0]), float.Parse (vals [1]), float.Parse (vals [2]));
			val = node.GetValue ("noise");
			if(val!=null)
				noise = double.Parse (val);
			val = node.GetValue ("momentMag");
			if(val!=null)
				momentMag = double.Parse (val);
				
			populateArrays ();
		}

		public BodyField (CelestialBody body, Vector3 moment, Vector3 centre, double noise, double momentMag)
		{
			this.body = body;
			this.momentDir = moment.normalized;
			this.centre = centre;
			this.noise = noise;
			this.momentMag = momentMag;

			populateArrays ();
		}

		public Vector3 getField (Vector3 absLoc)
		{
			Vector3 ret = new Vector3(0,0,0);
			Vector3 rel = absLoc - body.GetTransform().position + centre;
			double r = rel.magnitude;
			foreach(Vector3 m in moments)
			{
				if(m.magnitude==0||r==0) continue;
				double mdotr = dot (rel, m);
				if(double.IsNaN(r)||double.IsNaN(mdotr)||r==0)
					Debug.LogError("NaN error "+absLoc.x+" "+absLoc.y+" "+absLoc.z+" "+bodyAbsLoc.x+" "+bodyAbsLoc.y+" "+bodyAbsLoc.z+" "+r);
				ret.x += (float)((3 * konstant * rel.x * mdotr/(r*r*r*r*r)) - (konstant * m.x/(r*r*r)));
				ret.y += (float)((3 * konstant * rel.y * mdotr/(r*r*r*r*r)) - (konstant * m.y/(r*r*r)));
				ret.z += (float)((3 * konstant * rel.z * mdotr/(r*r*r*r*r)) - (konstant * m.z/(r*r*r)));

			//	Debug.Log(body.GetName()+" "+r+" ret:"+ret+" vdot:"+mdotr+" rel:"+rel+" m:"+m);
			}

			return ret;
		}
	
		public Vector3 getField(Vessel vessel)
		{
			return getField(vessel.flightIntegrator.integratorTransform.position);
		}

		private double dot(Vector3 A, Vector3 B)
		{
			return (double)A.x * (double)B.x + (double)A.y * (double)B.y + (double)A.z * (double)B.z;
		}

		private void populateArrays ()
		{
			bodyAbsLoc = body.GetTransform().position;
			moment = new Vector3(momentDir.x, momentDir.y, momentDir.z).normalized;
			moment.Scale (new Vector3((float)momentMag, (float)momentMag, (float)momentMag));
			moments [0] = moment;
			for (int n = 1; n<10; n++) {
				Vector3 mom = new Vector3 ((float)r.NextDouble (), (float)r.NextDouble (), (float)r.NextDouble ());
				//Vector3 cent = new Vector3((float)r.NextDouble(),(float)r.NextDouble(),(float)r.NextDouble());
				mom.Normalize ();
				//cent.Normalize();
				mom.Scale (new Vector3 ((float)(momentDir.magnitude * momentMag * Math.Pow (noise, n)), 
				                        (float)(momentDir.magnitude * momentMag * Math.Pow (noise, n)), 
				                        (float)(momentDir.magnitude * momentMag * Math.Pow (noise, n))));
				moments [n] = mom;
			}
		}
	}

}

