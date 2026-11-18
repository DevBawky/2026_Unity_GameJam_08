import {isRoomNumber,isLegacyRoomCode} from './room-number.mjs';
import {chapterSpec,stageSpec,nextStagePosition} from './campaign.mjs';
import {REPORT_OPTIONS,UPGRADE_CARDS,submitReport,voteForCard,voteThreshold,maxDurability,cardCost,canBuyCard} from './intermission.mjs';
import {initial,begin,PLAYER_IDS,humanIds,setHumanSlots,step,advance,operate,setInput,stationAt,horseAim,royalTarget,ROLES,WHEELS,feedbackKey,hit,nextStage,restartStage,restartChapter,phase,width,target,catchPhase,FUSE_TICKS,HZ,STEP} from './sim.mjs?v=crew7';
import {cameraViews,workInfo} from './workstation.mjs?v=outline8';
import {render} from './scene.mjs?v=outline8';
import {createControls} from './controls.mjs?v=outline8';
const $=id=>document.getElementById(id), all=q=>Array.from(document.querySelectorAll(q));
const dlg=$('dialog'),body=$('dialog-body'),canvas=$('world');
let s=initial(),mode='practice',myPlayer='p1',room=null,screen='',syncBusy=false,actBusy=false,netFailed=0,lastSync=0,lastTick=performance.now(),accum=0,lastPaint=0,lastStatus='',lastSerial=0,autoPaused=false,terminalSeen='',toastTimer,ac=null;
let sound=false;try{sound=localStorage.getItem('gg-sound')==='on';}catch{}
const linkCode=new URL(location.href).searchParams.get('room')?.trim().toUpperCase()||'';
const invitation=isRoomNumber(linkCode)||isLegacyRoomCode(linkCode)?linkCode:'';
let joinAttempt=null;
function markup(el,html){if(el.dataset.value!==html){el.innerHTML=html;el.dataset.value=html;}}
function label(el,text){if(el.textContent!==text)el.textContent=text;}
function toast(msg){label($('toast'),msg);$('toast').classList.add('on');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').classList.remove('on'),2500);}
function tone(freq=350,duration=.08,type='triangle'){if(!sound)return;try{ac??=new AudioContext();if(ac.state!=='running')ac.resume().catch(()=>{});const osc=ac.createOscillator(),g=ac.createGain();osc.type=type;osc.frequency.setValueAtTime(freq,ac.currentTime);osc.frequency.exponentialRampToValueAtTime(Math.max(40,freq*.68),ac.currentTime+duration);g.gain.setValueAtTime(.045,ac.currentTime);g.gain.exponentialRampToValueAtTime(.001,ac.currentTime+duration);osc.connect(g);g.connect(ac.destination);osc.start();osc.stop(ac.currentTime+duration);}catch{}}
function soundUI(){const b=$('sound');b.ariaLabel=sound?'효과음 끄기':'효과음 켜기';b.setAttribute('aria-pressed',String(sound));label($('sound-state'),sound?'켜짐':'꺼짐');}
$('sound').onclick=()=>{sound=!sound;try{localStorage.setItem('gg-sound',sound?'on':'off');}catch{}soundUI();tone(500);};
soundUI();
let inputBusy=false,lastInputAt=0,lastInputKey='';
const pendingActs=[];
const inputKey=input=>[input.mx,input.my,input.ax,input.ay].map(v=>Math.round(v*30)).join('/');
const position=state=>({chapter:state.chapter??1,stage:state.stage});
const sameStage=(a,b)=>(a.chapter??1)===(b.chapter??1)&&a.stage===b.stage;
const views=()=>cameraViews(s,mode==='local',myPlayer,canvas.clientWidth,canvas.clientHeight);
const controls=createControls({
 isDuo:()=>mode==='local',active:()=>myPlayer,canPlay:()=>s.mode==='run'&&!dlg.open,
 getState:()=>s,getViews:views,canvas,onAct:useStation,onEscape:()=>openScreen('settings')
});
all('[data-work]').forEach(b=>b.onclick=()=>controls.action(b.dataset.work));
function openScreen(name){
 controls.clear();
 if(!dlg.open){if(mode!=='online'&&s.mode==='run'&&!['result'].includes(name)){s.mode='paused';autoPaused=true;}dlg.showModal();}
 screen=name;drawDialog();dlg.scrollTop=0;
}
function closeScreen(){if(dlg.open)dlg.close();}
dlg.addEventListener('close',()=>{screen='';if(autoPaused&&mode!=='online'&&s.mode==='paused')s.mode='run';autoPaused=false;lastTick=performance.now();});
$('close-dialog').onclick=closeScreen;dlg.addEventListener('click',e=>{if(e.target===dlg){const r=dlg.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)closeScreen();}});
function title(h,p){return '<h2 class="modal-title">'+h+'</h2><p class="intro">'+p+'</p>';}
function option(id,tag,name,desc,tail='›'){return '<button class="option" data-choice="'+id+'"><span class="token">'+tag+'</span><span><b>'+name+'</b><small>'+desc+'</small></span><span class="end">'+tail+'</span></button>';}
function row(h,p){return '<div class="guide-row"><b>'+h+'</b><p>'+p+'</p></div>';}
const escapeHtml=value=>String(value??'').replace(/[&<>"']/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
const durability=()=>Math.round(s.hp)+' / '+maxDurability(s);
const resultError='<p id="error" class="error" role="alert"></p>';
function upgradeGuide(){
 return UPGRADE_CARDS.map(card=>{
   const level=s.traits[card.id]??0;
   const owned=card.maxLevel?' · '+level+'/'+card.maxLevel+'단계':'';
   return '<div class="trait-item"><span class="token" aria-hidden="true">'+escapeHtml(card.icon||'◇')+'</span><span><b>'+escapeHtml(card.name)+'</b><small>'+escapeHtml(card.desc)+'</small></span><span class="end">'+cardCost(s,card.id)+' AP'+owned+'</span></div>';
 }).join('');
}
function careSummary(progress){
 const entries=progress.care.map(child=>{
   const metrics=child.seatedPercent==null||child.attention==null?'돌봄 기록 없음':'착석 '+child.seatedPercent+'% · 평균 관심 '+child.attention+'% · 복귀 '+child.reseats+'회';
   return '<li><b>'+escapeHtml(child.name)+'</b><span>'+metrics+'</span><strong>'+child.points+' AP</strong></li>';
 }).join('');
 return '<section class="intermission-section care-summary" id="royal-care" aria-labelledby="care-heading">'+
   '<div class="intermission-heading"><h3 id="care-heading">왕자·공주 돌봄 평가</h3><strong>+'+progress.earned+' 애정 포인트</strong></div>'+
   '<ul>'+entries+'</ul><p>착석 시간 60%, 평균 관심 40%를 반영합니다.</p></section>';
}
function reportPanel(progress,canLead){
 if(progress.report){
   const reported=REPORT_OPTIONS.find(option=>option.id===progress.report.id);
   return '<section id="king-report" class="king-report"><h3>마왕과의 교신</h3>'+
     (reported?'<small>'+escapeHtml(reported.label)+'</small>':'')+
     '<p>“'+escapeHtml(progress.report.reply)+'”</p></section>';
 }
 const ammo=s.ammo;
 const details={
   damage:'내구도 '+durability()+' · '+(s.wheel.broken>=0?'바퀴 수리 필요':'바퀴 4개 장착'),
   supplies:'남은 포탄 '+(ammo.stock+ammo.rack+Number(ammo.loaded))+'발 · 손실 '+ammo.lost+'발',
 };
 const choices=REPORT_OPTIONS.map(option=>
   '<button type="button" class="option report-choice" data-choice="report" data-report="'+escapeHtml(option.id)+'"><span><b>'+escapeHtml(option.label)+'</b><small>'+escapeHtml(details[option.id]??option.description)+'</small></span><span class="end">보고</span></button>'
 ).join('');
 return '<section id="king-report" class="king-report"><h3>마왕과의 교신</h3>'+
   '<p>“수고했다. 다음 길에 나서기 전에 지금 상황을 보고해라.”</p>'+
   (canLead?choices:'<small>방장이 보고할 내용을 고르고 있습니다.</small>')+'</section>';
}
function upgradeShop(progress,canLead,departureLabel){
 const humans=humanIds(s),threshold=voteThreshold(humans);
 const localVoters=mode==='local'?humans:[myPlayer];
 const votes=progress.votes;
 const cards=UPGRADE_CARDS.map(card=>{
   const cost=cardCost(s,card.id),level=s.traits[card.id]??0;
   const voters=humans.filter(id=>votes[id]===card.id);
   const reason=card.maxLevel&&level>=card.maxLevel?'최대 단계':cost>s.affection?'포인트 부족':canBuyCard(s,card.id)?'':'수리할 곳 없음';
   const buttons=localVoters.map(player=>{
     const selected=voters.includes(player);
     const action=humans.length===1?'구매':selected?'찬성함':'찬성';
     const caption=mode==='local'?player.toUpperCase()+' '+action:action;
     return '<button type="button" class="card-vote" aria-label="'+player.toUpperCase()+' '+escapeHtml(card.name)+' '+action+'" aria-pressed="'+selected+'" data-choice="vote-upgrade" data-player="'+player+'" data-card="'+escapeHtml(card.id)+'" data-round="'+progress.round+'"'+(reason?' disabled':'')+'>'+caption+'</button>';
   }).join('');
   const levelText=card.maxLevel?'<small>강화 '+level+'/'+card.maxLevel+'</small>':'';
   const ballot=humans.length>1?'<small class="card-ballot">'+voters.length+'/'+threshold+'표'+(voters.length?' · '+voters.map(id=>id.toUpperCase()).join(', '):'')+'</small>':'';
   return '<article class="upgrade-card"><div class="upgrade-card-heading"><span class="token" aria-hidden="true">'+escapeHtml(card.icon||'◇')+'</span><h4>'+escapeHtml(card.name)+'</h4><strong>'+cost+' AP</strong></div>'+
     '<p>'+escapeHtml(card.desc)+'</p><div class="upgrade-card-meta">'+levelText+ballot+(reason?'<small>'+reason+'</small>':'')+'</div><div class="upgrade-votes">'+buttons+'</div></article>';
 }).join('');
 const purchase=progress.lastPurchase;
 const purchasedCard=purchase?UPGRADE_CARDS.find(card=>card.id===purchase.cardId):null;
 const note=humans.length===1?'애정 포인트로 원하는 카드를 구매하세요.':'플레이어마다 카드 하나에 투표할 수 있습니다. 같은 카드에 '+threshold+'표가 모이면 자동으로 팀 포인트가 결제됩니다.';
 return '<section class="intermission-section" id="upgrade-shop" aria-labelledby="upgrade-heading">'+
   '<div class="intermission-heading"><h3 id="upgrade-heading">팀 정비 상점</h3><strong id="affection-bank">보유 '+s.affection+' AP</strong></div>'+
   '<p>'+note+'</p><p id="shop-purchase" class="shop-purchase" role="status">'+(purchasedCard?escapeHtml(purchasedCard.name)+' 구매 완료 · '+purchase.cost+' AP 사용':'')+'</p>'+
   '<div class="upgrade-grid">'+cards+'</div></section>'+
   (canLead?'<button type="button" class="primary modal-primary" data-choice="depart">'+departureLabel+' 출발</button>':'<p class="intro departure-note">방장이 '+departureLabel+' 출발을 결정합니다.</p>');
}
function resultDialog(){
 const {chapter,stage}=position(s),chapterInfo=chapterSpec(chapter),stageInfo=stageSpec(chapter,stage);
 const canLead=mode!=='online'||room.host;
 if(s.mode==='lost'){
   const heading=s.escapee?s.escapee+'가 탈출했습니다':'마차가 붙잡혔습니다';
   const retry=canLead?'<button class="primary modal-primary" data-choice="retry">이 스테이지 다시 도전</button>':row('재출발 대기','방장이 다시 도전하면 함께 출발합니다.');
   return title(heading,stageInfo.title+'에서 '+Math.floor(s.distance).toLocaleString()+' m를 달렸습니다.')+retry;
 }

 const progress=s.intermission;
 const following=nextStagePosition(chapter,stage);
 const record=row('작전 기록',Math.floor(s.distance).toLocaleString()+' m 탈출 · 추격대 '+s.kill+'명 격퇴 · 내구도 '+durability());
 if(!progress)return title('스테이지 클리어!',stageInfo.title+'의 돌봄 기록을 불러오는 중입니다.')+record;
 const summary=careSummary(progress);
 if(!following){
   const heading=title('챕터 클리어!',chapterInfo.title+'의 '+chapterInfo.stages.length+'개 스테이지를 모두 통과했습니다.');
   const replay=canLead?'<button class="primary modal-primary" data-choice="restart-chapter">이 챕터 다시 시작</button>':row('재출발 대기','방장이 챕터를 다시 시작하면 함께 출발합니다.');
   return heading+summary+record+replay+resultError;
 }

 const nextChapter=following.chapter!==chapter;
 const heading=title(nextChapter?'챕터 클리어!':'스테이지 클리어!',stageInfo.title+' 구간을 무사히 통과했습니다.');
 const shop=progress.phase==='shop'?upgradeShop(progress,canLead,nextChapter?'다음 챕터':'다음 스테이지'):'';
 return heading+summary+reportPanel(progress,canLead)+shop+record+resultError;
}
function resultSignature(){
 const progress=s.intermission;
 return JSON.stringify([s.mode,position(s),humanIds(s),room?.host,room?.captain,myPlayer,s.affection,progress?.phase,progress?.round,progress?.votes,progress?.report]);
}
function refreshResult(){
 if(screen!=='result'||!dlg.open)return;
 const scroll=dlg.scrollTop;
 const focus=document.activeElement?.closest('[data-choice]')?.dataset;
 drawDialog();
 if(focus){
   const match=all('#dialog-body [data-choice]').find(btn=>!btn.disabled&&btn.dataset.choice===focus.choice&&btn.dataset.card===focus.card&&btn.dataset.player===focus.player&&btn.dataset.report===focus.report);
   if(match)match.focus({preventScroll:true});
   else{
     const heading=body.querySelector('.modal-title');
     heading.tabIndex=-1;
     heading.focus({preventScroll:true});
   }
 }
 dlg.scrollTop=scroll;
}
function drawDialog(){
 const settings=screen==='settings',status=screen==='status';
 dlg.classList.toggle('intermission',screen==='result'&&s.mode==='clear');
 $('settings-menu').hidden=!settings;$('status-panel').hidden=!status;body.hidden=settings||status;
 $('back-settings').hidden=settings||screen==='result';
 if(settings||status){body.replaceChildren();fullUI();return;}
 const existing=$('join-code')?.value??invitation;
 if(screen==='guide'){
   body.innerHTML=title('갑판 조작 가이드','왼쪽 스틱으로 작업 원에 들어가세요. 마부와 포수는 앞뒤를 각각 약 2배로 봅니다. 포탄 운반은 3배, 다른 작업은 4배 시야로 전환됩니다.')+
   row('마차 맨 앞 마부석','위쪽 오른말과 아래쪽 왼말 중 하나를 조준하고 채찍질을 연타하세요. 오른말은 마차를 화면 위로, 왼말은 화면 아래로 움직입니다. 같은 말을 계속 때리면 상처가 쌓여 지치거나 잠시 가속을 거부합니다.')+
   row('투석기 바위','다가오는 바위 그림자에 테두리가 생기면 착탄 지점이 길 위에 고정됩니다. 반대쪽 말로 방향을 틀거나, 두 말을 재촉해 기본 속도보다 약 20% 빠르게 달리면 착탄점을 앞지를 수 있습니다.')+
   row('운반 · 파란 적재칸','포탄 더미에서 한 발을 들어 대포 아래 적재칸까지 걸어가면 자동으로 내려놓습니다. 운반 중 크게 흔들리면 원형 게이지의 색 구간에서 행동을 눌러 포탄을 붙잡으세요.')+
   row('장전 · 초록 원','대포가 비고 적재칸에 포탄이 있으면 장전수 자리가 열립니다. 초록 구간에서 행동을 누르면 한 발을 장전합니다. 실패하면 포탄 한 발이 마차 밖으로 떨어집니다.')+
   row('포격 · 주황 원','점화 뒤 2.5초 동안 오른쪽 스틱이나 마우스로 크게 흔들리는 조준점을 움직입니다. 심지가 다 타는 순간 조준선 방향으로 발사해 경로에 닿는 적을 모두 맞힙니다.')+
   row('왕자와 공주','관심이 떨어지면 자리에서 나와 갑판을 돌아다니고 통로를 막습니다. 가까이서 행동을 눌러 안아 올리고, 아이의 좌석 앞에서 다시 행동을 눌러 앉히세요. 돌아다니는 동안 포탄이나 대포, 고삐를 건드릴 수 있습니다.')+
   row('차륜 수리','바퀴가 빠지면 해당 모서리의 초록 원에서 바퀴 장착과 차축 핀 고정을 차례로 완료하세요.')+
   row('키보드와 마우스','WASD로 이동하고 마우스나 방향키로 방향을 잡습니다. Space 또는 게임 화면 클릭으로 행동합니다. 한 화면 2인에서는 P1이 WASD·마우스·Space, P2가 방향키·IJKL·Enter를 사용합니다.')+
   row('게임패드','왼쪽 스틱으로 이동, 오른쪽 스틱으로 조준, 오른쪽 트리거(R2) 또는 아래쪽 얼굴 버튼(A)으로 행동합니다.')+
   row('마차의 흔들림','충돌, 왕국군의 일격, 대포 반동은 조준점과 타이밍 게이지를 흔듭니다. 잠시 기다려 바늘을 다시 읽거나 흔들리는 순간에 맞추세요.')+
   row('챕터와 웨이브','챕터 안의 스테이지를 순서대로 통과하세요. 웨이브마다 정해진 시간이 되면 화면 오른쪽 밖에서 적이 들어옵니다. 뒤로 갈수록 추격대가 많아지거나 강해집니다.')+
   row('거리와 장애물','스테이지마다 실제 속도로 1,000m를 달려야 합니다. 앞길의 통나무, 바위, 야생동물을 피하면 내구도와 속도를 지킬 수 있습니다.')+
   row('마왕 보고와 팀 정비','스테이지를 마치면 돌봄 평가로 애정 포인트를 받습니다. 마왕에게 상황을 보고한 뒤 같은 카드에 과반수가 찬성하면 포인트를 써서 마차를 수리하거나 강화합니다. 1인 플레이는 바로 구매할 수 있습니다.')+
   '<button class="neutral" data-choice="settings">환경설정으로 돌아가기</button>';
 }else if(screen==='traits'){
   body.innerHTML=title('정비 카드 도감','스테이지 사이에 애정 포인트로 마차를 수리하거나 강화합니다. 강화 효과는 팀 전체에 적용됩니다.')+upgradeGuide();
 }else if(screen==='crew'&&room){
   const ready=s.mode==='ready',members=humanIds(s);
   const description=ready?(room.host?'혼자서도 바로 출발할 수 있습니다. 동료는 플레이 도중에도 참가할 수 있습니다.':'방장이 작전을 시작하면 함께 출발합니다.'):'이 방번호로 진행 중인 작전에 합류할 수 있습니다.';
   const roster=PLAYER_IDS.map(id=>'<div class="room-seat '+id+'"><b>'+id.toUpperCase()+'</b><span>'+(!members.includes(id)?'봇':id===myPlayer?'나':'플레이어')+(members.includes(id)&&room.captain===id?' · 방장':'')+'</span></div>').join('');
   body.innerHTML=title('온라인 협동 방',description)+
   '<div class="room-ticket"><span>방번호</span><strong id="room-number">'+room.code+'</strong></div>'+
   '<h3 class="room-count">플레이어 '+room.crew+' / 4명 · 봇 '+(4-room.crew)+'명</h3><div class="room-seats">'+roster+'</div>'+
   option('copy','↗','초대 링크 복사','링크를 받은 동료는 이 방에 바로 참가할 수 있습니다.')+
   (ready&&room.host?'<button class="primary modal-primary" data-choice="room-start">작전 시작</button>':'<button class="primary modal-primary" data-choice="close">게임으로 돌아가기</button>')+
   '<button class="neutral" data-choice="leave">방 나가기</button><p id="error" class="error" role="alert"></p>';
 }else if(screen==='crew'){
   const legacy=isLegacyRoomCode(invitation);
   body.innerHTML=title('협동 플레이','방장 포함 최대 4명이 한 마차에 탑승합니다. 빈자리는 봇이 맡습니다.')+
   option('create','↗','온라인 협동 방 만들기','동료에게 알려 줄 숫자 3자리 방번호를 받습니다.')+
   '<label class="room-input-label" for="join-code">'+(legacy?'이전 초대 코드로 참가':'방번호로 참가')+'</label>'+
   '<form class="join-form" id="join-form" novalidate><input aria-label="방번호" aria-describedby="error" placeholder="'+(legacy?'초대 코드':'숫자 3자리')+'" id="join-code" type="text" inputmode="'+(legacy?'text':'numeric')+'" maxlength="'+(legacy?6:3)+'" autocomplete="off" spellcheck="false" enterkeyhint="go"><button type="submit">방 참가</button></form><p id="error" class="error" role="alert"></p>'+
   '<div class="modal-section">같은 기기에서 플레이</div>'+
   option('local','2P','한 화면 협동','두 쌍의 화면 스틱 또는 키보드로 각자 졸개를 움직입니다.',mode==='local'?'✓':'›')+
   option('solo','1P','1인 갑판 연습','한 명을 직접 조작하고 세 동료가 빈 작업을 맡습니다.',mode==='practice'?'✓':'›');
   const field=$('join-code');field.value=existing;
   field.oninput=()=>{field.removeAttribute('aria-invalid');label($('error'),'');};
   $('join-form').onsubmit=e=>{
     e.preventDefault();
     const code=field.value.normalize('NFKC').trim().toUpperCase();
     if(!isRoomNumber(code)&&!(legacy&&isLegacyRoomCode(code))){
       field.setAttribute('aria-invalid','true');field.focus();errorMsg('숫자 3자리 방번호를 입력해 주세요.');return;
     }
     connect('join',code);
   };
 }else if(screen==='result'){
   body.innerHTML=resultDialog();
 }
}
function resetLocal(m){controls.clear();mode=m;myPlayer='p1';room=null;netFailed=0;if(s.mode!=='ready'&&s.mode!=='run'&&s.mode!=='paused')s=initial();setHumanSlots(s,m==='local'?['p1','p2']:['p1']);closeScreen();toast(m==='local'?'한 화면 협동으로 전환했습니다.':'1인 갑판 연습으로 전환했습니다.');}
body.addEventListener('click',async e=>{
 const btn=e.target.closest('[data-choice]');if(!btn)return;const v=btn.dataset.choice;
 if(v==='close')closeScreen();else if(v==='settings')openScreen('settings');else if(v==='local'||v.startsWith('solo'))resetLocal(v==='local'?'local':'practice');
 else if(v==='room-start'){await start();}else if(v==='create')connect('create');else if(v==='leave'){try{await transport('leave');}catch(e){if(e.status!==404){errorMsg(e.message);return;}}dropRoom();closeScreen();toast('협동 방에서 나왔습니다.');}
 else if(v==='copy'){const url=new URL(location.href);url.search='';url.searchParams.set('room',room.code);try{await navigator.clipboard.writeText(url.href);toast('초대 링크를 복사했습니다.');}catch{const field=document.createElement('input');field.value=url.href;field.className='neutral';field.readOnly=true;body.append(field);field.select();toast('선택한 링크를 복사해 주세요.');}}
 else if(v==='report'){
   const id=btn.dataset.report;
   if(mode==='online'){await send('report',{...position(s),id});return;}
   const error=submitReport(s,id);
   if(error)errorMsg(error);
   else{refreshResult();tone(640);}
 }else if(v==='vote-upgrade'){
   const id=btn.dataset.card,round=Number(btn.dataset.round);
   if(mode==='online'){await send('vote-upgrade',{...position(s),id,round});return;}
   const error=voteForCard(s,btn.dataset.player,id,round,humanIds(s));
   if(error)errorMsg(error);
   else{refreshResult();tone(s.intermission.round>round?720:450);}
 }else if(v==='depart'){
   if(mode==='online'){if(await send('depart',position(s)))closeScreen();return;}
   const next=nextStage(s);
   if(next===s){errorMsg('마왕에게 보고한 뒤 출발할 수 있습니다.');return;}
   s=next;closeScreen();tone(720);
 }else if(v==='retry'||v==='restart-chapter'){
   if(mode==='online'){if(await send(v,position(s)))closeScreen();}
   else{s=v==='retry'?restartStage(s):restartChapter(s);closeScreen();}
 }
});
all('[data-view]').forEach(b=>b.onclick=()=>openScreen(b.dataset.view));
$('settings-button').onclick=$('back-settings').onclick=()=>openScreen('settings');
function errorMsg(msg){if($('error'))label($('error'),msg);else toast(msg);}
async function transport(op,extra={},explicit=null){
 const auth=explicit??room;let response;
 try{response=await fetch('/api/room',{method:'POST',headers:{'Content-Type':'application/json','X-Crew-Key':auth?.token||''},body:JSON.stringify({op,code:auth?.code,...extra}),signal:AbortSignal.timeout(8500)});}catch{throw new Error('연결이 지연되고 있습니다. 잠시 후 다시 시도하세요.');}
 let data;try{data=await response.json();}catch{throw new Error('협동 방을 불러올 수 없습니다. 다시 접속해 주세요.');}
 if(!response.ok){const error=new Error(data.error||'협동 방 연결 실패');error.status=response.status;throw error;}return data;
}
function remember(){try{if(room)sessionStorage.setItem('gg-room',JSON.stringify({code:room.code,token:room.token}));else sessionStorage.removeItem('gg-room');}catch{}}
function dropRoom(){controls.clear();pendingActs.length=0;lastInputKey='';room=null;mode='practice';myPlayer='p1';s=initial();netFailed=0;remember();const u=new URL(location.href);u.searchParams.delete('room');history.replaceState(null,'',u);}
function accept(data,latency=0){
 if(!room||data.code!==room.code||data.rev<room.rev||(data.rev===room.rev&&data.stamp<room.stamp))return;
 const beforeResult=screen==='result'?resultSignature():null;
 const stageChanged=!sameStage(data.state,s);
 if(data.state.mode!=='run'||stageChanged)pendingActs.length=0;
 if(stageChanged){controls.clear();lastInputKey='';lastInputAt=0;}
 const prevCaptain=room.captain,previous=humanIds(s).join()+prevCaptain,prevMode=s.mode;room.host=data.host;room.captain=data.captain;room.rev=data.rev;room.stamp=data.stamp;room.crew=data.crew;myPlayer=data.player;s=data.state;
 advance(s,Math.min(12,Math.round(latency*HZ/2000)));netFailed=0;lastTick=performance.now();accum=0;
 if(previous!==humanIds(s).join()+room.captain&&dlg.open&&screen==='crew')drawDialog();
 if(screen==='result'&&s.mode==='run')closeScreen();
 else if(beforeResult!==null&&beforeResult!==resultSignature())refreshResult();
 if(prevMode==='ready'&&s.mode==='run'&&screen==='crew')closeScreen();
}
async function connect(op,code='',saved=null){
 if(actBusy)return;actBusy=true;const buttons=body.querySelectorAll('button,input');buttons.forEach(x=>x.disabled=true);
 const extra={code};
 if(op==='join'&&!saved){
   if(joinAttempt?.code!==code)joinAttempt={code,id:crypto.randomUUID()};
   extra.joinId=joinAttempt.id;
 }
 try{const data=await transport(op,extra,saved);joinAttempt=null;room={code:data.code,token:data.token??saved?.token,rev:-1,stamp:0,crew:0};mode='online';autoPaused=false;accept(data);remember();const u=new URL(location.href);u.searchParams.set('room',room.code);history.replaceState(null,'',u);
   if(op==='create')openScreen('crew');else {if(s.mode==='ready')openScreen('crew');else closeScreen();toast(myPlayer.toUpperCase()+' 졸개로 탑승했습니다.');}
 }catch(e){if(saved&&[403,404].includes(e.status))dropRoom();openScreen('crew');errorMsg(e.message);}
 finally{actBusy=false;buttons.forEach(x=>x.disabled=false);}
}
async function send(op,extra={}){
 if(actBusy||!room)return false;actBusy=true;const id=room.token,now=performance.now();
 try{const data=await transport(op,extra);if(room?.token===id)accept(data,performance.now()-now);return true;}catch(e){errorMsg(e.message);return false;}finally{actBusy=false;}
}
async function sync(){
 if(syncBusy||!room)return;const id=room.token;syncBusy=true;const now=performance.now();
 try{const data=await transport('sync');if(room?.token===id)accept(data,performance.now()-now);}
 catch(e){if(room?.token===id){netFailed++;if(e.status===404||e.status===403){dropRoom();if(screen==='crew')drawDialog();toast(e.status===403?'연결이 끊겨 자리를 봇이 맡았습니다. 방번호로 다시 참가하세요.':'협동 방이 종료되었습니다.');}else if(netFailed===2)toast('동료와 다시 연결하고 있습니다.');}}
 finally{syncBusy=false;lastSync=performance.now();}
}
async function start(){
 if(s.mode==='clear'||s.mode==='lost'){openScreen('result');return;}
 if(mode==='online'){if(!room.host){toast('방장이 출발과 일시 정지를 맡습니다.');return;}await send(s.mode==='ready'?'start':'pause');}
 else if(s.mode==='ready'){begin(s);tone(500);}else s.mode=s.mode==='run'?'paused':'run';
 controls.clear();lastInputKey='';lastTick=performance.now();accum=0;
}
$('start').onclick=start;
function useStation(id,input){
 if(s.mode!=='run'||dlg.open)return;
 const p=s.players[id],role=stationAt(p,s);
 if(role==='carry'||role==='royal-carry'||(role==='gunner'&&(!s.ammo.loaded||s.fuse.end)))return;
 if(!role){toast('갑판의 작업 원 안으로 이동하세요.');return;}
 setInput(s,id,input);
 const whipping=role==='driver';
 if(whipping&&!horseAim(input))return;
 const royalId=role==='royal-pickup'||role==='royal-seat'?royalTarget(s,p)?.id:null;
 if(mode==='online'){
   if(!whipping)pendingActs.length=0;
   const now=performance.now();
   if(pendingActs.length<4)pendingActs.push({tick:s.tick,...position(s),input,role:whipping||royalId?role:null,royalId,queuedAt:now});
   pumpInput(now,input);return;
 }
 if(!operate(s,id)&&(whipping||royalId))return;
 const msg=s.players[id].note||s[feedbackKey(role)+'Msg']||'';
 tone(msg.includes('실패')?130:460,.075);
}
async function pumpInput(now,input){
 if(mode!=='online'||!room||inputBusy||actBusy||s.mode!=='run')return;
 const key=inputKey(input);
 const moving=Math.abs(input.mx)+Math.abs(input.my)>.01;
 const currentRole=stationAt(s.players[myPlayer],s);
 while(pendingActs.length){
   const next=pendingActs[0];
   if(sameStage(next,s)&&(!next.role||next.role===currentRole&&now-next.queuedAt<=600))break;
   pendingActs.shift();
 }
 const act=pendingActs.shift();
 if(!act&&(now-lastInputAt<90||key===lastInputKey&&(!moving||now-lastInputAt<460)))return;
 const sentInput=act?.role?{...act.input,mx:input.mx,my:input.my}:act?.input??input;
 lastInputKey=inputKey(sentInput);lastInputAt=now;inputBusy=true;
 const token=room.token;
 try{
   const extra={...position(act??s),input:sentInput};
   if(act)extra.tick=act.tick;
   if(act?.role)extra.role=act.role;
   if(act?.royalId)extra.royalId=act.royalId;
   const data=await transport(act?'act':'input',extra);
   if(room?.token===token)accept(data,performance.now()-now);
 }catch(e){lastInputKey='';if(act)toast(e.message);}
 finally{inputBusy=false;}
}
function fullUI(){
 const full=Boolean(document.fullscreenElement||document.webkitFullscreenElement);
 $('expand').setAttribute('aria-pressed',String(full));
 label($('full-state'),full?'끄기':'켜기');
}
async function toggleFullscreen(){
 const root=document.documentElement,full=document.fullscreenElement||document.webkitFullscreenElement;
 closeScreen();
 try{
   if(full)await (document.exitFullscreen||document.webkitExitFullscreen).call(document);
   else{
     const enter=root.requestFullscreen||root.webkitRequestFullscreen;
     if(!enter)throw new Error('unsupported');
     await enter.call(root);
   }
 }catch{toast('이 브라우저에서는 현재 창 전체로 플레이합니다.');}
 fullUI();
}
$('expand').onclick=toggleFullscreen;
document.addEventListener('fullscreenchange',fullUI);document.addEventListener('webkitfullscreenchange',fullUI);
function clockText(seconds){
 return Math.floor(seconds/60)+':'+String(seconds%60).padStart(2,'0');
}
function campaignHud(playing){
 const {chapter,stage}=position(s),chapterInfo=chapterSpec(chapter),stageInfo=stageSpec(chapter,stage);
 const launched=s.wave??0,next=stageInfo.waves[launched],elapsed=s.tick/HZ;
 const nextWave=next?'다음 '+clockText(Math.max(0,Math.ceil(next.at-elapsed))):'추가 웨이브 없음';
 const wave='웨이브 '+launched+'/'+stageInfo.waves.length;
 const remaining=Math.max(0,Math.ceil(s.goal-s.distance)).toLocaleString()+' m';
 $('campaign-hud').hidden=!playing;
 label($('campaign-path'),'챕터 '+chapter+' · 스테이지 '+stage);
 label($('remaining-distance'),'남은 '+remaining);
 label($('campaign-wave'),wave);label($('campaign-next-wave'),nextWave);
 label($('chapter-no'),'챕터 '+chapter);label($('chapter-name'),chapterInfo.title);
 label($('stage-no'),'스테이지 '+stage+' / '+chapterInfo.stages.length);label($('stage-name'),stageInfo.title);
 label($('wave-no'),wave);label($('next-wave'),nextWave);label($('dist'),remaining);
 label($('stage-clock'),'주행 '+Math.floor(s.distance).toLocaleString()+' / '+s.goal.toLocaleString()+' m · '+clockText(Math.floor(elapsed))+' 경과');
 $('bar').style.width=Math.min(100,s.distance/s.goal*100)+'%';
}
function hud(cameras){
 const focused=cameras.some(v=>v.role),duo=mode==='local';
 const playing=s.mode==='run'||s.mode==='paused';
 const jobs=(duo?['p1','p2']:[myPlayer]).map(id=>({id,role:playing?stationAt(s.players[id],s):null}));
 $('desk').classList.toggle('focusing',focused);canvas.parentElement.classList.toggle('split',cameras.length===2);
 const panels=$('work-panels');panels.hidden=!jobs.some(job=>job.role);panels.classList.toggle('duo',duo);
 for(const [i,slot] of ['p1','p2'].entries()){
   const job=jobs[i],panel=$('job-'+slot);panel.hidden=!job?.role;
   if(!job?.role)continue;
   const {id,role}=job,v=cameras[i]??cameras[0];
   const p=s.players[id],info=workInfo(s,p,role),part=k=>panel.querySelector('.'+k);
   const timed=!info.mode,catching=role==='catch',whipping=info.mode==='whip',royal=info.mode==='royal';
   const needle=catching?catchPhase(p,s.tick):timed?phase(s,role):0;
   const band=timed?width(s,role):0;
   const ready=timed&&hit(s,role,needle)&&s.mode==='run'&&!info.blocked&&(catching||!(s[feedbackKey(role)+'Cd']>s.tick));
   panel.style.setProperty('--work',info.color);panel.dataset.hit=String(ready);panel.classList.toggle('direct-work',whipping||royal);
   label(part('work-role'),id.toUpperCase()+' · '+info.roleName);part('work-zoom').hidden=royal;label(part('work-zoom'),Number(v.zoom.toFixed(1))+'×');
   label(part('work-title'),info.title);label(part('work-detail'),info.detail);label(part('work-step'),info.step);
   const lit=role==='gunner'&&s.fuse.end>0,remaining=lit?(s.fuse.end-s.tick)/FUSE_TICKS:0;
   label(part('dial-word'),s.mode==='paused'?'정지':timed?(ready?'지금!':'대기'):lit?((s.fuse.end-s.tick)/HZ).toFixed(1)+'초':role==='gunner'?(s.ammo.loaded?'장전':'빈 포'):'운반');
   const arc=timed?band:lit?remaining:role==='gunner'&&s.ammo.loaded?1:0;
   part('work-arc').setAttribute('stroke-dasharray',arc*100+' '+(100-arc*100));
   part('work-arc').setAttribute('transform','rotate('+(-90+(timed?(target(s,role)-band/2)*360:0))+' 50 50)');
   part('work-hand').style.visibility=timed?'visible':'hidden';
   part('work-hand').setAttribute('transform','rotate('+needle*360+' 50 50)');
   part('work-dial').hidden=whipping||royal;
   part('work-dial').ariaLabel=timed?'원형 타이밍 게이지':lit?'자동 발사까지 남은 시간':'작업 상태';
   part('work-dial').style.transform=timed&&s.shake&&s.mode==='run'?'translate('+Math.sin(s.tick*1.7)*s.shake*6+'px,'+Math.cos(s.tick)*s.shake*3+'px)':'';
   part('work-do').hidden=!timed&&!whipping&&!info.action;part('work-do').disabled=info.blocked||s.mode!=='run';
   label(part('work-do'),info.action||(whipping?'채찍질':catching?'붙잡기':role==='loader'?'장전':ready?'지금 행동':'행동'));
   const actionKey=slot==='p2'?'Enter':'Space';
   label(part('work-help'),royal?info.action?info.action+' · '+actionKey:'':whipping?'조준 후 '+actionKey+' 연타':role==='gunner'?'오른쪽 스틱 · 조준':role==='carry'?'왼쪽 스틱 · 이동':'색 구간에서 행동 · '+actionKey);

 }
 campaignHud(playing);
 const modeLabel=mode==='online'?(netFailed>1?'재연결 중':room.code+' · '+myPlayer.toUpperCase()):mode==='local'?'한 화면 2인 협동':'1인 연습 모드';
 label($('mode-label'),modeLabel);
 const clearLabel=s.mode==='clear'&&nextStagePosition(s.chapter??1,s.stage)?(s.intermission?.phase==='shop'?'팀 정비 상점':'마왕에게 보고'):'작전 결과';
 const startLabel=({ready:'작전 시작',run:'일시 정지',paused:'계속 달리기',clear:clearLabel,lost:'작전 결과'})[s.mode];
 const icon=s.mode==='run'?'<path d="M7 5h3v14H7zm7 0h3v14h-3z"/>':'<path d="m6 4 14 8-14 8z"/>';
 if(lastStatus!==startLabel){$('start').innerHTML='<svg viewBox="0 0 24 24" width="15" height="15" fill="currentColor">'+icon+'</svg><span>'+startLabel+'</span>';$('start').ariaLabel=startLabel;$('start').title=startLabel;$('start').classList.toggle('pause-only',s.mode==='run');lastStatus=startLabel;}
 $('start').disabled=mode==='online'&&!room.host&&!['clear','lost'].includes(s.mode);
 label($('hp'),durability());label($('affection-readout'),(s.affection??0)+' AP');
 markup($('speed'),(s.mode==='ready'?0:Math.round(s.speed))+' <small>m/s</small>');
 const broken=WHEELS[s.wheel.broken],ev=$('event'),recent=s.eventUntil>s.tick;
 const rock=s.catapult,incoming=!!rock&&s.tick<rock.landAt;
 let warning='';
 if(incoming){
   const offset=rock.y-s.y;
   const side=Math.abs(offset)<10?'가운데':offset<0?'오른쪽(위)':'왼쪽(아래)';
   warning=s.tick<rock.lockAt?'투석기 바위 접근 · '+side:'착탄까지 '+Math.ceil((rock.landAt-s.tick)/HZ)+'초 · '+side;
 }else if(typeof rock?.hit==='boolean')warning=rock.hit?'투석기 바위 명중':'투석기 바위 회피';
 const royalNotice=s.royalNotice?.until>s.tick?s.royalNotice.text:'';
 ev.classList.toggle('show',!!(warning||royalNotice||recent||broken)&&s.mode==='run');
 ev.classList.toggle('catapult',!!warning);
 ev.classList.toggle('royal',!!royalNotice);
 ev.classList.toggle('damage',!warning&&!royalNotice&&!!broken&&!recent);
 label(ev,[warning,royalNotice].filter(Boolean).join(' · ')||(recent?s.event:broken?broken.name+' 수리 필요 · '+s.wheel.progress+'/2':''));
 for(const [id,p] of Object.entries(s.players)){
   const human=humanIds(s).includes(id);
   label($(id+'-crew-name'),human?(id===myPlayer?'내 졸개':'플레이어'):'자동 동료');
   const role=stationAt(p,s);label($(id+'-crew-job'),p.ball?'포탄 운반 중':role?ROLES[role]:!human&&p.job?ROLES[p.job]+' 이동 중':'갑판');
 }
 label($('ammo-readout'),'포탄 더미 '+s.ammo.stock+'발 · 적재칸 '+s.ammo.rack+'발 · '+(s.ammo.loaded?'대포 장전됨':'대포 비어 있음')+' · 손실 '+s.ammo.lost+'발');
 label($('royal-readout'),s.royals.map(r=>r.name+' 관심 '+Math.floor(r.attention)+'%'+(r.state==='wandering'?' · 돌아다니는 중':r.state==='carried'?' · 안겨 있음':'')).join(' / '));
 label($('wheel-readout'),broken?broken.name+' 수리 필요':'바퀴 4개 장착');
 $('pads-p2').hidden=!duo;$('pads-p1').parentElement.classList.toggle('duo',duo);
 for(const slot of duo?['p1','p2']:['p1']){
   const id=duo?slot:myPlayer,p=s.players[id],role=stationAt(p,s);
   const ident=$(slot+'-ident');
   if(ident.dataset.actor!==id){
     ident.dataset.actor=id;
     const pads=$('pads-'+slot).querySelectorAll('[data-pad]');
     pads[0].ariaLabel=id.toUpperCase()+' 이동 스틱';
     pads[1].ariaLabel=id.toUpperCase()+' 방향 스틱, 놓으면 행동';
   }
   const atGun=role==='gunner',atDriver=role==='driver',atRoyal=role?.startsWith('royal-'),ignite=$(slot+'-ignite'),aim=$('pads-'+slot).querySelector('.aim-stick');
   ignite.hidden=!atGun;ignite.disabled=!s.ammo.loaded||!!s.fuse.end||s.mode!=='run';
   label(ignite, s.fuse.end?'점화 중':s.ammo.loaded?'점화':'장전 대기');
   label($(slot+'-aim-label'),atGun||atDriver?'조준':atRoyal?'방향':'방향 · 행동');aim.ariaLabel=id.toUpperCase()+(atGun?' 대포 조준 스틱':atDriver?' 말 조준 스틱':atRoyal?' 방향 스틱':' 방향 스틱, 놓으면 행동');
   label(ident,id.toUpperCase());ident.classList.toggle('second',id==='p2');
   label($(slot+'-location'),ROLES[role]||'갑판');
   let hint=p.ball?'포탄을 들고 뒤쪽 적재칸으로 이동':broken?broken.name+' 옆 수리 원으로 이동':'왼쪽 스틱으로 색 원까지 이동';
   if(role)hint=atGun?(s.fuse.end?'심지가 타는 동안 오른쪽 스틱으로 조준':'장전 후 점화 버튼'):(p.ball?'파란 적재칸에 닿으면 자동으로 내려놓기':'작업 원 밖으로 걸어 나가면 전체 시야');
   if(p.note&&p.noteUntil>s.tick)hint=p.note;
   const context=$('pads-'+slot).querySelector('.context');context.classList.toggle('visible',s.mode==='run'&&!role&&!!p.note&&p.noteUntil>s.tick);
   label($(slot+'-hint'),hint);
 }

}
function frame(now){
 const dt=Math.min(.2,(now-lastTick)/1000);lastTick=now;
 if(s.mode==='run'&&!(mode==='online'&&netFailed>=2)){
  const first=controls.input('p1'),second=mode==='local'?controls.input('p2'):null;
  if(dlg.open){first.mx=first.my=0;if(second)second.mx=second.my=0;}
  if(mode==='online')pumpInput(now,first);
  accum+=dt;
  while(accum>=STEP){
    setInput(s,myPlayer,first);
    if(second)setInput(s,'p2',second);
    step(s);accum-=STEP;
  }
 }
 if(room&&now-lastSync>(s.mode==='run'?350:750))sync();
 if(now-lastPaint>=32){const cams=views();render(canvas,s,now,cams);hud(cams);lastPaint=now;}
 if(s.serial!==lastSerial){if(s.serial>lastSerial)tone(105,.16,'triangle');lastSerial=s.serial;}
 const terminal=['lost','clear'].includes(s.mode)?(s.chapter??1)+'/'+s.stage+'/'+s.mode:'';
 if(terminal&&terminal!==terminalSeen){openScreen('result');tone(s.mode==='clear'?700:200,.24);}terminalSeen=terminal;
 requestAnimationFrame(frame);
}
// Browsers suspend animation in background tabs. Keep an open room seat alive.
setInterval(()=>{if(room&&document.hidden&&performance.now()-lastSync>8000)sync();},10_000);
hud(views());render(canvas,s,performance.now(),views());requestAnimationFrame(frame);
let saved;try{saved=JSON.parse(sessionStorage.getItem('gg-room'));}catch{}
if(saved&&(isRoomNumber(saved.code)||isLegacyRoomCode(saved.code))&&(!invitation||invitation===saved.code))connect('join',saved.code,saved);else if(invitation)openScreen('crew');
