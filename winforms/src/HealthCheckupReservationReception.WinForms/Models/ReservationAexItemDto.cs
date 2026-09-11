// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
namespace HealthCheckupReservationReception.Models
{
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
}
