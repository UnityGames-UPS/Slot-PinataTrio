using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The controller: the round. Bet, spin flow, Stop, autospin, the three features and the buttons.
// The board itself — reels, icons and every animation over them — is SlotView, driven through its
// internal methods. (Moved here unchanged from SlotBehaviour in split pass 2.)
public class GameManager : MonoBehaviour
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

  // What the round is doing. Every entry point (spin, bet, autospin, the info screen) checks this.
  // - Idle:     no round running; the buttons are live.
  // - Spinning: a base-game round — reels, result, landing, wins — for a manual or an autospin spin.
  // - Feature:  inside HandlePendingFeatures — intros, the pick, the wheel, every free spin and every
  //             Link Bonus round. A spin run inside a feature (Pick Jackpot's free spins) leaves the
  //             state alone; TweenRoutine notes that as `featureSpin`.
  internal enum GameState { Idle, Spinning, Feature }
  internal GameState CurrentState { get; private set; } = GameState.Idle;

  // Not round state, so kept apart from GameState:
  internal bool IsAutoSpin = false;                // a mode the player toggles
  private bool _inputLocked = false;               // the info screen is open
  private bool stopRequested = false;              // Stop pressed during this spin (fast stop)
  private bool resumeAutoSpinAfterFeature = false; // a feature parked autospin; resume it after the round

  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine;
  internal int BetCounter = 0;
  private double currentBalance = 0;
  private double currentTotalBet = 0;
  private float SpinDelay = 0.2f;
  [SerializeField] private float minSpinDuration = 2f;
  [SerializeField] private float stopButtonWindow = 0.5f;
  [SerializeField] private float naturalStopStagger = 0.2f;
  [SerializeField] private float stopButtonStagger = 0.05f;
  private float spinStartTime;

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
      if (CurrentState == GameState.Idle) ToggleButtonGrp(true);
      if (AutoSpin_Button) AutoSpin_Button.interactable = CurrentState != GameState.Feature;
    }
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      if (CurrentState != GameState.Idle)
      {
        Debug.Log($"[AutoSpin] Start blocked (state={CurrentState})");
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
      yield return new WaitUntil(() => CurrentState == GameState.Idle && !_inputLocked);
      if (currentBalance < currentTotalBet)
      {
        StopAutoSpin();
        uiManager.LowBalPopup();
        break;
      }
      Debug.Log("[AutoSpin] AutoSpinCoroutine -> StartSlots()");
      StartSlots();
      // Waits out the whole round, features included. If a feature parked autospin, the round's end
      // restarts it through AutoSpin(), which stops this coroutine and starts a fresh one.
      yield return new WaitUntil(() => CurrentState == GameState.Idle);
      yield return new WaitForSeconds(SpinDelay);
    }
    yield return new WaitUntil(() => CurrentState == GameState.Idle);
    Debug.Log("[AutoSpin] AutoSpinCoroutine exiting");
    ToggleButtonGrp(true);
    if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
  }
  #endregion

  // One button: Stop while a base-game round is spinning, Spin when idle. During a feature the button
  // is disabled anyway, and a press does nothing.
  private void OnSpinButtonPressed()
  {
    if (CurrentState == GameState.Spinning)
      OnStopSpinPressed();
    else if (CurrentState == GameState.Idle)
      StartSlots();
  }

  private void OnStopSpinPressed()
  {
    stopRequested = true;
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
    GameConfig config = SocketManager.Config;
    uiManager.InitialiseUI(config.Bets, config.JackpotMultipliers);
    uiManager.InitialiseBalanceAndWin(config.InitialBalance, config.Bets[BetCounter]);
    currentBalance = config.InitialBalance;
    currentTotalBet = config.Bets[BetCounter];

    MeterValues thresholds = config.MeterThresholds;
    if (thresholds != null) uiManager.SetMeterThresholds(thresholds.Green, thresholds.Red, thresholds.Blue);
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
  }

  #region SlotSpin
  private void StartSlots()
  {
    PrepareRound();
    tweenroutine = StartCoroutine(TweenRoutine());
  }

  // The UI reset before every spin, base game or free spin.
  private void PrepareRound()
  {
    uiManager.ResetTotalWin();
    uiManager.HideSpinWin();
    slotView.PrepareSpin();
    uiManager.ShowTicker();
  }

  // A spin inside a feature (a free spin) costs nothing.
  private void BalanceDeduction(bool featureSpin)
  {
    if (featureSpin) return;
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
    // A spin started inside a feature (Pick Jackpot's free spins) runs under the feature's state and
    // leaves it alone. It is free, skips the low-balance check, and skips the end-of-spin wins.
    bool featureSpin = CurrentState == GameState.Feature;
    Debug.Log($"[Spin] TweenRoutine start (state={CurrentState}, featureSpin={featureSpin}, isAutoSpin={IsAutoSpin})");
    if (!featureSpin && currentBalance < currentTotalBet)
    {
      Debug.Log("[Spin] Low balance, aborting TweenRoutine");
      StopAutoSpin();
      uiManager.LowBalPopup();
      yield return new WaitForSeconds(1);
      if (!_inputLocked) ToggleButtonGrp(true);
      yield break;
    }
    if (!featureSpin) CurrentState = GameState.Spinning;
    if (audioManager) audioManager.PlaySpinLoop();
    spinStartTime = Time.time;
    Spin_Button.GetComponent<Image>().sprite = StopSprite;
    ToggleButtonGrp(false);
    yield return slotView.StartReels();
    BalanceDeduction(featureSpin);
    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone && Time.time - spinStartTime >= minSpinDuration);

    // This spin's own result, captured once. Everything below that belongs to the spin reads `spin`;
    // the features capture their own responses (Wild Jokers' structure).
    RoundResult spin = SocketManager.LatestResult;
    if (spin == null || !spin.HasPayload || spin.Matrix == null)
    {
      slotView.CancelSpin();
      Spin_Button.GetComponent<Image>().sprite = SpinSprite;
      if (!featureSpin) CurrentState = GameState.Idle;
      if (!_inputLocked) ToggleButtonGrp(true);
      yield break;
    }

    slotView.SetResult(spin.Matrix, spin.Coins);
    float stopWindowEnd = Time.time + stopButtonWindow;
    while (Time.time < stopWindowEnd)
    {
      if (stopRequested) break;
      yield return null;
    }
    float stagger = stopRequested ? stopButtonStagger : naturalStopStagger;
    yield return slotView.StopReels(stagger);
    stopRequested = false;
    if (audioManager) audioManager.StopSpinLoop();
    yield return slotView.WaitForLastReel();
    Spin_Button.GetComponent<Image>().sprite = SpinSprite;
    if (Spin_Button) Spin_Button.interactable = false;
    yield return slotView.WaitForLandingEffects();
    slotView.ShowJackpotOverlays();

    MeterValues meters = SocketManager.CurrentMeters;
    if (meters != null) uiManager.UpdateMeters(meters.Green, meters.Red, meters.Blue);

    uiManager.UpdateTotalWin(spin.WinAmount);
    uiManager.UpdateBalance(spin.Balance);
    currentBalance = spin.Balance;
    SpinDelay = spin.WinAmount > 0 ? 1.2f : 0.2f;

    bool hadPendingFeature = spin.TriggeredFeatures.Count > 0;
    if (hadPendingFeature)
    {
      Debug.Log("[Spin] Pending feature triggered -> HandlePendingFeatures");
      yield return StartCoroutine(HandlePendingFeatures(spin));
    }

    // Deliberately the LATEST response, not `spin`: after a feature that is the feature's last response
    // (the wheel reply, the last free spin, the last Link Bonus round), which is what this popup has
    // always shown. With no feature it is `spin` itself. Wild Jokers shows the trigger spin's own win
    // here instead; switching is this one line, once the backend confirms what those responses'
    // winAmount means (GameExplanation §7, open question).
    RoundResult shown = SocketManager.LatestResult;
    yield return StartCoroutine(uiManager.ShowSpinWin(shown.WinAmount));
    if (!featureSpin && !hadPendingFeature)
    {
      yield return StartCoroutine(CheckAndShowJackpotWin(shown));
      yield return StartCoroutine(uiManager.ShowBonusWinSequence(shown.WinAmount, currentTotalBet));
    }

    // Idle first: AutoSpin() refuses to start unless the round is over.
    if (!featureSpin) CurrentState = GameState.Idle;
    Debug.Log($"[Spin] TweenRoutine end (state={CurrentState}, featureSpin={featureSpin}, resumeAutoSpin={resumeAutoSpinAfterFeature}, isAutoSpin={IsAutoSpin}, inputLocked={_inputLocked})");
    if (resumeAutoSpinAfterFeature) { resumeAutoSpinAfterFeature = false; AutoSpin(); }
    if (!_inputLocked) ToggleButtonGrp(true);
  }
  #endregion

  #region PendingFeatures
  // Owns the Feature state for every feature the spin won, from the first intro to the last total.
  // The previous state is put back at the end (Spinning, so the round finishes normally).
  // `trigger` is the spin that won them; each feature reads what it needs from it, or captures its
  // own response right after waiting for it.
  private IEnumerator HandlePendingFeatures(RoundResult trigger)
  {
    GameState previousState = CurrentState;
    CurrentState = GameState.Feature;
    bool wasAutoSpinning = IsAutoSpin;
    Debug.Log($"[Feature] HandlePendingFeatures start (wasAutoSpinning={wasAutoSpinning})");
    StopAutoSpin();
    foreach (string feature in trigger.TriggeredFeatures)
    {
      uiManager.SetReelFrame(feature);
      switch (feature)
      {
        case "pickJackpot":
          yield return StartCoroutine(HandleRedPinataPick());
          break;
        case "wheelBonus":
          yield return StartCoroutine(HandleWheelBonus());
          break;
        case "linkBonus":
          yield return StartCoroutine(HandleLinkBonus(trigger));
          break;
      }
      uiManager.SetReelFrame("default");
    }
    resumeAutoSpinAfterFeature = wasAutoSpinning;
    CurrentState = previousState;
    Debug.Log($"[Feature] HandlePendingFeatures done (state={CurrentState}, resumeAutoSpin={resumeAutoSpinAfterFeature})");
  }

  private IEnumerator HandleWheelBonus()
  {
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);
    uiManager.SetupFeaturePinata("wheelBonus");
    StartCoroutine(uiManager.SlideContentDown());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("wheelBonus"));
    if (audioManager) audioManager.PlayBonusBgMusic();

    SocketManager.SendWheelBonus();
    yield return new WaitUntil(() => SocketManager.isWheelBonusDone);
    SocketManager.isWheelBonusDone = false;

    // The wheel's own response, captured once.
    RoundResult wheelResponse = SocketManager.LatestResult;
    WheelBonusResult wheel = wheelResponse?.Wheel ?? WheelBonusResult.Fallback();

    if (bonusController != null) bonusController.StartWheelBonus(wheel.SpinHistory, wheel.Award);
    yield return new WaitUntil(() => bonusController == null || bonusController.isBonusDone);

    yield return StartCoroutine(uiManager.SlideContentUp());

    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(wheel.Tier, wheel.Award, wheel.Award));
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(wheel.Award, currentTotalBet));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("wheelBonus");

    if (wheelResponse != null)
    {
      uiManager.UpdateBalance(wheelResponse.Balance);
      currentBalance = wheelResponse.Balance;
    }

    uiManager.LockFeatureUI(false);
  }

  private IEnumerator SetupRedPinataAfterSlide()
  {
    yield return StartCoroutine(uiManager.SlideContentDown());
    uiManager.SetupFeaturePinata("pickJackpot");
  }

  private IEnumerator HandleRedPinataPick()
  {
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);
    StartCoroutine(SetupRedPinataAfterSlide());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("pickJackpot"));
    if (audioManager) audioManager.PlayBonusBgMusic();
    uiManager.ShowPickJackpotScreen();

    yield return new WaitUntil(() => uiManager.PickJackpotSelected);

    SocketManager.SendPickJackpot();
    uiManager.PickJackpotSelected = false;

    yield return new WaitUntil(() => SocketManager.isPickJackpotDone);
    SocketManager.isPickJackpotDone = false;

    // The pick's own response, captured once.
    PickJackpotResult pick = SocketManager.LatestResult?.Pick;
    string goalJackpot = pick?.GoalJackpot;
    double awardValue = pick?.Award ?? 0;

    yield return StartCoroutine(uiManager.RevealJackpot(goalJackpot));

    if (audioManager) audioManager.PlayFreeGameStarted();
    yield return StartCoroutine(FreeSpinLoop());

    uiManager.PlayBustedPinataOnce("red");
    // The last free spin's winAmount is the round's total (GameExplanation §3).
    double freeSpinTotalWin = SocketManager.LatestResult?.WinAmount ?? 0;
    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(goalJackpot, awardValue, freeSpinTotalWin));
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(freeSpinTotalWin, currentTotalBet));
    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("pickJackpot");
    uiManager.LockFeatureUI(false);
  }

  // Pick Jackpot's free spins. Each is a full TweenRoutine run under the Feature state (so it is free
  // and leaves the state alone), waited on directly until the server says the round is over.
  private IEnumerator FreeSpinLoop()
  {
    while (true)
    {
      yield return new WaitForSeconds(SpinDelay);
      Debug.Log("[Feature] FreeSpinLoop -> free spin");
      PrepareRound();
      yield return StartCoroutine(TweenRoutine());

      // The free spin that just ran is the latest response.
      bool roundGoesOn = SocketManager.LatestResult != null && SocketManager.LatestResult.IsFreeSpinActive;
      Debug.Log($"[Feature] FreeSpinLoop round done, isFreeSpinActive={roundGoesOn}");
      if (!roundGoesOn)
        break;
    }
  }

  // The base game's jackpot win, if the result has one (the converter picks the first jackpot win).
  private IEnumerator CheckAndShowJackpotWin(RoundResult result)
  {
    JackpotWin jackpot = result?.BaseJackpot;
    if (jackpot == null) yield break;

    yield return StartCoroutine(uiManager.ShowJackpotWinSequence(jackpot.Tier, jackpot.Amount, result.WinAmount));
  }

  private IEnumerator HandleLinkBonus(RoundResult trigger)
  {
    StartCoroutine(uiManager.SlideContentDown());
    yield return StartCoroutine(uiManager.PlayFeatureIntro("linkBonus"));
    StartCoroutine(uiManager.SlideContentUp());
    if (audioManager) audioManager.PlayBonusBgMusic();
    uiManager.LockFeatureUI(true);
    ToggleButtonGrp(false);

    // The zones and the opening spin count come from the spin that triggered the Link Bonus.
    List<LinkZone> targetZones = trigger.LinkTargetZones;
    int initialSpins = trigger.HasPayload ? trigger.FreeSpinsRemaining : 3;

    uiManager.SetupFeaturePinata("linkBonus");
    uiManager.UpdateLinkBonusSpinsRemaining(initialSpins);

    yield return StartCoroutine(linkBonusController.StartLinkBonus(targetZones));

    if (audioManager) audioManager.PlayFreeGameStarted();
    yield return StartCoroutine(LinkBonusFreeSpinLoop());

    // The total comes from the Link Bonus's last round, the latest response.
    RoundResult lastRound = SocketManager.LatestResult;
    double awardValue = lastRound?.Link?.Award ?? 0;
    List<LinkCell> allLockedCells = lastRound?.Link?.LockedCells ?? lastRound?.LinkLockedCells;

    yield return StartCoroutine(linkBonusController.PlayTotalWinSequence(allLockedCells, awardValue));
    yield return StartCoroutine(uiManager.ShowBonusWinSequence(awardValue, currentTotalBet));

    if (audioManager) audioManager.StopBonusBgMusic();
    uiManager.CleanupFeaturePinata("linkBonus");
    // The Link Bonus switched AnimationLayer off, which stopped every overlay on its first frame.
    slotView.ReplayOverlays();
    linkBonusController.ResetAll();

    if (lastRound != null)
    {
      uiManager.UpdateBalance(lastRound.Balance);
      currentBalance = lastRound.Balance;
    }

    uiManager.LockFeatureUI(false);
  }

  private IEnumerator LinkBonusFreeSpinLoop()
  {
    while (true)
    {
      yield return new WaitForSeconds(SpinDelay);
      yield return StartCoroutine(LinkBonusSpinRound());
      // The round that just ran is the latest response.
      if (!(SocketManager.LatestResult?.IsLinkBonusActive ?? false)) break;
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

    // This round's own response, captured once.
    RoundResult round = SocketManager.LatestResult;
    List<LinkCell> lockedCells = round?.LinkLockedCells;

    yield return StartCoroutine(linkBonusController.StopCellsSequential(round?.Matrix, lockedCells));
    linkBonusController.UpdateLockedCells(lockedCells);
    uiManager.UpdateLinkBonusSpinsRemaining(round?.FreeSpinsRemaining ?? 0);

    if (round != null)
    {
      uiManager.UpdateBalance(round.Balance);
      currentBalance = round.Balance;
    }
  }
  #endregion

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }


  // The one place the spin and bet buttons are enabled. `toggle` false is the mid-spin case: the bet
  // buttons go off, and Spin stays live as Stop — except in a feature, where everything is off.
  private void ToggleButtonGrp(bool toggle)
  {
    bool inFeature = CurrentState == GameState.Feature;
    bool active = toggle && !inFeature && !IsAutoSpin;
    if (Spin_Button) Spin_Button.interactable = toggle ? active : !inFeature;
    if (MaxBet_Button) MaxBet_Button.interactable = active;
    if (BetMinus_Button) BetMinus_Button.interactable = active;
    if (BetPlus_Button) BetPlus_Button.interactable = active;
  }
}
