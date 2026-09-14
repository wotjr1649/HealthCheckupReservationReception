// ── 공통 모델 ────────────────────────────────────────────────────────────────
//
// 화면이 아니라 **모든 SP 가 함께 쓰는 것**이다.
//
//   형식                     SP · Result Set            무엇
//   DbResult                 모든 SP 의 RS0             성공여부·결과코드·결과메시지
//   CommonWorkStatusDto      SP-COM-01 RS1              오늘날짜·운영시간·업무가능
//   CommonWorkStatusReadDto  SP-COM-01 RS0+RS1          한 호출의 두 Result Set

using System;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 모든 외부 SP 의 첫 번째 Result Set (05 §3.1 · §16.2).
    /// 컬럼 이름은 한글이고 C# 식별자는 영문 PascalCase 다 — 두 이름이 만나는 곳은
    /// Result Set 컬럼명 문자열뿐이다 (05 §16.2). 그 문자열은 Repositories/ 안에만 있다.
    /// </summary>
    public sealed class DbResult
    {
        public bool Success { get; set; }          // [성공여부]
        public int Code { get; set; }              // [결과코드]
        public string Message { get; set; }        // [결과메시지]
        public string Field { get; set; }          // [오류항목]
        public DateTime ServerTime { get; set; }   // [서버시각]

        public DbCode DbCode
        {
            get { return (DbCode)Code; }
        }
    }

    /// <summary>
    /// SP-COM-01 `[dbo].[USP_HC_공통업무상태_조회]` 의 RS1 (05 §7.1).
    /// 이 열 이름들은 05 §16.5 대응표에 없다 — 화면·C# 계층에 짝이 없는
    /// 조회 결과 전용 컬럼이기 때문이다. 여기 붙인 영문 이름이 그 값의 C# 이름이다.
    /// </summary>
    public sealed class CommonWorkStatusDto
    {
        public DateTime Today { get; set; }          // [오늘날짜]
        public string DayName { get; set; }          // [요일명]
        public string HolidayName { get; set; }      // [휴무일명]   NULL 가능
        public TimeSpan OpenTime { get; set; }       // [운영시작시각]
        public TimeSpan CloseTime { get; set; }      // [운영종료시각]
        public bool IsBusinessDay { get; set; }      // [업무일여부]
        public bool IsWithinHours { get; set; }      // [운영시간내여부]
        public bool IsWorkAllowed { get; set; }      // [현재업무가능]
        public int BlockCode { get; set; }           // [차단코드]    0 / 308 / 309
        public string BlockMessage { get; set; }     // [차단메시지]
    }

    /// <summary>
    /// SP-COM-01 한 번의 호출이 낸 두 Result Set 을 그대로 나른다.
    /// 리포지토리는 숫자와 값만 내보내고 그것을 업무 문구로 바꾸는 것은 Service 다
    /// (킷 §2 · contract/repository.md).
    /// </summary>
    public sealed class CommonWorkStatusReadDto
    {
        public DbResult Result { get; set; }            // RS0
        public CommonWorkStatusDto Status { get; set; }  // RS1. RS0 실패면 null
    }
}
