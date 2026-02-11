using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using AOT;
using System.Collections.Concurrent; // [MonoPInvokeCallback] 필수

public class MultiWindow : MonoBehaviour
{
    // 플랫폼별 라이브러리 이름 매핑
#if UNITY_STANDALONE_WIN
    const string DLL_NAME = "MultiWindowD3D11";
#elif UNITY_STANDALONE_LINUX
    const string DLL_NAME = "libMultiWindowLinux";
#endif

    // --- Native Imports ---
    [DllImport(DLL_NAME)] private static extern IntPtr StartSubWindow(IntPtr texturePtr, int width, int height);
    [DllImport(DLL_NAME)] private static extern void StopSubWindow(IntPtr handle);
    [DllImport(DLL_NAME)] private static extern void SignalFrameReady(IntPtr handle);
    [DllImport(DLL_NAME)] private static extern void SetEventCallback(IntPtr handle, IntPtr callback);
    [DllImport(DLL_NAME)] private static extern void SetCloseCallback(IntPtr handle, IntPtr callback);
    [DllImport(DLL_NAME)] private static extern void UpdateTexture(IntPtr handle, IntPtr texturePtr);
    [DllImport(DLL_NAME)] private static extern void FocusWindow(IntPtr handle);
    [DllImport(DLL_NAME)] private static extern void SetConfig(IntPtr handle,
        int x, int y, int w, int h, string title, 
        bool borderless, bool transparent, bool resizable, bool minBtn, bool maxBtn);
    
    IntPtr windowHandle = IntPtr.Zero;

    // --- Data Types ---
    private enum NativeEventType {
        Closed=0, Moved=1, Resized=2, FocusGained=3, FocusLost=4, 
        Minimized=5, Maximized=6, Restored=7
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EventCallbackDelegate(IntPtr handle, int type, int data1, int data2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool CloseCallbackDelegate(IntPtr handle);

    // --- Event Queue (For Main Thread) ---
    private struct NativeEvent { public NativeEventType type; public int d1, d2; }
    private Queue<NativeEvent> eventQueue = new Queue<NativeEvent>();
    private object queueLock = new object();

    // --- Instance & Delegates ---
    // [중요] 모든 창 인스턴스를 관리하는 딕셔너리
    private static ConcurrentDictionary<IntPtr, MultiWindow> instances = new ConcurrentDictionary<IntPtr, MultiWindow>();
    
    // [중요] 정적 콜백 델리게이트 (GC 방지용)
    private static EventCallbackDelegate evtDel;
    private static CloseCallbackDelegate closeDel;

    // --- Public Settings ---
    public Camera targetCamera;
    public string title = "Sub Window";
    public Vector2Int size = new Vector2Int(800, 600);
    public bool borderless = false;
    public bool transparent = false;
    public bool preventClose = false;
    public bool resizable = true;
    public bool minBtn = true;
    public bool maxBtn = true;

    // Events
    public Action<Vector2Int> OnResize;
    public Action OnClose;

    private RenderTexture rt;
    
    void Awake() // Start 대신 Awake 권장
    {
        // 최초 1회만 정적 델리게이트 생성
        if (evtDel == null) {
            evtDel = OnStaticEvent;
            closeDel = OnStaticCloseCheck;
        }
    }

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        CreateRT(size.x, size.y);

        // 1. 창 생성 및 핸들 받기
        windowHandle = StartSubWindow(rt.GetNativeTexturePtr(), size.x, size.y);

        if (windowHandle != IntPtr.Zero)
        {
            // 2. 딕셔너리에 등록 (나중에 찾기 위해)
            instances[windowHandle] = this;

            // 3. 콜백 연결
            SetEventCallback(windowHandle, Marshal.GetFunctionPointerForDelegate(evtDel));
            SetCloseCallback(windowHandle, Marshal.GetFunctionPointerForDelegate(closeDel));

            ApplyConfig();
            StartCoroutine(RenderLoop());
        }
    }

    void CreateRT(int w, int h) {
        if (rt != null) rt.Release();
        rt = new RenderTexture(w, h, 24);
        rt.Create();
        targetCamera.targetTexture = rt;
    }

    // [C++ Call -> Thread Safe Queue]
    [MonoPInvokeCallback(typeof(EventCallbackDelegate))]
    private static void OnStaticEvent(IntPtr handle, int type, int d1, int d2)
    {
        if (instances.TryGetValue(handle, out var window))
        {
            window.EnqueueEvent(type, d1, d2);
        }
    }

    [MonoPInvokeCallback(typeof(CloseCallbackDelegate))]
    private static bool OnStaticCloseCheck(IntPtr handle)
    {
        if (instances.TryGetValue(handle, out var window))
        {
            if (window.preventClose) return false;
        }
        return true;
    }

    private void EnqueueEvent(int type, int d1, int d2)
    {
        lock (queueLock) {
            eventQueue.Enqueue(new NativeEvent { type = (NativeEventType)type, d1 = d1, d2 = d2 });
        }
    }

    void Update()
    {
        lock (queueLock) {
            while (eventQueue.Count > 0) {
                var e = eventQueue.Dequeue();
                ProcessEvent(e);
            }
        }
    }
    
    void ProcessEvent(NativeEvent e)
    {
        switch(e.type) {
            case NativeEventType.Closed:
                Debug.Log("Window Closed");
                OnClose?.Invoke();
                Destroy(gameObject);
                break;
            case NativeEventType.Resized:
                if (e.d1 > 0 && e.d2 > 0) {
                    // [핵심] RT 리사이징 및 C++ 포인터 업데이트
                    CreateRT(e.d1, e.d2);
                    UpdateTexture(windowHandle, rt.GetNativeTexturePtr());
                    OnResize?.Invoke(new Vector2Int(e.d1, e.d2));
                    Debug.Log("Window Resized");
                }
                break;
            case NativeEventType.FocusGained:
                Debug.Log("Window Focus Gained");
                break;
            case NativeEventType.FocusLost:
                Debug.Log("Window Focus Lost");
                break;
            case NativeEventType.Moved:
                Debug.Log("Window Moved");
                break;
            case NativeEventType.Minimized:
                Debug.Log("Window Minimized");
                break;
            case NativeEventType.Maximized:
                Debug.Log("Window Maximized");
                break;
            case NativeEventType.Restored:
                Debug.Log("Window Restored");
                break;
        }
    }

    public void ApplyConfig() {
        if (windowHandle == IntPtr.Zero) return;
        SetConfig(windowHandle, 100, 100, size.x, size.y, title, borderless, transparent, resizable, minBtn, maxBtn);
    }

    public void SetFocus()
    {
        if (windowHandle == IntPtr.Zero) return;
        FocusWindow(windowHandle);
    }

    IEnumerator RenderLoop() {
        while (windowHandle != IntPtr.Zero) {
            yield return new WaitForEndOfFrame();
            // [중요] C++ 스레드를 깨우는 신호
            SignalFrameReady(windowHandle);
        }
    }

    void OnDestroy()
    {
        if (windowHandle != IntPtr.Zero)
        {
            // 딕셔너리에서 제거
            instances.TryRemove(windowHandle, out _);
            
            StopSubWindow(windowHandle);
            windowHandle = IntPtr.Zero;
        }
        if (rt) rt.Release();
    }
}