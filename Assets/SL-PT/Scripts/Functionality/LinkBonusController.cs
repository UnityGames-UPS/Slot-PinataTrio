using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LinkBonusController : MonoBehaviour
{
  // Cells indexed as [row * 5 + col]. Rows 0-2 top to bottom, cols 0-4 left to right.
  // Element 0 = row0/col0, Element 1 = row0/col1 ... Element 14 = row2/col4
  [Header("Cell Grid")]
  [SerializeField] private LinkBonusCell[] cells;
  [SerializeField] private GameObject LinkBonusGrid;
  [SerializeField] private SlotView slotView;

  [Header("Symbols — same order as SlotView myImages")]
  [SerializeField] private Sprite[] symbolSprites;

  [Header("Setup Animation")]
  [SerializeField] private RectTransform BlueCircle;
  [SerializeField] private Transform LinkBonusGridCenter;
  [SerializeField] private float blueCircleBigScale = 3f;
  [SerializeField] private float blueCircleRevealDuration = 0.6f;
  [SerializeField] private float zoneRevealDelay = 0.3f;
  [SerializeField] private float flickerDuration = 1.5f;
  [SerializeField] private float flickerInterval = 0.1f;
  [SerializeField] private float preSpinGlowDuration = 0.5f;
  [SerializeField] private int flickerMinCount = 2;
  [SerializeField] private int flickerMaxCount = 4;

  [Header("Spin Settings")]
  [SerializeField] private float cellStopStagger = 0.08f;

  [Header("Total Win")]
  [SerializeField] private GameObject TotalWinPanel;
  [SerializeField] private TMP_Text TotalWinText;
  [SerializeField] private RectTransform TotalWinTarget;
  [SerializeField] private RectTransform SpinRemainingArea;
  [SerializeField] private float spinAreaSlideDistance = 260f;
  [SerializeField] private RectTransform TotalWinFrame;
  [SerializeField] private float totalWinSlideDistance = 100f;
  [SerializeField] private float winFlyDuration = 0.4f;
  [SerializeField] private float winFlyStagger = 0.1f;

  private List<LinkCell> _currentLockedCells = new List<LinkCell>();
  private Vector2 _spinRemainingAreaOriginalPos;
  private Vector2 _totalWinFrameOriginalPos;

  private LinkBonusCell GetCell(int row, int col) => cells[row * 5 + col];

  private Sprite GetSprite(int symbolId)
  {
    if (symbolId >= 0 && symbolId < symbolSprites.Length) return symbolSprites[symbolId];
    return symbolSprites.Length > 2 ? symbolSprites[2] : null;
  }

  internal IEnumerator StartLinkBonus(List<LinkZone> targetZones)
  {
    if (SpinRemainingArea) _spinRemainingAreaOriginalPos = SpinRemainingArea.anchoredPosition;
    if (TotalWinFrame) _totalWinFrameOriginalPos = TotalWinFrame.anchoredPosition;

    _currentLockedCells.Clear();
    if (TotalWinPanel) TotalWinPanel.SetActive(false);
    if (BlueCircle) BlueCircle.gameObject.SetActive(false);
    if (LinkBonusGrid) LinkBonusGrid.SetActive(true);

    foreach (var cell in cells)
      cell.Initialize(GetSprite(2));

    yield return StartCoroutine(PlayFlickerAnimation());

    if (targetZones != null)
      foreach (var zone in targetZones)
      {
        yield return StartCoroutine(RevealZone(zone));
        yield return new WaitForSeconds(zoneRevealDelay);
      }

    foreach (var cell in cells) cell.ShowPreSpinGlow();
    yield return new WaitForSeconds(preSpinGlowDuration);
    foreach (var cell in cells) cell.HidePreSpinGlow();
  }

  private IEnumerator PlayFlickerAnimation()
  {
    float elapsed = 0f;
    List<int> indices = new List<int>();
    for (int i = 0; i < cells.Length; i++) indices.Add(i);

    while (elapsed < flickerDuration)
    {
      int count = Random.Range(flickerMinCount, flickerMaxCount + 1);
      for (int i = indices.Count - 1; i > 0; i--)
      {
        int j = Random.Range(0, i + 1);
        int temp = indices[i]; indices[i] = indices[j]; indices[j] = temp;
      }
      for (int i = 0; i < count; i++)
        cells[indices[i]].ShowFlickerZone();

      yield return new WaitForSeconds(flickerInterval);
      foreach (var cell in cells) cell.HideFlickerZone();
      elapsed += flickerInterval * 2f;
    }
  }

  private IEnumerator RevealZone(LinkZone zone)
  {
    if (BlueCircle == null) yield break;

    LinkBonusCell targetCell = GetCell(zone.Row, zone.Col);
    Vector3 centerPos = LinkBonusGridCenter != null ? LinkBonusGridCenter.position : transform.position;

    BlueCircle.gameObject.SetActive(true);
    BlueCircle.position = centerPos;
    BlueCircle.localScale = Vector3.one * blueCircleBigScale;

    BlueCircle.DOMove(targetCell.CellRect.position, blueCircleRevealDuration).SetEase(Ease.InCubic);
    yield return BlueCircle.DOScale(Vector3.one, blueCircleRevealDuration).SetEase(Ease.InCubic).WaitForCompletion();

    BlueCircle.gameObject.SetActive(false);
    targetCell.ShowZoneMultiplier(zone.Multiplier);
  }

  public void ShowPreSpinGlow()
  {
    foreach (var cell in cells) cell.ShowPreSpinGlow();
  }

  public void HidePreSpinGlow()
  {
    foreach (var cell in cells) cell.HidePreSpinGlow();
  }

  public void StartSpinRound()
  {
    foreach (var cell in cells)
      if (!cell.IsLocked) cell.StartSpin();
  }

  // matrix is the round's board, already parsed ([3,5], row-major); null if the response had none.
  internal IEnumerator StopCellsSequential(int[,] matrix, List<LinkCell> lockedCells)
  {
    if (matrix == null) yield break;
    for (int col = 0; col < 5; col++)
    {
      for (int row = 0; row < 3; row++)
      {
        LinkBonusCell cell = GetCell(row, col);

        if (!cell.IsLocked)
        {
          int symbolId = matrix[row, col];
          double? prizeValue = lockedCells?.Find(c => c.Row == row && c.Col == col)?.Prize;

          bool hasPrize = prizeValue.HasValue && prizeValue.Value > 0;
          bool isJackpot = symbolId >= 3 && symbolId <= 7;
          Sprite displaySprite = (hasPrize || isJackpot) ? GetSprite(symbolId) : GetSprite(2);

          if (isJackpot && slotView != null)
            slotView.PopulateAnimationSprites(cell.CellAnim, symbolId);

          cell.TriggerFlashPinkGlow();
          cell.StopAt(displaySprite, prizeValue, isJackpot);
        }

        yield return new WaitForSeconds(cellStopStagger);
      }
    }
  }

  internal void UpdateLockedCells(List<LinkCell> newLockedCells)
  {
    if (newLockedCells == null) return;
    foreach (var locked in newLockedCells)
    {
      bool alreadyLocked = _currentLockedCells.Exists(
        c => c.Row == locked.Row && c.Col == locked.Col);
      if (!alreadyLocked)
        GetCell(locked.Row, locked.Col).Freeze();
    }
    _currentLockedCells = new List<LinkCell>(newLockedCells);
  }

  internal IEnumerator PlayTotalWinSequence(List<LinkCell> allLockedCells, double totalWin)
  {
    if (SpinRemainingArea)
    {
      SpinRemainingArea.DOAnchorPosY(SpinRemainingArea.anchoredPosition.y - spinAreaSlideDistance, 0.4f).SetEase(Ease.InBack);
      if (TotalWinFrame)
        yield return TotalWinFrame.DOAnchorPosY(TotalWinFrame.anchoredPosition.y + totalWinSlideDistance, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();
      else
        yield return new WaitForSeconds(0.4f);
    }

    if (TotalWinPanel) TotalWinPanel.SetActive(true);
    if (TotalWinText) TotalWinText.text = "0.000";

    Vector3 targetPos = TotalWinTarget != null
      ? TotalWinTarget.position
      : (TotalWinPanel != null ? TotalWinPanel.transform.position : Vector3.zero);

    double runningTotal = 0;

    if (allLockedCells != null)
    {
      foreach (var locked in allLockedCells)
      {
        LinkBonusCell cell = GetCell(locked.Row, locked.Col);

        GameObject copy = new GameObject("WinFlyCopy");
        copy.transform.SetParent(transform.root, false);
        Image copyImg = copy.AddComponent<Image>();
        copyImg.sprite = GetSprite(locked.SymbolId);
        RectTransform copyRT = copy.GetComponent<RectTransform>();
        copyRT.sizeDelta = new Vector2(80f, 80f);
        copyRT.position = cell.CellRect.position;

        cell.DimCell();

        double startVal = runningTotal;
        if (locked.Prize.HasValue) runningTotal += locked.Prize.Value;
        double endVal = runningTotal;

        copyRT.DOMove(targetPos, winFlyDuration).SetEase(Ease.InOutQuad).OnComplete(() => Destroy(copy));

        DOTween.To(
          () => (float)startVal,
          v => { if (TotalWinText) TotalWinText.text = v.ToString("F3"); },
          (float)endVal,
          winFlyDuration);

        yield return new WaitForSeconds(winFlyDuration + winFlyStagger);
      }
    }

    if (TotalWinText) TotalWinText.text = totalWin.ToString("F3");
    yield return new WaitForSeconds(2f);
  }

  public void ResetAll()
  {
    foreach (var cell in cells) cell.ResetCell();
    _currentLockedCells.Clear();
    if (LinkBonusGrid) LinkBonusGrid.SetActive(false);
    if (TotalWinPanel) TotalWinPanel.SetActive(false);
    if (BlueCircle) BlueCircle.gameObject.SetActive(false);
    if (SpinRemainingArea) SpinRemainingArea.anchoredPosition = _spinRemainingAreaOriginalPos;
    if (TotalWinFrame) TotalWinFrame.anchoredPosition = _totalWinFrameOriginalPos;
  }
}
