using System;
using UnityEngine;

[Serializable]
public class CustomerData
{
    public string name;
    public string shortBio;
    public Color color;
    public string initial;

    public CustomerData(string name, string shortBio, Color color, string initial)
    {
        this.name = name;
        this.shortBio = shortBio;
        this.color = color;
        this.initial = initial;
    }
}
