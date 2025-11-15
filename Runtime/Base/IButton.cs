using UnityEngine.EventSystems;

public interface IButton:IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
{
    public void OnClick();
}