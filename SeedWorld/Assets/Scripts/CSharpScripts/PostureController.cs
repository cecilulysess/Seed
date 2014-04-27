using UnityEngine;
using System.Collections;

public interface ILocomotionState{
	ILocomotionState Next(double accx, double accy, double accz, double rotx, double roty, double rotz);

	string ToString();
}

/* Posture controller maintained the flying states of the virtual agent. 
 * It takes the input value of from the tracker and transfer it to corresponding 
 * manipulation states of the agent.
 * 
 */
public class PostureController : MonoBehaviour {
	public static bool MoveForward;

	public static bool MoveLeft;

	public static bool MoveRight;

	public static bool MoveUp;

	public static bool MoveDown;

	public static bool IsStartFlying { 
		get { 
			if (isStartFlying) {
				isStartFlying = false; 
				return true;
			} 
			return false;
		}  }

	private static bool isStartFlying;

	private static long initAnimationWaitCount;
	private static long KWAITINGCOUNT = 200000000;

	private double lax, lay, laz, rax,ray, raz, lrx, lry, lrz, rrx, rry, rrz;

	public ILocomotionState State { get { return state; }}

	private ILocomotionState state;

	public delegate void OnStateChange(PostureController newpose);

	public OnStateChange StateChanged;

	private FlyerController fc;
	// Use this for initialization
	void Start () {
		ControllerServer cs = this.transform.GetComponent<ControllerServer> ();
		cs.reporter += UpdateData;
		state = StandbyState.Instance;
		fc = transform.GetComponent<FlyerController>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void UpdateData(bool isLeft, double ax, double ay, double az, double rx, double ry, double rz){
		//		Debug.Log ("Input data:" + isLeft + ", " + ax);
		if (isLeft) {
			lax = ax;
			lay = ay;
			laz = az;
			lrx = rx;
			lry = ry;
			lrz = rz;
		} else {
			rax = ax;
			ray = ay;
			raz = az;
			rrx = rx;
			rry = ry;
			rrz = rz;
		}

		ILocomotionState newstate = this.state.Next(lax, lay, laz, lrx, lry, lrz);

		if (newstate != state) {
			Debug.Log("Changing to new state: " + newstate.ToString());
			if (newstate is NormalForwardState) {
				Debug.Log("Enable flying");
				fc.IsFlying = true;
			} 
		}
		state = newstate;
//		Debug.Log("Update data from tracker");
	}

	/// Thread safe implementation of singleton state
	/// standby state stand for the state before flying
	sealed class StandbyState : ILocomotionState {
		private StandbyState(){}
		private int cnt = 0;
		public static StandbyState Instance { get {return Nested.instance;} }

		private class Nested {
			static Nested() {}
			internal static readonly StandbyState instance = new StandbyState();
		}

		public ILocomotionState Next(double accx, double accy, double accz, double rotx, double roty, double rotz) {
			if (accy < -0.18) {
				cnt ++;
				Debug.Log(string.Format("Detect y axis triggered {0} times", cnt));
				PostureController.isStartFlying = true;
				while(PostureController.initAnimationWaitCount++ < PostureController.KWAITINGCOUNT);
				return IdleState.Instance;
			}
			return Instance;
		}

		public string ToString() {return "Standby State";}
	}

	/// Thread safe implementation of singleton state
	/// idle state stand for the state ready to take action
	sealed class IdleState : ILocomotionState {
		private IdleState(){}
		public static IdleState Instance { get {return Nested.instance;} }
		private int cnt = 0;	
		private class Nested {
			static Nested() {}
			internal static readonly IdleState instance = new IdleState();
		}

		public ILocomotionState Next(double accx, double accy, double accz, double rotx, double roty, double rotz) {
			PostureController.MoveForward = false;
			PostureController.MoveDown = false;
			PostureController.MoveLeft = false;
			PostureController.MoveUp = false;
			PostureController.MoveRight = false;

			if (accz < -0.35) {
				cnt ++;
				PostureController.MoveForward = true;
				Debug.Log("Set moving forward");
				Debug.Log(string.Format("Detect z axis triggered {0} times", cnt));
				return NormalForwardState.Instance;
			}
			return Instance;
		}

		public string ToString() {return "Idle State";}
	}

	/// Thread safe implementation of singleton state
	sealed class NormalForwardState : ILocomotionState {
		private NormalForwardState(){}
		public static NormalForwardState Instance { get {return Nested.instance;} }
		private int cnt = 0;
		private class Nested {
			static Nested() {}
			internal static readonly NormalForwardState instance = new NormalForwardState();
		}

		public ILocomotionState Next(double accx, double accy, double accz, double rotx, double roty, double rotz) {
//			PostureController.MoveForward = false;
//			PostureController.MoveDown = false;
//			PostureController.MoveLeft = false;
//			PostureController.MoveUp = false;
//			PostureController.MoveRight = false;
			
			if (accz  > 0.25) {
				cnt ++;
				Debug.Log(string.Format("Detect stop z axis triggered {0} times", cnt));
				PostureController.MoveForward = false;
				return IdleState.Instance;
			}
			//if (rotz < )
//			PostureController.MoveForward = false;
			return Instance;
		}

		public string ToString() {return "NormalForward State";}
	}

	/// Thread safe implementation of singleton state
	sealed class TurnLeftState : ILocomotionState {
		private TurnLeftState(){}
		public static TurnLeftState Instance { get {return Nested.instance;} }
		private int cnt = 0;
		private class Nested {
			static Nested() {}
			internal static readonly TurnLeftState instance = new TurnLeftState();
		}

		public ILocomotionState Next(double accx, double accy, double accz, double rotx, double roty, double rotz) {
//			PostureController.MoveForward = false;
//			PostureController.MoveDown = false;
//			PostureController.MoveLeft = false;
//			PostureController.MoveUp = false;
//			PostureController.MoveRight = false;
			
			if (accz  > 0.25) {
				cnt ++;
				Debug.Log(string.Format("Detect stop z axis triggered {0} times", cnt));
				PostureController.MoveForward = false;
				return IdleState.Instance;
			}
//			PostureController.MoveForward = false;
			return Instance;
		}

		public string ToString() {return "TurnLeft State";}
	}

}
