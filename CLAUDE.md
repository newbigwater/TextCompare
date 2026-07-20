# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 저장소 구성

- `03. Src/TextCompare/` — **실제 작업 대상**. C# / .NET Framework 4.8 / WinForms 텍스트·XML·JSON 비교 프로그램 (`TextCompare.sln`).
- `01. Doc/` — 설계서 (Obsidian vault, 한국어). 아키텍처·알고리즘·UI·빌드 문서. 구조 변경 시 함께 갱신한다.
- `02. 3'rd/winmerge/` — 참고용 WinMerge 소스 체크아웃. **읽기 전용 참고 자료이며 수정하지 않는다.** 파일 검색 시 이 디렉터리를 제외해야 노이즈가 없다.

## 빌드

Visual Studio 2019+에 포함된 MSBuild만 사용한다. `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`는 `ToolsVersion="15.0"`을 이해하지 못하므로 **절대 사용 금지**.

```bash
# Git Bash — MSYS 경로 변환이 /p: 인자를 깨뜨리므로 MSYS_NO_PATHCONV=1 필수
cd "D:/50. Utility/Compare/03. Src/TextCompare"
MSYS_NO_PATHCONV=1 \
  "/c/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin/MSBuild.exe" \
  TextCompare.sln -p:Configuration=Debug -p:Platform="Any CPU" -nologo -v:minimal
```

- 머신 환경변수 `platform`이 MSBuild `Platform` 속성과 충돌하므로 `-p:Platform=`을 항상 명시한다 (환경변수는 건드리지 않는다).
- MSBuild 경로가 다르면 vswhere로 확인: `& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath`

## 테스트

`TextCompare.SelfTest` — UI 없는 콘솔 회귀 테스트 (전 로직 검증, 통과 시 `ALL TESTS PASSED` + 종료 코드 0):

```bash
cd "D:/50. Utility/Compare/03. Src/TextCompare"
MSYS_NO_PATHCONV=1 ./TextCompare.SelfTest/bin/Debug/TextCompare.SelfTest.exe
```

테스트는 `TextCompare.SelfTest/Program.cs` 안의 카테고리별 메서드(`RunCoreTests`, `RunAlignmentTests`, `RunStructureDifferEndToEndTests` 등)로 구성된다. 개별 테스트 러너는 없으므로, 특정 영역만 확인하려면 해당 `Run*` 메서드를 참고한다.

## Classic csproj 주의사항

두 프로젝트 모두 SDK-style이 아닌 classic csproj다 (자동 glob 없음):

- **새 .cs 파일을 추가하면 반드시 `TextCompare.csproj`에 `<Compile Include>` 항목을 직접 추가**해야 한다.
- UI가 아닌 로직 파일은 `TextCompare.SelfTest.csproj`에도 `<Link>`로 링크 추가한다 (SelfTest는 로직 파일을 링크로 공유).

## 아키텍처

단방향 의존 레이어 구조. 비교 로직은 UI를 전혀 모르고, UI는 `Document` 레이어의 뷰모델만 바라본다.

핵심 계약: **`Alignment/AlignedRow`는 라인 비교(`DocumentAligner`)와 XML/JSON 구조 비교(`StructureDiffer`)가 공통으로 생산하는 출력 타입**이다. 두 비교 모드는 `List<AlignedRow>`를 만드는 지점까지만 다르고, 이후 `DiffDocument.FromRows` → `DiffViewerControl` 렌더링 경로는 완전히 동일하다. 새 비교 모드(CSV 등)를 추가하려면 `AlignedRow` 목록을 생산하는 Differ만 작성하면 되고 UI는 손대지 않는다.

| 네임스페이스 (폴더) | 책임 |
|---|---|
| `Core` | 범용 diff 엔진 (`MyersDiff<T>`), 정규화 옵션, 제외 필터 |
| `Alignment` | ghost-line 정렬 — 라인 삭제 시 뒤 줄이 밀려 잘못 짝지어지는 WinMerge 문제의 해결책 |
| `Intraline` | 변경 라인 쌍의 문자 단위 강조 구간 계산 |
| `Structure` | XML/JSON 파싱, 자연 키(natural key) 기반 객체 매칭 — id 재사용 문제 해결 |
| `Document` | `DiffDocument`/`DiffBlock` 뷰모델 (UI가 바라보는 유일한 계층) |
| `IO` | 인코딩 판별 (BOM → UTF-8 엄격 검증 → CP949 순) |
| `Controls` | WinForms UserControl 6종 + 헬퍼. `DiffPaneControl`/`LocationPaneControl`은 owner-drawn |
| `Builders` | `MainFormBuilder` — MainForm 조립 |

비교 모드 분기는 확장자 기준: 양쪽 다 `.xml`이면 XML 구조 비교, `.json`이면 JSON 구조 비교, 그 외 텍스트 라인 비교. 비교 실행은 `MainForm`에서 BackgroundWorker + `CompareProgressForm`으로 비동기 수행된다.

NuGet 패키지 없이 .NET Framework 4.8 내장 어셈블리만 사용한다 — 외부 패키지를 추가하지 않는다.

## UI 코드 규칙

- **레이아웃은 TableLayoutPanel + 명시적 행 번호**로 배치한다. `Dock=Top` 순서 의존 쌓기는 과거 툴바가 콘텐츠를 가리는 버그를 냈으므로 금지.
- WinForms Designer 패턴 준수: 자식 컨트롤 필드는 `*.Designer.cs` partial class에 선언, 생성/속성/`Controls.Add()`는 `InitializeComponent()` 안에서. 동적 좌표 계산·드래그앤드롭 배선만 일반 `.cs`에 둔다.
- UI 자동화 테스트가 없으므로 UI 변경 검증 순서: ① 빌드 + SelfTest로 로직 회귀 확인 → ② `Control.DrawToBitmap()`(개별 컨트롤 단위)으로 화면 검증. 단 `RichTextBox`는 `DrawToBitmap`으로 내용이 비어 보이므로 텍스트/색상 속성을 리플렉션으로 읽어 검증한다 → ③ 실제 창 캡처가 필요하면 해당 프로세스 창 영역(`GetWindowRect`)만 캡처한다 (전체 화면 캡처 금지).

## 문서 언어

설계서(`01. Doc/`), README, 커밋 대상 문서는 한국어로 작성한다. `01. Doc/` 문서는 Obsidian `[[위키링크]]`와 Mermaid 다이어그램을 사용한다.
