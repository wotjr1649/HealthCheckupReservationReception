using System;

namespace HealthCheckupReservationReception.Models
{
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
}
