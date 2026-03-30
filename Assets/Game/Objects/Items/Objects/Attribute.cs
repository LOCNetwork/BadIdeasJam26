using System;
using UnityEngine;

[Serializable]
public class Attribute
{
    public Attributes key;
    public string value;

    public Attribute Clone()
    {
        Attribute attribute = new Attribute();
        attribute.key = key;
        attribute.value = value;

        return attribute;
    }
}
