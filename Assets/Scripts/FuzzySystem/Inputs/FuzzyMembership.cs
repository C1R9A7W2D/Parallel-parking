using ParkingSystem.FuzzySystem.Inputs;
using System;
using UnityEngine;
using static ParkingSystem.FuzzySystem.Inputs.LinguisticVariables;

public static class FuzzyMembership
{
    // Треугольная функция
    public static float Triangular(float x, float a, float b, float c)
    {
        if (x <= a || x >= c) return 0f;
        if (x == b) return 1f;
        if (x > a && x < b) return (x - a) / (b - a);
        return (c - x) / (c - b);
    }

    // Трапециевидная функция
    public static float Trapezoidal(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x > a && x < b) return (x - a) / (b - a);
        return (d - x) / (d - c);
    }

    // Гауссова функция
    public static float Gaussian(float x, float mean, float sigma)
    {
        float exponent = -0.5f * Mathf.Pow((x - mean) / sigma, 2);
        return Mathf.Exp(exponent);
    }
}

// 4. FuzzySet.cs - Класс нечеткого множества
[System.Serializable]
public class FuzzySet
{
    public string name; // "VeryLow", "Low", "Medium", "High", "VeryHigh"
    public MembershipType type;
    public float[] parameters; // Параметры функции

    public float GetMembership(float crispValue)
    {
        switch (type)
        {
            case MembershipType.Triangular:
                return FuzzyMembership.Triangular(crispValue,
                    parameters[0], parameters[1], parameters[2]);
            case MembershipType.Trapezoidal:
                return FuzzyMembership.Trapezoidal(crispValue,
                    parameters[0], parameters[1], parameters[2], parameters[3]);
            case MembershipType.Gaussian:
                return FuzzyMembership.Gaussian(crispValue,
                    parameters[0], parameters[1]); // mean, sigma
            default:
                return 0f;
        }
    }
}