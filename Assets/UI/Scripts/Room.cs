using UnityEngine;
using UnityEngine.UIElements;

public class Room : MonoBehaviour
{
    public Transform CharacterHolder;
    public Camera RoomCam;

    public VisualElement UIElement { get; set; }
    public float ShiftYBy { get; set; }
    public float NextY => UIElement.resolvedStyle.top + ShiftYBy;

    public void SetBackground(Texture2D bkgTexture)
    {
        UIElement.style.backgroundImage = new StyleBackground(bkgTexture);
    }

    public void SetDancer(GameObject dancer, string dance)
    {
        if (CharacterHolder.childCount > 0)
            Destroy(CharacterHolder.GetChild(0).gameObject);

        GameObject dancerInst = Instantiate(dancer, CharacterHolder);
        dancerInst.GetComponent<Animator>().SetTrigger(dance);
    }

    private void LateUpdate()
    {
        UIElement.style.top = UIElement.resolvedStyle.top + ShiftYBy;
        ShiftYBy = 0f;
    }
}