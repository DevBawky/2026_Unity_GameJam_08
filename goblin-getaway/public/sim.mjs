import {CHAPTERS,chapterSpec,stageSpec,nextStagePosition,ENEMY_PROFILES} from './campaign.mjs';
import {ROAD_OBSTACLE_TYPES,obstacleY,roadObstacleProfile} from './road-obstacles.mjs';
import {finishCare,maxDurability,newCare,pruneVotes,recordCare,recordReseat,UPGRADE_CARDS} from './intermission.mjs';
export {obstacleY} from './road-obstacles.mjs';

export const HZ = 30;
export const STEP = 1 / HZ;
export const LANES = [82, 182, 282];
const [CART_TOP, CART_CENTER, CART_BOTTOM] = LANES;
export const ROAD_TOP = CART_TOP - 134;
export const ROAD_BOTTOM = CART_BOTTOM + 134;
export const DECK = {left:363,right:694,top:-81,bottom:81};
export const STATIONS = {
 driver:{x:385,y:0,r:26},gunner:{x:658,y:3,r:26},
 rack:{x:616,y:41,r:19},loader:{x:622,y:-39,r:21},
 supply:{x:536,y:62,r:23}
};
export const ROYAL_SEATS={
 prince:{x:510,y:-74,dropY:-34},
 princess:{x:549,y:-74,dropY:-34}
};
export const ROYAL_REACH=30, ROYAL_RETURN_REACH=18;
export const FUSE_TICKS=75;
export const CATCH_TICKS=48;
export const ENEMY_RETREAT_TICKS=24;
export const ENEMY_RECOVERY_TICKS=42;
export const CATAPULT_RADIUS={x:35,y:24};
const CATAPULT_FIRST=6*HZ, CATAPULT_APPROACH=33, CATAPULT_FLIGHT=144, CATAPULT_LINGER=21;
const CART_HULL={left:340,right:715,halfHeight:100};
const CATAPULT_TARGET_X=718, ROAD_UNITS_PER_METER=4;
const WALK_SPEED=165, CARRY_SPEED=50, STEERING_RATE=4.2;
const ENEMY_STRIKE_X=773, ENEMY_RETREAT_SPEED=54, ENEMY_RECOVERY_SPEED=.4;
const ENEMY_TRACK_X=1040, ENEMY_TRACK_SPEED=30, ENEMY_STRIKE_REACH=46;
export const WAVE_ENTRY_X=1280;
const WAVE_SPACING=56;
const WHIP_COOLDOWN=3, HORSE_REST_TICKS=HZ*3, HORSE_HEAL_TICKS=24;
const HORSE_TIRED_WOUNDS=5, HORSE_ANGRY_WOUNDS=8;
const HORSE_SIDES=['right','left'];
const ROYAL_BODY=22, ROYAL_WALK=56, ROYAL_CARRY=125, ROYAL_ATTENTION_LOW=20;
const ROYAL_REST=7*HZ, ROYAL_BOT_GRACE=9*HZ;
const DEFAULT_TRAITS={spring:0,rein:0,sight:0,steady:0,barrel:0,hull:0,porter:0};
const baseSpeed=s=>13+3*s.traits.rein;
export const HORSE_NAMES={right:'오른말',left:'왼말'};
export const ROLES={driver:'마부',gunner:'포수',supply:'포탄 운반',deliver:'포탄 적재',carry:'포탄 운반',catch:'포탄 붙잡기','royal-pickup':'왕실 안아 올리기','royal-carry':'왕실 데려가기','royal-seat':'왕실 다시 앉히기',repair:'바퀴 수리'};
function supplies(){return {stock:17,rack:1,loaded:true,lost:0};}
function passengers(){
 return [['prince','왕자',75],['princess','공주',63]].map(([id,name,attention])=>{
   const {x,y}=ROYAL_SEATS[id];
   return {id,name,attention,x,y,state:'seated',carrier:null,restUntil:0,mischief:null,destination:null};
 });
}
function horseWear(){return Object.fromEntries(HORSE_SIDES.map(side=>[side,{wounds:0,lastHit:-9999,angryUntil:0}]));}

export const BARREL = {x:704,y:0,length:44};
export const cannonScale=s=>1+.17*(s.traits?.barrel??0);
export const WHEELS = [
 {x:405,y:-72,name:'앞쪽 위 바퀴'}, {x:650,y:-72,name:'뒤쪽 위 바퀴'},
 {x:405,y:72,name:'앞쪽 아래 바퀴'}, {x:650,y:72,name:'뒤쪽 아래 바퀴'}
];
export const repairLabels=['바퀴 끼우기','차축 핀 고정'];
function wheelState(){return {broken:-1,progress:0,since:0,first:true,eligible:110};}
export function feedbackKey(role){return role==='gunner'?'gun':role==='repair'?'fix':role==='driver'?'drive':role;}

const clamp=(v,min,max)=>Math.max(min,Math.min(max,v));
const bench=(x,y)=>x>468&&x<583&&y< -48;
function actor(x,y,face){return {x,y,mx:0,my:0,ax:face,ay:0,until:0,ball:false,royal:null,job:null};}
function crew(){return {p1:actor(480,10,-1),p2:actor(614,11,1),p3:actor(556,50,1),p4:actor(500,-28,-1)};}
function campaignLocation(chapter,stage){
 const chapterNumber=CHAPTERS[chapter-1]?chapter:1;
 const count=chapterSpec(chapterNumber).stages.length;
 return {chapter:chapterNumber,stage:Number.isInteger(stage)&&stage>0?(stage-1)%count+1:1};
}
export const PLAYER_IDS=['p1','p2','p3','p4'];
export function humanIds(s){return s.humanSlots??PLAYER_IDS.slice(0,s.humans??1);}
export function setHumanSlots(s,slots){
 const previous=humanIds(s);
 for(const id of PLAYER_IDS){
   if(previous.includes(id)===slots.includes(id))continue;
   const p=s.players[id];p.mx=0;p.my=0;p.until=0;p.job=null;
 }
 s.humanSlots=[...slots];s.humans=slots.length;
 pruneVotes(s,slots);
}
export function ensureDeck(s){
 if(s.chapter===undefined||s.wave===undefined){
   Object.assign(s,campaignLocation(s.chapter,s.stage));
   const stage=stageSpec(s.chapter,s.stage);
   // Keep live legacy enemies, but skip waves that would already have launched.
   s.wave=stage.waves.filter(wave=>wave.at*HZ<=s.tick).length;
   s.goal=stage.goal;
   s.distance=s.mode==='clear'?s.goal:Math.min(s.distance,s.goal);
   if(s.mode==='ready')s.enemies=[];
   delete s.nextEnemy;
 }
 if(!s.players)s.players=crew();
 if(!s.players.p3){const fresh=crew();s.players.p3=fresh.p3;s.players.p4=fresh.p4;}
 if(!s.horseWear){
   s.horseWear=horseWear();
   for(const p of Object.values(s.players))if(HORSE_SIDES.includes(p.job))p.job=null;
 }
 if(s.royals?.length!==2||!s.royals.every(r=>ROYAL_SEATS[r.id]&&Number.isFinite(r.attention))){
   const old=s.royals;
   s.royals=passengers();
   for(const [i,royal] of s.royals.entries()){
     if(Number.isFinite(old?.[i]?.stress))royal.attention=clamp(100-old[i].stress,0,100);
     royal.restUntil=s.tick+2*HZ;
   }
   for(const p of Object.values(s.players))if(p.job==='royal')p.job=null;
 }
 for(const p of Object.values(s.players))p.royal??=null;
 s.horses??={right:0,left:0};s.ammo??=supplies();s.humans??=1;
 if(!s.wheel){s.wheel=wheelState();s.fixCd=0;s.fixMsg='';s.fixUntil=0;}
 if(!Number.isFinite(s.gunAngle))s.gunAngle=0;
 if(s.ammo.loaded===undefined){
   const rounds=s.ammo.magazine??0;
   s.ammo.loaded=rounds>0;s.ammo.rack=Math.max(0,rounds-1);s.ammo.lost=0;
   delete s.ammo.magazine;
 }
 s.fuse??={end:0,owner:null};s.falling??=[];
 s.catapult??=null;
 s.nextCatapult??=Math.max(CATAPULT_FIRST,s.tick+2*HZ);
 s.traits??={};
 for(const [trait,level] of Object.entries(DEFAULT_TRAITS))s.traits[trait]??=level;
 s.affection??=0;
 s.stageEntry??={hp:maxDurability(s),wheel:wheelState()};
 s.care??=newCare();
 s.intermission??=null;
 if(s.mode==='clear')finishCare(s);
 return s;
}
export const onSpot=(p,spot)=>Math.hypot(p.x-spot.x,p.y-spot.y)<=spot.r;
export const canLoad=s=>!s.ammo.loaded&&s.ammo.rack>0;
export function royalTarget(s,p){
 if(!p||p.ball)return null;
 if(p.royal)return s.royals.find(r=>r.id===p.royal&&r.state==='carried')??null;
 let target=null,reach=ROYAL_REACH;
 for(const royal of s.royals){
   if(royal.state!=='wandering')continue;
   const distance=Math.hypot(p.x-royal.x,p.y-royal.y);
   if(distance<=reach){target=royal;reach=distance;}
 }
 return target;
}
export function stationAt(p,s){
 if(!p)return null;
 if(p.ball)return p.slipUntil?'catch':'carry';
 const royal=royalTarget(s,p);
 if(royal){
   if(!p.royal)return 'royal-pickup';
   const seat=ROYAL_SEATS[royal.id];
   return Math.hypot(p.x-seat.x,p.y-seat.dropY)<=ROYAL_RETURN_REACH?'royal-seat':'royal-carry';
 }
 const wheel=WHEELS[s?.wheel?.broken];
 if(wheel&&Math.hypot(p.x-wheel.x,p.y-wheel.y)<=28)return 'repair';
 for(const [role,spot] of Object.entries(STATIONS)){
   if(!onSpot(p,spot)||role==='rack'||(role==='loader'&&!canLoad(s)))continue;
   return role;
 }
 return null;
}
export function aimAngle(p){return clamp(Math.atan2(p.ay,Math.max(.12,p.ax)),-1.06,1.06);}
export function horseAim(p){
 if(p?.ax<=.25&&Math.abs(p.ay)>=.12)return p.ay<0?'right':'left';
 return null;
}
export function horseCondition(s,side){
 const wear=s.horseWear?.[side];
 if(!wear)return 'fresh';
 if(wear.angryUntil>s.tick)return 'angry';
 return wear.wounds>=HORSE_TIRED_WOUNDS?'tired':'fresh';
}
export function setInput(s,id,input){
 const p=s.players?.[id];if(!p||!input||s.mode!=='run')return;
 if(!['mx','my','ax','ay'].every(k=>Number.isFinite(input[k])))return;
 const move=Math.max(1,Math.hypot(input.mx,input.my)),aim=Math.hypot(input.ax,input.ay);
 p.mx=input.mx/move;p.my=input.my/move;p.until=s.tick+39;
 if(aim>.12){p.ax=input.ax/aim;p.ay=input.ay/aim;}
}
export function operate(s,id,atTick=s.tick){
 const p=s.players[id],role=stationAt(p,s);if(!role||s.mode!=='run')return false;
 if(role==='carry'||role==='royal-carry')return false;
 if(role==='royal-pickup'||role==='royal-seat')return handleRoyal(s,p,id,role);
 if(role==='driver')return whipHorse(s,p);
 if(role==='gunner'){
   if(!s.ammo.loaded||s.fuse.end)return false;
   s.fuse={end:s.tick+FUSE_TICKS,owner:id};s.gunAngle=aimAngle(p);
   feedback(s,'gunner','점화! 오른쪽 스틱으로 조준');return true;
 }
 if(role==='catch'){
   if(atTick<p.slipUntil-CATCH_TICKS)return false;
   if(hit(s,role,catchPhase(p,atTick))){
     p.slipUntil=0;p.safeUntil=s.tick+HZ;message(p,s,'포탄을 붙잡았습니다');
   }else dropBall(s,p,true);
   return true;
 }
 action(s,role,phase({...s,tick:atTick},role),p);return true;
}
export const stageTitle = (stage,chapter=1)=>stageSpec(chapter,stage).title;
export const TRAITS=UPGRADE_CARDS.filter(card=>card.id!=='repair');
export const catchPhase=(p,tick)=>clamp((tick-p.slipUntil+CATCH_TICKS)/CATCH_TICKS,0,1);
export function firingAngle(s,base=s.gunAngle){
 let roll=.017*Math.sin(s.tick*.28)+.012*Math.sin(s.tick*.53);
 if(s.fuse.end){
   const litTicks=s.tick-s.fuse.end+FUSE_TICKS;
   const shotPhase=s.fuse.end*.618;
   const buildUp=clamp(litTicks/8,0,1);
   roll+=buildUp*(.23*Math.sin(litTicks*.31+shotPhase)+.085*Math.sin(litTicks*.67-shotPhase));
 }
 const motion=roll+s.shake*.18*Math.sin(s.tick*.53+s.shakeSign);
 return clamp(base+motion*Math.pow(.78,s.traits?.steady??0),-1.1,1.1);
}
function message(p,s,msg){p.note=msg;p.noteUntil=s.tick+HZ*1.6;}
function whipHorse(s,p,side=horseAim(p),power=3.3){
 if(!side){message(p,s,'오른말이나 왼말을 조준하세요');return false;}
 const wear=(s.horseWear??=horseWear())[side],name=HORSE_NAMES[side];
 if(wear.angryUntil>s.tick){if(p)message(p,s,name+'이 버팁니다 · 잠시 쉬게 하세요');return false;}
 if(s.tick-wear.lastHit<WHIP_COOLDOWN)return false;
 wear.lastHit=s.tick;
 wear.wounds=Math.min(HORSE_ANGRY_WOUNDS,wear.wounds+1);
 let msg;
 if(wear.wounds>=HORSE_ANGRY_WOUNDS){
   wear.angryUntil=s.tick+HORSE_REST_TICKS;
   s.horses[side]=0;
   msg=name+'이 화가 났습니다 · 잠시 쉬게 하세요';
   s.event=msg;s.eventUntil=s.tick+45;
 }else{
   const tired=wear.wounds>=HORSE_TIRED_WOUNDS;
   s.horses[side]=Math.min(18,s.horses[side]+(tired?power/3:power));
   msg=tired?name+'이 지쳐 가속이 줄었습니다':name+' 가속 · '+(side==='right'?'화면 위':'화면 아래');
 }
 feedback(s,'driver',msg);if(p)message(p,s,msg);
 return true;
}
function fallBall(s,p){
 s.ammo.lost++;
 s.falling.push({x:p.x,y:p.y,side:p.y<0?-1:1,tick:s.tick});
}
function dropBall(s,p,carried){
 if(carried){p.ball=false;p.slipUntil=0;}
 fallBall(s,p);
 const msg=carried?'포탄을 놓쳤습니다 · 1발 손실':'장전 실패 · 포탄 1발 손실';
 message(p,s,msg);s.event=msg;s.eventUntil=s.tick+45;
}
function cannonStep(s){
 if(!s.fuse.end)return;
 const humans=humanIds(s);
 if(!humans.includes(s.fuse.owner)){
   const atGun=humans.find(id=>stationAt(s.players[id],s)==='gunner');
   if(atGun)s.fuse.owner=atGun;
 }
 const p=s.players[s.fuse.owner];
 if(p&&onSpot(p,STATIONS.gunner)&&!p.ball)s.gunAngle=aimAngle(p);
 if(s.tick<s.fuse.end)return;
 fireCannon(s,firingAngle(s),p);
}
function fireCannon(s,angle,p=null){
 const dx=Math.cos(angle),dy=Math.sin(angle),caliber=cannonScale(s);
 const muzzle=BARREL.length*caliber;
 s.shots.push({x:BARREL.x+muzzle*dx,y:s.y+BARREL.y+s.shake*Math.sin(s.tick*.37*3)*3+muzzle*dy,
   vx:650*dx,vy:650*dy,caliber});
 s.ammo.loaded=false;s.fuse={end:0,owner:null};
 if(p)message(p,s,'포탄 자동 발사!');
 feedback(s,'gunner','포탄 자동 발사!');impact(s,1.05,'대포 반동');
}
function advanceShots(s){
 for(const shot of s.shots){
   if(shot.spent)continue;
   const x=shot.x,y=shot.y,dx=shot.vx*STEP,dy=shot.vy*STEP;
   shot.x+=dx;shot.y+=dy;
   if(shot.enemy){
     if(shot.x<728)shot.spent=true;
     continue;
   }
   // Use each full tick of travel against the rider's elliptical hit area.
   const caliber=shot.caliber??1;
   const hitX=28*caliber,hitY=36*caliber;
   const moveX=dx/hitX,moveY=dy/hitY;
   const travelSquared=moveX*moveX+moveY*moveY;
   for(const enemy of s.enemies){
     if(enemy.spent)continue;
     const offsetX=(enemy.x-x)/hitX,offsetY=(enemy.y-y)/hitY;
     const along=clamp((offsetX*moveX+offsetY*moveY)/(travelSquared||1),0,1);
     if(Math.hypot(offsetX-along*moveX,offsetY-along*moveY)>1)continue;
     enemy.spent=true;s.kill++;burst(s,enemy.x,enemy.y,'hit');
   }
 }
 s.shots=s.shots.filter(shot=>!shot.spent&&shot.x<1280&&shot.y>ROAD_TOP-32&&shot.y<ROAD_BOTTOM+32);
 s.enemies=s.enemies.filter(enemy=>!enemy.spent);
}
function finishCarry(s,p){
 if(!p.ball)return;
 if(p.slipUntil){
   if(s.tick>=p.slipUntil)dropBall(s,p,true);
 }else if(onSpot(p,STATIONS.rack)){
   p.ball=false;s.ammo.rack++;message(p,s,'적재칸에 포탄을 내려놓았습니다');
 }
}
const TAU=Math.PI*2;
function rand(s){ s.seed=(Math.imul(s.seed,1664525)+1013904223)>>>0;return s.seed/4294967296; }
function royalNotice(s,text){s.royalNotice={text,until:s.tick+2*HZ};}
function handleRoyal(s,p,id,role){
 const royal=royalTarget(s,p);
 const seat=ROYAL_SEATS[royal.id];
 if(role==='royal-pickup'){
   royal.state='carried';royal.carrier=id;royal.x=p.x;royal.y=p.y;
   royal.mischief=null;royal.destination=null;p.royal=royal.id;
 }else{
   royal.state='seated';royal.carrier=null;royal.x=seat.x;royal.y=seat.y;
   royal.attention=100;royal.restUntil=s.tick+ROYAL_REST;p.royal=null;
   recordReseat(s,royal.id);
   royalNotice(s,royal.name+' 좌석 복귀 · 관심 회복');
 }
 return true;
}
function blocksStep(actor,blocker,x,y,radius){
 const next=(x-blocker.x)**2+(y-blocker.y)**2;
 return next<radius**2&&next<(actor.x-blocker.x)**2+(actor.y-blocker.y)**2;
}
function slide(actor,x,y,blocked){
 if(!blocked(x,actor.y))actor.x=x;
 if(!blocked(actor.x,y))actor.y=y;
}
function setRoyalDestination(s,royal,destination,mischief=null){
 royal.destination=destination;royal.mischief=mischief;
 royal.moveUntil=s.tick+(mischief?7:4)*HZ;
}
function royalMischief(s,royal){
 if(s.royalMischiefUntil>s.tick)return;
 let text;
 if(royal.mischief==='rack'&&s.ammo.rack>0){
   s.ammo.rack--;fallBall(s,royal);text=' 적재칸에서 포탄 1발을 버렸습니다';
 }else if(royal.mischief==='gunner'&&s.ammo.loaded&&!s.fuse.end){
   s.gunAngle=(rand(s)<.5?-1:1)*(.75+rand(s)*.28);
   fireCannon(s,s.gunAngle);text=' 대포를 아무 데나 쐈습니다';
 }else if(royal.mischief==='driver'){
   const obstacle=s.obstacles.find(o=>!o.passed&&o.x>40&&o.x<287&&Math.abs(s.y-obstacleY(o))>16);
   const toward=obstacle?(obstacleY(obstacle)<s.y?'right':'left'):rand(s)<.5?'right':'left';
   const side=horseCondition(s,toward)==='angry'?(toward==='right'?'left':'right'):toward;
   if(whipHorse(s,null,side,8))text=' '+HORSE_NAMES[side]+'에 채찍질했습니다';
 }
 if(text){
   const subject=royal.id==='prince'?'왕자가':'공주가';
   royalNotice(s,subject+text);
   s.royalMischiefUntil=s.tick+6*HZ;
 }
}
function wanderRoyal(s,royal){
 if(royal.destination&&s.tick>=royal.moveUntil){
   royal.destination=null;royal.mischief=null;
 }
 if(!royal.destination){
   if(s.tick>=royal.nextMischief&&!(s.royalMischiefUntil>s.tick)){
     const choices=['driver'];
     if(s.ammo.rack>0)choices.push('rack');
     if(s.ammo.loaded&&!s.fuse.end)choices.push('gunner');
     const kind=choices[Math.floor(rand(s)*choices.length)];
     setRoyalDestination(s,royal,STATIONS[kind],kind);
     royal.nextMischief=s.tick+(9+rand(s)*4)*HZ;
   }else{
     const x=DECK.left+22+rand(s)*(DECK.right-DECK.left-44),y=-34+rand(s)*96;
     setRoyalDestination(s,royal,{x,y});
   }
 }
 const {x,y}=royal.destination;
 const dx=x-royal.x,dy=y-royal.y,length=Math.hypot(dx,dy);
 if(length<=5){
   if(royal.mischief)royalMischief(s,royal);
   royal.destination=null;royal.mischief=null;
   return;
 }
 const pace=ROYAL_WALK*STEP/length,leavingBench=royal.y<-48;
 const players=Object.values(s.players);
 slide(royal,royal.x+dx*pace,royal.y+dy*pace,(x,y)=>
   (!leavingBench&&bench(x,y))||
   players.some(p=>blocksStep(royal,p,x,y,ROYAL_BODY))||
   s.royals.some(other=>other!==royal&&other.state==='wandering'&&blocksStep(royal,other,x,y,ROYAL_BODY))
 );
}
function advanceRoyals(s){
 for(const royal of s.royals){
   if(royal.state==='carried'){
     const p=s.players[royal.carrier];
     royal.x=p.x;royal.y=p.y;
     continue;
   }
   if(royal.state==='wandering'){
     wanderRoyal(s,royal);
     continue;
   }
   if(s.tick<royal.restUntil)continue;
   royal.attention=Math.max(0,royal.attention-4.2*STEP);
   if(royal.attention>ROYAL_ATTENTION_LOW)continue;
   const seat=ROYAL_SEATS[royal.id];
   royal.state='wandering';royal.wanderedAt=s.tick;royal.nextMischief=s.tick+(4+rand(s)*2)*HZ;
   setRoyalDestination(s,royal,{x:seat.x,y:seat.dropY});
   royalNotice(s,royal.name+' 좌석 이탈 · 갑판을 돌아다닙니다');
 }
}
export function catapultX(s){
 if(!s.catapult)return null;
 return CATAPULT_TARGET_X+(s.distance-s.catapult.distance)*ROAD_UNITS_PER_METER;
}
export function catapultAirX(s){
 const x=catapultX(s);
 if(!s.catapult||s.tick>=s.catapult.lockAt)return x;
 const remaining=s.catapult.lockAt-s.tick;
 // As the rock closes in, its forward speed eases toward the wagon's base pace.
 return x+baseSpeed(s)*ROAD_UNITS_PER_METER*remaining*STEP+340*(remaining/CATAPULT_APPROACH)**2;
}
export function initial(stage=1,upgrades=DEFAULT_TRAITS,seed=718,chapter=1){
  const location=campaignLocation(chapter,stage);
  const goal=stageSpec(location.chapter,location.stage).goal;
  const traits={...DEFAULT_TRAITS,...upgrades};
  const hp=maxDurability({traits}),wheel=wheelState();
  return {mode:'ready',...location,wave:0,traits,seed,tick:0,distance:0,goal,speed:13+traits.rein*3,hp,boost:0,lane:1,y:CART_CENTER,shake:0,shakeSign:1,fuse:{end:0,owner:null},falling:[],kill:0,nextId:10,
    obstacles:[{id:1,x:204,y:CART_CENTER-37,type:'rock'},{id:2,x:83,y:CART_CENTER+37,type:'log'}],
    enemies:[],shots:[],particles:[],
    nextRock:44,catapult:null,nextCatapult:CATAPULT_FIRST,driveCd:0,gunCd:0,driveMsg:'',gunMsg:'',driveUntil:0,gunUntil:0,event:'',eventUntil:0,serial:0,road:0,players:crew(),humans:1,horses:{right:0,left:0},horseWear:horseWear(),ammo:supplies(),royals:passengers(),gunAngle:0,wheel,fixCd:0,fixMsg:'',fixUntil:0,
    affection:0,care:newCare(),intermission:null,stageEntry:{hp,wheel:{...wheel}}};
}
export function begin(s){if(s.mode==='ready')s.mode='run';}
function beginStage(s,chapter,stage,traits,affection=0,entry=null){
 const next=initial(stage,traits,s.seed,chapter);
 next.affection=affection;
 if(entry){
   next.hp=entry.hp;
   next.wheel={...entry.wheel};
   if(next.wheel.broken>=0)next.speed*=.57;
   next.stageEntry={hp:next.hp,wheel:{...next.wheel}};
 }
 setHumanSlots(next,humanIds(s));
 begin(next);
 return next;
}
export function nextStage(s){
 if(s.mode!=='clear'||s.intermission?.phase!=='shop')return s;
 const next=nextStagePosition(s.chapter??1,s.stage);
 if(!next)return s;
 const wheel={...s.wheel,eligible:Math.max(0,s.wheel.eligible-s.distance),
   since:s.wheel.broken>=0?s.wheel.since-s.tick:0};
 return beginStage(s,next.chapter,next.stage,s.traits,s.affection,{hp:s.hp,wheel});
}
export function restartStage(s){
 return s.mode==='lost'?beginStage(s,s.chapter,s.stage,s.traits,s.affection,s.stageEntry):s;
}
export function restartChapter(s){
 return s.mode==='clear'&&!nextStagePosition(s.chapter??1,s.stage)?beginStage(s,s.chapter,1):s;
}
export function width(s,role){return Math.min(.34,(role==='repair'?.26:.235)+s.traits.sight*.027);}
export function target(s,role){return role==='repair'?[.46,.82][s.wheel.progress]:({supply:.54,loader:.68,catch:.57})[role];}
export function phase(s,role){const t=s.tick/HZ;const freq=role==='repair'?.55:.49;
  return ((t*(freq+Math.min(.17,(s.stage-1)*.026))+.09+s.shake*.16*Math.sin(t*20+s.shakeSign))%1+1)%1;}
export function hit(s,role,p=phase(s,role)){const gap=Math.abs(((p-target(s,role)+1.5)%1)-.5);return gap<=width(s,role)/2;}
function impact(s,power,msg){s.shake=Math.min(1.45,power*Math.pow(.78,s.traits.spring));s.shakeSign=rand(s)*TAU;s.event=msg;s.eventUntil=s.tick+42;s.serial++;
 for(const p of Object.values(s.players)){
   if(p.ball&&!p.slipUntil&&!(p.safeUntil>s.tick)&&s.shake>.40)p.slipUntil=s.tick+CATCH_TICKS;
 }
}
function feedback(s,role,msg){const key=feedbackKey(role);s[key+'Msg']=msg;s[key+'Until']=s.tick+26;s[key+'Cd']=s.tick+8;}
function loseWheel(s){
 if(s.wheel.broken!==-1||s.distance<s.wheel.eligible)return;
 const id=Math.floor(rand(s)*WHEELS.length);
 Object.assign(s.wheel,{broken:id,progress:0,since:s.tick,first:false});
 impact(s,1.2,WHEELS[id].name+' 이탈');
}
function burst(s,x,y,kind){s.particles.push({x,y,kind,life:18,id:s.nextId++});}
function advanceObstacles(s,travel){
  if(s.distance>=s.nextRock){
    const type=ROAD_OBSTACLE_TYPES[Math.floor(rand(s)*ROAD_OBSTACLE_TYPES.length)];
    const {halfWidth}=roadObstacleProfile(type);
    const y=CART_TOP+halfWidth+rand(s)*(CART_BOTTOM-CART_TOP-2*halfWidth);
    s.obstacles.push({id:s.nextId++,x:-52,y,type});
    s.nextRock=s.distance+Math.max(32,76-(s.stage-1)*4)+rand(s)*23;
  }
  for(const obstacle of s.obstacles){
    obstacle.x+=travel;
    if(obstacle.passed||obstacle.x<287||obstacle.x>=344)continue;
    const profile=roadObstacleProfile(obstacle.type);
    if(Math.abs(s.y-obstacleY(obstacle))>=profile.halfWidth)continue;
    obstacle.passed=true;
    s.hp-=profile.damage;
    const activeFactor=s.obstacleSlow?.factor??1;
    if(profile.slowFactor<=activeFactor){
      const until=s.tick+Math.round(profile.slowSeconds*HZ);
      s.speed*=profile.slowFactor/activeFactor;
      s.obstacleSlow={
        factor:profile.slowFactor,
        until:profile.slowFactor===activeFactor?Math.max(s.obstacleSlow?.until??0,until):until
      };
    }
    impact(s,.98,profile.name+' 충돌');
    burst(s,292,obstacleY(obstacle),'dust');
    loseWheel(s);
  }
  s.obstacles=s.obstacles.filter(obstacle=>obstacle.x<1260);
}
function advanceCatapult(s){
  const rock=s.catapult;
  if(!rock){
    if(s.tick<s.nextCatapult)return;
    const offset=76+rand(s)*8;
    const clearance=CART_HULL.halfHeight+CATAPULT_RADIUS.y-offset+8;
    let side=rand(s)<.5?'right':'left';
    if(s.y-CART_TOP<clearance)side='right';
    else if(CART_BOTTOM-s.y<clearance)side='left';
    s.catapult={
      launchedAt:s.tick,lockAt:s.tick+CATAPULT_APPROACH,landAt:s.tick+CATAPULT_FLIGHT,
      distance:s.distance+baseSpeed(s)*CATAPULT_FLIGHT*STEP,
      y:s.y+offset*(side==='right'?-1:1),side
    };
    s.nextCatapult=s.tick+Math.max(9*HZ,13*HZ-(s.stage-1)*15);
    return;
  }
  if(s.tick>=rock.landAt+CATAPULT_LINGER){s.catapult=null;return;}
  if(s.tick<rock.landAt||rock.hit!==undefined)return;
  const x=catapultX(s);
  const dx=x-clamp(x,CART_HULL.left,CART_HULL.right);
  const dy=rock.y-clamp(rock.y,s.y-CART_HULL.halfHeight,s.y+CART_HULL.halfHeight);
  rock.hit=Math.hypot(dx/CATAPULT_RADIUS.x,dy/CATAPULT_RADIUS.y)<=1;
  if(rock.hit){
    s.hp-=22;
    s.speed*=.78;
    impact(s,1.42,'투석기 바위 명중');
    loseWheel(s);
  }else{
    s.event='투석기 바위 회피';
    s.eventUntil=s.tick+42;
  }
}
function advanceWaves(s){
  const {waves}=stageSpec(s.chapter,s.stage);
  while(s.wave<waves.length&&s.tick>=waves[s.wave].at*HZ){
    const {groups}=waves[s.wave++];
    let offset=0;
    for(const {enemy,count} of groups){
      const {kind}=ENEMY_PROFILES[enemy];
      for(let i=0;i<count;i++){
        const y=LANES[Math.floor(rand(s)*LANES.length)]+rand(s)*12-6;
        s.enemies.push({id:s.nextId++,x:WAVE_ENTRY_X+offset,y,kind,profile:enemy});
        offset+=WAVE_SPACING;
      }
    }
  }
}
function advancePursuers(s,base){
  const previousPace=18+Math.min(s.stage*3,24)-(s.speed-base)*2.05;
  for(const enemy of s.enemies){
    if(enemy.spent)continue;
    const sinceStrike=s.tick-(enemy.lastStrike??-Infinity);
    if(sinceStrike<=ENEMY_RETREAT_TICKS){
      enemy.x+=ENEMY_RETREAT_SPEED*STEP;
      continue;
    }
    const recovering=sinceStrike<=ENEMY_RETREAT_TICKS+ENEMY_RECOVERY_TICKS;
    const paceScale=recovering?ENEMY_RECOVERY_SPEED:1;
    if(enemy.x<ENEMY_TRACK_X){
      const lateralStep=ENEMY_TRACK_SPEED*paceScale*STEP;
      enemy.y+=clamp(s.y-enemy.y,-lateralStep,lateralStep);
    }
    const profile=ENEMY_PROFILES[enemy.profile];
    const pace=profile?(profile.speed-s.speed)*ROAD_UNITS_PER_METER:previousPace;
    const nextX=enemy.x-pace*paceScale*STEP;
    enemy.x=Math.max(ENEMY_STRIKE_X,nextX);
    if(recovering||nextX>=ENEMY_STRIKE_X||Math.abs(s.y-enemy.y)>ENEMY_STRIKE_REACH)continue;
    enemy.lastStrike=s.tick;
    s.hp-=profile?.damage??14;
    impact(s,1.32,'추격대의 일격');
    burst(s,718,s.y,'hit');
    loseWheel(s);
    s.shots.push({x:enemy.x,y:enemy.y,vx:-430,vy:(s.y-enemy.y)*4,enemy:true});
  }
}
function action(s,role,p,operator){
  if(s.mode!=='run'||s[feedbackKey(role)+'Cd']>s.tick)return;
  const tell=msg=>{feedback(s,role,msg);message(operator,s,msg);};
  if(role==='supply'&&s.ammo.stock<1){tell('포탄 더미가 비었습니다');return;}
  if(role==='loader'){
    if(!canLoad(s))return;
    s.ammo.rack--;
    if(hit(s,role,p)){s.ammo.loaded=true;tell('장전 완료 · 포수 점화 가능');}
    else {dropBall(s,operator,false);feedback(s,role,operator.note);}
    return;
  }
  if(!hit(s,role,p)){tell('타이밍 실패');return;}
  if(role==='repair'){
    if(s.wheel.broken<0)return;
    if(s.wheel.progress===0){s.wheel.progress=1;tell('바퀴 장착 완료');}
    else{s.wheel.broken=-1;s.wheel.progress=0;s.wheel.eligible=s.distance+240;
      s.event='바퀴 수리 완료';s.eventUntil=s.tick+60;tell('수리 완료 · 속도 회복');}
    return;
  }
  if(role==='supply'){
    operator.ball=true;operator.slipUntil=0;s.ammo.stock--;tell('포탄 확보 · 파란 적재칸으로 이동');return;
  }
}
export function step(s){
  if(s.mode!=='run')return;
  s.tick++;
  const wearBySide=s.horseWear??=horseWear();
  runBots(s);
  for(const p of Object.values(s.players)){
    if(p.until<s.tick){p.mx=0;p.my=0;}
    const pace=p.slipUntil?0:p.ball?CARRY_SPEED*(1+.2*s.traits.porter):p.royal?ROYAL_CARRY:WALK_SPEED;
    const x=clamp(p.x+p.mx*pace*STEP,DECK.left,DECK.right);
    const y=clamp(p.y+p.my*pace*STEP,DECK.top,DECK.bottom);
    slide(p,x,y,(nextX,nextY)=>bench(nextX,nextY)||s.royals.some(r=>r.state==='wandering'&&blocksStep(p,r,nextX,nextY,ROYAL_BODY)));
    finishCarry(s,p);
  }
  advanceRoyals(s);
  recordCare(s);
  s.boost=Math.max(0,s.boost-STEP);s.shake=Math.max(0,s.shake-STEP*1.05);
  for(const side of HORSE_SIDES){
    s.horses[side]=Math.max(0,s.horses[side]-2.2*STEP);
    const wear=wearBySide[side],rest=s.tick-wear.lastHit-HORSE_REST_TICKS;
    if(wear.wounds&&rest>=0&&rest%HORSE_HEAL_TICKS===0)wear.wounds--;
  }
  if(s.obstacleSlow?.until<=s.tick)s.obstacleSlow=null;
  const base=baseSpeed(s);
  const speedTarget=(base+(s.horses.right+s.horses.left)/2)*(s.wheel.broken>=0?.57:1)*(s.obstacleSlow?.factor??1);
  s.speed+=(speedTarget-s.speed)*.07;
  if(s.wheel.first&&s.distance>=155)loseWheel(s);
  const travel=s.speed*STEP;
  s.distance+=travel;s.road+=travel*ROAD_UNITS_PER_METER;s.y=clamp(s.y+(s.horses.left-s.horses.right)*STEERING_RATE*STEP,CART_TOP,CART_BOTTOM);
  cannonStep(s);
  s.falling=s.falling.filter(b=>s.tick-b.tick<32);
  s.lane=s.y<(CART_TOP+CART_CENTER)/2?0:s.y>(CART_CENTER+CART_BOTTOM)/2?2:1;
  advanceObstacles(s,travel*ROAD_UNITS_PER_METER);
  advanceCatapult(s);
  advancePursuers(s,base);
  advanceShots(s);
  advanceWaves(s);
  s.particles.forEach(p=>p.life--);s.particles=s.particles.filter(p=>p.life>0);
  if(s.hp<=0){s.hp=0;s.mode='lost';}
  else if(s.distance>=s.goal-1e-8){s.distance=s.goal;s.mode='clear';finishCare(s);}
}
export function advance(s,ticks){for(let i=0;i<Math.min(7200,ticks)&&s.mode==='run';i++)step(s);}

function spotFor(s,job,p){
 if(job==='royal-pickup'){
   const humans=humanIds(s).map(id=>s.players[id]);
   return s.royals.find(r=>r.state==='wandering'&&s.tick-r.wanderedAt>=ROYAL_BOT_GRACE&&
     !humans.some(player=>royalTarget(s,player)===r));
 }
 if(job==='royal-seat'){
   const seat=ROYAL_SEATS[p.royal];
   return seat&&{x:seat.x,y:seat.dropY};
 }
 return job==='repair'?WHEELS[s.wheel.broken]:STATIONS[job==='deliver'?'rack':job];
}
function neededRein(s){
 const rock=s.catapult;
 if(rock&&s.tick<rock.landAt){
   if(Math.abs(s.y-rock.y)>=CART_HULL.halfHeight+CATAPULT_RADIUS.y+8)return null;
   return s.y>=rock.y?'left':'right';
 }
 const threat=s.obstacles.find(o=>!o.passed&&o.x>35&&o.x<290&&Math.abs(s.y-obstacleY(o))<roadObstacleProfile(o.type).halfWidth+6);
 if(threat){
   const clearance=roadObstacleProfile(threat.type).halfWidth+6;
   if(s.y<CART_TOP+clearance)return 'left';
   if(s.y>CART_BOTTOM-clearance)return 'right';
   return s.y>=obstacleY(threat)?'left':'right';
 }
 if(s.lane===0)return 'left';if(s.lane===2)return 'right';
 return s.horses.right>s.horses.left?'left':'right';
}
function assignBots(s,ids){
 const held=new Set(humanIds(s).map(id=>s.players[id]).map(p=>stationAt(p,s)).filter(Boolean));
 held.delete('royal-pickup');
 const free=ids.slice();
 function assign(job,pick=true){
   if(!pick||held.has(job)||!free.length)return;
   const spot=spotFor(s,job);if(!spot)return;
   const cost=id=>{const p=s.players[id];return Math.hypot(spot.x-p.x,spot.y-p.y)-(p.job===job?72:0);};
   const id=free.reduce((best,id)=>cost(id)<cost(best)?id:best);
   s.players[id].job=job;free.splice(free.indexOf(id),1);held.add(job);
 }
 for(const id of ids){
   if(s.players[id].ball){
     s.players[id].job='deliver';held.add('deliver');free.splice(free.indexOf(id),1);
   }else if(s.players[id].royal){
     s.players[id].job='royal-seat';free.splice(free.indexOf(id),1);
   }
 }
 if(s.fuse.owner&&free.includes(s.fuse.owner)){
   s.players[s.fuse.owner].job='gunner';free.splice(free.indexOf(s.fuse.owner),1);held.add('gunner');
 }
 assign('repair',s.wheel.broken>=0&&s.tick-s.wheel.since>HZ*3);
 assign('loader',canLoad(s));
 assign('royal-pickup');
 assign('gunner',s.ammo.loaded&&s.enemies.length>0);
 const needSupply=s.ammo.stock>0&&!Object.values(s.players).some(p=>p.ball);
 assign('supply',s.ammo.rack===0&&needSupply);
 assign('driver');
 assign('supply',s.ammo.rack<3&&needSupply);
 for(const id of free)s.players[id].job=null;
}
function runBots(s){
 const humans=humanIds(s),ids=PLAYER_IDS.filter(id=>!humans.includes(id));
 if(!ids.length)return;
 if(s.tick%8===1)assignBots(s,ids);
 for(const id of ids){
   const p=s.players[id],job=p.royal?'royal-seat':p.job,spot=spotFor(s,job,p);
   if(!spot){p.mx=0;p.my=0;continue;}
   const dx=spot.x-p.x,dy=(spot.y< -48&&Math.abs(dx)>46?-5:spot.y)-p.y,len=Math.hypot(dx,dy);
   let ax=1,ay=0;
   const horse=job==='driver'?neededRein(s):null;
   if(horse){ax=-1;ay=horse==='right'?-.4:.4;}
   if(job==='gunner'){const e=s.enemies.reduce((a,b)=>!a||b.x<a.x?b:a,null);if(e){ax=e.x-BARREL.x;ay=e.y-s.y;}}
   setInput(s,id,{mx:len>6?dx/len:0,my:len>6?dy/len:0,ax,ay});
   if(p.slipUntil){
     if(hit(s,'catch',catchPhase(p,s.tick))&&s.tick%4===0)operate(s,id);
     continue;
   }
   if(stationAt(p,s)!==job||s.tick%3!==0)continue;
   if(job==='gunner'){
     const occupied=humanIds(s).map(id=>s.players[id]).some(h=>onSpot(h,STATIONS.gunner));
     if(occupied||s.tick<HZ*5||s.fuse.end||!s.enemies.length)continue;
   }else if(job==='driver'){
     if(!horse)continue;
     const occupied=humans.some(id=>stationAt(s.players[id],s)==='driver');
     const wear=s.horseWear[horse];
     const evading=s.catapult&&s.tick<s.catapult.landAt;
     const maxWounds=evading?HORSE_ANGRY_WOUNDS-1:HORSE_TIRED_WOUNDS-2;
     if(occupied||s.horses[horse]>(evading?9:6)||wear.wounds>=maxWounds||s.tick-wear.lastHit<(evading?9:HZ))continue;
   }else if(job==='royal-pickup'){
     if(royalTarget(s,p)!==spot)continue;
   }else if(job!=='royal-seat'&&!hit(s,job))continue;
   operate(s,id);
 }
}
