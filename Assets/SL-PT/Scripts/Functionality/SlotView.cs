using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

// The board: the five reel strips, the icons on them, and every animation that plays over them.
// It knows nothing about rounds, bets or features. GameManager drives it through the internal
// methods in "Board API" and hands it each spin's result through SetResult. The board never reads
// the server's data itself.
public class SlotView : MonoBehaviour
{
  [Header("Sprites")]
  [SerializeField]
  private Sprite[] myImages;  // reel icon per symbol id

  [Header("Slot Images")]
  [SerializeField]
  private List<SlotImage> Tempimages;  // the three visible reel icons per reel, and their coin texts
  [SerializeField]
  private List<SlotImage> Animimages;  // the AnimationLayer overlay cells per reel, and their coin texts

  [Header("Slots Transforms")]
  [SerializeField]
  private Transform[] Slot_Transform;

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

  [Header("Reel Geometry")]
  // Serialized: the scene's value (130) overrides this default. It sets both the spin loop distance
  // and the pre-land snap height — see ToDo.md (spin stutter) before changing it.
  [SerializeField]
  private int IconSizeFactor = 160;

  [Header("References")]
  [SerializeField]
  private AudioManager audioManager;
  [SerializeField]
  private UIManager uiManager;

  private int tweenHeight = 0;
  private readonly List<Tweener> alltweens = new List<Tweener>();
  private int _activePinataAnims = 0;

  // This spin's result, captured once by SetResult before any reel lands, so nothing that draws a
  // landing ever reads the server's data.
  private int[,] currentMatrix;
  private List<CoinWin> currentCoins;

  private int[,] initialMatrix = new int[,]
{
  { 0, 0, 7, 0, 0 },  // row 0 (top):    grand, mega, major, minor, mini
  { 0, 6, 0, 5, 0 },  // row 1 (middle): yellow bubbles
  { 3, 0, 0, 0, 4 }   // row 2 (bottom): yellow bubbles
};

  private void Start()
  {
    tweenHeight = (15 * IconSizeFactor) - 280;
  }

  #region BoardAPI
  // The board the game opens on, before the first spin.
  internal void ShowInitialBoard()
  {
    for (int row = 0; row < RowCount; row++)
    {
      for (int col = 0; col < ReelCount; col++)
      {
        int val = initialMatrix[row, col];
        ApplyReelIcon(Tempimages[col].slotImages[row], val);
        if (IsJackpotSymbol(val)) ShowOverlay(col, row, val);
      }
    }
    RandomizeOffscreenIcons();
  }

  // Clears the previous spin's presentation and refreshes the off-screen icons, before the reels move.
  internal void PrepareSpin()
  {
    HideAllOverlays();
    RandomizeOffscreenIcons();
  }

  // Starts the five reels spinning, 0.1 s apart.
  internal IEnumerator StartReels()
  {
    _activePinataAnims = 0;
    for (int i = 0; i < ReelCount; i++)
    {
      InitializeTweening(Slot_Transform[i]);
      yield return new WaitForSeconds(0.1f);
    }
  }

  // Abandons a spin that got no usable result.
  internal void CancelSpin()
  {
    KillAllTweens();
  }

  // Takes this spin's result: the row-major symbol matrix and the coin values. Writes the visible
  // reel icons now (the reels are still spinning) and keeps both for the landing.
  internal void SetResult(int[,] matrix, List<CoinWin> coins)
  {
    currentMatrix = matrix;
    currentCoins = coins;
    for (int row = 0; row < RowCount; row++)
    {
      for (int col = 0; col < ReelCount; col++)
      {
        ApplyReelIcon(Tempimages[col].slotImages[row], matrix[row, col]);
      }
    }
  }

  // Lands the reels left to right, `stagger` seconds apart.
  internal IEnumerator StopReels(float stagger)
  {
    for (int i = 0; i < ReelCount; i++)
    {
      yield return StopTweening(Slot_Transform[i], i, stagger);
    }
  }

  // Until the last reel's land tween has finished.
  internal IEnumerator WaitForLastReel()
  {
    yield return alltweens[^1].WaitForCompletion();
  }

  // Until every pinata has reached its meter. Then the reel tweens are released.
  internal IEnumerator WaitForLandingEffects()
  {
    yield return new WaitUntil(() => _activePinataAnims == 0);
    KillAllTweens();
  }
  #endregion

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

  #region GameAnimation
  // Runs once the whole board has landed and every pinata has reached its meter. Jackpot symbols
  // animate here (owner: they wait for the full board). Coin cells were already started as their
  // reel landed, and ShowOverlay never restarts a cell, so they are not touched again.
  internal void ShowJackpotOverlays()
  {
    bool hasJackpot = false;

    for (int row = 0; row < RowCount; row++)
    {
      for (int col = 0; col < ReelCount; col++)
      {
        int val = currentMatrix[row, col];
        if (!IsJackpotSymbol(val)) continue;

        hasJackpot = true;
        if (ShowOverlay(col, row, val) && audioManager) audioManager.PlayJackpotIcon();
      }
    }

    bool hasCoinValueAnywhere = currentCoins != null && currentCoins.Count > 0;
    if (hasCoinValueAnywhere && !hasJackpot && audioManager) audioManager.PlayNormalIcon();
  }
  #endregion

  #region OverlayCells
  // Every animation on the board plays on the static AnimationLayer cells (Animimages), never on the
  // moving reel icons (Tempimages). Each cell is shown only through ShowOverlay and torn down only
  // through HideAllOverlays, so it is set up the same way every time (frames, speed, size, position)
  // and cleared completely, whatever it was doing.

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
  internal void ReplayOverlays()
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
    var coins = currentCoins;
    if (coins == null || col >= Tempimages.Count) return;
    foreach (var coin in coins)
    {
      if (coin.Col != col) continue;
      int row = coin.Row;
      if (row < Tempimages[col].coinValueTexts.Count)
      {
        TMP_Text txt = Tempimages[col].coinValueTexts[row];
        if (txt) { txt.text = coin.Value.ToString("F2"); txt.gameObject.SetActive(true); if (audioManager) audioManager.PlayCoinValueAppear(); }
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
      int val = currentMatrix[row, col];
      if (IsPinataSymbol(val))
        flyRoutines.Add(StartCoroutine(FlyPinataToMeter(col, row, val)));
    }
    foreach (var r in flyRoutines) yield return r;
    _activePinataAnims--;
  }

  // Coin cells animate as soon as their reel has landed.
  private void ShowCoinValuesForColumn(int col)
  {
    var coins = currentCoins;
    if (coins == null) return;
    foreach (var coin in coins)
    {
      if (coin.Col != col) continue;
      int row = coin.Row;
      int symbolId = currentMatrix[row, col];

      // A symbol with no frames switches nothing on. The reel icon stays, with the coin text it was
      // given as the reel landed (ShowTempCoinValuesForColumn).
      if (!ShowOverlay(col, row, symbolId)) continue;

      TMP_Text txt = GetCoinText(Animimages, col, row);
      if (txt) { txt.text = coin.Value.ToString("F2"); txt.gameObject.SetActive(true); }
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
    for (int i = 0; i < ReelCount; i++)
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
