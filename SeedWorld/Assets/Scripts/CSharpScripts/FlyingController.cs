using UnityEngine;
using System.Collections;

public class FlyingController : MonoBehaviour {
	protected Animator anim;
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
}
