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

    
    // UI parts
    private VisualElement _reactiveElement;
    private VisualElement _slide1;
    private VisualElement _slide2;

    // Gestures params
    private Vector3 _lastPointerPosition;
    private float _slideHeight;
    private float _cumulatedShift;

    private void OnEnable()
    {
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        _reactiveElement = rootVisualElement.Q("TopPart");
        _slide1 = _reactiveElement.Q("Slide1");
        _slide2 = _reactiveElement.Q("Slide2");
        _reactiveElement.RegisterCallback<GeometryChangedEvent>(Init);
    }

    private void Init(GeometryChangedEvent evt)
    {
        _reactiveElement.UnregisterCallback<GeometryChangedEvent>(Init);
        var height = _reactiveElement.resolvedStyle.height;
        var width = _reactiveElement.resolvedStyle.width;

        _slideHeight = height;
        _slide1.style.height = height;
        _slide2.style.height = height;
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);

        // Resize Render textures to screen size
        RoomRT1.width = (int)width;
        RoomRT1.height = (int)height;
        RoomRT2.width = (int)width;
        RoomRT2.height = (int)height;

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
        StartCoroutine(SlideTransitionCoroutine(-_cumulatedShift));
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
                float newSlide1Top = _slide1.resolvedStyle.top + factorizedMove;
                _slide1.style.top = newSlide1Top;
                _slide2.style.top = newSlide1Top;
            }
            else
            {
                float targetHeight = _slideHeight * Mathf.Sign(yShift) - _cumulatedShift;
                _cumulatedShift = 0f;
                StartCoroutine(SlideTransitionCoroutine(targetHeight));
            }
        }

        _lastPointerPosition = evt.position;
    }

    private static float InExpo(float t) => (float)Math.Pow(2, 10 * (t - 1));
    private static float OutExpo(float t) => 1 - InExpo(1 - t);

    private IEnumerator SlideTransitionCoroutine(float targetHeight)
    {
        // No interaction during transition
        _reactiveElement.UnregisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _reactiveElement.UnregisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
        _reactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);

        float timeElapsed = 0;
        float startPos = _slide1.resolvedStyle.top;
        
        while (timeElapsed < TransitionDuration)
        {
            // Using Out Expo easing for slide
            float newSlideTop = startPos + (OutExpo(timeElapsed) * targetHeight);
            _slide1.style.top = newSlideTop;
            _slide2.style.top = newSlideTop;
            yield return null;
            timeElapsed += Time.deltaTime;
        }
        
        // just in case we over- or undershoot the animation
        _slide1.style.top = startPos + targetHeight;
        _slide2.style.top = startPos + targetHeight;

        // Bind pointer down
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }
}
