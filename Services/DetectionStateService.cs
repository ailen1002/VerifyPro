using System;
using System.Collections.Generic;
using ReactiveUI;
using VerifyPro.Enums;

namespace VerifyPro.Services;

public class DetectionStateService : ReactiveObject
{
    private DetectionState _currentState = DetectionState.Idle;
    public DetectionState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) return;
            _currentState = value;
            StateChanged?.Invoke(_currentState);
        }
    }
    public event Action<DetectionState>? StateChanged;

    private readonly List<string> _activeTests = [];
    private bool _hasError;

    public void StartTest(string testName)
    {
        if (!_activeTests.Contains(testName))
            _activeTests.Add(testName);

        _hasError = false;
        CurrentState = DetectionState.Running;
    }

    public void ReportTestResult(string testName, bool success)
    {
        _activeTests.Remove(testName);

        if (!success)
        {
            _hasError = true;
            CurrentState = DetectionState.Error;
        }

        if (_activeTests.Count != 0) return;
        CurrentState = _hasError ? DetectionState.Error : DetectionState.Pass;
    }

    public void SaveResults()
    {
        CurrentState = DetectionState.Idle;
        _activeTests.Clear();
        _hasError = false;
    }
}
