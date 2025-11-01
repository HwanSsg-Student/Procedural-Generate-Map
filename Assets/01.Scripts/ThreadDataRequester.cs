using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
public class ThreadDataRequester : MonoBehaviour
{
    static ThreadDataRequester instance;

    Queue<TheadInfo> _dataQueue = new Queue<TheadInfo>();

    private void Awake()
    {
        instance = FindObjectOfType<ThreadDataRequester>();
    }

    void Update()
    {
        if (_dataQueue.Count > 0)
        {
            for (int i = 0; i < _dataQueue.Count; i++)
            {   
                TheadInfo threadInfo = _dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }
    void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();

        lock (_dataQueue)
        {
            _dataQueue.Enqueue(new TheadInfo(callback, data));
        }
    }
    struct TheadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public TheadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}
