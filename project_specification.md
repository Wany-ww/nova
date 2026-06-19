제시해주신 요구사항을 바탕으로, C# 백엔드와 웹 기술(Frontend)을 결합한 **노드 기반 비주얼 스크립팅 시스템(가칭: FlowEngine)**의 전체 명세서입니다. 이 문서는 개발자가 실제 구현에 착수할 수 있도록 기술 스택, UI 구조, 노드 로직, 그리고 Lua 연동 상세를 포함합니다.

---

# 프로젝트 명세서: FlowEngine (C# 기반 웹 윈도우 애플리케이션)

## 1. 프로젝트 개요
본 프로젝트는 **ComfyUI**와 유사한 노드 기반 인터페이스를 통해 사용자가 로직을 설계하고 실행할 수 있는 윈도우 애플리케이션입니다. C#의 강력한 백엔드 기능과 웹 기술의 유연한 UI 표현력을 결합하여, Lua 스크립트 기반의 워크플로우를 시각적으로 제어하는 것을 목적으로 합니다.

## 2. 기술 스택
*   **Framework**: .NET 8/9 기반 **WPF 또는 WinForms** + **WebView2** (Blazor Hybrid 추천)
*   **Frontend (Canvas UI)**: React 또는 Svelte (노드 라이브러리: React Flow 또는 커스텀 라이브러리)
*   **Scripting Engine**: **MoonSharp** 또는 **NLua** (C# 내 Lua 인터프리터)
*   **Communication**: 시스템 호출 및 데이터 전달을 위한 가교(JS Bridge)

## 3. UI/UX 전체 구성
애플리케이션은 크게 5개의 영역으로 나뉩니다.

### 3.1. 메뉴 (Menu Bar)
*   **프로젝트**: 불러오기(Open), 저장하기(Save/Save As), 끝내기(Exit)
*   **편집**: 취소(Undo), 다시 실행(Redo), 전체 삭제
*   **설정**: 테마 변경, 폰트 크기, Lua 라이브러리 경로 설정

### 3.2. 사이드바 (Sidebar)
*   **기능**: 노드 라이브러리 트리 뷰.
*   **동작**: 노드 아이콘을 드래그하여 Canvas에 드롭(Drag & Drop) 시 해당 노드 인스턴스 생성.
*   **가시성**: 토글 버튼을 통해 숨기기/보이기 가능.

### 3.3. 캔버스 (Canvas)
*   **핵심 기능**: 노드 배치, 노드 간 연결(Link), 노드 삭제, 줌/팬(Zoom/Pan).
*   **연결 규칙**: 출력 핀(Output Pin)과 입력 핀(Input Pin)의 타입이 일치할 때만 연결 허용.
*   **상태 표시**: 실행 중인 노드는 테두리 하이라이트 표시.

### 3.4. 로그 및 콘솔 창 (Bottom Panel)
*   **Log 창**: 사용자 정의 로그(`print()` 등).
    *   Filter: [ ] Info, [ ] Warn, [ ] Error 체크박스로 필터링 가능.
*   **Console 창**: 시스템 동작 로그(노드 로드 완료, 스크립트 컴파일 에러, 엔진 상태 등)를 별도 표시.

---

## 4. 노드(Node) 상세 명세

### 4.1. 노드 구조 (UI)
각 노드는 다음과 같은 레이아웃을 가집니다.

```text
┌─────────────────────────────────────────────────────────────┐
│ Name (Header)                         [ - ] cnt / total [ + ]  [ ▶ ] │
├──────────────────────────────┬──────────────────────────────┤
│ [●] a : bool (Input)         │                e : table [●] │
│ [●] b : int                  │                f : image [●] │
│ [●] c : float                │                              │
│ [●] d : string               │                              │
└──────────────────────────────┴──────────────────────────────┘
```
*   **Header**: 노드 이름, 반복 실행 제어(`cnt`/`total`), 실행 버튼(`▶`).
*   **Input Pins (Left)**: 데이터 입력부. 이름과 타입 표시.
*   **Output Pins (Right)**: 데이터 출력부. 이름과 타입 표시.

### 4.2. 노드 속성 및 상세 보기 (Double Click)
노드 더블 클릭 시 팝업 또는 속성 창(Property Grid)이 노출됩니다.
*   **Input Pin 리스트**: 각 핀의 현재 값 수정 가능 (연결되지 않은 경우 직접 입력).
*   **Output Pin 리스트**: 마지막 실행 결과 값 표시 (Read-only).
*   **타입 지원**: `bool`, `int`, `float`, `string`, `table`, `image` 등.

### 4.3. 실행 로직 (Flow Control)
*   **반복 실행**: `total` 값만큼 노드의 로직이 반복 수행되며 `cnt`가 업데이트됩니다.
*   **연쇄 실행**: `▶` 버튼 클릭 시:
    1.  현재 노드의 Lua 스크립트 실행.
    2.  출력 값을 연결된 다음 노드의 입력으로 전달.
    3.  다음 노드의 실행을 재귀적으로 호출 (다중 연결 시 병렬/순차 처리 규칙에 따름).

---

## 5. 스크립트 엔진 및 파싱 (Lua Integration)

### 5.1. 타입 정의 문법 (Type Definition)
사용자는 노드 정의 시 TypeScript와 유사한 Lua 주석 또는 확장 문법을 사용하여 핀을 정의합니다.

**입력 예시:**
```lua
-- @node: AddOperation
-- @description: 두 실수를 더합니다.
function add(a : float, b : float) -> c : float
    return a + b
end
```

### 5.2. Lua 변환 및 실행 메커니즘
시스템은 위 문법을 파싱하여 실제 실행 가능한 Lua 코드로 변환합니다.

1.  **Parsing 단계**: `a : float`에서 이름(`a`)과 타입(`float`)을 추출하여 노드 UI의 Input Pin을 생성합니다.
2.  **Translation 단계**: 실행 시에는 타입 정의가 제거된 순수 Lua 코드로 변환됩니다.
    ```lua
    function add(a, b)
        return a + b
    end
    ```
3.  **Execution 단계**: 
    *   C# 엔진이 입력 핀에 연결된 값들을 Lua 함수의 인자로 전달합니다.
    *   `return`된 값은 노드의 출력 핀(`c`)에 할당됩니다.

---

## 6. 데이터 모델 (Data Schema)

### 6.1. 프로젝트 저장 형식 (JSON)
```json
{
  "project_name": "New Project",
  "nodes": [
    {
      "id": "node_001",
      "type": "AddOperation",
      "pos": { "x": 100, "y": 200 },
      "properties": { "total": 1, "cnt": 0 },
      "inputs": { "a": 10.5, "b": 20.0 },
      "script": "function add(a, b) return a + b end"
    }
  ],
  "links": [
    { "from": "node_001", "output": "c", "to": "node_002", "input": "a" }
  ]
}
```

---

## 7. 주요 개발 로드맵
1.  **Phase 1**: C# WebView2 기반 기본 윈도우 프레임 및 React Flow 캔버스 연동.
2.  **Phase 2**: Lua 파서 구현 및 MoonSharp 연동 테스트.
3.  **Phase 3**: 노드 드래그 앤 드롭 및 핀 연결 로직 구현.
4.  **Phase 4**: 실행 엔진(Flow Runner) 및 로그/콘솔 시스템 구축.
5.  **Phase 5**: 프로젝트 저장/불러오기 및 예외 처리.

---

본 문서는 사용자의 요구사항을 기술적으로 구체화한 것이며, 개발 시 이 가이드를 바탕으로 세부 컴포넌트 설계를 진행할 수 있습니다.