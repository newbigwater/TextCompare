# TextCompare

C# / .NET Framework 4.8 / WinForms 기반의 텍스트·XML·JSON 비교 프로그램. [WinMerge](https://manual.winmerge.org/en/)를 참고하여, WinMerge의 라인 삭제 시 뒤 줄이 밀려서 잘못 비교되는 문제를 구조적으로 해결하고, XML/JSON은 텍스트 줄이 아니라 **객체(엘리먼트) 단위로 비교**하도록 새로 설계했다.

## 목차

- [프로젝트 설명](#프로젝트-설명)
- [주요 기능](#주요-기능)
- [Manual](#manual)
  - [빌드](#빌드)
  - [사용법](#사용법)
  - [git 연동](#git-연동)
  - [명령줄 옵션](#명령줄-옵션)
  - [단축키](#단축키)
- [문서](#문서)
  - [설계서](#설계서)
  - [외부 라이브러리](#외부-라이브러리)

## 프로젝트 설명

라인 기반 비교 도구(WinMerge 등)는 두 가지 실무 문제가 있다:

1. **라인 삭제/삽입 시 뒤따르는 줄이 밀려서 잘못 짝지어진다.** `A,B,C` vs `A,C`(B 삭제)에서 B와 C가 "값이 바뀐 같은 줄"처럼 잘못 표시된다.
2. **XML/JSON의 `id` 속성은 버전 사이에 재사용될 수 있다.** id 기준으로 매칭하면 서로 무관한 두 엔티티가 잘못 짝지어진다.

TextCompare는 (1)을 **ghost-line 정렬**로, (2)를 **자연 키(natural key) 기반 객체 매칭**으로 해결한다. 두 해법 모두 `AlignedRow`라는 공용 계약으로 수렴하기 때문에, 라인 비교와 XML/JSON 구조 비교가 완전히 동일한 UI 렌더링 파이프라인을 공유한다. 자세한 설계 배경은 [설계서](#설계서)를 참고.

## 주요 기능

- 텍스트 라인 비교, XML/JSON 객체(구조) 비교 자동 분기(확장자 기준)
- 좌/우 동기 스크롤, 문자 단위(intraline) 강조
- WinMerge 스타일 위치 창(전체 개요 맵) + 하단 선택 diff 미리보기
- 파일 드래그앤드롭(놓은 위치에 따라 Left/Right 자동 배정)
- 실시간 편집 모드(입력 후 자동 재비교) + 저장(원본 인코딩 유지)
- 대소문자/공백 무시 옵션
- 정규식 제외 필터 — 패턴별로 "라인 전체 제외" / "매치 부분만 제외"(구간 회색 표시) 모드 선택
- UTF-8(BOM 유무)/UTF-16/CP949(EUC-KR) 자동 인코딩 감지
- **git 연동**: `git difftool` 외부 도구 등록(원클릭), 앱 내 "HEAD와 비교"로 작업 중 수정 내용 즉시 검토, WinMerge 스타일 명령줄 옵션(`/dl` `/dr` `/e` `/wl` 등)

## Manual

### 빌드

Visual Studio 2019 이상(또는 해당 MSBuild)과 .NET Framework 4.8 개발자 팩이 필요하다.

- **배치 빌드(권장)**: 저장소 루트에서 `BuildAll.bat` 실행. vswhere로 MSBuild를 자동 탐색해 Release 전체 리빌드 → SelfTest 실행 → `artifact\` 생성까지 수행한다. 성공 시 exit code 0, 실패 시 1 (CI 친화적, `pause` 없음).
- **Visual Studio**: `03. Src/TextCompare/TextCompare.sln` 열고 빌드.
- **CLI**: [빌드 및 테스트 문서](01.%20Doc/06-빌드-및-테스트.md) 참고 (Git Bash에서 MSBuild 호출 시 주의사항 포함).

#### 배치 빌드 시스템 구성

| 경로 | 내용 |
|---|---|
| `BuildAll.bat` | 전체 빌드 진입점 (Rebuild → SelfTest → git log 이력 기록) |
| `BuildInfo.bat` | Release PostBuildEvent에서 호출됨. 빌드된 exe의 FileVersion을 읽어 artifact 스테이징 + `TextCompare.buildInfo.txt` 생성 |
| `build\<Platform><Config>\` | 최종 바이너리 (예: `build\AnyCPURelease\`) |
| `output\<Platform><Config>\<어셈블리명>\` | obj 중간 산출물 |
| `artifact\<Platform><Config>\` | 배포물: `TextCompare.exe`, `TextCompare.exe.config`, `TextCompare.buildInfo.txt` |
| `artifact\history.TextCompare.txt` | `git log` 이력 |

버전 관리: 메인 프로젝트는 `AssemblyVersion("1.0.*")` 와일드카드(+ `Deterministic=false`)로 빌드 시각 기반 `1.0.<build>.<revision>` 버전이 자동 생성되며, `BuildInfo.bat`이 이를 buildInfo.txt에 기록한다.

주의사항:

- 이 저장소의 프로젝트는 출력 경로가 `bin\`이 아니라 저장소 루트의 `build\`로 설정되어 있다. VS에서 Debug(F5) 실행 시에도 `build\AnyCPUDebug\`에서 실행된다.
- 일부 PC에는 `Platform` 환경변수가 설정되어 있어 MSBuild의 `Platform` 프로퍼티와 충돌한다. `BuildAll.bat`은 이를 `set Platform=`으로 제거하고 `/p:Platform="Any CPU"`를 명시한다. CLI에서 직접 빌드할 때도 동일하게 명시할 것.

### 사용법

1. 프로그램 실행 후 상단 `Left(As-Is)` / `Right(To-Be)` 칸에 비교할 두 파일을 지정한다.
   - `찾아보기...` 버튼으로 선택하거나, 파일을 각 칸(또는 좌/우 비교 페인)에 직접 드래그앤드롭한다.
   - 파일 2개를 한 번에 드롭하면 Left/Right에 자동 배정되고 즉시 비교가 실행된다.
2. 필요하면 `대소문자 무시` / `공백 무시` 옵션을 켠 뒤 `비교` 버튼을 누른다.
   - 양쪽 확장자가 `.xml`이면 XML 구조 비교, `.json`이면 JSON 구조 비교, 그 외에는 텍스트 라인 비교가 자동으로 선택된다.
3. `◀ 이전 차이` / `다음 차이 ▶` 버튼, `Alt+↑`/`Alt+↓`, 또는 좌측 위치 창 클릭으로 차이 사이를 이동한다. 선택한 차이의 원문은 하단 미리보기 패널에 잘림 없이 표시된다.
4. 텍스트 비교 모드에서 `편집 모드` 버튼을 누르면 직접 내용을 수정할 수 있다. 입력을 멈추면 자동으로 재비교되며, `Ctrl+S` 또는 `저장` 버튼으로 원본 인코딩 그대로 저장한다.
   - XML/JSON 구조 비교 모드에서는 편집을 지원하지 않는다(버튼을 누르면 안내 메시지가 표시된다).

### git 연동

WinMerge의 [버전 관리 연동](https://manual.winmerge.org/en/Version_control.html)과 같은 방식으로, git과 함께 수정 내용을 검토할 수 있다.

**① 앱 내 "HEAD와 비교"** — 상단 `git ▾` 버튼 → `HEAD와 비교`:

1. 비교할 파일을 Left 또는 Right 칸에 지정한다(git 저장소 안의 파일).
2. `git ▾ → HEAD와 비교`를 누르면 마지막 커밋(HEAD) 버전이 왼쪽(읽기 전용, `HEAD: 파일명` 라벨), 현재 작업본이 오른쪽에 놓여 즉시 비교된다.
3. HEAD 버전은 임시 파일로 추출되며 앱 종료 시 자동 삭제된다. 파일이 저장소 밖이거나, 아직 커밋된 적이 없거나, git이 설치되어 있지 않으면 각각 안내 메시지가 표시된다.

**② git difftool로 사용** — `git ▾ → git difftool로 등록` (또는 `TextCompare.exe /register-git`):

전역 git 설정(`git config --global`)에 difftool로 등록된다. 이후 저장소에서:

```bash
git difftool          # 수정된 파일을 하나씩 TextCompare로 검토
git difftool HEAD~3   # 3커밋 전과 비교
```

왼쪽에 "이전 버전"(읽기 전용), 오른쪽에 "작업본"이 라벨로 표시되고, `Esc` 한 번으로 창을 닫고 다음 파일로 넘어간다. 등록 해제는 `git ▾ → git difftool 등록 해제` 또는 `/unregister-git`.

### 명령줄 옵션

```
TextCompare.exe [옵션] [<left> <right>]
```

| 옵션 | 동작 |
|---|---|
| `/dl <label>` | 왼쪽 제목 라벨(경로 대신 표시) |
| `/dr <label>` | 오른쪽 제목 라벨 |
| `/e` | `Esc` 한 번으로 창 닫기 |
| `/wl`, `/wr` | 왼쪽/오른쪽 읽기 전용(편집·저장 차단) |
| `/u` | 최근 목록에 추가 안 함(WinMerge 호환용) |
| `/register-git` | git 전역 difftool로 등록 후 종료 |
| `/unregister-git` | 등록 해제 후 종료 |

옵션은 `/dl`·`-dl` 두 접두 모두 인식하며 대소문자를 구분하지 않는다. 종료 코드는 0=차이 없음, 1=차이 있음, 2=오류(WinMerge 관례, git의 `difftool.trustExitCode`와 호환).

### 단축키

| 단축키 | 동작 |
|---|---|
| `Ctrl+S` | 저장 |
| `Alt+↑` | 이전 차이로 이동 |
| `Alt+↓` | 다음 차이로 이동 |
| `Esc` | 창 닫기 (`/e` 옵션으로 실행된 경우만) |

## 문서

### 설계서

전체 아키텍처, 핵심 알고리즘, 구조적 비교, UI 레이어, 데이터 흐름, 빌드/테스트를 다루는 설계서는 `01. Doc/`에 있다(Obsidian vault로 열람 권장 — `[[위키링크]]`와 Mermaid 다이어그램 포함).

→ [`01. Doc/00-개요.md`](01.%20Doc/00-개요.md) 부터 시작

- [아키텍처](01.%20Doc/01-아키텍처.md)
- [핵심 비교 알고리즘](01.%20Doc/02-핵심-비교-알고리즘.md)
- [구조적 비교 (XML/JSON)](01.%20Doc/03-구조적-비교-XML-JSON.md)
- [UI 레이어](01.%20Doc/04-UI-레이어.md)
- [데이터 흐름](01.%20Doc/05-데이터-흐름.md)
- [빌드 및 테스트](01.%20Doc/06-빌드-및-테스트.md)
- [git 연동](01.%20Doc/09-git-연동.md)

### 외부 라이브러리

NuGet 패키지 없이 .NET Framework 4.8 내장 어셈블리만 사용한다. 실제 사용 여부와 각 어셈블리의 역할은 다음 문서에 정리했다.

→ [`01. Doc/07-외부-라이브러리.md`](01.%20Doc/07-외부-라이브러리.md)
