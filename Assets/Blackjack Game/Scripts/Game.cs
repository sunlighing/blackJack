﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Blackjack game logic class,all key logic put here.
/// include chips management,game rule, poker deal,etc.
/// </summary>
public class Game : RefSingleton<Game> {

    const int DEFAULT_MONEY = 5000;

    enum RESULT 
    {
        NONE,
        BANKER_WIN,
        PLAYER_WIN,
    }

    int money;
    public int Money
    {
        get
        {
            return money;
        }
        set
        {
            if (money != value)
            {
                money = value;
                PlayerPrefs.SetInt("Money", value);
                PlayerPrefs.Save();
                chipsPanel.RefreshChips();
                totalMonty.text = money.ToString();
            }
        }
    }

    public int chips;

    int lastChips = 0;

    public Text totalMonty;
    public Button btnCancel;
    public Button btnDeal;

    public UIChipsPanel chipsPanel;
    public UIButtonsPanel buttonsPanel;

    public Dealer dealer;

    public CardsSlot bankerSlot;
    public CardsSlot playerSlot;


    public CanvasGroup playerScorePanel;
    public Text playerScore;

    public CanvasGroup bankerScorePanel;
    public Text bankerScore;


    public Text winMoney;
    public GameObject winPanel;
    public GameObject winBJ;
    public GameObject rewardPanel;
    public Text rewardMoney;

    public Toggle musicSwitch;

	// Use this for initialization
	void Start () {
        this.InitGame();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Exit this game.
    /// </summary>
    public void Exit()
    {
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Inits the game.
    /// </summary>
    void InitGame()
    {
        PlayerPrefs.SetInt("Money", DEFAULT_MONEY);
        PlayerPrefs.Save();

        this.Money = PlayerPrefs.GetInt("Money", DEFAULT_MONEY);
        this.totalMonty.text = this.money.ToString();

        Chip.Instance.gameObject.SetActive(false);
        this.ShowDealButtons(false);

        this.winPanel.SetActive(false);
        this.rewardPanel.SetActive(false);
    }

    /// <summary>
    /// Adds the chips.
    /// </summary>
    /// <param name="chip">Chip.</param>
    public void AddChips(int chip)
    {
        this.chips += chip;
        this.Money -= chip;
        this.ShowDealButtons(true);
    }

    /// <summary>
    /// Clears the chips.
    /// </summary>
    public void ClearChips()
    {
        SoundManager.Instance.PlayButton();
        this.Money += this.chips;
        this.chips = 0;
        Chip.Instance.Clear();
        this.ShowDealButtons(false);
    }

    /// <summary>
    /// Shows the deal buttons.
    /// </summary>
    /// <param name="show">If set to <c>true</c> show.</param>
    public void ShowDealButtons(bool show)
    {
        this.btnDeal.gameObject.SetActive(show);
        this.btnCancel.gameObject.SetActive(show);
    }

    /// <summary>
    /// start game.
    /// </summary>
    public void GameStart()
    {
        SoundManager.Instance.PlayButton();
        this.ShowDealButtons(false);
        this.chipsPanel.HidePanel();
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// main game loop.
    /// </summary>
    /// <returns>The loop.</returns>
    IEnumerator GameLoop()
    {
        Chip.Instance.FlyToSide();
        yield return new WaitForSeconds(1f);

        Game.Instance.playerSlot.AddCard(dealer.Deal());
        yield return new WaitForSeconds(0.5f);
        Game.Instance.playerSlot.AddCard(dealer.Deal());
        yield return new WaitForSeconds(0.5f);

        Game.Instance.bankerSlot.AddCard(dealer.Deal());
        yield return new WaitForSeconds(0.5f);
        Game.Instance.bankerSlot.AddCard(dealer.Deal(), false);
        yield return new WaitForSeconds(0.5f);

        if(!this.CheckResult())
        {
            buttonsPanel.ShowButtons();
        }
    }

    /// <summary>
    /// Checks game result.
    /// </summary>
    /// <returns><c>true</c>, if result was checked, <c>false</c> otherwise.</returns>
    /// <param name="final">If set to <c>true</c> final.</param>
    bool CheckResult(bool final= false)
    {
        int bankerPoint = bankerSlot.Point;
        int playerPoint = playerSlot.Point;

        this.ShowScore(2, true, playerPoint);


        if (bankerPoint == playerPoint && bankerPoint == 21)
        {
            ShowResult(RESULT.NONE);
            //TIE
            return true;
        }

        if (bankerPoint == 21)
        {
            ShowResult(RESULT.BANKER_WIN, bankerPoint);
            return true;
        }

        if(playerPoint == 21)
        {
            ShowResult(RESULT.PLAYER_WIN, playerPoint);
            return true;
        }

        if (bankerPoint > 21 && playerPoint > 21)
        {
            ShowResult(RESULT.BANKER_WIN, bankerPoint);
            return true;
        }

        if(bankerPoint>21)
        {
            ShowResult(RESULT.PLAYER_WIN, playerPoint);
            return true;
        }

        if (playerPoint > 21)
        {
            ShowResult(RESULT.BANKER_WIN, bankerPoint);
            return true;
        }

        if(final)
        {
            if (bankerPoint == playerPoint)
            {
                ShowResult(RESULT.NONE);
                //TIE
                return true;
            }
            if(bankerPoint > playerPoint)
            {
                ShowResult(RESULT.BANKER_WIN, bankerPoint);
            }
            else
            {
                ShowResult(RESULT.PLAYER_WIN, playerPoint);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Shows game result.
    /// </summary>
    /// <param name="result">Result.</param>
    /// <param name="point">Point.</param>
    void ShowResult(RESULT result, int point=0)
    {
        Debug.LogFormat("{0},{1}", result, point);
        StartCoroutine(ResultProcess(result, point));

    }

    /// <summary>
    /// Result process.
    /// </summary>
    /// <param name="result">Result.</param>
    /// <param name="point">Point.</param>
    IEnumerator ResultProcess(RESULT result,int point)
    {
        bankerSlot.FlipAll();
        this.ShowScore(1, true, bankerSlot.Point);
        this.ShowScore(2, true, playerSlot.Point);

        buttonsPanel.HideButtons();
        yield return new WaitForSeconds(1f);

        if (result == RESULT.BANKER_WIN)
        {
            SoundManager.Instance.PlayFailed();
            Chip.Instance.FlyToBanker();
        }
        else
        {
            Chip.Instance.FlyToPlayer();
            int addMoney = 0;

            if (result == RESULT.PLAYER_WIN)
            {
                SoundManager.Instance.PlayWin();
                if (point == 21)
                {
                    addMoney = (int)(this.chips * 1.5);
                }
                else
                    addMoney = this.chips;
            }
            if (addMoney > 0)
            {
                this.ShowWinChips(addMoney, point == 21);
                this.Money += (this.chips + addMoney);
                yield return new WaitForSeconds(2f);
            }
            if (result == RESULT.NONE)
                this.Money += this.chips;
        }

        this.lastChips = this.chips;
        this.chips = 0;
        if (this.lastChips > this.Money)
        {
            this.lastChips = 0;
        }
        playerSlot.RemoveAll();
        bankerSlot.RemoveAll();
        chipsPanel.ShowPanel();
        ShowScore(0, false, 0);
        if (this.Money == 0)
        {
            yield return new WaitForSeconds(2f);
            this.ShowRewards(500);
            yield return new WaitForSeconds(2f);
        }
        if(this.lastChips >0)
        {
            yield return new WaitForSeconds(1f);
            Chip.Instance.ShowChips(null, new Vector3(640, 0, 0));
            this.AddChips(this.lastChips);
        }
        yield return null;
    }

    /// <summary>
    /// Show  win chips.
    /// </summary>
    /// <param name="chip">Chip.</param>
    /// <param name="blackjack">If set to <c>true</c> blackjack.</param>
    void ShowWinChips(int chip,bool blackjack)
    {
        this.winMoney.text = "+" + chip.ToString();
        this.winPanel.transform.localScale = Vector3.zero;
        this.winPanel.SetActive(true);
        this.winBJ.SetActive(blackjack);
        LeanTween.scale(this.winPanel, Vector3.one, 0.2f).setEase(LeanTweenType.easeInBack).setOnComplete(()=>{
            LeanTween.delayedCall(this.gameObject, 1f, () => {
                LeanTween.scale(this.winPanel, Vector3.zero, 0.2f).setEase(LeanTweenType.easeInBack);
            });
        });

    }

    /// <summary>
    /// Show rewards.
    /// </summary>
    /// <param name="money">Money.</param>
    void ShowRewards(int money)
    {
        this.rewardMoney.text = money.ToString();
        this.rewardPanel.transform.localScale = Vector3.zero;
        this.rewardPanel.SetActive(true);
        LeanTween.scale(this.rewardPanel, Vector3.one, 0.2f).setEase(LeanTweenType.easeInBack).setOnComplete(() => {
            LeanTween.delayedCall(this.gameObject, 1f, () => {
                LeanTween.scale(this.rewardPanel, Vector3.zero, 0.2f).setEase(LeanTweenType.easeInBack);
                this.Money += money;
            });
        });

    }

    /// <summary>
    /// On Hit button clicked
    /// </summary>
    public void Hit()
    {
        SoundManager.Instance.PlayButton();
        StartCoroutine(HitProcess());
    }

    /// <summary>
    /// ON Stand button clicked
    /// </summary>
    public void Stand()
    {
        SoundManager.Instance.PlayButton();
        StartCoroutine(StandProcess());
    }

    /// <summary>
    /// Stand process.
    /// </summary>
    /// <returns>The process.</returns>
    IEnumerator StandProcess()
    {
        yield return BankerProcess();
        this.Final();
    }

    /// <summary>
    /// ON Double button clicked
    /// </summary>
    public void Double()
    {
        
    }

    /// <summary>
    /// Oo Split button clicked
    /// </summary>
    public void Split()
    {
        
    }

    /// <summary>
    /// Hit process.
    /// </summary>
    /// <returns>The process.</returns>
    IEnumerator HitProcess()
    {
        Game.Instance.playerSlot.AddCard(dealer.Deal());
        yield return new WaitForSeconds(0.5f);

        int point = playerSlot.Point;
        this.ShowScore(2, true, point);
        if (point >= 21)
        {//player end
            yield return BankerProcess();
            this.Final();
        }
        else
            this.CheckResult();
    }

    /// <summary>
    /// Banker process.
    /// </summary>
    /// <returns>The process.</returns>
    IEnumerator BankerProcess()
    {
        bankerSlot.FlipAll();
        int point = bankerSlot.Point;
        this.ShowScore(1, true, point);
        int playerPoint = playerSlot.Point;
        if (point < 21 && point <= playerPoint && playerPoint <= 21)
        {
            float prob = 0;
            if (point <= 16)
            {
                prob = 1;
            }
            else if (point < 18)
            {
                prob = 0.7f;
            }
            else if (point < 20)
            {
                prob = 0.05f;
            }

            float rnd = Random.Range(0f, 1f);
            Debug.LogFormat("Point: {0} : Rnd:{1} < {2}", point, rnd, prob);
            if (rnd < prob)
            {
                yield return BankerHit();
                yield return BankerProcess();
            }
        }
    }

    /// <summary>
    /// Banker on hit
    /// </summary>
    /// <returns>The hit.</returns>
    IEnumerator BankerHit()
    {
        Game.Instance.bankerSlot.AddCard(dealer.Deal());
        yield return new WaitForSeconds(0.5f);

    }

    /// <summary>
    /// Final result check
    /// </summary>
    void Final()
    {
        this.CheckResult(true);
    }

    /// <summary>
    /// Show the score.
    /// </summary>
    /// <param name="side">Side.</param>
    /// <param name="show">If set to <c>true</c> show.</param>
    /// <param name="score">Score.</param>
    void ShowScore(int side,bool show, int score)
    {
        if (side == 1 && show)
            this.bankerScore.text = score.ToString();
        if (side == 2 && show)
            this.playerScore.text = score.ToString();
        if(side == 0 || side == 1)
        {
            if(!this.bankerScorePanel.alpha.Equals(show ? 1f:0f))
            {
                LeanTween.alphaCanvas(this.bankerScorePanel, show ? 1f : 0f, 0.2f);
            }
        }
        if (side == 0 || side == 2)
        {
            if (!this.playerScorePanel.alpha.Equals(show ? 1f : 0f))
            {
                LeanTween.alphaCanvas(this.playerScorePanel, show ? 1f : 0f, 0.2f);
            }
        }
    }

    public void RecycleCard(Card card)
    {
        this.dealer.Recycle(card);
    }
}
