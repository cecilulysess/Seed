/****************************************************************
 * Author: Julian Wu [cosmobserver@gmail.com]
 * This file contains code that allow the head of the virtual
 * agent rotate align with the Oculus Rift rotation
 * 
****************************************************************/

using UnityEngine;
using System.Collections;

public class PlayerHeadController : MonoBehaviour {
	protected OVRCameraController cameraController = null;
	protected GameObject target = null;
	protected float limitation = 80.0f;

	// Use this for initialization
	void Awake () {
		OVRCameraController[] controllers = gameObject.GetComponentsInChildren<OVRCameraController>();
		if (controllers.Length == 1) {
				cameraController = controllers [0];
				target = GameObject.FindWithTag ("PlayerHead");
		} else {
				Debug.LogWarning ("Multiple OVR Cameracontroller found");
		}
		Debug.Log ("Finding controller and head" + controllers.Length);
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if (target != null) {
			float roty = cameraController.transform.localEulerAngles.y;
			if (roty < limitation) roty = limitation;
			else if (roty > -limitation + 360) roty = 360 - limitation;
			target.transform.localEulerAngles = new Vector3( 
			                                                -cameraController.transform.localEulerAngles.y,
                0,
                0) ;
		}
	}
}
