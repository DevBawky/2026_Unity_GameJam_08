import {roomDb} from '#room-db';
import {initial, begin, advance, ensureDeck, operate, setInput, stationAt, leaveStation, stopStationInputs, royalActionTarget, nextStage, restartStage, restartChapter, setHumanSlots, addRackShell, cannonIndexFor, cannonState, isCannonRole, isOperationIntroActive, operationElapsedTicks, HZ} from '../../../public/sim.mjs';
import {nextStagePosition} from '../../../public/campaign.mjs';
import {submitReport,voteForCard} from '../../../public/intermission.mjs';
import {isRoomNumber,isLegacyRoomCode} from '../../../public/room-number.mjs';
import {addEmote,recentEmotes} from '../../../public/emotes.mjs';
import {normalizeCrewNickname} from '../../../public/crew-ranking.mjs';
const respond=(body,status=200)=>Response.json(body,{status,headers:{'Cache-Control':'no-store'}});
const fail=(error,status=400)=>respond({error},status);

const expiry=()=>Date.now()+2*60*60*1000;
// Pick the first free number from a random starting point. Selection and
// insertion are one write so two hosts cannot receive the same live number.
const allocateRoom=`
  WITH RECURSIVE choices(number, attempt) AS (
    VALUES (CAST(? AS INTEGER), 1)
    UNION ALL
    SELECT CASE WHEN number=999 THEN 100 ELSE number+1 END, attempt+1
    FROM choices WHERE attempt<900
  )
  INSERT INTO rooms (code,host,guest,state,clock,rev,expires,captain,seen1)
  SELECT CAST(number AS TEXT), ?, NULL, ?, ?, 0, ?, 'p1', ? FROM choices
  WHERE NOT EXISTS (SELECT 1 FROM rooms WHERE code=CAST(choices.number AS TEXT))
  LIMIT 1 RETURNING code`;
const seats=[
  {id:'p1',key:'host',seen:'seen1',empty:''},
  {id:'p2',key:'guest',seen:'seen2',empty:null},
  {id:'p3',key:'guest3',seen:'seen3',empty:null},
  {id:'p4',key:'guest4',seen:'seen4',empty:null}
];
const disconnectAfter=60_000;
const checkpointAfter=2_000;
const tickBoundary=tick=>Math.ceil(tick*1000/HZ);
const joined=row=>seats.filter(seat=>row[seat.key]);
const byToken=(row,token)=>token?seats.find(seat=>row[seat.key]===token):null;
function leader(row){
  const present=joined(row);
  return present.find(seat=>seat.id===row.captain)?.id??present[0]?.id;
}
function setCrewNickname(state,id,name){
  if(name)(state.crewNames??={})[id]=name;
  else if(state.crewNames)delete state.crewNames[id];
}
function snapshot(row,seat,now=Date.now()){
  const state=ensureDeck(JSON.parse(row.state)),members=joined(row).map(member=>member.id);
  setHumanSlots(state,members);
  let clock=row.clock;
  if(state.mode==='run'){
    // Store the last completed tick boundary so a stream of short input intervals carries forward.
    const elapsed=operationElapsedTicks(state),before=tickBoundary(elapsed);
    const due=Math.floor((now-row.clock+before)*HZ/1000)-elapsed;
    advance(state,Math.max(0,due));
    clock+=tickBoundary(operationElapsedTicks(state))-before;
  }
  if(state.emotes)state.emotes=recentEmotes(state,now);
  return {clock,data:{code:row.code,player:seat.id,host:seat.id===leader(row),captain:leader(row),crew:members.length,state,rev:row.rev,stamp:now}};
}
// Advance with the old controllers before handing an existing goblin to its new controller.
async function changeSeat(db,row,seat,newToken,now,{checkHeartbeat=false,nickname=''}={}){
  const {data:snap,clock}=snapshot(row,seat,now);
  const next={...row,[seat.key]:newToken??seat.empty,[seat.seen]:newToken?now:null,rev:row.rev+1,clock};
  next.captain=leader(next);
  setHumanSlots(snap.state,joined(next).map(member=>member.id));
  setCrewNickname(snap.state,seat.id,nickname);
  next.state=JSON.stringify(snap.state);
  const guard=checkHeartbeat?' AND '+seat.seen+' IS ?':'';
  const args=[next[seat.key],next[seat.seen],next.captain??null,next.state,clock,expiry(),row.code,row.host,row.rev,row[seat.key]];
  if(checkHeartbeat)args.push(row[seat.seen]);
  const result=await db.prepare('UPDATE rooms SET '+seat.key+'=?, '+seat.seen+'=?, captain=?, state=?, clock=?, rev=rev+1, expires=? WHERE code=? AND host=? AND rev=? AND '+seat.key+' IS ?'+guard).bind(...args).run();
  return result.meta.changes?next:null;
}
export async function POST(req){
 try{
  const b=await req.json(),op=b.op,token=req.headers.get('X-Crew-Key')??'';
  const db=await roomDb();
  if(!db)return fail('온라인 협동 데이터베이스가 설정되어 있지 않습니다.',503);
  if(op==='create'){
    const host=crypto.randomUUID(),now=Date.now(),fresh=initial();
    setCrewNickname(fresh,'p1',normalizeCrewNickname(b.nickname));
    const state=JSON.stringify(fresh);
    const start=100+crypto.getRandomValues(new Uint32Array(1))[0]%900;
    const [,inserted]=await db.batch([
      db.prepare('DELETE FROM rooms WHERE expires<=?').bind(now),
      db.prepare(allocateRoom).bind(start,host,state,now,expiry(),now)
    ]);
    const code=inserted.results[0]?.code;
    if(!code)return fail('사용 가능한 방번호가 모두 사용 중입니다. 잠시 후 다시 시도해 주세요.',503);
    return respond({...snapshot({code,host,state,clock:now,rev:0},seats[0],now).data,token:host});
  }
  const code=typeof b.code==='string'?b.code.trim().toUpperCase():'';
  if(!isRoomNumber(code)&&!isLegacyRoomCode(code))return fail('숫자 3자리 방번호를 입력해 주세요.');
  const joining=op==='join'&&!token;
  const attempt=joining&&typeof b.joinId==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(b.joinId)?b.joinId:null;
  for(let tries=0;tries<12;tries++){
    const now=Date.now();
    const row=await db.prepare('SELECT * FROM rooms WHERE code=? AND expires>?').bind(code,now).first();
    if(!row)return fail('사용 중인 방을 찾지 못했습니다. 방번호를 확인해 주세요.',404);
    const seat=byToken(row,token||attempt);
    if(!joining&&!seat)return fail('방에 다시 입장해 주세요.',403);
    const members=joined(row);
    for(const member of members){
      const seen=row[member.seen];
      // Older rooms have no heartbeat until their first request after migration.
      if(seen==null||(seat===member&&now-seen>8000)){
        await db.prepare('UPDATE rooms SET '+member.seen+'=?, expires=? WHERE code=? AND '+member.key+'=? AND '+member.seen+' IS ?').bind(now,expiry(),code,row[member.key],seen??null).run();
      }
    }
    const stale=members.find(member=>member!==seat&&row[member.seen]!=null&&now-row[member.seen]>disconnectAfter);
    if(stale){
      await changeSeat(db,row,stale,null,now,{checkHeartbeat:true});
      continue;
    }
    if(joining){
      if(seat)return respond({...snapshot(row,seat,now).data,token:attempt});
      const open=seats.find(member=>!row[member.key]);
      if(!open)return fail('이미 플레이어 4명이 탑승한 방입니다.',409);
      const guest=attempt??crypto.randomUUID(),next=await changeSeat(db,row,open,guest,now,{nickname:normalizeCrewNickname(b.nickname)});
      if(next)return respond({...snapshot(next,open,now).data,token:guest});
      continue;
    }
    const {data:snap,clock:advancedClock}=snapshot(row,seat,now);let state=snap.state;
    if(op==='sync'){
      if(advancedClock===row.clock||(state.mode==='run'&&now-row.clock<checkpointAfter))return respond(snap);
      const result=await db.prepare('UPDATE rooms SET state=?,clock=?,rev=rev+1 WHERE code=? AND host=? AND rev=?').bind(JSON.stringify(state),advancedClock,code,row.host,row.rev).run();
      if(result.meta.changes)return respond({...snap,rev:row.rev+1});
      continue;
    }
    if(op==='join')return respond(snap);
    if(op==='leave'){
      if(members.length===1){
        const result=await db.prepare('DELETE FROM rooms WHERE code=? AND host=? AND rev=?').bind(code,row.host,row.rev).run();
        if(result.meta.changes)return respond({left:true});
      }else if(await changeSeat(db,row,seat,null,now))return respond({left:true});
      continue;
    }
    const sameCampaign=(b.chapter==null||b.chapter===state.chapter)&&(b.stage==null||b.stage===state.stage);
    if(['report','vote-upgrade','depart','retry','restart-chapter'].includes(op)&&!sameCampaign)return fail('현재 스테이지 상태가 변경되었습니다.',409);
    if(op==='start'){
      if(!snap.host||state.mode!=='ready')return fail('출발 대기 중에는 방장이 작전을 시작할 수 있습니다.');
      begin(state,{operationIntro:true});
    }else if(op==='emote'){
      const error=addEmote(state,seat.id,b.key,now);
      if(error)return fail(error);
    }else if(op==='exit-station'){
      if(state.mode!=='run'||isOperationIntroActive(state)||b.stage!==state.stage||!sameCampaign)return respond(snap);
      const expectedCannon=isCannonRole(b.role)?b.cannonIndex??0:undefined;
      if(!b.role||!leaveStation(state,seat.id,b.role,expectedCannon))return respond(snap);
    }else if(op==='add-rack-shell'){
      if(b.stage!==state.stage||!sameCampaign||!addRackShell(state))return respond(snap);
    }else if(op==='act'||op==='input'){
      if(state.mode!=='run'||isOperationIntroActive(state)||b.stage!==state.stage||!sameCampaign)return respond(snap);
      const tick=Number.isInteger(b.tick)?Math.max(state.tick-12,Math.min(state.tick,b.tick)):state.tick;
      const input=b.input?.aimRole==='gunner'&&b.input.aimCannon==null?{...b.input,aimCannon:0}:b.input;
      setInput(state,seat.id,input);
      if(op==='act'){
        const player=state.players[seat.id];
        const role=stationAt(player,state),atRole=!b.role||role===b.role;
        const child=royalActionTarget(state,player,role);
        const sameChild=b.royalId?child?.id===b.royalId:role!=='royal-feed'&&role!=='royal-attention';
        const staleWhip=role==='driver'&&b.input?.whipSequence!==undefined&&b.input.whipSequence!==(player.whipSwing?.sequence??0);
        let sameCannon=true;
        if(isCannonRole(role)){
          const index=cannonIndexFor(state,player);
          const round=cannonState(state,index).cannonRecoil?.tick??0;
          const legacyAction=b.cannonIndex==null&&b.cannonRound==null&&role!=='cleaner';
          sameCannon=b.role===role&&index===(b.cannonIndex??0)&&(legacyAction||b.cannonRound===round);
        }
        if(atRole&&sameChild&&!staleWhip&&sameCannon)operate(state,seat.id,tick,b.taps);
      }
    }else if(op==='pause'&&snap.host&&['run','paused'].includes(state.mode)){
      state.mode=state.mode==='run'?'paused':'run';
      if(state.mode==='paused')stopStationInputs(state);
    }
    else if(op==='report'&&snap.host&&state.mode==='clear'){
      const error=submitReport(state,b.id);
      if(error)return fail(error);
    }else if(op==='vote-upgrade'&&state.mode==='clear'){
      // A duplicate tap from a completed vote round must not charge the team again.
      if(Number.isInteger(b.round)&&b.round<state.intermission?.round)return respond(snap);
      const error=voteForCard(state,seat.id,b.id,b.round,members.map(member=>member.id));
      if(error)return fail(error);
    }else if(op==='depart'&&snap.host&&state.mode==='clear'){
      const next=nextStage(state);
      if(next===state)return fail('마왕에게 보고를 마친 뒤 다음 스테이지로 출발하세요.');
      state=next;
    }
    else if(op==='retry'&&snap.host&&state.mode==='lost'){
      state=restartStage(state);
    }else if(op==='restart-chapter'&&snap.host&&state.mode==='clear'&&!nextStagePosition(state.chapter,state.stage)){
      state=restartChapter(state);
    }else return fail('이 역할에서는 사용할 수 없는 조작입니다.');
    // A chat signal must not reset the simulation clock between movement packets.
    const persisted=op==='emote'?{...JSON.parse(row.state),emotes:state.emotes}:state;
    const resetClock=op==='start'||op==='pause'||state!==snap.state;
    const clock=op==='emote'?row.clock:resetClock?now:advancedClock;
    const result=await db.prepare('UPDATE rooms SET state=?,clock=?,rev=rev+1,expires=? WHERE code=? AND host=? AND rev=?').bind(JSON.stringify(persisted),clock,expiry(),code,row.host,row.rev).run();
    if(result.meta.changes)return respond({...snap,state,rev:row.rev+1,stamp:now});
  }
  return fail('동시에 조작이 들어왔습니다. 다시 시도해 주세요.',409);
 }catch{return fail('협동 방 서버에 연결할 수 없습니다. 잠시 후 다시 시도해 주세요.',503);}
}
