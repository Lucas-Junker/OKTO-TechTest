using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIBehaviour : MonoBehaviour
{
    public float MoveThreshold;

    private VisualElement _reactiveElement;
    private Vector3 _lastPointerPosition;
    private float _squaredThreshold;

    private void OnEnable()
    {
        _reactiveElement = GetComponent<UIDocument>().rootVisualElement.Q("TopPart");
        _reactiveElement.RegisterCallback<PointerDownEvent>(OnPointerDownCallback);
    }

    private void Start()
    {
        // Using squared threshold to avoir sqrRoot computation
        // during pointer move
        _squaredThreshold = MoveThreshold * MoveThreshold;
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
        // Using squared threshold to avoir sqrRoot computation
        // during pointer move
        Vector3 shift = evt.position - _lastPointerPosition;
        if (shift.sqrMagnitude >= _squaredThreshold)
        {
            Debug.Log("Move");
        }

        _lastPointerPosition = evt.position;
    }
}
