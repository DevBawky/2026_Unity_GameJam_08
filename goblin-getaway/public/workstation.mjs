import {stationAt,horseCondition,horseProgress,runawaySide,HORSE_NAMES,HORSE_RUNAWAY_HITS,royalTarget,royalBoredHint,royalSootheProgress,feedTarget,canLoad,canTakeSpareWheel,STATIONS,WHEELS,DECK,HZ,CAMERA_TRANSITION_SECONDS,LANES,ROAD_TOP,ROAD_BOTTOM,playerFloor,CELLAR_FLOOR,CANNON_VIEW_RIGHT_X,CANNON_CLEAN_TAPS,cannonAim,cannonAfterShot,isCannonRole,CANNON_VIEW_HOLD_TICKS,CANNON_VIEW_RETURN_TICKS,ROAD_CAMERA_MIN_X} from './sim.mjs?v=double-shell87';
import {horseWhipTarget} from './horse-targets.mjs?v=whip-reach73';

const isRoyalRole=role=>['royal-soothe','royal-pickup','royal-carry','royal-seat'].includes(role);
const isSpareWheelRole=role=>['wheel-missing','wheel-pickup','wheel-carry','wheel-return'].includes(role);
const driverViewRange=1.5;
const convoyCenterX=450,convoyScreenX=.55;
const convoyMinimumWidth=760;
const gunViewLeft=STATIONS.gunner.x-65;
const gunViewTop=ROAD_TOP-12,gunViewHeight=ROAD_BOTTOM-ROAD_TOP+24;
const cameraTransitionMs=CAMERA_TRANSITION_SECONDS*1000;

function overviewCamera(base){
 const {span,height,role}=base;
 // Keep the horses and deck together, with more room ahead to the left.
 const overviewZoom=Math.min(1,height/(ROAD_BOTTOM-ROAD_TOP+12),span/convoyMinimumWidth);
 // Keep the forward edge bounded even on unusually wide displays.
 const ahead=span*convoyScreenX;
 const zoom=Math.max(role==='driver'?overviewZoom/driverViewRange:overviewZoom,ahead/(convoyCenterX-ROAD_CAMERA_MIN_X));
 const left=convoyCenterX-ahead/zoom;
 return {...base,zoom,left,top:LANES[1]-height*.48/zoom};
}
function gunnerCamera(base){
 const {span,height}=base;
 const zoom=Math.min(2,span/(CANNON_VIEW_RIGHT_X-gunViewLeft),height/gunViewHeight);
 const left=CANNON_VIEW_RIGHT_X-span/zoom;
 return {...base,zoom,left,top:gunViewTop-(height/zoom-gunViewHeight)/2};
}
function cameraCenter({left,top,span,height,zoom}){
 return {centerX:left+span/(2*zoom),centerY:top+height/(2*zoom)};
}
function blendCamera(target,from,time,blendViewport=false){
 const eased=time*time*(3-2*time);
 const blend=(start,end)=>start+(end-start)*eased;
 // Interpolating world extents keeps the cannon pad in view between two camera poses.
 const zoom=blendViewport?1/blend(1/from.zoom,1/target.zoom):blend(from.zoom,target.zoom);
 const {centerX,centerY}=cameraCenter(target);
 const left=blend(from.centerX??centerX,centerX)-target.span/(2*zoom);
 return {...target,zoom,left:target.floor===CELLAR_FLOOR?left:Math.max(ROAD_CAMERA_MIN_X,left),
   top:blend(from.centerY??centerY,centerY)-target.height/(2*zoom)};
}

export function cameraViews(s,duo,player,pixelWidth=1200,pixelHeight=350,reducedMotion=false){
 const aspect=Math.max(1,pixelWidth)/Math.max(1,pixelHeight);
 const screenWidth=Math.min(1200,Math.max(650,550+300*aspect));
 const height=screenWidth/aspect;
 const playing=s.mode==='run'||s.mode==='paused';
 const ids=duo?['p1','p2']:[player];
 const views=ids.map(id=>{
   const p=s.players[id],floor=playerFloor(p);
   const role=playing?stationAt(p,s):null;
   const shot=role==='cleaner'?cannonAfterShot(s,p):null;
   const recoilReturn=shot?Math.max(0,Math.min(1,(s.tick-shot.tick-CANNON_VIEW_HOLD_TICKS)/CANNON_VIEW_RETURN_TICKS)):null;
   const overview=isRoyalRole(role)||isSpareWheelRole(role)||role==='royal-feed'||role==='food-carry'||(floor===CELLAR_FLOOR&&role==='carry');
   return {id,p,floor,role:overview?null:role,recoilReturn};
 });
 const count=duo&&(views.some(view=>view.role)||views[0].floor!==views[1].floor)?2:1;
 return views.slice(0,count).map(({id,p,floor,role,recoilReturn},i)=>{
   const span=screenWidth/count;
   const base={id,localPlayers:count===1?ids:[id],role,floor,span,height,screenWidth,start:i*span};
   if(floor===CELLAR_FLOOR){
     const zoom=role?Math.min(3.2,span/120,height/112):Math.min(3,span/(DECK.right-DECK.left+80),height/(DECK.bottom-DECK.top+72));
     const cx=role?p.x:(DECK.left+DECK.right)/2,cy=s.y+(role?p.y:0);
     return {...base,zoom,left:cx-span/(2*zoom),top:cy-height*.48/zoom};
   }
   if(!role||role==='driver')return overviewCamera(base);
   if(role==='gunner')return gunnerCamera(base);
   const carrying=role==='carry'||role==='catch';
   const destination=role==='carry'?STATIONS.rack:null;
   const carryZoom=destination?Math.min(3,span/(Math.abs(p.x-destination.x)+72),height/(Math.abs(p.y-destination.y)+84)):3;
   const zoom=carrying?carryZoom:4;
   const cx=destination?(p.x+destination.x)/2:p.x;
   const cy=s.y+(destination?(p.y+destination.y)/2:p.y);
   const target={...base,zoom,left:cx-span/(2*zoom),top:cy-height*.48/zoom};
   if(reducedMotion||recoilReturn===null)return target;
   // Keep the work controls on the cleaning step while the camera follows the shot.
   const from=gunnerCamera(base);
   return {...blendCamera(target,{zoom:from.zoom,...cameraCenter(from)},recoilReturn,true),cannonReturning:true};
 });
}
export function createCameraTransition(){
 let presented=[];
 return (targets,now,reducedMotion=false)=>{
   const floorLayoutChanged=targets.length!==presented.length&&targets.some(target=>
     presented.some(({view})=>view.floor!==target.floor&&
       target.localPlayers.some(id=>view.localPlayers.includes(id))));
   presented=targets.map(target=>{
     const prior=presented.find(({view})=>view.localPlayers.includes(target.id));
     const previous=prior?.view;
     if(reducedMotion||!previous||previous.floor!==target.floor||floorLayoutChanged||
        previous.screenWidth!==target.screenWidth||previous.height!==target.height)
       return {view:target};
     if(target.cannonReturning)return {view:target};

     let transition=prior.transition;
     if(previous.role!==target.role||previous.span!==target.span){
       transition={start:now,zoom:previous.zoom,...cameraCenter(previous),blendViewport:isCannonRole(previous.role)||isCannonRole(target.role)};
     }
     if(!transition||now-transition.start>=cameraTransitionMs)return {view:target};

     return {view:blendCamera(target,transition,(now-transition.start)/cameraTransitionMs,transition.blendViewport),transition};
   });
   return presented.map(({view})=>view);
 };
}
export function worldPoint(view,screenX,screenY){
 if(screenX<view.start||screenX>view.start+view.span)return null;
 return {x:view.left+(screenX-view.start)/view.zoom,y:view.top+screenY/view.zoom};
}
export function workInfo(s,p,role){
 if(role==='driver'){
   const target=horseWhipTarget(s,p);
   const side=target?.side;
   const condition=side&&horseCondition(s,side);
   const runaway=runawaySide(s);
   const progress=side?horseProgress(s,side):0;
   const hits=Math.round(progress*HORSE_RUNAWAY_HITS);
   const wouldTrigger=target?.zone==='rump'&&hits===HORSE_RUNAWAY_HITS-1;
   const action=condition==='fresh'&&!(runaway&&wouldTrigger)?'채찍질':undefined;
   const info={mode:'whip',action,blocked:!action,color:'#bb91d9'};
   if(runaway){
     const direction=runaway==='right'?'위로 급회전':'아래로 급회전';
     return {...info,color:'#ffa887',hint:HORSE_NAMES[runaway]+' 폭주 · '+direction,
       progress:horseProgress(s,runaway),progressKind:'horse-burst'};
   }
   if(condition==='tired'){
     return {...info,color:'#e5bd8d',hint:HORSE_NAMES[side]+' 지침 · 체력 회복 중',
       progress,progressKind:'horse-recovery'};
   }
   if(condition==='angry')return {...info,color:'#ffa887',hint:HORSE_NAMES[side]+' 놀람 · 잠시 쉬게 하세요'};
   if(target?.zone!=='rump')return {...info,hint:'왼쪽 스틱으로 말 엉덩이를 겨냥하세요'};
   return {...info,hint:HORSE_NAMES[side]+' 채찍질 · '+hits+'/'+HORSE_RUNAWAY_HITS+'회',
     progress,progressKind:'horse-runaway'};
 }
 if(role==='cleaner')return {mode:'mash',color:'#e9cd82',action:'포신 청소',
   hint:'행동 버튼을 연타하세요',progress:s.ammo.cleaning/CANNON_CLEAN_TAPS,progressKind:'cannon-cleaning'};
 if(role==='gunner'){
   const info={mode:'fuse',color:'#f4b46d'};
   const accuracy={progress:cannonAim(s,p).accuracy,progressKind:'cannon-accuracy'};
   if(s.fuse.end>0)return {...info,...accuracy,action:'점화 중',hint:'착탄점을 조정하세요',seconds:(s.fuse.end-s.tick)/HZ,blocked:true};
   return {...info,...accuracy,action:'점화',hint:'왼쪽 스틱으로 착탄점을 맞추세요'};
 }
 if(role==='carry'){
   const downstairs=playerFloor(p)===CELLAR_FLOOR;
   return {mode:'carry',color:'#9bcde1',hint:downstairs?'계단으로 1층에 올라가세요':'5시 방향 파란 적재칸으로 옮기세요',blocked:true};
 }
 if(role==='cook')return {mode:'cook',color:'#ffd192',action:'길게 눌러 만들기',hint:'행동을 길게 눌러 간식을 만드세요'};
 if(role==='royal-feed'){
   const child=feedTarget(s,p);
   const info={mode:'royal',color:'#f0c282'};
   if(!child)return {...info,blocked:true};
   return {...info,hint:child.name+'에게 간식을 주세요',action:child.name+' 먹이기'};
 }
 if(role==='food-carry'){
   const downstairs=playerFloor(p)===CELLAR_FLOOR;
   return {mode:'royal',color:'#f0c282',blocked:true,
     hint:downstairs?'간식을 들고 1층으로 올라가세요':'앉아 있는 아이에게 간식을 가져가세요'};
 }
 if(role==='catch')return {color:'#ffaf71',action:'포탄 붙잡기'};
 if(role==='loader'){
   if(canLoad(s))return {color:'#a7d6aa',action:'장전'};
   return {color:'#a7d6aa',action:'장전 대기',mode:'waiting',blocked:true,
     hint:'적재칸에 포탄을 가져오세요'};
 }
 if(isRoyalRole(role)){
   const royal=royalTarget(s,p);
   const info={mode:'royal',color:'#d988a0'};
   if(!royal)return {...info,blocked:true};
   const child=royal.name+'를';
   if(role==='royal-soothe'){
     const progress=royalSootheProgress(royal);
     if(royal.bored)return {...info,hint:royalBoredHint(royal.name),
       progress,progressKind:'soothe-bored',blocked:true};
     return {...info,hint:'안고 있으면 달래져요.\n걸으면 더 빨라요.',progress,blocked:true};
   }
   if(role==='royal-pickup')return {...info,hint:child+' 안아 올리세요',action:'안아 올리기'};
   if(role==='royal-seat')return {...info,hint:child+' 좌석에 앉히세요',action:'앉히기'};
   if(playerFloor(p)===CELLAR_FLOOR)return {...info,hint:'계단으로 1층에 올라가세요',blocked:true};
   return {...info,hint:child+' 좌석으로 데려가세요',blocked:true};
 }
 if(isSpareWheelRole(role)){
   const wheel=WHEELS[s.wheel.broken],info={mode:'wheel',color:'#ebc58b'};
   if(role==='wheel-pickup')return {...info,hint:'예비 바퀴를 드세요',action:'예비 바퀴 들기',blocked:!canTakeSpareWheel(s)};
   if(role==='wheel-missing')return {...info,hint:'12시 방향에서 예비 바퀴를 가져오세요',blocked:true};
   if(role==='wheel-return')return {...info,hint:'예비 바퀴를 수리 원으로 가져가세요',action:'바퀴 돌려놓기'};
   return {...info,hint:wheel?wheel.name+' 쪽으로 이동하세요':'지하층 수리 원으로 이동하세요',blocked:true};
 }
 if(role==='repair')return {color:'#80b99a',action:s.wheel.progress?'차축 핀 고정':'바퀴 끼우기'};
 return {color:'#89b7cf',action:'포탄 들기',blocked:!s.ammo.stock};
}
