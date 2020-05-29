using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class Messages : MonoBehaviour {
    private InputField _inputField;
    private Image _image;
    private Text _text;

    [SerializeField] private float fadeRate = .5f;

    private void Awake() {
        _inputField = GetComponent<InputField>();
        _image = _inputField.image;
        _text = _inputField.textComponent;
    }

    private void Update() {
        float multiplier = Mathf.Pow(1 - fadeRate, Time.smoothDeltaTime);
        Color c = _image.color;
        c.a *= multiplier;
        _image.color = c;
        c = _text.color;
        c.a *= multiplier;
        _text.color = c;
    }

    public void ShowMessage(string message) {
        _inputField.text = message;
        Color c = _image.color;
        c.a = 1;
        _image.color = c;
        c = _text.color;
        c.a = 1;
        _text.color = c;
    }
}
