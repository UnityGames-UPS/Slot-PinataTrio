using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using System.Reflection;

public class SlotBehaviour : MonoBehaviour
{
  [Header("Sprites")]
  [SerializeField]
  private Sprite[] myImages;  //images taken initially

  [Header("Slot Images")]
  [SerializeField]
  private List<SlotImage> images;     //class to store total images
  [SerializeField]
  private List<SlotImage> Tempimages;     //class to store the result matrix

  [Header("Slots Transforms")]
  [SerializeField]
  private Transform[] Slot_Transform;

  [Header("Buttons")]
  [SerializeField]
  private Button Spin_Button;
  [SerializeField]
  private Button MaxBet_Button;
  [SerializeField]
  private Button AutoSpin_Button;
  [SerializeField]
  private Button BetPlus_Button;
  [SerializeField]
  private Button BetMinus_Button;
  [SerializeField]
  private Sprite SpinSprite;
  [SerializeField]
  private Sprite StopSprite;

  [Header("Animated Sprites")]
  [SerializeField]
  private Sprite[] Mini_Sprite;
  [SerializeField]
  private Sprite[] Minor_Sprite;
  [SerializeField]
  private Sprite[] Major_Sprite;
  [SerializeField]
  private Sprite[] Mega_Sprite;
  [SerializeField]
  private Sprite[] Grand_Sprite;
  [SerializeField]
  private Sprite[] GreenPinata_Sprite;
  [SerializeField]
  private Sprite[] RedPinata_Sprite;
  [SerializeField]
  private Sprite[] BluePinata_Sprite;
  [SerializeField]
  private Sprite[] YellowBubble_Sprite;
  [SerializeField]
  private Sprite[] PinkBubble_Sprite;

  [Header("Miscellaneous UI")]
  [SerializeField]
  private TMP_Text Balance_text;
  [SerializeField]
  private TMP_Text TotalBet_text;
  [SerializeField]
  private TMP_Text TotalWin_text;
  [SerializeField]
  private GameObject CoinValuePrefab;
  [SerializeField]
  private Transform CoinValueParent;
  private List<GameObject> _coinOverlays = new List<GameObject>();

  [Header("Audio Management")]
  [SerializeField]
  private AudioController audioController;

  [SerializeField]
  private UIManager uiManager;

  private int tweenHeight = 0;

  [Header("Animation List")]
  [SerializeField]
  private List<ImageAnimation> TempList;

  [SerializeField]
  private SocketIOManager SocketManager;

  private List<Tweener> alltweens = new List<Tweener>();
  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine;
  private Tween BalanceTween;
  internal bool IsAutoSpin = false;
  private bool IsSpinning = false;
  private bool StopSpinToggle = false;
  private bool CheckSpinAudio = false;
  internal bool CheckPopups = false;
  internal int BetCounter = 0;
  private double currentBalance = 0;
  private double currentTotalBet = 0;
  [SerializeField]
  private int IconSizeFactor = 160;
  private int numberOfSlots = 5;
  private float SpinDelay = 0.2f;
  [SerializeField] private float minSpinDuration = 2f;
  [SerializeField] private float stopButtonWindow = 0.5f;
  [SerializeField] private float naturalStopStagger = 0.2f;
  [SerializeField] private float stopButtonStagger = 0.05f;
  private float spinStartTime;
  internal bool socketConnected = false;
  private int[,] initialMatrix = new int[,]
{
  { 0, 0, 7, 0, 0 },  // row 0 (top):    grand, mega, major, minor, mini
  { 0, 6, 0, 5, 0 },  // row 1 (middle): yellow bubbles
  { 3, 0, 0, 0, 4 }   // row 2 (bottom): yellow bubbles
};

  private void Start()
  {
    IsAutoSpin = false;

  if (Spin_Button) Spin_Button.onClick.RemoveAllListeners();
  if (Spin_Button) Spin_Button.onClick.AddListener(OnSpinButtonPressed);

  if (BetPlus_Button) BetPlus_Button.onClick.RemoveAllListeners();
  if (BetPlus_Button) BetPlus_Button.onClick.AddListener(() => ChangeBet(true));

  if (BetMinus_Button) BetMinus_Button.onClick.RemoveAllListeners();
  if (BetMinus_Button) BetMinus_Button.onClick.AddListener(() => ChangeBet(false));

  if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
  if (MaxBet_Button) MaxBet_Button.onClick.AddListener(() => MaxBet());

  if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
  if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

  tweenHeight = (15 * IconSizeFactor) - 280;
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      IsAutoSpin = true;
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
    }
  }

  private void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      IsAutoSpin = false;
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      yield return new WaitUntil(() => !CheckPopups);
      StartSlots();
      yield return new WaitUntil(() => !IsSpinning);
      yield return new WaitForSeconds(SpinDelay);
    }
    yield return new WaitUntil(() => !IsSpinning);
    ToggleButtonGrp(true);
    if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
  }
  #endregion

  private void OnSpinButtonPressed()
  {
    if (IsSpinning)
      OnStopSpinPressed();
    else
      StartSlots();
  }

  private void OnStopSpinPressed()
  {
    StopSpinToggle = true;
  }

  private void MaxBet()
  {
    if (uiManager.BetCount == 0) return;
    if (audioController) audioController.PlayButtonAudio();
    BetCounter = uiManager.BetCount - 1;
    currentTotalBet = uiManager.GetBetAmount(BetCounter);
    uiManager.SetBet(BetCounter);
  }

  private void ChangeBet(bool IncDec)
  {
    if (uiManager.BetCount == 0) return;
    if (audioController) audioController.PlayButtonAudio();
    if (IncDec)
    {
      BetCounter++;
      if (BetCounter >= uiManager.BetCount)
      {
        BetCounter = 0;
      }
    }
    else
    {
      BetCounter--;
      if (BetCounter < 0)
      {
        BetCounter = uiManager.BetCount - 1;
      }
    }
    currentTotalBet = uiManager.GetBetAmount(BetCounter);
    uiManager.SetBet(BetCounter);
  }

  #region InitialFunctions
  // internal void shuffleInitialMatrix()
  // {
  //   for (int i = 0; i < Tempimages.Count; i++)
  //   {
  //     for (int j = 0; j < 3; j++)
  //     {
  //       int randomIndex = UnityEngine.Random.Range(0, 14);
  //       Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
  //     }
  //   }
  // }


  internal void InitializeMatrix()
  {
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int val = initialMatrix[row, col];
        Tempimages[col].slotImages[row].sprite = myImages[val];

        ImageAnimation animScript = Tempimages[col].slotImages[row]
          .GetComponent<ImageAnimation>();
        if (animScript != null)
        {
          PopulateAnimationSprites(animScript, val);
          if (animScript.textureArray.Count > 0 && val >= 3)
          {
            animScript.StartAnimation();
            TempList.Add(animScript);
          }
        }
      }
    }
  }

  internal void SetInitialUI()
  {
    BetCounter = 0;
    uiManager.InitialiseUI(SocketManager.InitialData.bets, SocketManager.UIData.paylines.symbols);
    if (TotalBet_text)
      TotalBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalWin_text) TotalWin_text.text = "0.000";
    if (Balance_text)
      Balance_text.text = SocketManager.PlayerData.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;
    currentTotalBet = SocketManager.InitialData.bets[BetCounter];
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
    audioController.CheckFocusFunction(focus, CheckSpinAudio);
  }

  #region AnimationSprites
  private void PopulateAnimationSprites(ImageAnimation animScript, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    switch (val)
    {
      case 0:
        foreach (var s in YellowBubble_Sprite) animScript.textureArray.Add(s);
        break;
      case 1:
        foreach (var s in PinkBubble_Sprite) animScript.textureArray.Add(s);
        break;
      case 3:
        foreach (var s in Mini_Sprite) animScript.textureArray.Add(s);
        break;
      case 4:
        foreach (var s in Minor_Sprite) animScript.textureArray.Add(s);
        break;
      case 5:
        foreach (var s in Major_Sprite) animScript.textureArray.Add(s);
        break;
      case 6:
        foreach (var s in Mega_Sprite) animScript.textureArray.Add(s);
        break;
      case 7:
        foreach (var s in Grand_Sprite) animScript.textureArray.Add(s);
        break;
      case 8:
        foreach (var s in GreenPinata_Sprite) animScript.textureArray.Add(s);
        break;
      case 9:
        foreach (var s in RedPinata_Sprite) animScript.textureArray.Add(s);
        break;
      case 10:
        foreach (var s in BluePinata_Sprite) animScript.textureArray.Add(s);
        break;
      default:
        break;
    }
  }
  #endregion

  #region SlotSpin
  private void StartSlots()
  {
    if (TotalWin_text) TotalWin_text.text = "0.000";
    if (TempList.Count > 0)
    {
      StopGameAnimation();
    }
    uiManager.ShowTicker();
    tweenroutine = StartCoroutine(TweenRoutine());
  }

  private void BalanceDeduction()
  {
    double initAmount = currentBalance;
    double targetAmount = currentBalance - currentTotalBet;
    BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, targetAmount, 0.8f).OnUpdate(() =>
    {
      if (Balance_text) Balance_text.text = initAmount.ToString("F3");
    });
  }

  private IEnumerator TweenRoutine()
  {
    // if (currentBalance < currentTotalBet)
    // {
    //   uiManager.LowBalPopup();
    //   yield return new WaitForSeconds(1);
    //   ToggleButtonGrp(true);
    //   yield break;
    // }
    ClearCoinOverlays();
    IsSpinning = true;
    spinStartTime = Time.time;
    Spin_Button.GetComponent<Image>().sprite = StopSprite;
    ToggleButtonGrp(false);
    for (int i = 0; i < numberOfSlots; i++)
    {
      InitializeTweening(Slot_Transform[i]);
      yield return new WaitForSeconds(0.1f);
    }
    BalanceDeduction();
    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone && Time.time - spinStartTime >= minSpinDuration);
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int resultNum = int.Parse(SocketManager.ResultData.matrix[row][col]);
        PopulateAnimationSprites(Tempimages[col].slotImages[row].GetComponent<ImageAnimation>(), resultNum);
        Tempimages[col].slotImages[row].sprite = myImages[resultNum];
      }
    }
    float stopWindowEnd = Time.time + stopButtonWindow;
    while (Time.time < stopWindowEnd)
    {
      if (StopSpinToggle) break;
      yield return null;
    }
    float stagger = StopSpinToggle ? stopButtonStagger : naturalStopStagger;
    for (int i = 0; i < numberOfSlots; i++)
    {
      yield return StopTweening(Slot_Transform[i], i, stagger);
    }
    StopSpinToggle = false;
    yield return alltweens[^1].WaitForCompletion();
    KillAllTweens();
    CheckForFeaturesAnimation();
    SpawnCoinOverlays();
    if (TotalWin_text) TotalWin_text.text = SocketManager.ResultData.payload.winAmount.ToString("F3");
    BalanceTween?.Kill();
    if (Balance_text) Balance_text.text = SocketManager.ResultData.player.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;
    SpinDelay = SocketManager.ResultData.payload.winAmount > 0 ? 1.2f : 0.2f;
    CheckPopups = false;
    Spin_Button.GetComponent<Image>().sprite = SpinSprite;
    IsSpinning = false;
    ToggleButtonGrp(true);
  }
  #endregion

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }


  private void ToggleButtonGrp(bool toggle)
  {
    if (MaxBet_Button) MaxBet_Button.interactable = toggle;
    if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
    if (BetMinus_Button) BetMinus_Button.interactable = toggle;
    if (BetPlus_Button) BetPlus_Button.interactable = toggle;
  }

  #region GameAnimation
  private void CheckForFeaturesAnimation()
  {
    var coinPositions = SocketManager.ResultData.payload?.coinWins;

    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int val = int.Parse(SocketManager.ResultData.matrix[row][col]);

        bool isJackpotOrPinata = val >= 3 && val <= 10;
        bool hasCoinValue = coinPositions != null &&
          coinPositions.Exists(c => c.position[0] == row && c.position[1] == col);

        if (!isJackpotOrPinata && !hasCoinValue) continue;

        ImageAnimation animScript = Tempimages[col].slotImages[row]
          .GetComponent<ImageAnimation>();
        if (animScript != null && animScript.textureArray.Count > 0)
        {
          animScript.StartAnimation();
          animScript.transform.SetAsLastSibling();
          TempList.Add(animScript);
        }
      }
    }
  }

  private void StopGameAnimation()
  {
    for (int i = 0; i < TempList.Count; i++)
    {
      TempList[i].StopAnimation();
    }
    TempList.Clear();
    TempList.TrimExcess();
  }

  private void ClearCoinOverlays()
  {
    foreach (GameObject go in _coinOverlays)
      Destroy(go);
    _coinOverlays.Clear();
  }

  private void SpawnCoinOverlays()
  {
    var coins = SocketManager.ResultData.payload.coinWins;
    if (coins == null || CoinValuePrefab == null || CoinValueParent == null) return;
    foreach (var coin in coins)
    {
      int row = coin.position[0];
      int col = coin.position[1];
      float x = -320f + col * 160f;
      float y = 160f - row * 160f;
      GameObject instance = Instantiate(CoinValuePrefab, CoinValueParent);
      instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
      instance.GetComponentInChildren<TMP_Text>().text = coin.value.ToString("F2");
      _coinOverlays.Add(instance);
    }
  }
  #endregion


  #region TweeningCode
  private void InitializeTweening(Transform slotTransform)
  {
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
    Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
    tweener.Play();
    alltweens.Add(tweener);
  }



  private IEnumerator StopTweening(Transform slotTransform, int index, float stagger)
  {
    alltweens[index].Kill();
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, IconSizeFactor);
    alltweens[index] = slotTransform.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutElastic);
    yield return new WaitForSeconds(stagger);
  }


  private void KillAllTweens()
  {
    for (int i = 0; i < numberOfSlots; i++)
    {
      alltweens[i].Kill();
    }
    alltweens.Clear();

  }
  #endregion

}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}

