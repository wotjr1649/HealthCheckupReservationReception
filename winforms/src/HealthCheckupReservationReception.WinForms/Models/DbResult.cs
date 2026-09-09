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
}
