using System.Collections.Generic;
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
        private const int PhoneMax = 13;
        private const int EmailMax = 200;
        private const int ZipcodeMax = 10;
        private const int AddressMax = 200;
        private const int OperatorNameMax = 50;

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
                SocialNumber = clsPatientText.Digits(request.SocialNumber),
                Birthday = clsPatientText.Digits(request.Birthday),
                MobilePhone = clsPatientText.Digits(request.MobilePhone),
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

        /// <summary>
        /// SP-PAT-05 (05 §7.4). **0행은 실패가 아니다** — 유효업무가 없다는 뜻이고
        /// 03 §8.5 는 그때 일정영역을 연다. `Value` 가 null 인 성공으로 돌려준다.
        /// </summary>
        public OperationResult<PatientValidWorkDto> GetValidWork(long patientId)
        {
            PatientValidWorkReadDto read = _repository.ReadValidWork(patientId);
            if (read == null || read.Result == null)
            {
                return OperationResult<PatientValidWorkDto>.Failure("수검자의 유효업무를 읽지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                return OperationResult<PatientValidWorkDto>.Failure(read.Result.Message);
            }

            return OperationResult<PatientValidWorkDto>.Success(read.Work);
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

        /// <summary>
        /// DLG-PAT-01 New 저장 (SP-PAT-03 · 05 §10.1).
        /// 비어 있음은 Presenter 가, 고유성·후보판정은 DB 가 한다 (07 §7).
        /// </summary>
        public OperationResult<PatientSaveReadDto> Register(PatientSaveRequest request)
        {
            PatientSaveRequest normalized = NormalizeSave(request);

            // 05 §10.1 조합 — 자동발급이면 차트번호를 싣지 않는다 (03 §6.3).
            if (normalized.AutoChartNo)
            {
                normalized.ChartNo = null;
            }

            string tooLong = FirstTooLongSave(normalized);
            if (tooLong != null)
            {
                return OperationResult<PatientSaveReadDto>.Failure(tooLong);
            }

            return Judged(_repository.Register(normalized));
        }

        /// <summary>DLG-PAT-01 Edit 저장 (SP-PAT-04 · 05 §10.2).</summary>
        public OperationResult<PatientSaveReadDto> Update(PatientSaveRequest request)
        {
            PatientSaveRequest normalized = NormalizeSave(request);

            string tooLong = FirstTooLongSave(normalized);
            if (tooLong != null)
            {
                return OperationResult<PatientSaveReadDto>.Failure(tooLong);
            }

            return Judged(_repository.Update(normalized));
        }

        /// <summary>
        /// RS0 을 받아 왔으면 성공이다 — `202`·`203` 은 실패 결과코드지만 화면이 이어서
        /// 처리해야 하므로 여기서 접지 않는다 (05 §3.5 · 03 §6.3 · §6.5).
        /// </summary>
        private static OperationResult<PatientSaveReadDto> Judged(PatientSaveReadDto read)
        {
            if (read == null || read.Result == null)
            {
                return OperationResult<PatientSaveReadDto>.Failure("수검자 저장 결과를 읽지 못했습니다.");
            }

            return OperationResult<PatientSaveReadDto>.Success(read);
        }

        /// <summary>
        /// 05 §2.2 — 빈 문자열은 미입력이고 주민번호만 `-` 를 뗀다.
        /// **휴대전화·전화번호에서 `-` 를 떼는 것은 검색값뿐이다.** 저장값은 표시 그대로
        /// 들어가며 `VARCHAR(13)` 이 하이픈까지 담는다.
        /// </summary>
        private static PatientSaveRequest NormalizeSave(PatientSaveRequest r)
        {
            return new PatientSaveRequest
            {
                ChartNo = Trim(r.ChartNo),
                Name = Trim(r.Name),
                SocialNumber = clsPatientText.Digits(r.SocialNumber),
                // [2026-09-10 사용자 결정] 전화 두 칸은 `-` 를 넣은 꼴로 저장한다.
                // DB 는 그대로다 — CK_수검자_CEL_DIGIT 가 `-` 를 벗기고 검사하고,
                // 조회도 REPLACE 라 옵션 없이 맞는다 (05 §7.2). 화면 표기와 저장값이 같아진다.
                MobilePhone = clsPatientText.FormatPhone(r.MobilePhone),
                Phone = clsPatientText.FormatPhone(r.Phone),
                Email = Trim(r.Email),
                Zipcode = Trim(r.Zipcode),
                Address = Trim(r.Address),
                AddressDetail = Trim(r.AddressDetail),
                Memo = Trim(r.Memo),
                HepatitisBExcluded = r.HepatitisBExcluded,
                OperatorName = Trim(r.OperatorName),
                AutoChartNo = r.AutoChartNo,
                SimilarConfirmed = r.SimilarConfirmed,
                PatientId = r.PatientId,
                RowVersion = r.RowVersion,
            };
        }

        // 05 §10.1 · §10.2 의 Parameter 크기. 비고는 NVARCHAR(MAX) 라 한도가 없다.
        private static string FirstTooLongSave(PatientSaveRequest r)
        {
            if (Over(r.ChartNo, ChartNoMax)) { return "차트번호는 " + ChartNoMax + "자 이하로 입력하십시오."; }
            if (Over(r.Name, NameMax)) { return "이름은 " + NameMax + "자 이하로 입력하십시오."; }
            if (r.SocialNumber != null && r.SocialNumber.Length != SocialNumberLength)
            {
                return "주민등록번호는 숫자 " + SocialNumberLength + "자리로 입력하십시오.";
            }
            if (Over(r.MobilePhone, MobilePhoneMax)) { return "휴대전화는 " + MobilePhoneMax + "자 이하로 입력하십시오."; }
            if (Over(r.Phone, PhoneMax)) { return "전화번호는 " + PhoneMax + "자 이하로 입력하십시오."; }

            // 04 §8.1.3 CK_수검자_CEL_DIGIT 이 휴대전화를 숫자 10~11자리로 못박는다.
            // 화면도 같은 것을 안내하지만(FrmPatientEditor.Phone_Leave) **판정은 여기서 한 번** 한다 —
            // 킷 §6. 여기가 없으면 화면을 우회한 값이 DB 제약 위반으로 튕겨 이유가 안 보인다.
            if (!clsPatientText.IsPhoneDigitCountValid(r.MobilePhone, true))
            {
                return "휴대전화는 숫자 10~11자리로 입력하십시오.";
            }

            if (!clsPatientText.IsPhoneDigitCountValid(r.Phone, false))
            {
                return "전화번호는 숫자 8~11자리로 입력하십시오.";
            }
            if (Over(r.Email, EmailMax)) { return "E-mail 은 " + EmailMax + "자 이하로 입력하십시오."; }
            if (Over(r.Zipcode, ZipcodeMax)) { return "우편번호는 " + ZipcodeMax + "자 이하로 입력하십시오."; }
            if (Over(r.Address, AddressMax)) { return "주소는 " + AddressMax + "자 이하로 입력하십시오."; }
            if (Over(r.AddressDetail, AddressMax)) { return "상세주소는 " + AddressMax + "자 이하로 입력하십시오."; }
            if (Over(r.OperatorName, OperatorNameMax)) { return "조작자명은 " + OperatorNameMax + "자 이하로 입력하십시오."; }
            return null;
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
    }
}
