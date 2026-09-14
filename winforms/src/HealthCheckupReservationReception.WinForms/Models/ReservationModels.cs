// ── 예약 모델 ────────────────────────────────────────────────────────────────
// 화면 ID: WF-RSV-01 (03 §8) · DLG-RSV-01 (03 §10)
//
// **SP-RSV-01 하나가 여섯 Result Set 을 낸다** — 아래 다섯이 그 RS1~RS5 다.
// 국가검사항목(RS4)만 WorkExamItemDto 를 함께 쓴다 (WorkModels.cs).
//
//   형식                              SP · Result Set     무엇
//   ReservationAvailabilityRequest    SP-RSV-01 입력      수검자·예약일·시간대·AEX 선택
//   ReservationSummaryDto             SP-RSV-01 RS1       예약요약·변경범위·저장가능
//   SlotInfoDto                       SP-RSV-01 RS2       시간대 정원·마감·선택가능
//   ExamTargetDto                     SP-RSV-01 RS3       TGT 대상판정
//   ReservationAexItemDto             SP-RSV-01 RS5       추가검사 7종과 선택가능 사유
//   ReservationAvailabilityReadDto    SP-RSV-01 RS0~RS5   한 호출의 여섯 Result Set
//   ReservationSaveRequest            SP-RSV-02 입력      신규 예약
//   ReservationChangeRequest          SP-RSV-03 입력      예약 변경

using System;
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 `[dbo].[USP_HC_예약가능정보_조회]` 의 입력 (05 §9.2 · §9.3).
    ///
    /// **신규예약과 예약변경이 같은 입력을 쓴다.** 신규는 <see cref="WorkId"/>·
    /// <see cref="RowVersion"/> 이 null 이고, 예약변경은 둘 다 필수다 (§9.3 조합표) —
    /// 지금 쓰는 곳은 WF-RSV-01 뿐이지만 갈라 두면 §9.4 변경범위 규칙이 두 곳에 생긴다.
    ///
    /// [X] 05 §9.3 — AEX BIT 중 하나라도 NULL 이면 `100` 이다. 그래서 <see cref="AexSelected"/>
    ///     는 늘 계약 개수만큼 차 있어야 하고, 생성자가 그 길이로 만들어 둔다.
    /// </summary>
    public sealed class ReservationAvailabilityRequest
    {
        public ReservationAvailabilityRequest()
        {
            AexSelected = new bool[AexParameterCount];
        }

        /// <summary>05 §9.2 의 `@추가검사01~07선택여부` 개수. Parameter 목록 그 자체다.</summary>
        public const int AexParameterCount = 7;

        public long PatientId { get; set; }         // [@수검자ID]
        public long? WorkId { get; set; }           // [@업무ID]   신규예약은 NULL
        public byte[] RowVersion { get; set; }      // [@행버전]   신규예약은 NULL
        public string ReserveType { get; set; }     // [@예약구분] NORMAL / WALKIN
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드] 날짜 고르는 중에는 NULL

        /// <summary>
        /// 순서가 곧 `추가검사01`~`07` 이다. RS5 는 추가검사코드 ASC 로 오므로 (05 §9.6)
        /// 화면이 받은 순서를 그대로 되돌려 주면 자리가 맞는다 — 코드 문자열을 C# 에 적지 않는다.
        /// </summary>
        public bool[] AexSelected { get; set; }
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 의 RS1 예약요약 (05 §9.6). 정확히 1행이다.
    ///
    /// **`저장가능` 을 화면이 다시 계산하지 않는다** (05 §9.12). 공통 업무가능·다른 유효예약·
    /// 시간대 선택가능·TGT·NEX 8건·AEX 유효성의 곱을 DB 가 이미 냈다. 화면이 한 번 더 세면
    /// 같은 규칙이 두 곳에 생긴다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class ReservationSummaryDto
    {
        public string ChangeScope { get; set; }    // [변경범위] ALL/SLOT/EXTRA/SLOT_EXTRA/NONE
        public long PatientId { get; set; }        // [수검자ID]
        public long? WorkId { get; set; }          // [업무ID]   신규예약은 NULL
        public string ReserveType { get; set; }    // [예약구분]
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드] 미선택이면 NULL

        // 셋 다 신규예약에서는 NULL 이다 (05 §9.6).
        public bool? DateChanged { get; set; }     // [예약일변경여부]
        public bool? SlotChanged { get; set; }     // [시간대변경여부]
        public bool? AexChanged { get; set; }      // [추가검사변경여부]

        public bool WorkAllowed { get; set; }      // [현재업무가능]
        public long? OtherWorkId { get; set; }     // [다른업무ID] 있으면 03 §8.5 가 Workbench 로 보낸다
        public bool CanSave { get; set; }          // [저장가능]
        public int BlockCode { get; set; }         // [차단코드]   대표 하나. 우선순위는 05 §9.6
        public string BlockMessage { get; set; }   // [차단메시지]
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 의 RS2 시간대정보 (05 §9.7). 일정을 평가하는 변경범위에서 AM/PM 2행이다.
    ///
    /// [X] **마감시각을 C# 에 심지 않는다.** 03 §8.6 이 표로 적은 네 값(Normal/WalkIn ×
    ///     AM/PM)은 R13 에서 DB `운영기준` 테이블로 옮겨졌고 (`04` §8.7) 여기 <see cref="CutoffTime"/>·
    ///     <see cref="CutoffPassed"/> 로 내려온다. 화면은 받은 것을 그리기만 한다.
    /// </summary>
    public sealed class SlotInfoDto
    {
        public string SlotCode { get; set; }        // [시간대코드] AM/PM
        public string SlotName { get; set; }        // [시간대명]   DB 가 준 표시명을 그대로 쓴다
        public int Capacity { get; set; }           // [정원]
        public int CurrentCount { get; set; }       // [현재인원]
        public int AppliedCount { get; set; }       // [적용후인원] 신규는 현재인원+1 (05 §9.7)
        public int RemainingSeats { get; set; }     // [잔여자리]
        public bool IsOperating { get; set; }       // [운영여부]   토요일 PM 등
        public TimeSpan? CutoffTime { get; set; }   // [마감시각]
        public bool CutoffPassed { get; set; }      // [마감경과여부]
        public bool Selectable { get; set; }        // [선택가능]   일정·운영·마감·정원만 본다
        public int BlockCode { get; set; }          // [차단코드]
        public string BlockMessage { get; set; }    // [차단메시지]
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 의 RS3 검진대상 (05 §9.8). `ALL` 에서 일정 평가가 가능하면 1행, 아니면 0행이다.
    ///
    /// 03 §8.7 의 판정문구는 화면 계산결과이고 별도 DB 컬럼이 아니다 — 사유는 DB 가 주고
    /// 화면은 `대상 —` / `비대상 —` 앞머리만 붙인다.
    /// </summary>
    public sealed class ExamTargetDto
    {
        public bool IsTarget { get; set; }              // [검진대상여부]
        public int Age { get; set; }                    // [나이]       예약일 기준
        public DateTime? LastCompletedDate { get; set; } // [최근완료일자] 최초검진이면 NULL
        public int ReasonCode { get; set; }             // [사유코드]   400 UnderAge · 401 NotDue
        public string ReasonMessage { get; set; }       // [사유메시지]
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 의 RS5 추가검사항목 (05 §9.10). AEX 를 실제 평가하는 변경범위에서 정확히 7행이다.
    ///
    /// `WorkExamItemDto`(저장된 구성 열람)와 갈라 둔다 — 이쪽은 **고르는 자리**라 선택상태와
    /// 선택불가 사유를 함께 나른다 (03 §8.9). 한 DTO 로 합치면 읽기 전용 Grid 가 쓰지 않는
    /// 칸 다섯을 늘 달고 다니게 된다.
    ///
    /// TGT 비대상이어도 7행이 오고 전부 `선택가능=0`·`유효선택여부=0` 이다 (05 §9.10).
    /// </summary>
    public sealed class ReservationAexItemDto
    {
        public string AexCode { get; set; }            // [추가검사코드] OPT01~OPT07
        public string ExamItemCode { get; set; }       // [검사항목코드]
        public string ExamItemName { get; set; }       // [검사항목명]
        public bool Requested { get; set; }            // [요청선택여부] 화면이 보낸 값
        public bool EffectiveSelected { get; set; }    // [유효선택여부] DB 가 실제로 인정한 값
        public bool Selectable { get; set; }           // [선택가능]
        public int ReasonCode { get; set; }            // [사유코드]   410 ExamOff · 411 WrongGender · 412 ExamDuplicate
        public string ReasonMessage { get; set; }      // [사유메시지] 03 §8.9 의 `선택불가 사유` 칸
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-01 한 번의 호출이 낸 여섯 Result Set (05 §9.5).
    ///
    /// **한 번에 받는 것이 계약이다.** 갈라 부르면 일정·TGT·NEX·AEX 가 서로 다른 시점의
    /// 값이 되어 한 화면에 앉는다 — 정원은 초 단위로 바뀐다.
    ///
    /// 채워지는 것은 <see cref="ReservationSummaryDto.ChangeScope"/> 가 정한다 (05 §9.11
    /// Cardinality 표). 신규예약은 늘 `ALL` 이라 전부 온다.
    /// </summary>
    public sealed class ReservationAvailabilityReadDto
    {
        public DbResult Result { get; set; }                    // RS0
        public ReservationSummaryDto Summary { get; set; }      // RS1. 정확히 1행
        public IList<SlotInfoDto> Slots { get; set; }           // RS2. AM/PM 2행 또는 0행
        public ExamTargetDto Target { get; set; }               // RS3. 1행 또는 0행
        public IList<WorkExamItemDto> NexItems { get; set; }    // RS4. 8~11행 또는 0행
        public IList<ReservationAexItemDto> AexItems { get; set; } // RS5. 7행 또는 0행
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    /// <summary>
    /// SP-RSV-02 `[dbo].[USP_HC_예약_등록]` 의 입력 (05 §11.1).
    ///
    /// 조회 SP 가 낸 `저장가능=1` 은 저장을 보장하지 않는다 — Write SP 는 조회 결과를
    /// 신뢰하지 않고 모든 조건을 다시 검증한다 (05 §11.1). 03 §8.11 의 2단계 저장이 그것이다.
    /// </summary>
    public sealed class ReservationSaveRequest
    {
        public ReservationSaveRequest()
        {
            AexSelected = new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        public long PatientId { get; set; }         // [@수검자ID]
        public string ReserveType { get; set; }     // [@예약구분] WalkIn 은 예약일=DB 오늘날짜여야 한다
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드]
        public bool[] AexSelected { get; set; }     // [@추가검사01~07선택여부]
        public string OperatorName { get; set; }    // [@조작자명]
    }

    // 화면 ID: DLG-RSV-01 — 예약 변경 (03 §10)
    /// <summary>
    /// SP-RSV-03 `[dbo].[USP_HC_예약_변경]` 의 입력 (05 §11.2).
    ///
    /// `ReservationSaveRequest`(신규)와 갈라 둔다 — 신규는 `수검자ID`·`예약구분` 을 보내고
    /// 변경은 `업무ID`·`행버전` 을 보낸다. 한 DTO 로 합치면 어느 칸이 어느 쪽 것인지
    /// 호출부가 알아서 비워야 하고, 그 규칙은 어디에도 적히지 않는다.
    ///
    /// [X] **무엇이 바뀌었는지 화면이 정하지 않는다.** 05 §11.2 가 *"현재 예약일·시간대와
    ///     AEX 코드 집합으로 … 변경여부 계산"* 이라고 못박았다 — 화면은 **원하는 최종 상태**를
    ///     통째로 보내고 변경범위는 DB 가 잰다. 변경이 하나도 없으면 `결과코드=1` No-op 이다.
    /// </summary>
    public sealed class ReservationChangeRequest
    {
        public ReservationChangeRequest()
        {
            AexSelected = new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        public long WorkId { get; set; }            // [@업무ID]
        public byte[] RowVersion { get; set; }      // [@행버전]   조회 시점의 것을 그대로 되돌려준다
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드]
        public bool[] AexSelected { get; set; }     // [@추가검사01~07선택여부]
        public string OperatorName { get; set; }    // [@조작자명]
    }
}
