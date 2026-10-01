using TMPro;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    private const float Frequency = 0.25f;
    private const int FrameRange = 60;

    [SerializeField] private TMP_Text _currentFpsText;
    [SerializeField] private TMP_Text _averageFpsText;
    [SerializeField] private TMP_Text _minFpsText;

    private float _lastCountTime;
    private float _nextCountTime;
    private int _frames;

    private float[] _fpsBuffer;
    private int _fpsBufferIndex;

    private float _minFps;
    private float _avgFps;

    private int _index;
    private float _sum;
    private float _lowest;
    private float _tempFps;

    private void Start()
    {
        _lastCountTime = Time.unscaledTime;
        _nextCountTime = Time.unscaledTime;
        _frames = Time.frameCount;
        InitializeBuffer();
    }

    private void Update()
    {
        UpdateBuffer();

        if (Time.unscaledTime < _nextCountTime)
        {
            return;
        }

        CalculateFps();
        float currentFps = (Time.frameCount - _frames) / (Time.unscaledTime - _lastCountTime);

        Display(_currentFpsText, currentFps);
        Display(_averageFpsText, _avgFps);
        Display(_minFpsText, _minFps);

        _lastCountTime = Time.unscaledTime;
        _nextCountTime += Frequency;
        _frames = Time.frameCount;
    }

    private void InitializeBuffer()
    {
        _fpsBuffer = new float[FrameRange];
        _fpsBufferIndex = 0;
    }

    private void UpdateBuffer()
    {
        _fpsBuffer[_fpsBufferIndex++] = 1f / Time.unscaledDeltaTime;
        if (_fpsBufferIndex >= FrameRange)
        {
            _fpsBufferIndex = 0;
        }
    }

    private void CalculateFps()
    {
        _sum = 0;
        _lowest = int.MaxValue;
        for (_index = 0; _index < FrameRange; _index++)
        {
            _tempFps = _fpsBuffer[_index];
            _sum += _tempFps;
            if (_tempFps < _lowest)
            {
                _lowest = _tempFps;
            }
        }

        _avgFps = _sum / FrameRange;
        _minFps = _lowest;
    }

    private void Display(TMP_Text label, float fps)
    {
        label.text = fps.ToString("F1");
    }
}