// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
namespace HealthCheckupReservationReception.Models
{
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
}
