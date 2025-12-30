using System;
using UnityEngine;
using UnityEngine.UI;

public class CalcButton : MonoBehaviour
{
    [SerializeField] private string value;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        CalculatorManager.Instance.ButtonPressed(value);
    }
}