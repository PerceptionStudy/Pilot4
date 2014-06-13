using UnityEngine;
using System;
using System.Diagnostics;

public class MolObject : MonoBehaviour
{
	public bool animate = true;
	
	private bool focus = false;	
	private int focusTechnique = -1; 
	//private bool firstWave = false;

	// Attractive Flicker settings
	public int amp1 = 100; 
	public int T1 = 50; 
	public int amp2 = 25; 
	public int T2 = 600; 
	public int d1 = 1000; 
	public int dt = 1500; 

	// AF counter
	private int t_prev = -1; 
	private float T_prev = 0.0f; 

	private MolColor defaultColor;
	private MolColor currentColor; 

	private HaloObject halo; 
	private TrailObject trail; 

	private Stopwatch stopWatch = new Stopwatch ();	

	public static MolObject CreateNewMolObject (Transform parent, string name, MolColor color)
	{
		var position = new Vector3 ((UnityEngine.Random.value - 0.5f) * MainScript.BoxSize.x, (UnityEngine.Random.value - 0.5f) * MainScript.BoxSize.y, (UnityEngine.Random.value - 0.5f) * MainScript.BoxSize.z);
		var molGameObject = Instantiate (Resources.Load ("MolPrefab"), position, Quaternion.identity) as GameObject;

		if (molGameObject != null) 
		{
			molGameObject.name = name;
			molGameObject.transform.parent = parent;

			var molObject = molGameObject.GetComponent<MolObject> ();

			molObject.defaultColor = color; 
			molObject.currentColor = color; 
			molObject.halo = null; 
			molObject.trail = null; 

			molGameObject.GetComponent<MeshRenderer> ().material.color = color.rgba;

			return molObject;
		}
		return null;
	}

	void Start ()
	{

	}

	void FixedUpdate ()
	{
		if (!MainScript.Animate)
		{
			rigidbody.drag = 10000;
			return;
		}

		Vector3 force = UnityEngine.Random.insideUnitSphere * Settings.Values.randomForce;
		force.z = 0;

		rigidbody.AddForce (force);		
		rigidbody.drag = Settings.Values.drag;
	}

	public void StartContext(int focusTechnique)
	{
		if(focusTechnique == 1){
			currentColor = new MolColor(Math.Max (currentColor.L - 50.0f, 0.0f), defaultColor.a, defaultColor.b);
			gameObject.GetComponent<MeshRenderer> ().material.color = currentColor.rgba; 
		}
	}

	public void StopContext()
	{
		currentColor = defaultColor; 
		gameObject.GetComponent<MeshRenderer> ().material.color = currentColor.rgba; 
	}
	
	public void StartFocus(int focusTechnique)
	{
		print ("start focus"); 

		focus = true;
		this.focusTechnique = focusTechnique; 

		MolColor haloColor = new MolColor (50.0f, 0.0f, 0.0f);  
		float haloScale = 2.0f; 

		if(focusTechnique == 0){
			this.t_prev = -1; 
			this.T_prev = 0.0f; 
		}
		else{
			this.halo = HaloObject.CreateNewHaloObject (this.transform.parent, this.transform.position, haloColor, haloScale); 
		}
		if(focusTechnique == 2){
			this.trail = TrailObject.CreateNewTrailObject (this.transform.parent, this.transform.position, haloColor, haloScale); 
		}
		
		stopWatch.Reset();
		stopWatch.Start();
	}
	
	public void StopFocus()
	{
		focus = false; 
		
		currentColor = defaultColor; 
		gameObject.GetComponent<MeshRenderer> ().material.color = currentColor.rgba; 

		if(this.halo != null){
			Destroy (this.halo.gameObject); 
		}
		this.halo = null; 

		if(this.trail != null){
			Destroy(this.trail.gameObject); 
		}
		this.trail = null; 
		
		stopWatch.Stop();
		stopWatch.Reset();
	}

	private void FocusUpdate()
	{
		int t = (int)stopWatch.ElapsedMilliseconds;

		// set halo to follow 
		if(this.halo != null){
			this.halo.transform.position = this.transform.position; 
		}

		// set trail to follow
		if(this.trail != null){
			this.trail.UpdateTrailTarget(this.transform.position); 
		}

		if(this.focusTechnique == 0){
			float amplitude; 
			float period; 

			// find current values for amplitude
			if(t < d1){
				amplitude = (float)this.amp1; 
				period = (float)this.T1; 
			}
			else if (t < (d1 + dt)){ 
				float lerp = (float)(t - this.d1) / (float)this.dt;
				amplitude = this.amp1 - lerp * (float)(this.amp1 - this.amp2); 
				period = this.T1 + lerp * (this.T2 - this.T1); 
			}
			else{
				amplitude = (float)this.amp2; 
				period = (float)this.T2; 
			}

			// find current center of wave
			float Lc; 
			if(defaultColor.L >= 50){
				Lc = Math.Min ((float)defaultColor.L + amplitude / 2.0f, 100.0f) - amplitude / 2.0f; 
			}
			else{
				Lc = Math.Max ((float)defaultColor.L - amplitude / 2.0f, 0.0f) + amplitude / 2.0f; 
			}

			// find current period 
			float T = this.T_prev + ((float)(t - t_prev) / period); 

			// calculate current luminance value
			float L = Lc + amplitude / 2.0f * (float)Math.Sin (2.0f * (float)Math.PI * T); 

			print ("L: " + L + ", t: " + t + ", amplitude: " + amplitude + ", period: " + period + " T: " + T + " -- base L: " + defaultColor.L); 

			L = Mathf.Clamp (L, 0.0f, 100.0f);

			// set object color
			currentColor = new MolColor(L, defaultColor.a, defaultColor.b);
			gameObject.GetComponent<MeshRenderer> ().material.color = currentColor.rgba;

			this.t_prev = t; 
			this.T_prev = T;
		}
	}


	int frameCount = 0;
	Vector3 lastPos = new Vector3();
	float speedAcc = 0; 

	void Update ()
	{
		if(focus) FocusUpdate();

		transform.localScale = new Vector3(Settings.Values.molScale, Settings.Values.molScale, Settings.Values.molScale);

		Vector3 temp = rigidbody.position;
		
		if(rigidbody.position.x > transform.parent.position.x + MainScript.BoxSize.x * 0.5f)
		{
			temp.x = transform.parent.position.x + MainScript.BoxSize.x * 0.5f - gameObject.GetComponent<SphereCollider>().radius * 2; 
		}
		else if(rigidbody.position.x < transform.parent.position.x - MainScript.BoxSize.x * 0.5f)
		{
			temp.x = transform.parent.position.x - MainScript.BoxSize.x * 0.5f + gameObject.GetComponent<SphereCollider>().radius * 2; 
		}
		
		if(rigidbody.position.y > transform.parent.position.y + MainScript.BoxSize.y * 0.5f)
		{
			temp.y = transform.parent.position.y + MainScript.BoxSize.y * 0.5f - gameObject.GetComponent<SphereCollider>().radius * 2; 
		}		
		else if(rigidbody.position.y < transform.parent.position.y - MainScript.BoxSize.y * 0.5f)
		{
			temp.y = transform.parent.position.y - MainScript.BoxSize.y * 0.5f + gameObject.GetComponent<SphereCollider>().radius * 2; 
		}

		temp.z = 0;
		
//		if(rigidbody.position.z > transform.parent.position.z + MainScript.BoxSize.z * 0.5f)
//		{
//			temp.z = transform.parent.position.z + MainScript.BoxSize.z * 0.5f - gameObject.GetComponent<SphereCollider>().radius * 2; 
//		}		
//		else if(rigidbody.position.z < transform.parent.position.z - MainScript.BoxSize.z * 0.5f)
//		{
//			temp.z = transform.parent.position.z - MainScript.BoxSize.z * 0.5f + gameObject.GetComponent<SphereCollider>().radius * 2; 
//		}
		
		rigidbody.position = temp; 

		if (!animate)
		{
			speedAcc += Vector3.Distance(transform.position, lastPos);
			frameCount ++;

			if(frameCount > 20)
			{
				print (speedAcc / (float)frameCount);
				speedAcc = 0;
				frameCount = 0;
			}

			lastPos = transform.position;
		}
	}
	

}