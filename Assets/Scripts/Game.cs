﻿using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour {
    private const string ScreenshotIndexKey = "Screenshot Index";
    private const string SaveDirectoryKey = "Save Directory";
    private const string ScreenshotBaseKey = "Screenshot Base";
    private const string SaveNameKey = "Save Name";

    private BitArray _board;
    private BitArray _buffer;
    private Camera _camera;
    private GameObject _allCells = null;
    private int _minRow = int.MaxValue;
    private int _maxRow = -1;
    private int _minCol = int.MaxValue;
    private int _maxCol = -1;
    private float _scroll = 0;
    private Vector3 _lastMousePosition = default;
    private bool _isPlaying = false;
    private float _rate = 1;
    private bool _canUndo = false;
    private Messages _messages;
    private bool _alreadyBegun = false;
    private SpriteRenderer _cursorImage;

    [SerializeField] private int rows = 100;
    [SerializeField] private int columns = 100;
    [SerializeField] private GameObject liveCellPrefab = null;
    [SerializeField] private GameObject allCellsPrefab = null;
    [SerializeField] private float zoomFactorPerScrollInput = 2;
    [SerializeField] private Button advanceButton = null;
    [SerializeField] private Button playButton = null;
    [SerializeField] private Button stopButton = null;
    [SerializeField] private Button undoButton = null;
    [SerializeField] private Transform cursor = null;

    public void Save() {
        State state = MakeStateToken();
        string directory = PlayerPrefs.GetString(SaveDirectoryKey);
        string filename = PlayerPrefs.GetString(SaveNameKey);
        string path = $"{directory}/{filename}";
        try {
            File.WriteAllText(path, state.ToString());
            _messages.ShowMessage($"Saved state to {path}");
        }
        catch(Exception e) {
            _messages.ShowMessage($"Failed to save state: {e}; ensure path is valid: {path}");
        }
    }

    public void Load() {
        string directory = PlayerPrefs.GetString(SaveDirectoryKey);
        string filename = PlayerPrefs.GetString(SaveNameKey);
        string path = $"{directory}/{filename}";
        string text = File.ReadAllText(path);
        try {
            State state = State.FromString(text, _board.Count);
            RestoreFromToken(state);
            _messages.ShowMessage($"Loaded state from {path}");
        }
        catch(Exception e) {
            _messages.ShowMessage($"Failed to restore state: {e}; ensure path is valid: {path}");
        }

        Pause();
    }

    public void Screenshot() {
        Pause();
        if(!PlayerPrefs.HasKey(ScreenshotIndexKey)) {
            PlayerPrefs.SetInt(ScreenshotIndexKey, 1);
        }

        int index = PlayerPrefs.GetInt(ScreenshotIndexKey);
        string directory = PlayerPrefs.GetString(SaveDirectoryKey);
        string basename = PlayerPrefs.GetString(ScreenshotBaseKey);
        string filename = $"{directory}/{basename}{index:0000}.png";
        try {
            ScreenCapture.CaptureScreenshot(filename);
            _messages.ShowMessage($"Saved screenshot to {filename}", .5f);
            index++;
            PlayerPrefs.SetInt(ScreenshotIndexKey, index);
            PlayerPrefs.Save();
        }
        catch(Exception e) {
            _messages.ShowMessage($"Failed to take screenshot: {e}; ensure path is valid: {filename}");
        }
    }

    public void Quit() {
        Application.Quit();
    }

    public void Clear() {
        Pause();
        for(int i = 0; i < _board.Count; i++) {
            _buffer[i] = _board[i];
            _board[i] = false;
        }

        Draw();
    }

    public void AutoPopulate(int numPoints = 20) {
        for(int i = 0; i < numPoints; i++) {
            int row = Mathf.RoundToInt(rows/2f + (Random.value - Random.value)*rows/2);
            if(row < 0) row = 0;
            if(row >= rows) row = rows - 1;
            int col = Mathf.RoundToInt(columns/2f + (Random.value - Random.value)*columns/2);
            if(col < 0) col = 0;
            if(col >= columns) col = columns - 1;
            int index = row + rows*col;
            _board[index] = !_board[index];
        }

        AutoZoom();
        Draw();
    }

    public void Undo() {
        if(!_canUndo) return;
        _canUndo = false;
        _isPlaying = false;
        advanceButton.interactable = !_isPlaying;
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
        undoButton.interactable = false;
        for(int i = 0; i < _board.Count; i++) {
            _board[i] = _buffer[i];
        }

        Draw();
    }

    public void SlowDown() {
        CancelInvoke(nameof(Run));
        _rate *= 1.414f;
        InvokeRepeating(nameof(Run), _rate, _rate);
        _messages.ShowMessage($"Rate: {1/_rate:F1}");
    }

    public void SpeedUp() {
        CancelInvoke(nameof(Run));
        _rate /= 1.414f;
        InvokeRepeating(nameof(Run), _rate, _rate);
        _messages.ShowMessage($"Rate: {1/_rate:F1}");
    }


    public void Pause() {
        _isPlaying = false;
        advanceButton.interactable = !_isPlaying;
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
    }

    public void Play() {
        _isPlaying = true;
        advanceButton.interactable = !_isPlaying;
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
    }

    public void Advance() {
        _isPlaying = false;
        advanceButton.interactable = !_isPlaying;
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
        UpdateGeneration(true);
        Draw();
    }

    public void AutoZoom() {
        for(int row = 0; row < rows; row++) {
            for(int col = 0; col < columns; col++) {
                int index = PosToIndex(row, col);
                if(!_board[index]) continue;
                _minCol = Mathf.Min(_minCol, col);
                _maxCol = Mathf.Max(_maxCol, col);
                _minRow = Mathf.Min(_minRow, row);
                _maxRow = Mathf.Max(_maxRow, row);
            }
        }

        _camera.orthographicSize = 5;
        float width = _camera.orthographicSize*_camera.aspect;
        float height = _camera.orthographicSize;
        if(_minRow < -width) width = -_minRow;
        if(_maxRow > width) width = _maxRow;
        if(_minCol < -height) height = -_minCol;
        if(_maxCol > height) height = _maxCol;
        if(height*_camera.aspect < width) _camera.orthographicSize = width/_camera.aspect;
        else _camera.orthographicSize = height;
    }

    private void Begin() {
        if(_alreadyBegun) return;
        GameObject go = GameObject.FindGameObjectWithTag("Startup Message");
        if(go) {
            Destroy(go);
        }

        _alreadyBegun = true;
    }

    public Vector2Int MousePosition {
        get {
            Vector3 pos = _camera.ScreenToWorldPoint(Input.mousePosition);
            int col = Mathf.RoundToInt((pos.x + columns)/2);
            int row = Mathf.RoundToInt((pos.y + rows)/2);
            return new Vector2Int(col, row);
        }
    }

    private void Update() {
        float x = 2*MousePosition.x - columns;
        float y = 2*MousePosition.y - rows;
        cursor.position = new Vector3(x, y, 0);
        Color c = _cursorImage.color;
        if(MousePosition.x >= 0 && MousePosition.x < columns && MousePosition.y >= 0 && MousePosition.y < rows) {
            c.a = .5f;
        }
        else {
            c.a = 0;
        }

        _cursorImage.color = c;
        _scroll += Input.GetAxis("Mouse ScrollWheel");
        if(Mathf.Abs(_scroll) > Mathf.Epsilon) {
            _camera.orthographicSize *= Mathf.Pow(zoomFactorPerScrollInput, -_scroll);
            _scroll = 0;
        }

        Vector3 pos = _camera.ScreenToWorldPoint(Input.mousePosition);
        pos.z = -10;
        if(Input.GetMouseButtonDown(0)) {
            _lastMousePosition = pos;
        }

        if(Input.GetMouseButton(0)) {
            Vector3 delta = pos - _lastMousePosition;
            if(!(delta.magnitude > .1f)) return;
            _camera.transform.Translate(-delta, Space.World);
            _lastMousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        }

        if(Input.GetMouseButtonDown(1)) {
            int index = PosToIndex(MousePosition.y, MousePosition.x);
            if(index <= 0 || index >= _board.Length) return;
            _isPlaying = false;
            advanceButton.interactable = !_isPlaying;
            playButton.interactable = !_isPlaying;
            stopButton.interactable = _isPlaying;
            _board[index] = !_board[index];
            Draw();
        }

        if(Input.GetKeyDown("space")) {
            if(_isPlaying) Pause();
            else Play();
        }
    }

    public void Draw() {
        if(_allCells) Destroy(_allCells);
        _allCells = Instantiate(allCellsPrefab, transform);

        bool nonempty = false;
        for(int row = 0; row < rows; row++) {
            for(int col = 0; col < columns; col++) {
                float x = 2*col - columns;
                float y = 2*row - rows;
                int index = PosToIndex(row, col);
                if(!_board[index]) continue;
                nonempty = true;
                Instantiate(liveCellPrefab, new Vector3(x, y, 0), Quaternion.identity, _allCells.transform);
            }
        }

        if(nonempty) Begin();
    }

    public void Setup() {
        Pause();
        GameObject go = GameObject.FindGameObjectWithTag("Setup Canvas");
        if(!go) return;

        Canvas setupCanvas = go.GetComponent<Canvas>();

        go = GameObject.FindGameObjectWithTag("Save Directory");
        InputField saveDir = go.GetComponent<InputField>();
        go = GameObject.FindGameObjectWithTag("Screenshot Base");
        InputField screenBase = go.GetComponent<InputField>();
        go = GameObject.FindGameObjectWithTag("Save Name");
        InputField saveName = go.GetComponent<InputField>();

        saveDir.text = PlayerPrefs.GetString(SaveDirectoryKey);
        screenBase.text = PlayerPrefs.GetString(ScreenshotBaseKey);
        saveName.text = PlayerPrefs.GetString(SaveNameKey);
        setupCanvas.enabled = true;
    }

    public void EndSetup() {
        GameObject go = GameObject.FindGameObjectWithTag("Setup Canvas");
        if(!go) return;
        Canvas setupCanvas = go.GetComponent<Canvas>();

        go = GameObject.FindGameObjectWithTag("Save Directory");
        InputField saveDir = go.GetComponent<InputField>();
        go = GameObject.FindGameObjectWithTag("Screenshot Base");
        InputField screenBase = go.GetComponent<InputField>();
        go = GameObject.FindGameObjectWithTag("Save Name");
        InputField saveName = go.GetComponent<InputField>();

        PlayerPrefs.SetString(SaveDirectoryKey, saveDir.text);
        PlayerPrefs.SetString(ScreenshotBaseKey, screenBase.text);
        PlayerPrefs.SetString(ScreenshotBaseKey, saveName.text);

        PlayerPrefs.Save();

        setupCanvas.enabled = false;
    }

    private void Awake() {
        _camera = Camera.main;
        _board = new BitArray(rows*columns);
        _buffer = new BitArray(rows*columns);
        _messages = FindObjectOfType<Messages>();
        if(cursor) _cursorImage = cursor.GetComponent<SpriteRenderer>();
    }

    private void Start() {
        if(!PlayerPrefs.HasKey(SaveDirectoryKey)) {
            PlayerPrefs.SetString(SaveDirectoryKey, Application.persistentDataPath);
        }

        if(!PlayerPrefs.HasKey(ScreenshotBaseKey)) {
            PlayerPrefs.SetString(ScreenshotBaseKey, "Life_Screenshot_");
        }

        if(!PlayerPrefs.HasKey(SaveNameKey)) {
            PlayerPrefs.SetString(SaveNameKey, "Life_Saved");
        }

        GameObject go = GameObject.FindGameObjectWithTag("Setup Canvas");
        if(go) go.GetComponent<Canvas>().enabled = false;

        undoButton.interactable = false;
        advanceButton.interactable = !_isPlaying;
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
        AutoZoom();
        Draw();
        InvokeRepeating(nameof(Run), _rate, _rate);
    }

    private void Run() {
        UpdateGeneration();
        Draw();
    }

    private void UpdateGeneration(bool force = false) {
        if(!_isPlaying && !force) return;
        for(int i = 0; i < _board.Count; i++) {
            _buffer[i] = _board[i];
        }

        _canUndo = true;
        undoButton.interactable = true;

        for(int i = 0; i < _board.Count; i++) {
            int neighbors = 0;
            int row = RowFromIndex(i);
            int col = ColFromIndex(i);
            neighbors += GetBufferValue(row - 1, col) ? 1 : 0;
            neighbors += GetBufferValue(row + 1, col) ? 1 : 0;
            neighbors += GetBufferValue(row, col - 1) ? 1 : 0;
            neighbors += GetBufferValue(row, col + 1) ? 1 : 0;
            neighbors += GetBufferValue(row - 1, col - 1) ? 1 : 0;
            neighbors += GetBufferValue(row - 1, col + 1) ? 1 : 0;
            neighbors += GetBufferValue(row + 1, col - 1) ? 1 : 0;
            neighbors += GetBufferValue(row + 1, col + 1) ? 1 : 0;
            if(neighbors == 3) _board[i] = true;
            else if(neighbors != 2) _board[i] = false;
        }
    }

    private bool GetBufferValue(int row, int col) {
        if(row < 0 || row >= rows) return false;
        if(col < 0 || col >= columns) return false;
        return _buffer[PosToIndex(row, col)];
    }

    private int PosToIndex(int row, int col) {
        return row + rows*col;
    }

    private int RowFromIndex(int index) {
        return index%rows;
    }

    private int ColFromIndex(int index) {
        return Mathf.FloorToInt(index/(float)rows);
    }

    private State MakeStateToken() {
        State state = new State {
            board = new BitArray(_board),
            buffer = new BitArray(_buffer),
            orthographicSize = _camera.orthographicSize,
            cameraPos = _camera.transform.position,
            rate = _rate
        };
        return state;
    }

    private void RestoreFromToken(State state) {
        Pause();
        _canUndo = false;
        for(int i = 0; i < _board.Count; i++) {
            _board[i] = state.board[i];
            _buffer[i] = state.buffer[i];
            if(_board[i] != _buffer[i]) _canUndo = true;
        }

        undoButton.interactable = _canUndo;
        _camera.orthographicSize = state.orthographicSize;
        _camera.transform.position = state.cameraPos;
        _rate = state.rate;
        Draw();
    }
}
