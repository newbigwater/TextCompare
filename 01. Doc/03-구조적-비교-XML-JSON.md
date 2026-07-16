---
title: 구조적 비교 (XML/JSON)
tags: [textcompare, 설계서, xml, json]
---

# 구조적 비교 (XML/JSON)

[[00-개요|← 개요로 돌아가기]]

## 왜 라인 비교로는 부족한가

실무 XML(예: 인터페이스 설계서)에서는 엘리먼트가 버전 사이에 추가/삭제/순서 변경되며, **`id` 속성 값이 다른 버전에서 다른 논리적 엔티티에 재사용**되는 경우가 있다. 이 상태에서 라인 기준(또는 `id` 기준) 비교를 하면 서로 무관한 두 엔티티가 "값이 바뀐 같은 항목"으로 잘못 매칭된다. [[02-핵심-비교-알고리즘#ghost-line-정렬|ghost-line 정렬]]은 라인 밀림은 해결하지만,애초에 "이 두 XML 엘리먼트가 같은 개체인지"를 판단하는 문제는 풀지 못한다 — 이를 위해 **객체(트리) 단위 구조 비교**가 필요하다.

## 공용 트리 모델 (`StructuredNode`)

XML과 JSON을 하나의 형태로 표현한다:

| 필드 | XML에서의 의미 | JSON에서의 의미 |
|---|---|---|
| `Name` | 태그명 | 프로퍼티명(배열 항목은 부모가 null 부여) |
| `Kind` | `Element` | `Element`(오브젝트) 또는 `Array` |
| `Attributes` | 속성(순서 보존) | 사용 안 함 |
| `Value` | 텍스트 콘텐츠(리프인 경우) | 스칼라 값의 원문 표현 |
| `ScalarKind` | 사용 안 함 | `"string"`/`"number"`/`"boolean"`/`"null"`(재직렬화 시 따옴표 여부 결정) |
| `Children` | 자식 엘리먼트 | 오브젝트 프로퍼티 또는 배열 항목 |

- **`XmlStructureParser`**: `System.Xml.Linq.XDocument`(BCL 내장, NuGet 불필요)로 파싱. `XDocument.Load(path)`로 XML 선언의 인코딩을 직접 신뢰한다(`EncodingDetector`의 휴리스틱보다 신뢰도가 높음).
- **`JsonStructureParser`**: 자체 구현 재귀하강 파서. `JavaScriptSerializer`(Dictionary 기반)를 쓰지 않은 이유는 **프로퍼티 순서를 보존해야** diff 정렬이 안정적이기 때문.

## 자연 키 전략 (`NodeKeyStrategy`)

형제 노드 목록(같은 부모의 자식들)에서 좌/우 사이의 안정적인 자연 키를 우선순위대로 탐색한다:

1. **노드 자신의 이름**이 양쪽에서 충분히 유일(90% 이상) → JSON 오브젝트 프로퍼티, 혹은 서로 다른 태그명을 가진 XML 형제(`Header`/`Body`/`Return` 등)
2. **후보 속성/자식값** (`name`, `id`, `key`, `code` 등, 대소문자 변형 포함)을 우선순위대로 검증 → 같은 태그가 반복되는 XML 형제(여러 개의 `Message`, `Field`). **`name`이 `id`보다 먼저 시도되므로, `id`가 버전 간 재사용돼도 `name`이 유일하면 자연스럽게 `name`이 채택**되어 오매칭을 피한다.
3. **전수조사** — 양쪽 90% 이상에 값이 있는 모든 속성/자식명을 모아 유일성 점수(고유값 수 / 전체 값 개수)가 가장 높은 것을 채택.
4. 그래도 없으면 `null` 반환 → 호출자는 위치 기반 매칭(`MatchPositional`)으로 폴백.

## 형제 목록 매칭 (`KeyedListMatcher`)

`DocumentAligner`(라인 비교)와의 **결정적인 차이**: 키가 있을 때 교체 구간을 절대 페어링하지 않는다. 키가 다른 두 노드는 "값이 바뀐 같은 개체"가 아니라 "서로 다른 개체"이기 때문 — 이것이 이 프로젝트의 정합성 비교 핵심이다.

- **`MatchByKey`**: 좌/우 각각의 키 배열을 뽑아 `MyersDiff<string>`을 재사용. 공통 구간은 매칭, 교체 구간은 **페어링 없이** 좌측 전부 `LeftOnly`, 우측 전부 `RightOnly`로 분리한다(라인 비교의 `paired` 개념이 없음).
- **`MatchPositional`**(자연 키가 없을 때 폴백): 노드 전체를 값으로 보고 깊은 동등성 비교자(`StructureEquality`)로 `MyersDiff<StructuredNode>` 실행. 이 경우는 라인 비교와 동일하게 위치 기준 최선 추정(교체 구간 페어링)을 허용한다 — 키가 없으므로 "같은 개체인지" 자체를 판단할 방법이 없기 때문.

## 재귀 트리 비교 (`StructureDiffer`)

루트 노드부터 재귀적으로 두 트리를 내려가며 `List<AlignedRow>`를 만든다:

1. 좌/우가 둘 다 자식이 없으면(리프) → `RenderLeaf`로 한 줄씩 렌더링해 비교.
2. 좌/우 자식 유무가 다르면(리프↔컨테이너 형태 변화) → 줄 단위 페어링이 불가능하므로 안전하게 완전 교체(좌측 전체 삭제 + 우측 전체 추가)로 처리.
3. 둘 다 자식이 있으면 → 여는 태그를 비교용 한 줄로 렌더링하고, `NodeKeyStrategy`로 키를 구해 `KeyedListMatcher`로 자식을 매칭한 뒤 각 매칭 결과에 따라 재귀(`Matched`) 또는 서브트리 전체를 한쪽에만 출력(`LeftOnly`/`RightOnly`), 마지막에 닫는 태그를 비교.

결과는 `TextCompare.Alignment.AlignedRow` 계약을 그대로 따르므로, **`DiffDocument.FromRows`부터 `Controls` 레이어까지 라인 비교와 완전히 동일한 경로로 렌더링된다**([[01-아키텍처#핵심-설계-원칙-alignedrow는-라인-비교와-구조-비교의-공용-계약|관련 설계 원칙]]).

## Pretty-Printer (`IStructurePrettyPrinter`)

`StructureDiffer`는 "이 노드를 한 줄 텍스트로 어떻게 그릴지"를 몰라도 되도록, 문법별 렌더링을 인터페이스로 분리했다:

- `RenderOpenTag` / `RenderCloseTag` / `RenderLeaf` 세 메서드만 구현하면 새 포맷(예: YAML)도 추가 가능.
- `XmlPrettyPrinter`: `<Tag attr="...">` 형태, 속성 이스케이프(`&amp;`/`&quot;`/`&lt;`/`&gt;`) 처리.
- `JsonPrettyPrinter`: `isLastSibling`으로 트레일링 콤마 생략, `ScalarKind`에 따라 문자열만 따옴표로 감싸 재직렬화.

## MainForm에서의 모드 분기

`MainForm.OnCompareRequested`가 확장자로 분기한다: 양쪽 모두 `.xml`이면 XML 구조 비교, 양쪽 모두 `.json`이면 JSON 구조 비교, 그 외에는 라인 비교. 구조 비교 모드에서는 편집이 지원되지 않는다(`ToggleEditMode`에서 안내 메시지 표시).
