using UnityEngine;
using System.Collections;

public class HaloObject : MonoBehaviour {

	public static HaloObject CreateNewHaloObject (Transform parent, Vector3 position, MolColor color, float scale)
	{
		var haloGameObject = Instantiate (Resources.Load ("HaloPrefab"), position, Quaternion.identity) as GameObject;
		
		if (haloGameObject != null) 
		{
			print ("create new halo"); 

			Transform tf = haloGameObject.transform; 

			haloGameObject.name = "halo";
			tf.parent = parent;
			tf.localScale = new Vector3(Settings.Values.molScale * scale, Settings.Values.molScale * scale, tf.localScale.z); 

			haloGameObject.GetComponent<MeshRenderer> ().material.color = color.rgba; 
			haloGameObject.collider.enabled = false; 
			
			var haloObject = haloGameObject.GetComponent<HaloObject> (); 

			return haloObject;
		}
		return null;
	}

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}
}
