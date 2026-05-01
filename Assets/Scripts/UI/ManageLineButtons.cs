using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ManageLineButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{

  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private TMP_Text num_text;

  public void OnPointerEnter(PointerEventData eventData) { }
  public void OnPointerExit(PointerEventData eventData) { }
  public void OnPointerDown(PointerEventData eventData) { }
  public void OnPointerUp(PointerEventData eventData) { }
}
