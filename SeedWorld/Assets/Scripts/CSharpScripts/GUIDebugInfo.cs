using UnityEngine;
using System.Collections;

public class GUIDebugInfo : MonoBehaviour {
	private bool ShowDebug = false;
	public GUIStyle guiskin;

	private double lax, lay, laz, rax,ray, raz;
	// Use this for initialization
	void Start () {
		ControllerServer cs = this.transform.GetComponent<ControllerServer> ();
		cs.reporter += UpdateData;
	}
	
	// Update is called once per frame
	void Update () {


		if (Input.GetKeyDown ("q")) {
				ShowDebug = !ShowDebug;
		}
	}

	void UpdateData(bool isLeft, double ax, double ay, double az){
//		Debug.Log ("Input data:" + isLeft + ", " + ax);
		if (isLeft) {
			lax = ax;
			lay = ay;
			laz = az;
		} else {
			rax = ax;
			ray = ay;
			raz = az;
		}
	}

	void OnGUI(){
			if (ShowDebug) {
					GUILayout.BeginArea (new Rect (10, Screen.height - 120, 250, 110), guiskin);
					GUILayout.BeginVertical ();
					GUILayout.Label ("Controller Left:");
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Acce X, Y, Z");
					GUILayout.Label (string.Format("[{0:F3}, {1:F3}, {2:F3}]", lax, lay, laz));
					GUILayout.EndHorizontal ();
		
					GUILayout.Label ("Controller Right:");
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Acce X, Y, Z");
					GUILayout.Label (string.Format("[{0:F3}, {1:F3}, {2:F3}]", rax, ray, raz));
					GUILayout.EndHorizontal ();
		
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Calculated Control Action: ");
					GUILayout.Label ("[" + 2 + "]");
					GUILayout.EndHorizontal ();
					GUILayout.EndVertical ();
					GUILayout.EndArea ();
			}
	}
}
