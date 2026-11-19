import Image from 'next/image';
import GameRuntime from "./game-runtime";
import { ArrowLeft, BookOpen, ChartNoAxesColumn, ChevronRight, Crosshair, Flag, Gauge, LogOut, Maximize, Move, MoveVertical, Music, Play, Settings, Shield, Smile, Sparkles, Timer, TriangleAlert, Users, Volume2, X } from 'lucide-react';

function WorkNotice({slot}:{slot:string}){
 return <div className="work-notice hud-bubble hud-notice" id={'work-notice-'+slot} hidden>
  <span className="hud-notice-line">
   <b className="work-notice-player" hidden>{slot.toUpperCase()}</b>
   <span>
    <span className="work-hint-text" aria-live="polite"/>
    <small className="work-countdown" hidden/>
   </span>
  </span>
  <div className="work-progress" hidden>
   <span className="work-progress-name" hidden>조준률</span>
   <progress className="work-progress-bar" aria-label="작업 진행도" max={1} value={0}/>
  </div>
 </div>;
}
function RoyalWatch(){
 return <ul className="royal-watch" id="royal-watch" aria-label="가까운 아이 상태" role="list" hidden>
  {[{id:'prince',name:'왕자'},{id:'princess',name:'공주'}].map(({id,name})=><li key={id} data-royal={id} hidden>
   <Image className="royal-watch-face" src={'/pixel-art/royal-face-'+id+'.png'} width={31} height={26} alt="" loading="eager" unoptimized/>
   <div className="royal-watch-details">
    <div className="royal-watch-heading"><b>{name}</b><span className="royal-watch-players" hidden/></div>
    <div className="royal-watch-meters">
     <span>관심 필요 <strong className="royal-watch-attention"/></span>
     <span>배고픔 <strong className="royal-watch-hunger"/></span>
    </div>
    <small className="royal-watch-condition" hidden/>
   </div>
  </li>)}
 </ul>;
}
function TimingPrompt({slot}:{slot:string}){
 const id='timing-'+slot;
 return <section className="timing-prompt" id={id} role="group" aria-labelledby={id+'-label'} hidden>
   <svg className="timing-ring" viewBox="0 0 100 100" aria-hidden="true" focusable="false">
    <circle className="timing-track" cx="50" cy="50" r="43" fill="none"/>
    <circle className="timing-band" cx="50" cy="50" r="43" fill="none" pathLength="100" strokeDasharray="0 100"/>
    <g className="timing-needle"><path d="M50 1 56 8 50 18 44 8Z" strokeLinejoin="round"/></g>
   </svg>
   <strong className="timing-label hud-bubble" id={id+'-label'}/>
   <small className="timing-feedback" hidden/>
 </section>;
}
function CrewMember({id}:{id:string}){
 return <article className={'member '+id}><span className="member-id">{id.toUpperCase()}</span><div><strong id={id+'-crew-name'}>{id==='p1'?'내 졸개':'자동 동료'}</strong><p id={id+'-crew-job'}>출발 대기</p></div></article>;
}
function PlayerControls({id}:{id:string}){
 return <div className="pad-pair" id={'pads-'+id} role="group" aria-label={id.toUpperCase()+' 조작'} hidden={id==='p2'}>
   <div className="pad-wrap">
    <button type="button" className="stick" id={id+'-stick'} data-mode="move" aria-label={id.toUpperCase()+' 이동 스틱'}>
     <i aria-hidden="true"><Move className="move-glyph"/><MoveVertical className="steer-glyph"/><Crosshair className="aim-glyph"/></i>
    </button>
    <span id={id+'-stick-label'} aria-hidden="true">이동</span>
   </div>
   <div className="context"><div><b className="crew-id" id={id+'-ident'}>{id.toUpperCase()}</b><strong id={id+'-location'}>갑판</strong></div><small id={id+'-hint'}>갑판 위 작업 원으로 이동</small></div>
   <div className="station-buttons">
    <div className="emote-controls">
     <button type="button" className="emote-toggle" id={'emote-'+id} data-emote-toggle={id} aria-expanded="false" aria-controls={'emote-menu-'+id} aria-label="이모티콘">
      <Smile size={19} aria-hidden="true"/>이모티콘
     </button>
     <div id={'emote-menu-'+id} className="emote-menu" role="group" aria-label="이모티콘 선택" hidden>
      <button type="button" className="emote-close" aria-label="이모티콘 목록 닫기"><X size={16} aria-hidden="true"/>닫기</button>
     </div>
    </div>
    <button type="button" className="station-exit" id={id+'-exit'} data-exit={id} hidden><LogOut size={14} aria-hidden="true"/>이탈</button>
    <button type="button" className="station-action" id={id+'-action'} data-work={id} disabled>행동</button>
   </div>
 </div>;
}
function SettingsMenu(){
 return <section id="settings-menu" hidden>
   <h2 className="modal-title">환경설정</h2>
   <div className="menu-list">
     <button id="expand"><Maximize/><span>전체화면</span><small id="full-state">켜기</small></button>
     <button type="button" id="music" aria-pressed="true"><Music aria-hidden="true"/><span>배경음악</span><small id="music-state">켜짐</small></button>
     <button id="sound"><Volume2/><span>효과음</span><small id="sound-state">꺼짐</small></button>
     <button id="timing-sound" disabled><Timer/><span>타이밍 안내음</span><small id="timing-sound-state">꺼짐</small></button>
     <button data-view="crew"><Users/><span>협동 플레이</span><ChevronRight/></button>
     <button data-view="status"><ChartNoAxesColumn/><span>작전 현황 · 탑승원 배치</span><ChevronRight/></button>
     <button data-view="guide"><BookOpen/><span>조작 가이드</span><ChevronRight/></button>
     <button data-view="traits"><Sparkles/><span>정비 카드 도감</span><ChevronRight/></button>
     <button type="button" id="test-stop" hidden><LogOut aria-hidden="true"/><span>테스트 종료</span></button>
   </div>
 </section>;
}
function StatusPanel(){
 return <section id="status-panel" hidden>
   <h2 className="modal-title">작전 현황</h2>
   <div className="stage-summary"><span id="chapter-no">챕터 1</span><strong id="chapter-name">왕국 국경 탈출</strong><span id="mode-label">1인 연습 모드</span></div>
   <div className="telemetry"><div className="metric-heading"><span id="stage-no">스테이지 1</span><strong id="stage-name">가시숲 고갯길</strong></div>
   <div className="metric-heading"><span id="wave-no">웨이브 0 / 5</span></div>
   <div className="distance"><div className="metric-heading"><Flag/><span id="distance-label">남은 거리</span><strong id="dist">1,000 m</strong></div><div className="dist-line" id="status-distance-line"><span id="bar"/></div><small id="stage-clock" className="stage-clock">주행 0 / 1,000 m · 0:00 경과</small></div>
   <div className="metric-heading"><Shield/><span>마차 내구도</span><strong id="hp">100 / 100</strong></div>
   <div className="metric-heading"><Sparkles/><span>애정 포인트</span><strong id="affection-readout">0 AP</strong></div>
   <div className="metric-heading"><Gauge/><span>이동 속도</span><strong id="speed">0 m/s</strong></div></div>
   <h3>탑승원 배치</h3><div className="members">{['p1','p2','p3','p4'].map(id=><CrewMember id={id} key={id}/>)}</div>
   <div className="cargo-readout"><span id="ammo-readout"/><span id="wheel-readout"/></div>
   <small className="game-version">그건 님이!!! · 프로토타입 0.7</small>
 </section>;
}
export default function Home(){return <>
<main className="desk" id="desk" aria-label="그건 님이!!! 게임">
 <section className="monitor" id="monitor" aria-label="마차 탈출 작전 게임 화면">
  <div className="world"><canvas id="world" aria-label="왼쪽을 향해 달리는 마차와 뒤에서 쫓아오는 왕국군"/></div>
  <div className="play-tools">
   <button type="button" id="test-start" className="primary test-start">테스트 모드</button>
   <button id="start" className="primary" data-mode="ready"><Play size={17} fill="currentColor"/><span>작전 시작</span></button>
   <button id="settings-button" className="gear" aria-label="환경설정" title="환경설정" aria-haspopup="dialog"><Settings size={20}/></button>
  </div>
  <section className="crew-ranking" id="crew-ranking" aria-label="승무원 점수 순위" hidden>
   <ol className="crew-ranking-list" id="crew-ranking-list" role="list"/>
  </section>
  <div className="campaign-hud" id="campaign-hud" role="group" aria-label="작전 진행" hidden>
   <span id="test-mode-label" className="test-mode-label" hidden>테스트 모드</span>
   <div className="campaign-route" id="campaign-route">
    <Flag className="campaign-goal" aria-hidden="true" focusable="false"/>
    <progress className="campaign-progress" id="campaign-progress" max={100} value={100} aria-label="목적지까지 남은 거리"/>
    <svg className="campaign-wagon" viewBox="0 0 32 24" aria-hidden="true" focusable="false">
     <path d="M4 8l4-5h13l4 5M22 15h5l3-3" fill="none" stroke="#4c3526" strokeWidth="2" strokeLinejoin="round" strokeLinecap="round"/>
     <path d="M3 8h23l-3 10H6z" fill="#e8b473" stroke="#4c3526" strokeWidth="2" strokeLinejoin="round"/>
     <path d="M8 12h13" stroke="#8e5c38" strokeWidth="1.5" strokeLinecap="round"/>
     <circle cx="9" cy="19" r="3.4" fill="#6b503c" stroke="#3a2b26" strokeWidth="1.7"/>
     <circle cx="21" cy="19" r="3.4" fill="#6b503c" stroke="#3a2b26" strokeWidth="1.7"/>
    </svg>
   </div>
   <div className="campaign-health" id="campaign-health" hidden>
    <Shield className="campaign-health-icon" aria-hidden="true" focusable="false"/>
    <progress className="campaign-progress campaign-health-progress" id="campaign-health-progress" max={100} value={100} aria-label="마차 체력"/>
   </div>
   <RoyalWatch/>
  </div>
  <div id="top-notices" className="top-notices">
   <div id="emote-notices" className="emote-notices" role="log" aria-label="팀원 소통" aria-live="polite" aria-relevant="additions"/>
   <div id="work-notices" className="work-notices" role="group" aria-label="작업 안내">
    <WorkNotice slot="p1"/><WorkNotice slot="p2"/>
   </div>
  </div>
  <div className="timing-prompts">
   <TimingPrompt slot="p1"/><TimingPrompt slot="p2"/>
  </div>
  <div className="royal-cry-overlay" id="royal-cry-overlay" aria-hidden="true" hidden>
   {[0,1,2,3,4].map(index=><span key={index}/>)}
  </div>
  <div className="catapult-alerts" aria-hidden="true">
   {Array.from({length:6},(_,pane)=><div className="catapult-alert-pane" key={pane} hidden><TriangleAlert focusable="false"/></div>)}
  </div>
  <div className="controls" role="group" aria-label="가상 조이스틱과 행동 버튼"><PlayerControls id="p1"/><PlayerControls id="p2"/></div>
 </section>
</main>
<dialog id="dialog" aria-label="게임 메뉴"><div className="dialog-head">
 <button id="back-settings" aria-label="환경설정으로 돌아가기" hidden><ArrowLeft size={16}/>환경설정</button>
 <button id="close-dialog" aria-label="닫기"><X size={20}/></button></div>
 <SettingsMenu/><StatusPanel/><div id="dialog-body"/></dialog><div id="toast" role="status" aria-live="polite"/><GameRuntime/>
</>}
