using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
  [Header("Info Slides")]
  [SerializeField]
  private Button Info_Button;
  [SerializeField]
  private GameObject InfoSlidesPanel;
  [SerializeField]
  private Button BackToGameButton;
  [SerializeField]
  private Button PrevButton;
  [SerializeField]
  private Button NextButton;

  private int currentSlideIndex = 0;
  private Texture2D[] slides = new Texture2D[6];
  [SerializeField]
  private RawImage slideImage;
  [SerializeField]
  private TextMeshProUGUI pageIndicator;

  [Header("Main Popup")]
  [SerializeField]
  private GameObject MainPopup_Object;

  [Header("Audio")]
  [SerializeField]
  private AudioController audioController;

  

  private void Start()
  {
    // Load slides from Resources folder
    LoadSlides();

    // Info Button listener
    if (Info_Button) Info_Button.onClick.RemoveAllListeners();
    if (Info_Button) Info_Button.onClick.AddListener(delegate { OpenPopup(InfoSlidesPanel); });

    // Back to Game Button listener
    if (BackToGameButton) BackToGameButton.onClick.RemoveAllListeners();
    if (BackToGameButton) BackToGameButton.onClick.AddListener(delegate { ClosePopup(InfoSlidesPanel); });

    // Previous Slide Button listener
    if (PrevButton) PrevButton.onClick.RemoveAllListeners();
    if (PrevButton) PrevButton.onClick.AddListener(PreviousSlide);

    // Next Slide Button listener
    if (NextButton) NextButton.onClick.RemoveAllListeners();
    if (NextButton) NextButton.onClick.AddListener(NextSlide);
  }

  private void LoadSlides()
  {
    for (int i = 0; i < 6; i++)
    {
      slides[i] = Resources.Load<Texture2D>($"Graphics/Slides/Slide{i + 1}");
    }
    ShowSlide(0);
  }

  private void OpenPopup(GameObject popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (popup) popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  private void ClosePopup(GameObject popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (popup) popup.SetActive(false);
    if (MainPopup_Object) MainPopup_Object.SetActive(false);
  }

  private void ShowSlide(int index)
  {
    currentSlideIndex = Mathf.Clamp(index, 0, 5);
    if (slideImage) slideImage.texture = slides[currentSlideIndex];
    if (pageIndicator) pageIndicator.text = (currentSlideIndex + 1) + "/6";

    // Update button interactability
    if (PrevButton) PrevButton.interactable = currentSlideIndex > 0;
    if (NextButton) NextButton.interactable = currentSlideIndex < 5;
  }

  private void NextSlide()
  {
    if (currentSlideIndex < 5)
    {
      ShowSlide(currentSlideIndex + 1);
    }
  }

  private void PreviousSlide()
  {
    if (currentSlideIndex > 0)
    {
      ShowSlide(currentSlideIndex - 1);
    }
  }
}