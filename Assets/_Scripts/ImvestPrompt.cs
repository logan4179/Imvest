using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ImvestPrompt
{
    public MathematicalOperation MyOperation;
    public int OperandA;
    public int OperandB;

    public string formattedQuestion;

    public float correctAnswer;
    public float incorrectAnswerA;
    public float incorrectAnswerB;

    /// <summary>
    /// This is just meant as an editor tool for generating the "answer" variables.
    /// </summary>
    public void SolveMe()
    {
        string operationString = "";

        switch (MyOperation)
        {
            case MathematicalOperation.addition:
                Debug.Log("was addition...");
                operationString = "+";
                correctAnswer = OperandA + OperandB;
                break;
            case MathematicalOperation.subtractition:
                operationString = "-";
                correctAnswer = OperandA - OperandB;
                break;
            case MathematicalOperation.multiply:
                operationString = "*";
                correctAnswer = OperandA * OperandB;
                break;
            case MathematicalOperation.divide:
                operationString = "/";
                correctAnswer = OperandA / OperandB;
                break;
            default:
                break;
        }

        Debug.Log($"got answer: '{correctAnswer}'");
        formattedQuestion = $"{OperandA} {operationString} {OperandB} = ";

        Debug.Log($"formattedQuestion: '{formattedQuestion}'");

    }

}