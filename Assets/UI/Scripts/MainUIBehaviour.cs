using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIBehaviour : MonoBehaviour
{
    [Header("Threshold to move slide (in px)")]
    public float MoveThreshold = 3;
    [Header("Factor to increase move feedback")]
    public float MoveFactor = 10;
    public RenderTexture RoomRT1;
    public RenderTexture RoomRT2;

    
    // UI parts
    private VisualElement _reactiveElement;
    private VisualElement _slide1;
    private VisualElement _slide2;

    // Gestures params
    private Vector3 _lastPointerPosition;

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
        
        // On down we want to start listening to up and move, but no longer down
        // to avoid parasite events.
        _reactiveElement.UnregisterCallback<PointerDownEvent>(OnPointerDownCallback);
        _reactiveElement.RegisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _reactiveElement.RegisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
    }

    private void OnPointerUpCallback(PointerUpEvent evt)
    {
        // On up we want to stop listening to up and move, an restart listening to down
        _reactiveElement.UnregisterCallback<PointerUpEvent>(OnPointerUpCallback);
        _reactiveElement.UnregisterCallback<PointerMoveEvent>(OnPointerMoveCallback);
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }

    private void OnPointerMoveCallback(PointerMoveEvent evt)
    {
        // We don't do anything if the pointer has move less than
        // the threshold
        float yShift = (evt.position - _lastPointerPosition).y;
        if (Mathf.Abs(yShift) >= MoveThreshold)
        {
            float factorizedMove = yShift * MoveFactor;
            float newSlide1Top = _slide1.resolvedStyle.top + factorizedMove;
            _slide1.style.top = newSlide1Top;
            _slide2.style.top = newSlide1Top;
        }

        _lastPointerPosition = evt.position;
    }
}
