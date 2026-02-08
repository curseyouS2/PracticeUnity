# PracticeUnity - 육성 시뮬레이션 게임 매뉴얼

## 목차
1. [프로젝트 개요](#1-프로젝트-개요)
2. [아키텍처 구조](#2-아키텍처-구조)
3. [Unity 씬 셋업 가이드](#3-unity-씬-셋업-가이드)
4. [ScriptableObject 데이터 생성 가이드](#4-scriptableobject-데이터-생성-가이드)
5. [핵심 시스템 상세](#5-핵심-시스템-상세)
6. [게임 흐름](#6-게임-흐름)
7. [콘텐츠 확장 가이드](#7-콘텐츠-확장-가이드)
8. [수치 밸런스 레퍼런스](#8-수치-밸런스-레퍼런스)
9. [트러블슈팅](#9-트러블슈팅)

---

## 1. 프로젝트 개요

30일간 캐릭터의 스탯을 육성하여 다양한 엔딩을 달성하는 **육성 시뮬레이션 게임**입니다.

### 핵심 컨셉
- **30일** 제한 시간 내에 활동을 선택하여 스탯을 성장시킴
- 매일 **16:00~24:00** (방과 후) 시간을 활용
- 장소를 방문하고, 활동을 수행하고, 캐릭터와 대화
- 최종 스탯에 따라 **멀티 엔딩** 분기

### 폴더 구조
```
Assets/_Main/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs              ← 게임 전체 흐름 관리 (진입점)
│   │   ├── Controllers/                ← 개별 도메인 로직
│   │   │   ├── StatusController.cs     ← 5대 스탯 관리
│   │   │   ├── HealthController.cs     ← 체력/피로도 관리
│   │   │   ├── ConditionController.cs  ← 컨디션(상태이상) 관리
│   │   │   ├── InventoryController.cs  ← 인벤토리 관리
│   │   │   ├── RelationshipController.cs ← 호감도 관리
│   │   │   └── TimeController.cs       ← 시간/날짜 관리
│   │   ├── Managers/                   ← Controller 조율 계층
│   │   │   ├── StatusManager.cs        ← 스탯/체력/컨디션 통합 관리
│   │   │   ├── RoutineManager.cs       ← 일과/장소/활동 관리
│   │   │   ├── CharacterManager.cs     ← 캐릭터 등장/스케줄 관리
│   │   │   └── DialogueManager.cs      ← 대화 진행 관리
│   │   └── Utilities/
│   │       └── TimeUtility.cs          ← 시간 포맷팅 유틸리티
│   ├── Data/
│   │   ├── GameData.cs                 ← 게임 상태 데이터 클래스
│   │   ├── Enums.cs                    ← 요일 열거형
│   │   └── ScriptableObjects/          ← SO 정의
│   │       ├── LocationSO.cs
│   │       ├── ActivitySO.cs
│   │       ├── CharacterSO.cs
│   │       ├── DialogueSO.cs
│   │       ├── EndingSO.cs
│   │       └── ItemSO.cs
│   └── UI/
│       ├── UIManager.cs                ← UI 전체 조율
│       ├── StatusPanel.cs              ← 좌측: 시간/스탯 표시
│       ├── CharacterPanel.cs           ← 우측: 캐릭터/인벤토리
│       ├── MainPanel.cs                ← 중앙: 장소/활동 선택
│       ├── DialoguePanel.cs            ← 대화 UI
│       ├── EndingPanel.cs              ← 엔딩 화면
│       └── InventorySlotUI.cs          ← 인벤토리 슬롯
└── Resources/
    └── Data/
        ├── LocationData.json           ← 장소/활동 참조 데이터
        └── EndingData.json             ← 엔딩 조건 참조 데이터
```

---

## 2. 아키텍처 구조

### 계층 구조: Manager → Controller → Data

```
┌─────────────────────────────────────────────────┐
│                  GameManager                     │  ← 싱글톤, 게임 흐름 총괄
│    (초기화, 이벤트 연결, 엔딩 판정)                  │
├────────────────┬────────────────┬────────────────┤
│ StatusManager  │RoutineManager  │  UIManager     │  ← Manager 계층
│ (스탯 통합관리) │(일과/시간 관리) │ (UI 조율)       │
├────────────────┼────────────────┼────────────────┤
│StatusController│TimeController  │ StatusPanel    │  ← Controller/Panel 계층
│HealthController│               │ MainPanel      │
│ConditionCtrl   │               │ CharacterPanel │
│InventoryCtrl   │               │ DialoguePanel  │
│RelationshipCtrl│               │ EndingPanel    │
└────────────────┴────────────────┴────────────────┘
```

### 설계 원칙
- **Manager**는 `MonoBehaviour` (Inspector에서 할당), **Controller**는 순수 C# 클래스
- Manager끼리는 **이벤트 기반** 통신 (`Action` 델리게이트)
- 데이터는 **ScriptableObject**로 에디터에서 편집
- 모든 Manager는 **Singleton 패턴** (`Instance` 프로퍼티)

---

## 3. Unity 씬 셋업 가이드

### Step 1: 빈 씬 준비
`growing.unity` 씬을 사용합니다.

### Step 2: Manager 오브젝트 생성

Hierarchy에 **빈 GameObject**를 만들고 다음 컴포넌트를 붙입니다:

#### GameManager 오브젝트
| 필드 | 할당 대상 | 설명 |
|------|----------|------|
| `statusManager` | StatusManager가 붙은 오브젝트 | 스탯 통합 관리 |
| `routineManager` | RoutineManager가 붙은 오브젝트 | 일과/시간 관리 |
| `uiManager` | UIManager가 붙은 오브젝트 | UI 조율 |
| `endings` | EndingSO 에셋 리스트 | 엔딩 데이터 |

> **권장:** 하나의 "Managers" 오브젝트에 GameManager, StatusManager, RoutineManager, CharacterManager, DialogueManager를 모두 붙이거나, 각각의 자식 오브젝트로 분리합니다.

#### StatusManager
- 별도의 Inspector 할당 필드 없음 (코드에서 Controller를 생성)
- GameManager가 `Initialize(gameState)`를 호출하면 자동 초기화

#### RoutineManager
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `locations` | LocationSO 리스트 | 게임에 등장하는 모든 장소 |
| `startTimeMinutes` | `960` (16:00) | 하루 시작 시간 |
| `sleepTimeMinutes` | `1440` (24:00) | 하루 종료 시간 (강제 수면) |
| `maxDays` | `30` | 총 플레이 일수 |

#### CharacterManager
| 필드 | 설명 |
|------|------|
| `allCharacters` | 게임에 등장하는 모든 CharacterSO 리스트 |

#### DialogueManager
- 별도의 Inspector 할당 필드 없음 (이벤트로 동작)

### Step 3: UI 구성

UIManager에 다음 패널들을 연결합니다:

| 필드 | UI 패널 | 역할 |
|------|---------|------|
| `statusPanel` | StatusPanel | 좌측 패널 - 시간, 스탯 바, 소지금 |
| `characterPanel` | CharacterPanel | 우측 패널 - 캐릭터 이미지, 인벤토리 |
| `mainPanel` | MainPanel | 중앙 패널 - 장소/활동 선택 버튼 |
| `endingPanel` | EndingPanel | 엔딩 카드 (기본 비활성) |
| `dialoguePanel` | DialoguePanel | 대화 창 (기본 비활성) |

### Step 4: Prefab 확인
- `Assets/Prefabs/LocationButton.prefab` - 장소 선택 버튼으로 사용
- MainPanel에서 동적으로 생성됨

---

## 4. ScriptableObject 데이터 생성 가이드

모든 게임 콘텐츠는 ScriptableObject로 정의합니다. Unity 에디터에서 **우클릭 → Create → Game** 메뉴로 생성합니다.

### 4.1 LocationSO (장소)

**생성:** `Create → Game → Location`

| 필드 | 타입 | 예시 | 설명 |
|------|------|------|------|
| `id` | string | `"school"` | 고유 ID (CharacterSchedule의 locationId와 매칭) |
| `locationName` | string | `"학교 도서관"` | 표시 이름 |
| `description` | string | `"조용한 분위기의 도서관"` | 장소 설명 |
| `openTimeMinutes` | int | `540` (09:00) | 이용 시작 시간 (분) |
| `closeTimeMinutes` | int | `1260` (21:00) | 이용 종료 시간 (분) |
| `activities` | List\<ActivitySO\> | - | 이 장소에서 할 수 있는 활동 목록 |
| `locationIcon` | Sprite | - | 장소 아이콘 |
| `backgroundImage` | Sprite | - | 장소 배경 이미지 |

> **시간 변환 공식:** `시간 × 60 + 분` (예: 16:30 = 960 + 30 = 990)
> **자정 넘김 지원:** closeTime < openTime이면 자동으로 자정 넘김 처리 (예: 22:00~02:00)

### 4.2 ActivitySO (활동)

**생성:** `Create → Game → Activity`

| 필드 | 타입 | 예시 | 설명 |
|------|------|------|------|
| `id` | string | `"study"` | 고유 ID (`"sleep"`, `"rest"`는 탈진 시 특수 처리) |
| `activityName` | string | `"공부하기"` | 표시 이름 |
| `description` | string | `"집중해서 공부합니다"` | 활동 설명 |
| `durationMinutes` | int | `60` | 소요 시간 (분) |
| `cost` | int | `0` | 필요 비용 |
| `statChanges` | StatChanges | 아래 참조 | 활동 완료 시 스탯 변화량 |
| `requirements` | List\<StatRequirement\> | - | 실행에 필요한 최소 스탯 |

**StatChanges 필드:**
| 필드 | 설명 | 비고 |
|------|------|------|
| `intelligence` | 지력 변화 | 효율 적용 |
| `charm` | 매력 변화 | 효율 적용 |
| `courage` | 용기 변화 | 효율 적용 |
| `moral` | 도덕성 변화 | 효율 적용 |
| `money` | 소지금 변화 | 효율 적용 |
| `physical` | 체력 변화 | 효율 적용 |
| `mental` | 정신력 변화 | 효율 적용 |
| `fatigue` | 피로도 변화 | **효율 미적용** (항상 원본 값 적용) |

> **중요:** `id`가 `"sleep"` 또는 `"rest"`인 활동은 **탈진 상태**에서도 실행 가능합니다. 다른 활동은 탈진 시 차단됩니다.

### 4.3 CharacterSO (캐릭터)

**생성:** `Create → Game → Character`

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | 고유 ID (호감도 추적에 사용) |
| `characterName` | string | 표시 이름 |
| `portrait` | Sprite | 초상화 (대화창에 표시) |
| `fullBodyImage` | Sprite | 전신 이미지 (캐릭터 패널에 표시) |
| `defaultDialogues` | string[] | 기본 대사 목록 (랜덤 선택) |
| `schedules` | List\<CharacterSchedule\> | 등장 스케줄 |

**CharacterSchedule 구조:**
| 필드 | 타입 | 예시 | 설명 |
|------|------|------|------|
| `activeDays` | List\<DayOfWeek\> | Monday, Wednesday | 등장 요일 (비워두면 매일) |
| `startTimeMinutes` | int | `960` (16:00) | 등장 시작 시간 |
| `endTimeMinutes` | int | `1200` (20:00) | 등장 종료 시간 |
| `locationId` | string | `"school"` | 등장 장소 (LocationSO의 id와 매칭) |
| `dialogue` | DialogueSO | - | 이 스케줄에서의 대화 |
| `requiredAffection` | int | `0` | 등장에 필요한 최소 호감도 |

> **예시:** "월/수/금 16:00~20:00에 학교 도서관에서 등장, 호감도 20 이상일 때만"

### 4.4 DialogueSO (대화)

**생성:** `Create → Game → Dialogue`

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | 고유 ID (일회성 대화 추적에 사용) |
| `dialogueType` | DialogueType | `Simple` (순차 대사) / `Branching` (선택지) |
| `lines` | List\<DialogueLine\> | 대화 줄 목록 |
| `requiredAffection` | int | 이 대화 진행에 필요한 최소 호감도 |
| `affectionReward` | int | 대화 완료 시 호감도 보상 (기본: 1) |
| `statReward` | StatChanges | 대화 완료 시 스탯 보상 |
| `oneTimeOnly` | bool | `true`면 한 번만 볼 수 있음 |
| `durationMinutes` | int | 대화 소요 시간 (기본: 30분) |

**DialogueLine 구조:**
| 필드 | 설명 |
|------|------|
| `speakerName` | 화자 이름 (비워두면 캐릭터 이름 자동 사용) |
| `text` | 대사 텍스트 |
| `speakerPortrait` | 표정/이미지 (비워두면 캐릭터 기본 portrait 사용) |
| `hasChoices` | `true`면 이 줄에서 선택지 표시 |
| `choices` | 선택지 목록 (hasChoices가 true일 때) |

**DialogueChoice 구조:**
| 필드 | 설명 |
|------|------|
| `choiceText` | 선택지에 표시되는 텍스트 |
| `affectionChange` | 선택 시 호감도 변화 |
| `statChange` | 선택 시 스탯 변화 |
| `nextDialogue` | 선택 후 이어질 DialogueSO (없으면 대화 종료) |
| `responseText` | 선택 직후 캐릭터의 반응 대사 |

**대화 작성 예시 (분기형):**
```
Line 0: "안녕! 오늘 뭐 할 거야?"
Line 1: "같이 할 일이 있는데..." (hasChoices = true)
  ├─ Choice A: "좋아, 같이 하자!" → affectionChange: +5, responseText: "정말? 고마워!"
  ├─ Choice B: "미안, 바빠서..." → affectionChange: -2, responseText: "그래... 다음에 하자."
  └─ Choice C: "뭔데?" → nextDialogue: (다른 DialogueSO로 분기)
```

### 4.5 EndingSO (엔딩)

**생성:** `Create → Game → Ending`

| 필드 | 타입 | 예시 | 설명 |
|------|------|------|------|
| `id` | string | `"scholar"` | 고유 ID |
| `endingName` | string | `"학자 엔딩"` | 엔딩 이름 |
| `description` | string | `"명문대에 진학했습니다!"` | 엔딩 설명 |
| `conditions` | List\<StatRequirement\> | Intelligence ≥ 80 | 엔딩 달성 조건 |
| `priority` | int | `1` | 우선순위 (**낮을수록 우선**) |
| `endingImage` | Sprite | - | 엔딩 일러스트 |

> **판정 로직:** 30일 종료 시, priority 오름차순으로 정렬 → 조건을 **모두 충족**하는 첫 번째 엔딩이 선택됨. 어떤 조건도 충족하지 못하면 마지막 엔딩(기본 엔딩)이 표시됩니다.

**기본 제공 엔딩 (EndingData.json 참조):**
| ID | 이름 | 조건 | 우선순위 |
|----|------|------|----------|
| `scholar` | 학자 엔딩 | 지력 ≥ 80 | 1 |
| `idol` | 아이돌 엔딩 | 매력 ≥ 80, 용기 ≥ 50 | 2 |
| `athlete` | 운동선수 엔딩 | 용기 ≥ 80, 매력 ≥ 40 | 3 |
| `balanced` | 평범한 엔딩 | 조건 없음 (기본) | 99 |

### 4.6 ItemSO (아이템)

**생성:** `Create → Game → Item`

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | 고유 ID |
| `itemName` | string | 표시 이름 |
| `description` | string | 아이템 설명 |
| `icon` | Sprite | 아이템 아이콘 |
| `itemType` | ItemType | `Consumable` / `Equipment` / `KeyItem` / `Material` |
| `maxStack` | int | 최대 중첩 수 (기본: 99) |
| `isConsumable` | bool | 사용(소비) 가능 여부 |
| `useEffect` | StatChanges | 사용 시 스탯 효과 |

---

## 5. 핵심 시스템 상세

### 5.1 시간 시스템

```
하루 = 16:00 (960분) ~ 24:00 (1440분) = 8시간(480분)
총 게임 기간 = 30일
```

- 활동을 수행하면 `durationMinutes`만큼 시간이 진행됨
- 시간이 24:00(1440분)을 넘으면 **자동으로 다음 날** 시작
- 다음 날 시작 시 `StatusManager.ProcessDayEnd()` 호출 (피로도 10 회복, 컨디션 처리)
- Day 1 = 월요일, Day 7 = 일요일, Day 8 = 다시 월요일 (7일 주기)

**시간대 구분:**
| 시간대 | 범위 | 한국어 |
|--------|------|--------|
| Morning | 06:00~12:00 | 아침 |
| Afternoon | 12:00~18:00 | 낮 |
| Evening | 18:00~21:00 | 저녁 |
| Night | 21:00~06:00 | 밤 |

> 게임은 16:00(낮)에 시작하여 18:00(저녁), 21:00(밤)을 거쳐 24:00에 종료됩니다.

### 5.2 스탯 시스템

#### 기본 스탯 (StatusController)
| 스탯 | 초기값 | 범위 | 설명 |
|------|--------|------|------|
| Intelligence (지력) | 10 | 0~999 | 학업 성적 |
| Charm (매력) | 10 | 0~999 | 외모/사교 |
| Courage (용기) | 10 | 0~999 | 체력/정신력 |
| Moral (도덕성) | 0 | 0~100 | 도덕/윤리 |
| Money (소지금) | 5000 | 제한 없음 | 돈 |

#### 건강 스탯 (HealthController)
| 스탯 | 초기값 | 범위 | 설명 |
|------|--------|------|------|
| Physical (체력) | 100 | 0~100 | 신체 건강 |
| Mental (정신력) | 100 | 0~100 | 정신 건강 |
| Fatigue (피로도) | 0 | 0~100 | 누적 피로 (**높을수록 나쁨**) |

### 5.3 효율 시스템

활동의 스탯 변화량은 **효율(Efficiency)**에 의해 보정됩니다.

```
최종 변화량 = 원본 변화량 × 건강 효율 × 컨디션 보정
(단, 피로도는 효율 미적용)
```

#### 건강 효율 (피로도 기반)
| 피로도 | 효율 |
|--------|------|
| 0~49 | 100% |
| 50~69 | 80% |
| 70~89 | 50% |
| 90~100 | 30% |

#### 컨디션 효율 보정
| 컨디션 | 보정값 | 한국어 |
|--------|--------|--------|
| None | ×1.0 | 보통 |
| Happy | ×1.2 | 행복 |
| Sad | ×0.8 | 슬픔 |
| Depressed | ×0.5 | 우울 |
| Sick | ×0.6 | 아픔 |
| Injured | ×0.7 | 부상 |
| Exhausted | - | 탈진 (sleep/rest만 가능) |

> **예시:** 지력+3 활동을 피로도 75, Happy 상태에서 수행하면
> `3 × 0.5 × 1.2 = 1.8 → 반올림 → 2` 만큼 지력 증가

### 5.4 컨디션 시스템

- 피로도 100 도달 시 자동으로 **탈진(Exhausted)** 상태
- 탈진 시 `"sleep"` 또는 `"rest"` ID를 가진 활동만 수행 가능
- 컨디션은 **지속 기간(duration)** 설정 가능 → 매일 1씩 감소, 0이 되면 자동 해제
- 피로도는 매일 **10씩 자동 회복** (`DailyRecovery()`)

### 5.5 호감도 시스템

| 레벨 | 이름 | 필요 호감도 |
|------|------|------------|
| 0 | 모르는 사이 | 0 |
| 1 | 아는 사이 | 20 |
| 2 | 친구 | 50 |
| 3 | 절친 | 100 |
| 4 | 특별한 사이 | 200 |

- 대화 선택지, 대화 완료 보상 등으로 호감도 변동
- 캐릭터 스케줄의 `requiredAffection`으로 등장 조건 설정
- 일회성 대화는 `oneTimeOnly` 플래그로 중복 방지

### 5.6 인벤토리 시스템

- 아이템은 Dictionary 기반 관리 (ItemSO → 수량)
- `maxStack`을 넘어가는 수량은 자동으로 잘림
- 소비 아이템(`isConsumable = true`) 클릭 시 `useEffect`의 StatChanges가 적용됨
- 아이템 타입: Consumable(소비), Equipment(장비), KeyItem(중요), Material(재료)

---

## 6. 게임 흐름

### 6.1 초기화 순서

```
GameManager.Start()
  └→ InitializeGame()
       ├→ StatusManager.Initialize(gameState)    // Controller 5개 생성
       ├→ RoutineManager.Initialize(1)           // TimeController 생성
       ├→ SubscribeToEvents()                    // 이벤트 연결
       ├→ UIManager.Initialize()                 // 패널 초기화
       ├→ UIManager.UpdateAllUI()                // 첫 화면 갱신
       └→ ShowAvailableLocations()               // 장소 버튼 표시
```

### 6.2 활동 실행 흐름

```
[사용자: 활동 버튼 클릭]
  └→ MainPanel.OnActivitySelected 이벤트
       └→ UIManager.HandleActivitySelected()
            └→ GameManager.ExecuteActivity(location, activity)
                 ├→ RoutineManager.CanExecuteActivity() // 실행 가능 체크
                 ├→ StatusManager.CalculateTotalEfficiency() // 효율 경고
                 └→ RoutineManager.ExecuteActivity()
                      ├→ StatusController.SpendMoney()     // 비용 지불
                      ├→ StatusManager.ApplyActivityEffect() // 스탯 적용
                      ├→ OnActivityExecuted 이벤트 발생
                      └→ TimeController.AdvanceTime()       // 시간 진행
                           └→ (시간 초과 시) AdvanceDay()
                                └→ StatusManager.ProcessDayEnd()
                                     ├→ HealthController.DailyRecovery() // 피로 -10
                                     └→ ConditionController.ProcessDayEnd()
```

### 6.3 대화 흐름

```
[사용자: 캐릭터 클릭]
  └→ MainPanel.OnCharacterTalkSelected 이벤트
       └→ UIManager.HandleCharacterTalk()
            └→ DialogueManager.StartDialogue(character, dialogue)
                 ├→ OnDialogueStarted 이벤트
                 └→ ShowCurrentLine() → OnLineChanged 이벤트
                      └→ [사용자: 화면 클릭 (다음 대사)]
                           └→ NextLine()
                                ├→ (선택지 있으면) OnChoicesShown 이벤트
                                │    └→ SelectChoice(index)
                                │         ├→ ApplyChoiceEffects() // 호감도/스탯 변화
                                │         └→ (nextDialogue 있으면) 분기 대화 시작
                                └→ (마지막 줄이면) EndDialogue()
                                     ├→ 호감도 보상 적용
                                     ├→ 스탯 보상 적용
                                     ├→ 일회성 표시
                                     ├→ 시간 진행
                                     └→ OnDialogueEnded 이벤트
```

### 6.4 엔딩 판정 흐름

```
Day 31 도달 (maxDays 초과)
  └→ RoutineManager.CheckGameEnd()
       └→ OnGameEnd 이벤트
            └→ GameManager.ShowEnding()
                 ├→ 엔딩 리스트를 priority 오름차순 정렬
                 ├→ 각 엔딩의 conditions를 순회
                 │    └→ 모든 StatRequirement 충족 여부 확인
                 ├→ 첫 번째 충족 엔딩 표시
                 └→ (없으면) 마지막 엔딩(기본 엔딩) 표시
```

---

## 7. 콘텐츠 확장 가이드

### 7.1 새 장소 추가

1. `Create → Game → Location`으로 LocationSO 생성
2. `id`, `locationName`, `openTimeMinutes`, `closeTimeMinutes` 설정
3. 활동(ActivitySO)을 만들어 `activities` 리스트에 추가
4. RoutineManager의 `locations` 리스트에 새 LocationSO 추가

### 7.2 새 캐릭터 추가

1. `Create → Game → Character`로 CharacterSO 생성
2. `id`, `characterName`, `portrait`, `defaultDialogues` 설정
3. `schedules`에 등장 스케줄 추가:
   - 등장 요일, 시간 범위, 장소 ID 지정
   - 대화 DialogueSO 연결
   - 필요 호감도 설정
4. CharacterManager의 `allCharacters` 리스트에 추가

### 7.3 새 엔딩 추가

1. `Create → Game → Ending`으로 EndingSO 생성
2. `conditions`에 필요한 스탯 조건 추가 (StatType + 최소값)
3. `priority` 설정 (다른 엔딩과 겹칠 때 우선순위 결정)
4. GameManager의 `endings` 리스트에 추가

### 7.4 분기형 대화 체인 만들기

```
DialogueSO_A (메인 대화)
  └→ Line 2 (hasChoices = true)
       ├→ Choice "응" → nextDialogue: DialogueSO_B
       └→ Choice "아니" → nextDialogue: DialogueSO_C

DialogueSO_B (호감 루트)
  └→ Line 1 (hasChoices = true)
       ├→ Choice "도와줄게" → affectionChange: +10, responseText: "고마워!"
       └→ Choice "힘내" → affectionChange: +3

DialogueSO_C (일반 루트)
  └→ Line 0: "그래, 알겠어." → 대화 종료
```

### 7.5 새 아이템 추가

1. `Create → Game → Item`으로 ItemSO 생성
2. 소비 아이템이면 `isConsumable = true`, `useEffect`에 효과 설정
3. 아이템 획득은 코드에서 `StatusManager.Instance.Inventory.AddItem(itemSO)` 호출

---

## 8. 수치 밸런스 레퍼런스

### 기본 제공 활동 (LocationData.json)

| 장소 | 활동 | 지력 | 매력 | 용기 | 피로 | 비용 |
|------|------|------|------|------|------|------|
| 학교 도서관 | 공부하기 | +3 | - | - | +10 | 0 |
| 학교 도서관 | 동아리 활동 | - | +2 | +1 | +5 | 0 |
| 폴로니안 몰 | 아르바이트 | - | +1 | - | +15 | 0 (수입 1500) |
| 폴로니안 몰 | 쇼핑 | - | +3 | - | +5 | 1000 |
| 체육관 | 운동하기 | - | - | +3 | +20 | 500 |
| 기숙사 | 푹 쉬기 | - | - | - | **-30** | 0 |

### 30일 시뮬레이션 참고

- 하루 사용 가능 시간: 480분 (8시간)
- 활동 1회 = 보통 60분 → 하루 최대 약 8회 활동
- 30일 × 8회 = 최대 약 240회 활동
- 학자 엔딩(지력 80) 달성: 초기 10 + (3 × ~24회) = 82 → 약 24일간 공부 집중 필요

---

## 9. 트러블슈팅

### Q: 게임이 시작되지 않는다
- GameManager에 `statusManager`, `routineManager`, `uiManager`가 모두 할당되어 있는지 확인
- 각 Manager 오브젝트가 씬에 존재하고 활성화 상태인지 확인

### Q: 장소가 표시되지 않는다
- RoutineManager의 `locations` 리스트가 비어있지 않은지 확인
- LocationSO의 `openTimeMinutes`/`closeTimeMinutes`가 현재 게임 시간 범위에 포함되는지 확인
- 게임 시작 시간은 16:00(960분)이므로, 오전에만 영업하는 장소는 표시되지 않음

### Q: 캐릭터가 등장하지 않는다
- CharacterManager의 `allCharacters`에 해당 캐릭터가 추가되어 있는지 확인
- CharacterSchedule의 `locationId`가 LocationSO의 `id`와 **정확히** 일치하는지 확인
- 스케줄의 `activeDays`, `startTimeMinutes`~`endTimeMinutes` 범위가 현재 시간에 해당하는지 확인
- `requiredAffection` 값이 현재 호감도 이하인지 확인

### Q: 활동을 실행할 수 없다
- 소지금이 `cost` 이상인지 확인
- `requirements`의 최소 스탯 조건을 충족하는지 확인
- 컨디션이 **Exhausted(탈진)**이면 `sleep`/`rest` 외 활동은 불가

### Q: 엔딩이 표시되지 않는다
- GameManager의 `endings` 리스트에 EndingSO가 추가되어 있는지 확인
- 조건 없는 기본 엔딩(priority가 가장 높은 값)이 반드시 포함되어 있어야 함

### Q: 대화에서 선택지가 안 나온다
- DialogueLine의 `hasChoices`가 `true`로 설정되어 있는지 확인
- `choices` 리스트에 최소 1개 이상의 DialogueChoice가 있는지 확인

---

## 부록: Inspector 할당 체크리스트

씬 설정 시 아래 항목을 순서대로 확인하세요:

- [ ] **GameManager** 오브젝트 생성 및 컴포넌트 추가
  - [ ] `statusManager` → StatusManager 오브젝트
  - [ ] `routineManager` → RoutineManager 오브젝트
  - [ ] `uiManager` → UIManager 오브젝트
  - [ ] `endings` → EndingSO 에셋 리스트 (기본 엔딩 포함)
- [ ] **StatusManager** 오브젝트 생성 및 컴포넌트 추가
- [ ] **RoutineManager** 오브젝트 생성 및 컴포넌트 추가
  - [ ] `locations` → LocationSO 에셋 리스트
- [ ] **CharacterManager** 오브젝트 생성 및 컴포넌트 추가
  - [ ] `allCharacters` → CharacterSO 에셋 리스트
- [ ] **DialogueManager** 오브젝트 생성 및 컴포넌트 추가
- [ ] **UIManager** 오브젝트 생성 및 컴포넌트 추가
  - [ ] `statusPanel` → StatusPanel
  - [ ] `characterPanel` → CharacterPanel
  - [ ] `mainPanel` → MainPanel
  - [ ] `endingPanel` → EndingPanel
  - [ ] `dialoguePanel` → DialoguePanel
- [ ] **ScriptableObject 에셋** 생성 완료
  - [ ] LocationSO 에셋 (최소 1개)
  - [ ] ActivitySO 에셋 (각 장소별)
  - [ ] EndingSO 에셋 (기본 엔딩 포함)
  - [ ] CharacterSO 에셋 (선택사항)
  - [ ] DialogueSO 에셋 (선택사항)
  - [ ] ItemSO 에셋 (선택사항)
