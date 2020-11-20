using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public enum ScoreEvent
{
	draw,
	mine,
	mineGold,
	gameWin,
	gameLoss
}

public class Prospector : MonoBehaviour
{
	

	#region Fields
	[Header("Game flow management")]
	static public Prospector S;
	static public int SCORE_FROM_PREVIOUS_ROUND = 0;
	static public int HIGH_SCORE = 0;

	public float reloadDelay = 5.0f; //The delay between rounds play sound here

	[Header("Bezier curve management")]
	public Transform fsPosMidObject;
	public Transform fsPosRunObject;
	public Transform fsPosMid2Object;
	public Transform fsPosEndObject;

	public Vector3 fsPosMid;
	public Vector3 fsPosRun;
	public Vector3 fsPosMid2;
	public Vector3 fsPosEnd;

	[Header("Card management")]
	public Deck deck;
	public TextAsset deckXML;
	public Layout layout;
	public TextAsset layoutXML;
	public Vector3 layoutCenter;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Transform layoutAnchor;

	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;
	public List<CardProspector> drawPile;

	[Header("Score management")]
	
	public int chain = 0; 
	public int scoreRun = 0;
	public int score = 0;
	public FloatingScore fsRun;
	public Text GTGameOver;
	public Text GTRoundResult;
	#endregion

	#region Methods
	void Awake()
	{
		S = this; 

		//Check for a high score in PlayerPrefs
		if (PlayerPrefs.HasKey("ProspectorHighScore"))
		{
			HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
		}

		//Add the score from the last round, which will be >0 if it was a win
		score += SCORE_FROM_PREVIOUS_ROUND;

		//And reset the SCORE_FROM_PREVIOUS_ROUND
		SCORE_FROM_PREVIOUS_ROUND = 0;

		//Set up the Texts that show at the end of the round. Set the Text Components
		GameObject go = GameObject.Find("GameOver");
		if (go != null)
		{
			GTGameOver = go.GetComponent<Text>();
		}

		go = GameObject.Find("RoundResult");
		if (go != null)
		{
			GTRoundResult = go.GetComponent<Text>();
		}

		//Make them invisible
		ShowResultsGTs(false);

		go = GameObject.Find("HighScore");
		string hScore = "High score: " + Utils.AddCommasToNumber(HIGH_SCORE);
		go.GetComponent<Text>().text = hScore;
	}

	void ShowResultsGTs(bool show)
	{
		GTGameOver.gameObject.SetActive(show);
		GTRoundResult.gameObject.SetActive(show);
	}

	// Use this for initialization
	void Start()
	{
		Scoreboard.S.score = score;
		deck = GetComponent<Deck>(); //Get the Deck
									 //plays sound everytime the deck is gotten
	

		deck.InitDeck(deckXML.text); //Pass DeckXML to it
		Deck.Shuffle(ref deck.cards); //This shuffles the deck. The ref keyword passes a reference to deck.cards, which allows
		//deck.cards to be modified by Deck.Shuffle()

		layout = GetComponent<Layout>(); //Get the layout
		layout.ReadLayout(layoutXML.text); //Pass LayoutXML to it

		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame();

		
		fsPosMid = fsPosMidObject.position;
		fsPosRun = fsPosRunObject.position;
		fsPosMid2 = fsPosMid2Object.position;
		fsPosEnd = fsPosEndObject.position;
	}
	
	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
	{
		List<CardProspector> lCP = new List<CardProspector>();
		CardProspector tCP;
		foreach (Card tCD in lCD)
		{
			tCP = tCD as CardProspector;
			lCP.Add(tCP);
		}
		return (lCP);
	}

	//The Draw function will pull a single card from the drawPile and return it
	CardProspector Draw()
	{
		CardProspector cd = drawPile[0]; //Pull the 0th CardProspector
		drawPile.RemoveAt(0); //Then remove it from List<> drawPile
		return(cd); //And return it
	}

	//Convert from the layoutID int to the CardProspector with that ID
	CardProspector FindCardByLayoutID (int layoutID)
	{
		foreach (CardProspector tCP in tableau)
		{
			//Search through all cards in the tableau List<>
			if (tCP.layoutID == layoutID)
			{
				//If the card has the same ID, return it
				return tCP;
			}
		}
		//If it'snot found, return null
		return null;
	}

	//LayoutGame() positions the initial tableau of cards, AKA the "mine"
	void LayoutGame()
	{
		//Create an empty GameObject to serve as an anchor for the tableau
		if (layoutAnchor == null)
		{
			GameObject tGO = new GameObject("_LayoutAnchor"); 
			layoutAnchor = tGO.transform; 
			layoutAnchor.transform.position = layoutCenter; 
		}

		CardProspector cp;
		
		foreach (SlotDef tSD in layout.slotDefs) 
		{
			cp = Draw(); //Pull a card from the top (beginning) of the drawPile
			cp.faceUp = tSD.faceUp; 
			cp.transform.parent = layoutAnchor; 

			
			cp.transform.localPosition = new Vector3(
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID);
			
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.tableau;

			
			cp.SetSortingLayerName(tSD.layerName); 
			tableau.Add(cp); 
		}

		
		foreach (CardProspector tCP in tableau)
		{
			foreach (int hid in tCP.slotDef.hiddenBy)
			{
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
		}

		
		MoveToTarget(Draw());

		
		UpdateDrawPile();
	}

	//CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd)
	{
		
		switch (cd.state)
		{
		case CardState.target:
			
			break;
		case CardState.drawpile:
			
			MoveToDiscard(target);
			MoveToTarget(Draw()); 
			UpdateDrawPile(); 
			ScoreManager(ScoreEvent.draw);
			break;
		case CardState.tableau:
			
			bool validMatch = true;
			if (!cd.faceUp)
			{
				
				validMatch = false;
			}
			if (!AdjacentRank(cd, target))
			{
				
				validMatch = false;
			}
			if (!validMatch)
			{
				return; 
			}

			
			tableau.Remove(cd); 
			MoveToTarget(cd); 
			SetTableauFaces(); 
			ScoreManager(ScoreEvent.mine);
			break;
		}

		
		CheckForGameOver();
	}

	
	void MoveToDiscard(CardProspector cd)
	{
		
		cd.state = CardState.discard;
		discardPile.Add(cd); 
		cd.transform.parent = layoutAnchor; 
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.multiplier.y,
			-layout.discardPile.layerID + 0.5f); 
		cd.faceUp = true;

		
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(-100 + discardPile.Count);
	}

	
	void MoveToTarget (CardProspector cd)
	{
		
		if (target != null) MoveToDiscard(target);
		target = cd; 
		cd.state = CardState.target;
		cd.transform.parent = layoutAnchor;

		
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.multiplier.y,
			-layout.discardPile.layerID);
		cd.faceUp = true; 

		
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(0);
	}

	
	void UpdateDrawPile()
	{
		CardProspector cd;

		
		for (int i = 0; i < drawPile.Count; i++)
		{
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;

			
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
				layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
				-layout.drawPile.layerID + 0.1f * i);
			cd.faceUp = false; 
			cd.state = CardState.drawpile;

			
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder(-10 * i);
		}
	}

	
	public bool AdjacentRank(CardProspector c0, CardProspector c1)
	{
		
		if (!c0.faceUp || !c1.faceUp)
		{
			return (false);
		}

		
		if (Mathf.Abs(c0.rank - c1.rank) == 1)
		{
			return (true);
		}

		
		if (c0.rank == 1 && c1.rank == 13)
		{
			return true;
		}

		if (c0.rank == 13 && c1.rank == 1)
		{
			return true;
		}

		
		return false;
	}

	
	void SetTableauFaces()
	{
		foreach (CardProspector cd in tableau)
		{
			bool fup = true; 
			foreach (CardProspector cover in cd.hiddenBy)
			{
				
				if (cover.state == CardState.tableau)
				{
					fup = false; 
				}
			}
			cd.faceUp = fup; 
		}
	}

	//Test whether the game is over
	void CheckForGameOver()
	{
		//If the tableau is empty, the game is over
		if (tableau.Count == 0)
		{
			//Call GameOver() with a win

			//AudioSource.PlayClipAtPoint(AudioClip missedmeagain, Vector3 playPosition, Float volume);
			GameOver(true);
			
			return;
		}

		//If there are still cards in the draw pile, the game's not over
		if (drawPile.Count > 0)
		{
			return;
		}

		//Check for remaining valid plays
		foreach (CardProspector cd in tableau)
		{
			if (AdjacentRank(cd, target))
			{
				//If there's a valid play, the game's not over
				return;
			}
		}

		//Since there are no valid plays, the game is over
		//Call GameOver with a loss
		GameOver(false);
	}

	//Called when the game is over. Simple for now, but expandable
	void GameOver(bool won)
	{
		if (won)
		{
			ScoreManager(ScoreEvent.gameWin);
		}
		else
		{
			ScoreManager(ScoreEvent.gameLoss);
		}

		//Reload the scene in reloadDelay seconds
		//This will give the score a moment to travel
		Invoke("ReloadLevel", reloadDelay);
	}

	void ReloadLevel()
	{
		//Reload trhe scene, resetting the game
		SceneManager.LoadScene("Scene 0");
	}

	//ScoreManager handles all the scoring
	void ScoreManager(ScoreEvent sEvt)
	{
		List<Vector3> fsPts;
		switch (sEvt)
		{
		//Same things need to happen whether it's a draw, a win, or a loss
		case ScoreEvent.draw: //Drawing a card
		case ScoreEvent.gameWin: //Won the round
		case ScoreEvent.gameLoss: //Lost the round
			chain = 0; //resets the score chain
			score += scoreRun; //Add scoreRun to the total score
			scoreRun = 0; //reset scoreRun

			//Add fsRun to the _Scoreboard score
			if (fsRun != null)
			{
				//Create points for the Bezier curve
				fsPts = new List<Vector3>();
				fsPts.Add(fsPosRun);
				fsPts.Add(fsPosMid2);
				fsPts.Add(fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init(fsPts, 0, 1);

				//Also adjust the fontSize
				fsRun.fontSizes = new List<float>(new float[] {28, 36, 4});
				fsRun = null; //Clear fsRun so it's created again
			}
			break;
		case ScoreEvent.mine: //Remove a mine card
			chain++; //Increase the score chain
			scoreRun += chain; //add score for this card to run

			//Create a FloatingScore for this score
			FloatingScore fs;

			//Move it from the mousePosition to fsPosRun
			Vector3 p0 = Input.mousePosition;
			//p0.x /= Screen.width;
			//p0.y /= Screen.height;
			fsPts = new List<Vector3>();
			fsPts.Add(p0);
			fsPts.Add(fsPosMid);
			fsPts.Add(fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore(chain, fsPts);
			fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
			if (fsRun == null)
			{
				fsRun = fs;
				fsRun.reportFinishTo = null;
			}
			else
			{
				fs.reportFinishTo = fsRun.gameObject;
			}
			break;
		}

		//This second switch statement handles round wins and losses
		switch (sEvt)
		{
		case ScoreEvent.gameWin:
			GTGameOver.text = "Round Over";
			//If it's a win, add the score to the next round. static fields are NOT reset by reloading the level
			Prospector.SCORE_FROM_PREVIOUS_ROUND = score;
			//print("You won this round! Round score: " + score);
			GTRoundResult.text = "You won this round! Play another to add to your score!\nRound Score: " + score;
			ShowResultsGTs(true);
			break;
		case ScoreEvent.gameLoss:
			GTGameOver.text = "Game Over";
			//If it's a loss, check against the high score
			if (Prospector.HIGH_SCORE <= score)
			{
				//print("You got the high score! High score: " + score);
				string sRR = "You got the high score!\nHigh score: " + score;
				GTRoundResult.text = sRR;
				Prospector.HIGH_SCORE = score;
				PlayerPrefs.SetInt("ProspectorHighScore", score);
			}
			else
			{
				//print("Your final score for the game was:" + score);
				GTRoundResult.text = "Your final score was: " + score;
			}
			ShowResultsGTs(true);
			break;
		default: 
			//print("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
			break;
		}
	}
	#endregion
}
