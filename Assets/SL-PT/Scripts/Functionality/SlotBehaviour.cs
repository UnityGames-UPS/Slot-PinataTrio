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
  [SerializeField]
  private Sprite AutoSpinIdleSprite;
  [SerializeField]
  private Sprite AutoSpinActiveSprite;

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

  [SerializeField]
  private SocketIOManager SocketManager;

  [SerializeField] private BonusController bonusController;
  [SerializeField] private LinkBonusController linkBonusController;
  [SerializeField] private float linkBonusPreSpinGlowDuration = 0.5f;

  private List<Tweener> alltweens = new List<Tweener>();
  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine;
  internal bool IsAutoSpin = false;
  private bool _isFeatureActive = false;
  private bool IsSpinning = false;
  private bool _restoreAutoSpin = false;
  private bool StopSpinToggle = false;
  private bool CheckSpinAudio = false;
  internal bool CheckPopups = false;
  private bool _isInFreeSpin = false;
  private bool _inputLocked = false;
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

  if (uiManager) uiManager.OnInfoScreenToggled += SetGameInputLocked;

  tweenHeight = (15 * IconSizeFactor) - 280;
  }

  private void SetGameInputLocked(bool locked)
  {
    _inputLocked = locked;
    if (locked)
    {
      if (Spin_Button) Spin_Button.interactable = false;
      if (MaxBet_Button) MaxBet_Button.interactable = false;
      if (BetMinus_Button) BetMinus_Button.interactable = false;
      if (BetPlus_Button) BetPlus_Button.interactable = false;
      if (AutoSpin_Button) AutoSpin_Button.interactable = false;
    }
    else
    {
      if (!IsSpinning) ToggleButtonGrp(true);
      if (AutoSpin_Button) AutoSpin_Button.interactable = !_isFeatureActive;
    }
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      if (_isFeatureActive || IsSpinning)
      {
        Debug.Log($"[AutoSpin] Start blocked (featureActive={_isFeatureActive}, isSpinning={IsSpinning})");
        return;
      }
      if (currentBalance < currentTotalBet)
      {
        uiManager.LowBalPopup();
        return;
      }
      Debug.Log("[AutoSpin] Starting AutoSpinCoroutine");
      IsAutoSpin = true;
      SetAutoSpinButtonSprite(true);
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
      Debug.Log("[AutoSpin] Stopping autospin");
      IsAutoSpin = false;
      SetAutoSpinButtonSprite(false);
    }
  }

  private void SetAutoSpinButtonSprite(bool active)
  {
    if (AutoSpin_Button == null) return;
    var img = AutoSpin_Button.GetComponent<Image>();
    if (img == null) return;
    img.sprite = active ? AutoSpinActiveSprite : AutoSpinIdleSprite;
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      yield return new WaitUntil(() => !CheckPopups && !IsSpinning && !_inputLocked);
      if (currentBalance < currentTotalBet)
      {
        StopAutoSpin();
        uiManager.LowBalPopup();
        break;
      }
      Debug.Log("[AutoSpin] AutoSpinCoroutine -> StartSlots()");
      StartSlots();
      yield return new WaitUntil(() => !IsSpinning);
      yield return new WaitForSeconds(SpinDelay);
    }
    yield return new WaitUntil(() => !IsSpinning);
    Debug.Log("[AutoSpin] AutoSpinCoroutine exiting");
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


  internal void InitializeMatrix()
  {
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int val = initialMatrix[row, col];
        ApplyReelIcon(Tempimages[col].slotImages[row], val);
        if (IsJackpotSymbol(val)) ShowOverlay(col, row, val);
      }
    }
    RandomizeOffscreenIcons();
  }

  internal void SetInitialUI()
  {
    BetCounter = 0;
    if (audioManager) audioManager.PlayGameStarted();
    uiManager.InitialiseUI(SocketManager.InitialData.bets, SocketManager.UIData.paylines.symbols);
    uiManager.InitialiseBalanceAndWin(SocketManager.PlayerData.balance, SocketManager.InitialData.bets[BetCounter]);
    currentBalance = SocketManager.PlayerData.balance;
    currentTotalBet = SocketManager.InitialData.bets[BetCounter];

    var meterState = SocketManager.CurrentMeterState;
    if (meterState != null) uiManager.SetMeterThresholds(meterState.greenThreshold, meterState.redThreshold, meterState.blueThreshold);
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
  }

  #region AnimationSprites
  // Used by LinkBonusController for its own cells. The board's overlay cells go through ShowOverlay.
  internal void PopulateAnimationSprites(ImageAnimation animScript, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    Sprite[] frames = GetFrames(val);
    if (frames != null) animScript.textureArray.AddRange(frames);
  }

  // Animation frames per symbol id. Blue (2) has none.
  private Sprite[] GetFrames(int symbolId)
  {
    switch (symbolId)
    {
      case 0: return YellowBubble_Sprite;
      case 1: return PinkBubble_Sprite;
      case 3: return Mini_Sprite;
      case 4: return Minor_Sprite;
      case 5: return Major_Sprite;
      case 6: return Mega_Sprite;
      case 7: return Grand_Sprite;
      case 8: return GreenPinata_Sprite;
      case 9: return RedPinata_Sprite;
      case 10: return BluePinata_Sprite;
      default: return null;
    }
  }

  private static bool IsJackpotSymbol(int symbolId) => symbolId >= 3 && symbolId <= 7;
  private static bool IsPinataSymbol(int symbolId) => symbolId >= 8 && symbolId <= 10;
  #endregion

  #region SlotSpin
  private void StartSlots()
  {
    uiManager.ResetTotalWin();
    uiManager.HideSpinWin();
    HideAllOverlays();
    RandomizeOffscreenIcons();
    uiManager.ShowTicker();
    tweenroutine = StartCoroutine(TweenRoutine());
  }

  private void BalanceDeduction()
  {
    if (_isInFreeSpin) return;
    uiManager.AnimateBalanceDeduction(currentBalance, currentBalance - currentTotalBet);
  }

  internal void UpdateBalanceDisplay(double newBalance)
  {
    currentBalance = newBalance;
    uiManager.UpdateBalance(newBalance);
    if (IsAutoSpin && currentBalance < currentTotalBet)
    {
      StopAutoSpin();
      uiManager.LowBalPopup();
    }
  }

  private IEnumerator TweenRoutine()
  {
    Debug.Log($"[Spin] TweenRoutine start (isInFreeSpin={_isInFreeSpin}, isFeatureActive={_isFeatureActive}, isAutoSpin={IsAutoSpin})");
    if (!_isInFreeSpin && currentBalance < currentTotalBet)
    {
      Debug.Log("[Spin] Low balance, aborting TweenRoutine");
      StopAutoSpin();
      uiManager.LowBalPopup();
      yield return new WaitForSeconds(1);
      if (!_inputLocked) ToggleButtonGrp(true);
      yield break;
    }
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
      if (!_inputLocked) ToggleButtonGrp(true);
      yield break;
    }

    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 5; col++)
      {
        int resultNum = int.Parse(SocketManager.ResultData.matrix[row][col]);
        ApplyReelIcon(Tempimages[col].slotImages[row], resultNum);
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
    if (Spin_Button) Spin_Button.interactable = false;
    yield return new WaitUntil(() => _activePinataAnims == 0);
    KillAllTweens();
    CheckForFeaturesAnimation();

    var meterState = SocketManager.CurrentMeterState;
    if (meterState != null) uiManager.UpdateMeters(meterState.greenMeter, meterState.redMeter, meterState.blueMeter);

    uiManager.UpdateTotalWin(SocketManager.ResultData.payload.winAmount);
    uiManager.UpdateBalance(SocketManager.ResultData.player.balance);
    currentBalance = SocketManager.PlayerData.balance;
    SpinDelay = SocketManager.ResultData.payload.winAmount > 0 ? 1.2f : 0.2f;

    var pendingFeatures = SocketManager.ResultData.payload?.pendingFeatures;
    bool hadPendingFeature = pendingFeatures != null && pendingFeatures.Exists(f => f.triggered);
    if (hadPendingFeature)
    {
      Debug.Log("[Spin] Pending feature triggered -> HandlePendingFeatures");
      yield return StartCoroutine(HandlePendingFeatures(pendingFeatures));
    }

    yield return StartCoroutine(uiManager.ShowSpinWin(SocketManager.ResultData.payload.winAmount));
    if (!_isInFreeSpin && !_isFeatureActive && !hadPendingFeature)
    {
      yield return StartCoroutine(CheckAndShowJackpotWin());
      yield return StartCoroutine(uiManager.ShowBonusWinSequence(SocketManager.ResultData.payload.winAmount, currentTotalBet));
    }

    CheckPopups = false;
    IsSpinning = false;
    Debug.Log($"[Spin] TweenRoutine end (restoreAutoSpin={_restoreAutoSpin}, isAutoSpin={IsAutoSpin}, isFeatureActive={_isFeatureActive}, inputLocked={_inputLocked})");
    if (_restoreAutoSpin) { _restoreAutoSpin = false; AutoSpin(); }
    if (!_inputLocked) ToggleButtonGrp(true);
  }
  #endregion

  #region PendingFeatures
  private IEnumerator HandlePendingFeatures(List<PendingFeature> features)
  {
    bool wasAutoSpinning = IsAutoSpin;
    Debug.Log($"[Feature] HandlePendingFeatures start (wasAutoSpinning={wasAutoSpinning})");
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
    _restoreAutoSpin = wasAutoSpinning;
    Debug.Log($"[Feature] HandlePendingFeatures done, _restoreAutoSpin={_restoreAutoSpin}");
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
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(awardValue, currentTotalBet));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("wheelBonus");

    uiManager.UpdateBalance(SocketManager.ResultData.player.balance);
    currentBalance = SocketManager.PlayerData.balance;

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
    if (audioManager) audioManager.PlayFreeGameStarted();
    yield return StartCoroutine(FreeSpinLoop());
    _isInFreeSpin = false;

    uiManager.PlayBustedPinataOnce("red");
    double freeSpinTotalWin = SocketManager.ResultData.payload?.winAmount ?? 0;
    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(goalJackpot, awardValue, freeSpinTotalWin));
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(freeSpinTotalWin, currentTotalBet));
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
      Debug.Log("[Feature] FreeSpinLoop -> StartSlots()");
      StartSlots();
      yield return new WaitUntil(() => !IsSpinning);

      Debug.Log($"[Feature] FreeSpinLoop round done, isFreeSpinActive={SocketManager.ResultData.payload.isFreeSpinActive}");
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
    StartCoroutine(uiManager.SlideContentDown());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("linkBonus"));
    StartCoroutine(uiManager.SlideContentUp());
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
    if (audioManager) audioManager.PlayFreeGameStarted();
    yield return StartCoroutine(LinkBonusFreeSpinLoop());
    _isInFreeSpin = false;

    var lbFeature = SocketManager.ResultData.payload?.triggeredFeatures?.Find(f => f.feature == "linkBonus");
    double awardValue = lbFeature?.awardValue ?? 0;
    var allLockedCells = lbFeature?.lockedCells ?? SocketManager.ResultData.payload?.linkBonusLockedCells;

    yield return StartCoroutine(linkBonusController.PlayTotalWinSequence(allLockedCells, awardValue));
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(awardValue, currentTotalBet));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("linkBonus");
    // The Link Bonus switched AnimationLayer off, which stopped every overlay on its first frame.
    ReplayOverlays();
    linkBonusController.ResetAll();

    uiManager.UpdateBalance(SocketManager.ResultData.player.balance);
    currentBalance = SocketManager.PlayerData.balance;

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

    uiManager.UpdateBalance(SocketManager.ResultData.player.balance);
    currentBalance = SocketManager.PlayerData.balance;
  }
  #endregion

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }


  private void ToggleButtonGrp(bool toggle)
  {
    bool active = toggle && !_isFeatureActive && !IsAutoSpin;
    if (Spin_Button) Spin_Button.interactable = toggle ? active : !_isFeatureActive;
    if (MaxBet_Button) MaxBet_Button.interactable = active;
    if (BetMinus_Button) BetMinus_Button.interactable = active;
    if (BetPlus_Button) BetPlus_Button.interactable = active;
  }

  #region GameAnimation
  // Runs once the whole board has landed and every pinata has reached its meter. Jackpot symbols
  // animate here (owner: they wait for the full board). Coin cells were already started as their
  // reel landed, and ShowOverlay never restarts a cell, so they are not touched again.
  private void CheckForFeaturesAnimation()
  {
    var coinPositions = SocketManager.ResultData.payload?.coinWins;
    bool hasJackpot = false;

    for (int row = 0; row < RowCount; row++)
    {
      for (int col = 0; col < ReelCount; col++)
      {
        int val = int.Parse(SocketManager.ResultData.matrix[row][col]);
        if (!IsJackpotSymbol(val)) continue;

        hasJackpot = true;
        if (ShowOverlay(col, row, val) && audioManager) audioManager.PlayJackpotIcon();
      }
    }

    bool hasCoinValueAnywhere = coinPositions != null && coinPositions.Count > 0;
    if (hasCoinValueAnywhere && !hasJackpot && audioManager) audioManager.PlayNormalIcon();
  }
  #endregion

  #region OverlayCells
  // Every animation on the board plays on the static AnimationLayer cells (Animimages), never on the
  // moving reel icons (Tempimages). Each cell is shown only through ShowOverlay and torn down only
  // through HideAllOverlays, so it is set up the same way every time (frames, speed, size, position)
  // and cleared completely, whatever it was doing. Self-contained on purpose: this region is meant to
  // move to SlotView as a whole.

  private const int RowCount = 3;
  private const int ReelCount = 5;

  // Sizes and speed are code, not serialized: a value saved in the scene would silently override them.
  private static readonly Vector2 DefaultCellSize = new Vector2(200f, 200f);

  // The orb frames are 300x300 canvases with the orb in the middle, so they need more room than the
  // 200x200 reel art to show the orb at the same size.
  private static readonly Dictionary<int, Vector2> OverlaySizes = new Dictionary<int, Vector2>
  {
    { 0, new Vector2(330f, 330f) },  // yellow orb
    { 1, new Vector2(330f, 330f) },  // pink orb
    { 3, new Vector2(210f, 210f) },  // mini
    { 4, new Vector2(210f, 210f) },  // minor
    { 5, new Vector2(210f, 210f) },  // major
    { 6, new Vector2(210f, 210f) },  // mega
    { 7, new Vector2(210f, 210f) },  // grand
  };

  // myImages holds each orb's FIRST animation frame and each jackpot's LAST (the loop wraps from it
  // to frame 0), so a reel icon and its overlay are the same sprite in the same rect, and a landing
  // flows straight into the animation. Keep these sizes equal to OverlaySizes for those symbols.
  private static readonly Dictionary<int, Vector2> ReelIconSizes = new Dictionary<int, Vector2>
  {
    { 0, new Vector2(330f, 330f) },  // yellow orb
    { 1, new Vector2(330f, 330f) },  // pink orb
    { 3, new Vector2(210f, 210f) },  // mini
    { 4, new Vector2(210f, 210f) },  // minor
    { 5, new Vector2(210f, 210f) },  // major
    { 6, new Vector2(210f, 210f) },  // mega
    { 7, new Vector2(210f, 210f) },  // grand
  };

  // Orbs rest dark on the reels and light up (untinted overlay) when they animate. 0.702 is measured:
  // the old yellow_orbs_dark / pink_orb_dark art is exactly 70.2% of the bright art on every channel.
  // Anything not listed is white, written every time, because reel icons are reused across symbols.
  private static readonly Color DarkOrbTint = new Color(0.702f, 0.702f, 0.702f, 1f);
  private static readonly Dictionary<int, Color> ReelIconTints = new Dictionary<int, Color>
  {
    { 0, DarkOrbTint },  // yellow orb
    { 1, DarkOrbTint },  // pink orb
  };

  // What the off-screen reel icons show while spinning: per reel, each symbol's weight from the
  // server's own reel config (Config/pinata_config.json, reelsInstance). Blue (2) is left out — it
  // belongs to the Link Bonus (owner). Mega only sits on reels 1, 3, 5 and Grand on reels 1, 3.
  private static readonly int[][] OffscreenSymbolIds = new int[][]
  {
    new[] { 0, 1, 3, 4, 5, 6, 7, 8, 9, 10 },
    new[] { 0, 1, 3, 4, 5, 8, 9, 10 },
    new[] { 0, 1, 3, 4, 5, 6, 7, 8, 9, 10 },
    new[] { 0, 1, 3, 4, 5, 8, 9, 10 },
    new[] { 0, 1, 3, 4, 5, 6, 8, 9, 10 },
  };
  private static readonly Dictionary<int, int> OffscreenWeights = new Dictionary<int, int>
  {
    { 0, 100 }, { 1, 100 },                      // orbs
    { 3, 1 }, { 4, 1 }, { 5, 1 }, { 6, 1 }, { 7, 1 },  // jackpots
    { 8, 5 }, { 9, 5 }, { 10, 5 },               // pinatas
  };

  // NOT a frame rate: ImageAnimation runs one loop in frames² / (24 x speed) seconds. 19 is what every
  // overlay cell had in the scene. It is written on every show, because a reused cell would otherwise
  // keep whatever speed the last symbol left on it.
  private const float OverlayAnimationSpeed = 19f;

  private readonly bool[,] overlayShown = new bool[RowCount, ReelCount];
  // A pinata that has flown to its meter: its cell stays empty until the next spin.
  private readonly bool[,] cellFlown = new bool[RowCount, ReelCount];

  // Each cell's scene layout, captured before anything moves it. The pinata flight moves, scales and
  // re-orders cells; HideAllOverlays puts every one back from here.
  private Vector2[,] overlayHomePos;
  private Vector2[,] overlayHomeSize;
  private int[,] overlayHomeSibling;
  private Transform[] overlayColumns;
  private int[] overlayColumnHomeSibling;

  private Image GetOverlayImage(int col, int row)
  {
    if (col < 0 || col >= Animimages.Count) return null;
    var cells = Animimages[col].slotImages;
    return row >= 0 && row < cells.Count ? cells[row] : null;
  }

  private Image GetReelIcon(int col, int row)
  {
    if (col < 0 || col >= Tempimages.Count) return null;
    var cells = Tempimages[col].slotImages;
    return row >= 0 && row < cells.Count ? cells[row] : null;
  }

  private static TMP_Text GetCoinText(List<SlotImage> layer, int col, int row)
  {
    if (col < 0 || col >= layer.Count) return null;
    var texts = layer[col].coinValueTexts;
    return row >= 0 && row < texts.Count ? texts[row] : null;
  }

  // Lazy rather than in Start, so it is captured on the first use whatever the start-up order is.
  private void EnsureOverlayHomes()
  {
    if (overlayHomePos != null) return;

    overlayHomePos = new Vector2[RowCount, ReelCount];
    overlayHomeSize = new Vector2[RowCount, ReelCount];
    overlayHomeSibling = new int[RowCount, ReelCount];
    overlayColumns = new Transform[ReelCount];
    overlayColumnHomeSibling = new int[ReelCount];

    for (int col = 0; col < ReelCount; col++)
    {
      for (int row = 0; row < RowCount; row++)
      {
        Image img = GetOverlayImage(col, row);
        if (img == null) continue;

        RectTransform rt = img.rectTransform;
        overlayHomePos[row, col] = rt.anchoredPosition;
        overlayHomeSize[row, col] = rt.sizeDelta;
        overlayHomeSibling[row, col] = rt.GetSiblingIndex();

        if (overlayColumns[col] == null && rt.parent != null)
        {
          overlayColumns[col] = rt.parent;
          overlayColumnHomeSibling[col] = rt.parent.GetSiblingIndex();
        }
      }
    }
  }

  // Writes one reel icon: its sprite, size and tint, together, every time.
  private void ApplyReelIcon(Image icon, int symbolId)
  {
    if (icon == null) return;
    if (symbolId >= 0 && symbolId < myImages.Length) icon.sprite = myImages[symbolId];
    icon.rectTransform.sizeDelta = ReelIconSizes.TryGetValue(symbolId, out Vector2 size) ? size : DefaultCellSize;
    icon.color = ReelIconTints.TryGetValue(symbolId, out Color tint) ? tint : Color.white;
  }

  // Gives every reel icon outside the visible rows a weighted random symbol, through ApplyReelIcon so
  // it gets the same sprite, size and tint as a landed one. The three visible icons are skipped: the
  // board must not change before the reels move. Called at game start and at every spin start.
  private void RandomizeOffscreenIcons()
  {
    for (int col = 0; col < Slot_Transform.Length && col < OffscreenSymbolIds.Length; col++)
    {
      Transform strip = Slot_Transform[col];
      if (strip == null) continue;
      List<Image> visible = col < Tempimages.Count ? Tempimages[col].slotImages : null;

      for (int i = 0; i < strip.childCount; i++)
      {
        Image icon = strip.GetChild(i).GetComponent<Image>();
        if (icon == null || (visible != null && visible.Contains(icon))) continue;
        ApplyReelIcon(icon, PickOffscreenSymbol(col));
      }
    }
  }

  private static int PickOffscreenSymbol(int col)
  {
    int[] ids = OffscreenSymbolIds[col];
    int total = 0;
    foreach (int id in ids) total += OffscreenWeights[id];

    int roll = UnityEngine.Random.Range(0, total);
    foreach (int id in ids)
    {
      roll -= OffscreenWeights[id];
      if (roll < 0) return id;
    }
    return ids[0];
  }

  // The one way an overlay cell comes on. Returns whether the cell is now showing.
  // - Already showing this spin: left exactly as it is. Never restarted.
  // - A pinata has already flown from it: stays empty.
  // - The symbol has no frames: nothing is switched on, and the reel icon stays visible.
  private bool ShowOverlay(int col, int row, int symbolId)
  {
    if (overlayShown[row, col]) return true;
    if (cellFlown[row, col]) return false;

    Sprite[] frames = GetFrames(symbolId);
    if (frames == null || frames.Length == 0) return false;

    Image img = GetOverlayImage(col, row);
    ImageAnimation anim = img != null ? img.GetComponent<ImageAnimation>() : null;
    if (anim == null) return false;

    EnsureOverlayHomes();

    if (anim.textureArray == null) anim.textureArray = new List<Sprite>();
    anim.textureArray.Clear();
    anim.textureArray.AddRange(frames);
    anim.AnimationSpeed = OverlayAnimationSpeed;
    anim.doLoopAnimation = true;

    RectTransform rt = img.rectTransform;
    rt.DOKill();
    rt.anchoredPosition = overlayHomePos[row, col];
    rt.localScale = Vector3.one;
    rt.sizeDelta = OverlaySizes.TryGetValue(symbolId, out Vector2 size) ? size : DefaultCellSize;

    // Activate first: ImageAnimation runs on a coroutine, which cannot start on an inactive object.
    // If the whole layer is off (a Link Bonus), ReplayOverlays starts it when the layer returns.
    img.gameObject.SetActive(true);
    if (img.gameObject.activeInHierarchy) anim.StartAnimation();

    // Hidden in the same step, so the static icon never shows through the animation's transparent
    // frames. Its coin text is a child and goes with it.
    Image reelIcon = GetReelIcon(col, row);
    if (reelIcon) reelIcon.gameObject.SetActive(false);

    overlayShown[row, col] = true;
    return true;
  }

  // The one way overlay cells go off: all 15, every time, whatever they were doing. Puts back every
  // reel icon, every cell's scene layout and draw order, and hides every coin text.
  private void HideAllOverlays()
  {
    EnsureOverlayHomes();

    for (int col = 0; col < ReelCount; col++)
    {
      for (int row = 0; row < RowCount; row++)
      {
        Image img = GetOverlayImage(col, row);
        if (img != null)
        {
          RectTransform rt = img.rectTransform;
          rt.DOKill();
          ImageAnimation anim = img.GetComponent<ImageAnimation>();
          if (anim != null) anim.StopAnimation();
          img.gameObject.SetActive(false);
          rt.anchoredPosition = overlayHomePos[row, col];
          rt.sizeDelta = overlayHomeSize[row, col];
          rt.localScale = Vector3.one;
        }

        TMP_Text overlayText = GetCoinText(Animimages, col, row);
        if (overlayText) overlayText.gameObject.SetActive(false);
        TMP_Text reelText = GetCoinText(Tempimages, col, row);
        if (reelText) reelText.gameObject.SetActive(false);

        Image reelIcon = GetReelIcon(col, row);
        if (reelIcon) reelIcon.gameObject.SetActive(true);

        overlayShown[row, col] = false;
        cellFlown[row, col] = false;
      }
    }

    RestoreOverlayOrder();
  }

  // Applied in ascending order of home index, so each SetSiblingIndex lands where it started.
  private void RestoreOverlayOrder()
  {
    for (int col = 0; col < ReelCount; col++)
    {
      var cells = new List<KeyValuePair<int, Transform>>();
      for (int row = 0; row < RowCount; row++)
      {
        Image img = GetOverlayImage(col, row);
        if (img != null) cells.Add(new KeyValuePair<int, Transform>(overlayHomeSibling[row, col], img.transform));
      }
      cells.Sort((a, b) => a.Key.CompareTo(b.Key));
      foreach (var cell in cells) cell.Value.SetSiblingIndex(cell.Key);
    }

    var columns = new List<KeyValuePair<int, Transform>>();
    for (int col = 0; col < ReelCount; col++)
      if (overlayColumns[col] != null) columns.Add(new KeyValuePair<int, Transform>(overlayColumnHomeSibling[col], overlayColumns[col]));
    columns.Sort((a, b) => a.Key.CompareTo(b.Key));
    foreach (var column in columns) column.Value.SetSiblingIndex(column.Key);
  }

  // Restarts every cell still showing. Needed after the Link Bonus: switching AnimationLayer off
  // stopped them all on their first frame, and nothing restarts an ImageAnimation when it comes back.
  private void ReplayOverlays()
  {
    for (int col = 0; col < ReelCount; col++)
    {
      for (int row = 0; row < RowCount; row++)
      {
        if (!overlayShown[row, col]) continue;
        Image img = GetOverlayImage(col, row);
        if (img == null || !img.gameObject.activeInHierarchy) continue;
        ImageAnimation anim = img.GetComponent<ImageAnimation>();
        if (anim != null) anim.StartAnimation();
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
        if (txt) { txt.text = coin.value.ToString("F2"); txt.gameObject.SetActive(true); if (audioManager) audioManager.PlayCoinValueAppear(); }
      }
    }
  }

  private IEnumerator AnimatePinatasOnReelStop(int col)
  {
    _activePinataAnims++;
    yield return alltweens[col].WaitForCompletion();
    ShowCoinValuesForColumn(col);
    List<Coroutine> flyRoutines = new List<Coroutine>();
    for (int row = 0; row < RowCount; row++)
    {
      int val = int.Parse(SocketManager.ResultData.matrix[row][col]);
      if (IsPinataSymbol(val))
        flyRoutines.Add(StartCoroutine(FlyPinataToMeter(col, row, val)));
    }
    foreach (var r in flyRoutines) yield return r;
    _activePinataAnims--;
  }

  // Coin cells animate as soon as their reel has landed.
  private void ShowCoinValuesForColumn(int col)
  {
    var coins = SocketManager.ResultData.payload?.coinWins;
    if (coins == null) return;
    foreach (var coin in coins)
    {
      if (coin.position[1] != col) continue;
      int row = coin.position[0];
      int symbolId = int.Parse(SocketManager.ResultData.matrix[row][col]);

      // A symbol with no frames switches nothing on. The reel icon stays, with the coin text it was
      // given as the reel landed (ShowTempCoinValuesForColumn).
      if (!ShowOverlay(col, row, symbolId)) continue;

      TMP_Text txt = GetCoinText(Animimages, col, row);
      if (txt) { txt.text = coin.value.ToString("F2"); txt.gameObject.SetActive(true); }
    }
  }

  private IEnumerator FlyPinataToMeter(int col, int row, int colorId)
  {
    RectTransform target = colorId == 8 ? GreenPinataTarget
                         : colorId == 9 ? RedPinataTarget
                         : BluePinataTarget;
    if (target == null) yield break;

    // Sizes it, starts it, and hides the reel icon: the pinata leaves its cell rather than a copy
    // flying off while the original stays behind.
    if (!ShowOverlay(col, row, colorId)) yield break;
    if (audioManager) audioManager.PlayBubblePop();

    Image animImg = GetOverlayImage(col, row);
    RectTransform animRT = animImg.rectTransform;

    // Raise the cell within its column, and the column within the layer, so the pinata draws over
    // every reel it crosses. HideAllOverlays restores the order at the next spin.
    animRT.SetAsLastSibling();
    if (animRT.parent) animRT.parent.SetAsLastSibling();

    Sequence flySeq = DOTween.Sequence();
    flySeq.Append(animRT.DOMove(target.position, pinataFlyDuration).SetEase(Ease.InOutQuad));
    flySeq.Join(animRT.DOScale(Vector3.one * pinataPeakScale, pinataFlyDuration * 0.6f).SetEase(Ease.OutQuad));
    flySeq.Insert(pinataFlyDuration * 0.6f, animRT.DOScale(Vector3.zero, pinataFlyDuration * 0.4f).SetEase(Ease.InQuad));
    yield return flySeq.WaitForCompletion();

    uiManager.PopPinata(colorId);

    ImageAnimation animScript = animImg.GetComponent<ImageAnimation>();
    if (animScript != null) animScript.StopAnimation();
    animImg.gameObject.SetActive(false);
    animRT.anchoredPosition = overlayHomePos[row, col];
    animRT.localScale = Vector3.one;

    // The reel icon stays hidden: the cell is empty until the next spin.
    overlayShown[row, col] = false;
    cellFlown[row, col] = true;
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

