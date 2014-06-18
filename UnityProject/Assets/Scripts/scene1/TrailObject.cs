using UnityEngine;
using System.Collections;

public class TrailObject : MonoBehaviour {

	private Vector3[] vertices; 
	private Vector2[] uv; 
	private int[] triangles; 
	private MolColor color; 
	private float scale; 
	private float targetSize; 
	

	public static TrailObject CreateNewTrailObject (Transform parent, Vector3 position, MolColor color, float targetSize, float scale)
	{
		scale /= 2.0f; 

		// set vertices  
		Vector3[] vertices = new Vector3[3]; 
		vertices [0] = new Vector3 (0.0f, (float)Screen.height * 0.5f, 0.0f); 
		Vector3 dir = position - vertices [0]; 
		dir.Normalize (); 
		vertices [1] = new Vector3 (position.x + targetSize * scale * dir.y, position.y - targetSize * scale * dir.x, 0.0f); 
		vertices [2] = new Vector3 (position.x - targetSize * scale * dir.y, position.y + targetSize * scale * dir.x, 0.0f); 

		print ("Create new trail [" + vertices[0] + ", " + vertices[1] + ", " + vertices[2] + "]"); 

		// set uv coordinates
		Vector2[] uv = new Vector2[]{new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)};

		// set triangles
		int[] triangles = new int[] {0, 1, 2}; 


		var trailGameObject = Instantiate (Resources.Load ("TrailsPrefab"), Vector3.zero, Quaternion.identity) as GameObject;
		
		if (trailGameObject != null) 
		{
			print ("create new trail"); 
			
			trailGameObject.name = "trail";
			trailGameObject.transform.parent = parent;
			
			trailGameObject.GetComponent<MeshRenderer> ().material.color = color.rgba; 

			// set mesh
			Mesh mesh = trailGameObject.GetComponent<MeshFilter> ().mesh; 
			mesh.Clear (); 
			mesh.vertices = vertices; 
			mesh.uv = uv; 
			mesh.triangles = triangles; 
			mesh.RecalculateBounds (); 
			
			var trailObject = trailGameObject.GetComponent<TrailObject> ();
			trailObject.vertices = vertices; 
			trailObject.triangles = triangles; 
			trailObject.uv = uv; 
			trailObject.scale = scale; 
			trailObject.color = color; 
			trailObject.targetSize = targetSize; 
			
			return trailObject;
		}
		return null;
	}

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

	}

	public void UpdateTrailTarget(Vector3 target){
		// update trail target
		Vector3 dir = target - this.vertices[0]; 
		dir.Normalize (); 
		vertices [1] = new Vector3 (target.x + targetSize * scale * dir.y, target.y - targetSize * scale * dir.x, 0.0f); 
		vertices [2] = new Vector3 (target.x - targetSize * scale * dir.y, target.y + targetSize * scale * dir.x, 0.0f); 
		
		// update mesh
		Mesh mesh = GetComponent<MeshFilter> ().mesh; 
		mesh.Clear (); 
		mesh.vertices = vertices;
		mesh.uv = uv; 
		mesh.triangles = triangles; 
		mesh.RecalculateBounds (); 

		// reset color (maybe unnecessary...) 
		GetComponent<MeshRenderer> ().material.color = this.color.rgba; 
	}

}
