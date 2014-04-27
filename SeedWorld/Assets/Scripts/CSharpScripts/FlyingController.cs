using UnityEngine;
using System.Collections;

public class FlyingController : MonoBehaviour {
	protected Animator anim;

	public FlyerController FlyerControllerReference;

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown("space") || PostureController.IsStartFlying ) {
			Debug.Log("Set start flying");
			anim.SetBool("StartFlying", true);
//			this.transform.parent.GetComponent<ControllerServer>().StartServer();
		}
	}
	

	//Mecanim AnimationEvent called when ReadyForFly animation done.
	void OnReadyForFlying(int value){
		Debug.Log("Triggered On Ready for Flying animation");
		if (FlyerControllerReference) {
			Debug.Log("Set is flying to true");
			FlyerControllerReference.IsFlying = true;
		}
	}
}
