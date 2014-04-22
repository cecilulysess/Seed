using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.Collections.Generic;

public class StateObject {
	public Socket workSocket = null;
	public const int BufferSize = 1024;
	public byte[] buffer = new byte[BufferSize];
	public StringBuilder sb = new StringBuilder();
}

public class ControllerServer : MonoBehaviour {
//	private ManualResetEvent allDone;
	private TcpListener listener;
	const int CONN_LIMIT = 2;
	private List<Thread> tp = new List<Thread>();
	public delegate void UpdateDataDelegate(bool isLeft, double ax, double ay, double az);

	public UpdateDataDelegate reporter;

	// Use this for initialization
	void Start () {
		listener = new TcpListener (IPAddress.Parse("0.0.0.0"), 8080);
		listener.Start ();
		Debug.Log ("Server Listening on port 8080");
		for (int i = 0; i < CONN_LIMIT; ++i) {
			Thread t = new Thread(new ThreadStart(Listening));
			t.Start ();
			tp.Add(t);

		}
		Debug.Log ("Start Server end");

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void Listening() {
		bool isLeft;
		double accx, accy, accz;
		while (true) {
			Socket so = listener.AcceptSocket();
			Debug.Log("Connected: " + so.RemoteEndPoint);

			try{
				Stream s = new NetworkStream(so);
				BinaryReader sr = new BinaryReader(s);
				BinaryWriter sw = new BinaryWriter(s);
//				sw.AutoFlush = true;
//				sw.WriteLine("Server response");
				while(true) {
//					string clientipt = sr.ReadLine();
//					Debug.Log("Client " +so.RemoteEndPoint + " input: " + clientipt);
					isLeft = (sr.ReadInt32() != 0) ;
					accx = sr.ReadDouble();
					accy = sr.ReadDouble();
					accz = sr.ReadDouble();
//					Debug.Log ("Client isLeft:" + isLeft + ", xyz:" + accx + ", "+accy+ "," + accz);
					if (reporter!= null){
						reporter(isLeft, accx, accy, accz);
					}
				}
				s.Close();
			} catch (Exception e) {
				Debug.LogException( e);
			}
			Debug.Log("Disconnect client: " + so.RemoteEndPoint);
			so.Close();
		}
	}

	void OnApplicationQuit(){
		foreach (Thread t in tp) {
			if (t.IsAlive) 
				t.Abort();
		}
		listener.Stop ();
	}


//	public void StartListening() {
//		IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
//		IPEndPoint localEP = new IPEndPoint(ipHostInfo.AddressList[0],8080);
//		
//		Console.WriteLine("Local address and port : {0}",localEP.ToString());
//		
//		Socket listener = new Socket( localEP.Address.AddressFamily,
//		                             SocketType.Stream, ProtocolType.Tcp );
//		
//		try {
//			listener.Bind(localEP);
//			listener.Listen(10);
//			
//			while (true) {
//				allDone.Reset();
//				
//				Console.WriteLine("Waiting for a connection...");
//				listener.BeginAccept(
//					new AsyncCallback(ControllerServer.acceptCallback), 
//					listener );
//				
//				allDone.WaitOne();
//			}
//		} catch (Exception e) {
//			Console.WriteLine(e.ToString());
//		}
//		
//		Console.WriteLine( "Closing the listener...");
//	}
//
//	void acceptCallback( IAsyncResult ar) {
//		allDone.Set();
//		
//		Socket listener = (Socket) ar.AsyncState;
//		Socket handler = listener.EndAccept(ar);
//		// Create the state object.
//		StateObject state = new StateObject();
//		state.workSocket = handler;
//		handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
//		                     new AsyncCallback(ControllerServer.readCallback), state);
//	}
//
//
//	public static void readCallback(IAsyncResult ar) {
//		StateObject state = (StateObject) ar.AsyncState;
//		Socket handler = state.WorkSocket;
//		
//		// Read data from the client socket.
//		int read = handler.EndReceive(ar);
//		
//		// Data was read from the client socket.
//		if (read > 0) {
//			state.sb.Append(Encoding.ASCII.GetString(state.buffer,0,read));
//			handler.BeginReceive(state.buffer,0,StateObject.BufferSize, 0,
//			                     new AsyncCallback(readCallback), state);
//		} else {
//			if (state.sb.Length > 1) {
//				// All the data has been read from the client;
//				// display it on the console.
//				string content = state.sb.ToString();
//				Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
//				                  content.Length, content);
//			}
//			handler.Close();
//		}
//	}
}
