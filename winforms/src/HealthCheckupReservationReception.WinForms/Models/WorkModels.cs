// ── 예약·접수 공통 업무 모델 ─────────────────────────────────────────────────
// 화면 ID: WF-WRK-01 (03 §9) · DLG-RCP-01 (03 §11) · DLG-RCP-02 (03 §12)
//
// **예약과 접수는 같은 행이다** — 상태코드만 다르다. 그래서 조회·상세·저장 모델이
// 하나이고, 예약 계열 전용인 것만 ReservationModels.cs 에 있다.
//
//   형식                     SP · Result Set                  무엇
//   WorkSearchRequest        SP-WRK-01 입력                   기간·상태·차트번호·이름
//   WorkListItemDto          SP-WRK-01 RS1                    업무 목록 한 행
//   WorkListReadDto          SP-WRK-01 RS0+RS1                한 호출의 결과
//   WorkDetailDto            SP-WRK-02 RS1                    업무 상세·정원
//   WorkExamItemDto          SP-WRK-02 RS2·RS3 · SP-RSV-01 RS4  검사항목 한 행 (공용)
//   WorkActionDto            SP-WRK-02 RS4                    가능한 업무 5행
//   WorkDetailReadDto        SP-WRK-02 RS0~RS5                한 호출의 여섯 Result Set
//   WorkActionRequest        SP-RSV-04·RCP-01·RCP-03 입력     업무ID·행버전만 보내는 것
//   ExtraExamChangeRequest   SP-RCP-02 입력                   접수완료 AEX 변경
//   WorkSaveResultDto        쓰기 SP 들의 RS1                 업무ID·상태코드·행버전
//   WorkSaveReadDto          RS0+RS1                          한 호출의 결과
//   WorkActionState          (SP 아님)                        화면 Action 의 열림·닫힘

using System;
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// SP-WRK-01 의 조회조건 (05 §8.1 · 03 §9.3).
    /// 값은 화면이 넣은 그대로다 — 정규화는 Service 가 한다.
    /// </summary>
    public sealed class WorkSearchRequest
    {
        public DateTime? FromDate { get; set; }    // [@시작일] 포함
        public DateTime? ToDate { get; set; }      // [@종료일] 포함
        public string StatusCode { get; set; }     // [@상태코드] null = 전체. 조회조건으로 보지 않는다
        public string ChartNo { get; set; }        // [@차트번호] 정확검색
        public string Name { get; set; }           // [@성명] 접두검색
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// SP-WRK-01 `[dbo].[USP_HC_예약접수목록_조회]` 의 RS1 (05 §8.1).
    /// 영문 이름은 05 §16.5 대응표가 정한다 — 표에 없는 넷(예약일·시간대코드·상태코드·상태명)은
    /// DB 계층에만 있는 컬럼이라 대응표의 대상이 아니다.
    /// </summary>
    public sealed class WorkListItemDto
    {
        public long WorkId { get; set; }         // [업무ID]     내부키. Grid 에 노출하지 않는다 (03 §9.4)
        public long PatientId { get; set; }      // [수검자ID]   내부키. 같은 이유로 노출하지 않는다
        public DateTime ReserveDate { get; set; }// [예약일]
        public string SlotCode { get; set; }     // [시간대코드] AM/PM
        public string StatusCode { get; set; }   // [상태코드]   RSV/RCP/CNR/CNC
        // [상태명] 은 읽지 않는다 — 표시명은 clsWorkText.FormatStatus 가 갖는다 (2026-09-11).
        public string Name { get; set; }         // [성명]
        public string ChartNo { get; set; }      // [차트번호]
        public string Gender { get; set; }       // [성별]       M/F 계산열
        public string Birthday { get; set; }     // [생년월일]   yyyyMMdd 계산열
        public string MobilePhone { get; set; }  // [휴대전화]
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>SP-WRK-01 한 번의 호출이 낸 두 Result Set (05 §8.1).</summary>
    public sealed class WorkListReadDto
    {
        public DbResult Result { get; set; }               // RS0
        public IList<WorkListItemDto> Rows { get; set; }   // RS1. RS0 실패면 null
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// SP-WRK-02 `[dbo].[USP_HC_예약접수상세_조회]` 의 RS1 업무상세 (05 §8.2).
    ///
    /// 정원·현재인원·잔여자리는 **현재 조회값**이며 Work 저장 Snapshot 이 아니다 (03 §9.5).
    /// </summary>
    public sealed class WorkDetailDto
    {
        public long WorkId { get; set; }           // [업무ID]
        public long PatientId { get; set; }        // [수검자ID]
        public string ChartNo { get; set; }        // [차트번호]
        public string Name { get; set; }           // [성명]
        public string Birthday { get; set; }       // [생년월일]
        public string Gender { get; set; }         // [성별]
        public string MobilePhone { get; set; }    // [휴대전화]
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드]
        public string StatusCode { get; set; }     // [상태코드]
        public int Capacity { get; set; }          // [정원]
        public int CurrentCount { get; set; }      // [현재인원]
        public int RemainingSeats { get; set; }    // [잔여자리]
        public byte[] RowVersion { get; set; }     // [행버전] 05 §16.3 — 문자열로 바꾸지 않는다
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// SP-WRK-02 의 RS2 국가검사항목·RS3 추가검사항목 (05 §8.2).
    ///
    /// 두 Result Set 은 컬럼이 하나만 다르다 — NEX 는 `국가검사구분`·`국가검사규칙코드` 를,
    /// AEX 는 `추가검사코드` 를 더 갖는다. 나머지 둘이 같으므로 DTO 를 한 벌만 둔다:
    /// 화면도 같은 ReadOnly Grid 두 개이고, 갈라 두면 Grid 정의가 두 벌이 된다.
    /// 해당 없는 칸은 null 이다.
    /// </summary>
    public sealed class WorkExamItemDto
    {
        public string ExamItemCode { get; set; }   // [검사항목코드] 둘 다 갖는다
        public string ExamItemName { get; set; }   // [검사항목명]   둘 다 갖는다
        public string NexType { get; set; }        // [국가검사구분]     NEX 만
        public string NexRuleCode { get; set; }    // [국가검사규칙코드] NEX 만
        public string AexCode { get; set; }        // [추가검사코드]     AEX 만
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// SP-WRK-02 의 RS4 가능한업무 (05 §8.2). 정확히 5행이며 `업무동작코드` 는 고정이다.
    ///
    /// **허용여부를 화면이 다시 계산하지 않는다** — 상태·공통 업무조건·마감의 조합과
    /// 차단 우선순위는 DB 가 갖는다 (05 §8.2 허용조건 표). 화면은 받은 값을 그리기만 한다.
    /// </summary>
    public sealed class WorkActionDto
    {
        public string ActionCode { get; set; }     // [업무동작코드]
        public bool Allowed { get; set; }          // [허용여부]
        public int ReasonCode { get; set; }        // [사유코드]
        public string ReasonMessage { get; set; }  // [사유메시지]
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>SP-WRK-02 한 번의 호출이 낸 여섯 Result Set (05 §8.2).</summary>
    public sealed class WorkDetailReadDto
    {
        public DbResult Result { get; set; }                  // RS0
        public WorkDetailDto Detail { get; set; }             // RS1. RS0 실패면 null
        public IList<WorkExamItemDto> NexItems { get; set; }  // RS2
        public IList<WorkExamItemDto> AexItems { get; set; }  // RS3
        public IList<WorkActionDto> Actions { get; set; }     // RS4

        /// <summary>
        /// RS5 추가검사구성 — **정확히 7행**이다 (05 §8.2, R18). `AexItems`(RS3)는 저장된
        /// 것만 주므로 고치는 화면은 이쪽을 쓴다.
        ///
        /// `ReservationAexItemDto` 를 그대로 쓴다 — 「고르는 자리」의 DTO 이고 RS5 의 일곱
        /// 컬럼이 그 모양에 그대로 앉는다. 둘로 가르면 같은 Grid 정의가 두 벌이 된다.
        /// </summary>
        public IList<ReservationAexItemDto> AexOptions { get; set; }
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// 선택행 하나에 거는 Write SP 셋의 입력 — `SP-RSV-04`(예약취소) · `SP-RCP-01`(접수완료) ·
    /// `SP-RCP-03`(접수취소). **셋의 Parameter 가 글자까지 같다** (05 §11.3 · §12.1 · §12.3):
    /// `@업무ID` · `@행버전` · `@조작자명`.
    ///
    /// 같은 모양을 셋으로 베끼지 않는다 (킷 §2 — 같은 shape 가 둘 이상이면 한 벌이다).
    /// 어느 SP 를 부를지는 Service 의 메서드 이름이 가른다.
    /// </summary>
    public sealed class WorkActionRequest
    {
        public long WorkId { get; set; }          // [@업무ID]
        public byte[] RowVersion { get; set; }    // [@행버전]
        public string OperatorName { get; set; }  // [@조작자명]
    }

    // 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
    /// <summary>
    /// SP-RCP-02 `[dbo].[USP_HC_접수추가검사_변경]` 의 입력 (05 §12.2).
    ///
    /// `ReservationChangeRequest`(예약변경)와 갈라 둔다 — 그쪽은 예약일·시간대를 함께
    /// 보내고 이쪽은 **AEX 만** 보낸다. 03 §12 가 예약일·시간대·NEX 를 ReadOnly 로 못박은
    /// 것이 곧 이 Parameter 목록이다.
    ///
    /// 동일 집합이면 SP 가 `결과코드=1` No-op 을 낸다 — 화면이 미리 견주지 않는다.
    /// </summary>
    public sealed class ExtraExamChangeRequest
    {
        public ExtraExamChangeRequest()
        {
            AexSelected = new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        public long WorkId { get; set; }            // [@업무ID]
        public byte[] RowVersion { get; set; }      // [@행버전]
        public bool[] AexSelected { get; set; }     // [@추가검사01~07선택여부]
        public string OperatorName { get; set; }    // [@조작자명]
    }

    // 화면 ID: WF-RSV-01 · WF-WRK-01 — 예약·접수 Write 공통 (05 §11)
    /// <summary>
    /// 05 §11 머리말 — **예약·접수 Write SP 의 성공 RS1 은 공통으로 이 Schema 다.**
    /// 예약등록·예약변경·예약취소·접수완료·추가검사변경·접수취소 여섯이 같은 것을 낸다.
    /// 그래서 DTO 도 한 벌이다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class WorkSaveResultDto
    {
        public long WorkId { get; set; }        // [업무ID]   03 §8.11 이 이것으로 Workbench 를 연다
        public string StatusCode { get; set; }  // [상태코드]
        public byte[] RowVersion { get; set; }  // [행버전]   05 §16.3 — 문자열로 바꾸지 않는다
    }

    // 화면 ID: WF-RSV-01 · WF-WRK-01 — 예약·접수 Write 공통 (05 §11)
    /// <summary>예약·접수 Write SP 한 번의 호출이 낸 두 Result Set (05 §11).</summary>
    public sealed class WorkSaveReadDto
    {
        public DbResult Result { get; set; }       // RS0
        public WorkSaveResultDto Row { get; set; } // RS1. 실패면 null
    }

    // 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
    /// <summary>
    /// 03 §9.6 · §9.7 — Workbench 의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖고
    /// 판정은 Presenter 가 하므로, 그 판정을 실어 나르는 자리다.
    ///
    /// 앞 다섯은 05 §8.2 RS4 `허용여부` 를 그대로 옮긴 것이다 — 화면이 다시 계산하지 않는다.
    /// <see cref="ChangeLog"/> 만 화면 규칙이다: 상태와 무관하게 행이 선택되면 열린다
    /// (03 §9.6 · §23.4 — 취소된 업무의 변경 내역을 보는 것이 열람의 목적이다).
    ///
    /// `[현장 당일예약]` 은 여기 없다. 선택행과 무관한 독립 Action 이라 (03 §9.7)
    /// Reception Ribbon 에서 늘 열려 있다.
    /// </summary>
    public sealed class WorkActionState
    {
        public bool EditReservation { get; set; }
        public bool CancelReservation { get; set; }
        public bool StartReception { get; set; }
        public bool EditExtra { get; set; }
        public bool CancelReception { get; set; }
        public bool ChangeLog { get; set; }

        /// <summary>행 미선택 · 상세 조회 실패 — 전부 닫힌 상태다 (03 §9.6 미선택 행).</summary>
        public static WorkActionState None()
        {
            return new WorkActionState();
        }
    }
}
