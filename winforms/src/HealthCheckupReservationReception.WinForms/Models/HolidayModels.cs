// ── 휴무일 모델 ──────────────────────────────────────────────────────────────
// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
//
//   형식                   SP · Result Set                무엇
//   HolidaySearchRequest   SP-HOL-01 입력                 기간·구분
//   HolidayListItemDto     SP-HOL-01 RS1                  휴무일 한 행
//   HolidayRegistryDto     SP-HOL-01 RS2                  공휴일 등재현황
//   HolidayListReadDto     SP-HOL-01 RS0~RS2              한 호출의 세 Result Set
//   HolidaySaveRequest     SP-HOL-02·03·04 입력           등록·수정·삭제
//   HolidaySaveReadDto     SP-HOL-02·03 RS1 (04 는 없다)  저장된 휴무일

using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Models
{
    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>
    /// SP-HOL-01 의 조회조건 (05 §12.5). 두 날짜는 **필수**다 — 없으면 SP 가 `100` 이다.
    /// </summary>
    public sealed class HolidaySearchRequest
    {
        public DateTime? FromDate { get; set; }      // [@시작일자]
        public DateTime? ToDate { get; set; }        // [@종료일자]
        public string HolidayType { get; set; }      // [@휴무구분] null = 세 구분 전부
    }

    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>
    /// SP-HOL-01 `[dbo].[USP_HC_휴무일목록_조회]` 의 RS1 (05 §12.5).
    /// `휴무일자` 오름차순 고정이고 0건은 성공이다.
    /// </summary>
    public sealed class HolidayListItemDto
    {
        public DateTime HolidayDate { get; set; }   // [휴무일자]  PK. 바꾸지 않는다 (05 §12.6)
        public string HolidayName { get; set; }     // [휴무일명]
        public string HolidayType { get; set; }     // [휴무구분]  DbHolidayType 셋 중 하나
        public bool IsActive { get; set; }          // [사용여부]  0 은 삭제가 아니라 일시 무효화다
        public string Memo { get; set; }            // [비고]
        public byte[] RowVersion { get; set; }      // [행버전]    표시하지 않는다 (03 §24.3)
    }

    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>
    /// SP-HOL-01 의 RS2 공휴일등재현황 (05 §12.5). **항상 1행**이다.
    ///
    /// [X] **임계 숫자를 화면이 갖지 않는다.** 그 값의 단일 출처는 `00` §7.4 이고 SP 가 그것을
    ///     <see cref="WarningThresholdDays"/> 로 함께 내보낸다 — 화면은
    ///     `잔여일수 &lt; 경고임계일수` 만 비교한다 (03 §24.6).
    ///
    /// RS2 는 `@휴무구분` 필터의 영향을 받지 않는다. 화면이 자체휴무일만 보고 있어도 만료
    /// 경고는 같아야 한다.
    /// </summary>
    public sealed class HolidayRegistryDto
    {
        public DateTime? LastHolidayDate { get; set; }   // [공휴일최종일자] 0건이면 NULL
        public int? RemainingDays { get; set; }          // [잔여일수]       이미 지났으면 음수
        public int WarningThresholdDays { get; set; }    // [경고임계일수]
    }

    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>SP-HOL-01 의 세 Result Set 을 한 덩어리로 나른다 (05 §12.5).</summary>
    public sealed class HolidayListReadDto
    {
        public DbResult Result { get; set; }
        public IList<HolidayListItemDto> Rows { get; set; }
        public HolidayRegistryDto Registry { get; set; }
    }

    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>
    /// SP-HOL-02 등록 · SP-HOL-03 수정 · SP-HOL-04 삭제의 입력 (05 §12.6~§12.8).
    ///
    /// `휴무구분` 이 없다 — 등록 SP 는 `자체휴무일` 만 만들고 화면이 고르지 않는다 (03 §24.5).
    /// `@조작자명` 도 없다: `변경이력` 의 대상 테이블이 `수검자`·`예약접수` 둘뿐이라 기록할 곳이
    /// 없고, 받아 두고 버리는 Parameter 를 계약에 남기지 않는다 (05 §12 머리말).
    /// </summary>
    public sealed class HolidaySaveRequest
    {
        public DateTime HolidayDate { get; set; }   // [@휴무일자] PK
        public byte[] RowVersion { get; set; }      // [@행버전]   수정·삭제만. 등록은 null
        public string HolidayName { get; set; }     // [@휴무일명]
        public bool IsActive { get; set; }          // [@사용여부]
        public string Memo { get; set; }            // [@비고]
    }

    // 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
    /// <summary>
    /// SP-HOL-02 의 RS1 휴무일결과 (05 §12.6). [R22] 등록·수정이 한 SP 다.
    /// SP-HOL-04 삭제는 RS1 이 없다 — 지운 행의 행버전을 돌려줄 이유가 없다 (05 §12.8).
    /// </summary>
    public sealed class HolidaySaveReadDto
    {
        public DbResult Result { get; set; }
        public DateTime? HolidayDate { get; set; }   // [휴무일자]
        public byte[] RowVersion { get; set; }       // [행버전]
    }
}
