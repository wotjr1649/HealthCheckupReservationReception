'use strict';
// 화면 배치 ↔ 설계 소스 대조 (07 §10 P11 계열).
//
// 배치의 단일 출처는 tools/docgen/wireframe/screens/*.js 다. 그 파일들이 화면설계서를
// 그리므로, 구현이 설계와 다르면 여기서 갈린다. 산출된 pptx 를 역추출하지 않는다 —
// 생성기를 그대로 돌려 kit 함수의 인자를 가로챈다.
//
//   SCR-000  설계를 못 읽으면 통과가 아니라 FAIL
//   SCR-001  screens/*.js 의 화면 ID 전건 ↔ 03 §2 양방향 차집합 0
//   SCR-002  kit.js 의 NAV 전건·순서 ↔ MainForm 의 RibbonPage 캡션 순서
//   SCR-003  화면마다 K.shell 의 Ribbon 그룹명·버튼 ↔ Ribbon.Pages[navActive]
//   SCR-004  화면마다 설계의 라벨·캡션·헤더 ⊆ 그 화면 ID 를 단 C# 의 문자열
//
// [X] 샘플 데이터는 계약이 아니다. K.grid 의 행과 K.field 의 값(홍길동·2026-000123)은
//     걷어내고 헤더·라벨만 본다. 낱말을 통째로 긁으면 그 값들이 전부 요구사항이 된다.
// [X] 콜아웃 번호(markLeft)와 설명 문구는 UI 가 아니다. kit 함수를 거치지 않으므로
//     의미 기반 추출이 자동으로 뺀다. 예외는 `[ 기본정보 ]` 꼴 구획 머리뿐이다.
const fs = require('fs');
const path = require('path');

const HERE = __dirname;
const WF_DIR = process.env.WF_DIR || path.resolve(HERE, '../../tools/docgen/wireframe');
const SRC_DIR = process.env.SRC_DIR || path.resolve(HERE, '../src/HealthCheckupReservationReception.WinForms');
const DOC03 = process.env.DOC03 || path.resolve(HERE, '../../docs/baseline/03_Wireframe_Definition.md');

let FAIL = 0;
const say = (ok, msg) => { console.log((ok ? 'PASS ' : 'FAIL ') + msg); if (!ok) FAIL = 1; };
const norm = s => String(s).replace(/\s+/g, ' ').trim();
const diff = (a, b) => a.filter(x => !b.includes(x));

// ── 설계 읽기 ────────────────────────────────────────────────────────
// 파일명 wf_pat_01 → 화면 ID WF-PAT-01. 화면이 아닌 페이지(a0_list·z_*)는 접두사로 걸러진다.
const idOf = file => file.replace(/\.js$/, '').toUpperCase().replace(/_/g, '-');
const isScreen = file => /^(wf|dlg|cnf)_/.test(file);

function readDesign() {
  const K = require(path.join(WF_DIR, 'kit'));
  const { Canvas } = require(path.join(WF_DIR, 'canvas'));
  const screens = [];
  let cur = null;

  const add = v => { if (cur && v != null && String(v).trim()) cur.labels.add(norm(v)); };

  // kit 함수를 가로챈다. screens/*.js 는 require('../kit') 로 같은 객체를 보므로
  // 여기서 바꾼 것이 그대로 보인다.
  const orig = {};
  for (const fn of ['shell', 'modal', 'confirm', 'searchBand', 'panel', 'field', 'grid']) orig[fn] = K[fn];

  const asShell = (o, drawn) => ({
    navActive: o.navActive, drawn,
    groups: (o.ribbon || []).map(g => ({ name: g.name, buttons: (g.buttons || []).map(b => b.t) })),
    tabs: (o.tabs || []).slice(), status: o.status,
  });
  K.shell = function (c, o) {
    if (cur) cur.shells.push(asShell(o, true));
    return orig.shell.apply(this, arguments);
  };
  K.modal = function (c, o) {
    add(o.title); (o.buttons || []).forEach(b => add(b.t ?? b));
    return orig.modal.apply(this, arguments);
  };
  K.confirm = function (c, x, y, w, h, o) {
    add(o.title); (o.lines || []).forEach(add); (o.buttons || []).forEach(b => add(b.t ?? b));
    return orig.confirm.apply(this, arguments);
  };
  K.searchBand = function (c, x, y, w, rows, o = {}) {
    add(o.caption ?? '조회조건');
    (o.buttons || []).forEach(add);
    (rows || []).forEach(r => r.forEach(f => add(f.label)));   // 값은 예시라 담지 않는다
    return orig.searchBand.apply(this, arguments);
  };
  K.panel = function (c, x, y, w, h, caption) { add(caption); return orig.panel.apply(this, arguments); };
  K.field = function (c, x, y, lw, bw, label) { add(label); return orig.field.apply(this, arguments); };
  K.grid = function (c, x, y, cols) { (cols || []).forEach(col => add(col.t)); return orig.grid.apply(this, arguments); };

  // `[ 기본정보 ]` 꼴 구획 머리만 원시 텍스트에서 줍는다. 나머지 c.text 는 설명 문구다.
  const origText = Canvas.prototype.text;
  Canvas.prototype.text = function (x, y, w, h, str) {
    if (typeof str === 'string' && /^\[\s*.+\s*\]$/.test(str.trim())) add(str);
    return origText.apply(this, arguments);
  };

  const dir = path.join(WF_DIR, 'screens');
  for (const file of fs.readdirSync(dir).filter(f => f.endsWith('.js') && isScreen(f)).sort()) {
    cur = { id: idOf(file), file, shells: [], labels: new Set() };
    const mod = require(path.join(dir, file));
    for (const page of (Array.isArray(mod.pages) ? mod.pages : [mod])) page.draw(new Canvas());
    // 그리지 않는 Context 선언. 도형을 만들지 않으므로 draw 로는 잡히지 않는다.
    for (const alt of (mod.altShells || [])) cur.shells.push(asShell(alt, false));
    screens.push(cur);
    cur = null;
  }

  Canvas.prototype.text = origText;
  for (const fn of Object.keys(orig)) K[fn] = orig[fn];
  return { screens, NAV: K.NAV.slice() };
}

// ── C# 읽기 ─────────────────────────────────────────────────────────
function walkCs(dir, out = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    if (e.name === 'obj' || e.name === 'bin') continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walkCs(p, out);
    else if (e.name.endsWith('.cs')) out.push(p);
  }
  return out;
}

function readCsharp() {
  const byId = new Map();          // 화면 ID → { files:[], strings:Set }
  let ribbonSrc = null;
  for (const f of walkCs(SRC_DIR)) {
    const src = fs.readFileSync(f, 'utf8');
    const m = src.match(/\/\/\s*화면\s*ID\s*:\s*([A-Z]{2,3}-[A-Z0-9-]+)/);
    if (!m) continue;
    const id = m[1];
    if (!byId.has(id)) byId.set(id, { files: [], strings: new Set() });
    const e = byId.get(id);
    e.files.push(path.relative(SRC_DIR, f));
    for (const s of src.matchAll(/"((?:[^"\\]|\\.)*)"/g)) {
      const v = norm(s[1]);
      if (v) e.strings.add(v);
    }
    if (/RibbonControl\(\)/.test(src)) ribbonSrc = src;
  }
  return { byId, ribbonSrc };
}

// Designer 의 Ribbon 구조를 읽는다. 디자이너가 내는 형태라 모양이 고정돼 있다.
function parseRibbon(src) {
  if (!src) return null;
  const text = new Map();
  for (const m of src.matchAll(/this\.(\w+)\.(?:Text|Caption)\s*=\s*"((?:[^"\\]|\\.)*)"/g)) text.set(m[1], norm(m[2]));

  const names = block => block.split(',').map(s => s.trim().replace(/^this\./, '')).filter(Boolean);
  const pm = src.match(/\.Pages\.AddRange\(new DevExpress\.XtraBars\.Ribbon\.RibbonPage\[\]\s*\{([\s\S]*?)\}\)/);
  if (!pm) return null;

  return names(pm[1]).map(page => {
    const gm = src.match(new RegExp(`this\\.${page}\\.Groups\\.AddRange\\(new [^{]*\\{([\\s\\S]*?)\\}\\)`));
    const groups = (gm ? names(gm[1]) : []).map(g => ({
      name: text.get(g) ?? g,
      buttons: [...src.matchAll(new RegExp(`this\\.${g}\\.ItemLinks\\.Add\\(this\\.(\\w+)\\);`, 'g'))]
        .map(x => text.get(x[1]) ?? x[1]),
    }));
    return { field: page, name: text.get(page) ?? page, groups };
  });
}

// ── 03 §2 화면 목록 ──────────────────────────────────────────────────
function readDoc03Ids() {
  if (!fs.existsSync(DOC03)) return [];
  const body = fs.readFileSync(DOC03, 'utf8').replace(/\r/g, '')
    .split('\n');
  const out = [];
  let inSec = false;
  for (const line of body) {
    if (/^# 2[.] /.test(line)) { inSec = true; continue; }
    if (inSec && /^# /.test(line)) break;
    if (!inSec) continue;
    const m = line.match(/^\|\s*((?:WF|DLG|CNF)-[A-Z0-9-]+)\s*\|/);
    if (m) out.push(m[1]);
  }
  return out.sort();
}

// ── 판정 ────────────────────────────────────────────────────────────
// wf_00 의 Ribbon 은 **셸 도해**라 축약돼 있다. 실측 두 가지:
//   그룹명  `현재 업무 Action` — 03 §4.2 가 쓰는 일반 슬롯 이름이지 업무 그룹명이 아니다
//   버튼    보기 그룹이 `[컬럼설정]` 뿐이다. 03 §9.6 은 `[변경이력] [컬럼설정]` 이다
// 계약은 03 이므로 wf_00 의 Ribbon 은 대조 대상에서 뺀다.
// [I] `예약 관리` Page 는 그림이 없다 — WF-WRK-01 은 Tab Caption 과 같은 Context 하나만
//     그린다(그 파일 머리말). 그래서 그 파일이 `altShells` 로 **선언만** 해 두었고
//     여기서 그림과 똑같이 대조한다. 산출물은 그대로다.
const SHELL_RIBBON_SKIP = new Set(['WF-00']);

function check() {
  let design;
  try {
    design = readDesign();
  } catch (e) {
    say(false, 'SCR-000 설계를 읽지 못했다 — ' + e.message);
    return;
  }
  if (!design.screens.length || !design.NAV.length) {
    say(false, 'SCR-000 설계에서 화면을 하나도 못 읽었다 (' + WF_DIR + ')');
    return;
  }
  say(true, `SCR-000 설계 화면 ${design.screens.length}건 · NAV ${design.NAV.length}건을 읽었다`);

  // SCR-001
  const docIds = readDoc03Ids();
  const designIds = design.screens.map(s => s.id).sort();
  if (!docIds.length) {
    say(false, 'SCR-001 03 §2 에서 화면 ID 를 못 읽었다 — 표 형식이 바뀌었다');
  } else {
    const a = diff(designIds, docIds), b = diff(docIds, designIds);
    if (!a.length && !b.length) say(true, `SCR-001 설계 화면 ID ↔ 03 §2 양방향 차집합 0 (${docIds.length}건)`);
    else {
      say(false, 'SCR-001 화면 ID 불일치');
      a.forEach(x => console.log('    설계에만: ' + x));
      b.forEach(x => console.log('    03 §2 에만: ' + x));
    }
  }

  const cs = readCsharp();
  const ribbon = parseRibbon(cs.ribbonSrc);

  // SCR-002
  if (!ribbon) {
    say(false, 'SCR-002 C# 에서 Ribbon Page 구성을 못 읽었다 — MainForm Designer 형태가 바뀌었다');
  } else {
    const got = ribbon.map(p => p.name);
    if (got.length === design.NAV.length && got.every((v, i) => v === design.NAV[i])) {
      say(true, `SCR-002 Navigation ${got.length}건이 kit.js NAV 와 순서까지 같다`);
    } else {
      say(false, 'SCR-002 Navigation 불일치');
      console.log('    설계 kit.NAV : ' + design.NAV.join(' | '));
      console.log('    C# RibbonPage: ' + got.join(' | '));
    }
  }

  // SCR-003
  let scr3 = true, scr3n = 0;
  const covered = new Set();
  for (const s of design.screens) {
    if (!ribbon || SHELL_RIBBON_SKIP.has(s.id)) continue;
    for (const sh of s.shells) {
    const page = ribbon[sh.navActive];
    if (!page) {
      say(false, `SCR-003 ${s.id} navActive=${sh.navActive} 에 해당하는 RibbonPage 가 없다`);
      scr3 = false; continue;
    }
    scr3n++; covered.add(sh.navActive);
    const wantBtn = sh.groups.flatMap(g => g.buttons).slice().sort();
    const gotBtn = page.groups.flatMap(g => g.buttons).sort();
    const label = `${s.id}${sh.drawn ? '' : '(선언)'} → ${page.name}`;
    const a = diff(wantBtn, gotBtn), b = diff(gotBtn, wantBtn);
    if (a.length || b.length) {
      say(false, `SCR-003 ${label} 버튼 불일치`);
      a.forEach(x => console.log('    설계에만: ' + x));
      b.forEach(x => console.log('    C# 에만: ' + x));
      scr3 = false;
    }
    const wantG = sh.groups.map(g => g.name), gotG = page.groups.map(g => g.name);
    if (wantG.length !== gotG.length || !wantG.every((v, i) => v === gotG[i])) {
      say(false, `SCR-003 ${label} 그룹명·순서 불일치`);
      console.log('    설계: ' + wantG.join(' → '));
      console.log('    C#  : ' + gotG.join(' → '));
      scr3 = false;
    }
    }
  }
  // [X] Page 하나라도 설계가 닿지 않으면 통과가 아니다. 예전에는 그 사실을 문구로만
  //     알렸고, 문구는 아무도 red 로 만들지 않는다.
  // [I] 그룹이 없는 Page 는 Modal 진입점이라 대조할 Ribbon 이 없다 — 휴무일 관리가 그것이다
  //     (03 §24.2). 존재와 순서는 SCR-002 가 지킨다. 인덱스를 박지 않고 성질로 가른다:
  //     그 Page 에 그룹이 생기는 순간 설계 근거를 요구한다.
  const missPages = ribbon
    ? ribbon.map((p, i) => i).filter(i => !covered.has(i) && ribbon[i].groups.length > 0)
    : [];
  if (missPages.length) {
    say(false, `SCR-003 설계가 닿지 않는 RibbonPage: ` +
      missPages.map(i => `[${i}] ${ribbon[i].name}`).join(', '));
    scr3 = false;
  }
  const modalPages = ribbon ? ribbon.filter(p => !p.groups.length).map(p => p.name) : [];
  if (scr3) say(true, `SCR-003 Ribbon 구성 ${scr3n}건이 설계와 같다 · 그룹 있는 Page 전건 피복` +
    (modalPages.length ? ` · Modal 진입점 ${modalPages.join(',')} 는 Ribbon 없음` : '') +
    ` · ${[...SHELL_RIBBON_SKIP].join(',')} 제외 (셸 도해라 Ribbon 이 축약돼 있다)`);

  // SCR-004
  let scr4 = true, done = 0, skipped = [];
  for (const s of design.screens) {
    const impl = cs.byId.get(s.id);
    if (!impl) { skipped.push(s.id); continue; }
    done++;
    const missing = [...s.labels].filter(l => !impl.strings.has(l));
    if (missing.length) {
      say(false, `SCR-004 ${s.id} 설계 라벨 ${missing.length}건이 C# 에 없다 (${impl.files.join(', ')})`);
      missing.forEach(x => console.log('    없음: ' + x));
      scr4 = false;
    }
  }
  if (scr4) say(true, `SCR-004 구현된 화면 ${done}건의 설계 라벨이 전부 C# 에 있다` +
    (skipped.length ? ` · 미구현 ${skipped.length}건 건너뜀 (${skipped.join(' ')})` : ''));
}

// ── selftest ────────────────────────────────────────────────────────
// [X] 실물 C# 을 사본으로 떠서 변조한다. 픽스처를 손으로 적으면 설계가 바뀔 때 픽스처도
//     같이 낡고, 그 낡음을 아무도 안 본다 (verify-winforms-unchanged.sh 의 같은 [X]).
// [I] 뒷정리는 만든 파일만 지우고 빈 디렉터리를 닫는다. 재귀 삭제를 쓰지 않는다.
function selftest() {
  const { execFileSync } = require('child_process');
  const os = require('os');
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'scrdesign-'));
  const made = [];
  const dirs = new Set();

  const copyTree = () => {
    for (const f of walkCs(SRC_DIR)) {
      const rel = path.relative(SRC_DIR, f);
      const dst = path.join(tmp, rel);
      fs.mkdirSync(path.dirname(dst), { recursive: true });
      let d = path.dirname(dst);
      while (d !== tmp) { dirs.add(d); d = path.dirname(d); }
      fs.copyFileSync(f, dst);
      made.push(dst);
    }
  };

  // [X] 자식에 경계를 둔다. 이 파일이 2026-09-09 에 세 번 멈춰 있었다 — 두 번은 이
  //     selftest 에서, 한 번은 그냥 `check` 에서. 원인은 아직 모른다(07 §12.5).
  //     **원인을 모르는 것과 무한히 매달리는 것은 다른 문제다.** stdin 을 물려주지 않고
  //     (핸들 상속 경로를 끊는다) 시간을 재서 FAIL 로 떨어뜨린다.
  //     바깥 경계는 scripts/test.sh 의 GATE_TIMEOUT 이 따로 친다.
  const CHILD_TIMEOUT_MS = 120000;
  let rc = 0;
  const run = (label, expect, env) => {
    let code = 0, out = '', timedOut = false;
    try {
      out = execFileSync(process.execPath, [__filename, 'check'], {
        env: Object.assign({}, process.env, env),
        encoding: 'utf8',
        stdio: ['ignore', 'pipe', 'pipe'],
        timeout: CHILD_TIMEOUT_MS,
        killSignal: 'SIGKILL',
      });
    } catch (e) {
      code = e.status;
      out = String(e.stdout || '');
      timedOut = e.killed || e.signal != null;
    }
    if (timedOut) {
      console.log(`FAIL SCR-SELFTEST ${label} — 자식이 ${CHILD_TIMEOUT_MS / 1000}초 안에 끝나지 않았다`);
      rc = 1;
      return;
    }
    if (code === expect) console.log(`PASS SCR-SELFTEST ${label}`);
    else {
      console.log(`FAIL SCR-SELFTEST ${label} (기대 exit ${expect}, 실제 ${code})`);
      out.split('\n').forEach(l => l && console.log('    ' + l));
      rc = 1;
    }
  };
  const tamper = (rel, from, to) => {
    const p = path.join(tmp, rel);
    const s = fs.readFileSync(p, 'utf8');
    if (!s.includes(from)) { console.log(`FAIL SCR-SELFTEST 변조 대상을 못 찾았다: ${from}`); rc = 1; return; }
    fs.writeFileSync(p, s.replace(from, to), 'utf8');
  };

  const DESIGNER = path.join('Views', 'MainForm.Designer.cs');
  copyTree();
  run('사본 그대로면 통과한다', 0, { SRC_DIR: tmp });

  tamper(DESIGNER, 'this.barPageRcpDesk.Text = "접수 관리";', 'this.barPageRcpDesk.Text = "접수데스크";');
  run('Navigation 캡션 변조를 잡는다', 1, { SRC_DIR: tmp });

  copyTree();
  tamper(DESIGNER, 'this.barBtnPatientNew.Caption = "신규등록";', 'this.barBtnPatientNew.Caption = "신규 등록";');
  run('Ribbon 버튼 캡션 변조를 잡는다', 1, { SRC_DIR: tmp });

  copyTree();
  tamper(DESIGNER, 'this.barGroupPatientWork.Text = "수검자";', 'this.barGroupPatientWork.Text = "수검자 업무";');
  run('Ribbon 그룹명 변조를 잡는다', 1, { SRC_DIR: tmp });

  copyTree();
  // 그룹이 생긴 Page 는 설계 근거를 요구한다 — 휴무일 Page 에 그룹을 하나 붙여 본다.
  tamper(DESIGNER, 'this.barPageHoliday.Name = "barPageHoliday";',
    'this.barPageHoliday.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] '
    + '{ this.barGroupRsvView }); this.barPageHoliday.Name = "barPageHoliday";');
  run('설계가 닿지 않는 Page 에 그룹이 생기면 잡는다', 1, { SRC_DIR: tmp });

  copyTree();
  run('03 을 못 읽으면 통과가 아니라 FAIL 이다', 1, { SRC_DIR: tmp, DOC03: path.join(tmp, '없는파일.md') });
  run('설계를 못 읽으면 통과가 아니라 FAIL 이다', 1, { SRC_DIR: tmp, WF_DIR: path.join(tmp, '없는디렉터리') });

  for (const f of made) { try { fs.unlinkSync(f); } catch (e) { /* 이미 없다 */ } }
  for (const d of [...dirs].sort((a, b) => b.length - a.length)) { try { fs.rmdirSync(d); } catch (e) { /* 비지 않았다 */ } }
  try { fs.rmdirSync(tmp); } catch (e) { /* 남은 것이 있으면 그대로 둔다 */ }
  process.exit(rc);
}

if (process.argv[2] === 'selftest') {
  selftest();
} else {
  check();
  console.log(FAIL ? '== FAIL ==' : '== PASS 배치 ↔ 설계 ==');
  process.exit(FAIL);
}
