import {CHAPTERS,chapterSpec,stageSpec,nextStagePosition,ENEMY_PROFILES} from './campaign.mjs';
import {ROAD_OBSTACLE_TYPES,obstacleY,roadObstacleProfile,roadsideTrees} from './road-obstacles.mjs?v=loose-wheels34';
import {finishCare,maxDurability,newCare,pruneVotes,recordCare,recordFeeding,recordReseat,UPGRADE_CARDS} from './intermission.mjs';
import {DRAFT_HORSE_SCALE,horseRumpAim,horseWhipTarget,HORSE_ZONE_NAMES,spookHorse,horseRebellion} from './horse-targets.mjs';
export {obstacleY} from './road-obstacles.mjs?v=loose-wheels34';

export const HZ = 30;
export const STEP = 1 / HZ;
export const CAMERA_TRANSITION_SECONDS=.5;
export const LANES = [82, 182, 282];
const [CART_TOP, CART_CENTER, CART_BOTTOM] = LANES;
export const ROAD_TOP = CART_TOP - 134;
export const ROAD_BOTTOM = CART_BOTTOM + 134;
export const ROAD_CAMERA_MIN_X=-1000, ROAD_OBSTACLE_DRAW_MARGIN=43;
export const DECK = {left:363,right:694,top:-81,bottom:81};
export const DECK_FLOOR='deck', CELLAR_FLOOR='cellar';
export const STAIRS={x:502,y:65,r:18};
export const playerFloor=p=>p?.floor===CELLAR_FLOOR?CELLAR_FLOOR:DECK_FLOOR;
export const STATIONS = {
 driver:{x:385,y:0,r:26},gunner:{x:658,y:3,r:26},
 rack:{x:674,y:62,r:19},loader:{x:622,y:-39,r:21},cleaner:{x:674,y:-41,r:20},
 supply:{x:665,y:0,r:23},cook:{x:383,y:-21,r:24},'wheel-pickup':{x:528,y:-62,r:24}
};
const CANNON_ROLES=['cleaner','loader','gunner'];
const WORK_ROLES=['repair','driver',...CANNON_ROLES,'supply','cook','wheel-pickup'];
export const ROYAL_SEATS={
 prince:{x:510,y:-74,dropY:-34},
 princess:{x:549,y:-74,dropY:-34}
};
export const ROYAL_REACH=30, ROYAL_RETURN_REACH=18;
const ROYAL_STATUS_REACH=52;
export const ROYAL_CRY_THRESHOLD=70;
const ROYAL_INITIAL_HUNGER={prince:14,princess:22};
const ROYAL_HUNGER_RATE=1.65, SNACK_HUNGER_RELIEF=55;
const ROYAL_FALL_CHANCE={cannon:.25,rock:.52,obstacle:.34};
const ROYAL_FALL_CRY_CHANCE=.85;
export const FUSE_TICKS=75;
export const CANNON_VIEW_HOLD_TICKS=2*HZ, CANNON_VIEW_RETURN_TICKS=CAMERA_TRANSITION_SECONDS*HZ;
export const CANNON_CLEAN_TAPS=12;
export const CATCH_TICKS=48;
const COOK_TICKS=3*HZ, COOK_SHAKE_LOSS=.55*HZ;
export const ENEMY_RETREAT_TICKS=48;
export const ENEMY_RECOVERY_TICKS=126;
const ENEMY_RETURN_TICKS=HZ;
export const CATAPULT_RADIUS={x:35,y:24};
const CATAPULT_FIRST=6*HZ, CATAPULT_APPROACH=33, CATAPULT_FLIGHT=144, CATAPULT_LINGER=21;
const CART_HULL={left:340,right:715,halfHeight:100};
const CATAPULT_TARGET_X=718, ROAD_UNITS_PER_METER=4;
const ROAD_SCROLL_UNITS_PER_METER=ROAD_UNITS_PER_METER*2;
const ROAD_FIRST_WARNING_METERS=42, ROAD_WARNING_TICKS=4*HZ, ROAD_OBSTACLE_ENTRY_X=ROAD_CAMERA_MIN_X-100;
const ROAD_OBSTACLE_HIT_START=287, ROAD_OBSTACLE_HIT_END=344, ROAD_STRIKE_MARGIN=HZ;
const WALK_SPEED=165, CARRY_SPEED=50, WHEEL_CARRY_SPEED=110, STEERING_RATE=4.2;
const ENEMY_RETREAT_SPEED=54, ENEMY_RECOVERY_SPEED=.4;
const ENEMY_TRACK_X=1040, ENEMY_TRACK_SPEED=30, ENEMY_STRIKE_REACH=26;
const PURSUIT_GAP=118;
const PURSUIT_SLOTS=[
  {y:-86,strikeX:750,hitX:692},
  {y:86,strikeX:750,hitX:692},
  {y:0,strikeX:773,hitX:715},
  {y:-43,strikeX:783,hitX:715},
  {y:43,strikeX:783,hitX:715},
];
// The sprite center is offset to keep the lance near the wagon during a strike.
export const CAVALRY_ART_X=40;
const CAVALRY_BODY={
  x:CAVALRY_ART_X-9*DRAFT_HORSE_SCALE,y:3*DRAFT_HORSE_SCALE,
  rx:43*DRAFT_HORSE_SCALE,ry:23*DRAFT_HORSE_SCALE,
};
export const WAVE_ENTRY_X=1280;
const WAVE_SPACING=56;
const TEST_ENEMY_LIMIT=16, TEST_ENEMY_ENGAGEMENT=14*HZ;
const WHIP_COOLDOWN=3, HORSE_REST_TICKS=HZ*3, HORSE_HEAL_TICKS=24;
export const HORSE_RUNAWAY_HITS=8;
const HORSE_RUNAWAY_TICKS=36, HORSE_TIRED_TICKS=4*HZ;
const HORSE_RUNAWAY_STEER=176, HORSE_RUNAWAY_PACE=.18, HORSE_TIRED_PACE=.78;
const HORSE_SIDES=['right','left'];
const ROYAL_BODY=22, ROYAL_WALK=56, ROYAL_CARRY=125, ROYAL_ATTENTION_LOW=20;
const ROYAL_REST=7*HZ, ROYAL_BOT_GRACE=9*HZ;
const ROYAL_STAIRS_CHANCE=.25;
const ROYAL_SOOTHE_DISTANCE=ROYAL_CARRY*2.5;
const ROYAL_BOREDOM_CHANCE=.30, ROYAL_BOREDOM_DISTANCE=ROYAL_SOOTHE_DISTANCE/3;
const ROYAL_EXCURSION_DISTANCE=82, ROYAL_DECK_END=80;
const CRYING_WALK_RATE=[1,.90,.75];
const CRYING_COOK_RATE=[1,.85,.65];
const CRYING_TIMING_WIDTH=[1,.80,.60];
const BOT_SOOTHE_WAYPOINTS=[{x:422,y:22},{x:610,y:22},{x:610,y:53},{x:422,y:53}];
const BOT_EXCURSION_WAYPOINTS=[{x:DECK.left+42,y:43},{x:DECK.right-42,y:43}];
const DEFAULT_TRAITS={spring:0,rein:0,sight:0,steady:0,barrel:0,hull:0,porter:0};
const baseSpeed=s=>13+3*s.traits.rein;
export const HORSE_NAMES={right:'오른말',left:'왼말'};
export const ROLES={driver:'마부',cleaner:'포신 청소',loader:'장전',gunner:'포수',supply:'포탄 운반',deliver:'포탄 적재',carry:'포탄 운반',catch:'포탄 붙잡기',cook:'간이 음식 제작대','food-carry':'간식 운반','royal-feed':'간식 먹이기','royal-soothe':'아이 달래기','royal-pickup':'왕실 안아 올리기','royal-carry':'왕실 데려가기','royal-seat':'왕실 다시 앉히기','wheel-pickup':'바퀴 보관함','wheel-carry':'예비 바퀴 운반','wheel-return':'바퀴 보관함','wheel-missing':'바퀴 수리 준비',repair:'바퀴 수리'};
const AMMO_STOCK=17, STARTING_RACK_SHELLS=3;
function supplies(){return {stock:AMMO_STOCK,rack:STARTING_RACK_SHELLS,loaded:false,lost:0,cleaning:0};}
function passengers(){
 return [['prince','왕자',75],['princess','공주',63]].map(([id,name,attention])=>{
   const {x,y}=ROYAL_SEATS[id];
   return {id,name,attention,hunger:ROYAL_INITIAL_HUNGER[id],crying:false,upset:false,sootheDistance:0,x,y,floor:DECK_FLOOR,state:'seated',carrier:null,restUntil:0,mischief:null,destination:null};
 });
}
const royalIsCrying=royal=>royal.upset||royal.hunger>=ROYAL_CRY_THRESHOLD;
export function cryingChildCount(s){
 return s.royals?.reduce((count,royal)=>count+Number(royal.crying===true),0)??0;
}
const freshHorseWear=()=>({wounds:0,lastHit:-9999,angryUntil:0});
function horseWear(){return Object.fromEntries(HORSE_SIDES.map(side=>[side,freshHorseWear()]));}

export const BARREL = {x:704,y:0,length:44};
const CANNON_SHOT_SPEED=1750, CANNON_SHOT_RADIUS=14, LEGACY_CANNON_RADIUS=6;
export const CANNON_AIM_MIN_X=866, CANNON_AIM_MAX_X=1080, CANNON_AIM_Y=154;
export const CANNON_IMPACT_TICKS=90;
const CANNON_AIM_CENTER=(CANNON_AIM_MIN_X+CANNON_AIM_MAX_X)/2;
const CANNON_AIM_SPAN=(CANNON_AIM_MAX_X-CANNON_AIM_MIN_X)/2;
export const cannonScale=s=>1+.17*(s.traits?.barrel??0);
export const cannonShotRadius=shot=>shot.radius??LEGACY_CANNON_RADIUS*(shot.caliber??1);
export const WHEELS = [
 {x:405,y:-72,name:'앞쪽 위 바퀴'}, {x:650,y:-72,name:'뒤쪽 위 바퀴'},
 {x:405,y:72,name:'앞쪽 아래 바퀴'}, {x:650,y:72,name:'뒤쪽 아래 바퀴'}
];
export const WHEEL_MOUNT_OFFSET=103, LOOSE_WHEEL_RADIUS=20, WHEEL_KNOCK_TICKS=24;
const LOOSE_WHEEL_EXIT_X=WAVE_ENTRY_X+100;
export const repairLabels=['바퀴 끼우기','차축 핀 고정'];
function wheelState(){return {broken:-1,progress:0,since:0,first:true,eligible:110};}
export function feedbackKey(role){return role==='gunner'?'gun':role==='repair'?'fix':role==='driver'?'drive':role;}

const clamp=(v,min,max)=>Math.max(min,Math.min(max,v));
const bench=(x,y)=>x>468&&x<583&&y< -48;
function actor(x,y,face){return {x,y,mx:0,my:0,ax:face,ay:0,until:0,ball:false,food:false,spareWheel:false,cookHeld:false,cookTicks:0,royal:null,job:null,station:null,exitedStation:null,floor:DECK_FLOOR,stairLocked:false};}
function crew(){return {p1:actor(480,10,-1),p2:actor(614,11,1),p3:actor(556,50,1),p4:actor(500,-28,-1)};}
function campaignLocation(chapter,stage){
 const chapterNumber=CHAPTERS[chapter-1]?chapter:1;
 const count=chapterSpec(chapterNumber).stages.length;
 return {chapter:chapterNumber,stage:Number.isInteger(stage)&&stage>0?(stage-1)%count+1:1};
}
export const PLAYER_IDS=['p1','p2','p3','p4'];
export function humanIds(s){return s.humanSlots??PLAYER_IDS.slice(0,s.humans??1);}
function resetCooking(p){p.cookHeld=false;p.cookTicks=0;}
export function stopCookingHold(s){for(const p of Object.values(s.players))p.cookHeld=false;}
export function setHumanSlots(s,slots){
 const previous=humanIds(s);
 for(const id of PLAYER_IDS){
   if(previous.includes(id)===slots.includes(id))continue;
   const p=s.players[id];p.mx=0;p.my=0;p.until=0;p.job=null;resetCooking(p);
 }
 s.humanSlots=[...slots];s.humans=slots.length;
 if(s.cannonExit&&!slots.includes(s.cannonExit.id))finishCannonExit(s);
 pruneVotes(s,slots);
}
export function ensureDeck(s){
 if(s.chapter===undefined||s.wave===undefined){
   Object.assign(s,campaignLocation(s.chapter,s.stage));
   const stage=stageSpec(s.chapter,s.stage);
   // Keep live legacy enemies, but skip waves that would already have launched.
   const {waves}=stage;
   const loopTicks=waveLoopTicks(waves);
   const loops=s.testMode&&waves.length?Math.floor(Math.max(0,s.tick-waves[0].at*HZ)/loopTicks):0;
   s.wave=loops*waves.length+waves.filter(wave=>wave.at*HZ<=s.tick-loops*loopTicks).length;
   s.goal=stage.goal;
   if(!s.testMode)s.distance=s.mode==='clear'?s.goal:Math.min(s.distance,s.goal);
   if(s.mode==='ready')s.enemies=[];
   delete s.nextEnemy;
 }
 if(!s.players)s.players=crew();
 if(!s.players.p3){const fresh=crew();s.players.p3=fresh.p3;s.players.p4=fresh.p4;}
 if(!s.horseWear){
   s.horseWear=horseWear();
   for(const p of Object.values(s.players))if(HORSE_SIDES.includes(p.job))p.job=null;
 }
 for(const side of HORSE_SIDES){
   const wear=s.horseWear[side]??=freshHorseWear();
   wear.wounds=Number.isFinite(wear.wounds)?clamp(wear.wounds,0,HORSE_RUNAWAY_HITS):0;
   wear.lastHit??=-9999;wear.angryUntil??=0;
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
 for(const p of Object.values(s.players)){
   p.royal??=null;p.station??=null;p.exitedStation??=null;p.food=!p.ball&&p.food===true;
   p.floor=playerFloor(p);
   p.spareWheel=p.spareWheel===true&&!p.ball&&!p.food&&!p.royal&&p.floor===CELLAR_FLOOR&&canTakeSpareWheel(s);
   if(typeof p.stairLocked!=='boolean')p.stairLocked=onSpot(p,STAIRS);
   if(p.station==='cook'&&occupiedStation(p,s)==='cook'){
     p.cookHeld=p.cookHeld===true&&s.mode==='run';
     p.cookTicks=Number.isFinite(p.cookTicks)?clamp(p.cookTicks,0,COOK_TICKS):0;
   }else resetCooking(p);
 }
 for(const royal of s.royals){
   royal.hunger=Number.isFinite(royal.hunger)?clamp(royal.hunger,0,100):ROYAL_INITIAL_HUNGER[royal.id];
   const distance=royal.sootheDistance;
   royal.sootheDistance=Number.isFinite(distance)?clamp(distance,0,ROYAL_SOOTHE_DISTANCE):0;
   royal.upset=royal.state!=='seated'&&royal.upset===true&&royal.sootheDistance<ROYAL_SOOTHE_DISTANCE;
   if(royal.bored&&(!royal.upset||royal.state!=='carried'||!Number.isFinite(royal.boredFrom?.x)||!Number.isFinite(royal.boredFrom?.y))){
     royal.bored=false;delete royal.boredFrom;
   }
   royal.floor=royal.state==='carried'?playerFloor(s.players[royal.carrier])
     :offSeat(royal)?playerFloor(royal):DECK_FLOOR;
   royal.crying=royalIsCrying(royal);
 }
 s.horses??={right:0,left:0};s.ammo??=supplies();s.humans??=1;
 if(!s.wheel){s.wheel=wheelState();s.fixCd=0;s.fixMsg='';s.fixUntil=0;}
 if(!Array.isArray(s.looseWheels))s.looseWheels=[];
 if(!Array.isArray(s.obstacles))s.obstacles=[];
 if(!Number.isFinite(s.nextRock))s.nextRock=s.distance+ROAD_FIRST_WARNING_METERS;
 if(s.roadWarning===undefined){
   s.roadWarning=null;
   if(s.mode==='ready')s.obstacles=[];
 }
 if(s.roadWarning)s.roadWarning.x=Math.min(s.roadWarning.x??ROAD_OBSTACLE_ENTRY_X,ROAD_OBSTACLE_ENTRY_X);
 if(!Number.isFinite(s.gunAngle))s.gunAngle=0;
 if(s.ammo.loaded===undefined){
   const rounds=s.ammo.magazine??0;
   s.ammo.loaded=rounds>0;s.ammo.rack=Math.max(0,rounds-1);s.ammo.lost=0;
   delete s.ammo.magazine;
 }
 if(s.mode==='ready'&&s.ammo.rack===2)s.ammo.rack=STARTING_RACK_SHELLS;
 s.fuse??={end:0,owner:null};s.falling??=[];s.cannonRecoil??=null;
 if(s.ammo.cleaning===undefined)s.ammo.cleaning=s.ammo.loaded?null:0;
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
export const cannonStation=s=>Number.isInteger(s.ammo.cleaning)?'cleaner':s.ammo.loaded?'gunner':'loader';
export const canLoad=s=>cannonStation(s)==='loader'&&s.ammo.rack>0;
export function addRackShell(s){
 if(s.mode!=='run')return false;
 s.ammo.rack++;
 return true;
}
export const canTakeSpareWheel=s=>s.wheel?.broken>=0&&s.wheel.progress===0;
const jobFloor=role=>role==='repair'||role==='supply'||role==='cook'||role==='wheel-pickup'?CELLAR_FLOOR:DECK_FLOOR;
function botJobFloor(p,role,spot){
 if(role==='royal-soothe')return playerFloor(p);
 if(role==='royal-pickup'&&spot)return playerFloor(spot);
 return jobFloor(role);
}
const reachesSpot=(p,spot,role)=>spot&&playerFloor(p)===jobFloor(role)&&onSpot(p,spot);
function workSpot(s,role){
 if(role==='repair'){
   const wheel=WHEELS[s.wheel.broken];
   return wheel?{...wheel,r:28}:null;
 }
 if(role==='supply'&&s.ammo.stock===0)return null;
 if(role==='wheel-pickup'&&!canTakeSpareWheel(s))return null;
 if(role==='gunner'&&s.cannonExit)return STATIONS.gunner;
 if(CANNON_ROLES.includes(role)&&role!==cannonStation(s))return null;
 return WORK_ROLES.includes(role)?STATIONS[role]:null;
}
function canWork(p,s,role){
 if(p.ball||p.food||p.royal)return false;
 return role==='repair'?(p.spareWheel||s.wheel?.progress===1):!p.spareWheel;
}
export function cannonAfterShot(s,p){
 const shot=s.cannonExit;
 return shot&&s.players[shot.id]===p?shot:null;
}
export function occupiedStation(p,s){
 if(!p?.station||!canWork(p,s,p.station))return null;
 const spot=workSpot(s,p.station);
 return reachesSpot(p,spot,p.station)?p.station:null;
}
export function cookingProgress(s,p){return occupiedStation(p,s)==='cook'?(p.cookTicks??0)/COOK_TICKS:0;}
function releaseStation(p,blockEntry=false){
 if(blockEntry)p.exitedStation=p.station;
 p.station=null;p.mx=0;p.my=0;
 resetCooking(p);
}
export function leaveStation(s,id,expectedRole){
 const p=s.players[id],role=occupiedStation(p,s);
 if(s.mode!=='run'||!role||(expectedRole&&role!==expectedRole)||cannonAfterShot(s,p))return false;
 releaseStation(p,true);
 return true;
}
function finishCannonExit(s){
 if(!s.cannonExit)return;
 const p=s.players[s.cannonExit.id];
 s.cannonExit=null;
 if(p.station!=='gunner')return;
 releaseStation(p,true);
 message(p,s,'포탄 발사 · 포신을 청소하세요');
}
function claimStations(s,humans){
 const owners=new Map();
 const ids=[...humans,...PLAYER_IDS.filter(id=>!humans.has(id))];
 for(const id of ids){
   const p=s.players[id],role=occupiedStation(p,s);
   if(role&&!owners.has(role)&&(humans.has(id)||p.job===role))owners.set(role,id);
   else if(p.station)releaseStation(p);
   if(p.exitedStation){
     const spot=p.exitedStation==='gunner'?STATIONS.gunner:workSpot(s,p.exitedStation);
     if(!reachesSpot(p,spot,p.exitedStation))p.exitedStation=null;
   }
 }
 for(const id of ids){
   const p=s.players[id],human=humans.has(id);
   if(p.station||p.exitedStation||p.ball||p.food||p.royal)continue;
   for(const role of human?WORK_ROLES:[p.job]){
     const spot=workSpot(s,role),owner=owners.get(role);
     if(!canWork(p,s,role)||!reachesSpot(p,spot,role)||(owner&&(!human||humans.has(owner))))continue;
     if(owner){
       const bot=s.players[owner];
       releaseStation(bot);bot.job=null;
     }
     p.station=role;p.mx=0;p.my=0;owners.set(role,id);
     if(role==='driver')Object.assign(p,horseRumpAim(s,'right'));
     if(role==='gunner'){
       Object.assign(p,cannonAimCursor(s));
       p.cannonCursor=true;
     }
     break;
   }
 }
}
const offSeat=royal=>royal.state==='wandering'||royal.state==='fallen';
export function royalSootheProgress(royal){
 const distance=royal?.sootheDistance;
 return Number.isFinite(distance)?clamp(distance/ROYAL_SOOTHE_DISTANCE,0,1):0;
}
export function royalStatusNearby(player,royal){
 return !!player&&!!royal&&playerFloor(player)===playerFloor(royal)&&
   Math.hypot(player.x-royal.x,player.y-royal.y)<=ROYAL_STATUS_REACH;
}
export function royalTarget(s,p){
 if(!p||p.ball)return null;
 if(p.royal)return s.royals.find(r=>r.id===p.royal&&r.state==='carried'&&s.players[r.carrier]===p)??null;
 const floor=playerFloor(p);
 let target=null,reach=ROYAL_REACH;
 for(const royal of s.royals){
   if(!offSeat(royal)||playerFloor(royal)!==floor)continue;
   const distance=Math.hypot(p.x-royal.x,p.y-royal.y);
   if(distance<=reach){target=royal;reach=distance;}
 }
 return target;
}
function seatApproach(id){
 const {x,dropY}=ROYAL_SEATS[id];
 return {x,y:dropY,r:ROYAL_RETURN_REACH};
}
export function feedTarget(s,p){
 if(!p?.food||p.ball||p.royal||playerFloor(p)!==DECK_FLOOR)return null;
 return s.royals.find(royal=>royal.state==='seated'&&onSpot(p,seatApproach(royal.id)))??null;
}
export function stationAt(p,s){
 if(!p)return null;
 if(p.ball)return p.slipUntil?'catch':'carry';
 const occupied=occupiedStation(p,s);
 if(occupied)return occupied;
 if(p.spareWheel)return reachesSpot(p,STATIONS['wheel-pickup'],'wheel-pickup')?'wheel-return':'wheel-carry';
 if(feedTarget(s,p))return 'royal-feed';
 const royal=royalTarget(s,p);
 if(royal){
   if(!p.royal)return 'royal-pickup';
   if(royal.upset)return 'royal-soothe';
   return playerFloor(p)===DECK_FLOOR&&onSpot(p,seatApproach(royal.id))?'royal-seat':'royal-carry';
 }
 if(p.food)return 'food-carry';
 if(canTakeSpareWheel(s)&&reachesSpot(p,workSpot(s,'repair'),'repair'))return 'wheel-missing';
 return null;
}
export function aimAngle(p){return clamp(Math.atan2(p.ay,Math.max(.12,p.ax)),-1.06,1.06);}
export function horseCondition(s,side){
 const wear=s.horseWear?.[side];
 if(!wear)return 'fresh';
 if(wear.runaway?.end>s.tick)return 'runaway';
 if(wear.runaway?.tiredUntil>s.tick)return 'tired';
 if(wear.angryUntil>s.tick)return 'angry';
 return 'fresh';
}
export function runawaySide(s){
 return HORSE_SIDES.find(side=>horseCondition(s,side)==='runaway')??null;
}
export function horseProgress(s,side){
 const wear=s.horseWear?.[side];
 switch(horseCondition(s,side)){
   case 'runaway': return clamp((s.tick-wear.runaway.start)/HORSE_RUNAWAY_TICKS,0,1);
   case 'tired': return clamp((s.tick-wear.runaway.end)/HORSE_TIRED_TICKS,0,1);
   case 'angry': return clamp(1-(wear.angryUntil-s.tick)/HORSE_REST_TICKS,0,1);
   default: return clamp(wear?.wounds??0,0,HORSE_RUNAWAY_HITS-1)/HORSE_RUNAWAY_HITS;
 }
}
export function setInput(s,id,input){
 const p=s.players?.[id];if(!p||!input||s.mode!=='run')return;
 if(!['mx','my','ax','ay'].every(k=>Number.isFinite(input[k])))return;
 const role=occupiedStation(p,s),move=Math.hypot(input.mx,input.my),aim=Math.hypot(input.ax,input.ay);
 p.until=s.tick+39;
 p.cookHeld=role==='cook'&&input.cookHeld===true;
 if(role){
   p.mx=0;p.my=0;
   if((input.aimRole!==undefined&&input.aimRole!==role)||(input.aimRole===undefined&&move>0))return;
   if(role==='driver'){
     p.ax=input.ax/Math.max(1,aim);p.ay=input.ay/Math.max(1,aim);
   }else if(role==='gunner'&&!cannonAfterShot(s,p)){
     if(input.aimCursor===true){
       p.ax=clamp(input.ax,-1,1);p.ay=clamp(input.ay,-1,1);
       p.cannonCursor=true;
     }else if(aim>.12){
       p.ax=input.ax/aim;p.ay=input.ay/aim;
       p.cannonCursor=false;
     }
   }
   return;
 }
 p.mx=input.mx/Math.max(1,move);p.my=input.my/Math.max(1,move);
 if(move>.12){p.ax=input.mx/move;p.ay=input.my/move;}
}
export function operate(s,id,atTick=s.tick,taps=1){
 const p=s.players[id],role=stationAt(p,s);if(!role||s.mode!=='run')return false;
 if(role==='carry'||role==='royal-carry'||role==='royal-soothe'||role==='food-carry'||role==='cook'||role==='wheel-carry'||role==='wheel-missing')return false;
 if(role==='wheel-pickup'){
   releaseStation(p,true);
   p.spareWheel=true;
   message(p,s,'예비 바퀴 확보 · 고장 난 바퀴로 가져가세요');
   return true;
 }
 if(role==='wheel-return'){
   p.spareWheel=false;
   p.exitedStation='wheel-pickup';
   message(p,s,'예비 바퀴를 보관함에 돌려놓았습니다');
   return true;
 }
 if(role==='royal-feed')return feedRoyal(s,p);
 if(role==='royal-pickup'||role==='royal-seat')return handleRoyal(s,p,id,role);
 if(role==='driver')return whipHorse(s,p);
 if(role==='cleaner'){
   if(!Number.isInteger(taps)||taps<1||taps>CANNON_CLEAN_TAPS)return false;
   s.ammo.cleaning+=taps;
   if(s.ammo.cleaning>=CANNON_CLEAN_TAPS){
     s.ammo.cleaning=null;
     releaseStation(p);
     message(p,s,'포신 청소 완료 · 장전대가 열렸습니다');
   }
   return true;
 }
 if(role==='gunner'){
   if(s.fuse.end||cannonAfterShot(s,p))return false;
   s.fuse={end:s.tick+FUSE_TICKS,owner:id};saveCannonAim(s,p);
   feedback(s,'gunner','점화! 왼쪽 스틱으로 조준');return true;
 }
 if(role==='catch'){
   if(atTick<p.slipUntil-CATCH_TICKS)return false;
   if(hit(s,role,catchPhase(p,atTick))){
     p.slipUntil=0;p.safeUntil=s.tick+HZ;message(p,s,'포탄을 붙잡았습니다');
   }else dropBall(s,p,true);
   return true;
 }
 if((role==='loader'&&!canLoad(s))||(role==='supply'&&!s.ammo.stock)||s[feedbackKey(role)+'Cd']>s.tick)return false;
 action(s,role,phase({...s,tick:atTick},role),p);
 if(p.station&&!occupiedStation(p,s))releaseStation(p,true);
 return true;
}
export const stageTitle = (stage,chapter=1)=>stageSpec(chapter,stage).title;
export const TRAITS=UPGRADE_CARDS.filter(card=>card.id!=='repair');
export const catchPhase=(p,tick)=>clamp((tick-p.slipUntil+CATCH_TICKS)/CATCH_TICKS,0,1);
function cannonMotion(s){
 let roll=.017*Math.sin(s.tick*.28)+.012*Math.sin(s.tick*.53);
 if(s.fuse.end){
   const litTicks=s.tick-s.fuse.end+FUSE_TICKS;
   const shotPhase=s.fuse.end*.618;
   const buildUp=clamp(litTicks/8,0,1);
   roll+=buildUp*(.23*Math.sin(litTicks*.31+shotPhase)+.085*Math.sin(litTicks*.67-shotPhase));
 }
 const motion=roll+s.shake*.18*Math.sin(s.tick*.53+s.shakeSign);
 return motion*.9*Math.pow(.78,s.traits?.steady??0);
}
export function firingAngle(s,base=s.gunAngle,motion=cannonMotion(s)){
 return clamp(base+motion,-1.1,1.1);
}
export function cannonAimInput(s,x,y){
 return {
   ax:clamp((x-CANNON_AIM_CENTER)/CANNON_AIM_SPAN,-1,1),
   ay:clamp((y-s.y-BARREL.y)/CANNON_AIM_Y,-1,1),
 };
}
export function cannonAimCursor(s,p){
 if(p?.cannonCursor)return {ax:p.ax,ay:p.ay};
 if(!p&&s.gunAim)return {...s.gunAim};
 const angle=p?aimAngle(p):s.gunAngle;
 return cannonAimInput(s,CANNON_AIM_CENTER,s.y+BARREL.y+Math.tan(angle??0)*(CANNON_AIM_CENTER-BARREL.x));
}
function saveCannonAim(s,p){
 s.gunAim=cannonAimCursor(s,p);
 s.gunAngle=Math.atan2(s.gunAim.ay*CANNON_AIM_Y,CANNON_AIM_CENTER+s.gunAim.ax*CANNON_AIM_SPAN-BARREL.x);
}
function cannonOriginY(s){
 return s.y+BARREL.y+s.shake*Math.sin(s.tick*.37*3)*3;
}
export function cannonAim(s,p){
 const {ax,ay}=cannonAimCursor(s,p);
 const x=CANNON_AIM_CENTER+ax*CANNON_AIM_SPAN;
 const originY=cannonOriginY(s);
 const base=Math.atan2(s.y+BARREL.y+ay*CANNON_AIM_Y-originY,x-BARREL.x);
 const motion=cannonMotion(s);
 const wave=.105*Math.sin(s.tick*.105)+.045*Math.sin(s.tick*.21+.9);
 const accuracy=clamp(.86+wave-Math.abs(motion)*.60-s.shake*.20,.32,.98);
 const miss=(1-accuracy)/.68;
 const spreadX=12+38*miss,spreadY=9+34*miss;
 const y=clamp(originY+Math.tan(firingAngle(s,base,motion))*(x-BARREL.x),
   ROAD_TOP+spreadY+6,ROAD_BOTTOM-spreadY-6);
 return {x,y,spreadX,spreadY,angle:Math.atan2(y-originY,x-BARREL.x),accuracy};
}
export function cannonBlastWidth(impact,forward){
 if(forward<0||forward>impact.length)return 0;
 const progress=forward/impact.length;
 const curve=(.55+.95*Math.sin(Math.PI*progress))*Math.sqrt(1-progress);
 return impact.halfWidth*Math.min(1,curve);
}
function message(p,s,msg){p.note=msg;p.noteUntil=s.tick+HZ*1.6;}
function advanceCooking(s){
 if(s.hp<=0)return;
 const cook=Object.values(s.players).find(p=>p.station==='cook');
 if(!cook)return;
 cook.cookTicks??=0;
 const loss=Math.min(cook.cookTicks,Math.round(s.cookShake*COOK_SHAKE_LOSS));
 if(loss>0){
   cook.cookTicks-=loss;
   message(cook,s,'마차 흔들림 · 제작 진행도 '+Math.round(loss*100/COOK_TICKS)+'%p 감소');
 }
 if(cook.until<s.tick)cook.cookHeld=false;
 if(!cook.cookHeld)return;
 cook.cookTicks+=CRYING_COOK_RATE[cryingChildCount(s)];
 if(cook.cookTicks<COOK_TICKS-1e-7)return;
 releaseStation(cook,true);
 cook.food=true;
 message(cook,s,'간식 완성 · 1층에서 좌석에 앉은 아이에게 먹이세요');
}
function whipHorse(s,p,side=null,power=3.3){
 const target=side?null:horseWhipTarget(s,p);
 side??=target?.side;
 if(!side){message(p,s,'오른말이나 왼말을 조준하세요');return false;}
 const wear=(s.horseWear??=horseWear())[side],name=HORSE_NAMES[side];
 const condition=horseCondition(s,side);
 if(condition!=='fresh'){
   const reason={runaway:'폭주 중입니다',tired:'지쳤습니다 · 잠시 쉬게 하세요',angry:'버팁니다 · 잠시 쉬게 하세요'};
   if(p)message(p,s,`${name} ${reason[condition]}`);
   return false;
 }
 if(s.tick-wear.lastHit<WHIP_COOLDOWN)return false;
 const rump=!target||target.zone==='rump';
 const willRunaway=rump&&wear.wounds>=HORSE_RUNAWAY_HITS-1;
 if(willRunaway&&runawaySide(s)){
   if(p)message(p,s,'다른 말이 폭주 중입니다');
   return false;
 }
 wear.lastHit=s.tick;
 wear.lastWhip=target?.local??null;
 if(!rump){
   wear.angryUntil=s.tick+HORSE_REST_TICKS;
   s.horses[side]=0;
   spookHorse(wear,s.tick,HORSE_REST_TICKS,()=>rand(s));
   const msg=name+' '+HORSE_ZONE_NAMES[target.zone]+'에 맞아 날뜁니다';
   s.event=msg;s.eventUntil=s.tick+45;
   feedback(s,'driver',msg);if(p)message(p,s,msg);
   return true;
 }
 wear.wounds=Math.min(HORSE_RUNAWAY_HITS,wear.wounds+1);
 s.horses[side]=Math.min(18,s.horses[side]+power);
 const direction=side==='right'?'화면 위':'화면 아래';
 let msg=name+' 가속 · '+direction;
 if(willRunaway){
   const end=s.tick+HORSE_RUNAWAY_TICKS;
   wear.runaway={start:s.tick,end,tiredUntil:end+HORSE_TIRED_TICKS};
   msg=name+' 폭주 · '+direction;
   s.event=msg;s.eventUntil=end;
 }
 feedback(s,'driver',msg);if(p)message(p,s,msg);
 return true;
}
function fallBall(s,p){
 s.ammo.lost++;
 s.falling.push({x:p.x,y:p.y,side:p.y<0?-1:1,tick:s.tick,floor:playerFloor(p)});
}
function dropBall(s,p,carried){
 if(carried){p.ball=false;p.slipUntil=0;}
 fallBall(s,p);
 const msg=carried?'포탄을 놓쳤습니다 · 1발 손실':'장전 실패 · 포탄 1발 손실';
 message(p,s,msg);s.event=msg;s.eventUntil=s.tick+45;
}
function cannonStep(s){
 if(!s.fuse.end)return;
 const holder=PLAYER_IDS.find(id=>occupiedStation(s.players[id],s)==='gunner');
 const p=s.players[holder];
 if(p){
   s.fuse.owner=holder;
   saveCannonAim(s,p);
 }
 if(s.tick<s.fuse.end)return;
 fireCannon(s,p);
}
function fireCannon(s,p=null){
 const gunner=p??Object.values(s.players).find(player=>occupiedStation(player,s)==='gunner');
 const viewer=gunner&&humanIds(s).find(id=>s.players[id]===gunner);
 const aim=cannonAim(s,p);
 // Sample the displayed ellipse with a separate seed so firing does not perturb other events.
 let seed=(s.seed^Math.imul(s.tick+1,1597334677))>>>0;
 seed=(Math.imul(seed,1664525)+1013904223)>>>0;
 const scatterAngle=seed/4294967296*TAU;
 seed=(Math.imul(seed,1664525)+1013904223)>>>0;
 const scatterRadius=Math.sqrt(seed/4294967296);
 const landing={x:aim.x+Math.cos(scatterAngle)*scatterRadius*aim.spreadX,
   y:aim.y+Math.sin(scatterAngle)*scatterRadius*aim.spreadY};
 const originY=cannonOriginY(s);
 const angle=Math.atan2(landing.y-originY,landing.x-BARREL.x);
 const dx=Math.cos(angle),dy=Math.sin(angle),caliber=cannonScale(s);
 const muzzle=BARREL.length*caliber;
 const x=BARREL.x+muzzle*dx,y=originY+muzzle*dy;
 s.shots.push({x,y,vx:CANNON_SHOT_SPEED*dx,vy:CANNON_SHOT_SPEED*dy,caliber,radius:CANNON_SHOT_RADIUS*caliber,landing});
 s.cannonRecoil={tick:s.tick,x,y,angle,caliber};
 s.ammo.loaded=false;s.ammo.cleaning=0;s.fuse={end:0,owner:null};
 if(viewer)s.cannonExit={id:viewer,tick:s.tick};
 else if(gunner){
   releaseStation(gunner);
   message(gunner,s,'포탄 발사 · 포신을 청소하세요');
 }
 feedback(s,'gunner','포탄 자동 발사!');impact(s,1.45,'대포 반동',ROYAL_FALL_CHANCE.cannon);
}
export const enemyKnockback=enemy=>enemy.cannonKnock??enemy.wheelKnock;
const activeEnemy=enemy=>!enemy.spent&&!enemyKnockback(enemy);
const pursuitSlot=enemy=>enemy.attackSlot??(enemy.id??0)%PURSUIT_SLOTS.length;
function pursuitPace(s,profile,base){
 if(profile)return (profile.speed-s.speed)*ROAD_UNITS_PER_METER;
 return 18+Math.min(s.stage*3,24)-(s.speed-base)*2.05;
}
export function incomingPursuit(s,view){
 const span=view.span/view.zoom;
 const approachStart=(view.right??view.left+span)-Math.min(100,span*.1);
 const base=baseSpeed(s);
 let incoming=null;
 for(const enemy of s.enemies){
   if(!activeEnemy(enemy)||enemy.lastStrike!=null||enemy.x+CAVALRY_ART_X<approachStart)continue;
   if(pursuitPace(s,ENEMY_PROFILES[enemy.profile],base)<=0)continue;
   if(!incoming||enemy.x<incoming.x)incoming=enemy;
 }
 return incoming;
}
function landCannonShot(s,shot){
 const caliber=shot.caliber??1,upgrade=Math.min(.5,caliber-1);
 const impact={...shot.landing,road:s.road,tick:s.tick,id:s.nextId++,caliber,
   length:200+70*upgrade,halfWidth:87+40*upgrade};
 (s.cannonImpacts??=[]).push(impact);
 for(const enemy of s.enemies){
   if(!activeEnemy(enemy))continue;
   const forward=enemy.x+CAVALRY_BODY.x-impact.x;
   const across=enemy.y+CAVALRY_BODY.y-impact.y;
   if(forward<0||forward>impact.length||Math.abs(across)>cannonBlastWidth(impact,forward))continue;
   const side=Math.sign(across)||1;
   enemy.cannonKnock={tick:s.tick,vx:205-64*forward/impact.length,vy:side*(72+Math.abs(across)*.9),spin:side*8.5};
   s.kill++;
   burst(s,enemy.x+CAVALRY_BODY.x,enemy.y+CAVALRY_BODY.y,'hit');
 }
}
function advanceShots(s){
 for(const shot of s.shots){
   if(shot.spent)continue;
   const x=shot.x,y=shot.y;
   const lands=shot.landing&&x+shot.vx*STEP>=shot.landing.x;
   const dx=lands?shot.landing.x-x:shot.vx*STEP;
   const dy=lands?shot.landing.y-y:shot.vy*STEP;
   shot.x+=dx;shot.y+=dy;
   if(shot.enemy){
     if(shot.x<=(shot.hitX??728))shot.spent=true;
     continue;
   }
   // Sweep the shell against the mounted body, with extra clearance from the barrel upgrade.
   const upgrade=(shot.caliber??1)-1,radius=cannonShotRadius(shot);
   const hitX=CAVALRY_BODY.rx+radius+22*upgrade,hitY=CAVALRY_BODY.ry+radius+30*upgrade;
   for(const enemy of s.enemies){
     if(!activeEnemy(enemy))continue;
     const at=ellipseEntry(x-enemy.x-CAVALRY_BODY.x,y-enemy.y-CAVALRY_BODY.y,dx,dy,hitX,hitY);
     if(at===null)continue;
     enemy.spent=true;s.kill++;burst(s,x+dx*at,y+dy*at,'hit');
   }
   if(lands){
     landCannonShot(s,shot);
     shot.spent=true;
   }
 }
 s.shots=s.shots.filter(shot=>!shot.spent&&shot.x<1280&&shot.y>ROAD_TOP-32&&shot.y<ROAD_BOTTOM+32);
 s.enemies=s.enemies.filter(enemy=>!enemy.spent);
 if(s.cannonImpacts?.length)s.cannonImpacts=s.cannonImpacts.filter(impact=>s.tick-impact.tick<CANNON_IMPACT_TICKS);
}
function finishCarry(s,p){
 if(!p.ball)return;
 if(p.slipUntil){
   if(s.tick>=p.slipUntil)dropBall(s,p,true);
 }else if(reachesSpot(p,STATIONS.rack,'deliver')){
   p.ball=false;s.ammo.rack++;message(p,s,'적재칸에 포탄을 내려놓았습니다');
 }
}
function advanceStairs(s,p,human){
 if(!onSpot(p,STAIRS)){p.stairLocked=false;return;}
 if(p.stairLocked)return;
 const floor=playerFloor(p);
 if(!human){
   const target=p.job==='royal-pickup'?spotFor(s,p.job,p):null;
   if(botJobFloor(p,p.job,target)===floor)return;
 }
 p.stairLocked=true;
 if(p.spareWheel){message(p,s,'예비 바퀴를 지하층 수리 지점으로 가져가세요');return;}
 p.floor=floor===DECK_FLOOR?CELLAR_FLOOR:DECK_FLOOR;
 p.exitedStation=null;
 message(p,s,p.floor===CELLAR_FLOOR?'지하층으로 내려왔습니다':'1층으로 올라왔습니다');
}
const TAU=Math.PI*2;
function rand(s){ s.seed=(Math.imul(s.seed,1664525)+1013904223)>>>0;return s.seed/4294967296; }
function fallRoyals(s,chance){
 // Sample from the impact's tick and shake seed without changing the road's random sequence.
 const rolls={seed:s.seed^Math.imul(s.tick,0x9e3779b9)};
 for(const royal of s.royals){
   if(royal.state!=='seated'||rand(rolls)>=chance)continue;
   const seat=ROYAL_SEATS[royal.id];
   const upset=rand(rolls)<ROYAL_FALL_CRY_CHANCE;
   Object.assign(royal,{state:'fallen',x:seat.x,y:seat.dropY,fallAt:s.tick,upset,sootheDistance:0});
   royal.crying=royalIsCrying(royal);
 }
}
function feedRoyal(s,p){
 const royal=feedTarget(s,p);
 if(!royal)return false;
 p.food=false;
 royal.hunger=Math.max(0,royal.hunger-SNACK_HUNGER_RELIEF);
 royal.crying=royalIsCrying(royal);
 recordFeeding(s,royal.id);
 message(p,s,royal.name+' 간식 먹이기 완료');
 return true;
}
function rollsRoyalBoredom(s,royal){
 // Keep stage RNG and its later events independent of this carry.
 const salt=royal.id==='prince'?0x85ebca6b:0xc2b2ae35;
 const rolls={seed:s.seed^Math.imul(s.tick+1,0x9e3779b9)^salt};
 rand(rolls);
 return rand(rolls)<ROYAL_BOREDOM_CHANCE;
}
function royalExcursion(p,royal){
 if(playerFloor(p)===CELLAR_FLOOR)return true;
 const atDeckEnd=p.x<=DECK.left+ROYAL_DECK_END||p.x>=DECK.right-ROYAL_DECK_END;
 return atDeckEnd&&Math.hypot(p.x-royal.boredFrom.x,p.y-royal.boredFrom.y)>=ROYAL_EXCURSION_DISTANCE;
}
function rockRoyal(s,p,distance){
 const royal=royalTarget(s,p);
 if(!royal?.upset)return;
 if(royal.bored){
   if(!royalExcursion(p,royal))return;
   royal.bored=false;delete royal.boredFrom;
   message(p,s,royal.name+' 기분 전환 · 계속 걸어서 달래 주세요');
   return;
 }
 if(distance<=0)return;
 royal.sootheDistance=(royal.sootheDistance??0)+distance;
 if(royal.boredomPending&&royal.sootheDistance>=ROYAL_BOREDOM_DISTANCE){
   royal.boredomPending=false;
   if(playerFloor(p)===DECK_FLOOR){
     royal.bored=true;royal.boredFrom={x:p.x,y:p.y};
     message(p,s,royal.name+' 지루해요 · 마차 앞뒤나 지하층으로 가세요');
     return;
   }
 }
 if(royal.sootheDistance<ROYAL_SOOTHE_DISTANCE-1e-7)return;
 royal.sootheDistance=ROYAL_SOOTHE_DISTANCE;
 royal.upset=false;
 royal.crying=royalIsCrying(royal);
 message(p,s,royal.name+' 달래기 완료 · 아이를 좌석으로 데려가세요');
}
function handleRoyal(s,p,id,role){
 const royal=royalTarget(s,p);
 const seat=ROYAL_SEATS[royal.id];
 if(role==='royal-pickup'){
   royal.state='carried';royal.carrier=id;royal.x=p.x;royal.y=p.y;royal.floor=playerFloor(p);
   royal.bored=false;royal.boredomPending=royal.upset&&rollsRoyalBoredom(s,royal);
   royal.mischief=null;royal.destination=null;p.royal=royal.id;
   delete royal.fallAt;
 }else{
   royal.state='seated';royal.carrier=null;royal.x=seat.x;royal.y=seat.y;royal.floor=DECK_FLOOR;
   royal.bored=false;royal.boredomPending=false;delete royal.boredFrom;
   royal.attention=100;royal.restUntil=s.tick+ROYAL_REST;royal.sootheDistance=0;p.royal=null;
   recordReseat(s,royal.id);
   message(p,s,royal.name+' 좌석 복귀 · 관심 회복');
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
 if(royal.mischief==='rack'&&s.ammo.rack>0){
   s.ammo.rack--;fallBall(s,royal);
 }else if(royal.mischief==='gunner'&&cannonStation(s)==='gunner'&&!s.fuse.end&&!s.cannonExit){
   const side=rand(s)<.5?-1:1,offset=rand(s);
   saveCannonAim(s,{ax:-.55+offset*1.5,ay:side*(.68+offset*.32),cannonCursor:true});
   fireCannon(s);
 }else if(royal.mischief==='driver'){
   const obstacle=s.obstacles.find(o=>!o.passed&&o.x>40&&o.x<287&&Math.abs(s.y-obstacleY(o))>16);
   const toward=obstacle?(obstacleY(obstacle)<s.y?'right':'left'):rand(s)<.5?'right':'left';
   const side=horseCondition(s,toward)==='fresh'?toward:toward==='right'?'left':'right';
   if(!whipHorse(s,null,side,8))return;
 }else return;
 s.royalMischiefUntil=s.tick+6*HZ;
}
function wanderRoyal(s,royal){
 const floor=playerFloor(royal),onDeck=floor===DECK_FLOOR;
 if(onDeck&&onSpot(royal,STAIRS)){
   royal.floor=CELLAR_FLOOR;
   // Keep the stair exit clear and give each child a different direction downstairs.
   setRoyalDestination(s,royal,{x:STAIRS.x+(royal.id==='prince'?-42:42),y:STAIRS.y-30});
   return;
 }
 if(royal.destination&&s.tick>=royal.moveUntil){
   royal.destination=null;royal.mischief=null;
 }
 if(!royal.destination){
   if(onDeck&&s.tick>=royal.nextMischief&&!(s.royalMischiefUntil>s.tick)){
     const choices=['driver'];
     if(s.ammo.rack>0)choices.push('rack');
     if(cannonStation(s)==='gunner'&&!s.fuse.end)choices.push('gunner');
     const kind=choices[Math.floor(rand(s)*choices.length)];
     setRoyalDestination(s,royal,STATIONS[kind],kind);
     royal.nextMischief=s.tick+(9+rand(s)*4)*HZ;
   }else{
     const xRoll=rand(s),yRoll=rand(s);
     if(onDeck&&xRoll<ROYAL_STAIRS_CHANCE)setRoyalDestination(s,royal,STAIRS);
     else{
       const x=DECK.left+22+xRoll*(DECK.right-DECK.left-44);
       const top=onDeck?-34:DECK.top+14;
       const y=top+yRoll*(62-top);
       setRoyalDestination(s,royal,{x,y});
     }
   }
 }
 const {x,y}=royal.mischief==='rack'?STATIONS.rack:royal.destination;
 const dx=x-royal.x,dy=y-royal.y,length=Math.hypot(dx,dy);
 if(length<=5){
   if(royal.mischief)royalMischief(s,royal);
   royal.destination=null;royal.mischief=null;
   return;
 }
 const pace=ROYAL_WALK*STEP/length,leavingBench=royal.y<-48;
 const players=Object.values(s.players);
 slide(royal,royal.x+dx*pace,royal.y+dy*pace,(x,y)=>
   (onDeck&&!leavingBench&&bench(x,y))||
   players.some(p=>playerFloor(p)===floor&&blocksStep(royal,p,x,y,ROYAL_BODY))||
   s.royals.some(other=>other!==royal&&offSeat(other)&&playerFloor(other)===floor&&blocksStep(royal,other,x,y,ROYAL_BODY))
 );
}
function advanceRoyals(s){
 for(const royal of s.royals){
   royal.hunger=Math.min(100,royal.hunger+ROYAL_HUNGER_RATE*STEP);
   royal.crying=royalIsCrying(royal);
   if(royal.state==='carried'){
     const p=s.players[royal.carrier];
     royal.x=p.x;royal.y=p.y;royal.floor=playerFloor(p);
     continue;
   }
   if(royal.state==='wandering'){
     wanderRoyal(s,royal);
     continue;
   }
   if(royal.state==='fallen')continue;
   if(s.tick<royal.restUntil)continue;
   royal.attention=Math.max(0,royal.attention-4.2*STEP);
   if(royal.attention>ROYAL_ATTENTION_LOW)continue;
   const seat=ROYAL_SEATS[royal.id];
   royal.state='wandering';royal.wanderedAt=s.tick;royal.nextMischief=s.tick+(4+rand(s)*2)*HZ;
   setRoyalDestination(s,royal,{x:seat.x,y:seat.dropY});
 }
}
export function catapultX(s){
 if(!s.catapult)return null;
 return CATAPULT_TARGET_X+(s.distance-s.catapult.distance)*ROAD_UNITS_PER_METER;
}
export function catapultHitsWagon(s,x=catapultX(s)){
 const rock=s.catapult;
 if(!rock)return false;
 const dx=x-clamp(x,CART_HULL.left,CART_HULL.right);
 const dy=rock.y-clamp(rock.y,s.y-CART_HULL.halfHeight,s.y+CART_HULL.halfHeight);
 return Math.hypot(dx/CATAPULT_RADIUS.x,dy/CATAPULT_RADIUS.y)<=1;
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
  return {mode:'ready',...location,wave:0,traits,seed,tick:0,distance:0,goal,speed:13+traits.rein*3,hp,boost:0,lane:1,y:CART_CENTER,shake:0,shakeSign:1,fuse:{end:0,owner:null},cannonRecoil:null,falling:[],kill:0,nextId:10,
    obstacles:[],roadWarning:null,
    enemies:[],shots:[],particles:[],cannonImpacts:[],
    nextRock:ROAD_FIRST_WARNING_METERS,catapult:null,nextCatapult:CATAPULT_FIRST,driveCd:0,gunCd:0,driveMsg:'',gunMsg:'',driveUntil:0,gunUntil:0,event:'',eventUntil:0,serial:0,road:0,players:crew(),humans:1,horses:{right:0,left:0},horseWear:horseWear(),ammo:supplies(),royals:passengers(),gunAngle:0,wheel,looseWheels:[],fixCd:0,fixMsg:'',fixUntil:0,
    affection:0,care:newCare(),intermission:null,stageEntry:{hp,wheel:{...wheel}}};
}
export function begin(s,{testMode=false}={}){
 if(s.mode!=='ready')return;
 if(testMode)s.testMode=true;
 s.mode='run';
}
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
export function width(s,role){
 const base=Math.min(.34,(role==='repair'?.26:.235)+s.traits.sight*.027);
 return base*CRYING_TIMING_WIDTH[cryingChildCount(s)];
}
export function target(s,role){return role==='repair'?[.46,.82][s.wheel.progress]:({supply:.54,loader:.68,catch:.57})[role];}
export function phase(s,role){const t=s.tick/HZ;const freq=role==='repair'?.55:.49;
  return ((t*(freq+Math.min(.17,(s.stage-1)*.026))+.09)%1+1)%1;}
export function hit(s,role,p=phase(s,role)){const gap=Math.abs(((p-target(s,role)+1.5)%1)-.5);return gap<=width(s,role)/2;}
function impact(s,power,msg,royalFallChance=0){
 const cushion=Math.pow(.78,s.traits.spring);
 s.shake=Math.min(1.45,power*cushion);s.shakeSign=rand(s)*TAU;s.event=msg;s.eventUntil=s.tick+42;s.serial++;
 s.cookShake=Math.max(s.cookShake,s.shake);
 if(royalFallChance)fallRoyals(s,royalFallChance*cushion);
 for(const p of Object.values(s.players)){
   if(p.ball&&!p.slipUntil&&!(p.safeUntil>s.tick)&&s.shake>.40)p.slipUntil=s.tick+CATCH_TICKS;
 }
}
function feedback(s,role,msg){const key=feedbackKey(role);s[key+'Msg']=msg;s[key+'Until']=s.tick+26;s[key+'Cd']=s.tick+8;}
function loseWheel(s){
 if(s.wheel.broken!==-1||s.distance<s.wheel.eligible)return;
 const id=Math.floor(rand(s)*WHEELS.length);
 const mount=WHEELS[id],side=Math.sign(mount.y),rear=id%2===1;
 const shake=s.shake*Math.sin(s.tick*.37*3)*3;
 (s.looseWheels??=[]).push({
   id:s.nextId++,mount:id,x:mount.x,y:s.y+shake+side*WHEEL_MOUNT_OFFSET,
   vx:(rear?126:134)+s.speed*2,vy:side*(rear?-46:84),angle:0,launchTick:s.tick,
 });
 Object.assign(s.wheel,{broken:id,progress:0,since:s.tick,first:false});
 impact(s,1.2,mount.name+' 이탈');
}
function burst(s,x,y,kind){s.particles.push({x,y,kind,life:18,id:s.nextId++});}
function ellipseEntry(x,y,dx,dy,rx,ry){
  const startX=x/rx,startY=y/ry,stepX=dx/rx,stepY=dy/ry;
  const distance=startX*startX+startY*startY-1;
  if(distance<=0)return 0;
  const movement=stepX*stepX+stepY*stepY;
  const approach=startX*stepX+startY*stepY;
  if(!movement||approach>=0)return null;
  const discriminant=approach*approach-movement*distance;
  if(discriminant<0)return null;
  const entry=(-approach-Math.sqrt(discriminant))/movement;
  return entry<=1?entry:null;
}
function wheelSurfaces(s){
  const solids=roadsideTrees(s.road,ROAD_TOP,ROAD_BOTTOM).map(({x,y,r})=>({x,y,rx:r,ry:r}));
  for(const obstacle of s.obstacles){
    if(obstacle.type!=='log'&&obstacle.type!=='rock')continue;
    const {halfWidth}=roadObstacleProfile(obstacle.type);
    const rx=obstacle.type==='log'?halfWidth*.7:halfWidth;
    solids.push({x:obstacle.x,y:obstacleY(obstacle),rx,ry:halfWidth});
  }
  return solids;
}
function nextWheelBounce(wheel,dx,dy,travel,solids){
  let bounce=null;
  const relativeDx=dx-travel;
  for(const solid of solids){
    const startX=wheel.x-solid.x+travel,startY=wheel.y-solid.y;
    const rx=solid.rx+LOOSE_WHEEL_RADIUS,ry=solid.ry+LOOSE_WHEEL_RADIUS;
    const at=ellipseEntry(startX,startY,relativeDx,dy,rx,ry);
    if(at===null||(bounce&&at>=bounce.at))continue;
    let nx=(startX+relativeDx*at)/(rx*rx),ny=(startY+dy*at)/(ry*ry);
    const length=Math.hypot(nx,ny);
    if(!length)continue;
    nx/=length;ny/=length;
    const approach=relativeDx*nx+dy*ny;
    if(approach>=0)continue;
    bounce={at,nx,ny,approach};
  }
  return bounce;
}
function rollWheelSegment(s,wheel,dx,dy,from,to,pursuerStarts){
  for(const enemy of s.enemies){
    if(!activeEnemy(enemy))continue;
    const start=pursuerStarts?.get(enemy.id)??enemy;
    const moveX=enemy.x-start.x,moveY=enemy.y-start.y;
    const x=wheel.x-(start.x+moveX*from+CAVALRY_BODY.x),y=wheel.y-(start.y+moveY*from+CAVALRY_BODY.y);
    const at=ellipseEntry(x,y,dx-moveX*(to-from),dy-moveY*(to-from),CAVALRY_BODY.rx+LOOSE_WHEEL_RADIUS,CAVALRY_BODY.ry+LOOSE_WHEEL_RADIUS);
    if(at===null)continue;
    const time=from+(to-from)*at;
    enemy.x=start.x+moveX*time;enemy.y=start.y+moveY*time;
    const kick=Math.sign(enemy.y+CAVALRY_BODY.y-wheel.y-dy*at)||Math.sign(wheel.vy)||1;
    enemy.wheelKnock={tick:s.tick,vx:Math.max(102,wheel.vx*.82+48),vy:clamp(wheel.vy*.46+kick*110,-170,170),spin:kick*7.4};
    s.kill++;
    burst(s,wheel.x+dx*at,wheel.y+dy*at,'hit');
  }
  wheel.x+=dx;wheel.y+=dy;
}
function advanceLooseWheels(s,travel,pursuerStarts){
  if(!s.looseWheels?.length)return;
  const solids=wheelSurfaces(s),groundSpeed=travel/STEP;
  for(const wheel of s.looseWheels){
    if(wheel.launchTick===s.tick)continue;
    const dx=wheel.vx*STEP,dy=wheel.vy*STEP;
    const bounce=nextWheelBounce(wheel,dx,dy,travel,solids);
    const fraction=bounce?.at??1;
    rollWheelSegment(s,wheel,dx*fraction,dy*fraction,0,fraction,pursuerStarts);
    if(bounce){
      const {nx,ny,approach}=bounce;
      const rebound=1.72*approach/STEP;
      wheel.vx-=rebound*nx;wheel.vy-=rebound*ny;
      wheel.x+=nx*.65;wheel.y+=ny*.65;
      wheel.bounceTick=s.tick;
      burst(s,wheel.x,wheel.y,'dust');
      rollWheelSegment(s,wheel,wheel.vx*STEP*(1-fraction),wheel.vy*STEP*(1-fraction),fraction,1,pursuerStarts);
    }
    const rollingX=wheel.vx-groundSpeed;
    wheel.angle+=Math.sign(rollingX||wheel.vx)*Math.hypot(rollingX,wheel.vy)*STEP/LOOSE_WHEEL_RADIUS;
    const offRoad=wheel.y<ROAD_TOP||wheel.y>ROAD_BOTTOM;
    wheel.vx+=(groundSpeed+60-wheel.vx)*(offRoad?.044:.025);
    wheel.vy*=offRoad?.88:.994;
  }
  s.looseWheels=s.looseWheels.filter(wheel=>wheel.x<LOOSE_WHEEL_EXIT_X);
}
function damageWagon(s,damage){
  s.hp-=damage;
  if(s.testMode)s.hp=Math.max(1,s.hp);
}
const roadObstacleAhead=obstacle=>!obstacle.passed&&obstacle.x<ROAD_OBSTACLE_HIT_END;
export function roadApproachWarning(s,view){
  if((s.mode!=='run'&&s.mode!=='paused')||view.floor===CELLAR_FLOOR||(view.role&&view.role!=='driver'))return null;
  const edge=view.left-ROAD_OBSTACLE_DRAW_MARGIN;
  const travelPerTick=Math.max(1,s.speed)*ROAD_SCROLL_UNITS_PER_METER/HZ;
  const range=travelPerTick*ROAD_WARNING_TICKS;
  const approaching=s.obstacles.find(obstacle=>roadObstacleAhead(obstacle)&&obstacle.x<edge&&obstacle.x>=edge-range);
  if(approaching)return {x:approaching.x,y:obstacleY(approaching)};
  const pending=s.roadWarning;
  if(!pending)return null;
  const untilSpawn=Math.max(0,pending.spawnAt-s.tick);
  return Math.max(0,edge-pending.x)+untilSpawn*travelPerTick<=range?pending:null;
}
function roadStrikeNear(s,x,landIn,spawnIn=0){
  const ticksPerUnit=HZ/(Math.max(1,s.speed)*ROAD_SCROLL_UNITS_PER_METER);
  const contact=spawnIn+Math.max(0,ROAD_OBSTACLE_HIT_START-x)*ticksPerUnit;
  const clear=spawnIn+(ROAD_OBSTACLE_HIT_END-x)*ticksPerUnit;
  return landIn>=contact-ROAD_STRIKE_MARGIN&&landIn<=clear+ROAD_STRIKE_MARGIN;
}
const roadLaneClear=(y,obstacle)=>Math.abs(y-obstacleY(obstacle))>=roadObstacleProfile(obstacle.type).halfWidth+6;
function sharedEscapeLane(s,rockY){
  const rockClearance=CART_HULL.halfHeight+CATAPULT_RADIUS.y+8;
  return LANES.some(y=>Math.abs(y-rockY)>=rockClearance&&
    (!s.roadWarning||roadLaneClear(y,s.roadWarning))&&
    !s.obstacles.some(obstacle=>roadObstacleAhead(obstacle)&&!roadLaneClear(y,obstacle)));
}
function scheduleRoadObstacle(s){
  const warning=s.roadWarning;
  if(warning){
    if(s.tick<warning.spawnAt)return;
    const rock=s.catapult;
    if(rock&&s.tick<rock.landAt&&roadStrikeNear(s,warning.x,rock.landAt-s.tick)){
      // Keep the announced lane if an airborne rock would land during this approach.
      warning.spawnAt=s.tick+ROAD_STRIKE_MARGIN;
      return;
    }
    s.obstacles.push({id:s.nextId++,x:warning.x,y:warning.y,type:warning.type});
    s.roadWarning=null;
    return;
  }
  if(s.distance<s.nextRock||s.obstacles.some(roadObstacleAhead))return;
  const type=ROAD_OBSTACLE_TYPES[Math.floor(rand(s)*ROAD_OBSTACLE_TYPES.length)];
  const y=rand(s)<.5?CART_TOP:CART_BOTTOM;
  s.roadWarning={type,x:ROAD_OBSTACLE_ENTRY_X,y,spawnAt:s.tick+ROAD_WARNING_TICKS};
  s.nextRock=s.distance+Math.max(125,138-(s.stage-1)*4)+rand(s)*12;
}
function advanceObstacles(s,travel){
  for(const obstacle of s.obstacles){
    obstacle.x+=travel;
    if(obstacle.passed||obstacle.x<ROAD_OBSTACLE_HIT_START||obstacle.x>=ROAD_OBSTACLE_HIT_END)continue;
    const profile=roadObstacleProfile(obstacle.type);
    if(Math.abs(s.y-obstacleY(obstacle))>=profile.halfWidth)continue;
    obstacle.passed=true;
    damageWagon(s,profile.damage);
    const activeFactor=s.obstacleSlow?.factor??1;
    if(profile.slowFactor<=activeFactor){
      const until=s.tick+Math.round(profile.slowSeconds*HZ);
      s.speed*=profile.slowFactor/activeFactor;
      s.obstacleSlow={
        factor:profile.slowFactor,
        until:profile.slowFactor===activeFactor?Math.max(s.obstacleSlow?.until??0,until):until
      };
    }
    impact(s,.98,profile.name+' 충돌',ROYAL_FALL_CHANCE.obstacle);
    burst(s,292,obstacleY(obstacle),'dust');
    loseWheel(s);
  }
  s.obstacles=s.obstacles.filter(obstacle=>obstacle.x<1260);
  scheduleRoadObstacle(s);
}
function advanceCatapult(s){
  const rock=s.catapult;
  if(!rock){
    if(s.tick<s.nextCatapult)return;
    const warning=s.roadWarning;
    if((warning&&roadStrikeNear(s,warning.x,CATAPULT_FLIGHT,warning.spawnAt-s.tick))
        ||s.obstacles.some(obstacle=>roadObstacleAhead(obstacle)&&roadStrikeNear(s,obstacle.x,CATAPULT_FLIGHT)))return;
    const rolls={seed:s.seed};
    const offset=76+rand(rolls)*8;
    const clearance=CART_HULL.halfHeight+CATAPULT_RADIUS.y-offset+8;
    let side=rand(rolls)<.5?'right':'left';
    if(s.y-CART_TOP<clearance)side='right';
    else if(CART_BOTTOM-s.y<clearance)side='left';
    if(warning||s.obstacles.some(roadObstacleAhead)){
      // A speed change can invalidate the overlap estimate. Always preserve a shared escape lane.
      const rockY=s.y+offset*(side==='right'?-1:1);
      if(!sharedEscapeLane(s,rockY)){
        if(!sharedEscapeLane(s,2*s.y-rockY))return;
        side=side==='right'?'left':'right';
      }
    }
    s.seed=rolls.seed;
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
  rock.hit=catapultHitsWagon(s);
  if(rock.hit){
    damageWagon(s,22);
    s.speed*=.78;
    impact(s,1.42,'투석기 바위 명중',ROYAL_FALL_CHANCE.rock);
    loseWheel(s);
  }else{
    s.event='투석기 바위 회피';
    s.eventUntil=s.tick+42;
  }
}
function waveLoopTicks(waves){
  if(!waves.length)return 12*HZ;
  const last=waves.at(-1).at;
  const gap=waves.length>1?Math.max(1,last-waves.at(-2).at):12;
  return (last-waves[0].at+gap)*HZ;
}
function advanceWaves(s){
  const {waves}=stageSpec(s.chapter,s.stage);
  if(!waves.length)return;
  const loopTicks=s.testMode?waveLoopTicks(waves):0;
  while(s.testMode||s.wave<waves.length){
    const {at,groups}=waves[s.wave%waves.length];
    const startsAt=at*HZ+Math.floor(s.wave/waves.length)*loopTicks;
    if(s.tick<startsAt)break;
    s.wave++;
    if(s.testMode){
      const incoming=groups.reduce((total,{count})=>total+count,0);
      let excess=s.enemies.length+incoming-TEST_ENEMY_LIMIT;
      if(excess>0)s.enemies=s.enemies.filter(enemy=>{
        if(excess<=0||!activeEnemy(enemy))return true;
        excess--;
        return false;
      });
    }
    const assigned=PURSUIT_SLOTS.map(()=>0),furthest=PURSUIT_SLOTS.map(()=>-Infinity);
    for(const enemy of s.enemies){
      if(!activeEnemy(enemy))continue;
      const slot=pursuitSlot(enemy);
      assigned[slot]++;
      furthest[slot]=Math.max(furthest[slot],enemy.x);
    }
    let offset=0;
    for(const {enemy,count} of groups){
      const {kind}=ENEMY_PROFILES[enemy];
      for(let i=0;i<count;i++){
        let attackSlot=rand(s)<.5?0:1;
        for(let slot=0;slot<PURSUIT_SLOTS.length;slot++){
          if(assigned[slot]<assigned[attackSlot])attackSlot=slot;
        }
        const y=s.y+PURSUIT_SLOTS[attackSlot].y+rand(s)*24-12;
        const x=Math.max(WAVE_ENTRY_X+offset,furthest[attackSlot]+PURSUIT_GAP);
        s.enemies.push({id:s.nextId++,x,y,kind,profile:enemy,attackSlot});
        assigned[attackSlot]++;
        furthest[attackSlot]=x;
        offset+=WAVE_SPACING;
      }
    }
  }
}
function advancePursuers(s,base){
  if(!s.enemies.length)return;
  const ahead=[];
  // Run the nearest enemy in each lane first so reinforcements can keep their spacing.
  for(const enemy of [...s.enemies].sort((a,b)=>a.x-b.x)){
    if(enemy.spent)continue;
    const knocked=enemyKnockback(enemy);
    if(knocked){
      if(s.tick-knocked.tick>=WHEEL_KNOCK_TICKS)enemy.spent=true;
      else{
        enemy.x+=knocked.vx*STEP;enemy.y+=knocked.vy*STEP;
        knocked.vx*=.965;knocked.vy*=.965;
      }
      continue;
    }
    if(s.testMode&&s.tick>=(enemy.testLeavesAt??Infinity)){enemy.spent=true;continue;}
    const slotIndex=pursuitSlot(enemy);
    const slot=PURSUIT_SLOTS[slotIndex];
    const leaderX=ahead[slotIndex];
    const targetY=s.y+slot.y;
    const sinceStrike=s.tick-(enemy.lastStrike??-Infinity);
    if(sinceStrike<=ENEMY_RETREAT_TICKS){
      enemy.x+=ENEMY_RETREAT_SPEED*STEP;
      ahead[slotIndex]=enemy.x;
      continue;
    }
    const returnAge=sinceStrike-ENEMY_RETREAT_TICKS-ENEMY_RECOVERY_TICKS;
    const recovering=returnAge<=0;
    const paceScale=ENEMY_RECOVERY_SPEED+(1-ENEMY_RECOVERY_SPEED)*clamp(returnAge/ENEMY_RETURN_TICKS,0,1);
    if(enemy.x<ENEMY_TRACK_X){
      const lateralStep=ENEMY_TRACK_SPEED*paceScale*STEP;
      enemy.y+=clamp(targetY-enemy.y,-lateralStep,lateralStep);
    }
    const profile=ENEMY_PROFILES[enemy.profile];
    const pace=pursuitPace(s,profile,base);
    const nextX=enemy.x-pace*paceScale*STEP;
    const stopX=leaderX===undefined?slot.strikeX:Math.max(slot.strikeX,
      Math.min(enemy.x+ENEMY_RETREAT_SPEED*STEP,leaderX+PURSUIT_GAP));
    enemy.x=Math.max(stopX,nextX);
    ahead[slotIndex]=enemy.x;
    if(leaderX!==undefined||recovering||nextX>=slot.strikeX||Math.abs(targetY-enemy.y)>ENEMY_STRIKE_REACH)continue;
    enemy.lastStrike=s.tick;
    if(s.testMode)enemy.testLeavesAt??=s.tick+TEST_ENEMY_ENGAGEMENT;
    damageWagon(s,profile?.damage??14);
    impact(s,1.32,'추격대의 일격');
    burst(s,slot.hitX,targetY,'hit');
    loseWheel(s);
    const vx=-430,vy=(targetY-enemy.y)*(-vx)/(enemy.x-slot.hitX);
    s.shots.push({x:enemy.x,y:enemy.y,vx,vy,enemy:true,hitX:slot.hitX});
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
    releaseStation(operator,true);
    return;
  }
  if(!hit(s,role,p)){tell('타이밍 실패');return;}
  if(role==='repair'){
    if(s.wheel.broken<0)return;
    if(s.wheel.progress===0){
      s.wheel.progress=1;
      for(const player of Object.values(s.players)){
        if(!player.spareWheel)continue;
        player.spareWheel=false;
        if(player!==operator)message(player,s,'바퀴 장착 완료 · 예비 바퀴를 보관함으로 돌려보냈습니다');
      }
      tell('바퀴 장착 완료');
    }
    else{s.wheel.broken=-1;s.wheel.progress=0;s.wheel.eligible=s.distance+240;
      s.event='바퀴 수리 완료';s.eventUntil=s.tick+60;tell('수리 완료 · 속도 회복');}
    return;
  }
  if(role==='supply'){
    operator.ball=true;operator.slipUntil=0;s.ammo.stock--;tell('포탄 확보 · 계단으로 1층 적재칸에 운반');return;
  }
}
export function step(s){
  if(s.mode!=='run')return;
  s.tick++;
  const afterShot=s.cannonExit;
  if(afterShot&&s.tick>=afterShot.tick+CANNON_VIEW_HOLD_TICKS+CANNON_VIEW_RETURN_TICKS)finishCannonExit(s);
  if(s.testMode&&s.ammo.stock<=0)s.ammo.stock=AMMO_STOCK;
  // A single collision can also knock off a wheel in this tick.
  s.cookShake=0;
  const wearBySide=s.horseWear??=horseWear();
  let fatiguePace=1;
  for(const side of HORSE_SIDES){
    s.horses[side]=Math.max(0,s.horses[side]-2.2*STEP);
    const wear=wearBySide[side];
    if(wear.runaway){
      if(s.tick<wear.runaway.end)continue;
      s.horses[side]=0;
      if(s.tick<wear.runaway.tiredUntil){
        fatiguePace*=HORSE_TIRED_PACE;
        wear.wounds=Math.ceil(HORSE_RUNAWAY_HITS*(1-horseProgress(s,side)));
        continue;
      }
      wear.wounds=0;
      delete wear.runaway;
    }
    const rest=s.tick-wear.lastHit-HORSE_REST_TICKS;
    if(wear.wounds&&rest>=0&&rest%HORSE_HEAL_TICKS===0)wear.wounds--;
  }
  const humans=new Set(humanIds(s));
  claimStations(s,humans);
  runBots(s);
  const walkRate=CRYING_WALK_RATE[cryingChildCount(s)];
  for(const [id,p] of Object.entries(s.players)){
    if(occupiedStation(p,s))continue;
    if(p.until<s.tick){p.mx=0;p.my=0;}
    const floor=playerFloor(p);
    const pace=p.slipUntil?0
      :p.ball?CARRY_SPEED*(1+.2*s.traits.porter)
      :p.royal?ROYAL_CARRY
      :p.spareWheel?WHEEL_CARRY_SPEED:WALK_SPEED;
    const stride=pace*walkRate*STEP;
    const startX=p.x,startY=p.y;
    const x=clamp(startX+p.mx*stride,DECK.left,DECK.right);
    const y=clamp(startY+p.my*stride,DECK.top,DECK.bottom);
    slide(p,x,y,(nextX,nextY)=>(floor===DECK_FLOOR&&bench(nextX,nextY))
      ||s.royals.some(r=>offSeat(r)&&playerFloor(r)===floor&&blocksStep(p,r,nextX,nextY,ROYAL_BODY))
      ||PLAYER_IDS.some(otherId=>{
        if(id===otherId||(humans.has(id)&&!humans.has(otherId)))return false;
        const other=s.players[otherId];
        if(playerFloor(other)!==floor)return false;
        const role=occupiedStation(other,s),spot=role&&workSpot(s,role);
        return spot&&blocksStep(p,spot,nextX,nextY,spot.r);
      }));
    advanceStairs(s,p,humans.has(id));
    if(p.royal)rockRoyal(s,p,Math.hypot(p.x-startX,p.y-startY));
    finishCarry(s,p);
  }
  claimStations(s,humans);
  advanceRoyals(s);
  recordCare(s);
  s.boost=Math.max(0,s.boost-STEP);s.shake=Math.max(0,s.shake-STEP*1.05);
  const runaway=runawaySide(s);
  if(s.obstacleSlow?.until<=s.tick)s.obstacleSlow=null;
  const base=baseSpeed(s);
  const rebellion=horseRebellion(s);
  const horsePace=base+(s.horses.right+s.horses.left)/2+(runaway?base*HORSE_RUNAWAY_PACE:0);
  const speedTarget=horsePace*(s.wheel.broken>=0?.57:1)*(s.obstacleSlow?.factor??1)*fatiguePace*(runaway?1:rebellion.pace);
  s.speed+=(speedTarget-s.speed)*.07;
  if(s.wheel.first&&s.distance>=155)loseWheel(s);
  const travel=s.speed*STEP;
  const roadTravel=travel*ROAD_SCROLL_UNITS_PER_METER;
  const steer=runaway?(runaway==='right'?-1:1)*HORSE_RUNAWAY_STEER:(s.horses.left-s.horses.right+rebellion.steer)*STEERING_RATE;
  s.distance+=travel;s.road+=roadTravel;s.y=clamp(s.y+steer*STEP,CART_TOP,CART_BOTTOM);
  cannonStep(s);
  s.falling=s.falling.filter(b=>s.tick-b.tick<32);
  s.lane=s.y<(CART_TOP+CART_CENTER)/2?0:s.y>(CART_CENTER+CART_BOTTOM)/2?2:1;
  advanceObstacles(s,roadTravel);
  advanceCatapult(s);
  const pursuerStarts=s.looseWheels?.length?new Map(s.enemies.map(({id,x,y})=>[id,{x,y}])):null;
  advancePursuers(s,base);
  advanceLooseWheels(s,roadTravel,pursuerStarts);
  advanceCooking(s);
  advanceShots(s);
  advanceWaves(s);
  s.particles.forEach(p=>p.life--);s.particles=s.particles.filter(p=>p.life>0);
  if(s.testMode)return;
  if(s.hp<=0){s.hp=0;s.mode='lost';}
  else if(s.distance>=s.goal-1e-8){s.distance=s.goal;s.mode='clear';finishCare(s);}
  if(s.mode!=='run')finishCannonExit(s);
}
export function advance(s,ticks){for(let i=0;i<Math.min(7200,ticks)&&s.mode==='run';i++)step(s);}

const royalCarryJob=(s,p)=>royalTarget(s,p)?.upset?'royal-soothe':'royal-seat';
function spotFor(s,job,p){
 if(job==='loader'&&p?.exitedStation==='loader'){
   const {x,y,r}=STATIONS.loader;
   return {x,y:y+r+8};
 }
 if(job==='royal-pickup'){
   const humans=humanIds(s).map(id=>s.players[id]);
   return s.royals.find(r=>offSeat(r)&&s.tick-(r.fallAt??r.wanderedAt)>=ROYAL_BOT_GRACE&&
     !humans.some(player=>royalTarget(s,player)===r));
 }
 if(job==='royal-soothe'){
   const royal=royalTarget(s,p);
   if(royal?.bored){
     const center=(DECK.left+DECK.right)/2;
     return BOT_EXCURSION_WAYPOINTS[royal.boredFrom.x<center?1:0];
   }
   return BOT_SOOTHE_WAYPOINTS[Math.floor(s.tick/(1.5*HZ))%BOT_SOOTHE_WAYPOINTS.length];
 }
 if(job==='royal-seat'){
   return p?.royal?seatApproach(p.royal):null;
 }
 if(job==='royal-feed'){
   const royal=s.royals.reduce((hungriest,child)=>child.state==='seated'&&(!hungriest||child.hunger>hungriest.hunger)?child:hungriest,null);
   return royal?{...seatApproach(royal.id),id:royal.id}:null;
 }
 return job==='repair'?WHEELS[s.wheel.broken]:STATIONS[job==='deliver'?'rack':job];
}
function roadThreat(s){
 return s.obstacles.find(obstacle=>roadObstacleAhead(obstacle)&&!roadLaneClear(s.y,obstacle))||
   (s.roadWarning&&!roadLaneClear(s.y,s.roadWarning)?s.roadWarning:null);
}
function neededRein(s,threat){
 const rock=s.catapult;
 if(rock&&s.tick<rock.landAt){
   if(Math.abs(s.y-rock.y)>=CART_HULL.halfHeight+CATAPULT_RADIUS.y+8)return null;
   return s.y>=rock.y?'left':'right';
 }
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
   const cost=id=>{
     const p=s.players[id];
     const direct=playerFloor(p)===botJobFloor(p,job,spot);
     const distance=direct?Math.hypot(spot.x-p.x,spot.y-p.y):Math.hypot(STAIRS.x-p.x,STAIRS.y-p.y)+Math.hypot(spot.x-STAIRS.x,spot.y-STAIRS.y);
     return distance-(p.job===job?72:0);
   };
   const id=free.reduce((best,id)=>cost(id)<cost(best)?id:best);
   s.players[id].job=job;free.splice(free.indexOf(id),1);held.add(job);
 }
 for(const id of ids){
   if(s.players[id].ball){
     s.players[id].job='deliver';held.add('deliver');free.splice(free.indexOf(id),1);
   }else if(s.players[id].spareWheel){
     s.players[id].job='repair';held.add('repair');free.splice(free.indexOf(id),1);
   }else if(s.players[id].royal){
     s.players[id].job=royalCarryJob(s,s.players[id]);free.splice(free.indexOf(id),1);
   }else if(s.players[id].food){
     s.players[id].job=spotFor(s,'royal-feed')?'royal-feed':'royal-pickup';
     free.splice(free.indexOf(id),1);
   }
 }
 const cannonJob=cannonStation(s);
 if(cannonJob==='gunner'&&s.fuse.owner&&!held.has('gunner')&&free.includes(s.fuse.owner)){
   s.players[s.fuse.owner].job='gunner';free.splice(free.indexOf(s.fuse.owner),1);held.add('gunner');
 }
 if(s.wheel.broken>=0&&s.tick-s.wheel.since>HZ*3){
   if(s.wheel.progress===1)assign('repair');
   else assign('wheel-pickup',!Object.values(s.players).some(player=>player.spareWheel));
 }
 assign('cleaner',cannonJob==='cleaner');
 assign('loader',canLoad(s));
 assign('royal-pickup');
 assign('gunner',cannonJob==='gunner'&&(s.enemies.some(activeEnemy)||!!s.fuse.end));
 const needSupply=s.ammo.stock>0&&!Object.values(s.players).some(p=>p.ball);
 assign('supply',s.ammo.rack===0&&needSupply);
 assign('driver');
 assign('supply',s.ammo.rack<3&&needSupply);
 assign('cook',s.royals.some(royal=>royal.state==='seated'&&royal.crying)&&!Object.values(s.players).some(p=>p.food));
 for(const id of free)s.players[id].job=null;
}
function runBots(s){
 const humans=humanIds(s),ids=PLAYER_IDS.filter(id=>!humans.includes(id));
 if(!ids.length)return;
 if(s.tick%8===1)assignBots(s,ids);
 for(const id of ids){
   const p=s.players[id],job=p.royal?royalCarryJob(s,p):p.job,spot=spotFor(s,job,p);
   if(p.royal)p.job=job;
   if(p.station&&p.station!==job)releaseStation(p);
   const floor=playerFloor(p),crossFloor=floor!==botJobFloor(p,spot?job:null,spot);
   const destination=crossFloor?(p.stairLocked?{x:STAIRS.x,y:STAIRS.y-STAIRS.r-8}:STAIRS):spot;
   if(!destination){p.mx=0;p.my=0;continue;}
   const dx=destination.x-p.x;
   const targetY=floor===DECK_FLOOR&&destination.y< -48&&Math.abs(dx)>46?-5:destination.y;
   const dy=targetY-p.y,len=Math.hypot(dx,dy);
   let ax=1,ay=0;
   const threat=job==='driver'?roadThreat(s):null;
   const horse=job==='driver'?neededRein(s,threat):null;
   if(horse)({ax,ay}=horseRumpAim(s,horse));
   if(job==='gunner'){
     const e=s.enemies.reduce((a,b)=>activeEnemy(b)&&(!a||b.x<a.x)?b:a,null);
     if(e){
       const enemyX=e.x+CAVALRY_BODY.x,enemyY=e.y+CAVALRY_BODY.y;
       const aimX=clamp(enemyX+18,CANNON_AIM_MIN_X,CANNON_AIM_MAX_X);
       const reach=(aimX-BARREL.x)/Math.max(1,enemyX-BARREL.x);
       const y=s.y+BARREL.y;
       ({ax,ay}=cannonAimInput(s,aimX,y+(enemyY-y)*reach));
     }else ({ax,ay}=cannonAimCursor(s,p));
   }
   setInput(s,id,{mx:len>6?dx/len:0,my:len>6?dy/len:0,ax,ay,aimRole:job,aimCursor:job==='gunner',cookHeld:job==='cook'});
   if(job==='cook'||job==='royal-soothe')continue;
   if(p.slipUntil){
     if(hit(s,'catch',catchPhase(p,s.tick))&&s.tick%4===0)operate(s,id);
     continue;
   }
   if(stationAt(p,s)!==job||s.tick%3!==0)continue;
   if(job==='gunner'){
     if(s.tick<HZ*5||s.fuse.end||!s.enemies.some(activeEnemy))continue;
   }else if(job==='cleaner'){
     if(s.tick%6!==0)continue;
   }else if(job==='driver'){
     if(!horse)continue;
     const wear=s.horseWear[horse];
     const evading=threat||s.catapult&&s.tick<s.catapult.landAt;
     const maxWounds=evading?HORSE_RUNAWAY_HITS-1:3;
     if(horseCondition(s,horse)!=='fresh')continue;
     if(s.horses[horse]>(evading?9:6)||wear.wounds>=maxWounds||s.tick-wear.lastHit<(evading?9:HZ))continue;
   }else if(job==='royal-pickup'){
     if(royalTarget(s,p)!==spot)continue;
   }else if(job==='royal-feed'){
     if(feedTarget(s,p)?.id!==spot.id)continue;
   }else if(job!=='royal-seat'&&job!=='wheel-pickup'&&!hit(s,job))continue;
   operate(s,id);
   if(job==='wheel-pickup'&&p.spareWheel)p.job='repair';
 }
}
