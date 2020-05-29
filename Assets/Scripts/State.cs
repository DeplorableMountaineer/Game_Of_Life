using System;
using System.Collections;
using System.Text;
using UnityEngine;

[Serializable]
public class State {
    public BitArray board;
    public BitArray buffer;
    public float orthographicSize;
    public Vector3 cameraPos;
    public float rate;


    public static State FromString(string str, int bitArraySize) {
        if(!GetFloat("Rate", str, out float rate, out string remaining)) {
            Debug.Log("failed to get rate");
            return null;
        }

        State result = new State {rate = rate};
        string val = remaining;
        if(!GetVector("CameraPos", val, out result.cameraPos, out remaining)) {
            Debug.Log("failed to get camera pos");
            return null;
        }

        val = remaining;
        if(!GetFloat("OrthoSize", val, out result.orthographicSize, out remaining)) {
            Debug.Log("failed to get ortho size");
            return null;
        }

        result.board = new BitArray(bitArraySize);
        result.buffer = new BitArray(bitArraySize);

        val = remaining;
        if(!GetBitArray("Board", val, result.board, out remaining)) {
            Debug.Log("failed to get board");
            return null;
        }

        val = remaining;
        if(GetBitArray("Buffer", val, result.buffer, out remaining)) return result;
        Debug.Log("failed to get buffer");
        return null;
    }

    public override string ToString() {
        StringBuilder result = new StringBuilder();

        result.Append($"Rate={rate};\n");
        result.Append($"CameraPos=({cameraPos.x},{cameraPos.y},{cameraPos.z});\n");
        result.Append($"OrthoSize={orthographicSize};\n");
        result.Append($"Board={BitArrayToString(board)}\n");
        result.Append($"Buffer={BitArrayToString(board)}\n");
        return result.ToString();
    }

    public static string BitArrayToString(BitArray ba) {
        StringBuilder result = new StringBuilder();
        int count = 0;
        bool current = false;
        for(int i = 0; i < ba.Count; i++) {
            if(ba[i] == current) {
                count++;
                continue;
            }

            result.Append($"{count};");
            count = 1;
            current = !current;
        }

        return result.ToString();
    }

    public static bool GetBitArray(string fieldName, string str, BitArray result, out string remaining) {
        if(!str.StartsWith(fieldName)) {
            remaining = str;
            return false;
        }

        remaining = str.Substring(fieldName.Length);
        if(!remaining.StartsWith("=") && !string.IsNullOrEmpty(fieldName)) {
            remaining = str;
            return false;
        }

        remaining = remaining.Substring(1);
        int len = remaining.IndexOf('\n');
        if(len < 0) {
            remaining = str;
            return false;
        }

        string val = remaining.Substring(0, len);
        remaining = remaining.Substring(len + 1);
        int loc = 0;
        bool current = false;
        while(val.Length > 0) {
            if(!GetFloat("", val, out float count, out string valRemaining)) {
                remaining = str;
                return false;
            }

            for(int i = 0; i < count; i++) {
                result[loc] = current;
                loc++;
            }

            current = !current;
            val = valRemaining;
        }

        return true;
    }

    public static bool GetVector(string fieldName, string str, out Vector3 result, out string remaining) {
        if(!str.StartsWith(fieldName)) {
            remaining = str;
            result = default;
            return false;
        }

        remaining = str.Substring(fieldName.Length);
        if(!remaining.StartsWith("=") && !string.IsNullOrEmpty(fieldName)) {
            remaining = str;
            result = default;
            return false;
        }

        if(remaining.StartsWith("=")) remaining = remaining.Substring(1);
        if(!remaining.StartsWith("(")) {
            remaining = str;
            result = default;
            return false;
        }

        remaining = remaining.Substring(1);
        string val = remaining;
        if(!GetFloat("", val, out float x, out remaining, ',')) {
            Debug.Log("Failed to get vector x");
            remaining = str;
            result = default;
            return false;
        }

        val = remaining;
        if(!GetFloat("", val, out float y, out remaining, ',')) {
            Debug.Log("Failed to get vector y");
            remaining = str;
            result = default;
            return false;
        }

        val = remaining;
        if(!GetFloat("", val, out float z, out remaining, ')')) {
            remaining = str;
            result = default;
            return false;
        }

        if(!remaining.StartsWith(";") && !string.IsNullOrEmpty(fieldName)) {
            Debug.Log("Failed to get vector z");
            remaining = str;
            result = default;
            return false;
        }

        if(remaining.StartsWith(";")) remaining = remaining.Substring(1);

        if(remaining.StartsWith("\n")) {
            remaining = remaining.Substring(1);
        }

        result = new Vector3(x, y, z);
        return true;
    }

    public static bool GetFloat(string fieldName, string str, out float result, out string remaining,
        char delim = ';') {
        if(!str.StartsWith(fieldName)) {
            Debug.Log($"Does not start with field name {fieldName}; str={str}");
            remaining = str;
            result = 0;
            return false;
        }

        remaining = str.Substring(fieldName.Length);
        if(!remaining.StartsWith("=") && !string.IsNullOrEmpty(fieldName)) {
            Debug.Log("Missing '=' sign");
            remaining = str;
            result = 0;
            return false;
        }

        if(remaining.StartsWith("=")) remaining = remaining.Substring(1);
        int end = 0;
        for(int i = 0; i < remaining.Length; i++) {
            if(remaining[i] != delim) continue;
            end = i + 1;
            break;
        }

        if(end == 0) {
            Debug.Log($"Missing delimiter '{delim}");
            remaining = str;
            result = 0;
            return false;
        }

        string val = remaining.Substring(0, end - 1);
        if(!float.TryParse(val, out result)) {
            Debug.Log($"Could not parse as float: {val}; remaining={remaining}, start=0, end={end - 1}");
            remaining = str;
            result = 0;
            return false;
        }

        remaining = remaining.Substring(end);
        if(remaining.StartsWith("\n")) {
            remaining = remaining.Substring(1);
        }

        return true;
    }
}
