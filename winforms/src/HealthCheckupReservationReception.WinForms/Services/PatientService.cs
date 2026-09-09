using System.Collections.Generic;
using System.Linq;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    /// <summary>
    /// WF-PAT-01 의 업무 계층 (03 §5.3 · 05 §7.2 · §7.3).
    /// 정규화와 길이 검증이 여기 있다 — 비어 있음 판정은 Presenter 가 한다 (킷 §6).
    /// </summary>
    public sealed class PatientService : IPatientService
    {
        // 05 §7.2 의 Parameter 크기. 화면의 MaxLength 는 UI 제한이지 검증이 아니다 (킷 §6).
        private const int ChartNoMax = 100;
        private const int NameMax = 100;
        private const int SocialNumberLength = 13;
        private const int BirthdayLength = 8;
        private const int MobilePhoneMax = 13;

        private readonly IPatientRepository _repository;

        public PatientService(IPatientRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<IList<PatientListItemDto>> Search(PatientSearchRequest request)
        {
            // 03 §5.3 — 주민번호와 전화번호는 `-` 를 제거하여 조회한다. 빈 문자열은 미입력이다.
            var normalized = new PatientSearchRequest
            {
                ChartNo = Trim(request.ChartNo),
                Name = Trim(request.Name),
                SocialNumber = Digits(request.SocialNumber),
                Birthday = Digits(request.Birthday),
                MobilePhone = Digits(request.MobilePhone),
            };

            string tooLong = FirstTooLong(normalized);
            if (tooLong != null)
            {
                return OperationResult<IList<PatientListItemDto>>.Failure(tooLong);
            }

            PatientListReadDto read = _repository.Search(normalized);
            if (read == null || read.Result == null)
            {
                return OperationResult<IList<PatientListItemDto>>.Failure("수검자 목록을 읽지 못했습니다.");
            }

            // 분기는 숫자 결과코드로만 한다 (05 §3.6 · §4.3).
            if (!read.Result.Success)
            {
                return OperationResult<IList<PatientListItemDto>>.Failure(read.Result.Message);
            }

            // 조회 0건은 성공이다 (05 §3.4).
            return OperationResult<IList<PatientListItemDto>>.Success(
                read.Rows ?? new List<PatientListItemDto>());
        }

        public OperationResult<PatientDetailDto> GetDetail(long patientId)
        {
            PatientDetailReadDto read = _repository.ReadDetail(patientId);
            if (read == null || read.Result == null)
            {
                return OperationResult<PatientDetailDto>.Failure("수검자 상세를 읽지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                return OperationResult<PatientDetailDto>.Failure(read.Result.Message);
            }

            if (read.Detail == null)
            {
                // RS1 은 정확히 1행이다 (05 §7.3). 비었으면 계약 위반이므로 성공으로 읽지 않는다.
                return OperationResult<PatientDetailDto>.Failure("수검자 상세 결과가 비어 있습니다.");
            }

            return OperationResult<PatientDetailDto>.Success(read.Detail);
        }

        private static string FirstTooLong(PatientSearchRequest r)
        {
            if (Over(r.ChartNo, ChartNoMax)) { return "차트번호는 " + ChartNoMax + "자 이하로 입력하십시오."; }
            if (Over(r.Name, NameMax)) { return "이름은 " + NameMax + "자 이하로 입력하십시오."; }
            if (r.SocialNumber != null && r.SocialNumber.Length != SocialNumberLength)
            {
                return "주민등록번호는 숫자 " + SocialNumberLength + "자리로 입력하십시오.";
            }
            if (r.Birthday != null && r.Birthday.Length != BirthdayLength)
            {
                return "생년월일은 숫자 " + BirthdayLength + "자리로 입력하십시오.";
            }
            if (Over(r.MobilePhone, MobilePhoneMax)) { return "휴대전화는 " + MobilePhoneMax + "자 이하로 입력하십시오."; }
            return null;
        }

        private static bool Over(string value, int max)
        {
            return value != null && value.Length > max;
        }

        private static string Trim(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string Digits(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null : digits;
        }
    }
}
