using UnityEngine;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System; 
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text; 
using System.Text.RegularExpressions;

public class Stimulus
{
	public int stimulusId;
	public int visionArea;
	public int repeatId;
	public int techID; 

	// stimulus timing settings 
	public int duration = 10000; 								// hardcoded timeout after 10 seconds 
	public int delay = UnityEngine.Random.Range (0, 100);	// random delay before highlight start


	public Stimulus(int stimulusId, int visionArea, int repeatId, int techID)
	{
		this.stimulusId = stimulusId;
		this.visionArea = visionArea;
		this.repeatId = repeatId;
		this.techID = techID; 
	}

	public override string ToString()
	{
		return "repetition: " + repeatId + "; visionArea: " + visionArea + "; technique: " + techID; 
	}
}

public class MainScript : MonoBehaviour 
{
	private bool initiated = false;	
	private int currentStimulusIndex = 0;
	private Stimulus currentStimulus = null;
	private MolObject stimulusObject = null; 	
	private bool setupGUI = true; 
	private bool countdown = false; 
	private bool intermediate = false;
	private bool stimulus = false; 
	private bool highlight = false; 
	private bool stimulusEnd = false; 
	private MolObject[] molObjects;
	private GameObject collideBox;
	private List<Stimulus> stimuli = new List<Stimulus>();

	// isoluminent RGB tripels from http://www.cs.ubc.ca/~tmm/courses/infovis/morereadings/FaceLumin.pdf (Figure 7)
	//public static Color[] MolColors = {new Color (0.847f,0.057f,0.057f), new Color(0.000f,0.592f,0.000f), new Color(0.316f,0.316f,0.991f), new Color(0.527f,0.527f,0.00f), new Color(0.000f,0.559f,0.559f), new Color(0.718f,0.000f,0.718f)}; 
	// 12 qualitative RGB values from http://colorbrewer2.org/
//	public static Color[] MolColors = {
//		new Color(141.0f,211.0f,199.0f), 
//		new Color(255.0f,255.0f,179.0f), 
//		new Color(190.0f,186.0f,218.0f), 
//		new Color(251.0f,128.0f,114.0f), 
//		new Color(128.0f,177.0f,211.0f), 
//		new Color(253.0f,180.0f,98.0f), 
//		new Color(179.0f,222.0f,105.0f), 
//		new Color(252.0f,205.0f,229.0f), 
//		new Color(217.0f,217.0f,217.0f), 
//		new Color(188.0f,128.0f,189.0f), 
//		new Color(204.0f,235.0f,197.0f), 
//		new Color(255.0f,237.0f,111.0f)
//	}; 
	public static Color[] MolColors = {
		new Color(166,206,227), 
		new Color(31,120,180), 
		new Color(178,223,138), 
		new Color(51,160,44), 
		new Color(251,154,153), 
		new Color(227,26,28), 
		new Color(253,191,111), 
		new Color(255,127,0), 
		new Color(202,178,214), 
		new Color(106,61,154), 
		new Color(255,255,153), 
		new Color(177,89,40)
	}; 

	public static Vector3 BoxSize = new Vector3();
	public static bool Animate = false;

	private string userID = ""; 
	private string conditionID = ""; 

	private Stopwatch stopWatch = new Stopwatch ();

	LogLib.Logger<int> distLogger; 
	LogLib.Logger<int> targetLogger; 

	public void CreateMolObjects()
	{
		// Destroy previous game objects
		if(molObjects != null)
		{
			foreach(MolObject obj in molObjects)
			{
				Destroy(obj.gameObject);
			}
		}

		molObjects = new MolObject[(int)Settings.Values.molCount];		

		for(int i = 0; i< (int)Settings.Values.molCount; i++)
			molObjects[i] = MolObject.CreateNewMolObject(gameObject.transform, "molObject_" + i, new MolColor(MolColors[UnityEngine.Random.Range(0, MolColors.Count ())]));	

		initiated = true;
		stimulusObject = null; 
	}

	void LoadStimuli ()
	{
		stimuli.Clear();

		int count = 0;

		for(int ecc = 0; ecc < 2; ecc++)
		{
			for(int rep = 0; rep <= Settings.Values.repeat; rep++)
			{ 
				for(int tech = 0; tech < 4; tech++)
				{
					Stimulus stimulus = new Stimulus(count, ecc, rep, tech);
					stimuli.Add(stimulus);

					count ++;
				}
			}
		}
		var shuffle = (from stimulus in stimuli orderby Guid.NewGuid() select stimulus);
		stimuli = shuffle.ToList();
		currentStimulusIndex = 0;
	}

	void LoadScene ()
	{
		LoadStimuli();		
		CreateMolObjects();
		
		setupGUI = true; 
		countdown = false; 
		intermediate = false; 
		stimulus = false; 
		highlight = false; 
		stimulusEnd = false; 
	}
	
	void Start () 
	{
		if (!initiated) 
		{
			BoxSize.x = Screen.width;
			BoxSize.y = Screen.height;
			BoxSize.z = Settings.Values.molScale * 2;
			
			collideBox = GameObject.Find("Box Object");
			collideBox.transform.localScale = new Vector3(BoxSize.x, BoxSize.y, BoxSize.z);
			
			Settings.LoadSettings();
			
			LoadScene();
		}
	}

	private bool showHud = false;
	private const int guiTopOffset = 20;
	private const int guiDownOffset = 10;
	private const int guiIncrement = 15;
	private GUIStyle style = new GUIStyle();
	private Rect windowRect = new Rect(20, 20, 225, 0);
	private Dictionary<string, string> tempSettings = new Dictionary<string, string>();

	void OnGUI()
	{
		if(showHud)
		{
			windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
		}

		if(setupGUI)
		{
			GUI.Window (1, new Rect(0.0f, 0.0f, Screen.width, Screen.height), SetupGUI, "Setup"); 
		}
		
		if(intermediate)
		{
			GUI.Window (2, new Rect(0.0f, 0.0f, Screen.width, Screen.height), IntermediateGUI, "Intermediate"); 
		}
		
		if(countdown)
		{
			GUI.Window (3, new Rect(0.0f, 0.0f, Screen.width, Screen.height), CountdownGUI, "Countdown"); 
		}
		
		if(stimulusEnd)
		{
			GUI.Window (4, new Rect(0.0f, 0.0f, Screen.width, Screen.height), StimulusEndGUI, "Stimulus Finished: Click where you spotted the target or press 'n' if you did not see any target"); 
		}
	}

	void DoMyWindow(int windowID) 
	{
		style.fontSize = 12;
		style.normal.textColor = Color.white;
		
		if(tempSettings.Count() == 0)
		{
			tempSettings = Settings.GetDictionarySettings();
		}
		
		int count = 0;
		
		foreach( KeyValuePair<string, string> kvp in new Dictionary<string, string>(tempSettings) )
		{
			GUI.Label(new Rect(10, guiTopOffset + count * guiIncrement, 150, 30), kvp.Key + ": ", style);
			string stringValue = GUI.TextField(new Rect(175, guiTopOffset + count * guiIncrement, 50, 20), kvp.Value.ToString(), style);
			stringValue = Regex.Replace(stringValue, @"[^0-9.]", "");
			
			if(stringValue != kvp.Value)
			{
				tempSettings[kvp.Key] = stringValue;
			}
			
			if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)		
			{				
				float tryParse = 0.0f;
				
				if(float.TryParse(stringValue, out tryParse))
				{
					tempSettings[kvp.Key] = tryParse.ToString();
				}
				else
				{
					print("Input field parsing failed");
					tempSettings[kvp.Key] = "0";
				}
			}
			
			count ++;
		}
		
		if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)		
		{				
			print("Applying and Saving Settings");
			
			string json = JsonConvert.SerializeObject(tempSettings);
			SettingsValues v = JsonConvert.DeserializeObject<SettingsValues>(json);
			Settings.Values = (SettingsValues)v.Clone();
			Settings.SaveSettings();
		}
		
		windowRect.height = guiTopOffset + guiDownOffset + count * guiIncrement;
		
		GUI.DragWindow(new Rect(0, 0, 10000, 10000));
	}

	void StimulusEndGUI(int windowID)
	{
		GUI.DrawTexture (new Rect (0.0f, 0.0f, Screen.width, Screen.height), (UnityEngine.Texture)Resources.Load ("stimulusEnd")); 
	}

	void CountdownGUI(int windowID)
	{
		int time = (int)stopWatch.ElapsedMilliseconds;

		if(time >= 2000)
		{
			countdown = false; 
			stimulus = true; 
			highlight = false; 

			StartNewStimulus();

			stopWatch.Stop (); 
			stopWatch.Reset (); 
			stopWatch.Start (); 
		}

		if (countdown)
		{
			string texName = "1"; 
			if(time < 1000) texName = "2";
			//		if(time < 1000) texName = "3";
			GUI.DrawTexture (new Rect (0.0f, 0.0f, Screen.width, Screen.height), (UnityEngine.Texture)Resources.Load (texName)); 
		}

	}

	void IntermediateGUI(int windowID)
	{
		GUI.DrawTexture (new Rect (0.0f, 0.0f, Screen.width, Screen.height), (UnityEngine.Texture)Resources.Load ("intermediate")); 
	}

	void SetupGUI(int windowID)
	{
		const float elemWidth = 150.0f; 
		const float elemHeight = 20.0f; 

		float xOffset = (Screen.width / 2.0f) - 150; 
		float yOffset = (Screen.height / 2.0f) - 50; 

		GUI.DrawTexture (new Rect (0.0f, 0.0f, Screen.width, Screen.height), (UnityEngine.Texture)Resources.Load ("guiBG")); 

		GUI.Label (new Rect (xOffset, yOffset, elemWidth, elemHeight), "User ID: "); 
		userID = GUI.TextField (new Rect (xOffset + 100, yOffset, elemWidth, elemHeight), userID); 

		GUI.Label (new Rect (xOffset, yOffset + elemHeight + 5, elemWidth, elemHeight), "Condition: "); 
		conditionID = GUI.TextField (new Rect (xOffset + 100, yOffset + elemHeight + 5, elemWidth, elemHeight), conditionID); 

		if(GUI.Button (new Rect(xOffset + 100, yOffset + 2 * elemHeight + 10, elemWidth, elemHeight), "Start!"))
		{
			if(userID.Length > 0)
			{
				distLogger = CreateLogger("distance"); 
				targetLogger = CreateLogger ("target"); 
				setupGUI = false; 
				intermediate = true; 
			}
		}
	}

	LogLib.Logger<int> CreateLogger(String name)
	{
		LogLib.Logger<int> logger = new LogLib.Logger<int> (name, userID, conditionID); 
		logger.AddFactor ("rep"); 
		logger.AddFactor ("ecc"); 
		logger.AddFactor ("tech");  
		return logger; 
	}

	void FiniLogger(LogLib.Logger<int> logger, String name)
	{
		string fileName = name + ".csv"; 
		StreamWriter fileWriter = new StreamWriter (fileName, true); 
		bool writeHeader = (new FileInfo(fileName).Length == 0); 
		logger.WriteSingleRowCSV(fileWriter, writeHeader);
	}

	void Log(LogLib.Logger<int> logger, Stimulus stimulus, int value)
	{
		logger.NewEntry(); 
		logger.Log ("rep", stimulus.repeatId.ToString()); 
		logger.Log ("ecc", stimulus.visionArea.ToString ()); 
		logger.Log ("tech", stimulus.techID.ToString ()); 
		logger.Log (value); 
	}

	void Update () 
	{
		Camera.main.orthographicSize = Screen.height * 0.5f;
		
		BoxSize.x = Screen.width;
		BoxSize.y = Screen.height;
		BoxSize.z = Settings.Values.molScale * 2;
		
		collideBox.transform.localScale = new Vector3(BoxSize.x, BoxSize.y, BoxSize.z);

		if(stimulus && !highlight){
			int time = (int)stopWatch.ElapsedMilliseconds; 
			if(time >= currentStimulus.delay){
				stimulusObject.StartFocus(currentStimulus.techID);
				foreach(MolObject mol in molObjects){
					if(mol != stimulusObject)
						mol.StartContext(currentStimulus.techID); 
				}
				print("Start highlight, visionArea: " + currentStimulus.visionArea + " technique: " + currentStimulus.techID + " distance: " +stimulusObject.gameObject.transform.position.x);
				stopWatch.Reset (); 
				highlight = true; 
			}
		}

		if(highlight)
		{
			int time = (int)stopWatch.ElapsedMilliseconds; 
			if(time >= currentStimulus.duration || Input.GetKeyDown("space"))
			{
				StopStimulus(); 
			}
		}

		if(stimulusEnd)
		{
			Vector3 mousePos = new Vector3(-1.0f, -1.0f, -1.0f); 
			bool targetFound = false; 
			if(Input.GetMouseButtonUp(0))
			{
				mousePos = Input.mousePosition; 
				mousePos.x -= Screen.width / 2; 
				mousePos.y -= Screen.height / 2; 
				stimulusEnd = false; 
				targetFound = true; 
			}
			if(Input.GetKeyDown ("n"))
			{
				stimulusEnd = false; 
			}
			if(!stimulusEnd)
			{
				int dist = -1; 
				if(targetFound)
					dist = (int)(Vector3.Distance(stimulusObject.transform.position, mousePos));
			
				Log (distLogger, currentStimulus, dist); 
				Log (targetLogger, currentStimulus, Convert.ToInt32(targetFound)); 

				print ("Stimulus: " + currentStimulus + ": distance: " + dist + " -- target: " + targetFound); 

				if(currentStimulusIndex >= stimuli.Count())
				{
					FiniLogger(distLogger, "distance");
					FiniLogger(targetLogger, "target");

					LoadScene();
				}
				else
				{

					intermediate = true; 
				}
			}

		}

		if (Input.GetKeyDown (KeyCode.R))
		{
			LoadScene();
		}

		if (Input.GetKeyDown (KeyCode.H))
		{
			showHud = !showHud;
		}

		if (Input.GetKeyDown (KeyCode.Escape))
		{
			Application.Quit();
		}

		if(Input.GetKeyDown (KeyCode.Return))
		{
			if(intermediate)
			{
				countdown = true; 
				stopWatch.Reset(); 
				stopWatch.Start (); 
				intermediate = false; 
				Screen.showCursor = false; 
			}
		}
	}

	void StartNewStimulus ()
	{
		Animate = true;

		currentStimulus = stimuli[currentStimulusIndex];

		var shuffle = (from mol in molObjects orderby  Guid.NewGuid() select mol);
		molObjects = shuffle.ToArray();

		print ("Find molObject at position < " + Settings.Values.fovealLimit + " or > " + Settings.Values.periphLimit + " (visionArea=" + currentStimulus.visionArea + ")"); 

		foreach(MolObject mol in molObjects)
		{
			if(currentStimulus.visionArea == 0)
			{
				if(Math.Abs(mol.transform.position.x) < Settings.Values.fovealLimit )
				{
					stimulusObject = mol;
					break;
				}
			}
			else
			{
				if(Math.Abs(mol.transform.position.x) > Settings.Values.periphLimit )
				{
					stimulusObject = mol;
					break;
				}
			}
		}

		if(stimulusObject == null)
		{
			throw new System.Exception("Did not find scene element that matches the stimulus properties");
		}

		//stimulusObject.StartStimulus();
		print("Start stimulus, visionArea: " + currentStimulus.visionArea + " technique: " + currentStimulus.techID + " distance: " +stimulusObject.gameObject.transform.position.x);

		currentStimulusIndex ++;
	}

	void StopStimulus()
	{
		Animate = false;

		stopWatch.Stop(); 
		stopWatch.Reset (); 
		
		stimulusObject.StopFocus();
		foreach(MolObject mol in molObjects){
			if(mol != stimulusObject)
				mol.StopContext(); 
		}
		
		stimulus = false; 
		stimulusEnd = true; 

//		stimulusObject = null;
//		currentStimulus = null;
		
		Screen.showCursor = true; 
	}

	void FixedUpdate()
	{

	}
}