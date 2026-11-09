using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour {

	static ThreadedDataRequester instance;
	Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

	void Awake() {
		instance = this;
	}

	public static void RequestData(Func<object> generateData, Action<object> callback) {
		var requester = instance;
		if (requester == null) {
			throw new InvalidOperationException("A ThreadedDataRequester must be active before requesting terrain data.");
		}
		ThreadStart threadStart = delegate {
			requester.DataThread (generateData, callback);
		};

		new Thread (threadStart) { IsBackground = true }.Start ();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
		object data;
		try {
			data = generateData ();
		} catch (Exception exception) {
			lock (dataQueue) {
				dataQueue.Enqueue(new ThreadInfo(_ => Debug.LogException(exception), null));
			}
			return;
		}
		lock (dataQueue) {
			dataQueue.Enqueue (new ThreadInfo (callback, data));
		}
	}
		

	void Update() {
		ThreadInfo[] completed;
		lock (dataQueue) {
			completed = dataQueue.ToArray();
			dataQueue.Clear();
		}
		// Callbacks can schedule more work; execute them outside the queue lock.
		foreach (ThreadInfo threadInfo in completed) {
			try {
				threadInfo.callback(threadInfo.parameter);
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}
	}

	void OnDestroy() {
		if (instance == this) instance = null;
	}

	struct ThreadInfo {
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo (Action<object> callback, object parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}
}
