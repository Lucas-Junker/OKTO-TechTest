using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIBehaviour : MonoBehaviour
{
    [Header("Threshold to move slide (in px)")]
    public float MoveThreshold = 3;
    [Header("Ratio of the slide to move to trigger transition")]
    public float TransitionThreshold = .1f;
    [Header("Factor to increase move feedback")]
    public float MoveFactor = 10f;
    public float TransitionDuration = 1f;

    public RenderTexture RoomRT1;
    public RenderTexture RoomRT2;
    public Room Room1;
    public Room Room2;
    public SlideDataSettings Settings;

    
    // UI parts
    private VisualElement _reactiveElement;

    // Gestures params
    private Vector3 _lastPointerPosition;
    private float _slideHeight;
    private float _cumulatedShift;
    private int _currentIndex = 0;
    private Room _activeRoom;
    private RenderTexture _activeRT;
    private int _preparedIndex = 1;

    private void OnEnable()
    {
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        _reactiveElement = rootVisualElement.Q("TopPart");
        Room1.UIElement = _reactiveElement.Q("Slide1");
        Room2.UIElement = _reactiveElement.Q("Slide2");
        _reactiveElement.RegisterCallback<GeometryChangedEvent>(Init);
    }

    private void Init(GeometryChangedEvent evt)
    {
        _reactiveElement.UnregisterCallback<GeometryChangedEvent>(Init);
        var height = _reactiveElement.resolvedStyle.height;
        var width = _reactiveElement.resolvedStyle.width;

        _slideHeight = height;
        Room1.UIElement.style.height = height;
        Room2.UIElement.style.height = height;
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);

        // Resize Render textures to screen size
        RoomRT1.width = (int)width;
        RoomRT1.height = (int)height;
        RoomRT2.width = (int)width;
        RoomRT2.height = (int)height;

        _activeRoom = Room1;
        SlideData data = Settings.SlideDataList[_currentIndex];
        _activeRoom.SetBackground(data.Background);
        _activeRoom.SetDancer(data.Dancer, data.Dance);

        data = Settings.SlideDataList[_currentIndex < Settings.SlideDataList.Count - 1 ? _currentIndex + 1 : 0];
        Room2.SetBackground(data.Background);
        Room2.SetDancer(data.Dancer, data.Dance);

    }

    private Room GetInactiveRoom() => _activeRoom == Room1 ? Room2 : Room1;

    private int GetPreviousIndex() => _currentIndex > 0 ? _currentIndex - 1 : Settings.SlideDataList.Count - 1;
    private int GetNextIndex() => _currentIndex < Settings.SlideDataList.Count - 1 ? _currentIndex + 1 : 0;

    private void PrepareSlide(int index, bool up)
    {
        if (_preparedIndex == index)
            return;

        _preparedIndex = index;

        SlideData data = Settings.SlideDataList[index];

        Room room = GetInactiveRoom();
        room.SetBackground(data.Background);
        room.SetDancer(data.Dancer, data.Dance);
        if (up)
        {
            Debug.Log("Prepare previous slide");
            room.ShiftYBy -= _slideHeight * 2;
        }
        else
        {
            Debug.Log("Prepare next slide");
            room.ShiftYBy += _slideHeight * 2;
        }

    }

    private void OnPointerDownCallback(PointerDownEvent evt)
    {
        _lastPointerPosition = evt.position;
        _cumulatedShift = 0;

        // On down we want to start listening to up and move, but no longer down
        // to avoid parasite events.
        _reactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);
        _reactiveElement.RegisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _reactiveElement.RegisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
    }

    private void OnPointerUpCallback(PointerUpEvent evt)
    {
        // On up we recenter the current slide if moved
        StartCoroutine(SlideMoveCoroutine(-_cumulatedShift));
        _cumulatedShift = 0;
    }

    private void OnPointerMoveCallback(PointerMoveEvent evt)
    {
        // We don't do anything if the pointer has move less than
        // the threshold
        float yShift = (evt.position - _lastPointerPosition).y;
        float absYShift = Mathf.Abs(yShift);
        if (absYShift >= MoveThreshold)
        {
            if (Mathf.Abs(_cumulatedShift) < _slideHeight * TransitionThreshold)
            {
                float factorizedMove = yShift * MoveFactor;
                _cumulatedShift += factorizedMove;

                if (_cumulatedShift > 0)
                {
                    Debug.Log("Move up");
                    PrepareSlide(GetPreviousIndex(), up: true);
                }
                else if (_cumulatedShift < 0)
                {
                    Debug.Log("Move down");
                    PrepareSlide(GetNextIndex(), up: false);
                }

                _activeRoom.ShiftYBy += factorizedMove;
                Room inactiveRoom = GetInactiveRoom();
                inactiveRoom.ShiftYBy += factorizedMove;
            }
            else
            {
                float targetHeight = _slideHeight * Mathf.Sign(yShift) - _cumulatedShift;
                _cumulatedShift = 0f;
                Debug.Log("Transition to slide");
                StartCoroutine(SlideTransitionCoroutine(targetHeight));
            }
        }

        _lastPointerPosition = evt.position;
    }

    private static float InExpo(float t) => (float)Math.Pow(2, 10 * (t - 1));
    private static float OutExpo(float t) => 1 - InExpo(1 - t);

    private IEnumerator SlideMoveCoroutine(float targetHeight)
    {
        // No interaction during transition
        _reactiveElement.UnregisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _reactiveElement.UnregisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
        _reactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);

        Room inactiveRoom = GetInactiveRoom();
        float timeElapsed = 0;
        float startPos1 = _activeRoom.UIElement.resolvedStyle.top;
        float startPos2 = inactiveRoom.UIElement.resolvedStyle.top;

        while (timeElapsed < TransitionDuration)
        {
            // Using Out Expo easing for slide
            float shift = OutExpo(timeElapsed) * targetHeight;
            _activeRoom.UIElement.style.top = startPos1 + shift;
            inactiveRoom.UIElement.style.top = startPos2 + shift;
            yield return null;
            timeElapsed += Time.deltaTime;
        }

        // just in case we over- or undershoot the animation
        _activeRoom.UIElement.style.top = startPos1 + targetHeight;
        inactiveRoom.UIElement.style.top = startPos2 + targetHeight;

        // Bind pointer down
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }

    private IEnumerator SlideTransitionCoroutine(float targetHeight)
    {
        yield return SlideMoveCoroutine(targetHeight);

        _activeRoom = GetInactiveRoom();
        _currentIndex = _preparedIndex;
    }
}
