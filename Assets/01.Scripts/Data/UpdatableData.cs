using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdated;
    public bool _autoUpdate;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if(_autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdateValues;
        }
    }

    public void NotifyOfUpdateValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdateValues;
        if (OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }
    }
#endif
}
