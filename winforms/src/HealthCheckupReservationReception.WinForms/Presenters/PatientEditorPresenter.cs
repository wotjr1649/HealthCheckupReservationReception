// 화면 ID: DLG-PAT-01 — 수검자 등록·정보수정 (03 §6)
using System;
using System.Collections.Generic;
using System.Globalization;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-PAT-01 수검자 등록·정보수정 (03 §6).
    ///
    /// 여기가 판정하는 것은 **비어 있음과 주민번호 파생** 둘뿐이다 (07 §7). 길이는 Service 가,
    /// 고유성·후보·동시성은 DB 가 판정한다. 화면 검증이 통과했다고 저장이 보장되지 않는다.
    /// </summary>
    public sealed class PatientEditorPresenter
    {
        private readonly IPatientEditorView _view;
        private readonly IPatientService _service;
        private readonly string _operatorName;

        /// <summary>null 이면 New 다 (03 §6.3). 값이 있으면 Edit 다 (§6.4).</summary>
        private readonly long? _patientId;

        // 03 §16 · 05 §16.3 — Modal 진입 때 받은 원본 동시성값을 숨은 값으로 들고 있는다.
        private byte[] _rowVersion;

        // 왜 파생이 실패했는지. 화면이 그 자리에 그대로 적는다. 성공했으면 null 이다.
        private string _socialHint;

        // 주민번호에서 산출한 값. 실패하면 둘 다 null 이고 저장이 막힌다 (03 §6.2).
        private string _birthday;
        private string _gender;

        // 03 §6.5 — `별도 수검자로 계속` 이 유효한 조합. 셋 중 하나가 바뀌면 되돌린다.
        private string _confirmedKey;

        public PatientEditorPresenter(
            IPatientEditorView view, IPatientService service, string operatorName, long? patientId)
        {
            _view = view;
            _service = service;
            _operatorName = operatorName;
            _patientId = patientId;

            _view.ViewLoaded += OnViewLoaded;
            _view.InputChanged += OnInputChanged;
            _view.SaveRequested += OnSaveRequested;

            _view.SaveEnabled = false;
        }

        private void OnViewLoaded(object sender, EventArgs e)
        {
            if (_patientId == null)
            {
                Refresh();
                return;
            }

            // 07 §3.3 — Edit Mode 진입값은 SP-PAT-02 가 준다.
            _view.ShowEditMode();

            OperationResult<PatientDetailDto> result;
            try
            {
                result = _service.GetDetail(_patientId.Value);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.ShowMessage("수검자 상세를 조회하지 못했습니다.");
                _view.CloseWith(null, null);
                return;
            }

            if (!result.IsSuccess)
            {
                _view.ShowMessage(result.Message);
                _view.CloseWith(null, null);
                return;
            }

            _rowVersion = result.Value.RowVersion;
            _view.LoadDetail(result.Value);
            Refresh();
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            Derive();

            // 03 §6.5 — 확인값은 성명 + 산출 생년월일 + 주민번호 조합 하나에만 유효하다.
            if (_confirmedKey != null && _confirmedKey != CurrentKey())
            {
                _confirmedKey = null;
            }

            // 03 §6.3 EP-04 · §6.2 — 이름이 비었거나 파생이 실패하면 저장할 수 없다.
            _view.SaveEnabled = !string.IsNullOrWhiteSpace(_view.Name) && _birthday != null;

            // [!] **다 친 값이 틀렸을 때만 알린다.** 03 §6.2 는 Clear + 저장 차단까지만 요구하는데,
            //     그러면 화면이 왜 비었는지 말해 주지 않는다 (사용자 지적 2026-09-10).
            //     자리가 덜 찬 동안에는 아무 말도 하지 않는다 — 타이핑 중에 빨간 글씨를 띄우면
            //     그것이 오히려 잘못 친 것처럼 읽힌다.
            _view.ShowFieldHint(PatientErrorField.SocialNumber, _socialHint ?? string.Empty);
        }

        /// <summary>
        /// 03 §6.2 · 05 §10.1 — 하이픈을 떼고 숫자 13자리를 확인한 뒤, 앞 6자리 날짜와
        /// 7번째 자리로 생년월일·성별을 산출한다. 형식·날짜·파생 어느 하나가 실패하면 둘 다
        /// Clear 한다.
        ///
        /// 산출값은 **DB 로 보내지 않는다** — 계산열이 유도한 값이 유일한 기준이다 (05 §10.1).
        /// </summary>
        private void Derive()
        {
            _birthday = null;
            _gender = null;
            _socialHint = null;

            string digits = clsPatientText.Digits(_view.SocialNumber);
            if (digits != null && digits.Length == 13)
            {
                Century century = CenturyOf(digits.Substring(6, 1));
                if (century == null)
                {
                    // 03 §6.2 표는 1~8 만 정의한다. 9·0(1800년대)과 그 밖의 값은 여기서 걸린다.
                    _socialHint = "뒷자리 첫 숫자는 1~8 이어야 합니다"
                        + " (1·2 = 1900년대, 3·4 = 2000년대, 5~8 = 외국인).";
                }
                else
                {
                    string text = century.Year.ToString(CultureInfo.InvariantCulture).Substring(0, 2)
                        + digits.Substring(0, 6);
                    DateTime born;
                    if (DateTime.TryParseExact(
                            text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out born))
                    {
                        _birthday = text;
                        _gender = century.Gender;
                    }
                    else
                    {
                        _socialHint = "앞 여섯 자리가 실제 날짜가 아닙니다.";
                    }
                }
            }

            _view.Birthday = _birthday == null ? string.Empty : clsPatientText.FormatBirthday(_birthday);
            _view.Gender = _gender == null ? string.Empty : clsPatientText.FormatGender(_gender);
        }

        /// <summary>
        /// 주민등록번호 7번째 자리 (03 §6.2 · 05 §10.1). 9 와 0 은 1800년대생이라 저장하지 않는다.
        ///
        /// **표의 출처는 03 §6.2 이고 여기는 그것을 옮긴 것이다.**
        /// `scripts/verify-social-century.sh` 가 아래 네 줄과 03 §6.2 를 대조한다 (ROOT AGENTS.md §6).
        /// </summary>
        private static Century CenturyOf(string code)
        {
            switch (code)
            {
                case "1": case "5": return new Century(1900, "M");
                case "2": case "6": return new Century(1900, "F");
                case "3": case "7": return new Century(2000, "M");
                case "4": case "8": return new Century(2000, "F");
                default: return null;
            }
        }

        private void OnSaveRequested(object sender, EventArgs e)
        {
            _view.ClearFieldErrors();

            // [X] 저장은 지금 화면에 있는 값으로 판정한다. 파생값을 InputChanged 가 남겨 준
            //     것에 기대면 이벤트 하나를 놓친 순간 낡은 값으로 저장하게 된다.
            Refresh();

            // 킷 §6 · 07 §7 — 비어 있음은 여기서 한 번만 판정한다 (03 §6.3 EP-04).
            if (string.IsNullOrWhiteSpace(_view.Name))
            {
                _view.ShowFieldError(PatientErrorField.Name, "이름을 입력하십시오.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_view.SocialNumber))
            {
                _view.ShowFieldError(PatientErrorField.SocialNumber, "주민등록번호를 입력하십시오.");
                return;
            }

            // 03 §6.2 — 형식·날짜·파생이 실패한 값으로는 저장하지 않는다.
            if (_birthday == null)
            {
                _view.ShowFieldError(PatientErrorField.SocialNumber, "주민등록번호를 다시 확인하십시오.");
                return;
            }

            // 03 §6.3 — 수동입력이면 차트번호가 필수다. Edit 는 언제나 수동이다 (§6.4).
            if (!_view.AutoChartNo && string.IsNullOrWhiteSpace(_view.ChartNo))
            {
                _view.ShowFieldError(PatientErrorField.ChartNo, "차트번호를 입력하십시오.");
                return;
            }

            Save();
        }

        private void Save()
        {
            PatientSaveRequest request = BuildRequest();

            OperationResult<PatientSaveReadDto> result;
            try
            {
                result = _patientId == null ? _service.Register(request) : _service.Update(request);
            }
            catch (Exception)
            {
                _view.ShowMessage("수검자를 저장하지 못했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                _view.ShowMessage(result.Message);
                return;
            }

            Apply(result.Value);
        }

        private PatientSaveRequest BuildRequest()
        {
            return new PatientSaveRequest
            {
                PatientId = _patientId ?? 0,
                RowVersion = _rowVersion,
                AutoChartNo = _patientId == null && _view.AutoChartNo,
                ChartNo = _view.ChartNo,
                Name = _view.Name,
                SocialNumber = _view.SocialNumber,
                MobilePhone = _view.MobilePhone,
                Phone = _view.Phone,
                Email = _view.Email,
                Zipcode = _view.Zipcode,
                Address = _view.Address,
                AddressDetail = _view.AddressDetail,
                Memo = _view.Memo,
                HepatitisBExcluded = _view.HepatitisBExcluded,
                SimilarConfirmed = _confirmedKey != null && _confirmedKey == CurrentKey(),
                OperatorName = _operatorName,
            };
        }

        /// <summary>
        /// 07 §6.1 의 ResultCode ↔ 표현 대응. 분기는 숫자로만 한다 (05 §4.3 · §3.6).
        /// **처리하지 않은 코드는 공통 경로로 떨어뜨린다** — 계약에 없는 코드가 오면 그것이
        /// 곧 계약 위반이므로 조용히 성공으로 읽지 않는다 (07 §6.2).
        /// </summary>
        private void Apply(PatientSaveReadDto read)
        {
            DbResult result = read.Result;
            PatientSaveResultDto first = read.Rows != null && read.Rows.Count > 0 ? read.Rows[0] : null;

            switch ((DbCode)result.Code)
            {
                case DbCode.Ok:
                case DbCode.NoChange:
                    Finish(first, null);
                    return;

                // 03 §6.3 — 동일 주민번호는 신규 등록 없이 기존 PatientId 를 돌려준다.
                case DbCode.ExistingPatient:
                    Finish(first, result.Message);
                    return;

                // 03 §6.3 · §15.2 — 이름이 달라도 기존 전체값을 확인한 뒤 기존 PatientId 다.
                case DbCode.SameNumberDifferentName:
                    if (first == null)
                    {
                        _view.ShowMessage(result.Message);
                        return;
                    }

                    if (_view.ConfirmExistingPatient(first))
                    {
                        _view.CloseWith(first.PatientId, first.ChartNo);
                    }

                    return;

                case DbCode.SimilarPatient:
                    OnSimilarPatient(read.Rows);
                    return;

                // 05 §16.3 — 최신 상세를 다시 읽되 사용자 입력을 자동으로 덮어쓰지 않는다.
                case DbCode.RowChanged:
                    _view.ShowMessage(result.Message);
                    RefreshRowVersion();
                    return;

                // 03 §15.1 Inline. `오류항목` 이 어느 입력항목인지 가리킨다 (05 §16.2).
                case DbCode.MissingValue:
                case DbCode.BadValue:
                case DbCode.BadRequest:
                    _view.ShowFieldError(result.Field, result.Message);
                    return;

                default:
                    _view.ShowMessage(result.Message);
                    return;
            }
        }

        private void Finish(PatientSaveResultDto saved, string notice)
        {
            if (saved == null)
            {
                // 05 §10.1 · §10.2 는 성공에 RS1 1행을 약속한다. 없으면 계약 위반이다.
                _view.ShowMessage("저장 결과를 읽지 못했습니다.");
                return;
            }

            if (notice != null)
            {
                _view.ShowMessage(notice);
            }

            _view.CloseWith(saved.PatientId, saved.ChartNo);
        }

        private void OnSimilarPatient(IList<PatientSaveResultDto> candidates)
        {
            DuplicateChoice choice = _view.ShowDuplicateCandidates(CurrentInput(), candidates);
            if (choice != DuplicateChoice.Continue)
            {
                // [입력값 수정]·[닫기] 는 Editor 로 돌아간다 (03 §6.5).
                return;
            }

            // 07 §3.3 — 확인값을 실어 SP-PAT-03 을 다시 부른다. 같은 조합이면 창이 되풀이되지 않는다.
            _confirmedKey = CurrentKey();
            Save();
        }

        private void RefreshRowVersion()
        {
            if (_patientId == null)
            {
                return;
            }

            OperationResult<PatientDetailDto> latest;
            try
            {
                latest = _service.GetDetail(_patientId.Value);
            }
            catch (Exception)
            {
                return;
            }

            // 03 §16 — 저장 실패 후 기존 행버전으로 재시도하지 않는다.
            if (latest.IsSuccess && latest.Value != null)
            {
                _rowVersion = latest.Value.RowVersion;
            }
        }

        /// <summary>DLG-PAT-03 왼쪽 `입력값` 구획 (03 §6.5). 후보 행과 같은 꼴로 넘긴다.</summary>
        private PatientSaveResultDto CurrentInput()
        {
            return new PatientSaveResultDto
            {
                Name = _view.Name,
                Birthday = _birthday,
                Gender = _gender,
                SocialNumber = clsPatientText.Digits(_view.SocialNumber),
                MobilePhone = _view.MobilePhone,
            };
        }

        private string CurrentKey()
        {
            return (_view.Name ?? string.Empty).Trim()
                + "|" + (_birthday ?? string.Empty)
                + "|" + (clsPatientText.Digits(_view.SocialNumber) ?? string.Empty);
        }

        private sealed class Century
        {
            public Century(int year, string gender)
            {
                Year = year;
                Gender = gender;
            }

            public int Year { get; private set; }

            public string Gender { get; private set; }
        }
    }
}
