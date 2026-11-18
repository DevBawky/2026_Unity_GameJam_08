import {isRoomNumber,isLegacyRoomCode} from './room-number.mjs';
import {chapterSpec,stageSpec,nextStagePosition} from './campaign.mjs';
import {REPORT_OPTIONS,UPGRADE_CARDS,submitReport,voteForCard,voteThreshold,maxDurability,cardCost,canBuyCard} from './intermission.mjs?v=wheel-axle83';
import {initial,begin,PLAYER_IDS,humanIds,setHumanSlots,step,advance,operate,setInput,stationAt,occupiedStation,leaveStation,royalTarget,royalStatusNearby,feedTarget,cookingProgress,stopCookingHold,ROLES,WHEELS,hasBrokenAxle,HORSE_RUNAWAY_HITS,CANNON_CLEAN_TAPS,cannonStation,isCannonRole,incomingPursuit,playerFloor,CELLAR_FLOOR,feedbackKey,hit,nextStage,restartStage,restartChapter,phase,width,target,catchPhase,HZ,STEP,LANES,roadApproachWarning,addRackShell} from './sim.mjs?v=double-shell87';
import {cameraViews,createCameraTransition,workInfo} from './workstation.mjs?v=double-shell87';
import {render} from './scene.mjs?v=occupied-pad88';
import {createControls} from './controls.mjs?v=double-shell87';
import {EMOTES,EMOTE_BUBBLE_MS,EMOTE_COOLDOWN_MS,emoteByKey,addEmote,recentEmotes} from './emotes.mjs';
const $=id=>document.getElementById(id), all=q=>Array.from(document.querySelectorAll(q));
const dlg=$('dialog'),body=$('dialog-body'),canvas=$('world');
const paintInterval=1000/HZ,paintTolerance=.5;
let s=initial(),mode='practice',myPlayer='p1',room=null,screen='',syncBusy=false,actBusy=false,netFailed=0,lastSync=0,lastTick=performance.now(),accum=0,lastPaint=0,lastStatus='',lastSerial=0,autoPaused=false,terminalSeen='',toastTimer,ac=null;
let lastCatapultLanding='';
let lastCannonLanding='';
let nextWhimper=0;
let sound=false;try{sound=localStorage.getItem('gg-sound')==='on';}catch{}
const linkCode=new URL(location.href).searchParams.get('room')?.trim().toUpperCase()||'';
const invitation=isRoomNumber(linkCode)||isLegacyRoomCode(linkCode)?linkCode:'';
let joinAttempt=null;
function markup(el,html){if(el.dataset.value!==html){el.innerHTML=html;el.dataset.value=html;}}
function label(el,text){if(el.textContent!==text)el.textContent=text;}
function updateProperty(element,key,value){if(element[key]!==value)element[key]=value;}
function updateStyle(element,key,value){if(element.style.getPropertyValue(key)!==value)element.style.setProperty(key,value);}
function updateAttribute(element,key,value){if(element.getAttribute(key)!==value)element.setAttribute(key,value);}
function toast(msg){label($('toast'),msg);$('toast').classList.add('on');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').classList.remove('on'),2500);}
function effectAudio(){
 ac??=new AudioContext();
 if(ac.state!=='running')ac.resume().catch(()=>{});
 return ac;
}
function tone(freq=350,duration=.08,type='triangle'){if(!sound)return;try{const context=effectAudio(),osc=context.createOscillator(),g=context.createGain();osc.type=type;osc.frequency.setValueAtTime(freq,context.currentTime);osc.frequency.exponentialRampToValueAtTime(Math.max(40,freq*.68),context.currentTime+duration);g.gain.setValueAtTime(.045,context.currentTime);g.gain.exponentialRampToValueAtTime(.001,context.currentTime+duration);osc.connect(g);g.connect(context.destination);osc.start();osc.stop(context.currentTime+duration);}catch{}}
function playRoyalWhimper(now){
 if(!sound||s.mode!=='run'||now<nextWhimper)return;
 const child=s.royals.find(royal=>royal.crying&&(royalStatusNearby(s.players[myPlayer],royal)||
   (mode==='local'&&royalStatusNearby(s.players.p2,royal))));
 if(!child)return;
 nextWhimper=now+4200;
 try{
   const audio=effectAudio(),start=audio.currentTime,voice=audio.createOscillator(),gain=audio.createGain();
   const pitch=child.id==='prince'?450:510;
   voice.type='triangle';
   voice.frequency.setValueAtTime(pitch*.74,start);
   voice.frequency.exponentialRampToValueAtTime(pitch,start+.075);
   voice.frequency.exponentialRampToValueAtTime(pitch*.63,start+.35);
   gain.gain.setValueAtTime(.001,start);
   gain.gain.linearRampToValueAtTime(.036,start+.045);
   gain.gain.exponentialRampToValueAtTime(.004,start+.18);
   gain.gain.linearRampToValueAtTime(.022,start+.22);
   gain.gain.exponentialRampToValueAtTime(.001,start+.36);
   voice.connect(gain).connect(audio.destination);voice.start(start);voice.stop(start+.37);
 }catch{}
}
function groundImpactSound(cannon=false){
 if(!sound)return;
 try{
   const context=effectAudio(),now=context.currentTime;
   const debris=context.createBuffer(1,Math.ceil(context.sampleRate*.46),context.sampleRate);
   const samples=debris.getChannelData(0);
   for(let i=0;i<samples.length;i++)samples[i]=Math.random()*2-1;
   const noise=context.createBufferSource(),filter=context.createBiquadFilter(),noiseGain=context.createGain();
   noise.buffer=debris;
   filter.type='lowpass';
   filter.frequency.setValueAtTime(1400,now);
   filter.frequency.exponentialRampToValueAtTime(210,now+.38);
   noiseGain.gain.setValueAtTime(.001,now);
   noiseGain.gain.exponentialRampToValueAtTime(cannon ? .36 : .44,now+.016);
   noiseGain.gain.exponentialRampToValueAtTime(.001,now+.45);
   noise.connect(filter).connect(noiseGain).connect(context.destination);
   noise.start(now);

   const thump=context.createOscillator(),thumpGain=context.createGain(),beat=now+.032;
   thump.type='triangle';
   thump.frequency.setValueAtTime(cannon?150:118,beat);
   thump.frequency.exponentialRampToValueAtTime(43,beat+.3);
   thumpGain.gain.setValueAtTime(.001,beat);
   thumpGain.gain.exponentialRampToValueAtTime(.31,beat+.012);
   thumpGain.gain.exponentialRampToValueAtTime(.001,beat+.4);
   thump.connect(thumpGain).connect(context.destination);
   thump.start(beat);
   thump.stop(beat+.4);
 }catch{}
}
function playCatapultLanding(){
 const rock=s.catapult;
 if(s.tick===0)lastCatapultLanding='';
 if(rock?.hit===undefined||s.tick<rock.landAt)return false;
 const landing=[room?.code,s.chapter,s.stage,rock.landAt].join('/');
 if(lastCatapultLanding===landing)return false;
 lastCatapultLanding=landing;
 if(s.tick-rock.landAt>HZ*.45)return false;
 groundImpactSound();
 return rock.hit;
}
function playCannonLanding(){
 if(s.tick===0)lastCannonLanding='';
 if(!sound)return;
 const impact=s.cannonImpacts?.at(-1);
 if(!impact||s.tick<impact.tick||s.tick-impact.tick>HZ*.45)return;
 const landing=[room?.code,s.chapter,s.stage,impact.id,impact.tick].join('/');
 if(lastCannonLanding===landing)return;
 lastCannonLanding=landing;
 groundImpactSound(true);
}
function soundUI(){const b=$('sound');b.ariaLabel=sound?'효과음 끄기':'효과음 켜기';b.setAttribute('aria-pressed',String(sound));label($('sound-state'),sound?'켜짐':'꺼짐');}
$('sound').onclick=()=>{sound=!sound;try{localStorage.setItem('gg-sound',sound?'on':'off');}catch{}soundUI();tone(500);};
soundUI();
let inputBusy=false,lastInputAt=0,lastInputKey='';
const pendingActs=[];
const inputKey=input=>[input.aimRole??'',input.whipSequence??'',Number(input.aimCursor===true),Number(input.cookHeld===true),...['mx','my','ax','ay'].map(key=>Math.round(input[key]*90))].join('/');
const position=state=>({chapter:state.chapter??1,stage:state.stage});
const sameStage=(a,b)=>(a.chapter??1)===(b.chapter??1)&&a.stage===b.stage;
const transitionCamera=createCameraTransition();
const reducedMotion=window.matchMedia('(prefers-reduced-motion: reduce)');
let paintedCameras;
const emoteButtons=all('[data-emote-toggle]'),emoteNotices=$('emote-notices');
let measuredNotice=null;
const emoteNoticeSize=new ResizeObserver(([entry])=>{
 const height=entry.borderBoxSize?.[0]?.blockSize??entry.target.offsetHeight;
 updateStyle(emoteNotices,'--notice-height',height+'px');
});
const emotePlayer=slot=>mode==='local'?slot:myPlayer;
const canEmote=()=>!dlg.open&&['ready','run','paused'].includes(s.mode);
let openEmotes=null,emoteSending=null;
function closeEmotes(restoreFocus=false){
 if(!openEmotes)return false;
 const {toggle,menu}=openEmotes;
 menu.hidden=true;
 toggle.setAttribute('aria-expanded','false');
 openEmotes=null;
 if(restoreFocus)toggle.focus({preventScroll:true});
 return true;
}
function emoteContent(container,icon,text){
 const symbol=document.createElement('span'),copy=document.createElement('span');
 symbol.className='emote-icon';symbol.setAttribute('aria-hidden','true');symbol.textContent=icon;
 copy.textContent=text;
 container.append(symbol,copy);
}
function chooseEmote(slot,key){
 closeEmotes(true);
 sendEmote(slot,key);
}
for(const toggle of emoteButtons){
 const slot=toggle.dataset.emoteToggle,menu=$('emote-menu-'+slot);
 for(const [index,{key,icon,label:name}] of EMOTES.entries()){
   const option=document.createElement('button'),shortcut=document.createElement('kbd');
   const number=String(index+1);
   option.type='button';option.className='emote-option';option.dataset.emote=key;
   option.setAttribute('aria-keyshortcuts',number);
   shortcut.className='emote-shortcut';shortcut.setAttribute('aria-hidden','true');shortcut.textContent=number;
   option.append(shortcut);
   emoteContent(option,icon,name);
   option.onclick=()=>chooseEmote(slot,key);
   menu.append(option);
 }
 toggle.onclick=()=>{
   const wasOpen=openEmotes?.toggle===toggle;
   closeEmotes();
   if(wasOpen||!canEmote())return;
   menu.hidden=false;toggle.setAttribute('aria-expanded','true');openEmotes={toggle,menu};
   menu.querySelector('button')?.focus({preventScroll:true});
 };
}
document.addEventListener('pointerdown',event=>{
 if(!event.target.closest?.('.emote-controls,.station-buttons,.pad-wrap'))closeEmotes();
},{capture:true});
document.addEventListener('click',event=>{
 if(event.target.closest?.('.station-action,.station-exit'))closeEmotes();
});
document.addEventListener('keydown',event=>{
 if(event.defaultPrevented||event.repeat||event.isComposing||event.ctrlKey||event.metaKey||event.altKey||!canEmote())return;
 if(event.target.isContentEditable||event.target.closest?.('input,textarea,select'))return;
 if(!/^[1-7]$/.test(event.key))return;
 const toggle=openEmotes?.toggle??$('emote-p1');
 const emote=EMOTES[Number(event.key)-1];
 if(toggle.disabled||!emote)return;
 event.preventDefault();
 chooseEmote(toggle.dataset.emoteToggle,emote.key);
});
async function sendEmote(slot,key){
 if(!canEmote())return;
 if(!room){
   const error=addEmote(s,emotePlayer(slot),key,Date.now());
   if(error)toast(error);
   return;
 }
 const token=room.token;
 if(emoteSending===token)return;
 emoteSending=token;
 const sent=performance.now();
 try{
   const data=await transport('emote',{key});
   if(room?.token===token)accept(data,performance.now()-sent);
 }catch(error){if(room?.token===token)toast(error.message);}
 finally{if(emoteSending===token)emoteSending=null;}
}
function updateEmoteDisplay(wall){
 const now=room?.receivedAt!=null?room.stamp+Math.max(0,wall-room.receivedAt):Date.now();
 const active=recentEmotes(s,now),icons=Object.create(null);
 const enabled=canEmote();
 if(!enabled)closeEmotes();
 for(const toggle of emoteButtons){
   const player=emotePlayer(toggle.dataset.emoteToggle);
   updateProperty(toggle,'disabled',!enabled||Boolean(room&&emoteSending===room.token)
     ||active.some(event=>event.player===player&&now-event.at<EMOTE_COOLDOWN_MS));
 }
 if(!active.length&&!emoteNotices.firstElementChild)return icons;
 const latest=new Map();
 for(const event of active)latest.set(event.player,event);
 const notices=[...latest.values()].sort((a,b)=>a.at-b.at).map(event=>({...event,id:event.player+':'+event.at}));
 for(const {player,key,at} of notices)if(now-at<EMOTE_BUBBLE_MS)icons[player]=emoteByKey(key).icon;
 const keys=new Set(notices.map(event=>event.id));
 for(const child of [...emoteNotices.children])if(!keys.has(child.dataset.event))child.remove();
 const displayed=new Set([...emoteNotices.children].map(child=>child.dataset.event));
 for(const {id,player,key} of notices){
   if(displayed.has(id))continue;
   const {icon,message}=emoteByKey(key),notice=document.createElement('p');
   const line=document.createElement('span');
   notice.className='emote-notice hud-bubble hud-notice';notice.dataset.event=id;
   line.className='hud-notice-line';
   emoteContent(line,icon,player.toUpperCase()+' 플레이어가 '+message);
   notice.append(line);
   emoteNotices.append(notice);
 }
 const firstNotice=emoteNotices.firstElementChild;
 if(firstNotice!==measuredNotice){
   emoteNoticeSize.disconnect();
   measuredNotice=firstNotice;
   if(firstNotice)emoteNoticeSize.observe(firstNotice,{box:'border-box'});
   else emoteNotices.style.removeProperty('--notice-height');
 }
 return icons;
}
const controls=createControls({
 isDuo:()=>mode==='local',active:()=>myPlayer,canPlay:()=>s.mode==='run'&&!dlg.open,
 getState:()=>s,getViews:()=>paintedCameras,canvas,onAct:useStation,onExit:exitWorkstation,onEscape:handleEscape,onAddShell:addShellToRack
});
all('[data-work]').forEach(b=>b.onclick=()=>controls.action(b.dataset.work));
all('[data-exit]').forEach(b=>b.onclick=()=>controls.exit(b.dataset.exit));
function handleEscape(){
 if(closeEmotes(true))return;
 if(controls.exit('p1')||(mode==='local'&&controls.exit('p2')))return;
 openScreen('settings');
}
function openScreen(name){
 closeEmotes();
 controls.clear();
 if(!dlg.open){
   if(mode!=='online'&&s.mode==='run'&&name!=='result'){s.mode='paused';stopCookingHold(s);autoPaused=true;}
   dlg.showModal();
 }
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
function wheelCondition(){
 if(s.wheel.broken<0)return '바퀴 4개 장착';
 const count=1+(s.wheel.pending?.length??0);
 return '바퀴 '+count+'개 수리 필요'+(hasBrokenAxle(s)?' · 차축 손상으로 급감속':'');
}
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
   const seconds=child.cryingSeconds;
   let feeding='';
   if(seconds!=null){
     const cryingTime=seconds>0&&seconds<1?'1초 미만':Math.round(seconds*10)/10+'초';
     feeding='<br>먹이기 '+child.feeds+'회 · 울음 '+cryingTime+(child.cryPenalty?' · '+child.cryPenalty+' AP 감점':'');
   }
   return '<li><b>'+escapeHtml(child.name)+'</b><span>'+metrics+feeding+'</span><strong>'+child.points+' AP</strong></li>';
 }).join('');
 const foodScored=progress.care.some(child=>child.cryingSeconds!==undefined);
 return '<section class="intermission-section care-summary" id="royal-care" aria-labelledby="care-heading">'+
   '<div class="intermission-heading"><h3 id="care-heading">왕자·공주 돌봄 평가</h3><strong>+'+progress.earned+' 애정 포인트</strong></div>'+
   '<ul>'+entries+'</ul><p>착석 시간 60%, 평균 관심 40%를 반영합니다.'+(foodScored?' 아이마다 울었던 시간에 따라 최대 4 AP가 깎입니다.':'')+'</p></section>';
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
   damage:'내구도 '+durability()+' · '+wheelCondition(),
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
 return JSON.stringify([s.mode,position(s),humanIds(s),room?.host,room?.captain,myPlayer,s.affection,progress?.phase,progress?.round,progress?.votes,progress?.report,progress?.care]);
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
   body.innerHTML=title('갑판 조작 가이드','화면 왼쪽 절반을 누른 채 원하는 방향으로 끌면 스틱이 움직입니다. 빈 작업 원에 들어가 오른쪽 버튼으로 작업하세요. 이탈을 누르면 갑판 시점으로 돌아갑니다.')+
   row('마차 맨 앞 마부석','왼쪽 스틱으로 말의 엉덩이를 겨냥하고 채찍질을 연타하세요. 오른말은 마차를 화면 위로, 왼말은 아래로 움직입니다. 허리·목·머리를 맞히면 말이 놀라 조향이 흐트러집니다.')+
   row('폭주와 체력 회복','같은 말을 '+HORSE_RUNAWAY_HITS+'회 연달아 맞히면 그 말이 폭주해 자기 쪽으로 급하게 방향을 틉니다. 폭주 후에는 지쳐 속도가 떨어지고, 체력이 차면 다시 채찍질할 수 있습니다.')+
   row('앞길 장애물','화면 왼쪽에 빨간 느낌표가 뜨면 같은 높이에서 장애물이 나타납니다. 표시가 위쪽이면 아래로, 아래쪽이면 위로 마차를 움직이세요.')+
   row('투석기 바위','다가오는 바위 그림자에 테두리가 생기면 착탄 지점이 길 위에 고정됩니다. 반대쪽 말을 폭주시켜 급하게 틀거나, 충분히 가속해 착탄점을 앞지르세요.')+
   row('1층과 지하층','마차 6시 반 방향의 계단으로 들어가면 층이 바뀝니다. 반대쪽 층에서도 같은 위치의 계단을 이용합니다. 바로 돌아오려면 계단 밖으로 한 걸음 나왔다가 다시 들어가세요.')+
   row('타이밍 맞추기','캐릭터 위에 표시된 행동을 확인하세요. 캐릭터 둘레의 바늘이 12시 방향 초록 구간에 들어와 지금!이 뜨면 행동 버튼을 누르세요.')+
   row('이모티콘과 팀 요청','이모티콘 목록에서 선택하거나 PC에서 숫자키 1~7을 눌러 바로 보낼 수 있습니다. 한 화면 2인에서는 P2가 자기 목록을 열고 숫자키를 사용합니다. 팀 요청은 다른 층에 있는 동료에게도 화면 위쪽에 표시됩니다.')+
   row('포탄 운반','지하층 포탄 더미에서 한 발을 들고 계단으로 1층에 올라오세요. 마차 5시 방향의 파란 적재칸에 닿으면 자동으로 내려놓습니다. 포탄이 미끄러지면 원형 게이지의 색 구간에서 붙잡으세요.')+
   row('포신 청소 · 노란 원','발사 전에는 청소, 장전, 발사 순서로 하나씩 열립니다. 대포 옆 노란 원에서 행동 버튼을 연타해 청소를 끝내세요. 발사하면 청소부터 다시 시작합니다.')+
   row('장전 · 초록 원','청소를 마치고 적재칸에 포탄이 있으면 장전할 수 있습니다. 바늘이 초록 구간에 들어올 때 누르세요. 실패하면 포탄 한 발이 마차 밖으로 떨어집니다. 한 발을 시도하면 자동으로 장전대에서 이탈합니다.')+
   row('포격 · 주황 원','왼쪽 스틱으로 착탄점을 옮기고 오른쪽 버튼으로 점화하세요. 심지가 타는 2.5초 동안에도 조준할 수 있습니다. 멀리 조준할수록 조준률이 낮아지고 탄착 범위가 넓어집니다. 포탄은 직선으로 적을 관통하고 착탄점 오른쪽으로 충격파를 일으킵니다.')+
   row('왕자와 공주','아이에게 가까이 가면 관심과 배고픔, 지금 상태를 확인할 수 있습니다. 왕자는 지하층 간식 제작대를 자주 찾고, 공주는 포탄 적재칸에서 포탄을 갖고 놀다 떨어뜨릴 수 있습니다. 아이를 안아 올리고 1층의 자기 좌석으로 데려오세요.')+
   row('아이들의 울음','우는 아이가 많을수록 졸개의 이동이 느려지고 간식 만들기와 달래기가 오래 걸립니다. 타이밍 바늘은 일정하게 돌고 초록색 성공 구간이 좁아집니다.')+
   row('떨어진 아이 달래기','바닥에 떨어진 아이를 안고 있기만 해도 달래기가 진행됩니다. 걸으면 가만히 안고 있을 때보다 약 3배 빠릅니다. 지루해하면 마차 앞뒤의 다른 구역이나 지하층으로 가세요. 다시 달래서 울음을 그치면 1층의 자기 좌석에 앉힐 수 있습니다.')+
   row('간식 만들기와 먹이기','지하층 9시 반 방향 음식 제작대에서 행동 버튼을 길게 눌러 진행도를 채우세요. 충격이 클수록 진행도가 더 많이 줄어듭니다. 완성한 간식은 1층 좌석에 앉은 아이에게 먹일 수 있습니다.')+
   row('바퀴 이탈과 수리','추격대의 공격으로 내구도가 깎이면 바퀴가 빠질 수 있습니다. 같은 차축의 위아래 바퀴가 모두 빠지면 마차가 크게 느려집니다. 빠진 바퀴는 뒤로 굴러 추격대를 쳐냅니다. 지하층 12시 보관함에서 예비 바퀴를 가져와 초록색 수리 원에서 하나씩 끼우고 핀을 고정하세요.')+
   row('키보드와 마우스','작업대에서 Esc를 누르면 이탈합니다. P1은 WASD로 이동·조준, Space로 행동, Q로도 이탈할 수 있습니다. 한 화면 2인에서 P2는 방향키, Enter, 오른쪽 Shift를 사용합니다. 두 명 모두 작업 중이면 Esc로 P1부터 이탈합니다. 마부와 포수는 왼쪽 마우스 버튼을 누른 채 끌어서도 조준할 수 있습니다.')+
   row('게임패드','왼쪽 스틱으로 이동·조준하고 A 또는 R2로 행동, B로 이탈합니다.')+
   row('마차의 흔들림','충돌, 왕국군의 일격, 대포 반동은 마차와 대포 조준을 흔듭니다. 충격이 크면 간식 제작 진행도가 줄어듭니다.')+
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
function resetLocal(m){controls.clear();pendingActs.length=0;mode=m;myPlayer='p1';room=null;netFailed=0;if(s.mode!=='ready'&&s.mode!=='run'&&s.mode!=='paused')s=initial();setHumanSlots(s,m==='local'?['p1','p2']:['p1']);closeScreen();toast(m==='local'?'한 화면 협동으로 전환했습니다.':'1인 갑판 연습으로 전환했습니다.');}
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
 if(stageChanged||(data.state.mode==='paused'&&s.mode==='run')){controls.clear();lastInputKey='';lastInputAt=0;}
 const prevCaptain=room.captain,previous=humanIds(s).join()+prevCaptain,prevMode=s.mode;room.host=data.host;room.captain=data.captain;room.rev=data.rev;room.stamp=data.stamp;room.crew=data.crew;myPlayer=data.player;s=data.state;
 room.receivedAt=performance.now()-Math.min(350,latency/2);
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
async function addShellToRack(){
 if(!room){addRackShell(s);return;}
 const currentRoom=room,now=performance.now();
 try{
   const data=await transport('add-rack-shell',position(s),currentRoom);
   if(room===currentRoom)accept(data,performance.now()-now);
 }catch(error){if(room===currentRoom)toast(error.message);}
}
async function start(testMode=false){
 if(testMode&&(mode==='online'||s.mode!=='ready'))return;
 if(s.mode==='clear'||s.mode==='lost'){openScreen('result');return;}
 if(mode==='online'){if(!room.host){toast('방장이 출발과 일시 정지를 맡습니다.');return;}await send(s.mode==='ready'?'start':'pause');}
 else if(s.mode==='ready'){begin(s,{testMode});tone(500);}
 else{
   s.mode=s.mode==='run'?'paused':'run';
   if(s.mode==='paused')stopCookingHold(s);
 }
 controls.clear();lastInputKey='';lastTick=performance.now();accum=0;
}
$('start').onclick=()=>start();
$('test-start').onclick=()=>start(true);
$('test-stop').onclick=()=>{
 if(!s.testMode||mode==='online')return;
 s=initial();
 setHumanSlots(s,mode==='local'?['p1','p2']:['p1']);
 controls.clear();pendingActs.length=0;lastInputKey='';lastTick=performance.now();accum=0;
 nextWhimper=0;lastCatapultLanding='';lastCannonLanding='';lastSerial=0;
 closeScreen();
};
function useStation(id,input){
 if(s.mode!=='run'||dlg.open||pendingActs[0]?.op==='exit-station')return;
 const p=s.players[id],role=stationAt(p,s);
 if(!role||role==='cook')return;
 setInput(s,id,input);
 const info=workInfo(s,p,role);
 if(!info.action||info.blocked)return;
 const royalId=role==='royal-feed'?feedTarget(s,p)?.id:
   ['royal-pickup','royal-seat'].includes(role)?royalTarget(s,p)?.id:null;
 if(mode==='online'){
   const now=performance.now();
   const act={op:'act',tick:s.tick,...position(s),input,role,royalId,queuedAt:now};
   if(role==='cleaner'){
     const cannonRound=s.cannonRecoil?.tick??0;
     const queued=pendingActs[0];
     const sameRound=queued?.role===role&&sameStage(queued,s)&&queued.cannonRound===cannonRound;
     // Keep each press while a previous request is in flight.
     const taps=sameRound?Math.min(CANNON_CLEAN_TAPS,queued.taps+1):1;
     pendingActs.splice(0,pendingActs.length,{...act,cannonRound,taps});
   }else{
     if(role!=='driver')pendingActs.length=0;
     if(pendingActs.length<4)pendingActs.push(act);
   }
   pumpInput(now,input);return;
 }
 if(!operate(s,id))return;
 const msg=s.players[id].note||s[feedbackKey(role)+'Msg']||'';
 tone(msg.includes('실패')?130:460,.075);
}
function exitWorkstation(id){
 if(s.mode!=='run'||dlg.open)return;
 const p=s.players[id],role=occupiedStation(p,s);
 if(!role)return;
 if(mode!=='online'){leaveStation(s,id);return;}
 pendingActs.length=0;
 const now=performance.now();
 pendingActs.push({op:'exit-station',...position(s),role,queuedAt:now});
 pumpInput(now,controls.input('p1'));
}
async function pumpInput(now,input){
 if(mode!=='online'||!room||inputBusy||actBusy||s.mode!=='run')return;
 const key=inputKey(input);
 const moving=Math.abs(input.mx)+Math.abs(input.my)>.01;
 const repeatAfter=input.cookHeld?300:moving?460:Infinity;
 const player=s.players[myPlayer],currentRole=stationAt(player,s);
 while(pendingActs.length){
   const next=pendingActs[0];
   const exiting=next.op==='exit-station';
   const cleaning=next.role==='cleaner'&&!exiting;
   const staleWhip=next.role==='driver'&&!exiting&&next.input?.whipSequence!==(player.whipSwing?.sequence??0);
   const currentRound=!cleaning||next.cannonRound===(s.cannonRecoil?.tick??0);
   const recent=cleaning||now-next.queuedAt<=600;
   const role=exiting?occupiedStation(player,s):currentRole;
   const sameRole=next.role===role||(exiting&&isCannonRole(next.role)&&isCannonRole(role));
   if(sameStage(next,s)&&currentRound&&!staleWhip&&sameRole&&(exiting||recent))break;
   pendingActs.shift();
 }
 const act=pendingActs.shift();
 if(!act&&(now-lastInputAt<90||key===lastInputKey&&now-lastInputAt<repeatAfter))return;
 const sentInput=act?.input?{...act.input,mx:input.mx,my:input.my}:input;
 lastInputKey=inputKey(sentInput);lastInputAt=now;inputBusy=true;
 const token=room.token;
 try{
   const extra={...position(act??s),input:sentInput};
   if(act?.tick!==undefined)extra.tick=act.tick;
   if(act?.role)extra.role=act.role;
   if(act?.royalId)extra.royalId=act.royalId;
   if(act?.taps!==undefined){extra.taps=act.taps;extra.cannonRound=act.cannonRound;}
   const data=await transport(act?.op??'input',extra);
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
 const showRoute=!s.testMode&&(playing||s.mode==='clear');
 const showTest=playing&&!!s.testMode;
 const route=$('campaign-route');
 updateProperty($('campaign-hud'),'hidden',!showRoute&&!showTest);
 updateProperty($('test-mode-label'),'hidden',!showTest);
 updateProperty(route,'hidden',!showRoute);
 const left=Math.max(0,s.goal-s.distance);
 const remaining=s.goal>0?Math.min(100,left/s.goal*100):0;
 if(showRoute){
   const bar=$('campaign-progress');
   updateProperty(bar,'value',remaining);
   updateAttribute(bar,'aria-valuetext',Math.ceil(left)+' m 남음');
   updateStyle(route,'--route-remaining',remaining+'%');
 }
 if(screen!=='status')return;

 const {chapter,stage}=position(s),stageInfo=stageSpec(chapter,stage);
 const launched=s.wave??0,elapsed=s.tick/HZ;
 const wave='웨이브 '+launched+(s.testMode?'':'/'+stageInfo.waves.length);
 const traveled=Math.floor(s.distance);
 const shownDistance=s.testMode?traveled:Math.ceil(left);
 const distance=shownDistance.toLocaleString()+' m';
 const chapterInfo=chapterSpec(chapter);
 label($('chapter-no'),'챕터 '+chapter);label($('chapter-name'),chapterInfo.title);
 label($('stage-no'),'스테이지 '+stage+' / '+chapterInfo.stages.length);label($('stage-name'),stageInfo.title);
 label($('wave-no'),wave);label($('dist'),distance);
 label($('distance-label'),s.testMode?'주행 거리':'남은 거리');
 updateProperty($('status-distance-line'),'hidden',!!s.testMode);
 label($('stage-clock'),'주행 '+traveled.toLocaleString()+(s.testMode?'':' / '+s.goal.toLocaleString())+' m · '+clockText(Math.floor(elapsed))+' 경과');
 updateProperty($('bar').style,'width',(s.testMode?0:100-remaining)+'%');
}
const floorName=floor=>floor===CELLAR_FLOOR?'지하층':'1층';
let campaignHudBottom=0;
const campaignHudBox=$('campaign-hud');
const playTools=$('settings-button').parentElement;
let playToolsBounds=null;
function measureTopHud(){
 campaignHudBottom=campaignHudBox.getBoundingClientRect().bottom;
 const tools=playTools.getBoundingClientRect(),world=canvas.getBoundingClientRect();
 playToolsBounds={left:tools.left-world.left,bottom:tools.bottom-world.top};
 if(campaignHudBottom)updateStyle(emoteNotices,'--campaign-notice-floor',campaignHudBottom+6+'px');
 else emoteNotices.style.removeProperty('--campaign-notice-floor');
}
const topHudSize=new ResizeObserver(measureTopHud);
topHudSize.observe(campaignHudBox);
topHudSize.observe(playTools);
window.addEventListener('resize',measureTopHud);
const hudSlots=['p1','p2'];
const timingPrompts=Object.fromEntries(hudSlots.map(slot=>{
 const prompt=$('timing-'+slot);
 return [slot,{prompt,band:prompt.querySelector('.timing-band'),needleEl:prompt.querySelector('.timing-needle'),
   text:prompt.querySelector('.timing-label'),feedback:prompt.querySelector('.timing-feedback')}];
}));
function showTimingPrompt(slot,job,anchors,running){
 const {prompt,band,needleEl,text,feedback}=timingPrompts[slot],anchor=job&&anchors[job.id];
 updateProperty(prompt,'hidden',!job?.timed||!anchor);
 if(prompt.hidden)return;
 const {p,role,info,needle,ready}=job;
 const {steadyX:x,steadyY:y,steadyScale:scale,left,right,top,bottom}=anchor;
 const radius=Math.max(24,Math.min(scale*29,x-left-8,right-x-8,y-top-54,bottom-y-20));
 const labelInset=Math.min(106,(right-left)/2);
 const labelOffset=Math.max(left+labelInset,Math.min(right-labelInset,x))-x;
 const labelBelow=y-radius-49<campaignHudBottom+4;
 updateStyle(prompt,'--timing-x',x+'px');
 updateStyle(prompt,'--timing-y',y+'px');
 updateStyle(prompt,'--timing-size',radius*2+'px');
 updateStyle(prompt,'--timing-label-x',labelOffset+'px');
 updateStyle(prompt,'--timing-label-width',Math.max(0,right-left-12)+'px');
 if(labelBelow)updateStyle(prompt,'--timing-label-gap',Math.max(5,campaignHudBottom+4-y-radius)+'px');
 updateProperty(prompt.dataset,'labelBelow',String(labelBelow));
 updateProperty(prompt.dataset,'ready',String(ready));
 updateProperty(prompt.dataset,'paused',String(!running));
 const bandPercent=width(s,role)*100;
 updateAttribute(band,'stroke-dasharray',bandPercent+' '+(100-bandPercent));
 updateAttribute(band,'transform','rotate('+(-90-bandPercent*1.8)+' 50 50)');
 updateAttribute(needleEl,'transform','rotate('+((needle-target(s,role))*360)+' 50 50)');
 label(text,!running?info.action+' · 일시 정지':(ready?'지금! ':'초록 구간에서 ')+info.action);
 const result=p.noteUntil>s.tick&&p.note===s[feedbackKey(role)+'Msg']?p.note:'';
 updateProperty(feedback,'hidden',!result);
 label(feedback,result);
}
const workHints=Object.fromEntries(hudSlots.map(slot=>{
 const bubble=$('work-hint-'+slot);
 return [slot,{
   bubble,text:bubble.querySelector('.work-hint-text'),countdown:bubble.querySelector('.work-countdown'),
   progressRow:bubble.querySelector('.work-progress'),progress:bubble.querySelector('progress'),
   progressName:bubble.querySelector('.work-progress-name'),percent:bubble.querySelector('.work-progress-value'),
 }];
}));
const workHintSizes=new WeakMap();
const workHintSize=new ResizeObserver(entries=>{
 for(const {target,borderBoxSize} of entries){
   const size=borderBoxSize?.[0];
   workHintSizes.set(target,{width:size?.inlineSize??target.offsetWidth,height:size?.blockSize??target.offsetHeight});
 }
});
for(const {bubble} of Object.values(workHints))workHintSize.observe(bubble,{box:'border-box'});
const workProgressLabels={
 cook:'간식 제작 진행도',soothe:'아이 달래기 진행도','soothe-bored':'아이 달래기 진행도 일시 정지',
 'horse-runaway':'말 폭주까지 진행도','horse-burst':'말 폭주 진행도','horse-recovery':'말 체력 회복 진행도',
 'cannon-accuracy':'조준률','cannon-cleaning':'포신 청소 진행도',
};
function showWorkHint(slot,job,anchors,view,canvasSize,sharePane){
 const {bubble,text,countdown,progressRow,progress,progressName,percent}=workHints[slot];
 const info=job?.info;
 updateProperty(bubble,'hidden',!info?.hint);
 if(bubble.hidden)return;
 // The zoomed gunner camera can frame the cannon while leaving the player off-screen.
 let anchor=anchors[job.id];
 if(!anchor){
   const ratio=canvasSize.width/view.screenWidth;
   const left=view.start*ratio,right=(view.start+view.span)*ratio;
   anchor={x:(left+right)/2,y:canvasSize.height*.62,scale:1,left,right,top:0};
 }
 const {x,y,scale,top}=anchor;
 const middle=(anchor.left+anchor.right)/2;
 const left=sharePane&&slot==='p2'?middle:anchor.left;
 const right=sharePane&&slot==='p1'?middle:anchor.right;
 const maxWidth=Math.max(0,right-left-12);
 const size=workHintSizes.get(bubble);
 const width=Math.min(size?.width||1050,maxWidth);
 const height=size?.height||96;
 const inset=6+width/2;
 const bubbleX=Math.max(left+inset,Math.min(right-inset,x));
 const offset=Math.max(23,Math.min(75,scale*28));
 const cooking=info.mode==='cook';
 const value=cooking?cookingProgress(s,job.p):info.progress;
 const topEdge=Math.max(top+6,campaignHudBottom+4);
 const below=y-offset-height<topEdge;
 const bubbleY=below?Math.max(topEdge,Math.min(y+offset,canvasSize.height-height-6)):y-offset;
 updateProperty(bubble.dataset,'below',String(below));
 updateProperty(bubble.dataset,'compact',String(sharePane||view.span<view.screenWidth));
 updateStyle(bubble,'--hint-x',bubbleX+'px');
 updateStyle(bubble,'--hint-y',bubbleY+'px');
 updateStyle(bubble,'--hint-width',maxWidth+'px');
 label(text,info.hint);
 updateProperty(countdown,'hidden',info.seconds===undefined);
 if(!countdown.hidden)label(countdown,' · '+info.seconds.toFixed(1)+'초');
 updateProperty(progressRow,'hidden',value===undefined);
 if(!progressRow.hidden){
   const kind=info.progressKind??(cooking?'cook':'soothe');
   if(progressRow.dataset.kind!==kind){
     progressRow.dataset.kind=kind;
     progress.ariaLabel=workProgressLabels[kind];
     progressName.hidden=kind!=='cannon-accuracy';
   }
   if(progress.value!==value)progress.value=value;
   label(percent,Math.floor(value*100)+'%');
 }
}
const royalWatch=Object.fromEntries(hudSlots.map(slot=>{
 const panel=$('royal-watch-'+slot);
 const rows=Object.fromEntries(Array.from(panel.children,row=>[row.dataset.royal,{row,children:Array.from(row.children)}]));
 return [slot,{panel,rows}];
}));
const playerHud=Object.fromEntries(hudSlots.map(slot=>{
 const pads=$('pads-'+slot);
 return [slot,{pads,work:$('work-'+slot),context:pads.querySelector('.context'),
   stick:$(slot+'-stick'),stickLabel:$(slot+'-stick-label'),action:$(slot+'-action'),exit:$(slot+'-exit'),
   ident:$(slot+'-ident'),location:$(slot+'-location'),hint:$(slot+'-hint')}];
}));
function showRoyalWatch(slot,player,duo){
 const {panel,rows}=royalWatch[slot];
 const panelLabel=(duo?slot.toUpperCase()+' ':'')+'가까운 아이 상태';
 if(panel.ariaLabel!==panelLabel)panel.ariaLabel=panelLabel;
 let watching=false;
 for(const child of s.royals){
   const {row,children}=rows[child.id];
   updateProperty(row,'hidden',!royalStatusNearby(player,child));
   if(row.hidden)continue;
   watching=true;
   const [name,meters,condition]=children;
   const posture=child.shellPlayUntil?'포탄을 갖고 노는 중':child.snackPauseUntil?'간식을 기다리는 중':
     {fallen:'넘어짐',wandering:'돌아다니는 중',carried:'안겨 있음',seated:'착석 중'}[child.state];
   const mood=child.bored?'지루해함':child.upset?(child.state==='carried'?'안겨서 우는 중':'놀라서 우는 중'):child.crying?'배고파서 우는 중':posture??'';
   updateProperty(row.dataset,'needsCare',String(child.crying||child.state==='fallen'));
   label(name,child.name);
   label(meters,'관심 '+Math.floor(child.attention)+'% · 배고픔 '+Math.floor(child.hunger??0)+'%');
   label(condition,mood);
 }
 updateProperty(panel,'hidden',!watching);
 return watching;
}
const catapultAlertPanes=all('.catapult-alert-pane');
function updateCatapultAlerts(alerts){
 for(const [i,pane] of catapultAlertPanes.entries()){
   const alert=alerts[i];
   updateProperty(pane,'hidden',!alert);
   if(!alert)continue;
   updateStyle(pane,'left',alert.left+'px');
   updateStyle(pane,'width',alert.width+'px');
   updateStyle(pane,'--alert-x',alert.x+'px');
   updateStyle(pane,'--alert-y',alert.y+'px');
 }
}
function hud(cameras,{anchors,catapultAlerts},canvasSize){
 const focused=cameras.some(v=>v.role&&v.role!=='driver'),duo=mode==='local';
 const running=s.mode==='run',playing=running||s.mode==='paused';
 const jobs=(duo?hudSlots:[myPlayer]).map(id=>{
   const p=s.players[id],role=playing?stationAt(p,s):null;
   const info=role?workInfo(s,p,role):null,timed=!!info&&!info.mode;
   const needle=role==='catch'?catchPhase(p,s.tick):timed?phase(s,role):0;
   const ready=timed&&running&&!info.blocked&&hit(s,role,needle)&&(role==='catch'||!(s[feedbackKey(role)+'Cd']>s.tick));
   return {id,p,role,info,timed,needle,ready,floor:playerFloor(p)};
 });
 const outside=cameras.some(view=>view.floor!==CELLAR_FLOOR);
 const sceneLabel=outside&&cameras.every(view=>view.floor!==CELLAR_FLOOR)?'왼쪽을 향해 달리는 마차와 뒤에서 쫓아오는 왕국군':outside?'마차 1층과 지하층 분할 화면':'마차 지하층 내부';
 const roadWarning=cameras.map(view=>roadApproachWarning(s,view)).find(Boolean);
 const roadNotice=roadWarning?' · 앞길 '+(roadWarning.y<LANES[1]?'위쪽':'아래쪽')+'에 장애물 등장 예정':'';
 const pursuing=playing&&cameras.some(view=>view.floor!==CELLAR_FLOOR&&(!view.role||view.role==='driver'||view.role==='gunner')&&incomingPursuit(s,view));
 const pursuitNotice=pursuing?' · 오른쪽에서 왕국군 접근 중':'';
 updateCatapultAlerts(catapultAlerts);
 const catapultNotice=catapultAlerts.length?' · 투석기 착탄 위험':'';
 updateAttribute(canvas,'aria-label',sceneLabel+roadNotice+pursuitNotice+catapultNotice);
 $('desk').classList.toggle('focusing',focused);canvas.parentElement.classList.toggle('split',cameras.length===2);
 const panels=$('work-panels');panels.classList.toggle('duo',duo);
 const shareHintPane=cameras.length===1&&jobs.length===2&&jobs.every(job=>job.info?.hint);
 let showPanels=false;
 for(const [i,slot] of hudSlots.entries()){
   const job=jobs[i];
   showTimingPrompt(slot,job,anchors,running);
   showWorkHint(slot,job,anchors,cameras[i]??cameras[0],canvasSize,shareHintPane);
   const watching=showRoyalWatch(slot,playing?job?.p:null,duo);
   updateProperty(playerHud[slot].work,'hidden',!watching);
   showPanels||=watching;
 }
 updateProperty(panels,'hidden',!showPanels);
 campaignHud(playing);
 const clearLabel=s.mode==='clear'&&nextStagePosition(s.chapter??1,s.stage)?(s.intermission?.phase==='shop'?'팀 정비 상점':'마왕에게 보고'):'작전 결과';
 const startLabel=({ready:'작전 시작',run:'일시 정지',paused:'계속 달리기',clear:clearLabel,lost:'작전 결과'})[s.mode];
 const icon=s.mode==='run'?'<path d="M7 5h3v14H7zm7 0h3v14h-3z"/>':'<path d="m6 4 14 8-14 8z"/>';
 const startButton=$('start');
 updateProperty($('test-start'),'hidden',mode==='online'||s.mode!=='ready');
 updateProperty($('test-stop'),'hidden',!s.testMode||mode==='online');
 if(lastStatus!==startLabel){
   startButton.innerHTML='<svg viewBox="0 0 24 24" width="15" height="15" fill="currentColor">'+icon+'</svg><span>'+startLabel+'</span>';
   startButton.ariaLabel=startLabel;startButton.title=startLabel;
   startButton.dataset.mode=s.mode;
   startButton.classList.toggle('pause-only',s.mode==='run');
   lastStatus=startLabel;
 }
 updateProperty(startButton,'disabled',mode==='online'&&!room.host&&!['clear','lost'].includes(s.mode));
 if(screen==='status'){
   const modeLabel=s.testMode?'테스트 모드':mode==='online'?(netFailed>1?'재연결 중':room.code+' · '+myPlayer.toUpperCase()):mode==='local'?'한 화면 2인 협동':'1인 연습 모드';
   label($('mode-label'),modeLabel);
   label($('hp'),durability());label($('affection-readout'),(s.affection??0)+' AP');
   markup($('speed'),(s.mode==='ready'?0:Math.round(s.speed))+' <small>m/s</small>');
   const humans=humanIds(s);
   for(const [id,p] of Object.entries(s.players)){
     const human=humans.includes(id);
     label($(id+'-crew-name'),human?(id===myPlayer?'내 졸개':'플레이어'):'자동 동료');
     const role=stationAt(p,s),assignment=role||(!human?p.job:null);
     const activity=p.ball?'포탄 운반 중':assignment?.startsWith('royal-')?'아이 돌봄':ROLES[assignment]?(ROLES[assignment]+(role?'':' 이동 중')):'';
     label($(id+'-crew-job'),activity?floorName(playerFloor(p))+' · '+activity:floorName(playerFloor(p)));
   }
   const cannonStatus={cleaner:'포신 청소 필요',loader:'장전 대기',gunner:'대포 장전됨'}[cannonStation(s)];
   label($('ammo-readout'),'지하층 포탄 더미 '+s.ammo.stock+'발 · 1층 적재칸 '+s.ammo.rack+'발 · '+cannonStatus+' · 손실 '+s.ammo.lost+'발');
   const broken=WHEELS[s.wheel.broken];
   const wheelStep=broken?(s.wheel.progress?'차축 핀 고정':Object.values(s.players).some(player=>player.spareWheel)?
     '예비 바퀴 운반 중':'지하층 12시 보관함에서 바퀴 가져오기'):'';
   label($('wheel-readout'),wheelCondition()+(broken?' · '+broken.name+' · '+wheelStep:''));
 }
 updateProperty(playerHud.p2.pads,'hidden',!duo);playerHud.p1.pads.parentElement.classList.toggle('duo',duo);
 for(const [i,{id,p,role,info,ready,floor}] of jobs.entries()){
   const {ident,stick,stickLabel,action,exit,location,context,hint}=playerHud[hudSlots[i]],playerName=id.toUpperCase();
   const seat=playing?occupiedStation(p,s):null;
   const atDriver=seat==='driver',atGun=seat==='gunner';
   const aiming=atDriver||atGun;
   const idle=!!seat&&!aiming||role==='catch';
   updateProperty(stick,'disabled',!running||idle);
   updateProperty(stick.dataset,'mode',aiming?'aim':idle?'idle':'move');
   updateProperty(stick,'ariaLabel',playerName+(atDriver?' 말 조준 스틱':atGun?' 대포 조준 스틱':idle?' 스틱 대기':' 이동 스틱'));
   label(stickLabel,aiming?'조준':idle?'대기':'이동');
   updateProperty(action,'disabled',!running||!info?.action||Boolean(info.blocked));
   updateProperty(action.dataset,'ready',String(ready));
   updateStyle(action,'--action',info?.color??'');
   label(action,info?.action??'행동');
   updateProperty(exit,'hidden',!seat);
   updateProperty(exit,'disabled',!running);
   label(ident,playerName);ident.classList.toggle('second',id==='p2');
   label(location,ROLES[role]||floorName(floor));
   const note=p.noteUntil>s.tick?p.note:'';
   context.classList.toggle('visible',running&&!role&&!!note);
   label(hint,note);
 }

}
let lastPresentedAt=0,sceneScale=1,qualitySamples=0,latePaints=0,stableWindows=0;
function paint(now){
 const gap=now-lastPresentedAt;
 lastPresentedAt=now;
 if(s.mode==='run'&&!document.hidden&&gap>0){
   qualitySamples++;
   if(gap>42)latePaints++;
   if(qualitySamples===24){
     if(latePaints>=8)sceneScale=Math.max(.67,sceneScale-.12);
     stableWindows=latePaints===0?stableWindows+1:0;
     if(stableWindows>=5){
       sceneScale=Math.min(1,sceneScale+.06);
       stableWindows=0;
     }
     qualitySamples=0;latePaints=0;
   }
 }else{
   qualitySamples=0;latePaints=0;stableWindows=0;
 }
 const canvasSize=canvas.getBoundingClientRect();
 const reduceMotion=reducedMotion.matches;
 const targets=cameraViews(s,mode==='local',myPlayer,canvasSize.width,canvasSize.height,reduceMotion);
 paintedCameras=transitionCamera(targets,now,reduceMotion);
 hud(paintedCameras,render(canvas,s,now,paintedCameras,updateEmoteDisplay(now),canvasSize,sceneScale,playToolsBounds),canvasSize);
}
function frame(now){
 const dt=Math.min(.2,(now-lastTick)/1000);lastTick=now;
 if(s.mode==='run'&&!(mode==='online'&&netFailed>=2)){
   const gamepads=controls.readGamepads();
   const first=controls.input('p1',gamepads),second=mode==='local'?controls.input('p2',gamepads):null;
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
 const paintElapsed=now-lastPaint;
 if(paintElapsed>=paintInterval-paintTolerance){
   // Keep the paint clock's phase when the display interval is not a divisor of 30 Hz.
   lastPaint+=Math.floor((paintElapsed+paintTolerance)/paintInterval)*paintInterval;
   paint(now);
 }
 const catapultHit=playCatapultLanding();
 playCannonLanding();
 if(s.serial!==lastSerial){if(s.serial>lastSerial&&!catapultHit)tone(105,.16,'triangle');lastSerial=s.serial;}
 playRoyalWhimper(now);
 const terminal=['lost','clear'].includes(s.mode)?(s.chapter??1)+'/'+s.stage+'/'+s.mode:'';
 if(terminal&&terminal!==terminalSeen){openScreen('result');tone(s.mode==='clear'?700:200,.24);}terminalSeen=terminal;
 requestAnimationFrame(frame);
}
// Browsers suspend animation in background tabs. Keep an open room seat alive.
setInterval(()=>{if(room&&document.hidden&&performance.now()-lastSync>8000)sync();},10_000);
lastPaint=performance.now();
paint(lastPaint);requestAnimationFrame(frame);
let saved;try{saved=JSON.parse(sessionStorage.getItem('gg-room'));}catch{}
if(saved&&(isRoomNumber(saved.code)||isLegacyRoomCode(saved.code))&&(!invitation||invitation===saved.code))connect('join',saved.code,saved);else if(invitation)openScreen('crew');
