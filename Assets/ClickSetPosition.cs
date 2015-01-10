using UnityEngine;
using System.Collections;

public class ClickSetPosition : MonoBehaviour {
	
	//public PropertiesAndCoroutines	coroutineScript;
	
	
	
	//private PropertiesAndCoroutines other;
	
	private GameObject go = null;
	private PropertiesAndCoroutines sn = null;
	
	
	void Awake()
	{
		Debug.Log ("Awake started");
		go = GameObject.Find("MyCube");
		
		if (go == null) 
		{
			Debug.Log ("find couldn't find MyCube");
		} 
		else 
		{
			Debug.Log ("MyCube was found using Find.");		
			sn = go.GetComponent<PropertiesAndCoroutines> ();
		}
		//other = (PropertiesAndCoroutines) go.GetComponent(typeof(PropertiesAndCoroutines));
		Debug.Log ("onAwake ended");
	}
	/*
	void onMouseDown()
	{
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;

		Debug.Log ("onMouseDown");

		Physics.Raycast(ray, out hit);

		if (hit.collider.gameObject == gameObject) 
		{
			//coroutineScript.Target = hit.point;	
		//	sn.Target = hit.point;
			Debug.Log ("onMouseDown 2");

		}
	}*/
	
	void Update() 
	{
		//Debug.Log ("Running...");
		
		if (Input.GetMouseButtonDown(0))
		{
			System.Console.WriteLine("Entered game settings menu.");
			Debug.Log ("Mouse button down next make ray.");
			
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			
			Debug.Log ("Now try physics and Raycast to get RaycastHit...");
			
			RaycastHit hit;

			bool result = Physics.Raycast(ray, out hit);

			Debug.DrawLine(Camera.main.transform.position, hit.point, Color.red);
			
			if (result == false) 
			{ 
				Debug.Log("We click on nothing!");
			}
			else
			{
				Debug.Log ("OK physics worked and we clicked on something, see if we got the plane..");
				
				if (hit.collider.gameObject == gameObject) 
				{
					Debug.Log ("hit!");
					//coroutineScript.Target = hit.point;	
					if (sn == null) 
					{
						Debug.Log ("Couldn't get script sn == null !!!");
					} 
					else 
					{
						Debug.Log ("Found script! Using it!");
						sn.Target = hit.point;
						Debug.Log ("Success!!");
					} 
				}
			}
		}
	}
}
