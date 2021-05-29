using System;
using System.Collections;
using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Landmark Modifier")]
	public class LandmarkModifier : GameObjectModifier
	{
		public override void Run(VectorEntity ve, UnityTile tile)
		{

            ve.GameObject.tag = "Buildings";
            // List<string> deletebuildings = new List<string>();
            // deletebuildings.Add("Buildings - 395134807");
            // deletebuildings.Add("Buildings - 395134809");
            // deletebuildings.Add("Buildings - 395134806");
            // deletebuildings.Add("Buildings - 395134805");

            // if (deletebuildings.Contains(ve.GameObject.name))
            // {
                // ve.GameObject.Destroy();
            //    ve.GameObject.GetComponent<MeshCollider>().enabled = false;
            // }
            // Debug.Log(ve.GameObject.name);
			// var min = ve.MeshFilter.sharedMesh.subMeshCount;
			// var mats = new Material[min];

			// BusServiceAvailability busServiceAvailability = GameObject.Find("BusIndicator").GetComponent<BusServiceAvailability>();
		// 	Vector2d latLon = GameObject.Find("Map").GetComponent<AbstractMap>().WorldToGeoPosition(ve.Transform.position);
		// 	float[] indicators = busServiceAvailability.GetIndicator((float) latLon[0], (float) latLon[1]);
			
			// for (int i = 0; i < min; i++)
			// {				
			// 	Material mat = new Material(Shader.Find("Specular"));
			// 	// Material mat = new Material(Shader.Find("Texture Plane_Sky Exposure"));
			// 	// mat.color = probabilityToColor(indicators[busServiceAvailability.GetCurrentStep()]);
			// 	mats[i] = mat;
			// }
			
			// ve.MeshRenderer.materials = mats;
		// }
		// private Color probabilityToColor(float probability) {
		// 	if (probability < 0.5f) {
		// 		return new Color(0.0f, probability*2f, 1f - probability*2f, 1f);
		// 	} else {
		// 		return new Color((probability-0.5f)*2f, 1f - (probability-0.5f)*2f, 0.0f, 1f);
		// 	}
		}
	}
}
