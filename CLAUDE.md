# PathFinder Simulator 3D - CLAUDE.md

## 프로젝트 개요

- **프로젝트명**: PathFinder Simulator 3D
- **엔진**: Unity 6
- **언어**: C#
- **소속**: Korea Univ. Sejong Campus S-CURT 2026-1

---

## 1. 파일 및 폴더 규칙

- 모든 스크립트는 반드시 `Assets/Scripts/` 폴더 하위에 생성한다.
- 비슷한 용도의 스크립트는 하위 폴더로 분류하여 관리한다.
  - 예시: Player 관련 → `Assets/Scripts/Player/PlayerController.cs`
  - 예시: UI 관련 → `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/` 하위 스크립트를 제외한 나머지 파일(Scene, Prefab, Asset, Settings 등)은 절대 수정하지 않는다.
  - 불가피하게 수정이 필요한 경우 반드시 사용자에게 먼저 수락 요청을 보낸 후 진행한다.
- Scene / Prefabs / Assets(비스크립트)는 절대 건드리지 않는다.

---

## 2. 함수명 규칙

- PascalCase 사용: 첫 글자를 대문자로 시작한다.
  - 예시: `MovePlayer()`, `CalculatePath()`
- **기존에 존재하는 함수명은 절대로 변경하지 않는다. 대소문자 변경도 금지.**

---

## 3. 변수명 규칙

- camelCase 사용: 첫 글자를 소문자로 시작한다.
  - 예시: `moveSpeed`, `targetPosition`
- 인스펙터에 노출이 필요한 private/protected 변수는 `[SerializeField]`를 사용한다.
  - 외부 접근이 필요한 경우에만 `public`으로 선언한다.
- 변수는 항상 클래스/함수 상단에 모아서 선언한다.
- **기존에 존재하는 변수명은 절대로 변경하지 않는다. 전역 변수는 특히 더 주의. 대소문자 변경도 금지.**

---

## 4. 주석 규칙

- 주석은 반드시 한국어로 작성한다.
- 특수문자는 `.,`을 제외하고 사용을 최소화하며, 문장은 간결하고 정적으로 작성한다.
- 각 변수 선언부 위와 각 함수 선언부 위에 해당 변수/함수의 기능 및 사용처를 설명하는 주석을 반드시 작성한다.

---

## 5. TODO 규칙

- 각 스크립트 파일 내 최상단(클래스 선언 위 또는 클래스 상단)에 해당 스크립트의 사용처 및 구현된 기능을 `//TODO: ` 뒤에 상세히 기록한다.
  ```csharp
  //TODO: 이 스크립트는 플레이어의 이동을 담당합니다.
  //TODO: 구현 기능: 이동, 점프, 애니메이션 연동
  ```

---

## 6. Design Pattern 규칙

- 스크립트에 특정 디자인 패턴이 적용된 경우 TODO 주석 아래에 명시한다.
  ```csharp
  //Design Pattern: Singleton
  ```

---

## 7. 인터페이스 규칙

- 인터페이스명은 `I`로 시작하고 PascalCase를 따른다.
  - 예시: `IMovable`, `IInteractable`
- 인터페이스 클래스 선언부 위에 사용 목적, 사용처, 구현 대상 등을 상세히 주석으로 작성한다.

---

## 8. 금지 규칙

- 외부 CDN 라이브러리 추가 금지.
- **기존 변수명/함수명은 절대 변경 금지. 대소문자 변경도 금지.**
- 기존 변수/함수가 더 이상 사용되지 않게 된 경우, 삭제하지 않고 위에 아래 주석을 추가한다.
  ```csharp
  //FIXME: 아래 변수/함수는 더이상 사용하지 않습니다. 삭제를 권장합니다.
  ```
- Scene / Prefabs / Assets(비스크립트) 수정 금지.
- 에러 및 예외 처리는 유니티의 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`를 사용한다.
- `namespace` 사용 시 반드시 사용자에게 허락 요청을 먼저 보내야 한다.
  - 요청 시 다음 사항을 반드시 명시: 어느 스크립트에, 어떤 범위로, 어떤 목적으로 namespace를 적용할 것인지.

---

## 9. using 규칙

- 파일 최상단에 실제로 사용하는 `using`만 선언한다. 사용하지 않는 `using`은 작성하지 않는다.

---

## 10. 스크립트 템플릿 (요약)

```csharp
using UnityEngine;

//TODO: 이 스크립트의 사용처 및 구현 기능을 여기에 상세히 기술
//Design Pattern: (사용된 패턴이 있으면 명시, 없으면 생략)

// 클래스 기능에 대한 설명 주석
public class ExampleClass : MonoBehaviour
{
    // 인스펙터 노출 변수
    [SerializeField]
    // 이동 속도
    private float moveSpeed;

    // 외부 접근이 필요한 변수
    // 현재 체력
    public int currentHp;

    // 이동 처리 함수
    private void Move()
    {
        // 구현 내용
    }
}
```

---

## 11. 스크립트 폴더 구조 예시

```
Assets/
└── Scripts/
    ├── Player/
    │   ├── PlayerController.cs
    │   └── PlayerStats.cs
    ├── AI/
    │   ├── PathFinder.cs
    │   └── AgentController.cs
    ├── UI/
    │   └── UIManager.cs
    └── Core/
        └── GameManager.cs
```
