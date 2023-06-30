using UnityEngine;
using UnityEngine.UIElements;

public class Room : MonoBehaviour
{
    public Transform CharacterHolder;
    public Camera RoomCam;

    private TextElement _playerNameElt;
    private TextElement _danceNameElt;
    private VisualElement _albumImageElt;
    private TextElement _artistNameElt;
    private TextElement _songNameElt;

    public VisualElement UIElement { get; private set; }
    public float ShiftYBy { get; set; }
    public float NextY => UIElement.resolvedStyle.top + ShiftYBy;

    public void SetVisualElement(VisualElement root)
    {
        UIElement = root;
        _playerNameElt = root.Q<TextElement>("PlayerName");
        _danceNameElt = root.Q<TextElement>("DanceName");
        _albumImageElt = root.Q("Album");
        _artistNameElt = root.Q<TextElement>("ArtistName");
        _songNameElt = root.Q<TextElement>("SongName");
    }

    public void SetSlideData(SlideData data)
    {
        // Set bkg
        UIElement.style.backgroundImage = new StyleBackground(data.Background);

        // Set dancer
        if (CharacterHolder.childCount > 0)
            Destroy(CharacterHolder.GetChild(0).gameObject);

        GameObject dancerInst = Instantiate(data.Dancer, CharacterHolder);;
        dancerInst.GetComponent<Animator>().SetTrigger(data.Dance);

        // Set other data
        _playerNameElt.text = data.PlayerName;
        _danceNameElt.text = data.DanceName;
        _albumImageElt.style.backgroundImage = new StyleBackground(data.AlbumImage);
        _artistNameElt.text = data.ArtistName;
        _songNameElt.text = data.SongName;
    }

    private void LateUpdate()
    {
        UIElement.style.top = UIElement.resolvedStyle.top + ShiftYBy;
        ShiftYBy = 0f;
    }
}