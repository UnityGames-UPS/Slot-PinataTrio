using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BonusController : MonoBehaviour
{
  // GREEN PIÑATA WHEEL BONUS — 6 SEGMENTS MATCHING pinata_config.json wheelBonus.segments
  // INDEX ORDER: 0=mini, 1=minor, 2=major, 3=mega, 4=grand, 5=boost
  private static readonly string[] SegmentNames = { "mini", "minor", "major", "mega", "grand", "boost" };
  private const float SegmentAngle = 360f / 6f;

  // VIKING GAME - MANUAL SPIN BUTTON - NOT USED IN THIS GAME
  // [SerializeField] private Button Spin_Button;

  [SerializeField] private RectTransform Wheel_Transform;

  // VIKING GAME - COLLIDER-BASED STOPPING SYSTEM - NOT USED IN THIS GAME
  // [SerializeField] private BoxCollider2D[] point_colliders;
  // [SerializeField] private TMP_Text[] Bonus_Text;

  [SerializeField] private TMP_Text WinAmountText;
  [SerializeField] private GameObject Bonus_Object;
  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private AudioController _audioManager;
  [SerializeField] private GameObject PopupPanel;
  [SerializeField] private Transform Win_Transform;
  [SerializeField] private Transform Boost_Transform;

  // VIKING GAME - SOCKET MANAGER USED FOR bonusdata AND ResultData.bonus — NOT USED IN THIS GAME
  // [SerializeField] private SocketIOManager m_SocketManager;

  [SerializeField] private UIManager uIManager;

  // VIKING GAME - COLLISION FLAG FOR COLLIDER-BASED WHEEL STOP - NOT USED IN THIS GAME
  // internal bool isCollision = false;

  internal bool isBonusDone = false;

  private Tween wheelRoutine;
  private int stopIndex = 0;

  // VIKING GAME - SPIN BUTTON LISTENER SETUP - NOT USED IN THIS GAME (WHEEL AUTO-TRIGGERS FROM SlotBehaviour)
  // private void Start()
  // {
  //   if (Spin_Button) Spin_Button.onClick.RemoveAllListeners();
  //   if (Spin_Button) Spin_Button.onClick.AddListener(Spinbutton);
  // }

  internal void StartWheelBonus(int segmentIndex, double winAmount)
  {
    isBonusDone = false;
    stopIndex = segmentIndex;
    if (PopupPanel) PopupPanel.SetActive(false);
    if (Win_Transform) Win_Transform.gameObject.SetActive(false);
    if (Boost_Transform) Boost_Transform.gameObject.SetActive(false);
    if (WinAmountText) WinAmountText.text = winAmount.ToString("F3");
    if (_audioManager) _audioManager.SwitchBGSound(true);
    if (Bonus_Object) Bonus_Object.SetActive(true);
    RotateWheel();
    DOVirtual.DelayedCall(2f, () => StopWheel());
  }

  internal int GetSegmentIndex(string segmentType)
  {
    for (int i = 0; i < SegmentNames.Length; i++)
    {
      if (SegmentNames[i] == segmentType) return i;
    }
    return 0;
  }

  // VIKING GAME - MANUAL SPIN BUTTON HANDLER - NOT USED IN THIS GAME
  // private void Spinbutton()
  // {
  //   isCollision = false;
  //   if (Spin_Button) Spin_Button.interactable = false;
  //   RotateWheel();
  //   DOVirtual.DelayedCall(1.5f, () => TurnCollider(stopIndex));
  // }

  // VIKING GAME - WHEEL SEGMENT POPULATION FROM SOCKET bonusdata - NOT USED IN THIS GAME
  // internal void PopulateWheel(List<string> bonusdata) { ... }

  private void RotateWheel()
  {
    if (Wheel_Transform) Wheel_Transform.localEulerAngles = Vector3.zero;
    if (Wheel_Transform) wheelRoutine = Wheel_Transform.DORotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear).SetLoops(-1);
    if (_audioManager) _audioManager.PlayBonusAudio("cycleSpin");
  }

  // VIKING GAME - COLLIDER RESET AND ENABLE - NOT USED IN THIS GAME
  // private void ResetColliders() { ... }
  // private void TurnCollider(int point) { ... }

  private void StopWheel()
  {
    if (wheelRoutine != null) wheelRoutine.Kill();

    // SPIN 3 EXTRA FULL ROTATIONS THEN LAND ON THE TARGET SEGMENT ANGLE
    float targetAngle = -(stopIndex * SegmentAngle) - (360f * 3);
    if (Wheel_Transform) Wheel_Transform.DORotate(new Vector3(0, 0, targetAngle), 2f, RotateMode.FastBeyond360)
        .SetEase(Ease.OutCubic)
        .OnComplete(OnWheelStopped);
  }

  private void OnWheelStopped()
  {
    bool isBoost = SegmentNames[stopIndex] == "boost";
    if (!isBoost)
    {
      if (Win_Transform) Win_Transform.gameObject.SetActive(true);
      if (Win_Transform) Win_Transform.localScale = Vector3.zero;
      if (PopupPanel) PopupPanel.SetActive(true);
      if (Win_Transform) Win_Transform.DOScale(Vector3.one, 1f);
    }
    else
    {
      // BOOST SEGMENT: NO JACKPOT WIN — GREEN METER GETS BOOSTED ON THE SERVER SIDE
      if (Boost_Transform) Boost_Transform.gameObject.SetActive(true);
      if (Boost_Transform) Boost_Transform.localScale = Vector3.zero;
      if (PopupPanel) PopupPanel.SetActive(true);
      if (Boost_Transform) Boost_Transform.DOScale(Vector3.one, 1f);
    }
    PlayWinLooseSound(!isBoost);

    DOVirtual.DelayedCall(1.5f, () =>
    {
      if (_audioManager) _audioManager.SwitchBGSound(false);
      if (Bonus_Object) Bonus_Object.SetActive(false);
      isBonusDone = true;
    });
  }

  internal void PlayWinLooseSound(bool isWin)
  {
    if (isWin)
    {
      if (_audioManager) _audioManager.PlayBonusAudio("win");
    }
    else
    {
      if (_audioManager) _audioManager.PlayBonusAudio("boost");
    }
  }
}
