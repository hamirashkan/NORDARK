using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefreshColors : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Refresh()
    {
        Color color = Color.white;
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Buildings");
         foreach (GameObject go in objects) {
             MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
             foreach (MeshRenderer r in renderers) {
                 foreach (Material m in r.materials) {
                     if (m.HasProperty("_Color"))
                         m.color = color;
                 }
             }
         }
    }
}
