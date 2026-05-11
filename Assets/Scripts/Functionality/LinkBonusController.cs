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

  [Header("Symbols — same order as SlotBehaviour myImages")]
  [SerializeField] private Sprite[] symbolSprites;

  [Header("Setup Animation")]
  [SerializeField] private RectTransform BlueCircle;
  [SerializeField] private Transform LinkBonusGridCenter;
  [SerializeField] private float blueCircleBigScale = 3f;
  [SerializeField] private float blueCircleRevealDuration = 0.6f;
  [SerializeField] private float zoneRevealDelay = 0.3f;
  [SerializeField] private float flickerDuration = 1.5f;
  [SerializeField] private float flickerInterval = 0.1f;

  [Header("Spin Settings")]
  [SerializeField] private float cellStopStagger = 0.08f;

  [Header("Total Win")]
  [SerializeField] private GameObject TotalWinPanel;
  [SerializeField] private TMP_Text TotalWinText;
  [SerializeField] private RectTransform TotalWinTarget;
  [SerializeField] private float winFlyDuration = 0.4f;
  [SerializeField] private float winFlyStagger = 0.1f;

  private List<LockedCell> _currentLockedCells = new List<LockedCell>();

  private LinkBonusCell GetCell(int row, int col) => cells[row * 5 + col];

  private Sprite GetSprite(int symbolId)
  {
    if (symbolId >= 0 && symbolId < symbolSprites.Length) return symbolSprites[symbolId];
    return symbolSprites.Length > 2 ? symbolSprites[2] : null;
  }

  private Sprite GetSprite(string symbolId)
  {
    return int.TryParse(symbolId, out int id) ? GetSprite(id) : GetSprite(2);
  }

  public IEnumerator StartLinkBonus(List<LinkBonusZone> targetZones)
  {
    _currentLockedCells.Clear();
    if (TotalWinPanel) TotalWinPanel.SetActive(false);
    if (BlueCircle) BlueCircle.gameObject.SetActive(false);
    if (LinkBonusGrid) LinkBonusGrid.SetActive(true);

    foreach (var cell in cells)
      cell.Initialize(GetSprite(2));

    foreach (var cell in cells)
      cell.StartSpin();

    yield return StartCoroutine(PlayFlickerAnimation());

    if (targetZones != null)
      foreach (var zone in targetZones)
      {
        yield return StartCoroutine(RevealZone(zone));
        yield return new WaitForSeconds(zoneRevealDelay);
      }
  }

  private IEnumerator PlayFlickerAnimation()
  {
    float elapsed = 0f;
    while (elapsed < flickerDuration)
    {
      cells[Random.Range(0, cells.Length)].ShowPreSpinGlow();
      yield return new WaitForSeconds(flickerInterval);
      foreach (var cell in cells) cell.HidePreSpinGlow();
      elapsed += flickerInterval * 2f;
    }
  }

  private IEnumerator RevealZone(LinkBonusZone zone)
  {
    if (BlueCircle == null) yield break;

    LinkBonusCell targetCell = GetCell(zone.position[0], zone.position[1]);
    Vector3 centerPos = LinkBonusGridCenter != null ? LinkBonusGridCenter.position : transform.position;

    BlueCircle.gameObject.SetActive(true);
    BlueCircle.position = centerPos;
    BlueCircle.localScale = Vector3.one * blueCircleBigScale;

    BlueCircle.DOMove(targetCell.CellRect.position, blueCircleRevealDuration).SetEase(Ease.InCubic);
    yield return BlueCircle.DOScale(Vector3.one, blueCircleRevealDuration).SetEase(Ease.InCubic).WaitForCompletion();

    BlueCircle.gameObject.SetActive(false);
    targetCell.ShowZoneMultiplier(zone.zoneMultiplier);
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

  public IEnumerator StopCellsSequential(List<List<string>> matrix, List<LockedCell> lockedCells)
  {
    for (int col = 0; col < 5; col++)
    {
      for (int row = 0; row < 3; row++)
      {
        LinkBonusCell cell = GetCell(row, col);

        if (!cell.IsLocked)
        {
          int symbolId = int.Parse(matrix[row][col]);
          double? prizeValue = lockedCells?.Find(c => c.position[0] == row && c.position[1] == col)?.prizeValue;

          cell.TriggerFlashPinkGlow();
          cell.StopAt(GetSprite(symbolId), prizeValue);
        }

        yield return new WaitForSeconds(cellStopStagger);
      }
    }
  }

  public void UpdateLockedCells(List<LockedCell> newLockedCells)
  {
    if (newLockedCells == null) return;
    foreach (var locked in newLockedCells)
    {
      bool alreadyLocked = _currentLockedCells.Exists(
        c => c.position[0] == locked.position[0] && c.position[1] == locked.position[1]);
      if (!alreadyLocked)
        GetCell(locked.position[0], locked.position[1]).Freeze();
    }
    _currentLockedCells = new List<LockedCell>(newLockedCells);
  }

  public IEnumerator PlayTotalWinSequence(List<LockedCell> allLockedCells, double totalWin)
  {
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
        LinkBonusCell cell = GetCell(locked.position[0], locked.position[1]);

        GameObject copy = new GameObject("WinFlyCopy");
        copy.transform.SetParent(transform.root, false);
        Image copyImg = copy.AddComponent<Image>();
        copyImg.sprite = GetSprite(locked.symbolId ?? "2");
        RectTransform copyRT = copy.GetComponent<RectTransform>();
        copyRT.sizeDelta = new Vector2(80f, 80f);
        copyRT.position = cell.CellRect.position;

        cell.DimCell();

        double startVal = runningTotal;
        if (locked.prizeValue.HasValue) runningTotal += locked.prizeValue.Value;
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
  }
}
