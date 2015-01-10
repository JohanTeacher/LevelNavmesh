
using UnityEngine;
using System.Collections;

public class PropertiesAndCoroutines : MonoBehaviour {
	
	
	private NavMeshAgent agent = null; 
	private GameObject Sphere1 = null;
	private SphereScript1 sn1 = null;
	
	private GameObject Sphere2 = null;
	private SphereScript2 sn2 = null;
	
	private GameObject Sphere3 = null;
	private SphereScript3 sn3 = null;
	
	private GameObject Sphere4 = null;
	private SphereScript4 sn4 = null;
	
	public float smoothing = 7f;
	
	public Vector3 Target
	{
		get { return target; }  // Why tagrget and not Target?
		
		// Lite magi...detta är smart att starta coroutiner inne från set på en variabel.
		// Sparar mycket prestanda. Märk att den ser först till att stoppa eventuell samma coruntiner som kör först.
		set
		{
			target = value; // Vart kom value i från? Kanske från Vector3 typen?
			
			agent.destination = target;
			
			if (sn1 == null) 
			{
				Debug.Log ("Couldn't get script sn1 == null !!!");
			} 
			else 
			{
				Debug.Log ("Found script 1! Using it!");
				sn1.Target = new Vector3(target.x-2f, target.y, target.z);
				//Debug.Log ("Success!!");
			} 
			
			if (sn2 == null) 
			{
				Debug.Log ("Couldn't get script sn2 == null !!!");
			} 
			else 
			{
				Debug.Log ("Found script 2! Using it!");
				sn2.Target = new Vector3(target.x+2f, target.y, target.z);
				//Debug.Log ("Success!!");
			} 
			
			if (sn3 == null) 
			{
				Debug.Log ("Couldn't get script sn3 == null !!!");
			} 
			else 
			{
				Debug.Log ("Found script 3! Using it!");
				sn3.Target = new Vector3(target.x, target.y, target.z+2f);
				//Debug.Log ("Success!!");
			} 
			
			if (sn4 == null) 
			{
				Debug.Log ("Couldn't get script sn4 == null !!!");
			} 
			else 
			{
				Debug.Log ("Found script 4! Using it!");
				sn4.Target = new Vector3(target.x, target.y, target.z-2f);
				//Debug.Log ("Success!!");
			} 
			
			//StopCoroutine("Movement");  // Stop kan bara användas om coroutinen startarts som en sträng dvs "Movement".
			//StartCoroutine("Movement", target);
		}
		
	}
	
	private Vector3 target;  // Detta är inte samma variable som Target...med stort T.
	
	void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		
		//Debug.Log ("Awake started");
		Sphere1 = GameObject.Find("Sphere1");
		
		if (Sphere1 == null) 
		{
			Debug.Log ("find couldn't find Sphere1");
		} 
		else 
		{
			Debug.Log ("Sphere1 was found using Find.");		
			sn1 = Sphere1.GetComponent<SphereScript1> ();
		}
		
		Sphere2 = GameObject.Find("Sphere2");
		
		if (Sphere2 == null) 
		{
			Debug.Log ("find couldn't find Sphere2");
		} 
		else 
		{
			Debug.Log ("Sphere2 was found using Find.");		
			sn2 = Sphere2.GetComponent<SphereScript2> ();
		}
		
		Sphere3 = GameObject.Find("Sphere3");
		
		if (Sphere3 == null) 
		{
			Debug.Log ("find couldn't find Sphere3");
		} 
		else 
		{
			Debug.Log ("Sphere3 was found using Find.");		
			sn3 = Sphere3.GetComponent<SphereScript3> ();
		}
		
		Sphere4 = GameObject.Find("Sphere4");
		
		if (Sphere4 == null) 
		{
			Debug.Log ("find couldn't find Sphere4");
		} 
		else 
		{
			Debug.Log ("Sphere4 was found using Find.");		
			sn4 = Sphere4.GetComponent<SphereScript4> ();
		}
		
		//Debug.Log ("onAwake ended");
	}
	
	
	
	IEnumerator Movement(Vector3 target) // Lite förvirrande att paramentern heter target också.
	{
		while (Vector3.Distance(transform.position, target) > 0.05f) 
		{
			transform.position = Vector3.Lerp(transform.position, target, smoothing * Time.deltaTime);
			
			yield return null;
		}
		
		//yield return new WaitForSeconds(2);
		
		
		if (sn1 == null) 
		{
			Debug.Log ("Couldn't get script sn1 == null !!!");
		} 
		else 
		{
			//Debug.Log ("Found script 1! Using it!");
			sn1.Target = new Vector3(target.x-2f, target.y, target.z);
			//Debug.Log ("Success!!");
		} 
		
		if (sn2 == null) 
		{
			Debug.Log ("Couldn't get script sn2 == null !!!");
		} 
		else 
		{
			//Debug.Log ("Found script 2! Using it!");
			sn2.Target = new Vector3(target.x+2f, target.y, target.z);
			//Debug.Log ("Success!!");
		} 
		
		if (sn3 == null) 
		{
			Debug.Log ("Couldn't get script sn3 == null !!!");
		} 
		else 
		{
			//Debug.Log ("Found script 3! Using it!");
			sn3.Target = new Vector3(target.x, target.y, target.z+2f);
			//Debug.Log ("Success!!");
		} 
		
		if (sn4 == null) 
		{
			Debug.Log ("Couldn't get script sn4 == null !!!");
		} 
		else 
		{
			//Debug.Log ("Found script 4! Using it!");
			sn4.Target = new Vector3(target.x, target.y, target.z-2f);
			//Debug.Log ("Success!!");
		} 
		
		
	}
	
}

