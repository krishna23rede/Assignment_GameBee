using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CalculatorManager : MonoBehaviour
{
    #region Singleton & State

    public static CalculatorManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI displayText;

    // Tracks whether the last action was an evaluation
    private bool isResultDisplayed = false;

    // Stores the full mathematical expression as a string
    private string currentExpression = "";

    #endregion


    #region Unity Lifecycle

    // Ensures only one CalculatorManager exists (Singleton pattern)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Initializes calculator state
    private void Start()
    {
        ResetAll();
    }

    #endregion


    #region Button Input Handling

    // Entry point for all calculator button presses
    public void ButtonPressed(string input)
    {
        switch (input)
        {
            case "AC":
                ResetAll();
                return;

            case "DEL":
                Backspace();
                return;

            case "=":
                EvaluateExpression();
                return;
        }

        HandlePostResultState(input);

        if (char.IsDigit(input[0])) // check if 0-9
        {
            HandleLeadingZero();
        }

        if (IsOperator(input))
        {
            if (!HandleOperator(input))
                return;
        }

        if (input == ".")
        {
            if (!HandleDecimal())
                return;
        }

        currentExpression += input;
        UpdateDisplay(currentExpression);
    }

    // Clears expression if a number is entered after a result is shown
    private void HandlePostResultState(string input)
    {
        if (!isResultDisplayed)
            return;

        if (char.IsDigit(input[0]) || input == ".")
        {
            currentExpression = "";
        }

        isResultDisplayed = false;
    }

    // Prevents numbers like 0666 by removing leading zero
    private void HandleLeadingZero()
    {
        string lastNumber = GetLastNumber();

        if (lastNumber == "0")
        {
            currentExpression =
                currentExpression.Remove(currentExpression.Length - 1);
        }
    }

    // Validates and handles operator input
    private bool HandleOperator(string input)
    {
        // Prevent operator at start (except minus)
        if (currentExpression.Length == 0 && input != "-")
            return false;

        char lastChar = currentExpression[^1];

        // Prevent operator after decimal point
        if (lastChar == '.')
            return false;

        // Replace previous operator if pressed consecutively
        if (IsOperator(lastChar.ToString()))
        {
            currentExpression =
                currentExpression.Remove(currentExpression.Length - 1);
        }

        return true;
    }

    // Handles decimal input and prevents multiple decimals in a number
    private bool HandleDecimal()
    {
        string lastNumber = GetLastNumber();

        // Convert ".5" → "0.5"
        if (lastNumber == "")
        {
            currentExpression += "0";
            return true;
        }

        // Prevent multiple decimals in the same number
        if (lastNumber.Contains("."))
            return false;

        return true;
    }

    #endregion


    #region Display & Editing

    // Updates the calculator display
    private void UpdateDisplay(string text)
    {
        displayText.text = text;
    }

    // Removes the last entered character
    private void Backspace()
    {
        if (isResultDisplayed)
        {
            ResetAll();
            return;
        }

        if (currentExpression.Length == 0)
            return;

        currentExpression =
            currentExpression.Remove(currentExpression.Length - 1);

        UpdateDisplay(
            currentExpression.Length == 0 ? "0" : currentExpression
        );
    }

    // Resets calculator to initial state
    private void ResetAll()
    {
        currentExpression = "";
        isResultDisplayed = false;
        UpdateDisplay("0");
    }

    #endregion


    #region Expression Utilities

    // Extracts the most recent number from the expression
    private string GetLastNumber()
    {
        string number = "";
        for (int i = currentExpression.Length - 1; i >= 0; i--)
        {
            if (IsOperator(currentExpression[i].ToString()))
                break;

            number = currentExpression[i] + number;
        }
        return number;
    }

    // Checks if a character is a supported operator
    private bool IsOperator(string c)
    {
        return c == "+" || c == "-" || c == "*" || c == "÷";
    }

    #endregion


    #region Evaluation & Formatting

    // Evaluates the current expression and updates display
    private void EvaluateExpression()
    {
        try
        {
            double result = Evaluate(currentExpression);
            string formattedResult = FormatResult(result);

            UpdateDisplay(formattedResult);
            currentExpression = formattedResult;
            isResultDisplayed = true;
        }
        catch
        {
            isResultDisplayed = false;
        }
    }

    // Formats result to a maximum of 4 decimal places
    private string FormatResult(double value)
    {
        return Math.Round(value, 4).ToString("0.####");
    }

    // Evaluates expression using DMAS rules
    private double Evaluate(string expression)
    {
        List<string> tokens = Tokenize(expression);
        tokens = ProcessMultiplicationDivision(tokens);
        return ProcessAdditionSubtraction(tokens);
    }

    #endregion


    #region Parsing & DMAS Logic

    // Converts expression string into numbers and operators
    private List<string> Tokenize(string expression)
    {
        List<string> tokens = new List<string>();
        string number = "";

        for (int i = 0; i < expression.Length; i++)
        {
            char c = expression[i];

            if (char.IsDigit(c) || c == '.')
            {
                number += c;
            }
            else if (IsOperator(c.ToString()))
            {
                if (number != "")
                {
                    tokens.Add(number);
                    number = "";
                }

                // Handle negative numbers
                if (c == '-' && (i == 0 || IsOperator(expression[i - 1].ToString())))
                {
                    number += c;
                }
                else
                {
                    tokens.Add(c.ToString());
                }
            }
        }

        if (number != "")
            tokens.Add(number);

        return tokens;
    }

    // Processes multiplication and division first
    private List<string> ProcessMultiplicationDivision(List<string> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == "*" || tokens[i] == "÷")
            {
                double left = Convert.ToDouble(tokens[i - 1]);
                double right = Convert.ToDouble(tokens[i + 1]);
                double result =
                    tokens[i] == "*" ? left * right : left / right;

                tokens[i - 1] = result.ToString();
                tokens.RemoveAt(i);
                tokens.RemoveAt(i);
                i--;
            }
        }
        return tokens;
    }

    // Processes addition and subtraction last
    private double ProcessAdditionSubtraction(List<string> tokens)
    {
        double result = Convert.ToDouble(tokens[0]);

        for (int i = 1; i < tokens.Count; i += 2)
        {
            string op = tokens[i];
            double next = Convert.ToDouble(tokens[i + 1]);

            if (op == "+") result += next;
            else if (op == "-") result -= next;
        }
        return result;
    }

    #endregion
}