using UnityEngine;
using System.Collections;

public class ProspectorStethoscope : MonoBehaviour
{
	public Vector3 mousePosition;
	
	void Start()
	{
	
	}
	
	
	void Update()
	{
		mousePosition = Input.mousePosition;
	}
}
