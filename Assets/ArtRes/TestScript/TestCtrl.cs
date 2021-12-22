using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cinemachine;
using TMPro;
using MightyTerrainMesh;

public class TestCtrl : MonoBehaviour, IMTWaterHeightProvider
{
    public GameObject waterPlane;
    public GameObject MainPanel;
    public GameObject Pad;
    public CinemachineVirtualCamera vcam;
    public TextMeshProUGUI frameText;
    public GameObject ViewTarget;
    private CinemachineOrbitalTransposer orbitalTransposer;
    private RectTransform padRectTran;
    private Transform avatarSlot;
    private Animator avatarAnim;
    private float preFrameSampleTime = 0;
    private int preFrameSampleCount = 0;
    private Vector3? moveDelta = null;
    void Awake()
    {
        MTWaterHeight.RegProvider(this);
    }
    void OnDestroy()
    {
        MTWaterHeight.UnregProvider(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        EventTrigger trigger = MainPanel.AddComponent<EventTrigger>();
        EventTrigger.Entry onDragEntry = new EventTrigger.Entry();
        onDragEntry.eventID = EventTriggerType.Drag;
        onDragEntry.callback.AddListener((data) => {
            OnMainPanelDrag((PointerEventData)data); }
        );
        trigger.triggers.Add(onDragEntry);
        //
        padRectTran = Pad.GetComponent<RectTransform>();
        EventTrigger triggerPad = Pad.AddComponent<EventTrigger>();
        EventTrigger.Entry onPadDragEntry = new EventTrigger.Entry();
        onPadDragEntry.eventID = EventTriggerType.Drag;
        onPadDragEntry.callback.AddListener((data) => {
            OnPadPanelDrag((PointerEventData)data);
        }
        );
        triggerPad.triggers.Add(onPadDragEntry); 
        EventTrigger.Entry onPadDragStartEntry = new EventTrigger.Entry();
        onPadDragStartEntry.eventID = EventTriggerType.BeginDrag;
        onPadDragStartEntry.callback.AddListener((data) => {
            avatarAnim.ResetTrigger("Walk2Idle");
            avatarAnim.SetTrigger("Idle2Walk");
        }
        );
        triggerPad.triggers.Add(onPadDragStartEntry);
        EventTrigger.Entry onPadDragEndEntry = new EventTrigger.Entry();
        onPadDragEndEntry.eventID = EventTriggerType.EndDrag;
        onPadDragEndEntry.callback.AddListener((data) => {
            avatarAnim.ResetTrigger("Idle2Walk");
            avatarAnim.SetTrigger("Walk2Idle");
            moveDelta = null;
        }
        );
        triggerPad.triggers.Add(onPadDragEndEntry);
        //
        orbitalTransposer = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        avatarSlot = ViewTarget.transform.Find("slot");
        avatarAnim = ViewTarget.GetComponentInChildren<Animator>();
        preFrameSampleTime = Time.time;
        preFrameSampleCount = Time.frameCount;
    }

    void OnMainPanelDrag(PointerEventData eventData)
    {
        if (orbitalTransposer != null && ViewTarget != null)
        {
            var curYVal = orbitalTransposer.m_FollowOffset.y;
            orbitalTransposer.m_FollowOffset.y = Mathf.Clamp(curYVal + eventData.delta.y * 0.03f,
                1, 20);
            ViewTarget.transform.rotation *= Quaternion.Euler(0, eventData.delta.x * 0.12f, 0);
        }
    }
    void OnPadPanelDrag(PointerEventData eventData)
    {
        moveDelta = null;
        if (ViewTarget != null)
        {
            var avatarForward = new Vector3(eventData.position.x - padRectTran.rect.center.x,
                0,
                eventData.position.y - padRectTran.rect.center.y);
            if (avatarForward.magnitude > 0.001f)
            {
                avatarForward.Normalize();
                avatarSlot.localRotation = Quaternion.LookRotation(avatarForward, Vector3.up);
                moveDelta = avatarSlot.rotation * Vector3.forward * Time.deltaTime * 3;
            }
        }
    }

    private void Update()
    {
        if (frameText != null)
        {
            if (Time.time - preFrameSampleTime > 1)
            {
                preFrameSampleTime = Time.time;
                var count = Time.frameCount - preFrameSampleCount;
                preFrameSampleCount = Time.frameCount;
                frameText.text = string.Format("Frame : {0}", count);
            }
        }
        if (moveDelta != null)
        {
            var nextPos = ViewTarget.transform.position + moveDelta.Value;
            nextPos.y = GetHeight(nextPos);
            ViewTarget.transform.position = nextPos;
        }
    }

    private float GetHeight(Vector3 pos)
    {
        if(Terrain.activeTerrains.Length > 0)
        {
            foreach(var t in Terrain.activeTerrains)
            {
                var localPos = t.transform.InverseTransformPoint(pos);
                if (t.terrainData.bounds.Contains(localPos))
                {
                    return t.SampleHeight(pos);
                }
            }
        }
        var h = pos.y;
        if (MTHeightMap.GetHeightInterpolated(pos, ref h))
        {
            return h;
        }
        return pos.y;
    }

    public bool Contains(Vector3 worldPos)
    {
        return true;
    }

    float IMTWaterHeightProvider.GetHeight(Vector3 worldPos)
    {
        if (waterPlane != null)
            return waterPlane.transform.position.y;
        return 0;
    }
}
