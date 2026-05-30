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
  private List<SlotImage> images;
  [SerializeField]
  private List<SlotImage> Tempimages;
  [SerializeField]
  private List<SlotImage> Animimages;

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

  [Header("Pinata Fly Targets")]
  [SerializeField] private RectTransform GreenPinataTarget;
  [SerializeField] private RectTransform RedPinataTarget;
  [SerializeField] private RectTransform BluePinataTarget;
  [SerializeField] private float pinataFlyDuration = 0.8f;
  [SerializeField] private float pinataPeakScale = 1.8f;

  [Header("Audio Management")]
  [SerializeField]
  private AudioManager audioManager;

  [SerializeField]
  private UIManager uiManager;

  private int tweenHeight = 0;

  [Header("Animation List")]
  [SerializeField]
  private List<ImageAnimation> TempList;

  [SerializeField]
  private SocketIOManager SocketManager;

  [SerializeField] private BonusController bonusController;
  [SerializeField] private LinkBonusController linkBonusController;
  [SerializeField] private float linkBonusPreSpinGlowDuration = 0.5f;

  private List<Tweener> alltweens = new List<Tweener>();
  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine;
  private Tween BalanceTween;
  internal bool IsAutoSpin = false;
  private bool _isFeatureActive = false;
  private bool IsSpinning = false;
  private bool StopSpinToggle = false;
  private bool CheckSpinAudio = false;
  internal bool CheckPopups = false;
  private bool _isInFreeSpin = false;
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
  private int _activePinataAnims = 0;
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
      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
    }
    else
    {
      StopAutoSpin();
    }
  }

  private void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      IsAutoSpin = false;
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      Debug.Log($"[AutoSpin] Loop start — IsAutoSpin:{IsAutoSpin} CheckPopups:{CheckPopups} IsSpinning:{IsSpinning}");
      yield return new WaitUntil(() => !CheckPopups);
      Debug.Log("[AutoSpin] CheckPopups cleared, calling StartSlots");
      StartSlots();
      yield return new WaitUntil(() => !IsSpinning);
      Debug.Log($"[AutoSpin] Spin complete — SpinDelay:{SpinDelay} IsAutoSpin:{IsAutoSpin}");
      yield return new WaitForSeconds(SpinDelay);
    }
    Debug.Log("[AutoSpin] Exited while loop");
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
    if (audioManager) audioManager.PlayButton();
    BetCounter = uiManager.BetCount - 1;
    currentTotalBet = uiManager.GetBetAmount(BetCounter);
    uiManager.SetBet(BetCounter);
  }

  private void ChangeBet(bool IncDec)
  {
    if (uiManager.BetCount == 0) return;
    if (audioManager) audioManager.PlayButton();
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

        ImageAnimation animScript = Animimages[col].slotImages[row]
          .GetComponent<ImageAnimation>();
        if (animScript != null)
        {
          PopulateAnimationSprites(animScript, val);
          if (animScript.textureArray.Count > 0 && val >= 3)
          {
            Animimages[col].slotImages[row].gameObject.SetActive(true);
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
    if (_isInFreeSpin) return;
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
    _activePinataAnims = 0;
    IsSpinning = true;
    if (audioManager) audioManager.PlaySpinLoop();
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

    if (SocketManager.ResultData?.payload == null)
    {
      KillAllTweens();
      Spin_Button.GetComponent<Image>().sprite = SpinSprite;
      CheckPopups = false;
      IsSpinning = false;
      ToggleButtonGrp(true);
      yield break;
    }

    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int resultNum = int.Parse(SocketManager.ResultData.matrix[row][col]);
        Tempimages[col].slotImages[row].sprite = myImages[resultNum];
        ImageAnimation animScript = Animimages[col].slotImages[row].GetComponent<ImageAnimation>();
        if (animScript != null) PopulateAnimationSprites(animScript, resultNum);
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
    if (audioManager) audioManager.StopSpinLoop();
    yield return alltweens[^1].WaitForCompletion();
    Spin_Button.GetComponent<Image>().sprite = SpinSprite;
    yield return new WaitUntil(() => _activePinataAnims == 0);
    KillAllTweens();
    CheckForFeaturesAnimation();
    if (TotalWin_text) TotalWin_text.text = SocketManager.ResultData.payload.winAmount.ToString("F3");
    BalanceTween?.Kill();
    if (Balance_text) Balance_text.text = SocketManager.ResultData.player.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;
    SpinDelay = SocketManager.ResultData.payload.winAmount > 0 ? 1.2f : 0.2f;

    var pendingFeatures = SocketManager.ResultData.payload?.pendingFeatures;
    bool hadPendingFeature = pendingFeatures != null && pendingFeatures.Exists(f => f.triggered);
    if (hadPendingFeature)
      yield return StartCoroutine(HandlePendingFeatures(pendingFeatures));

    if (!_isInFreeSpin && !_isFeatureActive && !hadPendingFeature)
      yield return StartCoroutine(CheckAndShowJackpotWin());

    CheckPopups = false;
    IsSpinning = false;
    ToggleButtonGrp(true);
  }
  #endregion

  #region PendingFeatures
  private IEnumerator HandlePendingFeatures(List<PendingFeature> features)
  {
    bool wasAutoSpinning = IsAutoSpin;
    StopAutoSpin();
    foreach (var feature in features)
    {
      if (!feature.triggered) continue;
      uiManager.SetReelFrame(feature.feature);
      switch (feature.feature)
      {
        case "pickJackpot":
          yield return StartCoroutine(HandleRedPinataPick());
          break;
        case "wheelBonus":
          yield return StartCoroutine(HandleWheelBonus(feature));
          break;
        case "linkBonus":
          yield return StartCoroutine(HandleLinkBonus(feature));
          break;
      }
      uiManager.SetReelFrame("default");
    }
    if (wasAutoSpinning) AutoSpin();
  }

  private IEnumerator HandleWheelBonus(PendingFeature feature)
  {
    _isFeatureActive = true;
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);
    CheckPopups = true;
    uiManager.SetupFeaturePinata("wheelBonus");
    StartCoroutine(uiManager.SlideContentDown());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("wheelBonus"));
    if (audioManager) audioManager.PlayBonusBgMusic();

    SocketManager.SendWheelBonus();
    yield return new WaitUntil(() => SocketManager.isWheelBonusDone);
    SocketManager.isWheelBonusDone = false;

    var wbFeature = SocketManager.ResultData.payload?.triggeredFeatures
      ?.Find(f => f.feature == "wheelBonus");
    double awardValue = wbFeature?.awardValue ?? 0;
    List<string> spinHistory = wbFeature?.spinHistory ?? new List<string> { wbFeature?.jackpotTier ?? "mini" };

    if (bonusController != null) bonusController.StartWheelBonus(spinHistory, awardValue);
    yield return new WaitUntil(() => bonusController == null || bonusController.isBonusDone);

    yield return StartCoroutine(uiManager.SlideContentUp());

    string wheelTier = wbFeature?.jackpotTier ?? "mini";
    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(wheelTier, awardValue, awardValue));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("wheelBonus");
    CheckPopups = false;
    _isFeatureActive = false;
    uiManager.LockFeatureUI(false);
  }

  private IEnumerator SetupRedPinataAfterSlide()
  {
    yield return StartCoroutine(uiManager.SlideContentDown());
    uiManager.SetupFeaturePinata("pickJackpot");
  }

  private IEnumerator HandleRedPinataPick()
  {
    _isFeatureActive = true;
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);
    CheckPopups = true;
    StartCoroutine(SetupRedPinataAfterSlide());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("pickJackpot"));
    if (audioManager) audioManager.PlayBonusBgMusic();
    uiManager.ShowPickJackpotScreen();

    yield return new WaitUntil(() => uiManager.PickJackpotSelected);

    SocketManager.SendPickJackpot();
    uiManager.PickJackpotSelected = false;

    yield return new WaitUntil(() => SocketManager.isPickJackpotDone);
    SocketManager.isPickJackpotDone = false;

    var pjFeature = SocketManager.ResultData.payload?.triggeredFeatures
      ?.Find(f => f.feature == "pickJackpot");
    string goalJackpot = pjFeature?.goalJackpot;
    double awardValue = pjFeature?.awardValue ?? 0;

    yield return StartCoroutine(uiManager.RevealJackpot(goalJackpot));

    _isInFreeSpin = true;
    yield return StartCoroutine(FreeSpinLoop());
    _isInFreeSpin = false;

    uiManager.PlayBustedPinataOnce("red");
    double freeSpinTotalWin = SocketManager.ResultData.payload?.winAmount ?? 0;
    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(goalJackpot, awardValue, freeSpinTotalWin));
    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("pickJackpot");
    _isFeatureActive = false;
    uiManager.LockFeatureUI(false);
  }

  private IEnumerator FreeSpinLoop()
  {
    while (true)
    {
      yield return new WaitForSeconds(SpinDelay);
      StartSlots();
      yield return new WaitUntil(() => !IsSpinning);

      if (!SocketManager.ResultData.payload.isFreeSpinActive)
        break;
    }
  }

  private IEnumerator CheckAndShowJackpotWin()
  {
    var waysWins = SocketManager.ResultData.payload?.waysWins;
    if (waysWins == null) yield break;

    var jackpotWin = waysWins.Find(w => w.symbolId >= 3 && w.symbolId <= 7);
    if (jackpotWin == null) yield break;

    string tier = jackpotWin.symbolName;
    double jackpotAmount = jackpotWin.jackpotPayout;
    double totalWin = SocketManager.ResultData.payload.winAmount;

    CheckPopups = true;
    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(tier, jackpotAmount, totalWin));
    CheckPopups = false;
  }

  private IEnumerator HandleLinkBonus(PendingFeature feature)
  {
    yield return StartCoroutine(uiManager.PlayFeatureIntro("linkBonus"));
    if (audioManager) audioManager.PlayBonusBgMusic();
    _isFeatureActive = true;
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);
    CheckPopups = true;

    var targetZones = SocketManager.ResultData.payload?.linkBonusTargetZones;
    int initialSpins = SocketManager.ResultData.payload?.freeSpinsRemaining ?? 3;

    uiManager.SetupFeaturePinata("linkBonus");
    uiManager.UpdateLinkBonusSpinsRemaining(initialSpins);

    yield return StartCoroutine(linkBonusController.StartLinkBonus(targetZones));

    _isInFreeSpin = true;
    yield return StartCoroutine(LinkBonusFreeSpinLoop());
    _isInFreeSpin = false;

    var lbFeature = SocketManager.ResultData.payload?.triggeredFeatures?.Find(f => f.feature == "linkBonus");
    double awardValue = lbFeature?.awardValue ?? 0;
    var allLockedCells = lbFeature?.lockedCells ?? SocketManager.ResultData.payload?.linkBonusLockedCells;

    yield return StartCoroutine(linkBonusController.PlayTotalWinSequence(allLockedCells, awardValue));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("linkBonus");
    linkBonusController.ResetAll();

    BalanceTween?.Kill();
    if (Balance_text) Balance_text.text = SocketManager.ResultData.player.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;

    CheckPopups = false;
    _isFeatureActive = false;
    uiManager.LockFeatureUI(false);
  }

  private IEnumerator LinkBonusFreeSpinLoop()
  {
    while (true)
    {
      yield return new WaitForSeconds(SpinDelay);
      yield return StartCoroutine(LinkBonusSpinRound());
      if (!(SocketManager.ResultData.payload?.isBluePinataLinkBonus ?? false)) break;
    }
  }

  private IEnumerator LinkBonusSpinRound()
  {
    uiManager.PlayBustedPinataOnce("blue");
    linkBonusController.ShowPreSpinGlow();
    yield return new WaitForSeconds(linkBonusPreSpinGlowDuration);
    linkBonusController.HidePreSpinGlow();

    linkBonusController.StartSpinRound();

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);

    var matrix = SocketManager.ResultData.matrix;
    var lockedCells = SocketManager.ResultData.payload?.linkBonusLockedCells;
    int spinsRemaining = SocketManager.ResultData.payload?.freeSpinsRemaining ?? 0;

    yield return StartCoroutine(linkBonusController.StopCellsSequential(matrix, lockedCells));
    linkBonusController.UpdateLockedCells(lockedCells);
    uiManager.UpdateLinkBonusSpinsRemaining(spinsRemaining);

    BalanceTween?.Kill();
    if (Balance_text) Balance_text.text = SocketManager.ResultData.player.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;
  }
  #endregion

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }


  private void ToggleButtonGrp(bool toggle)
  {
    bool active = toggle && !_isFeatureActive;
    if (Spin_Button) Spin_Button.interactable = toggle ? active : !_isFeatureActive;
    if (MaxBet_Button) MaxBet_Button.interactable = active;
    if (BetMinus_Button) BetMinus_Button.interactable = active;
    if (BetPlus_Button) BetPlus_Button.interactable = active;
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

        bool isJackpotOrPinata = val >= 3 && val <= 7; // pinatas (8-10) handled by fly animation
        bool hasCoinValue = coinPositions != null &&
          coinPositions.Exists(c => c.position[0] == row && c.position[1] == col);

        if (!isJackpotOrPinata && !hasCoinValue) continue;

        ImageAnimation animScript = Animimages[col].slotImages[row]
          .GetComponent<ImageAnimation>();
        if (animScript != null && animScript.textureArray.Count > 0)
        {
          Animimages[col].slotImages[row].gameObject.SetActive(true);
          animScript.StartAnimation();
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
      TempList[i].gameObject.SetActive(false);
    }
    TempList.Clear();
    TempList.TrimExcess();
  }

  private void ClearCoinOverlays()
  {
    foreach (GameObject go in _coinOverlays)
      Destroy(go);
    _coinOverlays.Clear();
    for (int col = 0; col < Animimages.Count; col++)
      for (int row = 0; row < Animimages[col].coinValueTexts.Count; row++)
        if (Animimages[col].coinValueTexts[row]) Animimages[col].coinValueTexts[row].gameObject.SetActive(false);
    for (int col = 0; col < Tempimages.Count; col++)
      for (int row = 0; row < Tempimages[col].coinValueTexts.Count; row++)
        if (Tempimages[col].coinValueTexts[row]) Tempimages[col].coinValueTexts[row].gameObject.SetActive(false);
  }

  private void SpawnCoinOverlays()
  {
    var coins = SocketManager.ResultData.payload.coinWins;
    if (coins == null) return;
    foreach (var coin in coins)
    {
      int row = coin.position[0];
      int col = coin.position[1];
      if (col < Animimages.Count && row < Animimages[col].coinValueTexts.Count)
      {
        TMP_Text txt = Animimages[col].coinValueTexts[row];
        if (txt)
        {
          txt.text = coin.value.ToString("F2");
          txt.gameObject.SetActive(true);
        }
      }
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
    if (audioManager) audioManager.PlayReelStop();
    ShowTempCoinValuesForColumn(index);
    StartCoroutine(AnimatePinatasOnReelStop(index));
    yield return new WaitForSeconds(stagger);
  }

  private void ShowTempCoinValuesForColumn(int col)
  {
    var coins = SocketManager.ResultData.payload?.coinWins;
    if (coins == null || col >= Tempimages.Count) return;
    foreach (var coin in coins)
    {
      if (coin.position[1] != col) continue;
      int row = coin.position[0];
      if (row < Tempimages[col].coinValueTexts.Count)
      {
        TMP_Text txt = Tempimages[col].coinValueTexts[row];
        if (txt) { txt.text = coin.value.ToString("F2"); txt.gameObject.SetActive(true); }
      }
    }
  }

  private IEnumerator AnimatePinatasOnReelStop(int col)
  {
    _activePinataAnims++;
    yield return alltweens[col].WaitForCompletion();
    ShowCoinValuesForColumn(col);
    List<Coroutine> flyRoutines = new List<Coroutine>();
    for (int row = 0; row < 3; row++)
    {
      int val = int.Parse(SocketManager.ResultData.matrix[row][col]);
      if (val >= 8 && val <= 10)
        flyRoutines.Add(StartCoroutine(FlyPinataToMeter(col, row, val)));
    }
    foreach (var r in flyRoutines) yield return r;
    _activePinataAnims--;
  }

  private void ShowCoinValuesForColumn(int col)
  {
    var coins = SocketManager.ResultData.payload?.coinWins;
    if (coins == null || col >= Animimages.Count) return;
    foreach (var coin in coins)
    {
      if (coin.position[1] != col) continue;
      int row = coin.position[0];
      Image animImg = Animimages[col].slotImages[row];
      animImg.gameObject.SetActive(true);
      ImageAnimation animScript = animImg.GetComponent<ImageAnimation>();
      if (animScript != null && animScript.textureArray?.Count > 0)
      {
        animScript.StartAnimation();
        TempList.Add(animScript);
      }
      if (row < Animimages[col].coinValueTexts.Count)
      {
        TMP_Text txt = Animimages[col].coinValueTexts[row];
        if (txt) { txt.text = coin.value.ToString("F2"); txt.gameObject.SetActive(true); }
      }
    }
  }

  private IEnumerator FlyPinataToMeter(int col, int row, int colorId)
  {
    Image animImg = Animimages[col].slotImages[row];
    RectTransform animRT = (RectTransform)animImg.transform;
    Vector2 startAnchoredPos = animRT.anchoredPosition;
    Vector3 startScale = animRT.localScale;

    RectTransform target = colorId == 8 ? GreenPinataTarget
                         : colorId == 9 ? RedPinataTarget
                         : BluePinataTarget;
    if (target == null) yield break;

    ImageAnimation animScript = animImg.GetComponent<ImageAnimation>();
    animImg.gameObject.SetActive(true);
    if (animScript != null && animScript.textureArray?.Count > 0) animScript.StartAnimation();

    Sequence flySeq = DOTween.Sequence();
    flySeq.Append(animImg.transform.DOMove(target.position, pinataFlyDuration).SetEase(Ease.InOutQuad));
    flySeq.Join(animRT.DOScale(startScale * pinataPeakScale, pinataFlyDuration * 0.6f).SetEase(Ease.OutQuad));
    flySeq.Insert(pinataFlyDuration * 0.6f, animRT.DOScale(Vector3.zero, pinataFlyDuration * 0.4f).SetEase(Ease.InQuad));
    yield return flySeq.WaitForCompletion();

    if (animScript != null && animScript.textureArray?.Count > 0) animScript.StopAnimation();
    animImg.gameObject.SetActive(false);
    animRT.anchoredPosition = startAnchoredPos;
    animRT.localScale = startScale;
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
  public List<TMP_Text> coinValueTexts = new List<TMP_Text>(10);
}

