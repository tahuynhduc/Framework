using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class ButtonCustomBase : MonoBehaviour, IButton
{
    [SerializeField] private bool button;
    [SerializeField] private float distanceOnClick;
    [SerializeField] protected bool interactable = true;
    [SerializeField] private EButtonType buttonType;
    protected Image CurrentImage;
    private float _lastClickTime;

    public ColorButtonSettings colorButtonSettings = new();
    
#if  UNITY_EDITOR
    
    private void OnValidate()
    {
        CurrentImage = GetComponent<Image>();
    }
    
#endif

    public bool CanClick()
    {
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
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        OnResetInteractionButton();
    }

    public virtual void OnPointerExit(PointerEventData eventData){}

    public abstract void OnClick();

    private void OnInteractionButton()
    {
        switch (buttonType)
        {
            case EButtonType.None:
                break;
            case EButtonType.ColorButton:
                CurrentImage.color = colorButtonSettings.pressedColor;
                break;
        }
    }

    private void OnResetInteractionButton()
    {
        switch (buttonType)
        {
            case EButtonType.None:
                break;
            case EButtonType.ColorButton:
                CurrentImage.color = colorButtonSettings.normalColor;
                break;
        }
    }
}