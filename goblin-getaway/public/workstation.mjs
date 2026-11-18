import {stationAt,WHEELS,ROLES,HORSE_NAMES,horseAim,horseCondition,royalTarget,STATIONS,DECK,HZ,LANES,ROAD_TOP,ROAD_BOTTOM,WAVE_ENTRY_X,obstacleY} from './sim.mjs?v=crew7';
import {roadObstacleProfile} from './road-obstacles.mjs';

const isRoyalRole=role=>role==='royal-pickup'||role==='royal-carry'||role==='royal-seat';

export function cameraViews(s,duo,player,pixelWidth=1200,pixelHeight=350){
 const aspect=Math.max(1,pixelWidth)/Math.max(1,pixelHeight);
 const screenWidth=Math.min(1200,Math.max(650,550+300*aspect));
 const height=screenWidth/aspect;
 const playing=s.mode==='run'||s.mode==='paused';
 const ids=duo?['p1','p2']:[player];
 const focusRoles=ids.map(id=>{
   const role=playing?stationAt(s.players[id],s):null;
   return isRoyalRole(role)?null:role;
 });
 const count=duo&&focusRoles.some(Boolean)?2:1;
 return ids.slice(0,count).map((id,i)=>{
   const p=s.players[id],role=focusRoles[i],span=screenWidth/count;
   const carrying=role==='carry'||role==='catch';
   const driving=role==='driver';
   const zoom=driving?Math.min(2,span/310,height/168):role==='gunner'?2:carrying?3:role?4:Math.min(1,height/(ROAD_BOTTOM-ROAD_TOP+12),span/(DECK.right-DECK.left+64));
   // Keep the muzzle at the left and the pursuing cavalry to the right.
   const overviewCenter=count===1?Math.min(600,480+screenWidth*.10):530;
   const overviewX=Math.min(overviewCenter,WAVE_ENTRY_X-56-span/(2*zoom));
   const cx=driving?STATIONS.driver.x+50-span/(2*zoom):role==='gunner'?(count===1?617+span/4:665+span/4):carrying?(p.x*.65+STATIONS.rack.x*.35):role?p.x:overviewX;
   const cy=driving?s.y:role==='gunner'?s.y*.8+LANES[1]*.2:role?s.y+p.y:LANES[1];
   return {id,localPlayers:count===1?ids:[id],role,zoom,span,height,screenWidth,start:i*span,left:cx-span/(2*zoom),top:cy-height*.48/zoom};
 });
}
export function worldPoint(view,screenX,screenY){
 if(screenX<view.start||screenX>view.start+view.span)return null;
 return {x:view.left+(screenX-view.start)/view.zoom,y:view.top+screenY/view.zoom};
}
export function workInfo(s,p,role){
 const response={title:'',detail:'',step:'',color:'#bb91d9',roleName:ROLES[role],blocked:false};
 if(role==='driver'){
   const side=horseAim(p),delta=s.horses.right-s.horses.left;
   const threat=s.obstacles.find(o=>!o.passed&&o.x>65&&o.x<287&&Math.abs(s.y-obstacleY(o))<roadObstacleProfile(o.type).halfWidth+4);
   const slowing=s.obstacleSlow?.until>s.tick;
   const condition=side?horseCondition(s,side):null;
   const horseStatus=side=>{
     const wounds=s.horseWear?.[side]?.wounds??0;
     const status=horseCondition(s,side);
     const label=status==='angry'?'화남':status==='tired'?'지침':wounds?'상처':'정상';
     return HORSE_NAMES[side]+' '+label+(wounds?' '+wounds:'');
   };
   return {...response,mode:'whip',color:condition==='angry'?'#ffa887':response.color,
     title:side?HORSE_NAMES[side]+' 조준':'채찍질할 말을 조준하세요',
     detail:condition==='angry'?'말이 화가 나서 채찍질에 반응하지 않습니다.':condition==='tired'?'말이 지쳐 가속이 약해졌습니다. 잠시 쉬게 하세요.':'오른쪽 스틱이나 마우스로 겨냥하고 채찍질을 연타하세요.',
     step:[threat?'전방 '+roadObstacleProfile(threat.type).name:slowing?'장애물로 감속 중':delta>1?'↖ 위로':delta< -1?'↙ 아래로':'← 직진',horseStatus('right'),horseStatus('left')].join(' · '),
     blocked:!side||condition==='angry'};
 }
 if(role==='gunner'){
   const lit=s.fuse.end>0;
   return {...response,color:'#f4b46d',mode:'fuse',title:lit?'오른쪽 스틱으로 추격대를 조준하세요':s.ammo.loaded?'심지에 불을 붙이세요':'장전수가 포탄을 넣을 차례입니다',
     detail:lit?'흔들리는 조준선을 적에게 맞추세요. 자동 발사된 포탄은 경로에 닿는 적을 모두 관통합니다.':s.ammo.loaded?'점화 버튼을 누르면 2.5초 동안 발사 방향을 조정할 수 있습니다.':'대포 옆 적재칸에 포탄이 있으면 장전수 자리가 열립니다.',
     step:lit?'자동 발사까지 '+((s.fuse.end-s.tick)/HZ).toFixed(1)+'초':s.ammo.loaded?'장전 1/1 · 점화 가능':'장전 0/1 · 적재칸 '+s.ammo.rack+'발',blocked:lit||!s.ammo.loaded};
 }
 if(role==='carry')return {...response,mode:'carry',color:'#9bcde1',title:'대포 옆 파란 적재칸으로 옮기세요',detail:'포탄을 들면 걸음이 느려집니다. 적재칸에 닿으면 자동으로 내려놓습니다.',step:'적재칸 '+s.ammo.rack+'발 · 운반 중',blocked:true};
 if(role==='catch')return {...response,color:'#ffaf71',title:'포탄이 미끄러집니다!',detail:'바늘이 색 구간에 들어오면 행동을 눌러 붙잡으세요. 놓치면 마차 밖으로 떨어집니다.',step:'남은 시간 '+(Math.max(0,p.slipUntil-s.tick)/HZ).toFixed(1)+'초'};
 if(role==='loader')return {...response,color:'#a7d6aa',title:'타이밍을 맞춰 포탄을 장전하세요',detail:'바늘이 초록 구간에 들어오면 행동을 누르세요. 실패하면 적재칸의 포탄 한 발을 잃습니다.',step:'적재칸 '+s.ammo.rack+'발 · 대포 비어 있음'};
 if(isRoyalRole(role)){
   const royal=royalTarget(s,p);
   const info={...response,mode:'royal',color:'#d988a0'};
   if(!royal)return {...info,blocked:true};
   info.step='관심 '+Math.floor(royal.attention)+'%';
   if(role==='royal-pickup')return {...info,title:royal.name+'를 안아 올리세요',detail:'안아 올린 뒤 아이의 좌석으로 데려가세요.',action:'안아 올리기'};
   if(role==='royal-seat')return {...info,title:royal.name+'를 좌석에 앉히세요',detail:'자리에 앉히면 관심이 차오릅니다.',action:'앉히기'};
   return {...info,title:royal.name+'를 좌석으로 데려가세요',detail:'표시된 '+royal.name+'의 좌석 앞에 서면 앉힐 수 있습니다.',blocked:true};
 }
 if(role==='repair')return {...response,color:'#80b99a',title:s.wheel.progress?'차축 핀을 고정하세요':'바퀴를 끼우세요',detail:s.wheel.progress?'핀을 고정하면 마차 속도가 회복됩니다.':'빠진 바퀴를 장착한 뒤 핀을 고정하세요.',step:(s.wheel.progress+1)+'/2 · '+WHEELS[s.wheel.broken].name};
 return {...response,color:'#89b7cf',title:s.ammo.stock?'포탄 한 발을 드세요':'포탄 더미가 비었습니다',detail:'포탄을 든 뒤 대포 아래쪽의 파란 적재칸까지 걸어가세요.',step:'남은 포탄 '+s.ammo.stock+'발',blocked:!s.ammo.stock};
}
