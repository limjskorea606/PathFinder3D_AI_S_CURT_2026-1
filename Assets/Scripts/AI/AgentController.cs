using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Player 오브젝트가 탐색된 경로를 따라 이동하도록 제어하는 스크립트입니다.
//TODO: 구현 기능: 경로 이동, 이동 속도 제어, 이동 정지, Player 자동 탐색

// Player 오브젝트의 경로 이동을 담당하는 에이전트 컨트롤러 클래스
public class AgentController : MonoBehaviour
{
    // 이동할 Player Transform (미설정 시 "Player" 태그 오브젝트 자동 탐색)
    [SerializeField]
    private Transform playerTransform;

    // 이동 속도 (유니티 단위/초)
    [SerializeField]
    private float moveSpeed = 5f;

    // 그리드 위로의 Y축 오프셋 (Player가 셀 위에 떠 있는 높이)
    [SerializeField]
    private float playerYOffset = 0.6f;

    // 경로 첫 노드로 순간이동 여부 (false면 현재 위치에서 이동 시작)
    [SerializeField]
    private bool snapToStartNode = true;

    // 이동 중 여부
    public bool IsMoving { get; private set; }

    // 이동 코루틴 참조
    private Coroutine moveCoroutine;

    // Start: Player Transform 자동 탐색
    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Player 태그 오브젝트를 찾을 수 없습니다. Inspector에서 직접 할당해 주세요.");
            }
        }
    }

    // 경로 이동 시작
    public void StartMovement(List<GridNode> path)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform이 할당되지 않았습니다.");
            return;
        }
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("이동할 경로가 없습니다.");
            return;
        }

        StopMovement();
        moveCoroutine = StartCoroutine(MoveAlongPath(path));
    }

    // 이동 정지
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        IsMoving = false;
    }

    // 경로를 따라 순서대로 이동하는 코루틴
    private IEnumerator MoveAlongPath(List<GridNode> path)
    {
        IsMoving = true;

        // 시작 노드로 즉시 위치 설정
        if (snapToStartNode && path.Count > 0)
        {
            GridNode first = path[0];
            playerTransform.position = new Vector3(first.worldPosition.x, playerYOffset, first.worldPosition.z);
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 targetPos = new Vector3(path[i].worldPosition.x, playerYOffset, path[i].worldPosition.z);

            while (Vector3.Distance(playerTransform.position, targetPos) > 0.01f)
            {
                playerTransform.position = Vector3.MoveTowards(
                    playerTransform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                // 이동 방향으로 회전
                Vector3 dir = (targetPos - playerTransform.position).normalized;
                if (dir != Vector3.zero)
                    playerTransform.rotation = Quaternion.LookRotation(dir);

                yield return null;
            }

            playerTransform.position = targetPos;
        }

        IsMoving = false;
    }
}
