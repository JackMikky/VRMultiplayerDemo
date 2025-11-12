using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

using UnityEngine.InputSystem;

#endif

public class StrokeDrawer : MonoBehaviour
{
    public GameObject linePrefab;
    public Transform drawSource;
    private KeyCode drawKey = KeyCode.F;

    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();
    private float minDistance = 0.01f;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

    // 缓存新版输入系统对应的 Key（如果能映射）
    private Key _mappedKey = Key.None;

    private void Awake()
    {
        // 尝试把 KeyCode 名称映射到 Input System 的 Key 枚举（常见字符键可映射）
        try
        {
            _mappedKey = (Key)System.Enum.Parse(typeof(Key), drawKey.ToString(), true);
        }
        catch
        {
            _mappedKey = Key.None;
        }
    }

#endif

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // 使用新输入系统
        if (_mappedKey != Key.None && Keyboard.current != null)
        {
            var keyControl = Keyboard.current[_mappedKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                StartStroke();
            }

            if (keyControl != null && keyControl.isPressed && currentLine != null)
            {
                Debug.Log("Drawing stroke point");
                Vector3 newPoint = drawSource.position;
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], newPoint) > minDistance)
                {
                    AddPoint(newPoint);
                }
            }

            if (keyControl != null && keyControl.wasReleasedThisFrame)
            {
                EndStroke();
            }
        }
        else
        {
            // 如果无法映射 KeyCode 到 Key，则退回到不响应按键（避免抛异常）
        }
#else
        // 旧输入系统（Legacy Input Manager）
        if (Input.GetKeyDown(drawKey))
        {
            StartStroke();
        }

        if (Input.GetKey(drawKey) && currentLine != null)
        {
            Vector3 newPoint = drawSource.position;
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], newPoint) > minDistance)
            {
                AddPoint(newPoint);
            }
        }

        if (Input.GetKeyUp(drawKey))
        {
            EndStroke();
        }
#endif
    }

    private void StartStroke()
    {
        GameObject lineObj = Instantiate(linePrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();
        points.Clear();
        AddPoint(drawSource.position);
    }

    private void AddPoint(Vector3 point)
    {
        points.Add(point);
        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    private void EndStroke()
    {
        //currentLine = null;
        //points.Clear();
    }
}