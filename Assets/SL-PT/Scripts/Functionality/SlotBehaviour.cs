using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The round: bet, spin flow, Stop, autospin, the three features and the buttons. The board itself —
// reels, icons and every animation over them — is SlotView, driven through its internal methods.
// (Split pass 2 moves this class's contents into GameManager.)
public class SlotBehaviour : MonoBehaviour
{
  [Header("Board")]
  [SerializeField]
  private SlotView slotView;

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

  [Header("Audio Management")]
  [SerializeField]
  private AudioManager audioManager;

  [SerializeField]
  private UIManager uiManager;

  [SerializeField]
  private SocketIOManager SocketManager;

  [SerializeField] private BonusController bonusController;
  [SerializeField] private LinkBonusController linkBonusController;
  [SerializeField] private float linkBonusPreSpinGlowDuration = 0.5f;

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
  private float SpinDelay = 0.2f;
  [SerializeField] private float minSpinDuration = 2f;
  [SerializeField] private float stopButtonWindow = 0.5f;
  [SerializeField] private float naturalStopStagger = 0.2f;
  [SerializeField] private float stopButtonStagger = 0.05f;
  private float spinStartTime;
  internal bool socketConnected = false;

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
    slotView.ShowInitialBoard();
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

  #region SlotSpin
  private void StartSlots()
  {
    uiManager.ResetTotalWin();
    uiManager.HideSpinWin();
    slotView.PrepareSpin();
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
    IsSpinning = true;
    if (audioManager) audioManager.PlaySpinLoop();
    spinStartTime = Time.time;
    Spin_Button.GetComponent<Image>().sprite = StopSprite;
    ToggleButtonGrp(false);
    yield return slotView.StartReels();
    BalanceDeduction();
    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone && Time.time - spinStartTime >= minSpinDuration);

    if (SocketManager.ResultData?.payload == null)
    {
      slotView.CancelSpin();
      Spin_Button.GetComponent<Image>().sprite = SpinSprite;
      CheckPopups = false;
      IsSpinning = false;
      if (!_inputLocked) ToggleButtonGrp(true);
      yield break;
    }

    slotView.SetResult(ParseMatrix(SocketManager.ResultData.matrix), SocketManager.ResultData.payload.coinWins);
    float stopWindowEnd = Time.time + stopButtonWindow;
    while (Time.time < stopWindowEnd)
    {
      if (StopSpinToggle) break;
      yield return null;
    }
    float stagger = StopSpinToggle ? stopButtonStagger : naturalStopStagger;
    yield return slotView.StopReels(stagger);
    StopSpinToggle = false;
    if (audioManager) audioManager.StopSpinLoop();
    yield return slotView.WaitForLastReel();
    Spin_Button.GetComponent<Image>().sprite = SpinSprite;
    if (Spin_Button) Spin_Button.interactable = false;
    yield return slotView.WaitForLandingEffects();
    slotView.ShowJackpotOverlays();

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
    slotView.ReplayOverlays();
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

  // The server sends the matrix row-major as strings. Parsed once here, before the board sees it.
  private static int[,] ParseMatrix(List<List<string>> matrix)
  {
    var result = new int[3, 5];
    for (int row = 0; row < 3; row++)
      for (int col = 0; col < 5; col++)
        result[row, col] = int.Parse(matrix[row][col]);
    return result;
  }
}
