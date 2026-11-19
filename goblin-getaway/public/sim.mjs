import {CHAPTERS,chapterSpec,stageSpec,nextStagePosition,ENEMY_PROFILES,WAVE_ENEMY_MULTIPLIER} from './campaign.mjs?v=wave-density163';
import {ROAD_OBSTACLE_TYPES,obstacleY,roadObstacleProfile,roadsideTrees} from './road-obstacles.mjs?v=loose-wheels34';
import {finishCare,maxDurability,newCare,pruneVotes,recordCare,recordFeeding,recordReseat,UPGRADE_CARDS} from './intermission.mjs?v=wheel-axle83';
import {DRAFT_HORSES,DRAFT_HORSE_SCALE,DRAFT_HORSE_WALK,CAVALRY_RUN,draftHorseFrameOffset,draftHorseOrigin} from './horse-targets.mjs?v=cavalry-hooves169';
export {obstacleY} from './road-obstacles.mjs?v=loose-wheels34';

export const HZ = 30;
export const STEP = 1 / HZ;
export const CAMERA_TRANSITION_SECONDS=.5;
export const LANES = [82, 182, 282];
const [CART_TOP, CART_CENTER, CART_BOTTOM] = LANES;
export const ROAD_TOP = CART_TOP - 134;
export const ROAD_BOTTOM = CART_BOTTOM + 134;
export const ROAD_CAMERA_MIN_X=-1000, ROAD_OBSTACLE_DRAW_MARGIN=43;
export const CELLAR={left:363,right:694,top:-81,bottom:81};
export const DECK={left:388,right:667,top:CELLAR.top,bottom:43};
export const DECK_FLOOR='deck', CELLAR_FLOOR='cellar';
export const STAIRS={x:502,y:40,r:18};
export const TELESCOPE_POINT={x:436,y:-69,rx:24,ry:14.25};
export const SUPPLY_PILE={x:665,y:-1,width:72};
export const playerFloor=p=>p?.floor===CELLAR_FLOOR?CELLAR_FLOOR:DECK_FLOOR;
export const CANNON_IDS=[0,1];
const cannonPads=[{x:641,y:0,r:26},{x:641,y:-68,r:26}];
export const cannonPad=(index=0)=>cannonPads[index];
export const STATIONS = {
 driver:{x:385,y:0,r:26},rack:{x:648,y:56,r:19},
 cleaner:cannonPad(),loader:cannonPad(),gunner:cannonPad(),
 supply:{x:617,y:0,r:23},cook:{x:383,y:-21,r:24},'wheel-pickup':{x:528,y:-62,r:24}
};
const rackDeliverySpot={...STATIONS.rack,r:30};
const CANNON_ROLES=['cleaner','loader','gunner'];
const BOT_AMMO_RESERVE=2;
export const isCannonRole=role=>CANNON_ROLES.includes(role);
const formerCannonPads={cleaner:{x:674,y:-41,r:20},loader:{x:622,y:-39,r:21},gunner:{x:658,y:3,r:26}};
const formerSupplyPad={x:SUPPLY_PILE.x,y:STATIONS.supply.y,r:STATIONS.supply.r};
const DECK_WORK_ROLES=['driver',...CANNON_ROLES];
const canTakeBotStation=(role,humanCount)=>role==='driver'||!DECK_WORK_ROLES.includes(role)||
  (humanCount>1&&isCannonRole(role));
const WORK_ROLES=['repair',...DECK_WORK_ROLES,'supply','cook','wheel-pickup'];
export const ROYAL_SEATS={
 prince:{x:510,y:-74,dropY:-34},
 princess:{x:549,y:-74,dropY:-34}
};
export const ROYAL_REACH=30, ROYAL_RETURN_REACH=18;
const ROYAL_STATUS_REACH=52;
export const ROYAL_CRY_THRESHOLD=70;
const ROYAL_REQUEST_THRESHOLD=60;
const ROYAL_INITIAL_HUNGER={prince:14,princess:22};
const ROYAL_HUNGER_RATE=.495, SNACK_HUNGER_RELIEF=75;
const ROYAL_FALL_CHANCE={cannon:.0625,rock:.13,obstacle:.085};
const ROYAL_FALL_CRY_CHANCE=.85;
const ROYAL_SEAT_BOUNCE_SHAKE=.9, ROYAL_SEAT_BOUNCE_TICKS=12;
export const FUSE_TICKS=75;
const FUSE_WARNING_TICKS=.4*HZ;
export const CANNON_VIEW_HOLD_TICKS=2*HZ, CANNON_VIEW_RETURN_TICKS=CAMERA_TRANSITION_SECONDS*HZ;
export const CANNON_CLEAN_TAPS=12;
export const CATCH_TICKS=48;
const COOK_TICKS=3*HZ, COOK_PROGRESS_PER_TICK=1.5, COOK_SHAKE_LOSS=.55*HZ;
export const ENEMY_RETREAT_TICKS=48;
export const ENEMY_RECOVERY_TICKS=126;
const ENEMY_RETURN_TICKS=HZ;
export const CATAPULT_RADIUS={x:35,y:24};
const CATAPULT_FIRST=6*HZ, CATAPULT_APPROACH=33, CATAPULT_FLIGHT=144, CATAPULT_LINGER=21;
const CATAPULT_FIRE_RATE=1.3;
const CATAPULT_SALVO_WAVE=3,CATAPULT_SALVO_GAP=24,CATAPULT_SALVO_SPACING=22,CATAPULT_PATTERN=[2,1,3,1];
const CART_HULL={left:340,right:715,halfHeight:100};
// World X 180 projects to about 30% of the standard 16:9 overview.
const CATAPULT_TARGET_X=718, CATAPULT_FORWARD_X=180;
const ROAD_UNITS_PER_METER=4;
const ROAD_SCROLL_UNITS_PER_METER=ROAD_UNITS_PER_METER*8;
const ROAD_FIRST_WARNING_METERS=42, ROAD_WARNING_TICKS=4*HZ, ROAD_OBSTACLE_ENTRY_X=ROAD_CAMERA_MIN_X-100;
const ROAD_OBSTACLE_HIT_START=287, ROAD_OBSTACLE_HIT_END=344, ROAD_STRIKE_MARGIN=HZ;
const WALK_SPEED=165, CARRY_SPEED=50, WHEEL_CARRY_SPEED=110, WAGON_STEER_SPEED=140;
const WAGON_IDLE_SWAY=7,WAGON_IDLE_SWAY_TICKS=5*HZ;
const PORTER_SPEED_PER_LEVEL=10;
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
export const CAVALRY_ART_X=40, CAVALRY_SIZE_FACTOR=.75;
const cavalryBodyScale=DRAFT_HORSE_SCALE*CAVALRY_SIZE_FACTOR;
const CAVALRY_BODY={
  x:CAVALRY_ART_X-9*cavalryBodyScale,y:3*cavalryBodyScale,
  rx:43*cavalryBodyScale,ry:23*cavalryBodyScale,
};
export const WAVE_ENTRY_X=1280;
const WAVE_SPACING=56;
const TEST_ENEMY_LIMIT=16*WAVE_ENEMY_MULTIPLIER, TEST_ENEMY_ENGAGEMENT=14*HZ;
const WHIP_COOLDOWN=3, HORSE_RAGE_PER_WHIP=1/8, HORSE_RAGE_DECAY=.09;
const HORSE_RAGE_REST_TICKS=HZ, HORSE_BOOST_TICKS=5*HZ, HORSE_SLOW_TICKS=12*HZ;
const HORSE_RAGE_PACE=.35, HORSE_BOOST_PACE=2.20, HORSE_SLOW_PACE=.55;
const ROYAL_BODY=22, ROYAL_WALK=56, ROYAL_CARRY=125;
const ROYAL_ATTENTION_LOW=20, ROYAL_ATTENTION_DECAY=1.26, ROYAL_ATTENTION_GAIN=20;
const ROYAL_REST=7*HZ, ROYAL_BOT_GRACE=9*HZ, ROYAL_HEART_TICKS=18;
const ROYAL_PREFERENCES={
 prince:{stairs:.72,snacks:.7,shells:.1,hungerScale:.75,attentionScale:.5},
 princess:{stairs:.1,snacks:.08,shells:.8,hungerScale:.5,attentionScale:.75},
};
const ROYAL_SNACK_PAUSE=2*HZ, ROYAL_SHELL_PLAY_TICKS=Math.round(1.2*HZ);
const ROYAL_SOOTHE_DISTANCE=ROYAL_CARRY*2.5;
const ROYAL_BOREDOM_CHANCE=.30, ROYAL_BOREDOM_DISTANCE=ROYAL_SOOTHE_DISTANCE/3;
const ROYAL_EXCURSION_DISTANCE=82, ROYAL_DECK_END=80;
const CRYING_WALK_RATE=[1,.90,.75];
const CRYING_COOK_RATE=[1,.85,.65];
const CRYING_TIMING_WIDTH=[1,.80,.60];
const BOT_SOOTHE_WAYPOINTS=[{x:422,y:22},{x:610,y:22},{x:610,y:DECK.bottom},{x:422,y:DECK.bottom}];
const BOT_EXCURSION_WAYPOINTS=[{x:DECK.left+42,y:43},{x:DECK.right-42,y:43}];
const DEFAULT_TRAITS={spring:0,rein:0,sight:0,steady:0,barrel:0,hull:0,porter:0};
const baseSpeed=s=>13+3*s.traits.rein;
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
export function royalRequests(royal){
 const needsAttention=100-royal.attention>=ROYAL_REQUEST_THRESHOLD;
 const needsSnack=royal.hunger>=ROYAL_REQUEST_THRESHOLD;
 return Number(needsAttention)+2*Number(needsSnack);
}
export function cryingChildCount(s){
 return s.royals?.reduce((count,royal)=>count+Number(royal.crying===true),0)??0;
}
const freshHorseRage=()=>({value:0,lastWhipTick:null,boostUntil:0,slowUntil:0});

export const BARREL = {x:704,y:0,length:44};
const CANNON_SHOT_SPEED=1750, CANNON_SHOT_RADIUS=28, LEGACY_CANNON_RADIUS=6;
const CANNON_PATH_DAMAGE_SCALE=1.4, CANNON_BLAST_LENGTH_SCALE=2, CANNON_BLAST_WIDTH_SCALE=1.125;
const CANNON_NEAR_MISS_MARGIN=30, CANNON_NEAR_MISS_PACE=.75;
export const CANNON_NEAR_MISS_TICKS=2*HZ;
// Leave room beyond the farthest aim point for scatter and the full shockwave.
export const CANNON_VIEW_RIGHT_X=1660;
export const CANNON_AIM_MIN_X=866, CANNON_AIM_MAX_X=BARREL.x+.70*(CANNON_VIEW_RIGHT_X-BARREL.x), CANNON_AIM_Y=154;
export const CANNON_IMPACT_TICKS=90;
export const CAVALRY_DEATH_FRAME_TICKS=2, CAVALRY_DEATH_FRAME_COUNT=8, CAVALRY_DEATH_TICKS=HZ;
const CAVALRY_DEATH_BACKSTEP=8;
const CANNON_AIM_CENTER=973;
const CANNON_FULL_ACCURACY_RANGE=310, CANNON_MAX_RANGE_PENALTY=.72;
const cannonAimSpan=offset=>offset<0?CANNON_AIM_CENTER-CANNON_AIM_MIN_X:CANNON_AIM_MAX_X-CANNON_AIM_CENTER;
const cannonAimX=ax=>CANNON_AIM_CENTER+ax*cannonAimSpan(ax);
export const cannonScale=s=>1+.17*(s.traits?.barrel??0);
export const cannonMount=(s,index=0)=>index===0?BARREL:{...BARREL,y:BARREL.y-70*cannonScale(s)};
export const cannonShotRadius=shot=>shot.radius??LEGACY_CANNON_RADIUS*(shot.caliber??1);
export const WHEELS = [
 {x:405,y:-72,name:'앞쪽 위 바퀴'}, {x:650,y:-72,name:'뒤쪽 위 바퀴'},
 {x:405,y:72,name:'앞쪽 아래 바퀴'}, {x:650,y:72,name:'뒤쪽 아래 바퀴'}
];
export const WHEEL_MOUNT_OFFSET=103, LOOSE_WHEEL_RADIUS=20, WHEEL_KNOCK_TICKS=24, LOOSE_WHEEL_HOP_TICKS=10;
const LOOSE_WHEEL_EXIT_X=WAVE_ENTRY_X+100;
const WHEEL_TRACK_SPACING=16, WHEEL_TRACK_RANGE=2400, MAX_WHEEL_TRACK_POINTS=180;
const MAX_LOOSE_WHEEL_TRACK_SEGMENTS=360;
export const HOOFPRINT_RANGE=960;
const MAX_CAVALRY_HOOFPRINTS=320;
// Source pixels at the bottom of each newly planted hoof in the gallop cycle.
const DRAFT_HORSE_FOOTFALLS=[null,[71,82],[93,83],null,[33,82],[24.5,81],null,null];
const CAVALRY_FOOTFALLS=[null,null,[[54,175]],null,[[110,174],[142,177]],[[45,173]],null,null];
const CAVALRY_WHEEL_DAMAGE=25, DAMAGED_WHEEL_PACE=.57, BROKEN_AXLE_PACE=.18;
export const repairLabels=['바퀴 끼우기','차축 핀 고정'];
function wheelState(){return {broken:-1,pending:[],progress:0,since:0,first:true,eligible:110,cavalryDamage:0};}
export function wheelNeedsRepair(s,index){
 return s.wheel?.broken===index||s.wheel?.pending?.includes(index)===true;
}
export function wheelIsMounted(s,index){
 return !wheelNeedsRepair(s,index)||(s.wheel.broken===index&&s.wheel.progress===1);
}
function recordWheelTracks(s){
 const tracks=s.wheelTracks??=[];
 let mounted=0;
 for(let index=0;index<WHEELS.length;index++){
   if(wheelIsMounted(s,index))mounted|=1<<index;
 }
 const last=tracks.at(-1);
 if((mounted||last?.[2])&&(!last||mounted!==last[2]||Math.hypot(s.road-last[0],s.y-last[1])>=WHEEL_TRACK_SPACING)){
   tracks.push([Math.round(s.road),Math.round(s.y),mounted]);
 }
 let remove=Math.max(0,tracks.length-MAX_WHEEL_TRACK_POINTS);
 while(remove<tracks.length&&s.road-tracks[remove][0]>WHEEL_TRACK_RANGE)remove++;
 if(remove)tracks.splice(0,remove);
}
function recordHoofprints(s,previousDistance,previousY,roadTravel){
 const prints=s.hoofprints??=[];
 const {width,height,frames,framesPerMeter,offsetX,offsetY}=DRAFT_HORSE_WALK;
 const first=Math.floor(previousDistance*framesPerMeter)+1;
 const last=Math.floor(s.distance*framesPerMeter);
 for(let gait=first;gait<=last;gait++){
   const progress=(gait/framesPerMeter-previousDistance)/(s.distance-previousDistance);
   const road=Math.round(s.road-roadTravel*(1-progress));
   const wagonY=previousY+(s.y-previousY)*progress;
   for(const side of Object.keys(DRAFT_HORSES)){
     const foot=DRAFT_HORSE_FOOTFALLS[(gait+draftHorseFrameOffset(side))%frames];
     if(!foot)continue;
     const mount=draftHorseOrigin(s,side);
     const x=mount.x+offsetX+(foot[0]-width/2)*DRAFT_HORSE_SCALE;
     const y=wagonY+mount.y+offsetY+(foot[1]-height/2)*DRAFT_HORSE_SCALE;
     prints.push([road,Math.round(x),Math.round(y)]);
   }
 }
 trimHoofprints(prints,s.road);
}
function trimHoofprints(prints,road,limit=Infinity){
 let remove=Math.max(0,prints.length-limit);
 while(remove<prints.length&&road-prints[remove][0]>=HOOFPRINT_RANGE)remove++;
 if(remove)prints.splice(0,remove);
}
export function hasBrokenAxle(s){
 return (wheelNeedsRepair(s,0)&&wheelNeedsRepair(s,2))||
   (wheelNeedsRepair(s,1)&&wheelNeedsRepair(s,3));
}
function wheelPace(s){
 return hasBrokenAxle(s)?BROKEN_AXLE_PACE:s.wheel.broken>=0?DAMAGED_WHEEL_PACE:1;
}
export function feedbackKey(role){return role==='gunner'?'gun':role==='repair'?'fix':role==='driver'?'drive':role;}

const clamp=(v,min,max)=>Math.max(min,Math.min(max,v));
const bench=(x,y)=>x>468&&x<583&&y< -48;
function actor(x,y,face){return {x,y,mx:0,my:0,ax:face,ay:0,facing:face,until:0,ball:false,food:false,spareWheel:false,cookHeld:false,cookTicks:0,royal:null,job:null,station:null,exitedStation:null,floor:DECK_FLOOR,stairLocked:false,score:0,scoreOwnerVersion:0};}
function crew(){return {p1:actor(480,10,-1),p2:actor(614,11,1),p3:actor(556,DECK.bottom,1),p4:actor(500,-28,-1)};}
function campaignLocation(chapter,stage){
 const chapterNumber=CHAPTERS[chapter-1]?chapter:1;
 const count=chapterSpec(chapterNumber).stages.length;
 return {chapter:chapterNumber,stage:Number.isInteger(stage)&&stage>0?(stage-1)%count+1:1};
}
export const PLAYER_IDS=['p1','p2','p3','p4'];
export function humanIds(s){return s.humanSlots??PLAYER_IDS.slice(0,s.humans??1);}
const MAX_SFX_EVENTS=24;
function queueSfx(s,cue,playerId,options){
 if(playerId&&!humanIds(s).includes(playerId))return;
 const events=s.sfxEvents??=[];
 const id=(s.sfxSequence??0)+1;
 s.sfxSequence=id;
 const tick=s.tick;
 events.push(playerId?{id,tick,cue,playerId,...options}:{id,tick,cue,...options});
 if(events.length>MAX_SFX_EVENTS)events.splice(0,events.length-MAX_SFX_EVENTS);
}
const SCORE={horseBoost:1,cannonClean:1,shellReload:2,enemyElimination:4,shellDelivery:8,
 childReseat:20,childSoothe:40,childFeed:40,wheelInstall:8,wheelRepair:12};
function awardScore(s,id,points){
 const player=s.players[id];
 player.score=Math.min(2147483647,Math.trunc(player.score??0)+Math.trunc(points));
}
function crewScoreCheckpoint(s){
 return Object.fromEntries(PLAYER_IDS.map(id=>[id,{
   score:s.players[id].score??0,ownerVersion:s.players[id].scoreOwnerVersion??0
 }]));
}
function resetCooking(p){p.cookHeld=false;p.cookTicks=0;}
export function stopStationInputs(s){
 s.wagonMoveY=0;
 for(const [id,p] of Object.entries(s.players)){
   if(p.cookHeld)queueSfx(s,'cook-stop',id);
   p.cookHeld=false;
   if(p.station==='driver')p.my=0;
 }
}
export function setHumanSlots(s,slots){
 const previous=humanIds(s);
 for(const id of PLAYER_IDS){
   if(previous.includes(id)===slots.includes(id))continue;
   const p=s.players[id];p.mx=0;p.my=0;p.until=0;p.job=null;resetCooking(p);
   delete p.refillAfterShot;
   p.score=0;p.scoreOwnerVersion=(p.scoreOwnerVersion??0)+1;
 }
 s.humanSlots=[...slots];s.humans=slots.length;
 for(const index of CANNON_IDS){
   const cannon=cannonState(s,index);
   if(cannon.cannonExit&&!slots.includes(cannon.cannonExit.id))cannon.cannonExit=null;
 }
 pruneVotes(s,slots);
}
export function ensureDeck(s){
 const legacyCampaign=s.chapter===undefined||s.wave===undefined;
 if(legacyCampaign)Object.assign(s,campaignLocation(s.chapter,s.stage));
 const stage=stageSpec(s.chapter,s.stage);
 if(legacyCampaign){
   // Keep live legacy enemies, but skip waves that would already have launched.
   const {waves}=stage;
   const loopTicks=waveLoopTicks(waves);
   const loops=s.testMode&&waves.length?Math.floor(Math.max(0,s.tick-waves[0].at*HZ)/loopTicks):0;
   s.wave=loops*waves.length+waves.filter(wave=>wave.at*HZ<=s.tick-loops*loopTicks).length;
   if(s.mode==='ready')s.enemies=[];
   delete s.nextEnemy;
 }
 const finished=s.mode==='clear'||s.mode==='lost';
 if(!finished&&s.goal<stage.goal){
   if(s.wave>=stage.catapultFastFromWave)shortenCatapultWait(s);
   s.goal=stage.goal;
 }
 if(legacyCampaign){
   if(!finished||!(s.goal>0))s.goal=stage.goal;
   if(!s.testMode)s.distance=s.mode==='clear'?s.goal:Math.min(s.distance,s.goal);
 }
 if(!s.players)s.players=crew();
 if(!s.players.p3){const fresh=crew();s.players.p3=fresh.p3;s.players.p4=fresh.p4;}
 if(!s.horseRage){
   s.horseRage=freshHorseRage();
   s.horses={right:0,left:0};
   for(const p of Object.values(s.players))if(p.job==='right'||p.job==='left')p.job=null;
 }else if(s.tick<s.horseRage.slowUntil){
   s.horseRage.slowUntil=s.horseRage.boostUntil+HORSE_SLOW_TICKS;
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
   p.facing??=p.ax<0?-1:1;
   p.score=Math.trunc(p.score??0);p.scoreOwnerVersion??=0;
   p.floor=playerFloor(p);
   if(p.floor===DECK_FLOOR){
     p.x=clamp(p.x,DECK.left,DECK.right);
     if(p.y>DECK.bottom){
       p.y=DECK.bottom;
       if(onSpot(p,STAIRS))p.stairLocked=true;
     }
   }
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
   if(royal.floor===DECK_FLOOR)royal.x=clamp(royal.x,DECK.left,DECK.right);
   royal.crying=royalIsCrying(royal);
 }
 s.horses??={right:0,left:0};s.ammo??=supplies();s.humans??=1;
 if(!s.wheel){s.wheel=wheelState();s.fixCd=0;s.fixMsg='';s.fixUntil=0;}
 if(!Array.isArray(s.wheel.pending))s.wheel.pending=[];
 s.wheel.cavalryDamage??=0;
 if(!Array.isArray(s.looseWheels))s.looseWheels=[];
 if(!Array.isArray(s.looseWheelTracks))s.looseWheelTracks=[];
 if(!Array.isArray(s.hoofprints))s.hoofprints=[];
 if(!Array.isArray(s.cavalryHoofprints))s.cavalryHoofprints=[];
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
 for(const p of Object.values(s.players)){
   if(p.station==='supply'&&reachesSpot(p,formerSupplyPad,'supply')){
     p.x+=STATIONS.supply.x-formerSupplyPad.x;
   }else if(isCannonRole(p.station)&&p.cannonIndex==null&&playerFloor(p)===DECK_FLOOR&&
       !onSpot(p,cannonPad())&&onSpot(p,formerCannonPads[p.station])){
     const {x,y}=cannonPad();p.x=x;p.y=y;
   }
 }
 for(const index of CANNON_IDS){
   const previousGunner=s.players[cannonState(s,index).cannonExit?.id];
   if(previousGunner?.station==='gunner'&&cannonIndexFor(s,previousGunner)===index&&
       cannonStation(s,index)==='cleaner'&&reachesSpot(previousGunner,cannonPad(index),'cleaner')){
     advanceCannonWork(s,previousGunner,index);
   }
 }
 if(!Array.isArray(s.catapults))s.catapults=s.catapult?[s.catapult]:[];
 s.catapult=activeCatapultRocks(s)[0]??null;
 s.catapultPattern??=0;
 s.nextCatapult??=Math.max(CATAPULT_FIRST,s.tick+2*HZ);
 if(Number.isFinite(s.catapult?.launchedAt)){
   const interval=catapultInterval(s.stage,s.wave>=stage.catapultFastFromWave);
   s.nextCatapult=Math.min(s.nextCatapult,s.catapult.launchedAt+interval);
 }
 s.wagonMoveY??=0;s.wagonMoveSince??=s.tick;
 s.wagonIdleY??=s.y;s.wagonIdleSince??=s.tick;
 s.cannonCombo??={count:0,tick:0};
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
export function atTelescope(p){
 if(!p||playerFloor(p)!==DECK_FLOOR)return false;
 const {x,y,rx,ry}=TELESCOPE_POINT;
 return Math.hypot((p.x-x)/rx,(p.y-y)/ry)<=1;
}
function newCannon(){
 return {ammo:{loaded:false,cleaning:0},fuse:{end:0,owner:null},gunAngle:0,cannonRecoil:null,cannonExit:null};
}
export function cannonState(s,index=0){
 return index===1?(s.upperCannon??=newCannon()):s;
}
export function cannonIndexFor(s,p){
 if(!p||playerFloor(p)!==DECK_FLOOR)return null;
 if(isCannonRole(p.station))return p.cannonIndex===1?1:0;
 return CANNON_IDS.find(index=>onSpot(p,cannonPad(index)))??null;
}
export function cannonStation(s,index=0){
 const {loaded,cleaning}=cannonState(s,index).ammo;
 return Number.isInteger(cleaning)?'cleaner':loaded?'gunner':'loader';
}
export const canLoad=(s,index=0)=>cannonStation(s,index)==='loader'&&s.ammo.rack>0;
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
function workSpot(s,role,index=0){
 if(role==='repair'){
   const wheel=WHEELS[s.wheel.broken];
   return wheel?{...wheel,r:28}:null;
 }
 if(role==='supply'&&s.ammo.stock===0)return null;
 if(role==='wheel-pickup'&&!canTakeSpareWheel(s))return null;
 if(isCannonRole(role))return role===cannonStation(s,index)?cannonPad(index):null;
 return WORK_ROLES.includes(role)?STATIONS[role]:null;
}
function canWork(p,s,role){
 if(p.ball||p.food||p.royal)return false;
 return role==='repair'?(p.spareWheel||s.wheel?.progress===1):!p.spareWheel;
}
export function cannonAfterShot(s,p){
 if(!p)return null;
 const shot=cannonState(s,cannonIndexFor(s,p)??0).cannonExit;
 return shot&&s.players[shot.id]===p?shot:null;
}
export function occupiedStation(p,s){
 if(!p?.station||!canWork(p,s,p.station))return null;
 const spot=workSpot(s,p.station,p.cannonIndex??0);
 return reachesSpot(p,spot,p.station)?p.station:null;
}
export function cookingProgress(s,p){return occupiedStation(p,s)==='cook'?(p.cookTicks??0)/COOK_TICKS:0;}
function releaseStation(p,blockEntry=false){
 if(blockEntry){
   p.exitedStation=p.station;
   if(isCannonRole(p.station))p.exitedCannonIndex=p.cannonIndex??0;
 }
 p.station=null;p.cannonIndex=null;p.mx=0;p.my=0;
 resetCooking(p);
}
function enterStation(s,p,role,index=0){
 p.station=role;p.mx=0;p.my=0;
 p.cannonIndex=isCannonRole(role)?index:null;
 if(role==='driver')p.facing=-1;
 if(role==='gunner'){
   Object.assign(p,cannonAimCursor(s,null,index));
   p.cannonCursor=true;
 }
}
function advanceCannonWork(s,p,index){
 const role=cannonStation(s,index);
 if(isCannonRole(p.job))p.job=role;
 enterStation(s,p,role,index);
}
export function leaveStation(s,id,expectedRole,expectedCannonIndex){
 const p=s.players[id],role=occupiedStation(p,s);
 if(s.mode!=='run'||!role)return false;
 if(expectedRole&&role!==expectedRole&&!(isCannonRole(expectedRole)&&isCannonRole(role)))return false;
 const index=cannonIndexFor(s,p)??0;
 if(expectedCannonIndex!==undefined&&(!isCannonRole(role)||index!==expectedCannonIndex))return false;
 if(cannonAfterShot(s,p))cannonState(s,index).cannonExit=null;
 if(p.cookHeld)queueSfx(s,'cook-stop',id);
 releaseStation(p,true);
 queueSfx(s,'station-exit',id);
 return true;
}
const stationKey=(role,index)=>isCannonRole(role)?`cannon-${index}`:role;
function claimStations(s,humans){
 const owners=new Map();
 const ids=[...humans,...PLAYER_IDS.filter(id=>!humans.has(id))];
 for(const id of ids){
   const p=s.players[id],role=occupiedStation(p,s);
   const key=stationKey(role,p.cannonIndex??0);
   if(role&&!owners.has(key)&&(humans.has(id)||p.job===role&&p.cannonIndex!==1))owners.set(key,id);
    else if(p.station){
      if(p.cookHeld)queueSfx(s,'cook-stop',id);
      releaseStation(p);
      queueSfx(s,'station-exit',id);
    }
   if(p.exitedStation){
     const spot=isCannonRole(p.exitedStation)?cannonPad(p.exitedCannonIndex??0):workSpot(s,p.exitedStation);
     if(!reachesSpot(p,spot,p.exitedStation)){
       p.exitedStation=null;p.exitedCannonIndex=null;
     }
   }
 }
 for(const id of ids){
   const p=s.players[id],human=humans.has(id);
   if(p.station||p.exitedStation||p.ball||p.food||p.royal)continue;
   const index=human?cannonIndexFor(s,p)??0:0;
   for(const role of human?WORK_ROLES:[p.job]){
     if(!human&&isCannonRole(role)&&s.ammo.rack<BOT_AMMO_RESERVE)continue;
     const key=stationKey(role,index),spot=workSpot(s,role,index),owner=owners.get(key);
     if(!canWork(p,s,role)||!reachesSpot(p,spot,role)||
        (owner&&(!human||humans.has(owner)||!canTakeBotStation(role,humans.size))))continue;
     if(owner){
       const bot=s.players[owner];
       releaseStation(bot);bot.job=null;
     }
     enterStation(s,p,role,index);
      queueSfx(s,'station-enter',id);
     owners.set(key,id);
     break;
   }
 }
}
const offSeat=royal=>royal.state==='wandering'||royal.state==='fallen';
export const royalBoredHint=name=>name+'님이 지루해해요.\n다른 곳으로 이동해봐요.';
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
function royalAttentionTarget(s,p){
 let target=null,nearest=Infinity;
 for(const royal of s.royals){
   if(royal.state!=='seated'||!royalStatusNearby(p,royal))continue;
   const distance=Math.hypot(p.x-royal.x,p.y-royal.y);
   if(distance<nearest){target=royal;nearest=distance;}
 }
 return target;
}
export function royalActionTarget(s,p,role){
 if(role==='royal-attention')return royalAttentionTarget(s,p);
 if(role==='royal-feed')return feedTarget(s,p);
 if(role==='royal-pickup'||role==='royal-seat')return royalTarget(s,p);
 return null;
}
export function canGiveRoyalAttention(s,royal){
 return !!royal&&royal.attention<100&&s.tick>=(royal.attentionHeart?.until??0);
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
 if(royalAttentionTarget(s,p))return 'royal-attention';
 return null;
}
export function aimAngle(p){return clamp(Math.atan2(p.ay,Math.max(.12,p.ax)),-1.06,1.06);}
export function horseRageStatus(s){
 const rage=s.horseRage;
 if(s.tick<rage?.boostUntil)return {phase:'boost',progress:1,seconds:(rage.boostUntil-s.tick)/HZ};
 if(s.tick<rage?.slowUntil)return {phase:'slow',progress:0,seconds:(rage.slowUntil-s.tick)/HZ};
 return {phase:'normal',progress:rage?.value??0,seconds:0};
}
export function setInput(s,id,input){
 const p=s.players?.[id];if(!p||!input||s.mode!=='run')return;
 if(!['mx','my','ax','ay'].every(k=>Number.isFinite(input[k])))return;
 const role=occupiedStation(p,s),move=Math.hypot(input.mx,input.my),aim=Math.hypot(input.ax,input.ay);
 p.until=s.tick+39;
 const cookHeld=role==='cook'&&input.cookHeld===true;
 if(cookHeld!==Boolean(p.cookHeld))queueSfx(s,cookHeld?'cook-start':'cook-stop',id);
 p.cookHeld=cookHeld;
 if(role){
   p.mx=0;p.my=0;
   if(role==='driver'){
     if(input.aimRole===undefined||input.aimRole===role)p.my=clamp(input.my,-1,1);
     return;
   }
   if((input.aimRole!==undefined&&input.aimRole!==role)||(input.aimRole===undefined&&move>0))return;
   if(role==='gunner'){
     if(input.aimCannon!==undefined&&input.aimCannon!==cannonIndexFor(s,p))return;
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
 const index=isCannonRole(role)?cannonIndexFor(s,p):0;
 const cannon=cannonState(s,index);
 if(role==='carry'||role==='royal-carry'||role==='royal-soothe'||role==='food-carry'||role==='cook'||role==='wheel-carry'||role==='wheel-missing')return false;
 if(role==='wheel-pickup'){
   releaseStation(p,true);
   p.spareWheel=true;
   message(p,s,'예비 바퀴 확보 · 고장 난 바퀴로 가져가세요');
   queueSfx(s,'wheel-pickup',id);
   return true;
 }
 if(role==='wheel-return'){
   p.spareWheel=false;
   p.exitedStation='wheel-pickup';
   message(p,s,'예비 바퀴를 보관함에 돌려놓았습니다');
   queueSfx(s,'wheel-return',id);
   return true;
 }
 if(role==='royal-feed')return feedRoyal(s,p,id);
 if(role==='royal-pickup'||role==='royal-seat')return handleRoyal(s,p,id,role);
 if(role==='royal-attention')return giveRoyalAttention(s,p,id);
 if(role==='driver')return whipHorse(s,id);
 if(role==='cleaner'){
   if(!Number.isInteger(taps)||taps<1||taps>CANNON_CLEAN_TAPS)return false;
   cannon.ammo.cleaning+=taps;
   if(cannon.ammo.cleaning>=CANNON_CLEAN_TAPS){
     cannon.ammo.cleaning=null;
     awardScore(s,id,SCORE.cannonClean);
     if(cannonAfterShot(s,p))cannon.cannonExit=null;
     advanceCannonWork(s,p,index);
     message(p,s,'포신 청소 완료 · 장전하세요');
     queueSfx(s,'cannon-ready');
   }else queueSfx(s,'cannon-clean',id);
   return true;
 }
 if(role==='gunner'){
   if(cannon.fuse.end)return false;
   // The station may change hands during the fuse. The shot still belongs to whoever lit it.
   const shooter={id,ownerVersion:p.scoreOwnerVersion??0};
   cannon.fuse={end:s.tick+FUSE_TICKS,owner:id,shooter};saveCannonAim(s,p,index);
   queueSfx(s,'cannon-fuse',undefined,{cannonIndex:index});
   feedback(s,'gunner','점화! 왼쪽 스틱으로 조준',index);return true;
 }
 if(role==='catch'){
   if(atTick<p.slipUntil-CATCH_TICKS)return false;
   if(hit(s,role,catchPhase(p,atTick))){
     p.slipUntil=0;p.safeUntil=s.tick+HZ;message(p,s,'포탄을 붙잡았습니다');
     queueSfx(s,'shell-catch',id);
   }else dropBall(s,p,true,id);
   return true;
 }
 if((role==='loader'&&!canLoad(s,index))||(role==='supply'&&!s.ammo.stock)||cannon[feedbackKey(role)+'Cd']>s.tick)return false;
 action(s,role,phase({...s,tick:atTick},role),id,index);
 if(p.station&&!occupiedStation(p,s))releaseStation(p,true);
 return true;
}
export const stageTitle = (stage,chapter=1)=>stageSpec(chapter,stage).title;
export const TRAITS=UPGRADE_CARDS.filter(card=>card.id!=='repair');
export const catchPhase=(p,tick)=>clamp((tick-p.slipUntil+CATCH_TICKS)/CATCH_TICKS,0,1);
function cannonMotion(s,index=0){
 const {fuse}=cannonState(s,index);
 let roll=.017*Math.sin(s.tick*.28)+.012*Math.sin(s.tick*.53);
 if(fuse.end){
   const litTicks=s.tick-fuse.end+FUSE_TICKS;
   const shotPhase=fuse.end*.618;
   const buildUp=clamp(litTicks/8,0,1);
   roll+=buildUp*(.23*Math.sin(litTicks*.31+shotPhase)+.085*Math.sin(litTicks*.67-shotPhase));
 }
 const motion=roll+s.shake*.18*Math.sin(s.tick*.53+s.shakeSign);
 return motion*.3*Math.pow(.78,s.traits?.steady??0);
}
export function firingAngle(s,base=s.gunAngle,motion=cannonMotion(s)){
 return clamp(base+motion,-1.1,1.1);
}
export function cannonAimInput(s,x,y,index=0){
 const offset=x-CANNON_AIM_CENTER;
 return {
   ax:clamp(offset/cannonAimSpan(offset),-1,1),
   ay:clamp((y-s.y-cannonMount(s,index).y)/CANNON_AIM_Y,-1,1),
 };
}
export function cannonAimCursor(s,p,index=cannonIndexFor(s,p)??0){
 if(p?.cannonCursor)return {ax:p.ax,ay:p.ay};
 const {gunAim,gunAngle}=cannonState(s,index);
 if(!p&&gunAim)return {...gunAim};
 const angle=p?aimAngle(p):gunAngle;
 const mount=cannonMount(s,index);
 return cannonAimInput(s,CANNON_AIM_CENTER,s.y+mount.y+Math.tan(angle??0)*(CANNON_AIM_CENTER-mount.x),index);
}
function saveCannonAim(s,p,index=0){
 const cannon=cannonState(s,index);
 cannon.gunAim=cannonAimCursor(s,p,index);
 cannon.gunAngle=Math.atan2(cannon.gunAim.ay*CANNON_AIM_Y,cannonAimX(cannon.gunAim.ax)-cannonMount(s,index).x);
}
function cannonOriginY(s,mount){
 return s.y+mount.y+s.shake*Math.sin(s.tick*.37*3)*3;
}
export function cannonAim(s,p,index=cannonIndexFor(s,p)??0){
 const mount=cannonMount(s,index);
 const {ax,ay}=cannonAimCursor(s,p,index);
 const x=cannonAimX(ax);
 const originY=cannonOriginY(s,mount);
 const forward=x-mount.x,targetY=ay*CANNON_AIM_Y;
 const base=Math.atan2(s.y+mount.y+targetY-originY,forward);
 const motion=cannonMotion(s,index);
 const wave=.105*Math.sin(s.tick*.105)+.045*Math.sin(s.tick*.21+.9);
 const baseAccuracy=clamp(.86+wave-Math.abs(motion)*.60-s.shake*.20,.32,.98);
 const falloff=clamp((Math.hypot(forward,targetY)-CANNON_FULL_ACCURACY_RANGE)/
   (CANNON_AIM_MAX_X-mount.x-CANNON_FULL_ACCURACY_RANGE),0,1);
 const accuracy=baseAccuracy*(1-CANNON_MAX_RANGE_PENALTY*falloff**1.5);
 const miss=(1-accuracy)/.68;
 const spreadX=12+38*miss,spreadY=9+34*miss;
 const y=clamp(originY+Math.tan(firingAngle(s,base,motion))*forward,
   ROAD_TOP+spreadY+6,ROAD_BOTTOM-spreadY-6);
 return {x,y,spreadX,spreadY,angle:Math.atan2(y-originY,forward),accuracy};
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
 const cooking=Object.entries(s.players).find(([,p])=>p.station==='cook');
 if(!cooking)return;
 const [id,cook]=cooking;
 cook.cookTicks??=0;
 const loss=Math.min(cook.cookTicks,Math.round(s.cookShake*COOK_SHAKE_LOSS));
 if(loss>0){
   cook.cookTicks-=loss;
   message(cook,s,'마차 흔들림 · 제작 진행도 '+Math.round(loss*100/COOK_TICKS)+'%p 감소');
   queueSfx(s,'cook-shake',id);
 }
 if(cook.until<s.tick){
   if(cook.cookHeld)queueSfx(s,'cook-stop',id);
   cook.cookHeld=false;
 }
 if(!cook.cookHeld)return;
 cook.cookTicks+=COOK_PROGRESS_PER_TICK*CRYING_COOK_RATE[cryingChildCount(s)];
 if(cook.cookTicks<COOK_TICKS-1e-7)return;
 releaseStation(cook,true);
 cook.food=true;
 message(cook,s,'간식 완성 · 1층에서 좌석에 앉은 아이에게 먹이세요');
 queueSfx(s,'cook-stop',id);
 queueSfx(s,'cook-ready',id);
}
function whipHorse(s,id){
 const rage=s.horseRage??=freshHorseRage();
 if(s.tick<rage.slowUntil||s.tick-(rage.lastWhipTick??-Infinity)<WHIP_COOLDOWN)return false;
 const p=s.players[id];
 rage.lastWhipTick=s.tick;
 rage.value=Math.min(1,rage.value+HORSE_RAGE_PER_WHIP);
 if(p){
   p.whipSwing={sequence:(p.whipSwing?.sequence??0)+1,tick:s.tick};
   awardScore(s,id,SCORE.horseBoost);
 }
 queueSfx(s,'whip');
 if(rage.value===1){
   rage.boostUntil=s.tick+HORSE_BOOST_TICKS;
   rage.slowUntil=rage.boostUntil+HORSE_SLOW_TICKS;
   s.event='말 폭주 · 5초간 추가 가속';s.eventUntil=s.tick+HZ*2;
   queueSfx(s,'horse-boost');
 }
 return true;
}
function advanceHorseRage(s){
 const rage=s.horseRage??=freshHorseRage();
 if(s.tick===rage.boostUntil){
   rage.value=0;
   s.event='말이 지침 · '+HORSE_SLOW_TICKS/HZ+'초간 감속';s.eventUntil=s.tick+HZ*2;
   queueSfx(s,'horse-tired');
 }
 if(s.tick===rage.slowUntil)queueSfx(s,'horse-recover');
 if(s.tick>=rage.slowUntil&&rage.lastWhipTick!==null&&s.tick-rage.lastWhipTick>HORSE_RAGE_REST_TICKS){
   rage.value=Math.max(0,rage.value-HORSE_RAGE_DECAY*STEP);
 }
}
function fallBall(s,p,id){
 s.ammo.lost++;
 s.falling.push({x:p.x,y:p.y,side:p.y<0?-1:1,tick:s.tick,floor:playerFloor(p)});
 queueSfx(s,'shell-drop',id);
}
function dropBall(s,p,carried,id){
 if(carried){p.ball=false;p.slipUntil=0;}
 fallBall(s,p,id);
 const msg=carried?'포탄을 놓쳤습니다 · 1발 손실':'장전 실패 · 포탄 1발 손실';
 message(p,s,msg);s.event=msg;s.eventUntil=s.tick+45;
}
function cannonStep(s,index){
 const cannon=cannonState(s,index);
 if(!cannon.fuse.end)return;
 const holder=PLAYER_IDS.find(id=>{
   const player=s.players[id];
   return occupiedStation(player,s)==='gunner'&&cannonIndexFor(s,player)===index;
 });
 const p=s.players[holder];
 if(p){
   cannon.fuse.owner=holder;
   saveCannonAim(s,p,index);
 }
 if(s.tick===cannon.fuse.end-FUSE_WARNING_TICKS)queueSfx(s,'cannon-imminent',undefined,{cannonIndex:index});
 if(s.tick<cannon.fuse.end)return;
 fireCannon(s,p,index);
}
function fireCannon(s,p=null,index=0){
 const cannon=cannonState(s,index),mount=cannonMount(s,index);
 const gunner=p??Object.values(s.players).find(player=>occupiedStation(player,s)==='gunner'&&cannonIndexFor(s,player)===index);
 const viewer=gunner&&humanIds(s).find(id=>s.players[id]===gunner);
 const {owner:firingId,shooter=null}=cannon.fuse;
 const owner=s.players[firingId];
 const sameController=owner&&(owner===gunner||shooter?.id===firingId&&shooter.ownerVersion===owner.scoreOwnerVersion);
 const refillBot=sameController&&!humanIds(s).includes(firingId)?owner:null;
 const aim=cannonAim(s,p,index);
 // Sample the displayed ellipse with a separate seed so firing does not perturb other events.
 let seed=(s.seed^Math.imul(s.tick+1,1597334677)^Math.imul(index,0x9e3779b9))>>>0;
 seed=(Math.imul(seed,1664525)+1013904223)>>>0;
 const scatterAngle=seed/4294967296*TAU;
 seed=(Math.imul(seed,1664525)+1013904223)>>>0;
 const scatterRadius=Math.sqrt(seed/4294967296);
 const landing={x:aim.x+Math.cos(scatterAngle)*scatterRadius*aim.spreadX,
   y:aim.y+Math.sin(scatterAngle)*scatterRadius*aim.spreadY};
 const originY=cannonOriginY(s,mount);
 const angle=Math.atan2(landing.y-originY,landing.x-mount.x);
 const dx=Math.cos(angle),dy=Math.sin(angle),caliber=cannonScale(s);
 const muzzle=mount.length*caliber;
 const x=mount.x+muzzle*dx,y=originY+muzzle*dy;
 s.shots.push({x,y,vx:CANNON_SHOT_SPEED*dx,vy:CANNON_SHOT_SPEED*dy,caliber,radius:CANNON_SHOT_RADIUS*caliber,landing,shooter,cannonIndex:index});
 cannon.cannonRecoil={tick:s.tick,x,y,angle,caliber,cannonIndex:index};
 cannon.ammo.loaded=false;cannon.ammo.cleaning=0;cannon.fuse={end:0,owner:null};
 cannon.cannonExit=viewer?{id:viewer,tick:s.tick,cannonIndex:index}:null;
 if(refillBot){
   refillBot.refillAfterShot=true;
   if(isCannonRole(refillBot.station))releaseStation(refillBot,true);
   if(canWork(refillBot,s,'supply'))refillBot.job=s.ammo.stock>0?'supply':null;
 }
 if(gunner&&gunner!==refillBot){
   advanceCannonWork(s,gunner,index);
   message(gunner,s,'포탄 발사 · 포신을 청소하세요');
 }
 feedback(s,'gunner','포탄 자동 발사!',index);impact(s,1.45,'대포 반동',ROYAL_FALL_CHANCE.cannon);
 queueSfx(s,'cannon-fire',undefined,{rate:1/Math.sqrt(caliber),cannonIndex:index});
}
export const enemyKnockback=enemy=>enemy.cannonKnock??enemy.wheelKnock;
const activeEnemy=enemy=>!enemy.spent&&!enemyKnockback(enemy);
const cannonShotKills=shot=>shot.kills??(shot.sfxHit?1:0);
function defeatWithCannon(s,enemy,motion,x,y,shot){
 const {shooter}=shot;
 shot.kills=cannonShotKills(shot)+1;
 enemy.cannonKnock={tick:s.tick,...motion};
 enemy.x+=CAVALRY_DEATH_BACKSTEP;
 s.kill++;
 if(shooter&&s.players[shooter.id]?.scoreOwnerVersion===shooter.ownerVersion){
   awardScore(s,shooter.id,SCORE.enemyElimination);
 }
 burst(s,x,y,'hit');
 if(!shot.sfxHit){
   shot.sfxHit=true;
   queueSfx(s,'cannon-hit');
 }
}
const pursuitSlot=enemy=>enemy.attackSlot??(enemy.id??0)%PURSUIT_SLOTS.length;
const enemyPaceScale=(s,enemy)=>s.tick<(enemy.nearMissUntil??0)?CANNON_NEAR_MISS_PACE:1;
function pursuitPace(s,profile,base,scale){
 const pace=profile?(profile.speed-s.speed)*ROAD_UNITS_PER_METER:18+Math.min(s.stage*3,24)-(s.speed-base)*2.05;
 // Slow the cavalry's road speed before accounting for the wagon's movement.
 return pace-(pace+s.speed*ROAD_UNITS_PER_METER)*(1-scale);
}
export function incomingPursuit(s,view){
 const span=view.span/view.zoom;
 const approachStart=(view.right??view.left+span)-Math.min(100,span*.1);
 const base=baseSpeed(s);
 let incoming=null;
 for(const enemy of s.enemies){
   if(!activeEnemy(enemy)||enemy.lastStrike!=null||enemy.x+CAVALRY_ART_X<approachStart)continue;
   if(pursuitPace(s,ENEMY_PROFILES[enemy.profile],base,enemyPaceScale(s,enemy))<=0)continue;
   if(!incoming||enemy.x<incoming.x)incoming=enemy;
 }
 return incoming;
}
function landCannonShot(s,shot){
 const caliber=shot.caliber??1,upgrade=Math.min(.5,caliber-1);
 const impact={...shot.landing,road:s.road,tick:s.tick,id:s.nextId++,caliber,cannonIndex:shot.cannonIndex??0,
   length:(200+70*upgrade)*CANNON_BLAST_LENGTH_SCALE,
   halfWidth:(87+40*upgrade)*CANNON_BLAST_WIDTH_SCALE};
 (s.cannonImpacts??=[]).push(impact);
 queueSfx(s,'cannon-ground',undefined,{rate:1/Math.sqrt(caliber)});
 for(const enemy of s.enemies){
   if(!activeEnemy(enemy))continue;
   const forward=enemy.x+CAVALRY_BODY.x-impact.x;
   const across=enemy.y+CAVALRY_BODY.y-impact.y;
   const nearestForward=clamp(forward,0,impact.length);
   const outsideWidth=Math.abs(across)-cannonBlastWidth(impact,nearestForward);
   if(forward!==nearestForward||outsideWidth>0){
     if(shot.nearMisses?.includes(enemy.id)||Math.hypot(forward-nearestForward,Math.max(0,outsideWidth))<=CANNON_NEAR_MISS_MARGIN){
       enemy.nearMissUntil=s.tick+CANNON_NEAR_MISS_TICKS;
     }
     continue;
   }
   const side=Math.sign(across)||1;
   defeatWithCannon(s,enemy,{kind:'blast',vx:205-64*forward/impact.length,vy:side*(72+Math.abs(across)*.9),spin:side*8.5},
      enemy.x+CAVALRY_BODY.x,enemy.y+CAVALRY_BODY.y,shot);
 }
}
function resolveCannonCombo(s,shot){
 const kills=cannonShotKills(shot);
 const combo=s.cannonCombo??={count:0,tick:0};
 if(kills===0){
   combo.count=0;
   combo.tick=s.tick;
 }else if(kills>=2||combo.count>0){
   combo.count+=kills;
   combo.tick=s.tick;
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
   const hitX=(CAVALRY_BODY.rx+radius+22*upgrade)*CANNON_PATH_DAMAGE_SCALE;
   const hitY=(CAVALRY_BODY.ry+radius+30*upgrade)*CANNON_PATH_DAMAGE_SCALE;
   for(const enemy of s.enemies){
     if(!activeEnemy(enemy))continue;
     const offsetX=x-enemy.x-CAVALRY_BODY.x,offsetY=y-enemy.y-CAVALRY_BODY.y;
     const at=ellipseEntry(offsetX,offsetY,dx,dy,hitX,hitY);
     if(at===null){
       if(ellipseEntry(offsetX,offsetY,dx,dy,hitX+CANNON_NEAR_MISS_MARGIN,hitY+CANNON_NEAR_MISS_MARGIN)!==null){
         if(shot.landing){
           // Wait for the shockwave so only enemies that survive the shot react.
           const nearMisses=shot.nearMisses??=[];
           if(!nearMisses.includes(enemy.id))nearMisses.push(enemy.id);
         }else enemy.nearMissUntil=s.tick+CANNON_NEAR_MISS_TICKS;
       }
       continue;
     }
     const vy=clamp(shot.vy*.18,-70,70);
      defeatWithCannon(s,enemy,{kind:'direct',vx:116,vy,spin:(Math.sign(vy)||1)*8.5},x+dx*at,y+dy*at,shot);
   }
   if(lands){
     landCannonShot(s,shot);
     shot.spent=true;
   }
 }
 s.shots=s.shots.filter(shot=>{
   const active=!shot.spent&&(shot.landing||shot.x<1280)&&shot.y>ROAD_TOP-32&&shot.y<ROAD_BOTTOM+32;
   if(!active&&!shot.enemy)resolveCannonCombo(s,shot);
   return active;
 });
 s.enemies=s.enemies.filter(enemy=>!enemy.spent);
 if(s.cannonImpacts?.length)s.cannonImpacts=s.cannonImpacts.filter(impact=>s.tick-impact.tick<CANNON_IMPACT_TICKS);
}
function finishCarry(s,p,id){
 if(!p.ball)return;
 if(p.slipUntil){
   if(s.tick>=p.slipUntil)dropBall(s,p,true,id);
 }else if(reachesSpot(p,rackDeliverySpot,'deliver')){
   p.ball=false;s.ammo.rack++;message(p,s,'적재칸에 포탄을 내려놓았습니다');
   delete p.refillAfterShot;
   awardScore(s,id,SCORE.shellDelivery);
   queueSfx(s,'shell-deposit');
 }
}
function advanceStairs(s,p,id,human){
 if(!onSpot(p,STAIRS)){p.stairLocked=false;return;}
 if(p.stairLocked)return;
 const floor=playerFloor(p);
 if(!human){
   const target=p.job==='royal-pickup'?spotFor(s,p.job,p):null;
   if(botJobFloor(p,p.job,target)===floor)return;
 }
 p.stairLocked=true;
 if(p.spareWheel){
   message(p,s,'예비 바퀴를 지하층 수리 지점으로 가져가세요');
   queueSfx(s,'work-fail',id);
   return;
 }
 p.floor=floor===DECK_FLOOR?CELLAR_FLOOR:DECK_FLOOR;
 if(p.floor===DECK_FLOOR)p.y=Math.min(p.y,DECK.bottom);
 p.exitedStation=null;
 message(p,s,p.floor===CELLAR_FLOOR?'지하층으로 내려왔습니다':'1층으로 올라왔습니다');
 queueSfx(s,'floor-change',id,{rate:p.floor===CELLAR_FLOOR?.9:1.08});
}
const TAU=Math.PI*2;
function rand(s){ s.seed=(Math.imul(s.seed,1664525)+1013904223)>>>0;return s.seed/4294967296; }
function fallRoyals(s,chance){
 // Sample from the impact's tick and shake seed without changing the road's random sequence.
 const rolls={seed:s.seed^Math.imul(s.tick,0x9e3779b9)};
 let fell=false;
 for(const royal of s.royals){
   if(royal.state!=='seated'||rand(rolls)>=chance)continue;
   const seat=ROYAL_SEATS[royal.id];
   const upset=rand(rolls)<ROYAL_FALL_CRY_CHANCE;
   Object.assign(royal,{state:'fallen',x:seat.x,y:seat.dropY,fallAt:s.tick,upset,sootheDistance:0});
   royal.crying=royalIsCrying(royal);
   fell=true;
 }
 if(fell)queueSfx(s,'royal-fall');
}
function feedRoyal(s,p,id){
 const royal=feedTarget(s,p);
 if(!royal)return false;
 p.food=false;
 royal.hunger=Math.max(0,royal.hunger-SNACK_HUNGER_RELIEF);
 royal.crying=royalIsCrying(royal);
 recordFeeding(s,royal.id);
 awardScore(s,id,SCORE.childFeed);
 message(p,s,royal.name+' 간식 먹이기 완료');
 queueSfx(s,'royal-feed',id);
 return true;
}
function giveRoyalAttention(s,p,id){
 const royal=royalAttentionTarget(s,p);
 if(!canGiveRoyalAttention(s,royal))return false;
 const recovered=Math.min(ROYAL_ATTENTION_GAIN,100-royal.attention);
 royal.attention+=recovered;
 awardScore(s,id,recovered);
 royal.attentionHeart={tick:s.tick,until:s.tick+ROYAL_HEART_TICKS,fromX:p.x,fromY:p.y,floor:playerFloor(p)};
 queueSfx(s,'royal-heart',id);
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
function rockRoyal(s,p,distance,stride){
 const royal=royalTarget(s,p);
 if(!royal?.upset)return;
 if(royal.bored){
   if(!royalExcursion(p,royal))return;
   royal.bored=false;delete royal.boredFrom;
   message(p,s,royal.name+' 기분 전환 · 다시 달랠 수 있어요');
   queueSfx(s,'royal-recovered',royal.carrier);
   return;
 }
 // Store progress in the same units as walking, with a third of a full stride from holding alone.
 const gain=(stride+2*distance)/3;
 royal.sootheDistance=(royal.sootheDistance??0)+gain;
 if(royal.boredomPending&&royal.sootheDistance>=ROYAL_BOREDOM_DISTANCE){
   royal.boredomPending=false;
   if(playerFloor(p)===DECK_FLOOR){
     royal.bored=true;royal.boredFrom={x:p.x,y:p.y};
     message(p,s,royalBoredHint(royal.name));
     queueSfx(s,'royal-bored',royal.carrier);
     return;
   }
 }
 if(royal.sootheDistance<ROYAL_SOOTHE_DISTANCE-1e-7)return;
 royal.sootheDistance=ROYAL_SOOTHE_DISTANCE;
 royal.upset=false;
 royal.crying=royalIsCrying(royal);
 awardScore(s,royal.carrier,SCORE.childSoothe);
 message(p,s,royal.name+' 달래기 완료 · 아이를 좌석으로 데려가세요');
 queueSfx(s,'royal-soothed',royal.carrier);
}
function handleRoyal(s,p,id,role){
 const royal=royalTarget(s,p);
 const seat=ROYAL_SEATS[royal.id];
 if(role==='royal-pickup'){
   if(royal.shellPlayUntil){
     s.ammo.rack++;
     delete royal.shellPlayUntil;
   }
   delete royal.snackPauseUntil;
   royal.state='carried';royal.carrier=id;royal.x=p.x;royal.y=p.y;royal.floor=playerFloor(p);
   royal.bored=false;royal.boredomPending=royal.upset&&rollsRoyalBoredom(s,royal);
   royal.mischief=null;royal.destination=null;p.royal=royal.id;
   delete royal.fallAt;
   queueSfx(s,'royal-pickup',id);
 }else{
   royal.state='seated';royal.carrier=null;royal.x=seat.x;royal.y=seat.y;royal.floor=DECK_FLOOR;
   royal.bored=false;royal.boredomPending=false;delete royal.boredFrom;
   royal.attention=100;royal.restUntil=s.tick+ROYAL_REST;royal.sootheDistance=0;p.royal=null;
   recordReseat(s,royal.id);
    awardScore(s,id,SCORE.childReseat);
   message(p,s,royal.name+' 좌석 복귀 · 관심 회복');
   queueSfx(s,'royal-seat',id);
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
   s.ammo.rack--;
   if(!s.ammo.rack)queueSfx(s,'shell-empty');
   royal.shellPlayUntil=s.tick+ROYAL_SHELL_PLAY_TICKS;
 }else if(royal.mischief==='gunner'&&cannonStation(s)==='gunner'&&!s.fuse.end&&!s.cannonExit){
   const side=rand(s)<.5?-1:1,offset=rand(s);
   saveCannonAim(s,{ax:-.55+offset*1.5,ay:side*(.68+offset*.32),cannonCursor:true});
   fireCannon(s);
 }else if(royal.mischief==='driver'){
   if(!whipHorse(s,null))return;
 }else return;
 s.royalMischiefUntil=s.tick+6*HZ;
 queueSfx(s,'royal-mischief');
}
function wanderRoyal(s,royal){
 const floor=playerFloor(royal),onDeck=floor===DECK_FLOOR;
 const bounds=onDeck?DECK:CELLAR;
 const preference=ROYAL_PREFERENCES[royal.id];
 if(royal.shellPlayUntil){
   if(s.tick<royal.shellPlayUntil)return;
   delete royal.shellPlayUntil;
   fallBall(s,{x:royal.x+12,y:royal.y+4,floor});
   return;
 }
 if(royal.snackPauseUntil){
   if(s.tick<royal.snackPauseUntil)return;
   delete royal.snackPauseUntil;
 }
 if(onDeck&&onSpot(royal,STAIRS)){
   royal.floor=CELLAR_FLOOR;
   if(royal.id==='prince')setRoyalDestination(s,royal,STATIONS.cook,'cook');
   else setRoyalDestination(s,royal,{x:STAIRS.x+42,y:STAIRS.y-30});
   return;
 }
 if(royal.destination&&s.tick>=royal.moveUntil){
   royal.destination=null;royal.mischief=null;
 }
 if(!royal.destination){
   if(onDeck&&s.tick>=royal.nextMischief&&!(s.royalMischiefUntil>s.tick)){
     const roll=rand(s),shellChance=s.ammo.rack>0?preference.shells:0;
     const canFire=cannonStation(s)==='gunner'&&!s.fuse.end&&!s.cannonExit;
     const kind=roll<shellChance?'rack':canFire&&roll>=(1+shellChance)/2?'gunner':'driver';
     setRoyalDestination(s,royal,STATIONS[kind],kind);
     royal.nextMischief=s.tick+(9+rand(s)*4)*HZ;
   }else{
     const xRoll=rand(s),yRoll=rand(s);
     if(onDeck&&xRoll<preference.stairs)setRoyalDestination(s,royal,STAIRS);
     else if(!onDeck&&xRoll<preference.snacks&&!onSpot(royal,STATIONS.cook))
       setRoyalDestination(s,royal,STATIONS.cook,'cook');
     else{
       const walkRoll=onDeck?(xRoll-preference.stairs)/(1-preference.stairs):xRoll;
       const x=bounds.left+22+walkRoll*(bounds.right-bounds.left-44);
       const top=onDeck?-34:bounds.top+14;
       const y=top+yRoll*(62-top);
       setRoyalDestination(s,royal,{x,y});
     }
   }
 }
 const {x,y}=royal.mischief==='rack'?STATIONS.rack:royal.destination;
 const dx=x-royal.x,dy=y-royal.y,length=Math.hypot(dx,dy);
 const lookingForSnacks=royal.mischief==='cook';
 if(length<=(lookingForSnacks?STATIONS.cook.r:5)){
   if(lookingForSnacks)royal.snackPauseUntil=s.tick+ROYAL_SNACK_PAUSE;
   else if(royal.mischief)royalMischief(s,royal);
   royal.destination=null;royal.mischief=null;
   return;
 }
 const pace=ROYAL_WALK*STEP/length,leavingBench=royal.y<-48;
 const players=Object.values(s.players);
 slide(royal,clamp(royal.x+dx*pace,bounds.left,bounds.right),royal.y+dy*pace,(x,y)=>
   (onDeck&&!leavingBench&&bench(x,y))||
   players.some(p=>playerFloor(p)===floor&&blocksStep(royal,p,x,y,ROYAL_BODY))||
   s.royals.some(other=>other!==royal&&offSeat(other)&&playerFloor(other)===floor&&blocksStep(royal,other,x,y,ROYAL_BODY))
 );
}
function advanceRoyals(s){
 for(const royal of s.royals){
   const {hungerScale,attentionScale}=ROYAL_PREFERENCES[royal.id];
   const requests=royalRequests(royal);
   royal.hunger=Math.min(100,royal.hunger+ROYAL_HUNGER_RATE*hungerScale*STEP);
   royal.crying=royalIsCrying(royal);
   if((royalRequests(royal)&2)&&!(requests&2))queueSfx(s,'royal-request');
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
   royal.attention=Math.max(0,royal.attention-ROYAL_ATTENTION_DECAY*attentionScale*STEP);
   if((royalRequests(royal)&1)&&!(requests&1))queueSfx(s,'royal-request');
   if(royal.attention>ROYAL_ATTENTION_LOW)continue;
   const seat=ROYAL_SEATS[royal.id];
   royal.state='wandering';royal.wanderedAt=s.tick;royal.nextMischief=s.tick+(4+rand(s)*2)*HZ;
   queueSfx(s,'royal-wander');
   // Leave the prince room to pass the crew beside his bench on the way to the stairs.
   setRoyalDestination(s,royal,{x:seat.x+(royal.id==='prince'?26:0),y:seat.dropY});
 }
}
const catapultRocks=s=>s.catapults??(s.catapult?[s.catapult]:[]);
export const activeCatapultRocks=s=>catapultRocks(s).filter(rock=>rock.launchedAt<=s.tick);
export function catapultX(s,rock=activeCatapultRocks(s)[0]){
 if(!rock)return null;
 return CATAPULT_TARGET_X+(s.distance-rock.distance)*ROAD_UNITS_PER_METER;
}
export function catapultHitsWagon(s,x,rock=activeCatapultRocks(s)[0]){
 if(!rock)return false;
 x??=catapultX(s,rock);
 const dx=x-clamp(x,CART_HULL.left,CART_HULL.right);
 const dy=rock.y-clamp(rock.y,s.y-CART_HULL.halfHeight,s.y+CART_HULL.halfHeight);
 return Math.hypot(dx/CATAPULT_RADIUS.x,dy/CATAPULT_RADIUS.y)<=1;
}
export function catapultAirX(s,rock=activeCatapultRocks(s)[0]){
 const x=catapultX(s,rock);
 if(!rock||s.tick>=rock.lockAt)return x;
 const remaining=rock.lockAt-s.tick;
 // As the rock closes in, its forward speed eases toward the wagon's base pace.
 return x+baseSpeed(s)*ROAD_UNITS_PER_METER*remaining*STEP+340*(remaining/CATAPULT_APPROACH)**2;
}
export function initial(stage=1,upgrades=DEFAULT_TRAITS,seed=718,chapter=1){
  const location=campaignLocation(chapter,stage);
  const goal=stageSpec(location.chapter,location.stage).goal;
  const traits={...DEFAULT_TRAITS,...upgrades};
  const hp=maxDurability({traits}),wheel=wheelState();
  return {mode:'ready',...location,wave:0,traits,seed,tick:0,distance:0,goal,speed:13+traits.rein*3,hp,boost:0,lane:1,y:CART_CENTER,wagonMoveY:0,wagonMoveSince:0,wagonIdleY:CART_CENTER,wagonIdleSince:0,shake:0,shakeSign:1,fuse:{end:0,owner:null},cannonRecoil:null,falling:[],kill:0,nextId:10,
    obstacles:[],roadWarning:null,wheelTracks:[],looseWheelTracks:[],hoofprints:[],cavalryHoofprints:[],sfxSequence:0,sfxEvents:[],
    enemies:[],shots:[],particles:[],cannonImpacts:[],cannonCombo:{count:0,tick:0},
    nextRock:ROAD_FIRST_WARNING_METERS,catapult:null,catapults:[],catapultPattern:0,nextCatapult:CATAPULT_FIRST,driveCd:0,gunCd:0,driveMsg:'',gunMsg:'',driveUntil:0,gunUntil:0,event:'',eventUntil:0,serial:0,road:0,players:crew(),humans:1,horses:{right:0,left:0},horseRage:freshHorseRage(),ammo:supplies(),royals:passengers(),gunAngle:0,wheel,looseWheels:[],fixCd:0,fixMsg:'',fixUntil:0,
    upperCannon:newCannon(),affection:0,care:newCare(),intermission:null,stageEntry:{hp,wheel:{...wheel}}};
}
export function begin(s,{testMode=false}={}){
 if(s.mode!=='ready')return;
 if(testMode)s.testMode=true;
 s.mode='run';
}
function beginStage(s,chapter,stage,traits,affection=0,entry=null){
 const next=initial(stage,traits,s.seed,chapter);
 next.sfxSequence=s.sfxSequence??0;
 next.affection=affection;
 if(entry){
   next.hp=entry.hp;
   next.wheel={...wheelState(),...entry.wheel};
   delete next.wheel.mountedAt;
   delete next.wheel.mountedIndex;
   next.speed*=wheelPace(next);
   next.stageEntry={hp:next.hp,wheel:{...next.wheel}};
 }
 setHumanSlots(next,humanIds(s));
 if(s.crewNames)next.crewNames={...s.crewNames};
 for(const id of PLAYER_IDS){
   const player=next.players[id],saved=entry?.crewScores?.[id];
   player.scoreOwnerVersion=s.players[id].scoreOwnerVersion??0;
   if(saved?.ownerVersion===player.scoreOwnerVersion)player.score=Math.trunc(saved.score??0);
 }
 if(entry)next.stageEntry.crewScores=crewScoreCheckpoint(next);
 begin(next);
 return next;
}
export function nextStage(s){
 if(s.mode!=='clear'||s.intermission?.phase!=='shop')return s;
 const next=nextStagePosition(s.chapter??1,s.stage);
 if(!next)return s;
 const wheel={...s.wheel,eligible:Math.max(0,s.wheel.eligible-s.distance),
   since:s.wheel.broken>=0?s.wheel.since-s.tick:0};
 return beginStage(s,next.chapter,next.stage,s.traits,s.affection,{hp:s.hp,wheel,crewScores:crewScoreCheckpoint(s)});
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
 const bounce=s.royalSeatBounce;
 if(s.shake>=ROYAL_SEAT_BOUNCE_SHAKE&&(!bounce||s.tick>=bounce.until||(bounce.tick===s.tick&&s.shake>bounce.strength))){
   s.royalSeatBounce={tick:s.tick,until:s.tick+ROYAL_SEAT_BOUNCE_TICKS,strength:s.shake};
 }
 if(royalFallChance)fallRoyals(s,royalFallChance*cushion);
 for(const [id,p] of Object.entries(s.players)){
   if(p.ball&&!p.slipUntil&&!(p.safeUntil>s.tick)&&s.shake>.40){
     p.slipUntil=s.tick+CATCH_TICKS;
     queueSfx(s,'shell-slip',id);
   }
 }
}
function feedback(s,role,msg,index=0){
 const key=feedbackKey(role),workState=cannonState(s,index);
 workState[key+'Msg']=msg;workState[key+'Until']=s.tick+26;workState[key+'Cd']=s.tick+8;
}
function loseWheel(s,{cavalry=false,contact}={}){
 const wheel=s.wheel;
 if(!cavalry&&!contact&&(wheel.broken>=0||s.distance<wheel.eligible))return;
 const candidates=[];
 let nearestDistance=Infinity;
 for(let index=0;index<WHEELS.length;index++){
   const available=contact?wheelIsMounted(s,index):!wheelNeedsRepair(s,index);
   if(!available)continue;
   const mount=WHEELS[index];
   const y=s.y+Math.sign(mount.y)*WHEEL_MOUNT_OFFSET;
   const distance=contact?(mount.x-contact.x)**2+(y-contact.y)**2:0;
   if(distance>nearestDistance)continue;
   if(distance<nearestDistance)candidates.length=0;
   nearestDistance=distance;
   candidates.push(index);
 }
 if(!candidates.length)return;
 const wasAxleBroken=hasBrokenAxle(s);
 const id=candidates[Math.floor(rand(s)*candidates.length)];
 const mount=WHEELS[id],side=Math.sign(mount.y),rear=id%2===1;
 const shake=s.shake*Math.sin(s.tick*.37*3)*3;
 (s.looseWheels??=[]).push({
   id:s.nextId++,mount:id,x:mount.x,y:s.y+shake+side*WHEEL_MOUNT_OFFSET,
   vx:(rear?126:134)+s.speed*2,vy:side*(rear?-46:84),angle:0,launchTick:s.tick,
 });
 if(wheel.broken===id||wheel.mountedIndex===id){
   delete wheel.mountedAt;
   delete wheel.mountedIndex;
 }
 if(wheel.broken<0||wheel.broken===id)Object.assign(wheel,{broken:id,progress:0,since:s.tick});
 else wheel.pending=[...(wheel.pending??[]),id];
 wheel.first=false;
 const axleJustBroke=!wasAxleBroken&&hasBrokenAxle(s);
 if(axleJustBroke)s.speed*=BROKEN_AXLE_PACE/DAMAGED_WHEEL_PACE;
 impact(s,1.2,mount.name+' 이탈'+(axleJustBroke?' · 차축 손상으로 급감속':''));
 queueSfx(s,axleJustBroke?'axle-break':'wheel-break');
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
function recordLooseWheelSegment(s,wheel,dx,dy,from,to,travel){
  if(s.tick-(wheel.bounceTick??wheel.launchTick)<LOOSE_WHEEL_HOP_TICKS)return;
  // Each endpoint uses the scrolling road position at that fraction of the tick.
  const fromX=Math.round(wheel.x-s.road+travel*(1-from)),fromY=Math.round(wheel.y);
  const toX=Math.round(wheel.x+dx-s.road+travel*(1-to)),toY=Math.round(wheel.y+dy);
  if(fromX!==toX||fromY!==toY)(s.looseWheelTracks??=[]).push([fromX,fromY,toX,toY,Math.round(s.road)]);
}
function trimLooseWheelTracks(s){
  const tracks=s.looseWheelTracks;
  if(!tracks?.length)return;
  let remove=Math.max(0,tracks.length-MAX_LOOSE_WHEEL_TRACK_SEGMENTS);
  while(remove<tracks.length&&s.road-tracks[remove][4]>WHEEL_TRACK_RANGE)remove++;
  if(remove)tracks.splice(0,remove);
}
function rollWheelSegment(s,wheel,dx,dy,from,to,travel,pursuerStarts){
  recordLooseWheelSegment(s,wheel,dx,dy,from,to,travel);
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
     queueSfx(s,'wheel-knock');
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
    rollWheelSegment(s,wheel,dx*fraction,dy*fraction,0,fraction,travel,pursuerStarts);
    if(bounce){
      const {nx,ny,approach}=bounce;
      const rebound=1.72*approach/STEP;
      wheel.vx-=rebound*nx;wheel.vy-=rebound*ny;
      wheel.x+=nx*.65;wheel.y+=ny*.65;
      wheel.bounceTick=s.tick;
      burst(s,wheel.x,wheel.y,'dust');
      rollWheelSegment(s,wheel,wheel.vx*STEP*(1-fraction),wheel.vy*STEP*(1-fraction),fraction,1,travel,pursuerStarts);
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
  const previousHp=s.hp;
  s.hp-=damage;
  if(s.testMode)s.hp=Math.max(1,s.hp);
  const criticalHp=maxDurability(s)*.25;
  if(previousHp>criticalHp&&s.hp>0&&s.hp<=criticalHp)queueSfx(s,'hull-critical');
  return Math.max(0,previousHp-Math.max(0,s.hp));
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
const roadLaneClear=(y,obstacle,margin=6)=>Math.abs(y-obstacleY(obstacle))>=roadObstacleProfile(obstacle.type).halfWidth+margin;
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
    if(catapultRocks(s).some(rock=>s.tick<rock.landAt&&roadStrikeNear(s,warning.x,rock.landAt-s.tick))){
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
  const upper=y<CART_CENTER;
  queueSfx(s,upper?'road-warning-upper':'road-warning-lower',undefined,{pan:upper?-.35:.35,rate:upper?1.14:.89});
}
function advanceObstacles(s,travel){
  for(const obstacle of s.obstacles){
    const previousX=obstacle.x;
    obstacle.x+=travel;
    if(obstacle.passed||obstacle.x<ROAD_OBSTACLE_HIT_START||previousX>=ROAD_OBSTACLE_HIT_END)continue;
    const profile=roadObstacleProfile(obstacle.type);
    const y=obstacleY(obstacle);
    if(Math.abs(s.y-y)>=profile.halfWidth)continue;
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
    queueSfx(s,'road-hit');
    burst(s,292,y,'dust');
    loseWheel(s,{contact:{x:Math.max(previousX,ROAD_OBSTACLE_HIT_START),y}});
  }
  s.obstacles=s.obstacles.filter(obstacle=>obstacle.x<1260);
  scheduleRoadObstacle(s);
}
function catapultInterval(stage,fast=false){
  const interval=Math.max(9*HZ,13*HZ-(stage-1)*15);
  return Math.round(interval/(CATAPULT_FIRE_RATE*(fast?1.5:1)));
}
function shortenCatapultWait(s){
  if(s.nextCatapult<=s.tick||!Number.isFinite(s.nextCatapult))return;
  const saved=catapultInterval(s.stage)-catapultInterval(s.stage,true);
  s.nextCatapult=Math.max(s.tick,s.nextCatapult-saved);
}
function catapultRoadConflict(s,landIn){
  const warning=s.roadWarning;
  return (warning&&roadStrikeNear(s,warning.x,landIn,warning.spawnAt-s.tick))
    ||s.obstacles.some(obstacle=>roadObstacleAhead(obstacle)&&roadStrikeNear(s,obstacle.x,landIn));
}
function singleCatapultRock(s){
  const warning=s.roadWarning;
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
  const flight=CATAPULT_FLIGHT*STEP,pace=baseSpeed(s);
  const forwardRange=(CATAPULT_TARGET_X-CATAPULT_FORWARD_X)/ROAD_UNITS_PER_METER;
  // Account for existing slowdowns at the forward end of the range.
  const forward=Math.max(0,forwardRange-Math.max(0,pace-s.speed)*flight);
  const landingDistance=s.distance+pace*flight+forward*rand({seed:s.seed});
  return {
    launchedAt:s.tick,lockAt:s.tick+CATAPULT_APPROACH,landAt:s.tick+CATAPULT_FLIGHT,
    distance:landingDistance,
    y:s.y+offset*(side==='right'?-1:1),side
  };
}
const canLaunchCatapultSalvo=s=>horseRageStatus(s).phase==='normal'&&!hasBrokenAxle(s)&&!s.obstacleSlow;
function startCatapultAttack(s){
  const pattern=s.catapultPattern??0;
  const scheduled=s.wave>=CATAPULT_SALVO_WAVE?CATAPULT_PATTERN[pattern]:1;
  const count=canLaunchCatapultSalvo(s)?scheduled:1;
  const timings=Array.from({length:count},(_,index)=>CATAPULT_FLIGHT+index*CATAPULT_SALVO_GAP);
  if(timings.some(landIn=>catapultRoadConflict(s,landIn)))return;
  if(count===1){
    const rock=singleCatapultRock(s);
    if(!rock)return;
    s.catapults=[rock];
  }else{
    // Aim at the fastest unboosted wagon. Only a boost moves it past the stones in time.
    const normalPace=baseSpeed(s)*wheelPace(s)*(1+HORSE_RAGE_PACE);
    s.catapults=timings.map((landIn,index)=>{
      const launchedAt=s.tick+index*CATAPULT_SALVO_GAP;
      const targetX=724+index*6;
      const y=CART_CENTER+(index-(count-1)/2)*CATAPULT_SALVO_SPACING;
      const distance=s.distance+normalPace*landIn*STEP+(CATAPULT_TARGET_X-targetX)/ROAD_UNITS_PER_METER;
      return {launchedAt,lockAt:launchedAt+CATAPULT_APPROACH,landAt:s.tick+landIn,
        distance,y,side:y<CART_CENTER?'right':'left',salvo:count,salvoIndex:index};
    });
  }
  if(s.wave>=CATAPULT_SALVO_WAVE&&count===scheduled)s.catapultPattern=(pattern+1)%CATAPULT_PATTERN.length;
  const {catapultFastFromWave}=stageSpec(s.chapter,s.stage);
  s.nextCatapult=s.tick+catapultInterval(s.stage,s.wave>=catapultFastFromWave);
}
function advanceCatapult(s){
  s.catapults=catapultRocks(s).filter(rock=>s.tick<rock.landAt+CATAPULT_LINGER);
  if(!s.catapults.length&&s.tick>=s.nextCatapult)startCatapultAttack(s);
  const active=activeCatapultRocks(s);
  s.catapult=active[0]??null;
  for(const rock of active){
    if(s.tick===rock.launchedAt)queueSfx(s,'catapult-launch');
    landCatapultRock(s,rock);
  }
}
function landCatapultRock(s,rock){
  if(s.tick===rock.lockAt)queueSfx(s,'catapult-lock',undefined,{pan:rock.y<s.y?-.35:.35});
  if(s.tick<rock.landAt||rock.hit!==undefined)return;
  const x=catapultX(s,rock);
  rock.hit=catapultHitsWagon(s,x,rock);
  queueSfx(s,rock.hit?'catapult-hit':'catapult-ground');
  if(rock.hit){
    damageWagon(s,22);
    s.speed*=.78;
    impact(s,1.42,'투석기 바위 명중',ROYAL_FALL_CHANCE.rock);
    loseWheel(s,{contact:{x,y:rock.y}});
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
  const {waves,catapultFastFromWave}=stageSpec(s.chapter,s.stage);
  if(!waves.length)return;
  const loopTicks=s.testMode?waveLoopTicks(waves):0;
  while(s.testMode||s.wave<waves.length){
    const {at,groups}=waves[s.wave%waves.length];
    const startsAt=at*HZ+Math.floor(s.wave/waves.length)*loopTicks;
    if(s.tick<startsAt)break;
    s.wave++;
    if(s.wave===catapultFastFromWave)shortenCatapultWait(s);
    queueSfx(s,'pursuit-wave',undefined,{pan:.4});
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
        s.enemies.push({id:s.nextId++,x,y,kind,profile:enemy,attackSlot,sfxWave:s.wave});
        assigned[attackSlot]++;
        furthest[attackSlot]=x;
        offset+=WAVE_SPACING;
      }
    }
  }
}
function advancePursuers(s,base,roadTravel){
  if(!s.enemies.length)return;
  const ahead=[];
  const {fps,frames,left,top,scale}=CAVALRY_RUN;
  const gait=Math.floor(s.tick*fps/HZ),footfallTick=gait!==Math.floor((s.tick-1)*fps/HZ);
  const artScale=scale*CAVALRY_SIZE_FACTOR;
  // Run the nearest enemy in each lane first so reinforcements can keep their spacing.
  for(const enemy of [...s.enemies].sort((a,b)=>a.x-b.x)){
    if(enemy.spent)continue;
    const knocked=enemyKnockback(enemy);
    if(knocked){
      const age=s.tick-knocked.tick;
      const duration=enemy.cannonKnock?CAVALRY_DEATH_TICKS:WHEEL_KNOCK_TICKS;
      if(age>=duration){enemy.spent=true;continue;}
      // Cannon casualties fall behind at road speed, even after the impact slide ends.
      if(enemy.cannonKnock)enemy.x+=roadTravel;
      if(!enemy.cannonKnock||age<=CAVALRY_DEATH_FRAME_TICKS*CAVALRY_DEATH_FRAME_COUNT){
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
    const startledScale=enemyPaceScale(s,enemy);
    const retreatStep=ENEMY_RETREAT_SPEED*startledScale*STEP;
    const sinceStrike=s.tick-(enemy.lastStrike??-Infinity);
    if(sinceStrike<=ENEMY_RETREAT_TICKS){
      enemy.x+=retreatStep;
      ahead[slotIndex]=enemy.x;
      continue;
    }
    const returnAge=sinceStrike-ENEMY_RETREAT_TICKS-ENEMY_RECOVERY_TICKS;
    const recovering=returnAge<=0;
    const paceScale=ENEMY_RECOVERY_SPEED+(1-ENEMY_RECOVERY_SPEED)*clamp(returnAge/ENEMY_RETURN_TICKS,0,1);
    if(enemy.x<ENEMY_TRACK_X){
      const lateralStep=ENEMY_TRACK_SPEED*paceScale*startledScale*STEP;
      enemy.y+=clamp(targetY-enemy.y,-lateralStep,lateralStep);
    }
    const profile=ENEMY_PROFILES[enemy.profile];
    const pace=pursuitPace(s,profile,base,startledScale);
    const previousX=enemy.x;
    const nextX=enemy.x-pace*paceScale*STEP;
    const stopX=leaderX===undefined?slot.strikeX:Math.max(slot.strikeX,
      Math.min(enemy.x+retreatStep,leaderX+PURSUIT_GAP));
    enemy.x=Math.max(stopX,nextX);
    ahead[slotIndex]=enemy.x;
    const wave=enemy.sfxWave??s.wave;
    if(leaderX===undefined&&enemy.lastStrike==null&&pace>0&&enemy.x<=slot.strikeX+145&&wave>(s.sfxPursuitWave??0)){
      s.sfxPursuitWave=wave;
      queueSfx(s,'pursuit-warning',undefined,{pan:.4});
    }
    if(leaderX!==undefined||recovering||nextX>=slot.strikeX||Math.abs(targetY-enemy.y)>ENEMY_STRIKE_REACH){
      const leaning=s.tick<(enemy.nearMissUntil??0)-CANNON_NEAR_MISS_TICKS+8;
      if(footfallTick&&!leaning&&enemy.x<=WAVE_ENTRY_X+HOOFPRINT_RANGE&&roadTravel+previousX>enemy.x){
        const feet=CAVALRY_FOOTFALLS[(gait+enemy.id)%frames];
        if(feet){
          const prints=s.cavalryHoofprints??=[];
          for(const [x,y] of feet)prints.push([Math.round(s.road),
            Math.round(enemy.x+CAVALRY_ART_X+(left+x)*artScale),Math.round(enemy.y+(top+y)*artScale)]);
        }
      }
      continue;
    }
    enemy.lastStrike=s.tick;
    enemy.strikeTarget={x:slot.hitX,y:targetY};
    if(s.testMode)enemy.testLeavesAt??=s.tick+TEST_ENEMY_ENGAGEMENT;
    const damage=damageWagon(s,profile?.damage??14);
    impact(s,1.32,'추격대의 일격');
    queueSfx(s,'pursuit-hit',undefined,{pan:.35});
    burst(s,slot.hitX,targetY,'hit');
    s.wheel.cavalryDamage=(s.wheel.cavalryDamage??0)+damage;
    if(s.wheel.cavalryDamage>=CAVALRY_WHEEL_DAMAGE){
      s.wheel.cavalryDamage-=CAVALRY_WHEEL_DAMAGE;
      loseWheel(s,{cavalry:true});
    }
  }
}
function action(s,role,p,id,index=0){
  const cannon=cannonState(s,index);
  if(s.mode!=='run'||cannon[feedbackKey(role)+'Cd']>s.tick)return;
  const operator=s.players[id];
  const tell=msg=>{feedback(s,role,msg,index);message(operator,s,msg);};
  if(role==='supply'&&s.ammo.stock<1){tell('포탄 더미가 비었습니다');return;}
  if(role==='loader'){
    if(!canLoad(s,index))return;
    s.ammo.rack--;
    if(hit(s,role,p)){
      cannon.ammo.loaded=true;
      awardScore(s,id,SCORE.shellReload);
      advanceCannonWork(s,operator,index);
      tell('장전 완료 · 조준하고 점화하세요');
      queueSfx(s,'shell-load',id);
    }else {dropBall(s,operator,false,id);feedback(s,role,operator.note,index);}
    if(!s.ammo.rack)queueSfx(s,'shell-empty');
    return;
  }
  if(!hit(s,role,p)){
    tell('타이밍 실패');
    queueSfx(s,'work-fail',id);
    return;
  }
  if(role==='repair'){
    if(s.wheel.broken<0)return;
    if(s.wheel.progress===0){
      s.wheel.progress=1;
      s.wheel.mountedAt=s.tick;
      s.wheel.mountedIndex=s.wheel.broken;
      awardScore(s,id,SCORE.wheelInstall);
      for(const player of Object.values(s.players)){
        if(!player.spareWheel)continue;
        player.spareWheel=false;
        if(player!==operator)message(player,s,'바퀴 장착 완료 · 예비 바퀴를 보관함으로 돌려보냈습니다');
      }
      tell('바퀴 장착 완료');
      queueSfx(s,'wheel-mount');
    }
    else{
      const [next=-1,...pending]=s.wheel.pending??[];
      Object.assign(s.wheel,{broken:next,pending,progress:0,since:next>=0?s.tick:0});
      if(next<0)s.wheel.eligible=s.distance+240;
      s.event='바퀴 수리 완료';s.eventUntil=s.tick+60;
      awardScore(s,id,SCORE.wheelRepair);
      tell(next>=0?'바퀴 수리 완료 · 다음 바퀴를 수리하세요':'수리 완료 · 속도 회복');
      queueSfx(s,'wheel-complete');
    }
    return;
  }
  if(role==='supply'){
    operator.ball=true;operator.slipUntil=0;s.ammo.stock--;tell('포탄 확보 · 계단으로 1층 적재칸에 운반');
    queueSfx(s,'shell-pickup',id);
    if(!s.ammo.stock)queueSfx(s,'shell-empty');
    return;
  }
}
function idleWagonY(s){
 const anchor=s.wagonIdleY??=s.y;
 const elapsed=s.tick-(s.wagonIdleSince??=s.tick);
 const settle=clamp(elapsed/(.75*HZ),0,1);
 const center=clamp(anchor,CART_TOP+WAGON_IDLE_SWAY,CART_BOTTOM-WAGON_IDLE_SWAY);
 const direction=anchor>CART_CENTER?-1:1;
 const sway=Math.sin(elapsed*TAU/WAGON_IDLE_SWAY_TICKS)*WAGON_IDLE_SWAY*direction;
 return anchor+(center+sway-anchor)*settle*settle*(3-2*settle);
}
export function step(s){
  if(s.mode!=='run')return;
  s.tick++;
  for(const index of CANNON_IDS){
    const cannon=cannonState(s,index),afterShot=cannon.cannonExit;
    if(afterShot&&s.tick>=afterShot.tick+CANNON_VIEW_HOLD_TICKS+CANNON_VIEW_RETURN_TICKS)cannon.cannonExit=null;
  }
  if(s.testMode&&s.ammo.stock<=0)s.ammo.stock=AMMO_STOCK;
  // A single collision can also knock off a wheel in this tick.
  s.cookShake=0;
  advanceHorseRage(s);
  const humans=new Set(humanIds(s));
  claimStations(s,humans);
  runBots(s);
  const walkRate=CRYING_WALK_RATE[cryingChildCount(s)];
  for(const [id,p] of Object.entries(s.players)){
    if(occupiedStation(p,s))continue;
    if(p.until<s.tick){p.mx=0;p.my=0;}
    const floor=playerFloor(p);
    const bounds=floor===CELLAR_FLOOR?CELLAR:DECK;
    const pace=p.slipUntil?0
      :p.ball?CARRY_SPEED+PORTER_SPEED_PER_LEVEL*s.traits.porter
      :p.royal?ROYAL_CARRY
      :p.spareWheel?WHEEL_CARRY_SPEED:WALK_SPEED;
    const stride=pace*walkRate*STEP;
    const startX=p.x,startY=p.y;
    const x=clamp(startX+p.mx*stride,bounds.left,bounds.right);
    const y=clamp(startY+p.my*stride,bounds.top,bounds.bottom);
    slide(p,x,y,(nextX,nextY)=>(floor===DECK_FLOOR&&bench(nextX,nextY))
      ||s.royals.some(r=>offSeat(r)&&playerFloor(r)===floor&&blocksStep(p,r,nextX,nextY,ROYAL_BODY))
      ||PLAYER_IDS.some(otherId=>{
        if(id===otherId)return false;
        const other=s.players[otherId];
        if(playerFloor(other)!==floor)return false;
        const role=occupiedStation(other,s);
        if(!role||(humans.has(id)&&!humans.has(otherId)&&canTakeBotStation(role,humans.size)))return false;
        const spot=workSpot(s,role,other.cannonIndex??0);
        return spot&&blocksStep(p,spot,nextX,nextY,spot.r);
      }));
    if(p.x!==startX)p.facing=Math.sign(p.x-startX);
    advanceStairs(s,p,id,humans.has(id));
    if(p.royal)rockRoyal(s,p,Math.hypot(p.x-startX,p.y-startY),stride);
    finishCarry(s,p,id);
  }
  claimStations(s,humans);
  advanceRoyals(s);
  recordCare(s);
  s.boost=Math.max(0,s.boost-STEP);s.shake=Math.max(0,s.shake-STEP*1.05);
  if(s.obstacleSlow?.until<=s.tick)s.obstacleSlow=null;
  const base=baseSpeed(s);
  const {phase:horsePhase,progress:rage}=horseRageStatus(s);
  const horsePace=horsePhase==='boost'?HORSE_BOOST_PACE:horsePhase==='slow'?HORSE_SLOW_PACE:1+rage*HORSE_RAGE_PACE;
  const speedTarget=base*horsePace*wheelPace(s)*(s.obstacleSlow?.factor??1);
  const {boostUntil,slowUntil}=s.horseRage;
  if(s.tick===boostUntil||s.tick===slowUntil)s.speed=speedTarget;
  else s.speed+=(speedTarget-s.speed)*.07;
  if(s.wheel.first&&s.distance>=155)loseWheel(s);
  const travel=s.speed*STEP;
  const roadTravel=travel*ROAD_SCROLL_UNITS_PER_METER;
  const driverId=PLAYER_IDS.find(id=>occupiedStation(s.players[id],s)==='driver');
  const driver=s.players[driverId];
  const steer=driver?.until>=s.tick?(driver.my??0)*WAGON_STEER_SPEED:0;
  const previousDistance=s.distance,previousY=s.y;
  s.distance+=travel;s.road+=roadTravel;
  if(steer){
    s.y=clamp(s.y+steer*STEP,CART_TOP,CART_BOTTOM);
    s.wagonIdleY=s.y;s.wagonIdleSince=s.tick;
  }else s.y=idleWagonY(s);
  const moveY=steer?Math.sign(s.y-previousY):0;
  if(moveY!==s.wagonMoveY){s.wagonMoveY=moveY;s.wagonMoveSince=s.tick;}
  if(roadTravel||s.y!==previousY)recordWheelTracks(s);
  if(roadTravel>0)recordHoofprints(s,previousDistance,previousY,roadTravel);
  for(const index of CANNON_IDS)cannonStep(s,index);
  s.falling=s.falling.filter(b=>s.tick-b.tick<32);
  s.lane=s.y<(CART_TOP+CART_CENTER)/2?0:s.y>(CART_CENTER+CART_BOTTOM)/2?2:1;
  advanceObstacles(s,roadTravel);
  advanceCatapult(s);
  const pursuerStarts=s.looseWheels?.length?new Map(s.enemies.map(({id,x,y})=>[id,{x,y}])):null;
  advancePursuers(s,base,roadTravel);
  trimHoofprints(s.cavalryHoofprints??=[],s.road,MAX_CAVALRY_HOOFPRINTS);
  advanceLooseWheels(s,roadTravel,pursuerStarts);
  trimLooseWheelTracks(s);
  advanceCooking(s);
  advanceShots(s);
  advanceWaves(s);
  s.particles.forEach(p=>p.life--);s.particles=s.particles.filter(p=>p.life>0);
  if(s.testMode)return;
  if(s.hp<=0){s.hp=0;s.mode='lost';}
  else if(s.distance>=s.goal-1e-8){s.distance=s.goal;s.mode='clear';finishCare(s);}
  if(s.mode!=='run')for(const index of CANNON_IDS)cannonState(s,index).cannonExit=null;
}
export function advance(s,ticks){for(let i=0;i<Math.min(7200,ticks)&&s.mode==='run';i++)step(s);}

const royalCarryJob=(s,p)=>royalTarget(s,p)?.upset?'royal-soothe':'royal-seat';
function spotFor(s,job,p){
 if(isCannonRole(job)&&isCannonRole(p?.exitedStation)){
   const {x,y,r}=cannonPad(p.exitedCannonIndex??0);
   return {x:x-r-8,y};
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
 const idleCenter=clamp(s.wagonIdleY??s.y,CART_TOP+WAGON_IDLE_SWAY,CART_BOTTOM-WAGON_IDLE_SWAY);
 const blocksLane=obstacle=>!roadLaneClear(idleCenter,obstacle,6+WAGON_IDLE_SWAY);
 return s.obstacles.find(obstacle=>roadObstacleAhead(obstacle)&&blocksLane(obstacle))||
   (s.roadWarning&&blocksLane(s.roadWarning)?s.roadWarning:null);
}
function catapultThreat(s){
 return activeCatapultRocks(s).find(rock=>{
   if(s.tick>=rock.landAt)return false;
   const travel=Math.max(baseSpeed(s),s.speed)*(rock.landAt-s.tick)*STEP;
   const landingX=catapultX(s,rock)+travel*ROAD_UNITS_PER_METER;
   return landingX>=CART_HULL.left-CATAPULT_RADIUS.x&&landingX<=CART_HULL.right+CATAPULT_RADIUS.x;
 });
}
const incomingSalvo=s=>catapultRocks(s).some(rock=>rock.salvo&&s.tick<rock.landAt);
function botDriverNeeded(s){
 if(roadThreat(s))return true;
 if(incomingSalvo(s))return horseRageStatus(s).phase==='normal';
 const rock=catapultThreat(s);
 if(rock&&Math.abs(s.y-rock.y)<CART_HULL.halfHeight+CATAPULT_RADIUS.y+8)return true;
 const salvoSize=CATAPULT_PATTERN[s.catapultPattern??0];
 if(s.wave<CATAPULT_SALVO_WAVE||salvoSize<2||!canLaunchCatapultSalvo(s))return false;
 const next=Math.max(s.nextCatapult,...catapultRocks(s).map(rock=>rock.landAt+CATAPULT_LINGER));
 const wait=Math.max(0,next-s.tick);
 if(wait>HZ*2.5)return false;
 for(let index=0;index<salvoSize;index++){
   if(catapultRoadConflict(s,wait+CATAPULT_FLIGHT+index*CATAPULT_SALVO_GAP))return false;
 }
 return true;
}
function botSteering(s,threat,rock){
 const avoidsRock=y=>!rock||Math.abs(y-rock.y)>=CART_HULL.halfHeight+CATAPULT_RADIUS.y+8;
 if(!threat&&avoidsRock(s.y))return 0;
 const lanes=LANES.filter(avoidsRock);
 const safe=threat?lanes.filter(y=>roadLaneClear(y,threat)):lanes;
 const target=(safe.length?safe:lanes).reduce((best,y)=>
   best===null||Math.abs(y-s.y)<Math.abs(best-s.y)?y:best,null);
 return target===null?0:clamp((target-s.y)/(WAGON_STEER_SPEED*STEP),-1,1);
}
function assignBots(s,ids){
 const held=new Set(humanIds(s).map(id=>s.players[id]).flatMap(p=>{
   const role=stationAt(p,s);
   return role&&(!isCannonRole(role)||cannonIndexFor(s,p)===0)?[role]:[];
 }));
 held.delete('royal-pickup');
 const free=ids.slice();
 function reserve(id,job){
   s.players[id].job=job;free.splice(free.indexOf(id),1);held.add(job);
 }
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
   reserve(id,job);
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
 const hasTarget=s.enemies.some(activeEnemy);
 const seatedGunner=free.find(id=>occupiedStation(s.players[id],s)==='gunner');
 if(cannonJob==='gunner'&&(s.fuse.end||hasTarget)&&!held.has('gunner')&&seatedGunner)reserve(seatedGunner,'gunner');
 if(s.ammo.stock>0){
   for(const id of [...free])if(s.players[id].refillAfterShot)reserve(id,'supply');
   const inTransit=Object.values(s.players).filter(player=>player.ball).length;
   assign('supply',s.ammo.rack+inTransit<BOT_AMMO_RESERVE);
 }
 assign('driver',botDriverNeeded(s));
 if(s.wheel.broken>=0&&s.tick-s.wheel.since>HZ*3){
   if(s.wheel.progress===1)assign('repair');
   else assign('wheel-pickup',!Object.values(s.players).some(player=>player.spareWheel));
 }
 const cannonStocked=s.ammo.rack>=BOT_AMMO_RESERVE;
 assign('cleaner',cannonStocked&&cannonJob==='cleaner');
 assign('loader',cannonStocked&&canLoad(s));
 assign('royal-pickup');
 assign('gunner',cannonStocked&&cannonJob==='gunner'&&(hasTarget||!!s.fuse.end));
 assign('cook',s.royals.some(royal=>royal.state==='seated'&&royal.crying)&&!Object.values(s.players).some(p=>p.food));
 for(const id of free)s.players[id].job=null;
}
function runBots(s){
 const humans=humanIds(s),ids=PLAYER_IDS.filter(id=>!humans.includes(id));
 if(!ids.length)return;
 if(s.tick%8===1)assignBots(s,ids);
 for(const id of ids){
   const p=s.players[id],job=p.ball?'deliver':p.royal?royalCarryJob(s,p):p.job,spot=spotFor(s,job,p);
   p.job=job;
   if(p.station&&p.station!==job)releaseStation(p);
   const floor=playerFloor(p),crossFloor=floor!==botJobFloor(p,spot?job:null,spot);
   const destination=crossFloor?(p.stairLocked?{x:STAIRS.x,y:STAIRS.y-STAIRS.r-8}:STAIRS):spot;
   if(!destination){p.mx=0;p.my=0;continue;}
   const dx=destination.x-p.x;
   const targetY=floor===DECK_FLOOR&&destination.y< -48&&Math.abs(dx)>46?-5:destination.y;
   const dy=targetY-p.y,len=Math.hypot(dx,dy);
   let ax=1,ay=0;
   const driving=job==='driver'&&occupiedStation(p,s)==='driver';
   const threat=driving?roadThreat(s):null;
   const rock=driving?catapultThreat(s):null;
   const salvo=driving&&incomingSalvo(s);
   const steer=driving?botSteering(s,threat,salvo?null:rock):0;
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
   const mx=!driving&&len>6?dx/len:0;
   const my=driving?steer:len>6?dy/len:0;
   setInput(s,id,{mx,my,ax,ay,aimRole:job,aimCursor:job==='gunner',cookHeld:job==='cook'});
   if(job==='cook'||job==='royal-soothe')continue;
   if(p.slipUntil){
     if(hit(s,'catch',catchPhase(p,s.tick))&&s.tick%4===0)operate(s,id);
     continue;
   }
   if(stationAt(p,s)!==job||s.tick%3!==0)continue;
   if(job==='loader'&&s.ammo.rack<BOT_AMMO_RESERVE)continue;
   if(job==='gunner'){
     if(s.tick<HZ*5||s.fuse.end||!s.enemies.some(activeEnemy))continue;
   }else if(job==='cleaner'){
     if(s.tick%6!==0)continue;
   }else if(job==='driver'){
     const {progress,phase}=horseRageStatus(s);
     const rush=salvo||rock&&!steer&&Math.abs(s.y-rock.y)<CART_HULL.halfHeight+CATAPULT_RADIUS.y+8;
     if(phase!=='normal'||(!rush&&(progress>=.6||s.tick-(s.horseRage.lastWhipTick??-Infinity)<HZ)))continue;
   }else if(job==='royal-pickup'){
     if(royalTarget(s,p)!==spot)continue;
   }else if(job==='royal-feed'){
     if(feedTarget(s,p)?.id!==spot.id)continue;
   }else if(job!=='royal-seat'&&job!=='wheel-pickup'&&!hit(s,job))continue;
   operate(s,id);
   if(job==='wheel-pickup'&&p.spareWheel)p.job='repair';
 }
}
