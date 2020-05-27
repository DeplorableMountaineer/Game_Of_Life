using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour {
    private BitArray _board;
    private BitArray _buffer;
    private Camera _camera;
    private GameObject _allCells = null;
    private int _minRow = Int32.MaxValue;
    private int _maxRow = -1;
    private int _minCol = Int32.MaxValue;
    private int _maxCol = -1;

    [SerializeField] private int rows = 100;
    [SerializeField] private int columns = 100;
    [SerializeField] private GameObject liveCellPrefab = null;
    [SerializeField] private GameObject allCellsPrefab = null;

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
        AutoAdjustZoom();
        Draw();
        InvokeRepeating(nameof(Run), 1, 1);
    }

    private void Run() {
        UpdateGeneration();
        AutoAdjustZoom();
        Draw();
    }

    private void UpdateGeneration() {
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

    private void AutoAdjustZoom() {
        for(int row = 0; row < rows; row++) {
            for(int col = 0; col < columns; col++) {
                float x = 2*col - columns;
                float y = 2*row - rows;
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
