// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
namespace HealthCheckupReservationReception.Models
{
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
}
