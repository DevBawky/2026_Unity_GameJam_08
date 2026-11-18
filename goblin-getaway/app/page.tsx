import GameRuntime from "./game-runtime";
import { ArrowLeft, BookOpen, ChartNoAxesColumn, ChevronRight, Crosshair, Flag, Gauge, Maximize, Move, Play, Settings, Shield, Sparkles, Users, Volume2, X } from 'lucide-react';

function JobPanel({slot}:{slot:string}){
 return <article className="job-panel" id={'job-'+slot} hidden>
   <div className="work-dial" aria-label="원형 타이밍 게이지"><svg viewBox="0 0 100 100" aria-hidden="true">
    <circle cx="50" cy="50" r="42" fill="none" stroke="#53605a" strokeWidth="8"/>
    <circle className="work-arc" cx="50" cy="50" r="42" fill="none" stroke="var(--work)" strokeWidth="8" pathLength="100"/>
    <g className="work-hand"><path d="m50 0 5 9-5 7-5-7z" stroke="#202923" fill="#fffbed" strokeWidth="1.4"/></g></svg>
    <span className="dial-word">대기</span></div>
   <div className="work-copy"><div className="work-meta"><b className="work-role"/><span className="work-zoom">4×</span></div><h3 className="work-title"/><div className="work-detail"/><small className="work-step"/></div>
   <div className="work-input"><button type="button" className="work-do" data-work={slot}>행동</button><span className="work-help"/></div>
 </article>;
}
function CrewMember({id}:{id:string}){
 return <article className={'member '+id}><span className="member-id">{id.toUpperCase()}</span><div><strong id={id+'-crew-name'}>{id==='p1'?'내 졸개':'자동 동료'}</strong><p id={id+'-crew-job'}>출발 대기</p></div></article>;
}
function PadPair({id}:{id:string}){
 return <div className="pad-pair" id={'pads-'+id} hidden={id==='p2'}>
   <div className="pad-wrap"><button className="stick move-stick" data-pad={id+'-move'} aria-label={id.toUpperCase()+' 이동 스틱'}><i><Move/></i></button><span>이동</span></div>
   <div className="context"><div><b className="crew-id" id={id+'-ident'}>{id.toUpperCase()}</b><strong id={id+'-location'}>갑판</strong></div><small id={id+'-hint'}>고삐나 대포 작업대로 이동</small></div>
   <div className="pad-wrap"><button id={id+'-ignite'} className="ignite" data-work={id} hidden>점화</button><span id={id+'-aim-label'}>방향 · 행동</span><button className="stick aim-stick" data-pad={id+'-aim'} aria-label={id.toUpperCase()+' 방향 스틱, 놓으면 행동'}><i><Crosshair/></i></button></div>
 </div>;
}
function SettingsMenu(){
 return <section id="settings-menu" hidden>
   <h2 className="modal-title">환경설정</h2>
   <div className="menu-list">
     <button id="expand"><Maximize/><span>전체화면</span><small id="full-state">켜기</small></button>
     <button id="sound"><Volume2/><span>효과음</span><small id="sound-state">꺼짐</small></button>
     <button data-view="crew"><Users/><span>협동 플레이</span><ChevronRight/></button>
     <button data-view="status"><ChartNoAxesColumn/><span>작전 현황 · 탑승원 배치</span><ChevronRight/></button>
     <button data-view="guide"><BookOpen/><span>조작 가이드</span><ChevronRight/></button>
     <button data-view="traits"><Sparkles/><span>정비 카드 도감</span><ChevronRight/></button>
   </div>
 </section>;
}
function StatusPanel(){
 return <section id="status-panel" hidden>
   <h2 className="modal-title">작전 현황</h2>
   <div className="stage-summary"><span id="chapter-no">챕터 1</span><strong id="chapter-name">왕국 국경 탈출</strong><span id="mode-label">1인 연습 모드</span></div>
   <div className="telemetry"><div className="metric-heading"><span id="stage-no">스테이지 1</span><strong id="stage-name">가시숲 고갯길</strong></div>
   <div className="metric-heading"><span id="wave-no">웨이브 0 / 5</span><strong id="next-wave">다음 0:04</strong></div>
   <div className="distance"><div className="metric-heading"><Flag/><span>남은 거리</span><strong id="dist">1,000 m</strong></div><div className="dist-line"><span id="bar"/></div><small id="stage-clock" className="stage-clock">주행 0 / 1,000 m · 0:00 경과</small></div>
   <div className="metric-heading"><Shield/><span>마차 내구도</span><strong id="hp">100 / 100</strong></div>
   <div className="metric-heading"><Sparkles/><span>애정 포인트</span><strong id="affection-readout">0 AP</strong></div>
   <div className="metric-heading"><Gauge/><span>이동 속도</span><strong id="speed">0 m/s</strong></div></div>
   <h3>탑승원 배치</h3><div className="members">{['p1','p2','p3','p4'].map(id=><CrewMember id={id} key={id}/>)}</div>
   <div className="cargo-readout"><span id="ammo-readout"/><span id="royal-readout"/><span id="wheel-readout"/></div>
   <small className="game-version">그건 님이!!! · 프로토타입 0.7</small>
 </section>;
}
export default function Home(){return <>
<main className="desk" id="desk" aria-label="그건 님이!!! 게임">
 <section className="monitor" id="monitor" aria-label="마차 탈출 작전 게임 화면">
  <div className="world"><canvas id="world" aria-label="왼쪽을 향해 달리는 마차와 뒤에서 쫓아오는 왕국군"/></div>
  <div className="play-tools">
   <button id="start" className="primary"><Play size={17} fill="currentColor"/><span>작전 시작</span></button>
   <button id="settings-button" className="gear" aria-label="환경설정" title="환경설정" aria-haspopup="dialog"><Settings size={20}/></button>
  </div>
  <div className="play-info">
   <div className="campaign-hud" id="campaign-hud" role="group" aria-label="작전 진행" hidden>
    <div className="campaign-main"><span id="campaign-path"/><strong id="remaining-distance"/></div>
    <div className="campaign-waves"><span id="campaign-wave"/><span id="campaign-next-wave"/></div>
   </div>
   <div className="event" id="event" role="status"></div>
  </div>
  <div className="controls" aria-label="트윈스틱 조작"><PadPair id="p1"/><PadPair id="p2"/></div>
  <div className="work-panels" id="work-panels" hidden><JobPanel slot="p1"/><JobPanel slot="p2"/></div>
 </section>
</main>
<dialog id="dialog" aria-label="게임 메뉴"><div className="dialog-head">
 <button id="back-settings" aria-label="환경설정으로 돌아가기" hidden><ArrowLeft size={16}/>환경설정</button>
 <button id="close-dialog" aria-label="닫기"><X size={20}/></button></div>
 <SettingsMenu/><StatusPanel/><div id="dialog-body"/></dialog><div id="toast" role="status" aria-live="polite"/><GameRuntime/>
</>}
