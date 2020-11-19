using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Scoreboard : MonoBehaviour
{
	public static Scoreboard S; 

	public GameObject prefabFloatingScore;

	[SerializeField]
	private int _score = 0;
	public string _scoreString;
	public Transform canvas;

	
	public int score
	{
		get
		{
			return(_score);
		}
		set
		{
			_score = value;
			scoreString = Utils.AddCommasToNumber(_score);
		}
	}

	
	public string scoreString
	{
		get
		{
			return (_scoreString);
		}
		set
		{
			_scoreString = value;
			GetComponent<Text>().text = _scoreString;
		}
	}

	void Awake()
	{
		S = this;
	}

	
	public void FSCallback(FloatingScore fs)
	{
		score += fs.score;
	}

	
	public FloatingScore CreateFloatingScore(int amt, List<Vector3> pts)
	{
		GameObject go = Instantiate(prefabFloatingScore) as GameObject;
		go.transform.SetParent(canvas);
		FloatingScore fs = go.GetComponent<FloatingScore>();
		fs.score = amt;
		fs.reportFinishTo = this.gameObject; //Set fs to call back to this
		fs.Init(pts);
		return(fs);
	}
}
