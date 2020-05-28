using System.Collections;
using UnityEngine;

/*
 * TODO: UI
 *         * autozoom button
 *         * autopopulate button
 *         * stop, start, restart, undo one frame (full rewind/replay?)
 *         * screenshot
 *         * save state/load state
 */
public class Game : MonoBehaviour {
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
    private bool _isPlaying = true;
    private float _rate = 1;

    [SerializeField] private int rows = 100;
    [SerializeField] private int columns = 100;
    [SerializeField] private GameObject liveCellPrefab = null;
    [SerializeField] private GameObject allCellsPrefab = null;
    [SerializeField] private float zoomFactorPerScrollInput = 2;

    public void SlowDown() {
        CancelInvoke(nameof(Run));
        _rate *= 1.414f;
        InvokeRepeating(nameof(Run), _rate, _rate);
    }

    public void SpeedUp() {
        CancelInvoke(nameof(Run));
        _rate /= 1.414f;
        InvokeRepeating(nameof(Run), _rate, _rate);
    }


    public void Pause() {
        _isPlaying = false;
    }

    public void Play() {
        _isPlaying = true;
    }

    public void Advance() {
        _isPlaying = false;
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

        float width = _camera.orthographicSize*_camera.aspect;
        float height = _camera.orthographicSize;
        if(_minRow < -width) width = -_minRow;
        if(_maxRow > width) width = _maxRow;
        if(_minCol < -height) height = -_minCol;
        if(_maxCol > height) height = _maxCol;
        if(height*_camera.aspect < width) _camera.orthographicSize = width/_camera.aspect;
        else _camera.orthographicSize = height;
    }


    private void Update() {
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
            pos = _camera.ScreenToWorldPoint(Input.mousePosition);
            int col = Mathf.RoundToInt((pos.x + columns)/2);
            int row = Mathf.RoundToInt((pos.y + rows)/2);
            int index = PosToIndex(row, col);
            if(index > 0 && index < _board.Length) {
                _isPlaying = false;
                _board[index] = !_board[index];
                Draw();
            }
        }
    }

    public void Draw() {
        if(_allCells) Destroy(_allCells);
        _allCells = Instantiate(allCellsPrefab, transform);

        for(int row = 0; row < rows; row++) {
            for(int col = 0; col < columns; col++) {
                float x = 2*col - columns;
                float y = 2*row - rows;
                int index = PosToIndex(row, col);
                if(!_board[index]) continue;
                Instantiate(liveCellPrefab, new Vector3(x, y, 0), Quaternion.identity, _allCells.transform);
            }
        }
    }

    private void Awake() {
        _camera = Camera.main;
        _board = new BitArray(rows*columns);
        _buffer = new BitArray(rows*columns);
    }

    private void Start() {
        Randomize(1000);
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

    private void Randomize(int numPoints = 20) {
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
}
