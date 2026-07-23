using System.Collections;
using UnityEngine;

//TODO: Player 오브젝트를 시작점에서 목표점까지 이동시키는 AI 스크립트입니다.
//TODO: 구현 기능: 시작점 스폰, 이동 시작/정지/재시작, PathFinderAI.SetNextPOS 호출을 통한 경로 추종, 목표 도달 감지
//TODO: Move(현재 위치에서 이동), Stop(현재 위치에서 정지), Restart(시작점 복귀 후 이동 재시작)

// Player를 경로대로 이동시키는 AI 클래스
public class PlayerAI : MonoBehaviour
{
    // 경로 계산을 담당하는 PathFinderAI 참조
    [SerializeField]
    private PathFinderAI pathFinderAI;

    // 시작점 Transform
    [SerializeField]
    private Transform startPoint;

    // 목표점 Transform
    [SerializeField]
    private Transform goalPoint;

    // 이동 속도 (유니티 단위/초)
    [SerializeField]
    private float moveSpeed = 3f;

    // 목표 위치 도달 판정 거리
    [SerializeField]
    private float arrivalThreshold = 0.1f;

    // 마지막으로 확인한 목표 위치 (목표 변경 감지용)
    private Vector3 lastKnownGoalPos;

    // 이동 중 여부
    public bool IsMoving { get; private set; }

    // 이동 코루틴 참조
    private Coroutine moveCoroutine;

    // Start: 유효성 검사 후 시작점에 스폰만 수행 (이동은 StartMovement() 호출 시 시작)
    private void Start()
    {
        if (pathFinderAI == null)
        {
            Debug.LogError("PathFinderAI가 할당되지 않았습니다.");
            return;
        }
        if (startPoint == null)
        {
            Debug.LogError("StartPoint가 할당되지 않았습니다.");
            return;
        }
        if (goalPoint == null)
        {
            Debug.LogError("GoalPoint가 할당되지 않았습니다.");
            return;
        }

        // 시작점에 배치만 수행, 이동은 StartMovement() 호출 대기
        transform.position = startPoint.position;
        lastKnownGoalPos   = goalPoint.position;
    }

    // 현재 위치에서 이동 시작. 이미 이동 중이면 재시작 (Move 버튼)
    // 탐색 과정 애니메이션을 먼저 재생한 뒤 이동을 시작한다.
    public void StartMovement()
    {
        if (pathFinderAI == null || goalPoint == null)
        {
            Debug.LogError("PlayerAI 설정이 완료되지 않았습니다.");
            return;
        }

        StopMovement();
        moveCoroutine = StartCoroutine(AnimateThenMove(transform.position));
    }

    // 탐색 과정 애니메이션을 재생한 후 이동을 시작하는 코루틴
    // fromPos에서 목표점까지 경로를 계산하고 탐색 확산을 보여준 뒤 Move를 시작한다.
    private IEnumerator AnimateThenMove(Vector3 fromPos)
    {
        lastKnownGoalPos = goalPoint.position;

        // 탐색 과정 애니메이션 (방문 노드 확산 후 경로 강조)
        // StartCoroutine으로 분리하지 않고 직접 yield하여 하나의 코루틴 트리로 묶는다.
        // 그래야 Stop 시 moveCoroutine 하나만 멈춰도 애니메이션과 이동이 모두 중단된다.
        yield return pathFinderAI.PrepareAndAnimate(fromPos, goalPoint.position);

        yield return Move();
    }

    // 현재 위치에서 이동 정지 및 경로 시각화 제거 (Stop 버튼)
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        IsMoving = false;

        if (pathFinderAI != null)
            pathFinderAI.ClearPathVisualization();
    }

    // 시작점으로 복귀 후 이동 재시작 (Restart 버튼)
    public void ResetToStart()
    {
        if (pathFinderAI == null || startPoint == null || goalPoint == null)
        {
            Debug.LogError("PlayerAI 설정이 완료되지 않았습니다.");
            return;
        }

        StopMovement();

        transform.position = startPoint.position;
        lastKnownGoalPos   = goalPoint.position;

        // 시작점에서 탐색 애니메이션을 재생한 뒤 이동 시작
        moveCoroutine = StartCoroutine(AnimateThenMove(startPoint.position));
    }

    // 경로를 따라 목표점까지 이동하는 코루틴
    // PathFinderAI.SetNextPOS를 반복 호출하여 다음 위치를 받아 이동한다.
    // 장벽/목표 변경 시 MoveToPosition이 조기 종료되어 즉시 재경로를 계산한다.
    private IEnumerator Move()
    {
        IsMoving = true;

        while (true)
        {
            // 목표 도달 확인
            if (Vector3.Distance(transform.position, goalPoint.position) <= arrivalThreshold)
            {
                Debug.Log("목표 지점에 도달했습니다.");
                break;
            }

            // PathFinderAI에서 다음 이동 위치를 받아옴 (장벽/목표 변경 시 자동 재계산)
            Vector3 nextPos = pathFinderAI.SetNextPOS(transform.position, goalPoint.position);
            lastKnownGoalPos = goalPoint.position;

            // 경로 없음: 잠시 대기 후 재시도 (장벽이 치워지거나 목표가 바뀔 때를 기다림)
            if (!pathFinderAI.IsPathReady)
            {
                Debug.LogWarning("유효한 경로가 없습니다. 재시도 중...");
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            // 다음 위치로 이동 (장벽/목표 변경 감지 시 조기 종료)
            // 직접 yield하여 상위 코루틴 트리에 포함시킨다 (Stop 시 함께 중단되도록)
            yield return MoveToPosition(nextPos);
        }

        IsMoving = false;
    }

    // 지정한 위치까지 부드럽게 이동하는 코루틴
    // 장벽 변경(IsPathDirty) 또는 목표 변경이 감지되면 즉시 종료하여 Move에 재경로를 위임한다.
    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
        {
            // 장벽 변경 감지: PathFinderAI가 새 장벽을 발견한 경우 즉시 중단
            if (pathFinderAI.IsPathDirty)
                yield break;

            // 목표 변경 감지: GoalPoint가 이동한 경우 즉시 중단
            if (Vector3.Distance(goalPoint.position, lastKnownGoalPos) > arrivalThreshold)
                yield break;

            Vector3 dir = (targetPos - transform.position).normalized;

            // 이동 방향으로 회전
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (Vector3.Distance(transform.position, targetPos) <= arrivalThreshold)
            transform.position = targetPos;
    }
}
