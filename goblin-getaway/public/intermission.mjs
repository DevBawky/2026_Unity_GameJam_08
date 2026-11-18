import {nextStagePosition} from './campaign.mjs';

export const UPGRADE_CARDS = [
  {id:'repair', name:'마차 수리', desc:'내구도 35를 회복하고 이탈한 바퀴를 수리합니다.', cost:3, icon:'+'},
  {id:'sight', name:'정밀 도구', desc:'타이밍 작업의 성공 판정 구간을 넓힙니다.', cost:4, maxLevel:3, icon:'◎'},
  {id:'steady', name:'대포 고정대', desc:'조준 중 대포가 덜 흔들립니다.', cost:4, maxLevel:3, icon:'≈'},
  {id:'barrel', name:'대구경 포신', desc:'포신과 포탄이 커지고 포탄의 명중 범위가 넓어집니다.', cost:6, maxLevel:3, icon:'◉'},
  {id:'hull', name:'강화 차체', desc:'최대 내구도와 현재 내구도를 각각 20 높입니다.', cost:6, maxLevel:3, icon:'◇'},
  {id:'porter', name:'운반 장구', desc:'포탄을 들고 이동하는 속도가 빨라집니다.', cost:4, maxLevel:3, icon:'➜'},
  {id:'spring', name:'완충 차축', desc:'충격으로 인한 마차의 흔들림이 줄어듭니다.', cost:5, maxLevel:3, icon:'⌁'},
  {id:'rein', name:'길들인 군마', desc:'마차의 기본 속도가 초속 3m 높아집니다.', cost:5, maxLevel:3, icon:'»'},
];

export const REPORT_OPTIONS = [
  {id:'care', label:'왕자와 공주의 상태 보고', description:'쌍둥이가 얼마나 편하게 이동했는지 보고합니다.'},
  {id:'damage', label:'마차 피해 보고', description:'현재 내구도와 바퀴 상태를 보고합니다.'},
  {id:'supplies', label:'포탄 보급 보고', description:'남은 포탄과 손실량을 보고합니다.'},
];

const cardById = id => UPGRADE_CARDS.find(card => card.id===id);
const childCare = () => ({seatedTicks:0, attentionSum:0, reseats:0});

export function newCare(){
  return {ticks:0, prince:childCare(), princess:childCare()};
}

export function recordCare(s){
  const care=s.care??=newCare();
  care.ticks++;
  for(const child of s.royals){
    const stats=care[child.id];
    if(child.state==='seated')stats.seatedTicks++;
    stats.attentionSum+=child.attention;
  }
}

export function recordReseat(s,id){
  const care=s.care??=newCare();
  if(id==='prince'||id==='princess')care[id].reseats++;
}

export function finishCare(s){
  if(s.mode!=='clear'||s.intermission)return s.intermission??null;
  const progress=s.care;
  const care=s.royals.map(child=>{
    const stats=progress?.[child.id];
    const measured=!!stats&&progress.ticks>0;
    const seatedPercent=measured?Math.round(stats.seatedTicks/progress.ticks*100):null;
    const attention=measured?Math.round(stats.attentionSum/progress.ticks):null;
    const points=measured?Math.round((seatedPercent*.6+attention*.4)/10):0;
    return {id:child.id,name:child.name,seatedPercent,attention,reseats:stats?.reseats??0,points};
  });
  const earned=care.reduce((sum,child)=>sum+child.points,0);
  s.affection=(s.affection??0)+earned;
  s.intermission={
    phase:nextStagePosition(s.chapter??1,s.stage)?'report':'complete',
    care,earned,report:null,round:0,votes:{},lastPurchase:null,
  };
  return s.intermission;
}

export function maxDurability(s){
  return 100+20*(s.traits?.hull??0);
}

function kingReply(s,id){
  if(id==='care'){
    const {care,earned}=s.intermission;
    if(care.every(child=>child.attention===null))return '이번 구간의 돌봄 기록이 없군. 다음 구간에서는 두 아이의 상태를 살펴라.';
    const summary=care.map(child=>child.name+' '+child.points+'점').join(', ');
    const advice=earned>=14?'잘 돌봤군. 다음에도 두 아이를 살펴라.':'두 아이가 자리를 비우면 가까이 가서 좌석으로 데려다주어라.';
    return summary+'으로 확인했다. '+advice;
  }
  if(id==='damage'){
    const durability=Math.round(s.hp)+' / '+maxDurability(s);
    if(s.wheel?.broken>=0)return '내구도 '+durability+'. 바퀴가 빠졌군. 출발 전에 수리할지 상의해라.';
    return '내구도 '+durability+'로 확인했다. 다음 구간에 대비해 수리와 보강 중 무엇이 필요한지 상의해라.';
  }
  const {stock=0,rack=0,loaded=false,lost=0}=s.ammo??{};
  return '남은 포탄은 '+(stock+rack+Number(loaded))+'발, 잃은 포탄은 '+lost+'발이군. 장전수와 운반을 맡을 동료가 계속 연락하게 해라.';
}

export function submitReport(s,id){
  const session=s.intermission;
  if(s.mode!=='clear'||session?.phase!=='report')return '지금은 보고할 수 없습니다.';
  if(!REPORT_OPTIONS.some(option=>option.id===id))return '보고할 항목을 골라 주세요.';
  session.report={id,reply:kingReply(s,id)};
  session.phase='shop';
  return null;
}

export function cardCost(_s,id){
  return cardById(id)?.cost??null;
}

function purchaseBlock(s,card){
  if(!card)return '구매할 카드를 골라 주세요.';
  if(card.id==='repair'&&s.hp>=maxDurability(s)&&!(s.wheel?.broken>=0))return '지금 수리할 곳이 없습니다.';
  if(card.maxLevel!==undefined&&(s.traits?.[card.id]??0)>=card.maxLevel)return '이미 최대 강화 단계에 도달했습니다.';
  if((s.affection??0)<card.cost)return '애정 포인트가 부족합니다.';
  return null;
}

export function canBuyCard(s,id){
  return purchaseBlock(s,cardById(id))===null;
}

function applyCard(s,card){
  if(card.id==='repair'){
    s.hp=Math.min(maxDurability(s),s.hp+35);
    if(s.wheel?.broken>=0){
      Object.assign(s.wheel,{broken:-1,progress:0,since:0,first:false,eligible:s.distance+240});
    }
    return;
  }
  const traits=s.traits??={};
  traits[card.id]=(traits[card.id]??0)+1;
  if(card.id==='hull')s.hp=Math.min(maxDurability(s),s.hp+20);
}

export function voteThreshold(humanSlots){
  return Math.floor(humanSlots.length/2)+1;
}

export function pruneVotes(s,humanSlots){
  const votes=s.intermission?.votes;
  if(!votes)return;
  for(const seat of Object.keys(votes)){
    if(!humanSlots.includes(seat))delete votes[seat];
  }
}

export function voteForCard(s,playerId,id,round,humanSlots){
  const session=s.intermission;
  if(s.mode!=='clear'||session?.phase!=='shop')return '교신을 마친 뒤 구매할 수 있습니다.';
  if(!humanSlots.includes(playerId))return '참가 중인 플레이어만 투표할 수 있습니다.';
  if(round!==session.round)return '투표가 끝났습니다. 새 카드에 다시 투표해 주세요.';

  const card=cardById(id);
  const blocked=purchaseBlock(s,card);
  if(blocked)return blocked;

  pruneVotes(s,humanSlots);
  session.votes[playerId]=id;
  const count=Object.values(session.votes).filter(vote=>vote===id).length;
  if(count<voteThreshold(humanSlots))return null;

  s.affection-=card.cost;
  applyCard(s,card);
  session.lastPurchase={cardId:id,cost:card.cost};
  session.round++;
  session.votes={};
  return null;
}
