using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonDontDestroy<Game>
{
	protected override void Awake()
	{
		base.Awake();
		if (!(SingletonDontDestroy<GameManager>.Instance != this))
		{
			Application.runInBackground = true;
		}
	}

	private void Start()
	{

		StartCoroutine(Init_IE());
	}

	private void Update()
	{
		
	}

	private IEnumerator Init_IE()
	{
		InitializeGame();
	
		yield return null;
		StartApp();
	}

	private void StartApp()
    {

    }

	private void InitializeGame()
    {
		NetworkManager.Connect("8.134.190.12", 8888);
    }
}
