using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{

  [Header("Menu UI")]
  [SerializeField]
  private Button Menu_Button;
  [SerializeField]
  private GameObject Menu_Object;
  [SerializeField]
  private RectTransform Menu_RT;

  [Header("Settings UI")]
  [SerializeField]
  private Button Settings_Button;
  [SerializeField]
  private GameObject Settings_Object;
  [SerializeField]
  private RectTransform Settings_RT;
  [SerializeField]
  private Button Terms_Button;
  [SerializeField]
  private Button Privacy_Button;

  [SerializeField]
  private Button Exit_Button;
  [SerializeField]
  private GameObject Exit_Object;
  [SerializeField]
  private RectTransform Exit_RT;

  // PAYTABLE FROM VIKING GAME
  // [SerializeField]
  // private Button Paytable_Button;
  // [SerializeField]
  // private GameObject Paytable_Object;
  // [SerializeField]
  // private RectTransform Paytable_RT;

  [Header("Betting UI")]
  [SerializeField] private TMP_Text TotalBetAmountText;
  [SerializeField] private TMP_Text MiniPayoutText;
  [SerializeField] private TMP_Text MinorPayoutText;
  [SerializeField] private TMP_Text MajorPayoutText;
  [SerializeField] private TMP_Text MegaPayoutText;
  [SerializeField] private TMP_Text GrandPayoutText;

  [Header("Pinata Meters UI")]
  [SerializeField] private TMP_Text GreenMeterText;
  [SerializeField] private TMP_Text RedMeterText;
  [SerializeField] private TMP_Text BlueMeterText;

  [Header("Reel Frame")]
  [SerializeField] private Image ReelFrame;
  [SerializeField] private Sprite DefaultReelFrameSprite;
  [SerializeField] private Sprite GreenReelFrameSprite;
  [SerializeField] private Sprite RedReelFrameSprite;
  [SerializeField] private Sprite BlueReelFrameSprite;

  [Header("Intro")]
  [SerializeField] private RectTransform PinataTrio;
  [SerializeField] private RectTransform GameContent;
  [SerializeField] private float introHoldDuration = 3f;
  [SerializeField] private float offscreenOffset = 1200f;
  [SerializeField] private float gameContentScrollAmount = 700f;
  [SerializeField] private RectTransform GreenPinata;
  [SerializeField] private RectTransform RedPinata;
  [SerializeField] private RectTransform BluePinata;
  [SerializeField] private float pinataScrollAmount = 250f;
  [SerializeField] private float pinataRedDelay = 0.3f;

  [Header("Feature Pinata UI")]
  [SerializeField] private float featureHideAmount = 550f;
  [SerializeField] private Sprite BustedRedPinataSprite;
  [SerializeField] private Sprite BustedBluePinataSprite;
  [SerializeField] private Image SmallReelFrame;
  [SerializeField] private Sprite GoalFrameSprite;
  [SerializeField] private Sprite SpinsRemainingFrameSprite;
  [SerializeField] private float pinataEarlyStart = 0.15f;

  [Header("Wheel Bonus UI")]
  [SerializeField] private TMP_Text WheelBonusGrandText;
  [SerializeField] private TMP_Text WheelBonusMegaText;
  [SerializeField] private TMP_Text WheelBonusMajorText;
  [SerializeField] private TMP_Text WheelBonusMinorText;
  [SerializeField] private TMP_Text WheelBonusMiniText;

  [Header("Pick Jackpot UI")]
  [SerializeField] private GameObject PickJackpotPanel;
  [SerializeField] private Button[] PinataButtons;
  [SerializeField] private Image[] JackpotRevealImages;
  [SerializeField] private TMP_Text PickJackpotTimerText;
  [SerializeField] private RectTransform FallingJackpotRT;
  [SerializeField] private Image FallingJackpotImage;
  [SerializeField] private Transform JackpotLandingPoint;
  [SerializeField] private Sprite MiniJackpotSprite;
  [SerializeField] private Sprite MinorJackpotSprite;
  [SerializeField] private Sprite MajorJackpotSprite;
  [SerializeField] private Sprite MegaJackpotSprite;
  [SerializeField] private Sprite GrandJackpotSprite;
  [SerializeField] private float pickJackpotTimerDuration = 10f;
  [SerializeField] private ImageAnimation[] PinataButtonAnimations;
  [SerializeField] private Image FreeSpinsUntilImage;
  [SerializeField] private float jackpotLaunchHeight = 500f;

  internal bool PickJackpotSelected = false;
  private int _selectedPinataIndex = -1;
  private Coroutine _pickJackpotTimerRoutine;
  private readonly string[] _jackpotOrder = { "mini", "minor", "major", "mega", "grand" };

  [Header("Ticker UI")]
  [SerializeField] private RectTransform TickerContainer;
  [SerializeField] private TMP_Text TickerText;
  [SerializeField] private float TickerDuration = 3f;

  private readonly string[] tickerMessages = { "GOOD LUCK", "ALL THE BEST" };
  private int tickerIndex = 0;
  private Tween tickerTween;

  private List<double> betAmounts;
  private double[] jackpotMultipliers = new double[5];
  internal int BetCount => betAmounts?.Count ?? 0;
  internal double GetBetAmount(int index) => betAmounts[index];

  [Header("Information UI")]
  [SerializeField]
  private Button Info_Button;
  [SerializeField]
  private GameObject InfoSlidesPanel;
  [SerializeField]
  private Button BackToGame_Button;
  [SerializeField]
  private Button NextButton;
  [SerializeField]
  private Button PrevButton;
  [SerializeField]
  private Image SlideContainer;
  [SerializeField]
  private Sprite[] InfoSlides;

  private int currentSlideIndex = 0;

  [Header("Popus UI")]
  [SerializeField]
  private GameObject MainPopup_Object;

  [Header("About Popup")]
  [SerializeField]
  private GameObject AboutPopup_Object;
  [SerializeField]
  private Button AboutExit_Button;
  [SerializeField]
  private Image AboutLogo_Image;
  [SerializeField]
  private Button Support_Button;

  // PAYTABLE FROM VIKING GAME
  // [Header("Paytable Popup")]
  // [SerializeField]
  // private GameObject PaytablePopup_Object;
  // [SerializeField]
  // private Button PaytableExit_Button;
  // [SerializeField]
  // private TMP_Text[] SymbolsText;
  // [SerializeField]
  // private TMP_Text FreeSpin_Text;
  // [SerializeField]
  // private TMP_Text Scatter_Text;
  // [SerializeField]
  // private TMP_Text Jackpot_Text;
  // [SerializeField]
  // private TMP_Text Bonus_Text;
  // [SerializeField]
  // private TMP_Text Wild_Text;

  [Header("Settings Popup")]
  [SerializeField]
  private GameObject SettingsPopup_Object;
  [SerializeField]
  private Button SettingsExit_Button;
  [SerializeField]
  private Button Sound_Button;
  [SerializeField]
  private Button Music_Button;

  [SerializeField]
  private GameObject MusicOn_Object;
  [SerializeField]
  private GameObject MusicOff_Object;
  [SerializeField]
  private GameObject SoundOn_Object;
  [SerializeField]
  private GameObject SoundOff_Object;

  [Header("Win Popup")]
  [SerializeField]
  private Sprite BigWin_Sprite;
  [SerializeField]
  private Sprite HugeWin_Sprite;
  [SerializeField]
  private Sprite MegaWin_Sprite;
  [SerializeField]
  private Sprite Jackpot_Sprite;
  [SerializeField]
  private Image Win_Image;
  [SerializeField]
  private GameObject WinPopup_Object;
  [SerializeField]
  private TMP_Text Win_Text;
  [SerializeField] private Button SkipWinAnimation;

  [Header("FreeSpins Popup")]
  [SerializeField]
  private GameObject FreeSpinPopup_Object;
  [SerializeField]
  private TMP_Text Free_Text;

  [Header("Splash Screen")]
  [SerializeField]
  private GameObject Loading_Object;
  [SerializeField]
  private Image Loading_Image;
  [SerializeField]
  private TMP_Text Loading_Text;
  [SerializeField]
  private TMP_Text LoadPercent_Text;
  [SerializeField]
  private Button QuitSplash_button;

  [Header("Disconnection Popup")]
  [SerializeField]
  private Button CloseDisconnect_Button;
  [SerializeField]
  private GameObject DisconnectPopup_Object;

  [Header("AnotherDevice Popup")]
  [SerializeField]
  private Button CloseAD_Button;
  [SerializeField]
  private GameObject ADPopup_Object;

  [Header("Reconnection Popup")]
  [SerializeField]
  private TMP_Text reconnect_Text;
  [SerializeField]
  private GameObject ReconnectPopup_Object;

  [Header("LowBalance Popup")]
  [SerializeField]
  private Button LBExit_Button;
  [SerializeField]
  private GameObject LBPopup_Object;

  [Header("Quit Popup")]
  [SerializeField]
  private GameObject QuitPopup_Object;
  [SerializeField]
  private Button YesQuit_Button;
  [SerializeField]
  private Button NoQuit_Button;
  [SerializeField]
  private Button CrossQuit_Button;

  [SerializeField]
  private AudioController audioController;
  [SerializeField]
  private Button m_AwakeGameButton;

  [SerializeField]
  private Button GameExit_Button;

  [SerializeField]
  private SlotBehaviour slotManager;

  private bool isMusic = true;
  private bool isSound = true;
  private Tween WinPopupTextTween;
  private Tween ClosePopupTween;
  internal bool isExit = false;
  internal int FreeSpins;
  private Vector2 _pickJackpotPanelOrigin;
  private Vector2[] _jackpotRevealImageOrigins;
  private Vector2 _greenPinataOrigin;
  private Vector2 _redPinataOrigin;
  private Vector2 _bluePinataOrigin;
  private Vector2 _smallReelFrameOrigin;
  private Sprite _redPinataOriginalSprite;
  private Sprite _bluePinataOriginalSprite;
  private void Start()
  {
    if (PickJackpotPanel) _pickJackpotPanelOrigin = PickJackpotPanel.GetComponent<RectTransform>().anchoredPosition;
    if (JackpotRevealImages != null)
    {
      _jackpotRevealImageOrigins = new Vector2[JackpotRevealImages.Length];
      for (int i = 0; i < JackpotRevealImages.Length; i++)
        _jackpotRevealImageOrigins[i] = JackpotRevealImages[i].rectTransform.anchoredPosition;
    }
    if (GreenPinata) _greenPinataOrigin = GreenPinata.anchoredPosition;
    if (RedPinata) { _redPinataOrigin = RedPinata.anchoredPosition; _redPinataOriginalSprite = RedPinata.GetComponent<Image>()?.sprite; }
    if (BluePinata) { _bluePinataOrigin = BluePinata.anchoredPosition; _bluePinataOriginalSprite = BluePinata.GetComponent<Image>()?.sprite; }
    if (SmallReelFrame) _smallReelFrameOrigin = SmallReelFrame.rectTransform.anchoredPosition;
    StartCoroutine(PlayIntro());

    if (Menu_Button) Menu_Button.onClick.RemoveAllListeners();
    if (Menu_Button) Menu_Button.onClick.AddListener(OpenMenu);

    if (Exit_Button) Exit_Button.onClick.RemoveAllListeners();
    if (Exit_Button) Exit_Button.onClick.AddListener(CloseMenu);

    //if (About_Button) About_Button.onClick.RemoveAllListeners();
    //if (About_Button) About_Button.onClick.AddListener(delegate { OpenPopup(AboutPopup_Object); });

    if (AboutExit_Button) AboutExit_Button.onClick.RemoveAllListeners();
    if (AboutExit_Button) AboutExit_Button.onClick.AddListener(delegate { ClosePopup(AboutPopup_Object); });

    // PAYTABLE FROM VIKING GAME
    // if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
    // if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPopup(PaytablePopup_Object); });
    // if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
    // if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

    if (InfoSlidesPanel) InfoSlidesPanel.SetActive(false);

    if (Info_Button) Info_Button.onClick.RemoveAllListeners();
    if (Info_Button) Info_Button.onClick.AddListener(() =>
    {
      currentSlideIndex = 0;
      InfoSlidesPanel.SetActive(true);
      ShowSlide(currentSlideIndex);

      if (BackToGame_Button) BackToGame_Button.onClick.RemoveAllListeners();
      if (BackToGame_Button) BackToGame_Button.onClick.AddListener(() => InfoSlidesPanel.SetActive(false));

      if (NextButton) NextButton.onClick.RemoveAllListeners();
      if (NextButton) NextButton.onClick.AddListener(() =>
      {
        currentSlideIndex = (currentSlideIndex + 1) % InfoSlides.Length;
        ShowSlide(currentSlideIndex);
      });

      if (PrevButton) PrevButton.onClick.RemoveAllListeners();
      if (PrevButton) PrevButton.onClick.AddListener(() =>
      {
        currentSlideIndex = (currentSlideIndex - 1 + InfoSlides.Length) % InfoSlides.Length;
        ShowSlide(currentSlideIndex);
      });
    });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(delegate { OpenPopup(SettingsPopup_Object); });

    if (SettingsExit_Button) SettingsExit_Button.onClick.RemoveAllListeners();
    if (SettingsExit_Button) SettingsExit_Button.onClick.AddListener(delegate { ClosePopup(SettingsPopup_Object); });

    if (MusicOn_Object) MusicOn_Object.SetActive(true);
    if (MusicOff_Object) MusicOff_Object.SetActive(false);

    if (SoundOn_Object) SoundOn_Object.SetActive(true);
    if (SoundOff_Object) SoundOff_Object.SetActive(false);

    if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
    if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate
    {
      OpenPopup(QuitPopup_Object);
    });

    if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
    if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
    if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
    if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

    if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
    if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate
    {
      CallOnExitFunction();
      Debug.Log("quit event: pressed YES Button ");

    });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction); //BackendChanges

    if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
    if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);

    if (QuitSplash_button) QuitSplash_button.onClick.RemoveAllListeners();
    if (QuitSplash_button) QuitSplash_button.onClick.AddListener(delegate { OpenPopup(QuitPopup_Object); });

    if (audioController) audioController.ToggleMute(false);

    isMusic = true;
    isSound = true;

    if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
    if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

    if (Music_Button) Music_Button.onClick.RemoveAllListeners();
    if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);

    if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
    if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);

  }

  private IEnumerator PlayIntro()
  {
    if (PinataTrio)
      PinataTrio.anchoredPosition = new Vector2(PinataTrio.anchoredPosition.x, PinataTrio.anchoredPosition.y + offscreenOffset);

    if (PinataTrio)
      yield return PinataTrio.DOAnchorPosY(PinataTrio.anchoredPosition.y - offscreenOffset, 0.8f).SetEase(Ease.OutBounce).WaitForCompletion();

    yield return new WaitForSeconds(introHoldDuration);

    if (PinataTrio) PinataTrio.DOAnchorPosY(PinataTrio.anchoredPosition.y + offscreenOffset, 0.6f).SetEase(Ease.InBack);
    if (GameContent)
      yield return GameContent.DOAnchorPosY(GameContent.anchoredPosition.y + gameContentScrollAmount, 0.8f).SetEase(Ease.OutCubic).WaitForCompletion();

    if (GreenPinata) GreenPinata.DOAnchorPosY(GreenPinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack);
    if (BluePinata) BluePinata.DOAnchorPosY(BluePinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack);

    yield return new WaitForSeconds(pinataRedDelay);

    if (RedPinata)
      yield return RedPinata.DOAnchorPosY(RedPinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
  }

  internal void LowBalPopup()
  {
    OpenPopup(LBPopup_Object);
  }

  internal void DisconnectionPopup()
  {
    if (!isExit)
    {
      OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    OpenPopup(ReconnectPopup_Object);
  }

  internal void CheckAndClosePopups()
  {
    if (ReconnectPopup_Object != null && ReconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(ReconnectPopup_Object);
    }
    if (DisconnectPopup_Object != null && DisconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(DisconnectPopup_Object);
    }
  }

  internal void PopulateWin(int value, double amount)
  {
    switch (value)
    {
      case 1:
        if (Win_Image) Win_Image.sprite = BigWin_Sprite;
        break;
      case 2:
        if (Win_Image) Win_Image.sprite = HugeWin_Sprite;
        break;
      case 3:
        if (Win_Image) Win_Image.sprite = MegaWin_Sprite;
        break;
      case 4:
        if (Win_Image) Win_Image.sprite = Jackpot_Sprite;
        break;
    }

    StartPopupAnim(amount);
  }

  private void StartFreeSpins(int spins)
  {
    // if (MainPopup_Object) MainPopup_Object.SetActive(false);
    if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(false);
    
    // slotManager.FreeSpin(spins); // TODO: wire up Pinata free spin
  }

  internal void FreeSpinProcess(int spins)
  {
    int ExtraSpins = spins - FreeSpins;
    FreeSpins = spins;
    // Debug.Log("ExtraSpins: " + ExtraSpins);
    // Debug.Log("Total Spins: " + spins);
    if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(true);
    if (Free_Text) Free_Text.text = ExtraSpins.ToString() + " Free spins awarded.";
    // if (MainPopup_Object) MainPopup_Object.SetActive(true);
    DOVirtual.DelayedCall(1.5f, () =>
    {
      StartFreeSpins(spins);
    });
  }

  void SkipWin()
  {
    Debug.Log("Skip win called");
    if (ClosePopupTween != null)
    {
      ClosePopupTween.Kill();
      ClosePopupTween = null;
    }
    if (WinPopupTextTween != null)
    {
      WinPopupTextTween.Kill();
      WinPopupTextTween = null;
    }
    ClosePopup(WinPopup_Object);
    slotManager.CheckPopups = false;
  }

  private void StartPopupAnim(double amount)
  {
    double initAmount = 0;
    if (WinPopup_Object) WinPopup_Object.SetActive(true);
    // if (MainPopup_Object) MainPopup_Object.SetActive(true);
    WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 1f).OnUpdate(() =>
    {
      if (Win_Text) Win_Text.text = initAmount.ToString("F3");
    });

    ClosePopupTween = DOVirtual.DelayedCall(2f, () =>
    {
      // ClosePopup(WinPopup_Object);
      if (WinPopup_Object) WinPopup_Object.SetActive(false);
      slotManager.CheckPopups = false;
    });
  }

  internal void ADfunction()
  {
    OpenPopup(ADPopup_Object);
  }

  // PAYTABLE FROM VIKING GAME
  // internal void InitialiseUIData(Paylines symbolsText)
  // {
  //   PopulateSymbolsPayout(symbolsText);
  // }

  // private void PopulateSymbolsPayout(Paylines paylines)
  // {
  //   double betPerLine = socketManager.InitialData.bets[slotManager.BetCounter];
  //   for (int i = 0; i < SymbolsText.Length; i++)
  //   {
  //     string text = null;
  //     if (paylines.symbols[i].multiplier[0] != 0)
  //     {
  //       text += "5x - " + (paylines.symbols[i].multiplier[0] * betPerLine);
  //     }
  //     if (paylines.symbols[i].multiplier[1] != 0)
  //     {
  //       text += "\n4x - " + (paylines.symbols[i].multiplier[1] * betPerLine);
  //     }
  //     if (paylines.symbols[i].multiplier[2] != 0)
  //     {
  //       text += "\n3x - " + (paylines.symbols[i].multiplier[2] * betPerLine);
  //     }
  //     if (SymbolsText[i]) SymbolsText[i].text = text;
  //   }
  //   FreeSpin_Text.text = GetSymbolDescription("FreeSpin");
  //   Wild_Text.text = GetSymbolDescription("Wild");
  //   Scatter_Text.text = GetSymbolDescription("Scatter");
  //   Jackpot_Text.text = GetSymbolDescription("Jackpot");
  //   Bonus_Text.text = GetSymbolDescription("Bonus");
  // }

  // internal string GetSymbolDescription(string name)
  // {
  //   if (socketManager.UIData.paylines.symbols == null) return null;
  //   foreach (var symbol in socketManager.UIData.paylines.symbols)
  //   {
  //     if (symbol.name == name)
  //     {
  //       return symbol.description;
  //     }
  //   }
  //   return null;
  // }

  private void CallOnExitFunction()
  {
    if (!isExit)
    {
      isExit = true;
      audioController.PlayButtonAudio();
      slotManager.CallCloseSocket();
    }
  }

  private void OpenMenu()
  {
    audioController.PlayButtonAudio();
    if (Menu_Object) Menu_Object.SetActive(false);
    if (Exit_Object) Exit_Object.SetActive(true);
    //if (About_Object) About_Object.SetActive(true);
    // PAYTABLE FROM VIKING GAME
    // if (Paytable_Object) Paytable_Object.SetActive(true);
    if (Settings_Object) Settings_Object.SetActive(true);

    //DOTween.To(() => About_RT.anchoredPosition, (val) => About_RT.anchoredPosition = val, new Vector2(About_RT.anchoredPosition.x, About_RT.anchoredPosition.y + 150), 0.1f).OnUpdate(() =>
    //{
    //    LayoutRebuilder.ForceRebuildLayoutImmediate(About_RT);
    //});

    // PAYTABLE FROM VIKING GAME
    // DOTween.To(() => Paytable_RT.anchoredPosition, (val) => Paytable_RT.anchoredPosition = val, new Vector2(Paytable_RT.anchoredPosition.x, Paytable_RT.anchoredPosition.y + 125), 0.1f).OnUpdate(() =>
    // {
    //   LayoutRebuilder.ForceRebuildLayoutImmediate(Paytable_RT);
    // });

    DOTween.To(() => Settings_RT.anchoredPosition, (val) => Settings_RT.anchoredPosition = val, new Vector2(Settings_RT.anchoredPosition.x, Settings_RT.anchoredPosition.y + 250), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_RT);
    });
  }

  private void CloseMenu()
  {

    if (audioController) audioController.PlayButtonAudio();
    //DOTween.To(() => About_RT.anchoredPosition, (val) => About_RT.anchoredPosition = val, new Vector2(About_RT.anchoredPosition.x, About_RT.anchoredPosition.y - 150), 0.1f).OnUpdate(() =>
    //{
    //    LayoutRebuilder.ForceRebuildLayoutImmediate(About_RT);
    //});

    // PAYTABLE FROM VIKING GAME
    // DOTween.To(() => Paytable_RT.anchoredPosition, (val) => Paytable_RT.anchoredPosition = val, new Vector2(Paytable_RT.anchoredPosition.x, Paytable_RT.anchoredPosition.y - 125), 0.1f).OnUpdate(() =>
    // {
    //   LayoutRebuilder.ForceRebuildLayoutImmediate(Paytable_RT);
    // });

    DOTween.To(() => Settings_RT.anchoredPosition, (val) => Settings_RT.anchoredPosition = val, new Vector2(Settings_RT.anchoredPosition.x, Settings_RT.anchoredPosition.y - 250), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_RT);
    });

    DOVirtual.DelayedCall(0.1f, () =>
     {
       if (Menu_Object) Menu_Object.SetActive(true);
       if (Exit_Object) Exit_Object.SetActive(false);
       //if (About_Object) About_Object.SetActive(false);
       // PAYTABLE FROM VIKING GAME
       // if (Paytable_Object) Paytable_Object.SetActive(false);
       if (Settings_Object) Settings_Object.SetActive(false);
     });
  }

  private void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  internal void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(false);
    if (!DisconnectPopup_Object.activeSelf)
    {
      if (MainPopup_Object) MainPopup_Object.SetActive(false);
    }
  }

  private void ToggleMusic()
  {
    isMusic = !isMusic;
    if (isMusic)
    {
      if (MusicOn_Object) MusicOn_Object.SetActive(true);
      if (MusicOff_Object) MusicOff_Object.SetActive(false);
      audioController.ToggleMute(false, "bg");
    }
    else
    {
      if (MusicOn_Object) MusicOn_Object.SetActive(false);
      if (MusicOff_Object) MusicOff_Object.SetActive(true);
      audioController.ToggleMute(true, "bg");
    }
  }

  private void UrlButtons(string url)
  {
    Application.OpenURL(url);
  }

  private void ToggleSound()
  {
    isSound = !isSound;
    if (isSound)
    {
      if (SoundOn_Object) SoundOn_Object.SetActive(true);
      if (SoundOff_Object) SoundOff_Object.SetActive(false);
      if (audioController) audioController.ToggleMute(false, "button");
      if (audioController) audioController.ToggleMute(false, "wl");
    }
    else
    {
      if (SoundOn_Object) SoundOn_Object.SetActive(false);
      if (SoundOff_Object) SoundOff_Object.SetActive(true);
      if (audioController) audioController.ToggleMute(true, "button");
      if (audioController) audioController.ToggleMute(true, "wl");
    }
  }

  internal void UpdateMeters(int green, int red, int blue)
  {
    // NULL GUARDS SINCE METER UI OBJECTS ARE NOT YET WIRED. WIRE ONCE METER DISPLAY IS BUILT IN SCENE.
    if (GreenMeterText) GreenMeterText.text = green.ToString();
    if (RedMeterText) RedMeterText.text = red.ToString();
    if (BlueMeterText) BlueMeterText.text = blue.ToString();
  }

  internal void LockFeatureUI(bool locked)
  {
    if (Menu_Button) Menu_Button.interactable = !locked;
    if (GameExit_Button) GameExit_Button.interactable = !locked;
  }

  internal void SetReelFrame(string feature)
  {
    if (ReelFrame == null) return;
    switch (feature)
    {
      case "wheelBonus":  ReelFrame.sprite = GreenReelFrameSprite; break;
      case "pickJackpot": ReelFrame.sprite = RedReelFrameSprite;   break;
      case "linkBonus":   ReelFrame.sprite = BlueReelFrameSprite;  break;
      default:            ReelFrame.sprite = DefaultReelFrameSprite; break;
    }
  }

  internal IEnumerator SlideContentDown()
  {
    yield return GameContent.DOAnchorPosY(GameContent.anchoredPosition.y - featureHideAmount, 0.5f)
      .SetEase(Ease.InOutCubic).WaitForCompletion();
    // Reset pinatas and small frame to pre-intro local Y while hidden, so SlideContentUp can animate them back up
    if (GreenPinata) GreenPinata.anchoredPosition = _greenPinataOrigin;
    if (BluePinata) BluePinata.anchoredPosition = _bluePinataOrigin;
    if (RedPinata) RedPinata.anchoredPosition = new Vector2(RedPinata.anchoredPosition.x, _redPinataOrigin.y);
    if (SmallReelFrame && SmallReelFrame.gameObject.activeSelf)
      SmallReelFrame.rectTransform.anchoredPosition = _smallReelFrameOrigin;
  }

  internal IEnumerator SlideContentUp()
  {
    float slideDuration = 0.5f;
    GameContent.DOAnchorPosY(GameContent.anchoredPosition.y + featureHideAmount, slideDuration).SetEase(Ease.OutCubic);
    yield return new WaitForSeconds(Mathf.Max(0f, slideDuration - pinataEarlyStart));
    if (GreenPinata) GreenPinata.DOAnchorPosY(GreenPinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack);
    if (BluePinata) BluePinata.DOAnchorPosY(BluePinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack);
    yield return new WaitForSeconds(pinataRedDelay);
    if (SmallReelFrame && SmallReelFrame.gameObject.activeSelf)
      SmallReelFrame.rectTransform.DOAnchorPosY(SmallReelFrame.rectTransform.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack);
    if (RedPinata)
      yield return RedPinata.DOAnchorPosY(RedPinata.anchoredPosition.y + pinataScrollAmount, 0.6f).SetEase(Ease.OutBack).WaitForCompletion();
  }

  internal void SetupFeaturePinata(string feature)
  {
    if (feature == "pickJackpot" && RedPinata)
    {
      Image img = RedPinata.GetComponent<Image>();
      if (img && BustedRedPinataSprite) img.sprite = BustedRedPinataSprite;
      RedPinata.anchoredPosition = new Vector2(0f, RedPinata.anchoredPosition.y);
      if (SmallReelFrame) { SmallReelFrame.sprite = GoalFrameSprite; SmallReelFrame.gameObject.SetActive(true); }
    }
    else if (feature == "linkBonus" && BluePinata)
    {
      Image img = BluePinata.GetComponent<Image>();
      if (img && BustedBluePinataSprite) img.sprite = BustedBluePinataSprite;
      BluePinata.anchoredPosition = new Vector2(0f, BluePinata.anchoredPosition.y);
      if (SmallReelFrame) { SmallReelFrame.sprite = SpinsRemainingFrameSprite; SmallReelFrame.gameObject.SetActive(true); }
      if (GreenPinata) GreenPinata.gameObject.SetActive(false);
      if (RedPinata) RedPinata.gameObject.SetActive(false);
    }
  }

  internal void CleanupFeaturePinata(string feature)
  {
    if (SmallReelFrame) { SmallReelFrame.rectTransform.anchoredPosition = _smallReelFrameOrigin; SmallReelFrame.gameObject.SetActive(false); }
    if (feature == "pickJackpot" && RedPinata)
    {
      Image img = RedPinata.GetComponent<Image>();
      if (img && _redPinataOriginalSprite) img.sprite = _redPinataOriginalSprite;
      // Restore original X only — Y is already at the correct post-animation position
      RedPinata.anchoredPosition = new Vector2(_redPinataOrigin.x, RedPinata.anchoredPosition.y);
    }
    else if (feature == "linkBonus" && BluePinata)
    {
      Image img = BluePinata.GetComponent<Image>();
      if (img && _bluePinataOriginalSprite) img.sprite = _bluePinataOriginalSprite;
      BluePinata.anchoredPosition = new Vector2(_bluePinataOrigin.x, BluePinata.anchoredPosition.y);
      if (GreenPinata) GreenPinata.gameObject.SetActive(true);
      if (RedPinata) RedPinata.gameObject.SetActive(true);
    }
  }

  internal void InitialiseUI(List<double> bets, List<Symbol> symbols)
  {
    betAmounts = bets;
    foreach (var symbol in symbols)
    {
      if (symbol.id >= 3 && symbol.id <= 7 && symbol.multiplier?[0] != null)
        jackpotMultipliers[symbol.id - 3] = symbol.multiplier[0].Value;
    }
    UpdateBetDisplay(betAmounts[0]);
  }

  internal void SetBet(int betIndex)
  {
    UpdateBetDisplay(betAmounts[betIndex]);
  }

  internal void ShowTicker()
  {
    if (TickerContainer == null || TickerText == null) return;

    tickerTween?.Kill();

    TickerText.text = tickerMessages[tickerIndex];
    tickerIndex = (tickerIndex + 1) % tickerMessages.Length;

    float containerWidth = TickerContainer.rect.width;
    float startX = containerWidth / 2f + TickerText.preferredWidth / 2f;
    float endX = -(containerWidth / 2f + TickerText.preferredWidth / 2f);

    TickerText.rectTransform.anchoredPosition = new Vector2(startX, TickerText.rectTransform.anchoredPosition.y);
    tickerTween = TickerText.rectTransform.DOAnchorPosX(endX, TickerDuration).SetEase(Ease.Linear);
  }

  internal void ShowPickJackpotScreen()
  {
    PickJackpotSelected = false;
    _selectedPinataIndex = -1;

    if (PickJackpotPanel)
    {
      PickJackpotPanel.GetComponent<RectTransform>().anchoredPosition = _pickJackpotPanelOrigin;
      PickJackpotPanel.SetActive(true);
    }

    if (JackpotRevealImages != null)
      for (int i = 0; i < JackpotRevealImages.Length; i++)
      {
        JackpotRevealImages[i].gameObject.SetActive(false);
        JackpotRevealImages[i].transform.localScale = Vector3.one;
        if (_jackpotRevealImageOrigins != null)
          JackpotRevealImages[i].rectTransform.anchoredPosition = _jackpotRevealImageOrigins[i];
      }

    for (int i = 0; i < PinataButtons.Length; i++)
    {
      int index = i;
      Image btnImg = PinataButtons[i].GetComponent<Image>();
      if (btnImg) btnImg.color = Color.white;
      PinataButtons[i].onClick.RemoveAllListeners();
      PinataButtons[i].onClick.AddListener(() => OnPinataSelected(index));
      PinataButtons[i].interactable = true;
    }

    if (_pickJackpotTimerRoutine != null) StopCoroutine(_pickJackpotTimerRoutine);
    _pickJackpotTimerRoutine = StartCoroutine(PickJackpotTimer());

    foreach (var anim in PinataButtonAnimations)
      if (anim != null) anim.StartAnimation();
  }

  private void OnPinataSelected(int index)
  {
    if (PickJackpotSelected) return;
    _selectedPinataIndex = index;
    if (_pickJackpotTimerRoutine != null)
    {
      StopCoroutine(_pickJackpotTimerRoutine);
      _pickJackpotTimerRoutine = null;
    }
    foreach (var btn in PinataButtons) btn.interactable = false;
    foreach (var anim in PinataButtonAnimations)
      if (anim != null) anim.StopAnimation();
    PickJackpotSelected = true;
  }

  private IEnumerator PickJackpotTimer()
  {
    float timeRemaining = pickJackpotTimerDuration;
    while (timeRemaining > 0)
    {
      timeRemaining -= Time.deltaTime;
      if (PickJackpotTimerText) PickJackpotTimerText.text = Mathf.CeilToInt(timeRemaining).ToString();
      yield return null;
    }
    OnPinataSelected(Random.Range(0, PinataButtons.Length));
  }

  internal IEnumerator RevealJackpot(string goalJackpot)
  {
    List<string> remaining = new List<string>(_jackpotOrder);
    remaining.Remove(goalJackpot);

    for (int i = remaining.Count - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1);
      string temp = remaining[i];
      remaining[i] = remaining[j];
      remaining[j] = temp;
    }

    string[] assignment = new string[PinataButtons.Length];
    assignment[_selectedPinataIndex] = goalJackpot;
    int remainingIndex = 0;
    for (int i = 0; i < assignment.Length; i++)
    {
      if (i == _selectedPinataIndex) continue;
      assignment[i] = remaining[remainingIndex++];
    }

    for (int i = 0; i < JackpotRevealImages.Length; i++)
    {
      JackpotRevealImages[i].sprite = GetJackpotSprite(assignment[i]);
      JackpotRevealImages[i].gameObject.SetActive(true);
      JackpotRevealImages[i].color = new Color(1, 1, 1, 0);
    }

    List<Tween> fadeTweens = new List<Tween>();
    for (int i = 0; i < PinataButtons.Length; i++)
    {
      Image pinataImg = PinataButtons[i].GetComponent<Image>();
      if (pinataImg) fadeTweens.Add(pinataImg.DOFade(0f, 0.5f));
    }
    for (int i = 0; i < JackpotRevealImages.Length; i++)
    {
      float targetAlpha = i == _selectedPinataIndex ? 1f : 0.4f;
      fadeTweens.Add(JackpotRevealImages[i].DOFade(targetAlpha, 0.5f));
    }
    yield return fadeTweens[^1].WaitForCompletion();

    yield return JackpotRevealImages[_selectedPinataIndex].transform
      .DOScale(1.3f, 0.3f).SetEase(Ease.OutBack).WaitForCompletion();

    yield return new WaitForSeconds(1f);

    if (FallingJackpotImage)
    {
      FallingJackpotImage.sprite = GetJackpotSprite(goalJackpot);
      FallingJackpotImage.preserveAspect = true;
    }

    for (int i = 0; i < JackpotRevealImages.Length; i++)
      if (i != _selectedPinataIndex)
        JackpotRevealImages[i].DOFade(0f, 0.5f);

    RectTransform selectedRT = JackpotRevealImages[_selectedPinataIndex].rectTransform;
    yield return selectedRT.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.InOutCubic).WaitForCompletion();

    for (int i = 0; i < JackpotRevealImages.Length; i++)
      if (i != _selectedPinataIndex)
        JackpotRevealImages[i].gameObject.SetActive(false);

    if (FreeSpinsUntilImage) FreeSpinsUntilImage.gameObject.SetActive(true);

    yield return new WaitForSeconds(2f);

    if (FallingJackpotRT)
    {
      FallingJackpotRT.gameObject.SetActive(true);
      FallingJackpotRT.anchoredPosition = Vector2.zero;
      FallingJackpotRT.localEulerAngles = Vector3.zero;
    }

    // panel exits fast, don't await so jackpot animates simultaneously
    if (PickJackpotPanel)
      PickJackpotPanel.GetComponent<RectTransform>()
        .DOAnchorPosY(PickJackpotPanel.GetComponent<RectTransform>().anchoredPosition.y + offscreenOffset, 0.4f)
        .SetEase(Ease.InBack);

    if (FallingJackpotRT)
    {
      // launch up near top while rotating 180° CW
      yield return DOTween.Sequence()
        .Append(FallingJackpotRT.DOAnchorPosY(jackpotLaunchHeight, 0.6f).SetEase(Ease.OutQuad))
        .Join(FallingJackpotRT.DOLocalRotate(new Vector3(0, 0, -180), 0.6f, RotateMode.LocalAxisAdd))
        .WaitForCompletion();

      // at apex: reels slide back up in parallel with the jackpot falling
      StartCoroutine(SlideContentUp());

      // fall to landing point while rotating another 180° CW, ends right-side up
      float landingY = JackpotLandingPoint != null
        ? ((RectTransform)JackpotLandingPoint).anchoredPosition.y : 0;
      yield return DOTween.Sequence()
        .Append(FallingJackpotRT.DOAnchorPosY(landingY, 0.5f).SetEase(Ease.InQuad))
        .Join(FallingJackpotRT.DOLocalRotate(new Vector3(0, 0, -180), 0.5f, RotateMode.LocalAxisAdd))
        .WaitForCompletion();
    }

    if (PickJackpotPanel) PickJackpotPanel.SetActive(false);
  }

  private Sprite GetJackpotSprite(string jackpotName)
  {
    switch (jackpotName)
    {
      case "mini": return MiniJackpotSprite;
      case "minor": return MinorJackpotSprite;
      case "major": return MajorJackpotSprite;
      case "mega": return MegaJackpotSprite;
      case "grand": return GrandJackpotSprite;
      default: return null;
    }
  }

  private void UpdateBetDisplay(double bet)
  {
    if (TotalBetAmountText) TotalBetAmountText.text = bet.ToString("F2");
    if (MiniPayoutText) MiniPayoutText.text = (bet * jackpotMultipliers[0]).ToString("F2");
    if (MinorPayoutText) MinorPayoutText.text = (bet * jackpotMultipliers[1]).ToString("F2");
    if (MajorPayoutText) MajorPayoutText.text = (bet * jackpotMultipliers[2]).ToString("F2");
    if (MegaPayoutText) MegaPayoutText.text = (bet * jackpotMultipliers[3]).ToString("F2");
    if (GrandPayoutText) GrandPayoutText.text = (bet * jackpotMultipliers[4]).ToString("F2");
    if (WheelBonusMiniText) WheelBonusMiniText.text = (bet * jackpotMultipliers[0]).ToString("F2");
    if (WheelBonusMinorText) WheelBonusMinorText.text = (bet * jackpotMultipliers[1]).ToString("F2");
    if (WheelBonusMajorText) WheelBonusMajorText.text = (bet * jackpotMultipliers[2]).ToString("F2");
    if (WheelBonusMegaText) WheelBonusMegaText.text = (bet * jackpotMultipliers[3]).ToString("F2");
    if (WheelBonusGrandText) WheelBonusGrandText.text = (bet * jackpotMultipliers[4]).ToString("F2");
  }

  private void ShowSlide(int index)
  {
    if (SlideContainer && InfoSlides != null && InfoSlides.Length > 0)
      SlideContainer.sprite = InfoSlides[index];
  }

  // TODO: replace with full jackpot win screen (win graphic, amount display, etc.)
  internal IEnumerator ShowJackpotWin(string jackpotTier, double winAmount)
  {
    if (FallingJackpotRT) FallingJackpotRT.gameObject.SetActive(false);
    PopulateWin(4, winAmount);
    yield return new WaitUntil(() => WinPopup_Object == null || !WinPopup_Object.activeSelf);
  }
}
