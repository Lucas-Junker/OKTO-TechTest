using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIBehaviour : MonoBehaviour
{
    // I've set my inspector fields as public to go fast
    // but usually I use "[SerializeField] private" signature for it.
    [Header("Threshold to move slide (in px)")]
    public float MoveThreshold = 3;
    [Header("Ratio of the slide to move to trigger transition")]
    public float TransitionThreshold = .1f;
    [Header("Factor to increase move feedback")]
    public float MoveFactor = 10f;
    public float TransitionDuration = 1f;

    [Header("Required components")]
    public RenderTexture RoomRT1;
    public RenderTexture RoomRT2;
    public Room Room1;
    public Room Room2;
    public SlideDataSettings Settings;

    // UI parts
    private VisualElement _interactiveElement;

    // Gestures vars
    private Vector3 _lastPointerPosition;
    private float _slideHeight;
    private float _cumulatedShift;
    private int _currentIndex = 0;
    private Room _activeRoom;
    private Room _inactiveRoom;
    private int _preparedIndex = 1;

    private void OnEnable()
    {
        // Retrieve main interactive element
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        _interactiveElement = rootVisualElement.Q("TopPart");

        // Bind rooms rendering and behaviour to UI elements
        Room1.SetVisualElement(_interactiveElement.Q("Slide1"));
        Room2.SetVisualElement(_interactiveElement.Q("Slide2"));

        // Wait for first layout computation for the rest of the init
        _interactiveElement.RegisterCallback<GeometryChangedEvent>(Init);
    }

    private void Init(GeometryChangedEvent evt)
    {
        _interactiveElement.UnregisterCallback<GeometryChangedEvent>(Init);

        var height = _interactiveElement.resolvedStyle.height;
        var width = _interactiveElement.resolvedStyle.width;
        _slideHeight = height;

        // Force size of slide
        Room1.UIElement.style.height = height;
        Room2.UIElement.style.height = height;

        // Resize Render textures to screen size
        RoomRT1.width = (int)width;
        RoomRT1.height = (int)height;
        RoomRT2.width = (int)width;
        RoomRT2.height = (int)height;

        _activeRoom = Room1;
        _inactiveRoom = Room2;

        // Init slide data
        SlideData data = Settings.SlideDataList[_currentIndex];
        _activeRoom.SetSlideData(data);

        data = Settings.SlideDataList[_currentIndex < Settings.SlideDataList.Count - 1 ? _currentIndex + 1 : 0];
        _inactiveRoom.SetSlideData(data);

        // Enable click
        _interactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }

    private int GetPreviousIndex() => _currentIndex > 0 ? _currentIndex - 1 : Settings.SlideDataList.Count - 1;
    private int GetNextIndex() => _currentIndex < Settings.SlideDataList.Count - 1 ? _currentIndex + 1 : 0;

    /// <summary>
    /// Prepare content of out of screen slide
    /// </summary>
    /// <param name="index">Data index in data settings</param>
    /// <param name="up">whether the slide is above or below currently displayed slide</param>
    private void PrepareSlide(int index, bool up)
    {
        // Already prepared, does nothing (avoid wrong positionning)
        if (_preparedIndex == index)
            return;

        _preparedIndex = index;

        SlideData data = Settings.SlideDataList[index];

        _inactiveRoom.SetSlideData(data);

        // Move slide above or below currently displayed slide
        if (up)
            _inactiveRoom.ShiftYBy -= _slideHeight * 2;
        else
            _inactiveRoom.ShiftYBy += _slideHeight * 2;

    }

    #region Event Callbacks

    private void OnPointerDownCallback(PointerDownEvent evt)
    {
        _lastPointerPosition = evt.position;
        _cumulatedShift = 0;

        // On down we want to start listening to up and move, but no longer down
        // to avoid parasite events.
        _interactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);
        _interactiveElement.RegisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _interactiveElement.RegisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
    }

    private void OnPointerUpCallback(PointerUpEvent evt)
    {
        // On up we recenter the current slide if moved
        StartCoroutine(SlideMoveCoroutine(0f));
    }

    private void OnPointerMoveCallback(PointerMoveEvent evt)
    {
        // We don't do anything if the pointer has move less than
        // the threshold
        float yShift = (evt.position - _lastPointerPosition).y;
        float absYShift = Mathf.Abs(yShift);
        if (absYShift >= MoveThreshold)
        {
            // If less then the threshold to init a transition to another slide,
            // Simply move the slides a little
            if (Mathf.Abs(_cumulatedShift) < _slideHeight * TransitionThreshold)
            {
                float factorizedMove = yShift * MoveFactor;

                // Store delta from initial pos for further computation
                _cumulatedShift += factorizedMove;

                // prepare inactive slide according to move (up or down)
                if (_cumulatedShift > 0)
                {
                    PrepareSlide(GetPreviousIndex(), up: true);
                }
                else if (_cumulatedShift < 0)
                {
                    PrepareSlide(GetNextIndex(), up: false);
                }

                // Store position changes in ShiftYBy to apply it only once per frame.
                _activeRoom.ShiftYBy += factorizedMove;
                _inactiveRoom.ShiftYBy += factorizedMove;
            }
            else
            {
                // If not prepared before transition
                // then do it now
                var index = 0;
                bool up = false;
                if (_cumulatedShift > 0)
                {
                    index = GetPreviousIndex();
                    up = true;
                }
                else if (_cumulatedShift < 0)
                {
                    index = GetNextIndex();
                    up = false;
                }

                if (_preparedIndex != index)
                    PrepareSlide(index, up);

                StartCoroutine(SlideTransitionCoroutine(_slideHeight * Mathf.Sign(yShift)));
            }
        }

        _lastPointerPosition = evt.position;
    }

    #endregion Event Callbacks

    #region Animations

    // Easings functions
    private float InCirc(float t) => -((float)Math.Sqrt(1 - t * t) - 1);
    private float OutCirc(float t) => 1 - InCirc(1 - t);

    private IEnumerator SlideMoveCoroutine(float targetHeight)
    {
        // No interaction during transition
        _interactiveElement.UnregisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _interactiveElement.UnregisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
        _interactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);

        float timeElapsed = 0;
        float startPos1 = _activeRoom.UIElement.resolvedStyle.top;
        float startPos2 = _inactiveRoom.UIElement.resolvedStyle.top;
        float remainingHeight = targetHeight - _cumulatedShift;

        while (timeElapsed < TransitionDuration)
        {
            // Using Out Circ easing for slide
            float shift = OutCirc(timeElapsed) * remainingHeight;
            _activeRoom.UIElement.style.top = startPos1 + shift;
            _inactiveRoom.UIElement.style.top = startPos2 + shift;
            yield return null;
            timeElapsed += Time.deltaTime;
        }

        // just in case we over- or undershoot the animation
        _activeRoom.UIElement.style.top = targetHeight;
        _inactiveRoom.UIElement.style.top = targetHeight + (_cumulatedShift < 0 ? _slideHeight : -_slideHeight);
        _cumulatedShift = 0;

        // Bind pointer down
        _interactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }

    private IEnumerator SlideTransitionCoroutine(float targetHeight)
    {
        yield return SlideMoveCoroutine(targetHeight);

        // swap active & inactive room
        Room tempRoom = _activeRoom;
        _activeRoom = _inactiveRoom;
        _inactiveRoom = tempRoom;
        int tempIndex = _currentIndex;
        _currentIndex = _preparedIndex;
        _preparedIndex = tempIndex;
    }

    #endregion Animations
}
