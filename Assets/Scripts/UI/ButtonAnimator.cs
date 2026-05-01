using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
  private RectTransform buttonRect;
  private Button button;
  private Vector3 originalPosition;
  private float originalScale;
  private Coroutine animCoroutine;

  private void Start()
  {
    buttonRect = transform as RectTransform;
    originalPosition = buttonRect.anchoredPosition;
    originalScale = buttonRect.localScale.x;
    button = GetComponent<Button>();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (button.interactable)
      AnimatePress(new Vector3(0, -8, 0), 0.9f, 0.08f);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (button.interactable)
      AnimateRelease(originalPosition, originalScale, 0.08f);
  }

  void AnimatePress(Vector3 offsetPosition, float targetScale, float duration)
  {
    if (animCoroutine != null)
      StopCoroutine(animCoroutine);
    animCoroutine = StartCoroutine(AnimateCoroutine(originalPosition + offsetPosition, targetScale, duration));
  }

  void AnimateRelease(Vector3 targetPosition, float targetScale, float duration)
  {
    if (animCoroutine != null)
      StopCoroutine(animCoroutine);
    animCoroutine = StartCoroutine(AnimateCoroutine(targetPosition, targetScale, duration));
  }

  IEnumerator AnimateCoroutine(Vector3 targetPosition, float targetScale, float duration)
  {
    Vector3 startPosition = buttonRect.anchoredPosition;
    float startScale = buttonRect.localScale.x;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      
      buttonRect.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t);
      buttonRect.localScale = Vector3.Lerp(new Vector3(startScale, startScale, startScale), 
                                           new Vector3(targetScale, targetScale, targetScale), t);
      yield return null;
    }

    buttonRect.anchoredPosition = targetPosition;
    buttonRect.localScale = new Vector3(targetScale, targetScale, targetScale);
  }
}