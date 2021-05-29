namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Template/Template Modifier")]
	public class Modifier_Template : GameObjectModifier
	{
		public override void Run(VectorEntity ve, UnityTile tile)
		{
			var min = ve.MeshFilter.sharedMesh.subMeshCount;
			var mats = new Material[min];

			Vector2d latLon = GameObject.Find("Map").GetComponent<AbstractMap>().WorldToGeoPosition(ve.Transform.position);
			float sample_data = 10;
			
			for (int i = 0; i < min; i++)
			{				
				Material mat = new Material(Shader.Find("Specular"));
				mat.color = dataToColor(sample_data);
				mats[i] = mat;
			}
			
			ve.MeshRenderer.materials = mats;
		}
		/// <summary>
		/// 	customized function, interpret data to color
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private Color dataToColor(float data) {
			if (data < 0.5f) {
				return new Color(0.0f, data * 2f, 1f - data * 2f, 1f);
			} else {
				return new Color((data - 0.5f)*2f, 1f - (data - 0.5f)*2f, 0.0f, 1f);
			}
		}
	}
}
