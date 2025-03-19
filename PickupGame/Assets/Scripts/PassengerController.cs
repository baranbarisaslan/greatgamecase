using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PassengerController : MonoBehaviour
{
    public float jumpDelay = 0.5f;
    public float jumpDuration = 0.5f;
    public float jumpPower = 2f;
    public int numJumps = 1;
    public bool destroyAfterAssignment = true;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public IEnumerator ProcessAssignment(Transform seat, bool isFirst, Vector3 firstPosition)
    {
        yield return AnimateAndFinish(seat, isFirst, firstPosition);
    }

    private IEnumerator AnimateAndFinish(Transform seat, bool isfirst, Vector3 firstPosition)
    {
        if (isfirst)
        {
            animator.SetBool("Jump", true);
            yield return new WaitForSeconds(jumpDelay);
            transform.SetParent(null);
            if (seat != null)
            {
                yield return transform.DOJump(seat.position, jumpPower, numJumps, jumpDuration)
                    .SetEase(Ease.OutQuad)
                    .WaitForCompletion();
                seat.gameObject.SetActive(true);
                Animator seatAnimator = seat.GetComponent<Animator>();
                if (seatAnimator != null)
                    seatAnimator.SetBool("Sit", true);
            }
            if (destroyAfterAssignment)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
        else
        {
            yield return transform.DOMove(firstPosition, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .WaitForCompletion();
            animator.SetBool("Walk", true);
            yield return new WaitForSeconds(0.25f);
            animator.SetBool("Jump", true);
            transform.SetParent(null);

            if (seat != null)
            {
                yield return transform.DOJump(seat.position, jumpPower, numJumps, jumpDuration)
                    .SetEase(Ease.OutQuad)
                    .WaitForCompletion();
                seat.gameObject.SetActive(true);
                Animator seatAnimator = seat.GetComponent<Animator>();
                if (seatAnimator != null)
                    seatAnimator.SetBool("Sit", true);
            }
            if (destroyAfterAssignment)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);

        }
 
    }
}
