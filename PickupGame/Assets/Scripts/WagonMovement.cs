using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class WagonMovement : MonoBehaviour
{
    public Transform middle, tail;
    public float cellSize = 1.25f;
    public float moveDuration = 0.3f;
    public float moveInterval = 0.3f;
    public LayerMask tileLayer;
    public LayerMask wagonLayer;
    public Canvas Gameover;

    private enum TouchedPart { Head, Tail }
    private TouchedPart currentLeader = TouchedPart.Head;

    private bool isDragging = false;
    private bool isMoving = false;
    private float moveTimer = 0f;

    private int headSlotsFilled = 0;
    private int middleSlotsFilled = 0;
    private int tailSlotsFilled = 0;
    private const int headSlotCount = 2;
    private const int middleSlotCount = 4;
    private const int tailSlotCount = 2;

    public bool isFirst = true;
    public Vector3 firstPosition;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (touch.phase == TouchPhase.Began && Physics.Raycast(ray, out hit))
            {
                if (IsTouchingWagon(hit.transform))
                {
                    currentLeader = (hit.transform == transform || hit.transform.IsChildOf(transform))
                        ? TouchedPart.Head : TouchedPart.Tail;
                    isDragging = true;
                    moveTimer = 0f;
                }
            }

            if (isDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Vector3 targetWorldPos = GetWorldPositionFromTouch(touch.position);
                Vector3 leaderPos = (currentLeader == TouchedPart.Head) ? transform.position : tail.position;
                Vector3 rawDir = targetWorldPos - leaderPos;

                if (rawDir.magnitude < cellSize * 0.15f) return;

                Vector3 quantizedDir = (Mathf.Abs(rawDir.x) >= Mathf.Abs(rawDir.z))
                    ? ((rawDir.x >= 0) ? Vector3.right : Vector3.left)
                    : ((rawDir.z >= 0) ? Vector3.forward : Vector3.back);

                moveTimer += Time.deltaTime;
                if (!isMoving && moveTimer >= moveInterval)
                {
                    MoveFixed(quantizedDir, currentLeader);
                    moveTimer = 0f;
                }
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                moveTimer = 0f;
            }
        }
    }

    bool IsTouchingWagon(Transform hitTransform)
    {
        return hitTransform == transform || hitTransform == middle || hitTransform == tail ||
               hitTransform.IsChildOf(transform) || hitTransform.IsChildOf(middle) || hitTransform.IsChildOf(tail);
    }

    Vector3 GetWorldPositionFromTouch(Vector2 touchPos)
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(touchPos);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
    }

    void MoveFixed(Vector3 direction, TouchedPart leader)
    {
        Transform main = (leader == TouchedPart.Head) ? transform : tail;
        Transform secondary = middle;
        Transform tertiary = (leader == TouchedPart.Head) ? tail : transform;

        Vector3 oldMainPos = main.position;
        Vector3 oldSecondaryPos = secondary.position;
        Vector3 newMainPos = oldMainPos + (direction * cellSize);

        if (IsOnTile(newMainPos) && !IsOccupiedByOtherWagon(newMainPos))
        {
            isMoving = true;
            main.DOLookAt(main.position + direction, moveDuration, AxisConstraint.Y);
            main.DOMove(newMainPos, moveDuration).SetEase(Ease.OutQuad);
            secondary.DOMove(oldMainPos, moveDuration).SetEase(Ease.OutQuad);
            tertiary.DOMove(oldSecondaryPos, moveDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => isMoving = false);
        }
    }

    bool IsOnTile(Vector3 pos)
    {
        return Physics.OverlapSphere(pos, 0.1f, tileLayer).Length > 0;
    }

    bool IsOccupiedByOtherWagon(Vector3 pos)
    {
        return Physics.OverlapSphere(pos, 0.5f, wagonLayer).Length > 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Exit"))
        {
            MeshRenderer exitRenderer = other.GetComponent<MeshRenderer>();
            if (exitRenderer)
            {
                Material exitMat = exitRenderer.material;
                StartCoroutine(FillSlots(exitMat, other.transform));
            }
        }
    }


    IEnumerator FillSlots(Material exitMat, Transform exitTransform)
    {
        List<PartInfo> parts = new List<PartInfo>
        {
            new PartInfo("Head", transform, headSlotCount, headSlotsFilled),
            new PartInfo("Middle", middle, middleSlotCount, middleSlotsFilled),
            new PartInfo("Tail", tail, tailSlotCount, tailSlotsFilled)
        };

        List<PartInfo> matchingParts = new List<PartInfo>();
        foreach (var p in parts)
        {
            MeshRenderer[] renderers = p.part.GetComponentsInChildren<MeshRenderer>();
            bool match = false;
            foreach (var r in renderers)
            {
                if (r.material.name == exitMat.name)
                {
                    match = true;
                    break;
                }
            }
            if (match)
                matchingParts.Add(p);
        }
        if (matchingParts.Count == 0)
            yield break;

        matchingParts.Sort((a, b) =>
        {
            if (a.partName == b.partName) return 0;
            if (a.partName == "Head") return -1;
            if (b.partName == "Head") return 1;
            if (a.partName == "Middle") return -1;
            if (b.partName == "Middle") return 1;
            return 0;
        });

        while (exitTransform.childCount > 0)
        {
            bool assigned = false;
            Transform passenger = exitTransform.GetChild(0);
            foreach (var p in matchingParts)
            {
                if (p.slotsFilled < p.maxSlots)
                {
                    Transform seat = p.part.childCount > p.slotsFilled ? p.part.GetChild(p.slotsFilled) : null;
                    bool isFirst = p.slotsFilled == 0;
                    if (isFirst)
                    {
                        firstPosition = passenger.position;
                    }
                    PassengerController pc = passenger.GetComponent<PassengerController>();
                    if (pc != null)
                    {
                        yield return StartCoroutine(pc.ProcessAssignment(seat, isFirst, firstPosition));
                    }
                    else
                    {
                        passenger.gameObject.SetActive(false);
                    }
                    p.slotsFilled++;
                    Debug.Log(p.partName + " slot " + p.slotsFilled + " filled.");
                    passenger.SetParent(null);
                    assigned = true;
                    yield return new WaitForSeconds(0.5f);
                    break;
                }
            }
            if (!assigned)
                break;
        }
        foreach (var p in matchingParts)
        {
            if (p.partName == "Head")
                headSlotsFilled = p.slotsFilled;
            else if (p.partName == "Middle")
                middleSlotsFilled = p.slotsFilled;
            else if (p.partName == "Tail")
                tailSlotsFilled = p.slotsFilled;
            if (p.slotsFilled >= p.maxSlots)
                Debug.Log(p.partName + " is filled.");
        }
        isFirst = true;
        firstPosition = Vector3.zero;
        if (headSlotsFilled >= headSlotCount && middleSlotsFilled >= middleSlotCount && tailSlotsFilled >= tailSlotCount)
        {
            FindAnyObjectByType<TimerDisplay>().GameOver();
        }
    }

    private class PartInfo
    {
        public string partName;
        public Transform part;
        public int maxSlots;
        public int slotsFilled;

        public PartInfo(string name, Transform partTransform, int max, int filled)
        {
            partName = name;
            part = partTransform;
            maxSlots = max;
            slotsFilled = filled;
        }
    }
}
