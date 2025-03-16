using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class WagonMovement : MonoBehaviour
{
    public Transform middle, tail;
    public float cellSize = 1.25f;
    public float moveDuration = 0.3f;
    public LayerMask tileLayer;
    public LayerMask wagonLayer;

    private Vector2 touchStartPos;
    private bool isDragging = false;
    private bool isMoving = false;
    private int headSlotsFilled = 0;
    private int middleSlotsFilled = 0;
    private int tailSlotsFilled = 0;
    private const int headSlotCount = 2;
    private const int middleSlotCount = 4;
    private const int tailSlotCount = 2;

    public Canvas Gameover;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchingHead(touch))
                {
                    isDragging = true;
                    touchStartPos = touch.position;
                }
            }
            if (isDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended))
            {
                if (!isMoving)
                {
                    Vector2 swipeDelta = touch.position - touchStartPos;
                    if (swipeDelta.magnitude > 50)
                    {
                        Move(GetDirection(swipeDelta));
                        touchStartPos = touch.position;
                    }
                }
                if (touch.phase == TouchPhase.Ended)
                    isDragging = false;
            }
        }
    }

    bool IsTouchingHead(Touch touch)
    {
        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            return hit.transform == transform || hit.transform.IsChildOf(transform.parent);
        return false;
    }

    Vector3 GetDirection(Vector2 swipe)
    {
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            return (swipe.x > 0) ? Vector3.right : Vector3.left;
        else
            return (swipe.y > 0) ? Vector3.forward : Vector3.back;
    }

    void Move(Vector3 direction)
    {
        Vector3 oldHeadPos = transform.position;
        Vector3 oldMiddlePos = middle.position;
        Vector3 newHeadPos = oldHeadPos + (direction * cellSize);
        if (newHeadPos == oldMiddlePos)
            return;
        if (IsOnTile(newHeadPos) && !IsOccupiedByOtherWagon(newHeadPos))
        {
            isMoving = true;
            Quaternion newRotation = Quaternion.LookRotation(direction);
            transform.DORotate(newRotation.eulerAngles, moveDuration).SetEase(Ease.OutQuad);
            transform.DOMove(newHeadPos, moveDuration).SetEase(Ease.OutQuad);
            middle.DOMove(oldHeadPos, moveDuration).SetEase(Ease.OutQuad);
            tail.DOMove(oldMiddlePos, moveDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => isMoving = false);
        }
    }

    bool IsOnTile(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, 0.1f, tileLayer);
        return colliders.Length > 0;
    }

    bool IsOccupiedByOtherWagon(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, 0.1f, wagonLayer);
        foreach (Collider col in colliders)
        {
            if (col.transform == transform || col.transform == middle || col.transform == tail)
                continue;
            return true;
        }
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Exit")
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
                    Animator anim = passenger.GetComponent<Animator>();
                    if (anim)
                        anim.SetBool("Jump", true);
                    yield return new WaitForSeconds(0.5f);
                    if (p.part.childCount > p.slotsFilled)
                    {
                        Transform seat = p.part.GetChild(p.slotsFilled);
                        seat.gameObject.SetActive(true);
                        Animator seatAnim = seat.GetComponent<Animator>();
                        if (seatAnim)
                            seatAnim.SetBool("Sit", true);
                    }
                    passenger.gameObject.SetActive(false);
                    p.slotsFilled++;
                    Debug.Log(p.partName + " slot " + p.slotsFilled + " filled.");
                    passenger.SetParent(null);
                    assigned = true;
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
        if (headSlotsFilled >= headSlotCount && middleSlotsFilled >= middleSlotCount && tailSlotsFilled >= tailSlotCount)
        {
            Transform parentObj = transform.parent;
            if (parentObj != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.OnComplete(() => Destroy(parentObj.gameObject));
                Gameover.gameObject.SetActive(true);
            }
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
