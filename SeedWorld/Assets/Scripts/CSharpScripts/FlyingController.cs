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
		if (Input.GetKeyDown("space")) {
			anim.SetBool("StartFlying", true);
		}
	}

	//Mecanim AnimationEvent called when ReadyForFly animation done.
	void OnReadyForFlying(int value){
		if (FlyerControllerReference) {
			FlyerControllerReference.IsFlying = true;
		}
	}
}
