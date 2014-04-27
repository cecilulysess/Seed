/************************************************************************************

Filename    :   FlyerController.cs
Content     :   Player controller interface. 

				This script derived from OVR Player Controller script and 
				modified by Yanxiang Wu.
				This script drives OVR camera as well as controls the locomotion
				of the player, and handles physical contact in the world for the 
				flying player.
Created     :   April 6th 2014
Authors     :   Yanxiang Wu
************************************************************************************/

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]

//-------------------------------------------------------------------------------------
// ***** OVRPlayerController
//
// OVRPlayerController implements a basic first person controller for the Rift. It is 
// attached to the OVRPlayerController prefab, which has an OVRCameraController attached
// to it. 
// 
// The controller will interact properly with a Unity scene, provided that the scene has
// collision assigned to it. 
//
// The OVRPlayerController prefab has an empty GameObject attached to it called 
// ForwardDirection. This game object contains the matrix which motor control bases it
// direction on. This game object should also house the body geometry which will be seen
// by the player.
//
public class FlyerController : OVRComponent
{
	protected CharacterController 	Controller 		 = null;
	protected OVRCameraController 	CameraController = null;
	
	public float Acceleration 	   = 0.1f;
	public float Damping 		   = 0.15f;
	public float BackAndSideDampen = 0.5f;
	public float JumpForce 		   = 0.3f;
	public float RotationAmount    = 1.5f;
	public float GravityModifier   = 0.379f;
	
	private float   MoveScale 	   = 1.0f;
	private Vector3 MoveThrottle   = Vector3.zero;
	private float   FallSpeed 	   = 0.0f;
	
	// Initial direction of controller (passed down into CameraController)
	private Quaternion OrientationOffset = Quaternion.identity;			
	// Rotation amount from inputs (passed down into CameraController)
	private float 	YRotation 	 = 0.0f;
	
	// Transfom used to point player in a given direction; 
	// We should attach objects to this if we want them to rotate 
	// separately from the head (i.e. the body)
	protected Transform DirXform = null;
	
	// We can adjust these to influence speed and rotation of player controller
	private float MoveScaleMultiplier     = 1.0f; 
	private float RotationScaleMultiplier = 1.0f; 
//	private bool  AllowMouseRotation      = true;
//	private bool  HaltUpdateMovement      = false;
	
	// TEST: Get Y from second sensor
	private float YfromSensor2            = 0.0f;

	// Jude: Is the user is flying
	public bool IsFlying = false;

	public PostureController PostureStates;
	// * * * * * * * * * * * * *
	protected Animator animator;
	
	// Awake
	new public virtual void Awake()
	{
		base.Awake();
		// We use Controller to move player around
		Controller = gameObject.GetComponent<CharacterController>();

		// We use OVRCameraController to set rotations to cameras, 
		// and to be influenced by rotation
		OVRCameraController[] CameraControllers;
		CameraControllers = gameObject.GetComponentsInChildren<OVRCameraController>();
		
		if(CameraControllers.Length == 0)
			Debug.LogWarning("OVRPlayerController: No OVRCameraController attached.");
		else if (CameraControllers.Length > 1)
			Debug.LogWarning("OVRPlayerController: More then 1 OVRCameraController attached.");
		else
			CameraController = CameraControllers[0];	
		
		// Instantiate a Transform from the main game object (will be used to 
		// direct the motion of the PlayerController, as well as used to rotate
		// a visible body attached to the controller)
		DirXform = null;
		Transform[] Xforms = gameObject.GetComponentsInChildren<Transform>();
		
		for(int i = 0; i < Xforms.Length; i++)
		{
			if(Xforms[i].name == "ForwardDirection")
			{
				DirXform = Xforms[i];
				break;
			}
		}
		
		if(DirXform == null)
			Debug.LogWarning("OVRPlayerController: ForwardDirection game object not found. Do not use.");
	}
	
	// Start
	new public virtual void Start()
	{
		base.Start();
		
		InitializeInputs();	
		Debug.Log ("Initialized Flyer Controller");
		SetCameras();

		animator = GetComponentsInChildren<Animator>()[0];
		
		if(animator.layerCount >= 2)
			animator.SetLayerWeight(1, 1);
	}
	
	// Update 
	new public virtual void Update()
	{
		base.Update();

		if (!IsFlying)
			return;
		// Test: get Y from sensor 2 
		if(OVRDevice.SensorCount == 2)
		{
			Quaternion q = Quaternion.identity;
			OVRDevice.GetPredictedOrientation(1, ref q);
			YfromSensor2 = q.eulerAngles.y;
		}
		
		UpdateMovement();
		 
		Vector3 moveDirection = Vector3.zero;
		
		float motorDamp = (1.0f + (Damping * DeltaTime));
		MoveThrottle.x /= motorDamp;
		MoveThrottle.y = MoveThrottle.y / motorDamp;
		MoveThrottle.z /= motorDamp;

		//Jude: Speed testing 
		moveDirection += MoveThrottle * 3 * DeltaTime;
//		Debug.Log ("Move Direction:" + moveDirection);

//		// Gravity
//		if (Controller.isGrounded && FallSpeed <= 0)
//			FallSpeed = ((Physics.gravity.y * (GravityModifier * 0.002f)));	
//		else
//			FallSpeed += ((Physics.gravity.y * (GravityModifier * 0.002f)) * DeltaTime);	
//
//		if (!IsFlying) {
//			moveDirection.y += FallSpeed * DeltaTime;
//		}

//		// Offset correction for uneven ground
//		float bumpUpOffset = 0.0f;
//		
//		if (Controller.isGrounded && MoveThrottle.y <= 0.001f)
//		{
//			bumpUpOffset = Mathf.Max(Controller.stepOffset, 
//			                         new Vector3(moveDirection.x, 0, moveDirection.z).magnitude); 
//			moveDirection -= bumpUpOffset * Vector3.up;
//		}			
		
		Vector3 predictedXZ = Vector3.Scale((Controller.transform.localPosition + moveDirection), 
		                                    new Vector3(1, 0, 1));	
		
		// Move contoller
		Controller.Move(moveDirection);
		
		Vector3 actualXZ = Vector3.Scale(Controller.transform.localPosition, new Vector3(1, 0, 1));
		
		if (predictedXZ != actualXZ)
			MoveThrottle += (actualXZ - predictedXZ) / DeltaTime; 
		
		// Update rotation using CameraController transform, possibly proving some rules for 
		// sliding the rotation for a more natural movement and body visual
		UpdatePlayerForwardDirTransform();
	}
	

	// UpdateMovement
	//
	// COnsolidate all movement code here
	//
	static float sDeltaRotationOld = 0.0f;
	float ForwardSpeed = 0.0f, DirHori = 0.0f, DirVert = 0.0f;
	public virtual void UpdateMovement()
	{
//		// Do not apply input if we are showing a level selection display
//		if(HaltUpdateMovement == true)
//			return;
		
		bool moveForward = false;
		bool moveLeft  	 = false;
		bool moveRight   = false;
		bool moveBack    = false;
		bool moveUp = false;
		bool moveDown = false;

		MoveScale = 1.0f;
		
		// * * * * * * * * * * *
		// Keyboard input
		
		// Move
		Debug.Log("PC.MoveForward is: " + PostureController.MoveForward);
		// WASD
		if (Input.GetKey(KeyCode.W) || PostureController.MoveForward) moveForward = true;
		if (Input.GetKey(KeyCode.A)) moveLeft	 = true;
		if (Input.GetKey(KeyCode.S)) moveBack 	 = true; 
		if (Input.GetKey(KeyCode.D)) moveRight 	 = true; 
		if (Input.GetKey (KeyCode.Q))
						moveUp = true;
		if (Input.GetKey (KeyCode.E))
						moveDown = true;
//		// Arrow keys
//		if (Input.GetKey(KeyCode.UpArrow) || PostureStates.MoveForward)    moveForward = true;
//		if (Input.GetKey(KeyCode.LeftArrow))  moveLeft 	  = true;
//		if (Input.GetKey(KeyCode.DownArrow))  moveBack 	  = true; 
//		if (Input.GetKey(KeyCode.RightArrow)) moveRight   = true; 
		
		if ( (moveForward && moveLeft) || (moveForward && moveRight) ||
		    (moveBack && moveLeft)    || (moveBack && moveRight) )
			MoveScale = 0.70710678f;

		// Jude: Animator Controlling parameters
		if (moveForward) {
						ForwardSpeed = 0.5f;
				} else {
						if (moveBack) {
								ForwardSpeed = -0.5f;
						} else {
								ForwardSpeed = 0.0f;
						}
				}
		if (moveLeft) {
						DirHori = -0.5f;
				} else {
						if (moveRight)
								DirHori = 0.5f;
						else 
								DirHori = 0.0f;
				}
		if (moveUp) {
			DirVert = 0.5f;
		} else {
			if (moveDown)
				DirVert = -0.5f;
			else 
				DirVert = 0.0f;
		}
		// Jude: need change according to the dynamics
		if (moveUp)
						MoveScale *= 0.707f;
		if (moveDown) 
						MoveScale *= 1.1f;
		
//		// No positional movement if we are in the air
//		if (!Controller.isGrounded)	
//			MoveScale = 0.0f;
		
		MoveScale *= DeltaTime;
		
		// Compute this for key movement
		float moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;
		
		// Run!
		if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift)) {
						moveInfluence *= 4.0f;
						ForwardSpeed *= 2.0f;
						DirHori *= 2.0f;
						DirVert *= 2.0f;
				}

		
		if(DirXform != null)
		{
			if (moveForward)
				MoveThrottle += DirXform.TransformDirection(Vector3.forward * moveInfluence);
			if (moveBack)
				MoveThrottle += DirXform.TransformDirection(Vector3.back * moveInfluence) * BackAndSideDampen;
//			if (moveLeft)
//				MoveThrottle += DirXform.TransformDirection(Vector3.left * moveInfluence) * BackAndSideDampen;
//			if (moveRight)
//				MoveThrottle += DirXform.TransformDirection(Vector3.right * moveInfluence) * BackAndSideDampen;
			if (moveUp)
				MoveThrottle += DirXform.TransformDirection(Vector3.up * moveInfluence);
			if (moveDown)
				MoveThrottle += DirXform.TransformDirection(Vector3.down * moveInfluence);
		}
		
		// Rotate
		
		// compute for key rotation
		float rotateInfluence = DeltaTime * RotationAmount * RotationScaleMultiplier;

		if (moveLeft)
						YRotation -= rotateInfluence * 0.1f;
		if (moveRight)
						YRotation += rotateInfluence * 0.1f;
//		//reduce by half to avoid getting ill
//		if (Input.GetKey(KeyCode.Q)) 
//			YRotation -= rotateInfluence * 0.5f;  
//		if (Input.GetKey(KeyCode.E)) 
//			YRotation += rotateInfluence * 0.5f; 
		
//		// * * * * * * * * * * *
//		// Mouse input
//		
//		// Move
//		
//		// Rotate
//		float deltaRotation = 0.0f;
////		if(AllowMouseRotation == false)
////			deltaRotation = Input.GetAxis("Mouse X") * rotateInfluence * 3.25f;
//		
//		float filteredDeltaRotation = (sDeltaRotationOld * 0.0f) + (deltaRotation * 1.0f);
//		YRotation += filteredDeltaRotation;
//		sDeltaRotationOld = filteredDeltaRotation;
//		
//		// * * * * * * * * * * *
//		// XBox controller input	
//		
//		// Compute this for xinput movement
//		moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;
//		
//		// Run!
//		moveInfluence *= 1.0f + 
//			OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftTrigger);
//		
//		// Move
//		if(DirXform != null)
//		{
//			float leftAxisY = 
//				OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftYAxis);
//			
//			float leftAxisX = 
//				OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.LeftXAxis);
//			
//			if(leftAxisY > 0.0f)
//				MoveThrottle += leftAxisY *
//					DirXform.TransformDirection(Vector3.forward * moveInfluence);
//			
//			if(leftAxisY < 0.0f)
//				MoveThrottle += Mathf.Abs(leftAxisY) *		
//					DirXform.TransformDirection(Vector3.back * moveInfluence) * BackAndSideDampen;
//			
//			if(leftAxisX < 0.0f)
//				MoveThrottle += Mathf.Abs(leftAxisX) *
//					DirXform.TransformDirection(Vector3.left * moveInfluence) * BackAndSideDampen;
//			
//			if(leftAxisX > 0.0f)
//				MoveThrottle += leftAxisX *
//					DirXform.TransformDirection(Vector3.right * moveInfluence) * BackAndSideDampen;
//			
//		}
//		
//		float rightAxisX = 
//			OVRGamepadController.GPC_GetAxis((int)OVRGamepadController.Axis.RightXAxis);
//		
//		// Rotate
//		YRotation += rightAxisX * rotateInfluence;    
		
		// Update cameras direction and rotation
		SetCameras();

		SetAnimator (ForwardSpeed, DirHori, DirVert);
	}

	void SetAnimator(float fs, float dh, float dv){
//		Debug.Log ("Set animator for: " + animator);
		animator.SetFloat("ForwardSpeed", fs, 0.5f, Time.deltaTime);
		animator.SetFloat("DirectionHorizontal", dh, .25f, Time.deltaTime);	
		animator.SetFloat("DirectionVertical", dv, .25f, Time.deltaTime);	
	}

	// UpdatePlayerControllerRotation
	// This function will be used to 'slide' PlayerController rotation around based on 
	// CameraController. For now, we are simply copying the CameraController rotation into 
	// PlayerController, so that the PlayerController always faces the direction of the 
	// CameraController. When we add a body, this will change a bit..
	public virtual void UpdatePlayerForwardDirTransform()
	{
//		if ((DirXform != null) && (CameraController != null))
//		{
//			Quaternion q = Quaternion.identity;
//			q = Quaternion.Euler(0.0f, YfromSensor2, 0.0f);
//			//			Debug.Log("Update " + DirXform + "'s quaternion to:" + q * CameraController.transform.rotation);
//			DirXform.rotation = q * CameraController.transform.rotation;
//		}
		//Jude: Make it turns body only
		if ((DirXform != null))
		{
//			Quaternion q = Quaternion.identity;
//			q = Quaternion.Euler(0.0f, YRotation, 0.0f);
//			//			Debug.Log("Update " + DirXform + "'s quaternion to:" + q * CameraController.transform.rotation);
//			Vector3 dir = q * Vector3.forward;
//			q.SetLookRotation(dir, Vector3.up);
//			DirXform.rotation = q;
//			Debug.Log("Yrotation: " + YRotation);
			transform.Rotate (Vector3.up, YRotation);
			// Jude: may removed if make camera follow the body
			CameraController.SetOrientationOffset(transform.rotation);
			YRotation = 0.0f;
			
//			transform.rotation = DirXform.transform.rotation;
		}
	}
	
	///////////////////////////////////////////////////////////
	// PUBLIC FUNCTIONS
	///////////////////////////////////////////////////////////
	

	// Stop
	public void Stop()
	{
		Controller.Move(Vector3.zero);
		MoveThrottle = Vector3.zero;
		FallSpeed = 0.0f;
	}	
	
	// InitializeInputs
	public void InitializeInputs()
	{
		// Get our start direction
		OrientationOffset = transform.rotation;
		// Make sure to set y rotation to 0 degrees
		YRotation = 0.0f;
	}
	
	// SetCameras
	public void SetCameras()
	{
		if(CameraController != null)
		{
			// Make sure to set the initial direction of the camera 
			// to match the game player direction
			CameraController.SetOrientationOffset(OrientationOffset);
//			CameraController.SetYRotation(YRotation);
		}
	}
	
	// Get/SetMoveScaleMultiplier
	public void GetMoveScaleMultiplier(ref float moveScaleMultiplier)
	{
		moveScaleMultiplier = MoveScaleMultiplier;
	}
	public void SetMoveScaleMultiplier(float moveScaleMultiplier)
	{
		MoveScaleMultiplier = moveScaleMultiplier;
	}
	
	// Get/SetRotationScaleMultiplier
	public void GetRotationScaleMultiplier(ref float rotationScaleMultiplier)
	{
		rotationScaleMultiplier = RotationScaleMultiplier;
	}
	public void SetRotationScaleMultiplier(float rotationScaleMultiplier)
	{
		RotationScaleMultiplier = rotationScaleMultiplier;
	}
	

	
}

