export const EMOTES = [
  {key:'ammo',icon:'💣',label:'포탄 필요',message:'포탄 보급을 요청합니다.'},
  {key:'load',icon:'🔄',label:'장전 필요',message:'대포 장전을 요청합니다.'},
  {key:'repair',icon:'🛠️',label:'바퀴 수리 필요',message:'바퀴 수리를 요청합니다.'},
  {key:'drive',icon:'🐎',label:'마차 조작 필요',message:'마차 조작을 요청합니다.'},
  {key:'care',icon:'🍼',label:'아이 돌봄 필요',message:'아이 돌봄을 요청합니다.'},
  {key:'good',icon:'👍',label:'좋아요',message:'좋아요를 보냈습니다.'},
  {key:'surprised',icon:'😮',label:'놀랐어요',message:'깜짝 놀랐습니다.'},
];

export const EMOTE_BUBBLE_MS = 2600;
export const EMOTE_COOLDOWN_MS = 1000;
const noticeMs = 4800;
const maxEvents = 24;
const presets = new Map(EMOTES.map(preset=>[preset.key,preset]));

export const emoteByKey = key=>presets.get(key);
const validPlayer = player=>typeof player==='string'&&/^p[1-4]$/.test(player);

export function recentEmotes(state,now){
  if(!Array.isArray(state.emotes)||!Number.isFinite(now))return [];
  return state.emotes.slice(-maxEvents).filter(event=>event&&validPlayer(event.player)&&emoteByKey(event.key)
    &&Number.isFinite(event.at)&&event.at>now-noticeMs&&event.at<=now+EMOTE_COOLDOWN_MS)
    .sort((first,second)=>first.at-second.at);
}

export function addEmote(state,player,key,now){
  if(!validPlayer(player)||!emoteByKey(key)||!Number.isFinite(now))return '사용할 수 없는 이모티콘입니다.';
  if(!['ready','run','paused'].includes(state.mode))return '작전 중에 이모티콘을 보낼 수 있습니다.';
  const events=recentEmotes(state,now);
  if(events.some(event=>event.player===player&&now-event.at<EMOTE_COOLDOWN_MS))return '잠시 뒤 이모티콘을 다시 보내세요.';
  events.push({player,key,at:Math.floor(now)});
  state.emotes=events.slice(-maxEvents);
  return null;
}
