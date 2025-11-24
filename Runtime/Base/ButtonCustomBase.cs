using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup),typeof(Image))]
public abstract class ButtonCustomBase : MonoBehaviour, IButton
{
    [SerializeField] private bool isButton;
    [SerializeField] private float distanceOnClick;
    [SerializeField] protected bool interactable = true;
    [SerializeField] private EButtonType buttonType;
    [SerializeField] private CanvasGroup _group;
    private Image _currentImage;
    private float _lastClickTime;

    private UnityEvent OnClickEvent = new();

    public ColorButtonSettings colorButtonSettings = new();

#if  UNITY_EDITOR
    private void OnValidate()
    {
        _currentImage = GetComponent<Image>();
        _group = GetComponent<CanvasGroup>();
    }
    
#endif
    public void SetActive(bool state)
    {
        _group.blocksRaycasts = state;
        _group.alpha = state ? 1f : 0f;
    }
    
    public void SetActive(bool state,bool blockRaycast)
    {
        _group.blocksRaycasts = blockRaycast;
        _group.alpha = state ? 1f : 0f;
    }

    public void AddListener(UnityAction listener)
    {
        OnClickEvent.AddListener(listener);
    }
    public bool CanClick()
    {
        Debug.Log($"{Time.time}---{_lastClickTime}---{Time.time - _lastClickTime}---{Time.time - _lastClickTime < distanceOnClick}");
        if (Time.time - _lastClickTime < distanceOnClick)
            return false;

        _lastClickTime = Time.time;
        return true;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        OnInteractionButton();
        if(!interactable) return;
        if(!CanClick()) return;
        OnClickEvent.Invoke();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        OnResetInteractionButton();
        
    }

    public virtual void OnPointerExit(PointerEventData eventData){}

    private void OnInteractionButton()
    {
        switch (buttonType)
        {
            case EButtonType.None:
                break;
            case EButtonType.ColorButton:
                _currentImage.color = colorButtonSettings.pressedColor;
                break;
        }

        if(isButton) SetInteractable(false);
    }

    private void OnResetInteractionButton()
    {
        switch (buttonType)
        {
            case EButtonType.None:
                break;
            case EButtonType.ColorButton:
                _currentImage.color = colorButtonSettings.normalColor;
                break;
        }
        if(isButton) SetInteractable(true);
    }
}